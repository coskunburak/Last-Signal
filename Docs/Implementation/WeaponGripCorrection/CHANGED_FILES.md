# Task-local changed files

Existing dirty changes were preserved. No commit, push, broad scene regeneration or vendor FBX edit. All six S021 prefab GUIDs and three profile GUIDs are retained. Existing Player/WorldBody references already resolve these assets; those prefabs and S013Cabin were not regenerated.

- `Assets/LastSignal/Animations/Weapons/Grip/MacheteGrip.controller`
- `Assets/LastSignal/Animations/Weapons/Grip/MacheteGripIdle.anim`
- `Assets/LastSignal/Animations/Weapons/Grip/PistolGrip.controller`
- `Assets/LastSignal/Animations/Weapons/Grip/PistolGripIdle.anim`
- `Assets/LastSignal/Animations/Weapons/Grip/WoodenAxeGrip.controller`
- `Assets/LastSignal/Animations/Weapons/Grip/WoodenAxeGripIdle.anim`
- `Assets/LastSignal/Data/Combat/S021/MachetePose.asset`
- `Assets/LastSignal/Data/Combat/S021/PistolPose.asset`
- `Assets/LastSignal/Data/Combat/S021/WoodenAxePose.asset`
- `Assets/LastSignal/Prefabs/Combat/S021/Diesel_WorldMachete.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Diesel_WorldPistol.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Diesel_WorldWoodenAxe.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Machete_Viewmodel.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Weapon_TacticalPistol.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/WoodenAxe_Viewmodel.prefab`
- `Assets/LastSignal/Scripts/Editor/Character/CharacterWeaponRigEditor.cs`
- `Assets/LastSignal/Scripts/Editor/Character/WeaponGripAuthoring.cs`
- `Assets/LastSignal/Scripts/Editor/S021CombatAuthoring.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterHandPose.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponPoseProfile.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponRig.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponSockets.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/FirstPersonWeaponGrip.cs`
- `Assets/LastSignal/Scripts/Runtime/Combat/MeleeWeaponPresenter.cs`
- `Assets/LastSignal/Scripts/Runtime/Combat/WeaponViewPresenter.cs`
- `Assets/LastSignal/Scripts/Tests/EditMode/CharacterIntegrationAssetTests.cs`
- `Assets/LastSignal/Scripts/Tests/EditMode/WeaponGripCorrectionTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterPoseProbe.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/WeaponGripCorrectionPlayTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/WeaponGripProbe.cs`
- `Tools/s021-verify.py`
- `Tools/weapon-grip-verify.py`

New Unity source/controller/clip assets include generated `.meta` files; folder `Grip.meta` is included. This documentation directory contains the eleven requested reports. Existing rifle and crowbar prefab/profile assets were not edited.
