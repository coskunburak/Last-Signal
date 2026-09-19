# S004 architecture and integration contract

P1 DESIGN ONLY. No production scripts, hitboxes, health, scene or package changes. Scope is one Shambler plus the minimum player-health integration, staged P2–P5.

## Existing production map

Actual code namespace is LastSignal. Assemblies: LastSignal.Runtime (Unity.InputSystem, Unity.ugui), LastSignal.Editor, LastSignal.EditModeTests and LastSignal.PlayModeTests. Keep these boundaries; do not introduce a second service framework to match the broader six-assembly proposal.

| Existing path/system | Verified responsibility / constraint |
|---|---|
| Assets/LastSignal/Runtime/Player/ | PlayerInputReader clones actions and owns input events; FirstPersonMotor uses CharacterController and bounded substeps; FirstPersonLook yaw/pitch/FOV; PlayerStance adjusts capsule/eye with clearance |
| Runtime/Interaction/ | First blocking ray within 2.2 m, commit revalidation; DoorInteractable owns motion and obstruction |
| Runtime/Combat/ | WeaponDefinition authoring, WeaponRuntimeState per weapon, WeaponController timers/transactions, PlayerCombatController event bridge/spawn, presenters |
| Runtime/Session/ | SessionFlow spawns/destroys player and resets doors; AcceptanceHud binds current player and menu/ammo UI |
| Assets/LastSignal/Prefabs/Player.prefab | Input/motor/look/stance/interaction/combat; camera and weapon parent; starting rifle; separate world-body presentation |
| Assets/LastSignal/Prefabs/Combat/Weapon_AssaultRifle.prefab | Project-owned VAL/MR POLY adapter, real source meshes/clips, magazine on mag bone; Muzzle/AimReference/CasingEjection; muzzle flash |
| Assets/Scenes/SampleScene.unity | Current actual gameplay scene, Session/HUD/Spawn/ground/two targets; no preplaced player |
| Editor/S001Project.cs, CombatAcceptanceProject.cs, RealAssetIntegration.cs | Existing authoring/build utilities reference old acceptance paths; do not regenerate on discovery |
| Assets/LastSignal/Tests/ | Existing EditMode and PlayMode tests; no root Tests directory |
| Packages/, ProjectSettings/ | AI Navigation installed; no current nav runtime integration; existing layer/build settings described below |
| Docs/, Builds/, Tools/ | Capitalized authoritative Docs; historical Mac build artifacts under Builds/S001 and S001-Baseline; Tools/s001.sh points to old acceptance route |

No inventory, persistence, production player-health or zombie simulation implementation was found. Health in DamageableTarget belongs to a test fixture, not the player.

## Existing rifle damage path

PlayerInputReader.Attack event → PlayerCombatController.OnFirePressed → ActiveWeapon.OnFirePressed → WeaponRuntimeState.CanFire/TryConsumeShot → WeaponFireResolver.Resolve → camera ray → short muzzle obstruction ray → full muzzle ray → hit collider.GetComponentInParent<IDamageable>() → TakeDamage(DamageInfo).

DamageInfo currently carries Amount, SourcePosition, HitPoint, HitNormal and GameObject Instigator. It lacks hit collider, damage type, region, attack/shot ID and persistent identity. ShotResult carries Collider, but it is emitted after damage. Do not compute a region later in ShotFired and apply damage a second time.

**Can a zombie receive damage without a weapon rewrite? YES.** Add a Component implementing the existing interface. For regions put one IDamageable proxy on each hitbox collider; its local region is known before forwarding to health. GetComponentInParent finds this proxy first. Authoring validation prohibits several ambiguous proxies on one collider chain. The health owner itself need not expose an additional root IDamageable that would conceal an incorrectly wired hitbox.

The resolver uses only the nearest full-muzzle hit and one damage call per Resolve. Its static hitRoots HashSet is cleared per damage resolution: it is not a durable shot-ID ledger, is mutable despite the “No state” comment, and is not safe to treat as reentrant/multishot deduplication. Preserve current single-hit behavior. Do not fan out hits with RaycastAll in the adapter. P4 tests use overlapping multiple colliders and assert one health decrement. An additive damage-envelope ID/type extension becomes necessary only if future penetration/pellets/retried commands require it; no speculative rewrite in S004.

Known receiver risk: the short .5 m obstruction ray rejects all collider hits, including a damageable extremely near the muzzle. P4 must test this near-contact boundary; if it blocks required combat, isolate a minimal separately justified resolver correction with regressions. No silent weapon change in P1.

## Proposed responsibilities and files

All paths in this table are **PROPOSED — do not create in P1**.

| Responsibility | Proposed placement / type | Ownership and dependencies |
|---|---|---|
| ZombieRuntimeState | Runtime/AI/, plain C# | State/time/target memory/action generation; explicit delta time, no Animator/scene searches |
| ZombieDefinition | Runtime/AI/, ScriptableObject | Read-only validated tuning snapshot, consistent with WeaponDefinition |
| ZombieController | Runtime/AI/, MonoBehaviour adapter | Owns one state instance; orchestrates perception/nav/combat in explicit order; session supplied |
| ZombiePerception | Runtime/AI/, plain helper using bounded Unity query adapter | Distance/cone/LOS to injected current player, owns no health |
| ZombieNavigation | Runtime/AI/, NavMeshAgent adapter | Sole transform/path movement owner; bounded repath/stuck behavior |
| ZombieCombat | Runtime/AI/, plain transaction helper | Eligibility/windup/commit/recovery and generation latch; player-health port |
| HealthState + PlayerHealth | Runtime/Combat/ + Runtime/Player/ | Plain bounded arithmetic shared as useful; PlayerHealth owns its player's state and death integration |
| ZombieHealth + ZombieHitbox | Runtime/AI/ | One zombie health owner, bone-bound region proxies implementing IDamageable |
| ZombieAnimationPresenter | Runtime/AI/ | Animator clips/parameters from committed state; no independent damage or navigation |
| Audio/VFX presentation | Existing presentation conventions, Runtime/AI/ when needed in P5 | Initially one small presentation adapter may handle bounded feedback; split only when behavior warrants it |
| ZombieEncounter | Runtime/Session/ | Explicit SessionFlow reference, authored spawn + prefab, session generation, one actor lifetime |
| Authoring checks | Editor/ | Required references, collider/proxy ownership, clip set, valid tuning, nav data and build paths |

Runtime assembly will need a reference to the installed AI Navigation assembly when using NavMeshSurface APIs; verify its actual asmdef during P2. UnityEngine.AI built-in Agent itself is not a second package. No new AI framework, networking SDK, global event bus, scene singleton, or one component per tuning field.

## Player health contract — DOES_NOT_EXIST

Future PlayerHealth owns one HealthState per spawned player; default 100 is a documented hypothesis. Enemy attack submits amount/source/action generation to that component through an explicit receiver port (the public IDamageable adapter can remain compatible). A bounded per-attacker generation guard rejects duplicates when provenance is supplied; attack transaction itself guarantees one attempt.

P3 must include health reduction **and a minimal terminal zero-health state** so the zombie never attacks an “alive” zero-HP player. On zero once: alive=false, clear weapon-held fire, ForceHolster/disable player combat actions, disable gameplay input/movement through explicit lifecycle integration, stop targeting, show a simple death/restart/menu route. A new session resets player health through construction. No automatic in-place resurrection, wound system, death bag, inventory loss, shelter respawn or persistence is implied.

A plain SetGameplay(false) is insufficient as the only death lock: SessionFlow.Resume currently reenables input whenever Player exists. P3 must make Resume respect terminal player state, or route death to an explicit session end/restart state. Preserve normal pause behavior and test Pause→death→Resume. UI buttons request session actions; UI never edits health. Implement this lifecycle seam only in its authorized gate.

## Session lifecycle and ownership

Current SessionFlow.Start auto-calls BeginSession; BeginSession is idempotent while a player exists, resets doors, instantiates the prefab, subscribes Pause/FocusLost and resumes. ReturnToMenu unsubscribes, deactivates/destroys player, clears references and sets timeScale=0; OnDestroy calls teardown then restores timeScale=1. No session events or zombie hooks exist today.

P2 introduces one explicit encounter participant reference/hook at the composition boundary: after a valid player spawn, increment session generation and Initialize/Begin the encounter with that player. Before deactivating the player on ReturnToMenu, End the encounter: invalidate generation, cancel attacks/paths, clear target, destroy live zombie/corpse and unsubscribe. Call is idempotent. On scene unload/encounter OnDisable repeat safe cancellation. Async results require matching generation. Do not rely solely on timeScale=0 or delayed Unity Destroy to prevent stale decisions.

Pause stops state timers, queries, movement and scaled Animator playback; resume validates target/path before continuing. The normal scene and deterministic arena use this same production encounter path. Do not instantiate zombies from hidden editor callbacks, DamageableTarget or a global singleton. Unexpected player destruction causes immediate target invalidation and safe encounter shutdown/Idle, not FindObjectOfType every Update. Restart creates fresh health/state/action IDs; no old coroutine or animation marker can affect it.

## Prefab and scene ownership

Third-party source remains under existing paths. Future approved adapter belongs under Assets/LastSignal/Prefabs/ (a documented Enemies subfolder may be added in P2) with project-owned materials and animation controller under existing Materials/Animations conventions. One production prefab is used in the normal scene and acceptance arena; optional fixture variants cannot contain alternate combat logic.

Future hierarchy: root with controller/agent/movement capsule/health → Visual with rig/Animator → bone children with local hitbox colliders + ZombieHitbox. Each proxy explicitly references root health. Root movement capsule lives on EnemyBody; bone proxies on EnemyHitbox. No independent limb health. Vendor prefab is nested or referenced without destructive edits.

Current scene ground is 40×40 m, y=-.25 with .5 m thickness; two targets at (0,1,10) and (5,1,20), second at the floor edge. No opaque-wall, door, corridor, obstacle or navigation fixture exists. SampleScene is the production-path integration seed, not an already adequate navigation arena. Propose Assets/LastSignal/Scenes/Combat/ZombieAcceptance.unity for deterministic wall/corner/corridor/unreachable tests, plus the shared encounter in SampleScene. This proposed path does not exist yet. Restore/reconcile missing old scenes before changing their tests; never alias a combat floor to a parkour test and claim equivalent coverage.

## Layers / tags / physics

Current tags list has no custom entries; standard built-in tags remain. Named layers: 0 Default, 1 TransparentFX, 2 Ignore Raycast, 4 Water, 5 UI. All collision pairs are enabled in the Editor query. Current player and FPS layers/serialized masks need preserving during normalization; rifle hitMask and interaction visibilityMask are **51** (0,1,4,5), not the broader code defaults. PlayerStance.clearanceMask=-1.

Editor inspection confirms player root/View on layer 2, and WorldBody plus world rifle/renderers on unnamed layer 30. Current WorldBody meshes are space_crew_man and its helmet. Reserve the existing layer-30 presentation meaning during any named-layer migration; do not reuse an unnamed index merely because TagManager has no label.

Propose named PlayerBody, EnemyBody and EnemyHitbox layers in P2/P4; choose unused indices after a fresh audit, never hardcode numbers in logic. Existing Default is the arena World; no need to relabel every asset. No Damageable tag framework or redundant Interaction layer is required for this slice.

| Query/collision | Future policy |
|---|---|
| PlayerBody ↔ EnemyBody | Physical collision enabled, both collide with World; no capsule interpenetration |
| EnemyHitbox ↔ bodies/world/other hitboxes | Physical collision disabled; proxies are **non-trigger** so current weapon QueryTriggerInteraction.Ignore can hit them |
| Weapon ray | Include World + EnemyHitbox, exclude self/viewmodel + EnemyBody; update actual serialized hitMask, not just a C# default |
| Interaction ray | World + EnemyBody blocks reaching a door through an enemy; exclude moving bone hitboxes to avoid prompt jitter; no zombie interaction action |
| Crouch clearance | World + EnemyBody, exclude EnemyHitbox; avoiding tiny hand colliders preventing stand |
| Perception/contact LOS | Opaque world/closed door blockers; exclude self/visual hitboxes; target validation separate |
| Nav build | Ground/world authoring only; exclude bodies, hitboxes, FPS mesh, targets not intended as world blockers |
| Dead corpse | Movement/hitbox colliders disabled, agent removed; no nav carving or bullet interception |

Ray queries are filtered explicitly; disabling physical collisions is not a reason to assume weapon ray exclusion. Collider-backed region routing requires a non-trigger proxy and correct serialized masks. Validate no root capsule masks the head proxy.

## Performance and future systems

No broad overlaps every frame, per-frame scene scans, LINQ/captures in hot loops, repeated GetComponent for stable references, per-frame path requests or redundant Animator parameter writes. Cached references and bounded per-agent cadence suffice for one zombie; stagger phases for 10/25 before adopting a general scheduler. Visual/audio concurrency is bounded; corpse cleanup releases ownership. Do not create pools before profiling demonstrates need.

P5 measures 1/10/25 of the same approved prefab, same fixed seed/route/quality/camera/target hardware, warmup separately from five measurement runs. Collect CPU decisions/perception/nav/animation, physics, rendering/GPU, GC/frame, active voices/particles, path requests/s, memory plateau and frame p50/p95/p99/spikes. Compare zero-agent control and preserve collision/threat rules across counts. LS-DOC-20 initial hypotheses: AI mean ≤2 ms, frame p95≤16.67 ms/p99≤25 ms at 1080p, warmed critical ticks 0 B; all **TUNING ACCEPTANCE** until hardware/profile agreed. Editor FPS is not Windows certification.

No save schema changes now. Future persistence must assign stable zombie identity and a death tombstone and restore non-damaging recovery; session runtime generations are not persistent IDs. No inventory/loot integration or network authority host is implemented or silently assumed.
