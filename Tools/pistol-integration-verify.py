#!/usr/bin/env python3
"""USER ONLY. One gate per call; inherits exact-version/lock/NUnit/build/source-preservation handling."""
import importlib.util
from pathlib import Path
import sys
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('triple_pistol_base', Path(__file__).with_name('s021-verify.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
runner.EVIDENCE_ROOT = runner.ROOT / 'Docs/Implementation/TriplePistolIntegration/Evidence'
runner.TESTS.update({
    'pistol-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.TriplePistolTests'),
    'pistol-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.TriplePistolPlayTests;LastSignal.Tests.WeaponGripCorrectionPlayTests.PistolFingerWrapSurvivesAdsFireAndReloadCommit'),
    'character-regression': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.CharacterIntegrationPlayTests;LastSignal.Tests.CharacterWeaponPosePlayTests;LastSignal.Tests.VehicleOccupancyTests'),
    'combat-regression': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S021CombatPlayTests;LastSignal.Tests.S014WeaponPresentationTests;LastSignal.Tests.AmmunitionIntegrationTests'),
})
runner.GATES = ('compile', *runner.TESTS, 'development-build')
if __name__ == '__main__':
    sys.exit(runner.main())
