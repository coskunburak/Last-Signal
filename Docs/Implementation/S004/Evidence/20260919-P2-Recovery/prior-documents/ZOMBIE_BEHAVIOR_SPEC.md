# S004 first Shambler behavior contract

P1 DESIGN ONLY. No runtime implementation. Normative for the proposed P2–P5 work; asset-dependent timings remain open. Sources: LS-DOC-03/09/10/11/19/23, inspected SessionFlow and weapon state conventions. All times are simulation seconds, frozen in pause/menu. One local player, one production zombie, no hearing/director/roaming/multiple archetypes in this slice.

## State machine and priorities

One gameplay state owner; Animator never decides transitions. Priority on each simulation step: session invalidation → lethal damage/Dead → allowed hit reaction → state action. Session shutdown destroys/disables the actor outside its behavior enum. A paused state is a session gate, not a new AI state. Alert is a presentation cue on acquisition, not a redundant state. **Searching** is justified by documented last-known-position behavior; no Investigating without a hearing stimulus system.

| State | Entry / target requirement | Exit and legal destinations | Movement / attack permission | Presentation and interruption |
|---|---|---|---|---|
| Idle | Initialized valid spawn, or search expires; no target required | Confirmed visible live player → Chasing; accepted reaction → HitReact; lethal → Dead | Stopped; bounded sight checks; never attack | Idle loop; alert cue on acquisition |
| Chasing | Visible live current-session player, or just reacquired | Eligibility → AttackWindup; lost sight → Searching; accepted hit → HitReact; lethal → Dead; invalid target → Idle | Agent toward last observed position; yaw from movement; attack eligibility only with current LOS | Locomotion reads actual speed; immediately stop decisions on session loss |
| Searching | LOS lost; retain lastKnownPosition and finite memory, not current hidden position | Visible reacquisition → Chasing; grace/search budget expires or target destroyed → Idle; reaction → HitReact; lethal → Dead | Move only to frozen lastKnownPosition; on arrival stop and bounded scan; no attack | Walk then idle/turn presentation; no wall tracking |
| AttackWindup | Target live/visible/in range/facing; cooldown expired; new attack generation | Contact time reached → AttackCommit; target invalid or leaves eligibility → Recovering; accepted reaction → HitReact; lethal → Dead | Agent stopped; limited gameplay yaw toward validated target; no damage | Windup telegraph; no root lunge. Cancelled attack cannot later commit |
| AttackCommit | Enter once when simulation crosses contact time; same target/generation | Exactly one validation attempt, hit or miss → Recovering; lethal → Dead | No navigation or target homing; at most one health mutation | Contact cue only; nonlethal reaction deferred/suppressed during atomic commit |
| Recovering | Attack committed or cancelled | Recovery and cooldown complete: visible target → Chasing, valid last-known memory → Searching, otherwise Idle; lethal → Dead | Stopped; no new attack or contact | Recovery pose; no nonlethal interruption that would erase recovery |
| HitReact | Accepted nonlethal damage while eligible and outside reaction cooldown | Timer expires: revalidate → Chasing / Searching / Idle; lethal → Dead | Stop navigation; cancel uncommitted attack generation; no damage | One reaction; repeated hits still reduce health but do not restart timer |
| Dead | Health <= 0, exactly once | No outgoing behavior transition | Stop/reset/disable agent safely; cancel attack; clear target; no perception | Non-looping death → corpse hold; all further damage ignored |

Any edge not listed is illegal. In particular Dead→anything, Idle/Searching→AttackCommit, AttackCommit→AttackCommit, Recovering→AttackWindup before cooldown, and Animator callback→arbitrary gameplay state are forbidden. Reacquisition goes through Chasing/eligibility, even when already near the player. If lifecycle invalidation and contact occur in one frame, invalidation wins. A lethal hit always wins over a reaction request in that simulation step.

P2 implements only Idle, Chasing, Searching and lifecycle suspension/teardown. Attack/HitReact/Dead transitions become enabled in P3/P4; P2 never delivers damage.

## Perception

The encounter owner supplies the current local player reference, aim/visibility anchor and session generation. Do not find by tag/name each frame. Until P3 health exists, the explicitly injected active player is targetable; P3 adds alive validity.

Acquisition requires target active, session playing, distance within SightDistance, dot-product within the configured full cone SightAngle/2, and unobstructed line to the authored target anchor. Cheap distance/angle checks precede raycasts. Self, FPS viewmodel and enemy hitboxes are excluded from the obstruction query. Opaque world and closed physical door block; lack of a material name does not make geometry transparent. No proximity exception that detects through a wall.

On LOS loss immediately freeze lastKnownPosition and enter Searching. LostTargetGrace is the total memory/search budget from last valid observation; it does **not** permit attacks or destination updates to the hidden player's current transform. Brief occlusion may lead to reacquisition before budget expiry. At the last-known point stop; no speculative room-search framework. At expiry clear target/memory and Idle. Destroyed/disabled player, generation mismatch, scene unload or menu clears references immediately, without waiting for grace. Pause freezes memory age and perception; resume revalidates before any action.

## Navigation

Use existing AI Navigation only. Future arena has an authored surface with walkable ground and opaque obstacle fixtures, using the configured agent type verified against the approved model. No world streaming navigation in S004.

- Validate spawn on the intended surface and physical clearance before activating the agent. Off-NavMesh spawn remains inactive with one actionable diagnostic; no recurring exception.
- Request destinations at minimum RepathInterval when last-observed target moved at least RepathDistance, or on a significant state/path failure. Coalesce requests; never one SetDestination per Update.
- While pathPending, do not consume an old success status as the new path result. Stop on invalid agent/surface state.
- Complete path: advance normally; stop at authored center-distance StoppingDistance. Recheck true target range/LOS/facing before attack; remainingDistance alone is not attack permission.
- Partial path: may advance only along valid reachable segments toward the returned endpoint; cannot attack across a missing segment. At endpoint stop, retry within bounded policy, then Searching/Idle.
- Invalid path/failed request: stop; one retry after interval, then abandon chase into finite search. No retry storm.
- Stuck: compare displacement/progress over StuckTimeout while movement requested; one bounded repath, then stop/search. No teleport, pushing through geometry or hidden re-position.
- Agent disabled/dead/not on NavMesh: guard ResetPath/isStopped access, cancel destination requests and future callbacks.
- Face desired velocity in chase, with a bounded yaw rate. At rest do not oscillate aim between velocity and target. Range hysteresis and stopping distance must prevent overlapping the player capsule.
- Door opening/closing must agree with physical collision and nav obstacle policy. Dynamic doors do not exist in the current SampleScene; P2 arena can use fixed closed blockers and open passages. Dynamic doors require a separately tested adapter before being claimed.

## Enemy-to-player attack transaction (P3)

Eligibility: active matching session, attacker alive, target live, current LOS, range/facing within limits, not reacting/recovering, cooldown elapsed. Allocate a monotonically increasing per-instance attack generation and latch the target/session generation. Windup starts no damage. Advance a simulation clock, independent of animation speed and visibility.

At contact, perform a **single attempt** with this generation. Before any mutation revalidate attacker, session, target existence/alive, range, facing and opaque-world LOS; even a target still in range behind a newly closed door must not be hit. Mark attempt consumed before publishing events. On success send one typed request to the player's health owner. On failure record miss and enter recovery. A missed window never retries later as the player re-enters range.

Use root center-to-center planar range plus an authored vertical tolerance and chest LOS; units and anchors fixed in tuning. This is a reach validation, not a collision-callback damage system. A single swing hitting several player colliders still has one target/transaction.

Cooldown is minimum start-to-start interval; earliest next start is max(previousStart+AttackCooldown, recoveryEnd). On windup cancellation still apply recovery/cooldown from the attempted start. Death or shutdown invalidates generation immediately. Nonlethal eligible HitReact cancels windup, preserving earliestNextAttack. Pause freezes windup/recovery, agent and Animator together; resume must revalidate. Scene unload/target destruction cancels permanently.

Contact event policy: authoritative simulation crosses the configured contact time, including at low FPS, and commits once. Animation events may signal SFX/contact presentation with a guard (current actor/session/action generation, expected state/time window). They cannot independently apply damage. Missing/duplicate/late events leave the simulation result unchanged; simulation timing is the fallback. No ungated Animator-state callback may hit the player.

## Receiving rifle damage, reaction and death (P4)

Each hitbox implements the existing IDamageable adapter and forwards region-scaled damage to one zombie health state. Apply finite nonnegative damage; reject NaN/infinity/negative requests rather than converting them to healing. Clamp current health to [0,max]. Emit immutable applied amount, source, hit point/normal and resulting health after committing. Healing is a future separate operation.

On any accepted nonlethal rifle hit, reaction may play only in Idle/Chasing/Searching/AttackWindup and outside HitReactionCooldown. One light reaction is enough for S004; heavy/light classification, dismemberment and limb health are deferred. Repeated automatic fire always deals valid damage, but does not extend/restart a reaction or reset attack cooldown. Existing reaction must finish; cooldown begins on accepted reaction. Death supersedes all queued hit effects.

Lethal sequence: set health zero and latch death once → invalidate attack generation → stop/reset agent if valid and disable it → clear target/memory → stop all AI ticking → disable movement collider and damage hitboxes → play death once → hold final pose → cleanup. Rendering may finish after gameplay becomes Dead; animation completion is not death truth.

Corpse policy for S004: remain visible 30 simulation seconds (**INITIAL_TUNING_VALUE**), then remove through encounter owner. Disabled colliders mean no movement/nav blockage and no bullet interception/damage. No ragdoll required. Corpse bullets pass through under this explicit policy. Death sound/VFX must not restart. Menu/unload removes corpse immediately. No loot, death container, resurrection, respawn timer or persistence in this slice; future save work needs stable ID+tombstone separately.

## Authorable tuning

A future ZombieDefinition ScriptableObject follows WeaponDefinition: serialized data, read-only accessors, per-instance state elsewhere. Copy/validate a session snapshot; live tuning changes do not silently alter an attack already underway.

| Key | Initial value / source | Validation or unresolved acceptance |
|---|---|---|
| MaxHealth | 90, LS-DOC-23 hypothesis | >0, finite; **INITIAL_TUNING_VALUE** |
| PlayerMaxHealth | 100, LS-DOC-09 hypothesis | P3 owner, finite >0 |
| HeadMultiplier / TorsoMultiplier / LimbMultiplier | 3 / 1 / 0.6 | Head 3 source LS-DOC-23; torso GDD 1; limbs deliberately consolidate GDD arms .55/legs .65. All **INITIAL_TUNING_VALUE** |
| SightDistance / SightAngle | 15 m / 120 degrees | **INITIAL_TUNING_VALUE**; distance > attack range, angle (0,360] |
| PerceptionInterval / LostTargetGrace | 0.1 s / 2 s | **INITIAL_TUNING_VALUE**; 10 Hz lies in LS-DOC-11 near-AI target |
| WalkSpeed / ChaseSpeed | 1.0 / 1.8 m/s | **INITIAL_TUNING_VALUE**; below current 3.2 m/s player walk, validate stride first |
| StoppingDistance / AttackRange | 1.2 / 1.6 m center distance | **INITIAL_TUNING_VALUE**; sum of body radii + clearance < stop < attack |
| AttackExitRange / VerticalTolerance / FacingHalfAngle | 1.8 m / 0.8 m / 45 degrees | **INITIAL_TUNING_VALUE**; exit hysteresis does not widen contact range |
| AttackDamage | 15 | **INITIAL_TUNING_VALUE**, not documented final lethality |
| AttackWindup / ContactTime / AttackRecovery / AttackCooldown | **NOT_VERIFIED — asset gate** | Derive from accepted clip/contact; finite positive, coherent windup/strike/recovery; no guessed production times |
| TurnSpeed | 180 degrees/s | **INITIAL_TUNING_VALUE**; no snapping |
| RepathInterval / RepathDistance | 0.25 s / 0.5 m | **INITIAL_TUNING_VALUE**; bounded query count |
| StuckTimeout | 1.5 s | **INITIAL_TUNING_VALUE**; test actual geometry |
| HitReactionCooldown / reaction duration | 1 s / **NOT_VERIFIED** | Cooldown provisional; duration from real clip; repeated hits cannot reset it |
| CorpseLifetime | 30 s | **INITIAL_TUNING_VALUE**, sim time |

These are starting hypotheses for a controlled arena, not final design balance. At the current rifle's 30 damage, 90 HP yields 3 torso hits or one 3x headshot, before future armor. Record tuning acceptance on real assets; a useful arithmetic example is not a playtest.

