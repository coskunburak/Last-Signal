#!/usr/bin/env python3
"""Owner-operated HDRP 17.5.0 migration and verification runner for Last Signal."""
import argparse
import datetime
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')

EXECUTE_METHODS = {
    'audit-active-materials': 'LastSignal.Editor.Rendering.HdrpActiveMaterialCoverageUtility.Run',
    'audit-material-integrity': 'LastSignal.Editor.Rendering.HdrpMaterialIntegrityUtility.DryRun',
    'repair-material-integrity': 'LastSignal.Editor.Rendering.HdrpMaterialIntegrityUtility.Repair',
    'verify-material-integrity': 'LastSignal.Editor.Rendering.HdrpMaterialIntegrityUtility.VerifyExisting',
    'rollback-material-integrity': 'LastSignal.Editor.Rendering.HdrpMaterialIntegrityUtility.Rollback',
    'setup-hdrp': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.SetupHdrpAssets',
    'validate-shaders': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.ValidateCustomShaders',
    'audit-materials': 'LastSignal.Editor.Rendering.HdrpMaterialMigrationUtility.AuditMaterials',
    'migrate-materials': 'LastSignal.Editor.Rendering.HdrpMaterialMigrationUtility.MigrateAllMaterialsToHdrp',
    'rollback-materials': 'LastSignal.Editor.Rendering.HdrpMaterialMigrationUtility.RollbackMaterialsToUrp',
    'create-test-scene': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.CreateIsolatedTestScene',
    'prepare-all': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.PrepareAllPreSwitch',
    'activate-hdrp': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.ActivateHdrpPipeline',
    'migrate-and-activate': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.MigrateAndActivateHdrp',
    'restore-urp': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.RestoreUrpPipeline',
    'references': 'LastSignal.Editor.FirstTown.FirstTownAtmosphereVerification.Validate',
}

TEST_ACTIONS = {
    'editmode': (
        'EditMode',
        'LastSignal.Tests.WorldTimeTests;LastSignal.Tests.FirstTownPersistenceTests',
    ),
    'playmode': (
        'PlayMode',
        'LastSignal.Tests.WorldTimeIntegrationTests;LastSignal.Tests.FirstTownPlayTests',
    ),
}

CHOICES = ['compile'] + list(EXECUTE_METHODS.keys()) + list(TEST_ACTIONS.keys()) + ['s004-edit', 's004-play']


def wait_for_unity_shutdown(timeout_sec: float = 10.0) -> None:
    deadline = time.time() + timeout_sec
    while time.time() < deadline:
        res = subprocess.run(
            ['/usr/bin/pgrep', '-fl', '/Unity.app/Contents/MacOS/Unity'],
            capture_output=True,
            text=True,
        )
        if res.returncode != 0:
            return
        time.sleep(0.4)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=CHOICES)
    parser.add_argument('--unity', type=Path, default=UNITY)
    args = parser.parse_args()

    wait_for_unity_shutdown()
    if args.action in ('s004-edit', 's004-play'):
        gate = 'edit' if args.action == 's004-edit' else 'play'
        res = subprocess.run([sys.executable, str(ROOT / 'Tools/p01-tree-verify.py'), gate, '--unity', str(args.unity)], cwd=ROOT)
        wait_for_unity_shutdown()
        return res.returncode

    spec = importlib.util.spec_from_file_location('s023_owner_guard', ROOT / 'Tools/s023-verify.py')
    guard = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(guard)
    guard.require_editor_closed(ROOT, parser)
    if not args.unity.is_file():
        parser.error('Unity executable missing; supply --unity.')
    if '6000.5.0f1' not in args.unity.parts:
        parser.error('Use project Unity version 6000.5.0f1.')

    stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    out = ROOT / 'Docs/Implementation/VisualRealismMigration/Evidence' / f'{stamp}-{args.action}'
    out.mkdir(parents=True, exist_ok=True)

    cmd = [
        str(args.unity),
        '-batchmode',
        '-projectPath',
        str(ROOT),
        '-logFile',
        str(out / 'unity.log'),
    ]

    if args.action in TEST_ACTIONS:
        platform, test_filter = TEST_ACTIONS[args.action]
        cmd += [
            '-runTests',
            '-testPlatform',
            platform,
            '-testFilter',
            test_filter,
            '-testResults',
            str(out / 'tests.xml'),
        ]
    else:
        cmd += ['-quit']
        if args.action in EXECUTE_METHODS:
            cmd += ['-executeMethod', EXECUTE_METHODS[args.action]]

    (out / 'command.json').write_text(json.dumps(cmd, indent=2) + '\n')
    result = subprocess.run(cmd, cwd=ROOT)
    wait_for_unity_shutdown()

    process_summary = {
        'action': args.action,
        'exitCode': result.returncode,
        'status': 'PASS' if result.returncode == 0 else 'FAIL',
    }

    if args.action in TEST_ACTIONS and (out / 'tests.xml').is_file():
        try:
            root = ET.parse(out / 'tests.xml').getroot()
            process_summary['tests'] = root.attrib
            passed = (
                result.returncode == 0
                and root.get('result') == 'Passed'
                and int(root.get('total', '0')) > 0
            )
            process_summary['status'] = 'PASS' if passed else 'FAIL'
        except (OSError, ET.ParseError, ValueError) as err:
            process_summary['xmlError'] = str(err)
            process_summary['status'] = 'FAIL'

    (out / 'process.json').write_text(json.dumps(process_summary, indent=2) + '\n')
    print('Evidence:', out)
    return 0 if process_summary['status'] == 'PASS' else (result.returncode or 1)


if __name__ == '__main__':
    sys.exit(main())
