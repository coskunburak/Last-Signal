#!/usr/bin/env python3
"""User-operated S023 verification. Runs ONE requested gate; never chains gates."""
import argparse
import datetime
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

GATES = {
    'focused-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.S023RegionTests;LastSignal.Tests.WorldCellTests'),
    'focused-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S023RegionPlayTests'),
    'regression-edit': ('EditMode', 'LastSignal.EditModeTests;LastSignal.Art.EditorTests', None),
    'regression-play': ('PlayMode', 'LastSignal.PlayModeTests', None),
}

def source_hashes(root):
    return {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest()
            for folder in ('Assets', 'Packages', 'ProjectSettings', 'Tools')
            for p in sorted((root / folder).rglob('*')) if p.is_file() and '__pycache__' not in p.parts}

def sentis_analytics_only(before, after):
    """Recognize only Inference's Standalone analytics define toggle."""
    pattern = rb'(?m)^  scriptingDefineSymbols:\r?\n(?:    (?!Standalone:)[^\r\n]*\r?\n)*    Standalone: ([^\r\n]*)'
    left, right = list(re.finditer(pattern, before)), list(re.finditer(pattern, after))
    if len(left) != 1 or len(right) != 1:
        return False
    token = b'SENTIS_ANALYTICS_ENABLED'
    a, b = left[0].group(1).split(b';'), right[0].group(1).split(b';')
    if sorted((a.count(token), b.count(token))) != [0, 1]:
        return False
    def without(data, match, symbols):
        return data[:match.start(1)] + b';'.join(s for s in symbols if s != token) + data[match.end(1):]
    return without(before, left[0], a) == without(after, right[0], b)

def write(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def require_editor_closed(root, parser):
    # File existence alone does not mean a live Unity lock. Never unlink it;
    # Unity retains responsibility for acquiring its own project lock at startup.
    commands = [['/usr/bin/pgrep', '-fl', '/Unity.app/Contents/MacOS/Unity']]
    lock = root / 'Temp/UnityLockfile'
    if lock.exists():
        commands.append(['/usr/sbin/lsof', '-nP', str(lock)])
    for command in commands:
        try:
            result = subprocess.run(command, capture_output=True, text=True, timeout=10)
        except (OSError, subprocess.TimeoutExpired) as error:
            parser.error('Cannot verify Editor is closed: ' + str(error))
        if result.returncode == 0:
            parser.error('Unity is running or the project lock is open. Close Unity normally; do not delete locks.')
        if result.returncode != 1 or result.stderr.strip():
            parser.error('Cannot verify Editor is closed: ' + (result.stderr.strip() or str(result.returncode)))
    if lock.exists():
        print('Unused UnityLockfile retained; no Unity process or open lock found.', flush=True)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=tuple(GATES) + ('development-build',))
    parser.add_argument('--unity', required=True, type=Path)
    parser.add_argument('--project', required=True, type=Path)
    parser.add_argument('--filter', help='Optional focused retest fullname; tests only.')
    args = parser.parse_args()
    root = args.project.resolve()
    if root != Path(__file__).resolve().parents[1]:
        parser.error('Project must be the repository containing this runner.')
    if not args.unity.is_file() or not os.access(args.unity, os.X_OK):
        parser.error('Unity executable missing.')
    expected_version = (root / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(':', 1)[1].strip()
    if expected_version not in args.unity.parts:
        parser.error('Use the installed Hub executable matching project version ' + expected_version)
    assembly_names = {json.loads(p.read_text()).get('name') for p in (root / 'Assets/LastSignal/Scripts/Tests').rglob('*.asmdef')}
    if args.gate in GATES:
        platform, assemblies, _ = GATES[args.gate]
        if platform not in ('EditMode', 'PlayMode') or not set(assemblies.split(';')) <= assembly_names:
            parser.error('Configured platform/assembly is unavailable.')
    build_source = root / 'Assets/LastSignal/Scripts/Editor/S023/S023Build.cs'
    if args.gate == 'development-build' and (not build_source.is_file() or 'DevelopmentMac()' not in build_source.read_text()):
        parser.error('Production build entry is unavailable.')
    require_editor_closed(root, parser)
    if args.gate == 'development-build' and args.filter:
        parser.error('A build has no test filter/platform/result XML.')
    run = Path(tempfile.mkdtemp(prefix='Manual-' + datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=3))).strftime('%Y%m%d-%H%M%S-') + args.gate + '-',
                              dir=ensure(root / 'Docs/Implementation/S023/Evidence')))
    command = [str(args.unity), '-batchmode', '-projectPath', str(root), '-logFile', str(run / 'unity.log')]
    env = os.environ.copy(); output = None
    if args.gate in GATES:
        platform, assemblies, default_filter = GATES[args.gate]
        command += ['-runTests', '-testPlatform', platform, '-assemblyNames', assemblies, '-testResults', str(run / 'tests.xml')]
        selected_filter = args.filter or default_filter
        if selected_filter: command += ['-testFilter', selected_filter]
    else:
        output = root / 'Builds/S023' / run.name
        if output.exists(): parser.error('Build destination already exists.')
        env['LASTSIGNAL_S023_BUILD_OUTPUT'] = str(output)
        command += ['-buildTarget', 'OSXUniversal', '-executeMethod', 'LastSignal.Editor.S023.S023Build.DevelopmentMac', '-quit']
    write(run / 'command.json', command)
    (run / 'head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'status.z').write_bytes(subprocess.check_output(['git', '-c', 'filter.lfs.required=false', '-c', 'filter.lfs.process=', '-c', 'filter.lfs.clean=', 'status', '--porcelain=v1', '-z'], cwd=root))
    before = source_hashes(root); write(run / 'source-before.json', before)
    settings = root / 'ProjectSettings/ProjectSettings.asset'
    settings_before = settings.read_bytes()
    (run / 'ProjectSettings.before.asset').write_bytes(settings_before)
    write(run / 'environment.json', {'unity': (root / 'ProjectSettings/ProjectVersion.txt').read_text(),
          'packages': json.loads((root / 'Packages/manifest.json').read_text()), 'executor': 'user', 'gate': args.gate})
    # Reuse established legacy-evidence preservation; no historical failed evidence is overwritten.
    backup = None; preservation = None; historical = None
    try:
        if args.gate in GATES:
            sys.dont_write_bytecode = True
            spec = importlib.util.spec_from_file_location('s015_preservation', root / 'Tools/s015-regression.py')
            preservation = importlib.util.module_from_spec(spec); spec.loader.exec_module(preservation)
            backup = Path(tempfile.mkdtemp(prefix='s023-legacy-'))
            historical = preservation.save_legacy(backup)
        print('EVIDENCE=' + str(run), flush=True)
        print('UNITY_COMMAND=' + json.dumps(command), flush=True)
        code = subprocess.call(command, cwd=root, env=env)
    except OSError as error:
        write(run / 'process-result.json', {'exitCode': None, 'error': str(error)}); return 2
    finally:
        if backup is not None and preservation is not None and historical is not None:
            preservation.restore_legacy(backup, historical, run)
            shutil.rmtree(backup)
    write(run / 'process-result.json', {'exitCode': code})
    after = source_hashes(root)
    write(run / 'source-after.json', after)
    settings_after = settings.read_bytes()
    (run / 'ProjectSettings.after.asset').write_bytes(settings_after)
    changed = sorted(p for p in before.keys() | after.keys() if before.get(p) != after.get(p))
    expected = ['ProjectSettings/ProjectSettings.asset'] if 'ProjectSettings/ProjectSettings.asset' in changed and sentis_analytics_only(settings_before, settings_after) else []
    unexpected = [p for p in changed if p not in expected]
    write(run / 'source-preservation.json', {'changed': changed, 'expectedChanged': expected,
          'unexpectedChanged': unexpected,
          'expectedChangeReason': 'Unity Inference AnalyticsDefineManager toggled only SENTIS_ANALYTICS_ENABLED on Standalone.' if expected else None})
    if output is not None:
        passed = code == 0 and not unexpected and (output / 'LastSignal.app').is_dir() and (output / 'build.txt').is_file() and 'Result=Succeeded' in (output / 'build.txt').read_text()
        write(run / 'result.json', {'build': 'PASS' if passed else 'FAIL', 'output': str(output), 'runtime': 'NOT_RUN', 'changed': changed, 'expectedChanged': expected, 'unexpectedChanged': unexpected, 'runnerExitCode': code if code else (0 if passed else 1)})
    else:
        try: result = ET.parse(run / 'tests.xml').getroot()
        except (OSError, ET.ParseError) as error:
            write(run / 'result.json', {'status': 'BLOCKED', 'error': str(error), 'runnerExitCode': code if code else 2}); return code if code != 0 else 2
        cases = list(result.iter('test-case'))
        passed = code == 0 and not unexpected and bool(cases) and result.get('result') == 'Passed' and all(c.get('result') == 'Passed' for c in cases)
        if args.gate in ('focused-edit', 'focused-play') and not args.filter:
            passed = passed and all(any(c.get('fullname', '').startswith(prefix + '.') for c in cases) for prefix in GATES[args.gate][2].split(';'))
        write(run / 'result.json', {'status': 'PASS' if passed else 'FAIL', 'nunit': result.attrib, 'caseCount': len(cases), 'changed': changed, 'expectedChanged': expected, 'unexpectedChanged': unexpected, 'passed': sum(c.get('result') == 'Passed' for c in cases), 'failed': [c.get('fullname') for c in cases if c.get('result') == 'Failed'], 'skipped': [c.get('fullname') for c in cases if c.get('result') not in ('Passed','Failed')], 'runnerExitCode': code if code else (0 if passed else 1)})
        for case in cases:
            if case.get('result') != 'Passed': print(case.get('fullname'), case.get('result'), case.findtext('failure/message', ''))
    print('PASS' if passed else 'FAIL: inspect result.json, source-preservation.json and unity.log')
    return code if code != 0 else (0 if passed else 1)

def ensure(path):
    path.mkdir(parents=True, exist_ok=True)
    return path

if __name__ == '__main__':
    sys.exit(main())
