# Changes in this overhaul

This list is relative to the prior grip correction, not the unrelated dirty Git baseline. Existing GUIDs/material references are retained.

- `Assets/LastSignal/Animations/Weapons/Grip/PistolGripIdle.anim`
- `Assets/LastSignal/Data/Combat/S021/MachetePose.asset`
- `Assets/LastSignal/Data/Combat/S021/PistolPose.asset`
- `Assets/LastSignal/Data/Combat/S021/WoodenAxePose.asset`
- `Assets/LastSignal/Prefabs/Combat/S021/Diesel_WorldMachete.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Diesel_WorldPistol.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Diesel_WorldWoodenAxe.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Machete_Viewmodel.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/Weapon_TacticalPistol.prefab`
- `Assets/LastSignal/Prefabs/Combat/S021/WoodenAxe_Viewmodel.prefab`
- `Assets/LastSignal/Scripts/Editor/Character/WeaponGripAuthoring.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterHandPose.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponPoseProfile.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponRig.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponSockets.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/FirstPersonWeaponGrip.cs`
- `Assets/LastSignal/Scripts/Runtime/Combat/WeaponViewPresenter.cs`
- `Assets/LastSignal/Scripts/Tests/EditMode/WeaponGripOverhaulTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/WeaponGripCorrectionPlayTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/WeaponGripOverhaulPlayTests.cs`
- `Tools/weapon-grip-verify.py`

New test sources include their .meta files. This directory contains nine focused records. Rifle/crowbar assets, gameplay definitions/controllers, Player prefab, WorldBody prefab, scenes, input, save schema and vendor source files were not edited by this overhaul.
