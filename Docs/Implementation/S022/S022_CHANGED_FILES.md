# S022 changed files — 2026-10-13

Relative to the dirty local baseline, not Git HEAD. No commits, pushes, vendor changes, deletions, old-fixture edits or production-scene edits were made by S022. Existing GUIDs are retained; new Unity assets/scripts have new `.meta` files. Folder metadata added for `Data/Enemies/Zombie/Resources/S022`.

## Modified
- `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellContent.cs`
- `Assets/LastSignal/Scripts/Runtime/Audio/ProductionAudio.cs`
- `Assets/LastSignal/Scripts/Runtime/Audio/ZombieAudioPresenter.cs`
- `Assets/LastSignal/Scripts/Runtime/Audio/AudioCatalog.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieController.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieAnimationPresenter.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieDefinition.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieNavigation.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieMeleeValidator.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieHealth.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/WorldPopulationManager.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveGame.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveValidation.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveCodec.cs`
- `Assets/LastSignal/Scripts/Runtime/Session/ZombieEncounter.cs`
- `Assets/LastSignal/Scripts/Editor/S015AudioAuthoring.cs`
- `Assets/LastSignal/Audio/Config/S015AudioCatalog.asset`

## Added
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieDefinition.Archetypes.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieArchetypeCycle.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieArchetypeCatalog.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieMeleeSpacing.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieEncounterRisk.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/ZombieEncounterBudget.cs`
- `Assets/LastSignal/Scripts/Runtime/AI/S022AcceptanceRoute.cs`
- `Assets/LastSignal/Data/Enemies/Zombie/Lurker.asset`
- `Assets/LastSignal/Data/Enemies/Zombie/Runner.asset`
- `Assets/LastSignal/Data/Enemies/Zombie/S022EncounterRisk.asset`
- `Assets/LastSignal/Data/Enemies/Zombie/Resources/S022/ZombieCatalog.asset`
- `Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Lurker.prefab`
- `Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Runner.prefab`
- `Assets/LastSignal/Scenes/Validation/S022ArchetypeAcceptance.unity`
- `Assets/LastSignal/Scripts/Tests/EditMode/S022ArchetypeTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/S022ArchetypePlayTests.cs`
- `Assets/LastSignal/Scripts/Editor/S013/S022Build.cs`
- `Tools/s022-verify.py`

The eight S022 Markdown records and implementation-baseline evidence are also new. Companion `.meta` files accompany every new Unity source/content asset above.

## Verification-driven regression repair
- `Assets/LastSignal/Scripts/Tests/PlayMode/ZombieMeleeTests.cs`: the 10-actor assertion now checks total accepted hits under target-owned contact spacing.
- `Assets/LastSignal/Scripts/Tests/PlayMode/PistolAnatomyPlayTests.cs`: finger penetration checks use the actual closed mesh cross-section rather than an assumed solid outer span.
- `Assets/LastSignal/Art/TriplePistols/DXT/Fingers_FPS.asset`
- `Assets/LastSignal/Art/TriplePistols/Glock17/Fingers_FPS.asset`
- `Assets/LastSignal/Art/TriplePistols/Custom9mm/Fingers_FPS.asset`

The three project-owned pistol finger assets above received bounded middle/ring/little pose adjustments after focused PlayMode evidence exposed actual frame penetration. Their GUIDs, meshes, grip sockets, weapon behavior and ammo state were retained. Unity validation of this repair is pending.

The 09:48 focused PlayMode rerun found DXT finger 6 still crossing the frame during reload. A second bounded calibration increased clearance across all three assets while preserving finger segment lengths and distal contact. `PistolAnatomyPlayTests.cs` now emits precise model-space coordinates and segment endpoints if the assertion fails again. This second repair has not been verified in Unity.

`Tools/s022-verify.py` now classifies the installed Unity Inference package's exact `SENTIS_ANALYTICS_ENABLED` Standalone define toggle as an expected, reported source change, matching the established S021 runner. All other source mutations still fail. The user's 09:42 Gate 3 evidence remains a historical runner FAIL even though all 773 NUnit cases passed; its `ProjectSettings.asset` change was not rolled back.

## Preservation
The initial status contained 16,208 entries (extensive vendor moves/deletions plus combat, animation, persistence, scenes and prior sprint work). 409 relevant existing source/data/prefab files were snapshotted before edits; the initial implementation comparison found 392 byte-identical and 17 intentional scoped edits, with none deleted. Later verification-driven repairs above are additional and those initial counts are not a current diff. Baseline: `Docs/Implementation/S022/Evidence/ImplementationBaseline-20261013-004816`. The temporary full source copies also remain at `/tmp/lastsignal-s022-baseline`. This comparison is source inspection, not a test result or a claim that the baseline compiled.

Shambler's existing definition/prefab GUIDs and 100 HP are preserved. New Lurker/Runner prefabs copy the current runtime prefab's full hierarchy and rig, with archetype-specific profile and health; they do not inherit future prefab changes automatically. New profile assets reference the current attack, hit, death and vehicle clips; no Animator asset or vendor rig was replaced. Five audio cue IDs were appended without renumbering older cues. Save fields are optional extensions, without schema bump. Details: S022_ARCHETYPE_DESIGN.md.
