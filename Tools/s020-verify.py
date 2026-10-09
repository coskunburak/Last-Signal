#!/usr/bin/env python3
"""User-operated S020 verification. Runs ONE requested gate; never chains gates."""
import argparse
import datetime
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

GATES = {
    's020-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.S020CatalogTests'),
    's020-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S020CatalogPlayTests'),
    'regression-edit': ('EditMode', 'LastSignal.EditModeTests;LastSignal.Art.EditorTests', None),
    'regression-play': ('PlayMode', 'LastSignal.PlayModeTests', None),
}

def source_hashes(root):
    return {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest()
            for folder in ('Assets', 'Packages', 'ProjectSettings', 'Tools')
            for p in sorted((root / folder).rglob('*')) if p.is_file() and '__pycache__' not in p.parts}

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
    if not args.unity.is_file():
        parser.error('Unity executable missing.')
    require_editor_closed(root, parser)
    if args.gate == 'development-build' and args.filter:
        parser.error('A build has no test filter/platform/result XML.')
    run = Path(tempfile.mkdtemp(prefix='Manual-' + datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=3))).strftime('%Y%m%d-%H%M%S-') + args.gate + '-',
                              dir=ensure(root / 'Docs/Implementation/S020/Evidence')))
    command = [str(args.unity), '-batchmode', '-projectPath', str(root), '-logFile', str(run / 'unity.log')]
    env = os.environ.copy(); output = None
    if args.gate in GATES:
        platform, assemblies, default_filter = GATES[args.gate]
        command += ['-runTests', '-testPlatform', platform, '-assemblyNames', assemblies, '-testResults', str(run / 'tests.xml')]
        selected_filter = args.filter or default_filter
        if selected_filter: command += ['-testFilter', selected_filter]
    else:
        output = root / 'Builds/S020' / run.name
        if output.exists(): parser.error('Build destination already exists.')
        env['LASTSIGNAL_S020_BUILD_OUTPUT'] = str(output)
        command += ['-buildTarget', 'OSXUniversal', '-executeMethod', 'LastSignal.Art.Editor.S020Build.DevelopmentMac', '-quit']
    write(run / 'command.json', command)
    (run / 'head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'status.z').write_bytes(subprocess.check_output(['git', '-c', 'filter.lfs.required=false', '-c', 'filter.lfs.process=', '-c', 'filter.lfs.clean=', 'status', '--porcelain=v1', '-z'], cwd=root))
    before = source_hashes(root); write(run / 'source-before.json', before)
    settings = root / 'ProjectSettings/ProjectSettings.asset'
    (run / 'ProjectSettings.before.asset').write_bytes(settings.read_bytes())
    write(run / 'environment.json', {'unity': (root / 'ProjectSettings/ProjectVersion.txt').read_text(),
          'packages': json.loads((root / 'Packages/manifest.json').read_text()), 'executor': 'user', 'gate': args.gate})
    # Reuse established legacy-evidence preservation; no historical failed evidence is overwritten.
    backup = None; preservation = None; historical = None
    try:
        if args.gate in ('regression-edit', 'regression-play'):
            sys.dont_write_bytecode = True
            spec = importlib.util.spec_from_file_location('s015_preservation', root / 'Tools/s015-regression.py')
            preservation = importlib.util.module_from_spec(spec); spec.loader.exec_module(preservation)
            backup = Path(tempfile.mkdtemp(prefix='s020-legacy-'))
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
    (run / 'ProjectSettings.after.asset').write_bytes(settings.read_bytes())
    changed = sorted(p for p in before.keys() | after.keys() if before.get(p) != after.get(p))
    write(run / 'source-preservation.json', {'changed': changed})
    if output is not None:
        passed = code == 0 and not changed and (output / 'LastSignal.app').is_dir() and (output / 'build.txt').is_file()
        write(run / 'result.json', {'build': 'PASS' if passed else 'FAIL', 'output': str(output), 'runtime': 'NOT_RUN'})
    else:
        try: result = ET.parse(run / 'tests.xml').getroot()
        except (OSError, ET.ParseError) as error:
            write(run / 'result.json', {'status': 'BLOCKED', 'error': str(error)}); return code if code != 0 else 2
        cases = list(result.iter('test-case'))
        passed = code == 0 and not changed and bool(cases) and result.get('result') == 'Passed' and all(c.get('result') == 'Passed' for c in cases)
        if args.gate in ('s020-edit', 's020-play') and not args.filter:
            passed = passed and all(c.get('fullname', '').startswith(GATES[args.gate][2] + '.') for c in cases)
        write(run / 'result.json', {'status': 'PASS' if passed else 'FAIL', 'nunit': result.attrib, 'caseCount': len(cases), 'changed': changed})
        for case in cases:
            if case.get('result') != 'Passed': print(case.get('fullname'), case.get('result'), case.findtext('failure/message', ''))
    print('PASS' if passed else 'FAIL: inspect result.json, source-preservation.json and unity.log')
    return code if code != 0 else (0 if passed else 1)

def ensure(path):
    path.mkdir(parents=True, exist_ok=True)
    return path

if __name__ == '__main__':
    sys.exit(main())
