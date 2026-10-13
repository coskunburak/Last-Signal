#!/usr/bin/env python3
"""User-operated S021 verification: one gate and one immutable evidence directory per invocation."""
import argparse
from datetime import datetime, timezone
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
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE_ROOT = ROOT / 'Docs/Implementation/S021/Evidence'
TESTS = {
    's021-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.S021CombatTests'),
    's021-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S021CombatPlayTests'),
    'regression-edit': ('EditMode', 'LastSignal.EditModeTests;LastSignal.Art.EditorTests', None),
    'regression-play': ('PlayMode', 'LastSignal.PlayModeTests', None),
}
GATES = ('compile', *TESTS, 'development-build')
ERROR = re.compile(r'error CS\d+|Scripts have compiler errors|Compilation failed|Aborting batchmode|BuildFailedException|Fatal error|Crash!!!', re.I)
PLAYER_SETTINGS = 'ProjectSettings/ProjectSettings.asset'


def sentis_analytics_only(before, after):
    """Recognize the installed Inference package's exact Standalone define toggle."""
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


def dump(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def snapshot():
    result = {}
    for folder in ('Assets', 'Packages', 'ProjectSettings', 'Tools'):
        for path in sorted((ROOT / folder).rglob('*')):
            if path.is_file() and '__pycache__' not in path.parts:
                result[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result


def git(*args):
    return subprocess.run(['git', '-c', 'filter.lfs.required=false', '-c', 'filter.lfs.process=',
                           '-c', 'filter.lfs.clean=', *args], cwd=ROOT, capture_output=True, check=True).stdout


def editor_path(override, parser):
    if override:
        editor = override.expanduser().resolve()
    else:
        match = re.search(r'm_EditorVersion:\s*(\S+)', (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text())
        if not match:
            parser.error('ProjectVersion.txt has no Unity Editor version.')
        editor = Path('/Applications/Unity/Hub/Editor') / match.group(1) / 'Unity.app/Contents/MacOS/Unity'
    if not editor.is_file():
        parser.error('Unity Editor executable missing: ' + str(editor) + ' (use --unity PATH).')
    return editor


def require_closed(parser):
    checks = [['/usr/bin/pgrep', '-fl', '/Unity.app/Contents/MacOS/Unity']]
    lock = ROOT / 'Temp/UnityLockfile'
    if lock.exists():
        checks.append(['/usr/sbin/lsof', '-nP', str(lock)])
    for command in checks:
        try:
            status = subprocess.run(command, capture_output=True, text=True, timeout=10)
        except (OSError, subprocess.TimeoutExpired) as error:
            parser.error('Cannot establish Editor lock state: ' + str(error))
        if status.returncode == 0:
            parser.error('Unity is running or this project lock is open. Save and close the Editor normally first.')
        if status.returncode != 1 or status.stderr.strip():
            parser.error('Cannot establish Editor lock state: ' + (status.stderr.strip() or str(status.returncode)))


def legacy_guard(gate):
    if gate not in ('regression-edit', 'regression-play'):
        return None, None, None
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location('s021_legacy', ROOT / 'Tools/s015-regression.py')
    helper = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helper)
    backup = Path(tempfile.mkdtemp(prefix='lastsignal-s021-legacy-'))
    return helper, backup, helper.save_legacy(backup)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=GATES)
    parser.add_argument('--unity', type=Path, help='Override the Editor path resolved from ProjectVersion.txt.')
    parser.add_argument('--filter', help='Optional NUnit full-name filter for focused test retests.')
    args = parser.parse_args()
    if args.filter and args.gate not in TESTS:
        parser.error('--filter applies only to test gates.')
    editor = editor_path(args.unity, parser)
    require_closed(parser)
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    evidence = EVIDENCE_ROOT / (stamp + '-' + args.gate + '-' + uuid.uuid4().hex[:8])
    evidence.mkdir(parents=True, exist_ok=False)
    log = evidence / 'unity.log'
    command = [str(editor), '-batchmode', '-projectPath', str(ROOT), '-logFile', str(log)]
    env = os.environ.copy()
    output = None
    if args.gate in TESTS:
        platform, assembly, default_filter = TESTS[args.gate]
        command += ['-runTests', '-testPlatform', platform, '-assemblyNames', assembly,
                    '-testResults', str(evidence / 'tests.xml')]
        if args.filter or default_filter:
            command += ['-testFilter', args.filter or default_filter]
    elif args.gate == 'compile':
        command += ['-quit']
    else:
        output = ROOT / 'Builds/S021' / evidence.name
        env['LASTSIGNAL_S021_BUILD_OUTPUT'] = str(output)
        command += ['-buildTarget', 'OSXUniversal', '-executeMethod',
                    'LastSignal.Art.Editor.S021Build.DevelopmentMac', '-quit']
    dump(evidence / 'invocation.json', {'gate': args.gate, 'utc': stamp, 'command': command, 'project': str(ROOT)})
    (evidence / 'git-head.txt').write_bytes(git('rev-parse', 'HEAD'))
    (evidence / 'git-status-before.z').write_bytes(git('status', '--porcelain=v1', '-z'))
    before = snapshot()
    settings_before = (ROOT / PLAYER_SETTINGS).read_bytes()
    dump(evidence / 'source-before.json', before)
    print('EVIDENCE=' + str(evidence), flush=True)
    print('GATE=' + args.gate, flush=True)
    helper = backup = historical = None
    code, launch_error = None, None
    try:
        helper, backup, historical = legacy_guard(args.gate)
        code = subprocess.call(command, cwd=ROOT, env=env)
    except (OSError, KeyboardInterrupt) as error:
        launch_error = repr(error)
    finally:
        if helper is not None:
            helper.restore_legacy(backup, historical, evidence)
            shutil.rmtree(backup)
    dump(evidence / 'process.json', {'exitCode': code, 'launchError': launch_error})
    after = snapshot()
    dump(evidence / 'source-after.json', after)
    (evidence / 'git-status-after.z').write_bytes(git('status', '--porcelain=v1', '-z'))
    changed = sorted(key for key in before.keys() | after.keys() if before.get(key) != after.get(key))
    expected = []
    if PLAYER_SETTINGS in changed and sentis_analytics_only(settings_before, (ROOT / PLAYER_SETTINGS).read_bytes()):
        expected.append(PLAYER_SETTINGS)
    unexpected = [key for key in changed if key not in expected]
    dump(evidence / 'source-preservation.json', {'changed': changed,
         'expectedChanged': expected, 'unexpectedChanged': unexpected,
         'expectedChangeReason': 'Inference AnalyticsDefineManager toggled only SENTIS_ANALYTICS_ENABLED on Standalone.' if expected else None,
         'projectSettingsChanged': [key for key in changed if key.startswith('ProjectSettings/')],
         'preexistingDirtyStatus': 'git-status-before.z'})
    contents = log.read_text(errors='replace') if log.exists() else ''
    diagnostics = [line for line in contents.splitlines() if ERROR.search(line)]
    excerpt = diagnostics[:80]
    result = {'gate': args.gate, 'status': 'FAIL', 'unityExitCode': code,
              'diagnostics': len(diagnostics), 'sourceChanged': changed,
              'expectedSourceChanged': expected, 'unexpectedSourceChanged': unexpected, 'evidence': str(evidence)}
    okay = code == 0 and launch_error is None and bool(contents) and not diagnostics and not unexpected
    if args.gate in TESTS:
        try:
            xml = ET.parse(evidence / 'tests.xml').getroot()
            cases = list(xml.iter('test-case'))
            bad = [case for case in cases if case.get('result') != 'Passed']
            result['nunit'] = {'root': xml.attrib, 'discovered': len(cases),
                               'failedSkippedInconclusive': [case.get('fullname') for case in bad]}
            okay = okay and bool(cases) and xml.get('result') == 'Passed' and not bad
            if args.gate in ('s021-edit', 's021-play') and not args.filter:
                prefix = TESTS[args.gate][2] + '.'
                okay = okay and all(case.get('fullname', '').startswith(prefix) for case in cases)
            excerpt += [case.get('fullname', '') + ': ' + case.findtext('failure/message', '') for case in bad[:40]]
        except (OSError, ET.ParseError, ValueError) as error:
            result['xmlError'] = str(error)
            excerpt.append('NUnit XML: ' + str(error))
            okay = False
    elif output is not None:
        report = output / 'build.txt'
        report_text = report.read_text(errors='replace') if report.exists() else ''
        result['build'] = {'path': str(output), 'report': str(report), 'summary': report_text}
        okay = okay and (output / 'LastSignal.app').is_dir() and 'Result=Succeeded' in report_text and 'Errors=0' in report_text
    (evidence / 'failure-excerpt.txt').write_text('\n'.join(excerpt) + ('\n' if excerpt else ''))
    result['status'] = 'PASS' if okay else 'FAIL'
    dump(evidence / 'gate-result.json', result)
    print(json.dumps({'status': result['status'], 'evidence': str(evidence),
                      'sourceChanged': len(changed), 'unexpectedSourceChanged': len(unexpected),
                      'diagnostics': len(diagnostics)}, indent=2))
    return 0 if okay else 1


if __name__ == '__main__':
    sys.exit(main())
