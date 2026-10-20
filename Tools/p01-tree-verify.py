#!/usr/bin/env python3
"""Owner-operated BLD-S004 verification. Runs exactly one gate, never authors or promotes scenes."""
import argparse
import datetime
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
GATES = {
    'edit': ('EditMode', ['P01TreeStructureTests', 'P01TreePersistenceTests', 'P01HarvestTests']),
    'play': ('PlayMode', ['P01TreePhysicsTests', 'P01HarvestPickupTests']),
    'physics': ('PlayMode', ['P01TreeStageTests.ActualAxeAndJointCommitSurviveMidFallSaveReloadExactlyOnce', 'P01TreeStageTests.ConfiguredChopsNeverDeleteTreeAndActualFractureRetainsOneYield', 'P01TreeStageTests.MissingPhysicalBindingCannotUseLegacyFourHitDeletion', 'P01TreeStageTests.NaturalAttackTimingRecordsNotchFallAudioAndEditorFrameSamples']),
    'persistence': ('EditMode', ['P01TreePersistenceTests', 'P01HarvestTests', 'SaveFoundationTests', 'SaveValidationTests', 'WorldCellTests']),
    'persistence-stage': ('PlayMode', ['P01TreeStageTests.PartialCutDisableReloadRetainsRevisionAndDoesNotMintYield', 'P01TreeStageTests.RealCellUnloadReloadRetainsPartialCutAndSingleFracturedYield']),
    'regression-edit': ('EditMode', ['S021CombatTests', 'MeleeGripTests', 'CharacterWeaponPoseTests', 'R02NoiseTests', 'R03AuditoryTests', 'S015AudioTests', 'ShelterDomainTests', 'WorldCellTests']),
    'regression-play': ('PlayMode', ['S021CombatPlayTests', 'MeleeGripPlayTests', 'CharacterIntegrationPlayTests', 'R02NoisePlayTests', 'R03AuditoryPlayTests', 'S015AudioPlayTests', 'P01HarvestPickupTests']),
}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=GATES)
    parser.add_argument('--unity', type=Path, default=UNITY)
    parser.add_argument('--stage', help='Owner-generated, saved, clearance-approved S004 scene; project-relative path.')
    parser.add_argument('--filter', help='Exact test fullname for a focused retest in this gate platform.')
    parser.add_argument('--chops', type=int, default=4, help='Initial disappearance-regression strike budget (1..32); test fixture only.')
    args = parser.parse_args()
    if not 1 <= args.chops <= 32: parser.error('--chops must be 1..32.')
    platform, tests = GATES[args.gate]
    assembly = 'LastSignal.' + platform + 'Tests'
    asmdef = ROOT / ('Assets/LastSignal/Scripts/Tests/' + platform + '/' + assembly + '.asmdef')
    if json.loads(asmdef.read_text())['name'] != assembly:
        parser.error('Test assembly does not match this project.')
    for test in tests:
        path = ROOT / ('Assets/LastSignal/Scripts/Tests/' + platform + '/' + test.split('.')[0] + '.cs')
        if not path.is_file(): parser.error('Missing fixture: ' + str(path))
    version = (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(':', 1)[1].strip()
    if not args.unity.is_file() or version not in args.unity.parts: parser.error('Use the installed Unity ' + version + ' executable.')
    if args.gate in ('physics', 'persistence-stage'):
        if not args.stage or not args.stage.startswith('Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_') or not args.stage.endswith('.unity'):
            parser.error('This gate requires --stage pointing to the owner-generated S004 scene.')
        stage = (ROOT / args.stage).resolve()
        if not stage.is_relative_to(ROOT / 'Assets/LastSignal/Scenes/Validation') or not stage.is_file(): parser.error('Stage file is missing or outside validation scenes.')
    # Reuse the project's existing live-process/project-lock guard; it never deletes Unity lock files.
    spec = importlib.util.spec_from_file_location('s023_owner_guard', ROOT / 'Tools/s023-verify.py')
    guard = importlib.util.module_from_spec(spec); spec.loader.exec_module(guard)
    guard.require_editor_closed(ROOT, parser)
    base = ROOT / 'Docs/Implementation/BLD/P01/S004/Evidence'; base.mkdir(parents=True, exist_ok=True)
    stamp = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=3))).strftime('%Y%m%d-%H%M%S')
    run = Path(tempfile.mkdtemp(prefix='OWNER-' + stamp + '-' + args.gate + '-', dir=base))
    command = [str(args.unity), '-batchmode', '-projectPath', str(ROOT), '-runTests', '-testPlatform', platform,
               '-assemblyNames', assembly, '-testFilter', args.filter or ';'.join('LastSignal.Tests.' + t for t in tests),
               '-testResults', str(run / 'tests.xml'), '-logFile', str(run / 'unity.log')]
    (run / 'command.json').write_text(json.dumps(command, indent=2))
    (run / 'head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT))
    (run / 'status.txt').write_bytes(subprocess.check_output(['git', 'status', '--short'], cwd=ROOT))
    if args.stage: (run / 'stage-sha256.txt').write_text(hashlib.sha256((ROOT / args.stage).read_bytes()).hexdigest())
    environment = os.environ.copy()
    environment['LASTSIGNAL_S004_CHOPS'] = str(args.chops)
    environment['LASTSIGNAL_S004_EVIDENCE'] = str(run)
    (run / 'fixture-settings.json').write_text(json.dumps({'chops': args.chops, 'stage': args.stage}, indent=2))
    if args.stage: environment['LASTSIGNAL_S004_STAGE'] = args.stage
    print('Owner-run gate:', args.gate, '\nEvidence:', run, flush=True)
    result = subprocess.run(command, cwd=ROOT, env=environment)
    passed = False; summary = {'exitCode': result.returncode, 'gate': args.gate, 'status': 'FAIL'}
    try:
        report = ET.parse(run / 'tests.xml').getroot()
        summary['tests'] = report.attrib
        passed = result.returncode == 0 and report.get('result') == 'Passed' and int(report.get('total', '0')) > 0
    except (OSError, ET.ParseError, ValueError) as error: summary['resultError'] = str(error)
    summary['status'] = 'PASS' if passed else 'FAIL'
    (run / 'result.json').write_text(json.dumps(summary, indent=2))
    print(summary['status'], '— return unity.log, tests.xml and result.json from', run)
    return 0 if passed else 1

if __name__ == '__main__':
    sys.exit(main())
