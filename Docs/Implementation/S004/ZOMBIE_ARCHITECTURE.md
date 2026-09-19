# Implemented zombie architecture — P2 recovery

All runtime files are under Assets/LastSignal/Scripts/Runtime. No monolithic replacement of the interrupted implementation was performed.

| Source | Responsibility |
|---|---|
| AI/ZombieController.cs | Authoritative orchestration, clock, Idle/Chasing/Searching transitions, target validity, pause and shutdown |
| AI/ZombieRuntimeState.cs | Plain state/confidence/memory; legal edges; destination refresh policy |
| AI/ZombieDefinition.cs | Serialized validated read-only tuning accessors; shared asset, per-actor state elsewhere |
| AI/ZombiePerception.cs | Cached target/capsule/head; bounded FOV/distance/opaque-world observations only |
| AI/ZombieNavigation.cs | NavMeshAgent position, gameplay yaw, guarded API use, bounded paths/retries/stuck policy |
| AI/ZombieSearch.cs | Remembered-data-only fixed local search pattern; no live player reference |
| AI/ZombieAnimationPresenter.cs | Actual velocity to B0B Idle/Locomotion; root motion disabled; no gameplay authority |
| Session/ZombieEncounter.cs | Explicit per-session actor creation/binding/pause/end; generation counter |
| Session/SessionFlow.cs | Player/session owner invokes encounter after spawn and before teardown |

Assets/LastSignal/Enemies/Zombie/Prefabs/LS_Zombie_Runtime.prefab wraps the unchanged B0B LS_Zombie_Shambler presentation prefab. It adds the agent, controller, perception, navigation, presenter and movement capsule. Both ZombieAcceptance and SampleScene use this same runtime prefab through an explicitly serialized encounter. No scene singleton or per-frame actor discovery exists.

Movement body currently uses built-in Ignore Raycast (2), physically collides with the player/world, and is excluded by existing rifle mask 51 and LOS mask 1. Existing stance mask includes it. Existing interaction mask also excludes it; enemy-blocked interaction remains a future mask integration concern. No hitboxes or IDamageable adapter were added. Visual hierarchy, vendor source and B0B animation assets are preserved.

Editor/ZombieRuntimeAuthoring composes the wrapper and scene encounter without regenerating existing B0A/B0B assets. ZombieAcceptanceAuthoring owns the retained arena bake. ZombieValidationRunner routes explicit QA commands to Unity TestRunnerApi; it contains no scene-wide destruction. Test teardown removes explicitly owned actors/cameras and invokes the loaded session's teardown, preserving test infrastructure.

Future P3/P4 contracts and discovery details are preserved in Evidence/20260919-P2-Recovery/prior-documents/ZOMBIE_ARCHITECTURE.md. They are proposals, not implemented health/attack systems. Shared tuning is not copied to an immutable session snapshot; live authoring during play can change current AI tuning.


## Implemented P3 extension

- `Player/PlayerHealth.cs`: component on production Player; implements existing IDamageable and receives unchanged DamageInfo (amount, source/hit position, normal, instigator). Finite-positive damage, clamp, one death event, health-change event, transaction counter and session reset. No AI/UI/input dependencies.
- `AI/ZombieController.cs`: retained state machine with AttackWindup/AttackCommit/Recovering, simulation clock, per-actor ulong sequence, consumed contact latch, locked forward, lifecycle and result diagnostics. Cached health/capsule references at Bind. No asynchronous delayed damage.
- `AI/ZombieMeleeValidator.cs`: allocation-free target/range/arc/world validation. Returns a compact result enum and contact point/distance/angle. Profiler marker surrounds validation.
- `AI/ZombieDefinition.cs`: real clip reference, measured normalized commit/contact/recovery, playback multiplier and melee geometry/damage. Validated on activation; missing animation/origin/tuning fails safely.
- `AI/ZombieAnimationPresenter.cs`: Attack begins once per sequence. During attack it advances the Animator explicitly by the controller delta at .8 speed, leaving automatic speed zero between calls. Culling is temporarily AlwaysAnimate; no Animator normalized-time read grants damage. End restores culling and blends Idle. Existing locomotion presentation remains.
- `Session/SessionFlow.cs`: subscribes to player death, locks existing input, cancels held weapon actions and terminally unequips via the existing combat controller. Resume respects IsAlive. Unsubscribes before destruction.
- `Session/AcceptanceHud.cs`: health beside ammo; existing panel displays death and menu access. Combat controller has no UI dependencies.

Runtime prefab adds only a project-owned MeleeOrigin child at (0,1.05,0). Vendor skeleton and B0B presentation prefab remain unchanged. No ZombieHealth or enemy IDamageable/hitbox adapter is introduced.

Combat test entrypoint: `ZombieMeleeTests`, `PlayerHealthTests`, `ZombieMeleeLogicTests`; evidence and report under the P3 run. Existing P2 tests preserve perception/search/navigation coverage; only explicit obsolete no-health/no-attack assertions change.
