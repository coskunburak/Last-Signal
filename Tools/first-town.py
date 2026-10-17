#!/usr/bin/env python3
"""Explicit user-run First Town commands. No automatic chaining, imports, tests or bake."""
import argparse
import csv
import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / 'Docs/Implementation/FirstTown/Evidence'
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
SCENE = 'Assets/LastSignal/Scenes/Production/FirstTownCore.unity'
METHOD = 'LastSignal.Editor.FirstTown.FirstTownAuthoring.'
EDIT_FILTER = 'LastSignal.Tests.FirstTownPersistenceTests;LastSignal.Tests.SaveValidationTests;LastSignal.Tests.SaveFoundationTests'
PLAY_FILTER = 'LastSignal.Tests.FirstTownPlayTests'


def write(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def source_checks():
    """Read-only file/reference checks. Does not claim C# compilation or Unity acceptance."""
    rows = list(csv.DictReader((ROOT / 'Docs/Implementation/FirstTown/asset-usage.csv').open()))
    missing = [row['path'] for row in rows if not (ROOT / row['path']).is_file() or not (ROOT / (row['path'] + '.meta')).is_file()]
    source = (ROOT / 'Assets/LastSignal/Scripts/Editor/FirstTown/FirstTownAuthoring.cs').read_text()
    errors = ['Missing asset or meta: ' + p for p in missing]
    if 'AddComponent<CellPortal>' in source or 'new CellPortal' in source:
        errors.append('Town authoring creates a portal.')
    if 'cells.Configure(Array.Empty<WorldCellContent>()' not in source:
        errors.append('Resident-only cell topology is missing.')
    if 'first-town-v1' not in source or 'ConfigurePhysicalTraversal' not in source:
        errors.append('Isolated save slot / physical traversal wiring is missing.')
    return {'status': 'FAIL' if errors else 'PASS', 'scope': 'file/reference source checks only',
            'selectedAssets': len(rows), 'errors': errors, 'compilation': 'NOT_RUN',
            'sceneGenerated': (ROOT / SCENE).is_file(), 'gameplay': 'NOT_RUN', 'visual': 'NOT_RUN'}


def protected_hashes():
    # Existing source scene/player and tracked character/weapon assets: do not scan third-party content.
    paths = subprocess.check_output(['git', 'ls-files', '-z', 'Assets/LastSignal/Scripts/Runtime/Character',
                                    'Assets/LastSignal/Scripts/Runtime/Combat', 'Assets/LastSignal/Prefabs/Player',
                                    'Assets/LastSignal/Scenes/Production/S013Cabin.unity', 'Packages', 'ProjectSettings/EditorBuildSettings.asset'], cwd=ROOT).decode().split('\0')
    return {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in paths if p and (ROOT / p).is_file()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('static', 'create', 'bake', 'validate', 'compile', 'editmode', 'playmode', 'regression', 'build'))
    parser.add_argument('--unity', type=Path, default=UNITY)
    args = parser.parse_args()
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    stamp = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=3))).strftime('%Y%m%d-%H%M%S-')
    run = Path(tempfile.mkdtemp(prefix=stamp + args.action + '-', dir=EVIDENCE))
    if args.action == 'static':
        report = source_checks(); write(run / 'result.json', report)
        print(json.dumps(report, indent=2, ensure_ascii=False)); print('EVIDENCE=' + str(run))
        return int(report['status'] != 'PASS')
    if (ROOT / 'Temp/UnityLockfile').exists():
        parser.error('Close the Unity Editor for this project before batch commands. Use the First Town menus for an open editor.')
    if not args.unity.is_file() or not os.access(args.unity, os.X_OK):
        parser.error('Unity executable missing; pass --unity with the 6000.5.0f1 executable.')
    expected = (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(':', 1)[1].strip()
    if expected not in args.unity.parts:
        parser.error('Unity version must match ' + expected)
    if args.action != 'create' and args.action != 'compile' and args.action != 'editmode' and not (ROOT / SCENE).exists():
        parser.error('Run create first.')
    before = protected_hashes(); write(run / 'protected-before.json', before)
    (run / 'git-status.txt').write_bytes(subprocess.check_output(['git', 'status', '--short'], cwd=ROOT))
    (run / 'head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT))
    command = [str(args.unity), '-batchmode', '-projectPath', str(ROOT), '-logFile', str(run / 'unity.log')]
    if args.action in ('editmode', 'playmode', 'regression'):
        filt = EDIT_FILTER if args.action == 'editmode' else PLAY_FILTER
        if args.action == 'regression':
            filt = 'LastSignal.Tests.PersistenceIntegrationTests;LastSignal.Tests.ShelterIntegrationTests;LastSignal.Tests.VehicleIntegrationTests'
        command += ['-runTests', '-testPlatform', 'EditMode' if args.action == 'editmode' else 'PlayMode',
                    '-testFilter', filt, '-testResults', str(run / 'tests.xml')]
    else:
        methods = {'create': 'Create', 'bake': 'BakeNavigation', 'validate': 'Validate', 'build': 'Build'}
        if args.action in methods:
            command += ['-executeMethod', METHOD + methods[args.action]]
        command += ['-quit']
    write(run / 'command.json', command)
    print('EVIDENCE=' + str(run), flush=True)
    code = subprocess.call(command, cwd=ROOT)
    after = protected_hashes(); write(run / 'protected-after.json', after)
    changed = sorted(p for p in before.keys() | after.keys() if before.get(p) != after.get(p))
    success = code == 0 and not changed
    test_result = None
    if args.action in ('editmode', 'playmode', 'regression'):
        try:
            summary = ET.parse(run / 'tests.xml').getroot()
            test_result = summary.attrib
            success = success and summary.get('result') == 'Passed' and int(summary.get('total', '0')) > 0 and int(summary.get('failed', '0')) == 0
        except (OSError, ET.ParseError, ValueError):
            success = False
    report = {'operation': args.action, 'status': 'PASS' if success else 'FAIL', 'exitCode': code,
              'protectedChanges': changed, 'testResult': test_result, 'visual': 'NOT_RUN', 'performance': 'NOT_RUN',
              'note': 'Status applies only to this explicit command; inspect the native report and log.'}
    write(run / 'result.json', report)
    print(json.dumps(report, indent=2))
    return int(not success)


if __name__ == '__main__':
    sys.exit(main())
