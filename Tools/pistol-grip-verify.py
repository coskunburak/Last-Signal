#!/usr/bin/env python3
"""User-operated pistol gates. Never launches a competing Editor; preserves existing S021 evidence rules."""
import importlib.util
from pathlib import Path
import sys
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('pistol_base', Path(__file__).with_name('weapon-grip-verify.py'))
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)
runner = base.runner
runner.EVIDENCE_ROOT = runner.ROOT / 'Docs/Implementation/PistolGripOverhaul/Evidence'
runner.TESTS['pistol-edit'] = ('EditMode', 'LastSignal.EditModeTests',
    'LastSignal.Tests.PistolAnatomyTests;LastSignal.Tests.WeaponGripCorrectionTests;LastSignal.Tests.WeaponGripOverhaulTests')
prefix = 'LastSignal.Tests.WeaponGripCorrectionPlayTests.'
runner.TESTS['pistol-play'] = ('PlayMode', 'LastSignal.PlayModeTests', ';'.join(prefix + method for method in (
    'PistolHipAdsAndShotStayRightHandOnly', 'PistolLowRaisedReadyAndReturnRemainOneHanded',
    'PistolFingerWrapSurvivesAdsFireAndReloadCommit', 'ReloadInterruptionSwitchPauseAndEnableRestoreContacts',
    'CrouchSprintSlidePitchAndRapidSwitchReleaseFinalPoseOnDeath', 'NewPlayerSessionDoesNotInheritReloadOrGripState',
    'AnatomicalContactsAndSilhouettesSurviveFovAndAspectChanges')))
runner.GATES = ('compile', *runner.TESTS, 'development-build')
if __name__ == '__main__':
    sys.exit(runner.main())
