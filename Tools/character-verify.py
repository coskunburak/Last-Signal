#!/usr/bin/env python3
"""User-operated character verification. Exactly one gate per invocation; no retries."""
import argparse
from datetime import datetime
import json
import importlib.util
import shutil
import tempfile
import os
from pathlib import Path
import re
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET

PROJECT = Path(__file__).resolve().parents[1]
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
GATES = {
    'compile': None,
    'character-edit': ('EditMode', 'LastSignal.Tests.CharacterIntegrationAssetTests'),
    'character-play': ('PlayMode', 'LastSignal.Tests.CharacterIntegrationPlayTests;LastSignal.Tests.VehicleOccupancyTests.CharacterVisualFollowsCommittedSeatAndRestoresOnExit;LastSignal.Tests.VehicleOccupancyTests.CharacterAndVehicleInspectionHaveExclusiveCameraOwnership;LastSignal.Tests.S018SurvivalPlayTests.CharacterPresentationBindsLateSurvivalAndUnsubscribesWhenDisabled'),
    'player-combat-edit': ('EditMode', 'LastSignal.Tests.PlayerHealthTests;LastSignal.Tests.WeaponStateTests;LastSignal.Tests.AmmunitionTransactionTests;LastSignal.Tests.VehicleFoundationTests;LastSignal.Tests.S018SurvivalTests'),
    'player-combat-play': ('PlayMode', 'LastSignal.Tests.MovementAcceptanceTests;LastSignal.Tests.CombatAcceptanceTests;LastSignal.Tests.S014WeaponPresentationTests;LastSignal.Tests.ScopeOpticPlayTests;LastSignal.Tests.VehicleOccupancyTests;LastSignal.Tests.S018SurvivalPlayTests'),
    'full-edit': ('EditMode', None),
    'full-play': ('PlayMode', None),
    'development-build': None,
}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=GATES)
    args = parser.parse_args()
    if not UNITY.is_file():
        parser.error(f'Unity editor not found: {UNITY}')
    # Never close/kill Unity; an unused lock file is left for Unity to acquire.
    commands = [['/usr/bin/pgrep', '-fl', '/Unity.app/Contents/MacOS/Unity']]
    lock = PROJECT / 'Temp/UnityLockfile'
    if lock.exists():
        commands.append(['/usr/sbin/lsof', '-nP', str(lock)])
    for check in commands:
        try:
            result = subprocess.run(check, capture_output=True, text=True, timeout=10)
        except (OSError, subprocess.TimeoutExpired) as error:
            parser.error('Cannot establish whether Unity is closed: ' + str(error))
        if result.returncode == 0:
            parser.error('Save your work and close Unity normally before batchmode. Do not delete locks.')
        if result.returncode != 1 or result.stderr.strip():
            parser.error('Cannot establish whether Unity is closed: ' + result.stderr.strip())
    evidence = PROJECT / 'Docs/Implementation/CharacterIntegration/Evidence' / (
        datetime.now().strftime('%Y%m%d-%H%M%S') + '-' + args.gate + '-' + uuid.uuid4().hex[:8])
    evidence.mkdir(parents=True, exist_ok=False)
    log = evidence / 'Editor.log'
    command = [str(UNITY), '-batchmode', '-projectPath', str(PROJECT), '-logFile', str(log)]
    gate = GATES[args.gate]
    if gate:
        platform, test_filter = gate
        command += ['-runTests', '-testPlatform', platform, '-assemblyNames',
                    'LastSignal.EditModeTests;LastSignal.Art.EditorTests' if args.gate == 'full-edit' else
                    'LastSignal.EditModeTests' if platform == 'EditMode' else 'LastSignal.PlayModeTests',
                    '-testResults', str(evidence / 'results.xml')]
        if test_filter:
            command += ['-testFilter', test_filter]
        # No -quit with Test Runner: Unity exits after results are written.
    elif args.gate == 'development-build':
        command += ['-buildTarget', 'OSXUniversal', '-quit', '-executeMethod', 'LastSignal.Editor.CharacterVerification.DevelopmentMac']
    else:
        command += ['-quit']
    env = dict(os.environ, LASTSIGNAL_CHARACTER_EVIDENCE=str(evidence))
    (evidence / 'invocation.json').write_text(json.dumps({'gate': args.gate, 'command': command}, indent=2))
    print(f'Gate: {args.gate}\nEvidence: {evidence}', flush=True)
    preservation = backup = historical = None
    try:
        if args.gate in ('full-edit', 'full-play'):
            # Existing suites write fixed legacy evidence paths. Reuse their preservation helper.
            sys.dont_write_bytecode = True
            spec = importlib.util.spec_from_file_location('character_legacy', PROJECT / 'Tools/s015-regression.py')
            preservation = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(preservation)
            backup = Path(tempfile.mkdtemp(prefix='lastsignal-character-'))
            historical = preservation.save_legacy(backup)
        code = subprocess.call(command, cwd=PROJECT, env=env)
    finally:
        if preservation is not None and backup is not None and historical is not None:
            preservation.restore_legacy(backup, historical, evidence)
            shutil.rmtree(backup)
    result = {'gate': args.gate, 'unity_exit_code': code, 'result': 'FAIL'}
    text = log.read_text(errors='replace') if log.exists() else ''
    diagnostics = re.findall(r'^.*(?:error CS\d+|Scripts have compiler errors|Compilation failed|Aborting batchmode due to failure).*$' , text, flags=re.M)
    okay = code == 0 and log.exists() and not diagnostics
    if gate:
        xml = evidence / 'results.xml'
        try:
            root = ET.parse(xml).getroot()
            total = int(root.attrib.get('total', root.attrib.get('testcasecount', '0')))
            failed = int(root.attrib.get('failed', root.attrib.get('failures', '0')))
            okay = okay and total > 0 and failed == 0 and root.attrib.get('result') == 'Passed'
            result.update(tests=total, failed=failed, skipped=int(root.attrib.get('skipped', '0')), xml_result=root.attrib.get('result'))
        except (OSError, ET.ParseError, ValueError) as error:
            okay = False
            result['xml_error'] = str(error)
    if args.gate == 'development-build':
        summary = evidence / 'build-summary.txt'
        okay = okay and summary.exists() and 'Result=Succeeded' in summary.read_text() and (evidence / 'LastSignal.app').exists()
    result['result'] = 'PASS' if okay else 'FAIL'
    result['compiler_diagnostics'] = diagnostics
    (evidence / 'gate-result.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))
    print('Send gate-result.json and the relevant log/XML. Run the next gate separately after reviewing this result.')
    return 0 if okay else 1

if __name__ == '__main__':
    sys.exit(main())
