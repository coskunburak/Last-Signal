# Files in this pistol revision

Runtime under `Assets/LastSignal/Scripts/Runtime/`:
* `Character/PistolFingerPose.cs` — per-skeleton bind-relative grip/trigger asset.
* `Character/PistolReloadPose.cs` — authoritative timer/commit presentation sampling.
* `Character/CharacterHandPose.cs` — optional authored pose path; generic rifle/melee path retained.
* `Character/CharacterWeaponPoseProfile.cs` — per-rig assets and configurable reload presentation.
* `Character/FirstPersonWeaponGrip.cs` — FPS fingers and magazine/offhand sampling.
* `Character/CharacterWeaponRig.cs` — Diesel fingers and matching reload composition.
* `Combat/WeaponViewPresenter.cs` — shared pistol reload lift/roll.

Persistent assets:
* `Assets/LastSignal/Data/Combat/S021/PistolPose.asset`.
* `Assets/LastSignal/Data/Combat/S021/PistolFingers_FPS.asset` and `PistolFingers_Diesel.asset` (new).
* `Assets/LastSignal/Prefabs/Combat/S021/Weapon_TacticalPistol.prefab` and `Diesel_WorldPistol.prefab`.

Editor under `Assets/LastSignal/Scripts/Editor/Character/`: new `PistolGripAuthoring.cs`, new `FirstPersonWeaponGripEditor.cs`, existing `WeaponGripAuthoring.cs` calls pistol pass last.

Tests under `Assets/LastSignal/Scripts/Tests/`: new `EditMode/PistolAnatomyTests.cs`, new `PlayMode/PistolAnatomyPlayTests.cs`, extended `PlayMode/WeaponGripProbe.cs`. New `Tools/pistol-grip-verify.py`; documentation in this directory. Unity metadata accompanies new scripts/assets.

Existing unrelated dirty files and the preceding axe/test corrections were preserved, not re-authored in this pistol task. Existing PistolGrip controller/clip, Player prefab, damage/ammo/reload transaction code and vendor FBX were not replaced.
