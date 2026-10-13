# Authored tests — no execution
The focused grip gates include both previous correction tests and the new overhaul coverage.

| Coverage | New source |
|---|---|
| Real skin bind matrices, neutral finger rest, wrong-hand rejection | WeaponGripOverhaulTests.FingersUseRealSkinBindPoseAndRejectCrossHandAssignment |
| Valid animation paths, event-free clips, no inherited pistol hand/weapon curves | OwnedClipsHaveValidBindingsAndCannotOverridePistolFingers |
| Repeat Apply/Restore has no drift | RepeatedFingerSamplingRestoresOriginalPose |
| Bounded ready blend and paused time | ReadyBlendIsBoundedAndFreezesWithoutSimulationTime |
| Nonzero impact velocity, positional continuity and exact recovery endpoint | ImpactDoesNotStopMotionAndRecoveryReturnsExactly |
| Real Player low/raised/return, shared world weight, one-hand contact | WeaponGripCorrectionPlayTests.PistolLowRaisedReadyAndReturnRemainOneHanded |
| 16:9 / 16:10 / 21:9 × 60/75/100 FOV, all three weapons | AnatomicalContactsAndSilhouettesSurviveFovAndAspectChanges |
| Heavy full-swing requested reach budget | HeavyFollowThroughKeepsReachWithinAuthoredBudget |

Play tests sample WeaponGripProbe after final IK at order 10000. Neutral palm separation and residual target <=2 cm; heavy attack residual budget <3.5 cm; neutral wrist forward-versus-forearm angle <85° is a coarse rejection bound, not proof of ideal anatomy. Viewport tests check a real mesh-local silhouette reference, not existence alone. Finger mesh penetration still requires visual acceptance.

Existing correction coverage remains: pistol ADS/fire/reload cancellation, melee active-window preservation, switching, crouch/sprint/slide/pitch, death, pause, disable/enable and new Player session. Existing CharacterIntegrationPlayTests and VehicleOccupancyTests cover camera-specific visibility/inspection/seating in the character regression gate. New S021 visual acceptance is still required for those shared systems. No PASS counts or measured runtime results are claimed.
