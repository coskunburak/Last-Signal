#!/usr/bin/env python3
"""One user-operated grip gate, reusing S021 lock, process, NUnit and source-preservation handling."""
import importlib.util
from pathlib import Path
import sys
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('grip_base', Path(__file__).with_name('s021-verify.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
runner.EVIDENCE_ROOT = runner.ROOT / 'Docs/Implementation/WeaponGripOverhaul/Evidence'
runner.TESTS = {
    'melee-grip-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.MeleeGripTests;LastSignal.Tests.CharacterArmClearanceTests;LastSignal.Tests.WeaponGripCorrectionTests;LastSignal.Tests.WeaponGripOverhaulTests;LastSignal.Tests.TriplePistolTests;LastSignal.Tests.PistolAnatomyTests'),
    'melee-grip-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.MeleeGripPlayTests'),
    'character-regression': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.CharacterIntegrationPlayTests;LastSignal.Tests.VehicleOccupancyTests'),
    'combat-regression': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S021CombatPlayTests;LastSignal.Tests.S014WeaponPresentationTests;LastSignal.Tests.WeaponGripCorrectionPlayTests;LastSignal.Tests.TriplePistolPlayTests'),
    'grip-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.WeaponGripCorrectionTests;LastSignal.Tests.WeaponGripOverhaulTests'),
    'grip-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.WeaponGripCorrectionPlayTests'),
    'character-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.CharacterIntegrationAssetTests;LastSignal.Tests.CharacterWeaponPoseTests'),
    'character-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.CharacterIntegrationPlayTests;LastSignal.Tests.VehicleOccupancyTests'),
    'combat-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.S021CombatTests;LastSignal.Tests.WeaponStateTests;LastSignal.Tests.AmmunitionTransactionTests'),
    'combat-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S021CombatPlayTests;LastSignal.Tests.S014WeaponPresentationTests'),
    'regression-edit': ('EditMode', 'LastSignal.EditModeTests;LastSignal.Art.EditorTests', None),
    'regression-play': ('PlayMode', 'LastSignal.PlayModeTests', None),
}
runner.GATES = ('compile', *runner.TESTS, 'development-build')
if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] in ('compile', 'melee-grip-edit', 'melee-grip-play', 'character-regression', 'combat-regression'):
        runner.EVIDENCE_ROOT = runner.ROOT / 'Docs/Implementation/MeleeGripOverhaul/Evidence'
    sys.exit(runner.main())
