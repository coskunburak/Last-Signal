# Test matrix — authored, NOT_RUN
| Requirement | Source |
|---|---|
| Real profiles, identity resolution, one-hand primary, optional stock/support, finite scale/contact rejection, actual Generic fingers, single Animator and no target recursion | WeaponGripCorrectionTests |
| Hand calibration / exact finger restoration / attachment geometry | WeaponGripCorrectionTests.GenericPalmCalibrationAndRotationRestorationUseActualArms |
| Rifle compatibility and removed pistol lens/trigger | WeaponGripCorrectionTests; updated CharacterIntegrationAssetTests uses mode-aware validity |
| Distinct light/heavy anticipation, combat phase boundaries | WeaponGripCorrectionTests.LightAndHeavyMotionHaveDistinctAnticipationAndReturnAtCombatEnd |
| Actual Player pistol hip/ADS/fire; no support hand | WeaponGripCorrectionPlayTests.PistolHipAdsAndShotStayRightHandOnly |
| Reload interruption, switching, pause, disable/enable | ReloadInterruptionSwitchPauseAndEnableRestoreContacts |
| Real machete/axe equip/attack/recover, contact and single active window | BothMeleeWeaponsFollowCombatPhasesWithSingleDamageWindow |
| Crouch/sprint/slide, extreme pitch, rapid switching, death | CrouchSprintSlidePitchAndRapidSwitchReleaseFinalPoseOnDeath |
| New Player lifecycle / no retained reload state | NewPlayerSessionDoesNotInheritReloadOrGripState |
| Camera-specific visibility and external inspection | Existing CharacterIntegrationPlayTests (character-play gate), plus weapon-specific manual matrix |
| Seat hand ownership | Existing VehicleOccupancyTests (character-play gate), plus S021 manual inspection |
| Unchanged melee hit/damage/ammo | Existing S021CombatPlayTests and combat regression gates |

WeaponGripProbe samples final LateUpdate at order 10000; no test invokes the solver in place of the runtime pipeline. Neutral tests inspect residual as well as contact. Test fixtures isolate scene/input and do not hardcode a screen resolution. No tests have run; counts/results are not asserted here. Camera/deformation coverage still requires manual acceptance.
