#!/usr/bin/env python3
"""Owner-operated, one-gate Unity runner for macOS and Windows. Never invoked on import."""
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
METHODS = {
    'cleanup-vendor-hdrp-metadata': 'LastSignal.Editor.Rendering.HdrpUrpMaterialMetadataCleanup.RunVendorHdrp',
    'audit-vendor-closure': 'LastSignal.Editor.Rendering.HdrpVendorDependencyAudit.Run',
    'retire-legacy-urp-settings': 'LastSignal.Editor.Rendering.HdrpLegacySettingsRetirement.Run',
    'migrate-legacy-default-material': 'LastSignal.Editor.Rendering.HdrpLegacyDefaultMaterialMigration.Run',
    'migrate-town-volume': 'LastSignal.Editor.Rendering.HdrpTownVolumeMigration.Run',
    'cleanup-legacy-urp-components': 'LastSignal.Editor.Rendering.HdrpUrpComponentCleanup.RunLegacy',
    'cleanup-urp-components': 'LastSignal.Editor.Rendering.HdrpUrpComponentCleanup.Run',
    'cleanup-urp-material-metadata': 'LastSignal.Editor.Rendering.HdrpUrpMaterialMetadataCleanup.Run',
    'prepare-development-profile': 'LastSignal.Editor.Rendering.HdrpDevelopmentProfile.Create',
    'apply-custom-fps-fingers': 'LastSignal.Editor.FpsPistolFingerAssignment.ApplyReviewedCustom9mm',
    'prepare-custom-fps-fingers': 'LastSignal.Editor.FpsPistolFingerCandidates.PrepareCustom9mm',
    'apply-fps-fingers': 'LastSignal.Editor.FpsPistolFingerAssignment.ApplyReviewedDxt',
    'prepare-fps-fingers': 'LastSignal.Editor.FpsPistolFingerCandidates.PrepareDxt',
    'repair-weapon-normals': 'LastSignal.Editor.Rendering.HdrpWeaponNormalRepair.Repair',
    'apply-mask-candidates': 'LastSignal.Editor.Rendering.HdrpProductionMaskCandidates.ApplyVerified',
    'prepare-mask-candidates': 'LastSignal.Editor.Rendering.HdrpProductionMaskCandidates.Prepare',
    'sources': 'LastSignal.Editor.FirstTownForest.ForestPrototypeAuthoring.ValidateSources',
    'generate': 'LastSignal.Editor.FirstTownForest.ForestPrototypeAuthoring.Generate',
    'references': 'LastSignal.Editor.FirstTownForest.ForestPrototypeBatch.ValidateSavedScene',
    'production-audit': 'LastSignal.Editor.Rendering.HdrpActiveMaterialCoverageUtility.RunProduction',
    'validate-shaders': 'LastSignal.Editor.Rendering.HdrpPipelineSetupUtility.ValidateCustomShaders',
}
TESTS = {
    'graphics-edit': ('EditMode', 'LastSignal.Tests.GraphicsSettingsTests'),
    'graphics-play': ('PlayMode', 'LastSignal.Tests.GraphicsSettingsPlayTests'),
    'component-id-reuse-edit': ('EditMode', ';'.join('LastSignal.Tests.HdrpUrpComponentCleanupTests.' + name for name in (
        'ReusedLightIdPreservesLightAndPhysics', 'ReusedCameraIdPreservesLensAndUnrelatedData',
        'ReusedIdRejectsDifferentOwner', 'ReusedIdRejectsDifferentComponentKind'))),
    'component-cleanup-edit': ('EditMode', 'LastSignal.Tests.HdrpUrpComponentCleanupTests'),
    'forest-shader-edit': ('EditMode', 'LastSignal.Tests.ForestHdrpBasemapTests'),
    'full-edit': ('EditMode', None),
    'full-play': ('PlayMode', None),
    'tree-stage': ('PlayMode', 'LastSignal.Tests.P01TreeStageTests'),
    'finger-contact': ('PlayMode', 'LastSignal.Tests.WeaponGripCorrectionPlayTests.PistolFingerWrapSurvivesAdsFireAndReloadCommit'),
    'persistence-play': ('PlayMode', 'LastSignal.Tests.PersistenceIntegrationTests'),
    'presentation-play': ('PlayMode', 'LastSignal.Tests.PersistenceIntegrationTests;LastSignal.Tests.TriplePistolPlayTests;LastSignal.Tests.WeaponGripCorrectionPlayTests'),
    'material-edit': ('EditMode', 'LastSignal.Tests.ArberryIntegrationAssetTests;LastSignal.Tests.CharacterIntegrationAssetTests;LastSignal.Tests.MRPolyCatalogTests;LastSignal.Tests.TriplePistolTests;LastSignal.Tests.ZombieAssetTests;LastSignal.Tests.ZombieBloodVfxTests;LastSignal.Art.Tests.S013PrefabValidationTests'),
    'audit-edit': ('EditMode', 'LastSignal.Tests.HdrpMaterialCoverageTests'),
    'edit': ('EditMode', 'LastSignal.Tests.ForestPrototypeTests'),
    'play': ('PlayMode', 'LastSignal.Tests.ForestPrototypePlayTests'),
    'regression-edit': ('EditMode', 'LastSignal.Tests.P01TreeStructureTests;LastSignal.Tests.P01TreePersistenceTests;LastSignal.Tests.FirstTownPersistenceTests'),
    'regression-play': ('PlayMode', 'LastSignal.Tests.MovementAcceptanceTests;LastSignal.Tests.P01TreePhysicsTests'),
    'world-edit': ('EditMode', 'LastSignal.Tests.WorldTimeTests;LastSignal.Tests.FirstTownPersistenceTests'),
    'world-play': ('PlayMode', 'LastSignal.Tests.WorldTimeIntegrationTests;LastSignal.Tests.FirstTownPlayTests'),
}


def discover_unity(version):
    if os.environ.get('UNITY_EDITOR'): return Path(os.environ['UNITY_EDITOR'])
    if sys.platform == 'darwin':
        return Path('/Applications/Unity/Hub/Editor') / version / 'Unity.app/Contents/MacOS/Unity'
    if os.name == 'nt':
        return Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Unity/Hub/Editor' / version / 'Editor/Unity.exe'
    return Path.home() / 'Unity/Hub/Editor' / version / 'Editor/Unity'


def require_closed():
    if os.name == 'nt':
        commands = [['powershell.exe', '-NoProfile', '-NonInteractive', '-Command',
                     "if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"]]
    else:
        pgrep = shutil.which('pgrep')
        if not pgrep: raise RuntimeError('pgrep unavailable; cannot verify Editor closed.')
        commands = [[pgrep, '-x', 'Unity']]
    for command in commands:
        r = subprocess.run(command, capture_output=True, text=True, timeout=10)
        if r.returncode == 0: raise RuntimeError('Save and close Unity normally before batch commands.')
        if r.returncode != 1 or r.stderr.strip(): raise RuntimeError('Cannot verify Editor closed: ' + r.stderr)
    if (ROOT / 'Temp/UnityLockfile').exists():
        raise RuntimeError('UnityLockfile present. Reopen/close Unity normally; this runner never removes locks.')


def validate_xml(path):
    if not path.is_file(): return False, 'Missing test XML'
    try:
        node = ET.parse(path).getroot()
        cases = node.findall('.//test-case')
        good = node.attrib.get('result') == 'Passed' and bool(cases)
        good = good and all(c.attrib.get('result') == 'Passed' for c in cases)
        return good, {'result': node.attrib.get('result'), 'testCases': len(cases), 'failed': node.attrib.get('failed')}
    except ET.ParseError as e:
        return False, str(e)


def vendor_audit_guards():
    plan_path = ROOT / 'Docs/Implementation/HDRPProductionOptimization/Evidence/vendor-closure-plan.json'
    if hashlib.sha256(plan_path.read_bytes()).hexdigest() != '7751fcfd68f1952819c29fe27f8ebe1133d54ba4d1025a304472a6a0c40aa0f6':
        raise RuntimeError('Vendor audit plan changed.')
    plan = json.loads(plan_path.read_text())
    guards = [(x['path'], x['sha256']) for x in plan['protectedInputs']]
    guards += [(x['path'], x['sha256']) for x in plan['items']]
    guards += [(x['path'] + '.meta', x['metaSha256']) for x in plan['items']]
    return [p for p, expected in guards if not (ROOT / p).is_file()
            or hashlib.sha256((ROOT / p).read_bytes()).hexdigest() != expected]


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('action', choices=['compile', *METHODS, *TESTS])
    p.add_argument('--unity', type=Path)
    p.add_argument('--output', type=Path, help='Evidence parent; timestamped subfolder always created')
    p.add_argument('--dry-run', action='store_true', help='Print command only; no Unity launch or evidence writes')
    a = p.parse_args()
    version = re.search(r'^m_EditorVersion: (.+)$', (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text(), re.M)[1]
    unity = (a.unity or discover_unity(version)).expanduser().resolve()
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    out = (a.output or ROOT / 'Docs/Implementation/HDRPProductionOptimization/Evidence') / (stamp + '-' + a.action)
    command = [str(unity), '-batchmode', '-projectPath', str(ROOT), '-logFile', str(out / 'unity.log')]
    if a.action in TESTS:
        platform, test_filter = TESTS[a.action]
        command += ['-runTests', '-testPlatform', platform, '-testResults', str(out / 'results.xml')]
        if test_filter:
            command += ['-testFilter', test_filter]
    else:
        if a.action in METHODS: command += ['-executeMethod', METHODS[a.action]]
        command += ['-quit']
    if a.dry_run:
        print(json.dumps(command, indent=2)); return 0
    if version not in unity.parts or not unity.is_file():
        p.error('Set --unity or UNITY_EDITOR to the Hub executable for Unity ' + version)
    try: require_closed()
    except (OSError, RuntimeError, subprocess.TimeoutExpired) as e: p.error(str(e))
    if a.action == 'audit-vendor-closure':
        changed = vendor_audit_guards()
        if changed: p.error('Vendor audit inputs changed before launch: ' + ', '.join(changed))
    out.mkdir(parents=True, exist_ok=False)
    receipt = {'stage': os.environ.get('LASTSIGNAL_S004_STAGE', ''), 'action': a.action, 'startedUtc': stamp, 'command': command, 'status': 'RUNNING', 'visualStatus': 'NOT_REVIEWED'}
    (out / 'process.json').write_text(json.dumps(receipt, indent=2) + '\n')
    result = subprocess.run(command, cwd=ROOT)
    receipt['exitCode'] = result.returncode
    log_path = out / 'unity.log'
    log = log_path.read_text(errors='replace') if log_path.exists() else ''
    errors = re.findall(r'^.*(?:error CS\d+|Shader error in|Scripts have compiler errors|Failed to resolve packages|An error occurred while resolving packages|Aborting batchmode due to failure).*$', log, re.M)
    receipt['diagnostics'] = errors
    success = result.returncode == 0 and bool(log) and not errors
    # Import/quit callbacks may run after executeMethod writes its receipt. Verify
    # the read-only audit's source hashes again after the Editor process has exited.
    if a.action == 'audit-vendor-closure':
        receipt['postExitChangedInputs'] = vendor_audit_guards()
        success = success and not receipt['postExitChangedInputs']
    if a.action == 'cleanup-vendor-hdrp-metadata':
        match = re.search(r'^URP material metadata cleanup: (.+?) — ', log, re.M)
        cleanup_path = (ROOT / match[1] / 'cleanup.json') if match else None
        changed = []
        if cleanup_path and cleanup_path.is_file():
            cleanup = json.loads(cleanup_path.read_text())
            plan = json.loads((cleanup_path.parent / 'plan.json').read_text())
            checks = [(x['path'], x['sha256']) for x in plan['protectedInputs']]
            checks += [(x['path'], x['afterSha256']) for x in cleanup['entries']]
            checks += [(x['path'] + '.meta', x['metaSha256']) for x in cleanup['entries']]
            changed = [path for path, expected in checks if not expected or not (ROOT / path).is_file()
                       or hashlib.sha256((ROOT / path).read_bytes()).hexdigest() != expected]
            success = success and cleanup.get('status') == 'APPLIED_MATERIAL_CONTENT_VERIFIED' and len(cleanup['entries']) == 36
        else:
            changed = ['Missing vendor metadata cleanup receipt']
        receipt['postExitChangedInputs'] = changed
        success = success and not changed
    if a.action == 'retire-legacy-urp-settings':
        match = re.search(r'^Legacy settings retirement receipt: (.+)$', log, re.M)
        report_path = (ROOT / match[1].strip()) if match else None
        expected = json.loads(report_path.read_text()).get('graphicsAfterSha256') if report_path and report_path.is_file() else None
        actual = hashlib.sha256((ROOT / 'ProjectSettings/GraphicsSettings.asset').read_bytes()).hexdigest()
        receipt['postExitGraphicsHashMatches'] = bool(expected) and expected == actual
        success = success and receipt['postExitGraphicsHashMatches']
    if a.action in TESTS:
        passed, receipt['tests'] = validate_xml(out / 'results.xml')
        success = success and passed
        receipt['status'] = 'TESTS_PASSED' if success else 'FAILED'
    else:
        receipt['status'] = 'PROCESS_COMPLETED_REVIEW_REQUIRED' if success else 'FAILED'
    receipt['completedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    (out / 'process.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(str(out)); print(receipt['status'])
    return 0 if success else 1


if __name__ == '__main__':
    raise SystemExit(main())
