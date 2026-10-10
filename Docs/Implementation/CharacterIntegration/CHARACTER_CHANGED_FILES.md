# Character changed-file inventory — 2026-10-10

Static Git inventory; no test/build status is implied. All paths are project-relative to `/Users/burakcoskun/Last Signal`. No commit or push was made. New Unity assets/scripts include their `.meta` files to preserve GUID references.

## Modified tracked integration files

- `Assets/LastSignal/Animations/WorldBody.controller`
- `Assets/LastSignal/Prefabs/Combat/Crowbar/Crowbar_Viewmodel.prefab`
- `Assets/LastSignal/Prefabs/Player/Player.prefab`
- `Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab`
- `Assets/LastSignal/Scripts/Runtime/Combat/MeleeAttackState.cs`
- `Assets/LastSignal/Scripts/Runtime/Interaction/InteractionController.cs`
- `Assets/LastSignal/Scripts/Runtime/Player/FirstPersonMotor.cs`
- `Assets/LastSignal/Scripts/Runtime/Player/PlayerInputReader.Vehicle.cs`
- `Assets/LastSignal/Scripts/Runtime/Player/PlayerLocomotionPresenter.cs`
- `Assets/LastSignal/Scripts/Runtime/Player/PlayerSurvival.cs`
- `Assets/LastSignal/Scripts/Runtime/Vehicles/VehicleInspectionView.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/S018SurvivalPlayTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/VehicleOccupancyTests.cs`

## Created character assets, code, tests and handoff

### Owned character art

- `Assets/LastSignal/Art/CharacterIntegration.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Character_Fingers.mask`
- `Assets/LastSignal/Art/CharacterIntegration/Character_Fingers.mask.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Character_UpperBody.mask`
- `Assets/LastSignal/Art/CharacterIntegration/Character_UpperBody.mask.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Consume.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Consume.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Driving_Loop.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Driving_Loop.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Interact.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Interact.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Jump_Land.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Jump_Land.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Jump_Loop.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Jump_Loop.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Jump_Start.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Jump_Start.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/PickUp_Table.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/PickUp_Table.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Pistol_Idle_Loop.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Pistol_Idle_Loop.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Sitting_Idle_Loop.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Sitting_Idle_Loop.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Sprint_Loop.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Sprint_Loop.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Walk_Loop.anim`
- `Assets/LastSignal/Art/CharacterIntegration/Clips/Walk_Loop.anim.meta`
- `Assets/LastSignal/Art/CharacterIntegration/CrowbarPose.asset`
- `Assets/LastSignal/Art/CharacterIntegration/CrowbarPose.asset.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_Humanoid.asset`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_Humanoid.asset.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldCrowbar.prefab`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldCrowbar.prefab.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldRifle.prefab`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldRifle.prefab.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Boots.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Boots.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Eyes.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Eyes.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Hair.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Hair.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Jacket.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Jacket.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Pants.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Pants.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Shirt.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Shirt.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Skin.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/Diesel_Skin.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/FPS_Jacket_0.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/FPS_Jacket_0.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/FPS_Jacket_1.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/FPS_Jacket_1.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/FPS_Skin_0.mat`
- `Assets/LastSignal/Art/CharacterIntegration/Materials/FPS_Skin_0.mat.meta`
- `Assets/LastSignal/Art/CharacterIntegration/RiflePose.asset`
- `Assets/LastSignal/Art/CharacterIntegration/RiflePose.asset.meta`

### Runtime components

- `Assets/LastSignal/Scripts/Runtime/Character.meta`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterBodyVisibility.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterBodyVisibility.cs.meta`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterFirstPersonActions.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterFirstPersonActions.cs.meta`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterInspectionCamera.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterInspectionCamera.cs.meta`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterSeatPresentation.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterSeatPresentation.cs.meta`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponPoseProfile.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponPoseProfile.cs.meta`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponRig.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponRig.cs.meta`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponSockets.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponSockets.cs.meta`

### Editor authoring / user-only build entry

- `Assets/LastSignal/Scripts/Editor/Character.meta`
- `Assets/LastSignal/Scripts/Editor/Character/CharacterControllerAuthoring.cs`
- `Assets/LastSignal/Scripts/Editor/Character/CharacterControllerAuthoring.cs.meta`
- `Assets/LastSignal/Scripts/Editor/Character/CharacterVerification.cs`
- `Assets/LastSignal/Scripts/Editor/Character/CharacterVerification.cs.meta`
- `Assets/LastSignal/Scripts/Editor/Character/DieselCharacterAuthoring.cs`
- `Assets/LastSignal/Scripts/Editor/Character/DieselCharacterAuthoring.cs.meta`

### Focused tests

- `Assets/LastSignal/Scripts/Tests/EditMode/CharacterIntegrationAssetTests.cs`
- `Assets/LastSignal/Scripts/Tests/EditMode/CharacterIntegrationAssetTests.cs.meta`
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterIntegrationPlayTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterIntegrationPlayTests.cs.meta`

### Documentation and runner

- `Docs/Implementation/CharacterIntegration/ANIMATION_COVERAGE_MATRIX.md`
- `Docs/Implementation/CharacterIntegration/ANIMATOR_REUSE_AUDIT.md`
- `Docs/Implementation/CharacterIntegration/CHARACTER_ARCHITECTURE.md`
- `Docs/Implementation/CharacterIntegration/CHARACTER_CHANGED_FILES.md`
- `Docs/Implementation/CharacterIntegration/CHARACTER_HANDOFF.md`
- `Docs/Implementation/CharacterIntegration/CHARACTER_IMPLEMENTATION_REPORT.md`
- `Docs/Implementation/CharacterIntegration/CHARACTER_MANUAL_QA.md`
- `Docs/Implementation/CharacterIntegration/CHARACTER_TEST_COMMANDS.md`
- `Docs/Implementation/CharacterIntegration/DIESEL_RIG_AUDIT.md`
- `Docs/Implementation/CharacterIntegration/FPS_WORLDBODY_SYNC.md`
- `Docs/Implementation/CharacterIntegration/SHOULDER_IK_INTEGRATION.md`
- `Tools/character-verify.py`

## Preserved inputs and unrelated observed changes

- `Assets/ThirdParty/Diesel.fbx` and `.meta`: user-imported, untracked before this work; preserved source dependency. The separate S14 Diesel import is also unchanged.
- `Assets/LastSignal/Materials/S020/drink.soda.mat` and `food.crackers.mat`: observed editor-side material normalization, outside intended character scope, not directly edited/reverted; review separately.

The production scene is referenced but not rewritten. Existing WorldBody.controller GUID is retained; original FPS controllers, tt3d source prefab, source animation FBXs/importers and both Diesel FBXs are preserved. Cosmetic rifle/crowbar meshes and existing HumanM/crouch clips remain asset dependencies through their existing GUIDs. No generated Evidence folder exists until user verification is invoked.

## Additional files from anatomical grip correction

- `Assets/LastSignal/Scripts/Runtime/Character/CharacterHandPose.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterHandPose.cs.meta`

Existing CharacterWeaponRig, pose/socket definitions, rifle prefab/profile, presenter, authoring source and focused tests were revised in place. No vendor data, Avatar or player scene was modified by this correction.
