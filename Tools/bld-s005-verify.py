#!/usr/bin/env python3
"""Owner-only BLD-S005 verification: exactly one gate; no authoring, build, or gate chaining."""
import argparse
import datetime
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
GATES = {
    'focused-edit': ('EditMode', ['P02RecipeValidationTests', 'P02CraftingTests', 'P02CraftTimeTests']),
    'focused-play': ('PlayMode', ['P02CraftIntegrationTests']),
    'integration': ('EditMode', ['P02CraftPersistenceTests', 'S010ProductionTests', 'InventorySnapshotTests', 'SaveValidationTests', 'SaveFoundationTests']),
    'regression-edit': ('EditMode', ['ShelterDomainTests', 'P01HarvestTests', 'WorldTimeTests', 'WorldCellTests']),
    'regression-play': ('PlayMode', ['S010IntegrationTests', 'S020CatalogPlayTests', 'P01HarvestPickupTests', 'PersistenceIntegrationTests', 'WorldTimeIntegrationTests']),
}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=GATES)
    parser.add_argument('--unity', type=Path, default=UNITY)
    parser.add_argument('--filter', help='Exact LastSignal.Tests.Class or Class.Method in the selected gate; no regex.')
    args = parser.parse_args()
    platform, classes = GATES[args.gate]
    version_file = ROOT / 'ProjectSettings/ProjectVersion.txt'
    if not version_file.is_file() or not (ROOT / 'Assets/LastSignal/Scripts').is_dir(): parser.error('Invalid Unity project directory.')
    version = version_file.read_text().splitlines()[0].split(':', 1)[1].strip()
    if not args.unity.is_file() or not os.access(args.unity, os.X_OK) or version not in args.unity.parts:
        parser.error('Use the installed executable for Unity ' + version + '. A valid Unity license is required.')
    assembly = 'LastSignal.' + platform + 'Tests'
    folder = ROOT / ('Assets/LastSignal/Scripts/Tests/' + platform)
    asmdef = folder / (assembly + '.asmdef')
    if not asmdef.is_file() or json.loads(asmdef.read_text())['name'] != assembly: parser.error('Missing/mismatched test assembly.')
    for name in classes:
        if not (folder / (name + '.cs')).is_file(): parser.error('Missing test fixture: ' + name)
    selected = ['LastSignal.Tests.' + c for c in classes]
    if args.filter:
        match = re.fullmatch(r'LastSignal\.Tests\.([A-Za-z_][A-Za-z0-9_]*)(?:\.([A-Za-z_][A-Za-z0-9_]*))?', args.filter)
        if not match or match.group(1) not in classes: parser.error('--filter must name a class/method in this gate.')
        if match.group(2) and not re.search(r'\b' + re.escape(match.group(2)) + r'\s*\(', (folder / (match.group(1) + '.cs')).read_text()):
            parser.error('Test method was not found in the selected source fixture.')
        selected = [args.filter]
    spec = importlib.util.spec_from_file_location('s005_owner_guard', ROOT / 'Tools/s023-verify.py')
    guard = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(guard)
    guard.require_editor_closed(ROOT, parser)  # Never deletes locks or closes an Editor.
    base = ROOT / 'Docs/Implementation/BLD/P02/S005/Evidence'
    base.mkdir(parents=True, exist_ok=True)
    stamp = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=3))).strftime('%Y%m%d-%H%M%S')
    run = Path(tempfile.mkdtemp(prefix='OWNER-' + stamp + '-' + args.gate + '-', dir=base))
    command = [str(args.unity), '-batchmode', '-projectPath', str(ROOT), '-runTests',
               '-testPlatform', platform, '-assemblyNames', assembly, '-testFilter', ';'.join(selected),
               '-testResults', str(run / 'tests.xml'), '-logFile', str(run / 'unity.log')]
    # No -quit: Unity Test Runner owns termination after results publication.
    request = {'gate': args.gate, 'platform': platform, 'classes': selected, 'command': command, 'unityVersion': version,
               'status': 'OWNER_RUNNING', 'timezone': 'Europe/Istanbul'}
    (run / 'request.json').write_text(json.dumps(request, indent=2) + '\n')
    print('Evidence:', run, flush=True)
    try:
        result = subprocess.run(command, cwd=ROOT, check=False)
    except OSError as error:
        (run / 'result.json').write_text(json.dumps({'status': 'FAIL_OWNER_VERIFIED', 'launchError': str(error)}, indent=2) + '\n')
        print('Unity launch failed:', error, file=sys.stderr)
        return 127
    summary = {'gate': args.gate, 'unityExitCode': result.returncode, 'status': 'FAIL_OWNER_VERIFIED'}
    passed = False
    try:
        report = ET.parse(run / 'tests.xml').getroot()
        summary['tests'] = dict(report.attrib)
        passed = (result.returncode == 0 and report.get('result') == 'Passed' and int(report.get('total', '0')) > 0
                  and int(report.get('failed', '0')) == 0 and int(report.get('skipped', '0')) == 0
                  and int(report.get('inconclusive', '0')) == 0)
    except (OSError, ET.ParseError, ValueError) as error:
        summary['resultError'] = str(error)
    if passed: summary['status'] = 'PASS_OWNER_VERIFIED'
    (run / 'result.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(summary['status'], '— return tests.xml, unity.log, result.json from', run)
    # Preserve actual nonzero Unity exit codes. A zero exit with missing/failed XML is a runner failure.
    return result.returncode if result.returncode != 0 else (0 if passed else 1)

if __name__ == '__main__':
    sys.exit(main())
