# Animator reuse audit

Initial inspection identified:

| Controller | Initial use | Decision |
|---|---|---|
| `Assets/LastSignal/Animations/WorldBody.controller` | Real Player/WorldBody; space_crew_man Avatar | REUSABLE / EXTENDED, same GUID `a35d1340d221840209f9f1dc49574e2a` |
| `Assets/LastSignal/Animations/VAL_MRPoly.controller` | Production rifle VAL viewmodel | Retained |
| `Assets/LastSignal/Animations/FPSArms.controller` | Older FPSArms prefab | Retained; not selected as a world-body controller |
| `Assets/LastSignal/Animations/Weapons/Crowbar/Crowbar.controller` | Crowbar FPS viewmodel | Retained |
| Vendor demo / zombie controllers | Unrelated animation demos/enemies | Not modified or presented as player controllers |

Original world controller: one unmasked Base Layer, no IK pass or StateMachineBehaviours; Standing Idle, Standing Move, Crouch Locomotion, Slide. Eight-way run and directional crouch blend trees, local Mixamo crouch/slide sources. Six parameters: MoveX, MoveY, HorizontalSpeed, PlaybackRate, IsCrouching, IsSliding. No combat/health/air/seat integration. Original HumanM locomotion and Mixamo clips were already Humanoid.

Extension preserves those states/transitions and original directional run tree. After the user's 2026-10-10 gait defect report, Standing Move again uses the coherent eight-way HumanM Standing Directional tree directly. The converted UAL walk/sprint curves advance the foot forward during their low-foot phase and have been disconnected from locomotion, without deleting the assets or their old blend trees. PlaybackRate follows measured speed / 3.2, so normal movement uses rate 1 and sprint about 1.72. Lower-speed movement is a retimed run, not a dedicated walk; sprint is accelerated run, not a separate sprint pose. Feet synchronization still requires visual acceptance; no stride warping is claimed. Crouch direction retains existing mirrored left/right reuse; no substitute slide introduced.

Added Base states: Character Jump/Fall/Land/Death/Seated. State guards prioritize death, seated, then airborne over locomotion. Root motion remains disabled. Base Layer owns the single IK pass.

Character Actions layer: upper-body AvatarMask; Interact, Pickup, Consume, Hit and MeleeActive; empty No Action default with default weight zero. The driver activates it on an accepted request/active melee and mutes it after completion, preventing an empty Write Defaults Off state from retaining an arm pose over the firearm stance. Root/legs excluded. Death/seat cancel the action layer; gameplay firing/equip/reload interrupt and mute cosmetic gestures through their outgoing transition. Melee clip time comes from MeleeAttackState.PresentationProgress.

Character Grip layer: the fingers-only mask and UAL pistol clip remain preserved, but runtime weight is now zero. CharacterHandPose applies Diesel-calibrated finger flexion after the final firearm arm solve. FingerGripWeight now controls this procedural closure. This avoids competing generic finger curves and restores animation ownership during gestures, treatment, death and seating.

UAL1/UAL2 imports were Generic. The authoring tool used temporary project-owned copies, explicit Humanoid mapping, then saved only selected independent muscle clips under `Art/CharacterIntegration/Clips` and removed the temporary copies. Vendor FBX and importer metadata remain unchanged. No replacement world controller or duplicate parameter names were introduced. All runtime behavior and transitions are NOT_RUN.
