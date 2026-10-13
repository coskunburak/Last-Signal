# Changed files and ownership

These lists compare this task with the captured local baseline, not Git HEAD. Earlier uncommitted combat/character work remains authoritative.

## Existing modified or untracked files extended by this task

- `Assets/LastSignal/Data/VFX/Blood/ZombieBlood.asset`
- `Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieController.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieDismemberment.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieHealth.cs`
- `Assets/LastSignal/Scripts/Runtime/Combat/DamageReceipt.cs`
- `Assets/LastSignal/Scripts/Runtime/Combat/WeaponAudioPresenter.cs`
- `Assets/LastSignal/Scripts/Runtime/Combat/WeaponFireResolver.cs`
- `Assets/LastSignal/Scripts/Runtime/VFX/ZombieBloodVfxPool.cs`
- `Assets/LastSignal/Scripts/Runtime/VFX/ZombieBloodVfxPresenter.cs`
- `Assets/LastSignal/Scripts/Runtime/VFX/ZombieBloodVfxProfile.cs`
- `Assets/LastSignal/Scripts/Runtime/VFX/ZombieLocalizedWounds.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/CombatFeedbackPlayTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/ZombieDamagePlayTests.cs`
- `Assets/LastSignal/Shaders/VFX/Blood/LS_LocalizedWound.shader`

## Previously clean files changed for integration

- `Assets/LastSignal/Materials/VFX/CombatFeedback/M_LocalizedWound.mat`
- `Assets/LastSignal/Scripts/Runtime/Combat/IDamageable.cs`
- `Assets/LastSignal/Scripts/Runtime/Combat/PlayerCombatController.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveCodec.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveGame.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveValidation.cs`
- `Assets/LastSignal/Scripts/Runtime/Session/ZombieEncounter.cs`
- `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellContent.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/PersistenceIntegrationTests.cs`

## New implementation / verification files

- `Assets/LastSignal/Data/VFX/Blood/ZombieWounds.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/ArmL.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/ArmR.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/FootL.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/FootR.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/ForeArmL.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/ForeArmR.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/HandL.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/HandR.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/Head.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/LegL.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/LegR.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/LowerLegL.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/LowerLegR.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/Pelvis.asset`
- `Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/Ribcage.asset`
- `Assets/LastSignal/Scripts/Editor/ZombieWoundAuthoring.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/ZombieWoundSnapshot.cs`
- `Assets/LastSignal/Scripts/Runtime/VFX/ZombieCorpseImpactReceiver.cs`
- `Assets/LastSignal/Scripts/Runtime/VFX/ZombieWoundProfile.cs`
- `Assets/LastSignal/Scripts/Runtime/VFX/ZombieWoundSurfaceResolver.cs`
- `Assets/LastSignal/Scripts/Tests/EditMode/ZombieWoundTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/ZombieWoundPlayTests.cs`
- `Tools/zombie-wounds-verify.py`

New Unity assets/scripts have companion `.meta` files; existing GUIDs were preserved. New surface folder metadata is included. The seven Markdown handoff documents, baseline/status/hash records and static inspection manifest live in this directory.

## Scope of changes

Runtime: extend receipts/dedup, skinned contact/wounds, blood/corpse pooling, restoration notification and encounter terminal snapshots. Assets: 15 readable mesh derivatives, existing production prefab/blood profile/wound material/shader and new wound tuning. Tests: two new wound fixtures plus a real persistence scenario; existing population benchmark adjusted only for finite corpse retention, preserving its allocation assertions. Tool: one explicit verification gate per invocation.

`TASK_CHANGES.patch` records text edits through the initial handoff against pre-task copies for existing files; focused gate corrections are described in `IMPLEMENTATION_REPORT.md`. `FINAL_HASHES.json` covers current task source/assets. Large newly generated mesh YAML is tracked by hashes and the asset files themselves. No unrelated input/UI/audio/vehicle/character files were reverted. No commit, push or repository cleanup was performed.
