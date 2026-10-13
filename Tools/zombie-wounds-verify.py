#!/usr/bin/env python3
"""User-operated zombie wounds verification. Runs exactly one explicitly selected gate."""
import argparse
from datetime import datetime, timezone
import importlib.util
import json
import os
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import sys
import tempfile
import uuid
import xml.etree.ElementTree as ET

PROJECT = Path(__file__).resolve().parents[1]
EDIT = ['ZombieWoundTests', 'CombatFeedbackTests', 'ZombieBloodVfxTests', 'ZombieDamageTests',
        'ZombieImpactReactionTests', 'WorldPopulationManagerTests', 'SaveFoundationTests']
PLAY = ['ZombieWoundPlayTests', 'CombatFeedbackPlayTests', 'ZombieBloodVfxPlayTests',
        'ZombieDamagePlayTests', 'ZombieDismembermentPlayTests', 'WorldPopulationPlayTests', 'PersistenceIntegrationTests']
GATES = ('focused-edit', 'focused-play', 'regression-edit', 'regression-play', 'development-build')


def editor_path(override):
    version_file = PROJECT / 'ProjectSettings/ProjectVersion.txt'
    match = re.search(r'^m_EditorVersion: (.+)$', version_file.read_text(), re.M)
    if not match:
        raise RuntimeError('Cannot read the project Unity version.')
    version = match.group(1).strip()
    paths = [Path(override).expanduser()] if override else []
    if os.environ.get('UNITY_EDITOR'):
        paths.append(Path(os.environ['UNITY_EDITOR']).expanduser())
    paths += [Path('/Applications/Unity/Hub/Editor') / version / 'Unity.app/Contents/MacOS/Unity',
              Path.home() / 'Applications/Unity/Hub/Editor' / version / 'Unity.app/Contents/MacOS/Unity']
    for candidate in paths:
        if candidate.is_dir() and candidate.suffix == '.app':
            candidate /= 'Contents/MacOS/Unity'
        if candidate.is_file() and os.access(candidate, os.X_OK):
            return candidate.resolve(), version
    raise RuntimeError(f'Unity {version} not found. Supply --unity /path/to/Unity.app (or UNITY_EDITOR).')


def ensure_closed():
    # Conservative: refuse if ANY Unity Editor is open. Never delete a lock or close the user's Editor.
    checks = [['/usr/bin/pgrep', '-fl', '/Unity.app/Contents/MacOS/Unity']]
    lock = PROJECT / 'Temp/UnityLockfile'
    if lock.exists():
        checks.append(['/usr/sbin/lsof', '-nP', str(lock)])
    for command in checks:
        result = subprocess.run(command, capture_output=True, text=True, timeout=10)
        if result.returncode == 0:
            raise RuntimeError('Save your work and close Unity normally before this batch gate. No process was started or stopped.')
        if result.returncode != 1 or result.stderr.strip():
            raise RuntimeError('Could not establish that Unity is closed: ' + result.stderr.strip())


def expected_methods(fixtures, platform):
    methods = {}
    for fixture in fixtures:
        path = PROJECT / f'Assets/LastSignal/Scripts/Tests/{platform}/{fixture}.cs'
        source = path.read_text()
        for attrs, method in re.findall(r'((?:\s*\[[^\]]+\])+)[ \t\r\n]*public\s+(?:void|IEnumerator)\s+(\w+)\s*\(', source):
            if re.search(r'\[(?:Test|TestCase|UnityTest)(?:\]|\()', attrs):
                methods[f'LastSignal.Tests.{fixture}.{method}'] = max(1, len(re.findall(r'\[TestCase\(', attrs)))
    if not methods:
        raise RuntimeError('Expected test manifest is empty; refusing an unverifiable test run.')
    return dict(sorted(methods.items()))


def inspect_xml(path, expected):
    root = ET.parse(path).getroot()
    cases = list(root.iter('test-case'))
    names = [case.get('fullname', '') for case in cases]
    missing = [name for name, minimum in expected.items() if sum(n == name or n.startswith(name + '(') for n in names) < minimum]
    failed = [c.get('fullname') for c in cases if c.get('result') != 'Passed']
    okay = bool(cases) and not missing and not failed and root.get('result') == 'Passed'
    return okay, {'count': len(cases), 'missing_required_methods': missing, 'non_passing_cases': failed,
                  'xml_result': root.get('result'), 'failed': root.get('failed'), 'skipped': root.get('skipped')}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=GATES)
    parser.add_argument('--unity', help='Installed Unity executable or Unity.app; project version is read from ProjectVersion.txt.')
    args = parser.parse_args()
    try:
        unity, version = editor_path(args.unity)
        ensure_closed()
    except (RuntimeError, OSError, subprocess.TimeoutExpired) as error:
        parser.error(str(error))
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ')
    evidence = PROJECT / 'Docs/Implementation/Combat/ZombieWounds/Evidence' / f'{stamp}-{args.gate}-{uuid.uuid4().hex[:8]}'
    evidence.mkdir(parents=True, exist_ok=False)
    log = evidence / 'Unity.log'
    command = [str(unity), '-batchmode', '-projectPath', str(PROJECT), '-logFile', str(log)]
    expected = []
    if args.gate == 'development-build':
        command += ['-buildTarget', 'OSXUniversal', '-quit', '-executeMethod', 'LastSignal.Editor.CombatFeedbackBuild.DevelopmentMac']
    else:
        platform = 'EditMode' if args.gate.endswith('edit') else 'PlayMode'
        fixtures = EDIT if platform == 'EditMode' else PLAY
        expected = expected_methods(fixtures, platform)
        if args.gate.startswith('focused'):
            expected.pop('LastSignal.Tests.ZombieDamagePlayTests.ProfileLiveAndMixedPopulationAndWarmedDamage', None)
            # Full persistence fixture belongs to regression; focus on the new terminal-snapshot boundary.
            expected = {name: count for name, count in expected.items() if '.PersistenceIntegrationTests.' not in name or name.endswith('.ZombieCorpseRemovalSaveAndCompletedEncounterReload')}
        assemblies = 'LastSignal.EditModeTests;LastSignal.Art.EditorTests' if args.gate == 'regression-edit' else 'LastSignal.EditModeTests' if platform == 'EditMode' else 'LastSignal.PlayModeTests'
        command += ['-runTests', '-testPlatform', platform, '-assemblyNames', assemblies, '-testResults', str(evidence / 'results.xml')]
        if args.gate.startswith('focused'):
            command += ['-testFilter', ';'.join(expected)]
        # No -quit or -nographics: retain graphics and let Test Runner finish writing XML.
    (evidence / 'invocation.json').write_text(json.dumps({'gate': args.gate, 'utc': stamp, 'project': str(PROJECT), 'project_unity_version': version, 'command': command, 'required_methods': expected}, indent=2))
    (evidence / 'command.txt').write_text(shlex.join(command) + '\n')
    for name, git_args in [('git-head.txt', ['rev-parse', 'HEAD']), ('git-status.txt', ['status', '--porcelain=v1']), ('git-diff-stat.txt', ['diff', '--stat', '--', 'Assets/LastSignal', 'Tools'])]:
        result = subprocess.run(['git', *git_args], cwd=PROJECT, capture_output=True)
        (evidence / name).write_bytes(result.stdout + result.stderr)
    print(f'Gate: {args.gate}\nEvidence: {evidence}', flush=True)
    env = dict(os.environ, LS_COMBAT_EVIDENCE=str(evidence))
    legacy = backup = historical = None
    code = -1
    error = None
    try:
        # Existing focused population tests may also write fixed historical paths. Preserve on every test gate.
        if expected:
            sys.dont_write_bytecode = True
            spec = importlib.util.spec_from_file_location('wound_legacy', PROJECT / 'Tools/s015-regression.py')
            legacy = importlib.util.module_from_spec(spec); spec.loader.exec_module(legacy)
            backup = Path(tempfile.mkdtemp(prefix='lastsignal-wound-verification-'))
            historical = legacy.save_legacy(backup)
        code = subprocess.call(command, cwd=PROJECT, env=env)
    except (OSError, KeyboardInterrupt) as exc:
        error = repr(exc)
    finally:
        (evidence / 'exit-code.txt').write_text(str(code) + '\n')
        if legacy and backup and historical is not None:
            legacy.restore_legacy(backup, historical, evidence)
            shutil.rmtree(backup)
    text = log.read_text(errors='replace') if log.exists() else ''
    diagnostics = re.findall(r'^.*(?:error CS\d+|Shader error|Scripts have compiler errors|Compilation failed|Aborting batchmode|Crash!!!|Fatal error|BuildFailedException|Unloading broken assembly).*$', text, re.M | re.I)
    # NUnit/Unity's log assertion system reports unexpected errors as failed cases. Preserve all exception
    # candidates for human review as well; expected LogAssert messages are not silently called failures.
    exception_lines = re.findall(r'^.*(?:Exception:|Assertion failed|\[Error\]).*$', text, re.M)
    okay = code == 0 and bool(text) and not diagnostics and error is None
    result = {'gate': args.gate, 'utc': stamp, 'unity_exit_code': code, 'diagnostics': diagnostics, 'exception_candidates': exception_lines, 'launch_error': error}
    if expected:
        try:
            xml_okay, summary = inspect_xml(evidence / 'results.xml', expected)
            result.update(summary); okay = okay and xml_okay
        except (OSError, ET.ParseError, ValueError) as exc:
            result['xml_error'] = str(exc); okay = False
    else:
        summary = evidence / 'build-summary.txt'
        build_text = summary.read_text() if summary.exists() else ''
        build_okay = 'Result=Succeeded\n' in build_text and 'Errors=0\n' in build_text and (evidence / 'LastSignal.app/Contents/MacOS').is_dir()
        result['build_summary'] = build_text; okay = okay and build_okay and not exception_lines
    # Exceptions require a human to reconcile intentional LogAssert messages with the XML and stack traces.
    result['result'] = 'REVIEW_REQUIRED' if okay and exception_lines else 'PASS' if okay else 'FAIL'
    (evidence / 'gate-result.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))
    print('Return gate-result.json, invocation.json, exit-code.txt, Unity.log and results.xml (if produced). Do not run the next gate until reviewed.')
    return 0 if result['result'] == 'PASS' else 2 if result['result'] == 'REVIEW_REQUIRED' else 1


if __name__ == '__main__':
    sys.exit(main())
