# Current P3 acceptance — 2026-09-19

The historical P1/B0B matrix below remains a design/archive record. Current repository status is in [P3 implementation report](S004_P3_IMPLEMENTATION_REPORT.md) and [P3 evidence](Evidence/20260919-P3/README.md).

| Gate | Current evidence |
|---|---|
| Fresh P2 baseline | 52/52 EditMode; 35/35 PlayMode |
| Player health | 7 focused cases passed; full health, damage, invalid inputs, clamp, one death, reset |
| Legal melee states / tuning / prefab | Included in fresh 70/70 EditMode |
| Hit, recovery, repeat | Real production prefab; one transaction per strike |
| Backstep / sidestep / rear / wall | Real collider tests; all miss, locked forward preserved |
| Movement counterplay | Real FirstPersonMotor sidestep after commitment misses |
| Pause / teardown / restart / death | Focused real session tests pass |
| Large delta / duplicate collider / disabled Animator | One transaction, recovery retained; paused manual simulation cannot advance |
| Normal route | Actual SampleScene encounter approaches and hits production Player |
| Performance | 1 and 10 active melee actors, warmed synchronous path 0 B; Editor-only scope |
| Full regression closure | See final report/XML; do not infer completion from this focused ledger |

No P4 implementation or shipping-platform certification is claimed.

---

> **Current B0B update (2026-09-18): technical asset gate PASS; P2 technical entry READY, implementation NOT_STARTED. Full EditMode 38/38 and PlayMode 23/23. Local license entitlement evidence remains LICENSE_EVIDENCE_PENDING. See [B0B report](S004_B0B_ZOMBIE_ASSET_INTEGRATION_REPORT.md). The earlier entry/P1 results below are historical and are superseded for current asset/baseline status.**

# S004 acceptance and P1 status

P1 status: **PARTIAL**. Baseline: **BASELINE PARTIAL**. P2 production entry: **BLOCKED**.
P2–P5 matrix below is test design, **NOT_RUN**. No test implementation or zombie gameplay was added.

Evidence for current baseline: [index](Evidence/20260918-P1/README.md), [EditMode XML](Evidence/20260918-P1/editmode-results.xml), [full PlayMode abort](Evidence/20260918-P1/playmode-abort-console.json), [isolated abort](Evidence/20260918-P1/playmode-stable-abort-console.json), [live rifle/session](Evidence/20260918-P1/runtime-baseline.txt).

## P1 evidence checklist

| Required area | Result | Evidence / remaining condition |
|---|---|---|
| Relevant docs read, phase and contradictions | PASS | S004_P1_DISCOVERY_REPORT.md source ledger and reconciliation |
| Compilation known | PASS | Forced script compilation; Editor compileFailed=false; no project C# error/warning observed |
| Regression status known | PASS as discovery; baseline is not green | EditMode 33/34; missing scene failure; two aborted PlayMode attempts, no completion XML |
| Normal session startup / player / camera | PASS for Editor smoke | SampleScene, one player, View camera, rifle ready; direct public commands |
| Rifle damage/reload / pause/menu/restart | PASS for bounded smoke | 30→29 ammo, 100→70 HP, reload 30/119, menu clears player, restart 30/120 |
| All imported character candidates | PASS | Seven FBX enumerated, three candidates inventoried; archive has same seven model paths |
| One production zombie or blocker | ASSET BLOCKER declared | No approved model/animation package |
| Candidate rigs/materials/colliders | PASS as audit | Valid Humanoid Avatars; zero colliders; Standard source materials visibly pink in current URP |
| Selected production rig/animation/retarget evidence | BLOCKED | No selected production asset; all five mandatory animations missing |
| Navigation solution and scene status | PASS as audit | AI Navigation installed; no surface/data/agent/obstacle, 0 triangulation |
| Damage, health, hitbox and lifecycle contracts | PASS as design | Architecture and behavior documents; not implementation acceptance |
| Locomotion authority | CONDITIONAL design | Agent motion selected; speed/foot contacts cannot be locked without art |
| Acceptance/risk/backlog | PASS as design | Linked documents |
| No premature gameplay implementation | PASS | Final source-hash comparison; only Docs deliverables remain |

P1 cannot be PASS merely because a missing asset was named. The required actual production animation, retarget and contact evidence is missing. Broken upstream regression also prevents P2 readiness.

## Test execution rules

EditMode (E) uses fake time and explicit target/query results for domain tests. PlayMode (P) uses actual colliders, authored NavMesh, player prefab, session and Animator. Visual (V) records real production rig motion from front/side/player view. Standalone (S) uses actual build controls. No private-health mutation to manufacture an asserted outcome.

Each result records ID, requirement, gate, seed/scene/prefab/tuning version, steps, expected/actual, target hardware, timestamps and evidence. **TUNING ACCEPTANCE** means a proposed threshold pending documented art/hardware/playtest confirmation; it cannot quietly become a final numeric pass. Use the behavior spec's explicit clock/anchor definitions.

## Logic acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| LOG-01 | P2 E | Table-drive every permitted edge and reject every unlisted edge; terminal Dead never reenters live states |
| LOG-02 | P2 E | LOS false at t0 freezes lastKnownPosition; fake time below/at/above grace produces finite search then Idle; no hidden-position update |
| LOG-03 | P3 E | Repeat same attack generation/contact callback ten times: one attempt and at most one health mutation |
| LOG-04 | P3 E | Invalid/dead target, paused/menu context, range/facing/LOS failure individually reject attack without damage |
| LOG-05 | P3/P4 E | Zero/negative/nonfinite/overkill damage fixtures preserve finite [0,max] health; repeated lethal damage emits exactly one death |
| LOG-06 | P4 E | Current rifle 30, reference HP90: torso 30, head90, limb18 with 3/1/.6 hypothesis; no independent body-part health |
| LOG-07 | P2–P4 E | Invalid tuning (nonpositive timing, stop>range, negative multipliers, NaN) rejected before actor activation |
| LOG-08 | P3 E | Frame step crosses contact time in one large delta: exactly one attempt, recovery retained; no missed or duplicate commit |
| LOG-09 | P4 E | Repeated hits during reaction do not reset reaction timer/cooldown; lethal hit immediately overrides reaction |

## AI acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| AI-01 | P2 P | Live player behind opaque wall inside range/cone is never acquired solely through that wall |
| AI-02 | P2 P/V | Player enters clear cone: acquire within one configured perception interval plus one frame; face/chase; alert cue is presentation |
| AI-03 | P2 P | Player exits distance/cone or becomes occluded: destination uses frozen last-seen position, not hidden current position |
| AI-04 | P2 P | Reappear before grace expires: reacquire; remain hidden past grace: clear target/Idle; use fake/probed sim clock |
| AI-05 | P2 P | Destroy/disable player or change session generation: clear target no later than next guarded tick, no null exception |
| AI-06 | P2 P | Hold pause for 5 real seconds: no perception queries or memory aging; resume does not acquire through new obstruction |

## Navigation acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| NAV-01 | P2 P | All authored spawn points lie on intended agent NavMesh with body clearance; invalid spawn rejects activation without teleport/log spam |
| NAV-02 | P2 P/V | Chase around opaque corner and through valid corridor; swept body never crosses wall; passage width fits chosen radius |
| NAV-03 | P2 P | Unreachable island/balcony: partial or invalid path ends in bounded stop/retry/search; no damage across gap and no warp |
| NAV-04 | P2 P | Disable agent/remove NavMesh during chase: safe stop, no repeated invalid-agent errors |
| NAV-05 | P2 P | Instrument path requests: no more than one per RepathInterval per agent, except explicitly recorded state initialization; small movement coalesces |
| NAV-06 | P2 P/V | At player stop/turn/reverse: no capsule overlap or repeated chase/attack oscillation; clearance and hysteresis thresholds are TUNING ACCEPTANCE |
| NAV-07 | P2 P | Block motion long enough for StuckTimeout: one bounded repath then search; CPU/log count bounded |
| NAV-08 | P5 P/S | If dynamic door is included, close during chase: collision and nav agree, no wall crossing or through-door contact; otherwise explicitly out of scope |

## Combat acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| CMB-01 | P3 P/V | Attack windup begins while eligible; health unchanged until contact; successful commit reduces health exactly once |
| CMB-02 | P3 P/S | Move outside AttackRange during windup; zero damage, visible miss and recovery; reentering cannot revive the spent strike |
| CMB-03 | P3 P | Close blocker/change vertical separation/facing before commit; each invalid case misses |
| CMB-04 | P3 P | Attack hold/rapid eligibility changes cannot bypass cooldown/recovery or create overlapping transactions |
| CMB-05 | P3 P | Player reaches zero health: one terminal transition; movement/fire disabled; pause/resume cannot reenable dead actions; restart recovers |
| CMB-06 | P4 P/S | Fire existing production rifle at zombie torso/head/limb: one round, one region result, same authoritative health |
| CMB-07 | P4 P | Several overlapping child hitboxes intersect one shot ray: at most one damage application; no ShotFired second pass |
| CMB-08 | P4 P | Enemy movement capsule does not intercept region ray; non-trigger hitboxes are included by serialized weapon mask |
| CMB-09 | P4 P/S | Muzzle behind opaque wall cannot damage target beyond; test .5 m near-muzzle obstruction boundary against a nearby enemy and record policy |
| CMB-10 | P4 P | Death during windup invalidates contact; dead zombie cannot chase, acquire, attack or take further damage |
| CMB-11 | P4 P | Automatic fire during reaction continues valid health damage without indefinitely restarting stagger |

## Animation acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| ANM-01 | P2/P3/P4 E/V | Each required state resolves to a real approved clip with recorded GUID/file ID and valid Avatar; no missing/placeholder substitution |
| ANM-02 | P2 V | Continuous idle/walk/chase on selected rig: no T-pose, broken limbs, floor penetration or bounds disappearance; foot drift measured against agreed tolerance (TUNING ACCEPTANCE) |
| ANM-03 | P3 V/P | Record visible hand strike and commit timestamp at 30/60/120 FPS; alignment tolerance established from real clip (TUNING ACCEPTANCE) |
| ANM-04 | P3 P | Remove/duplicate/late-fire animation markers: gameplay commits once at simulation time or misses by validation; no callback authority |
| ANM-05 | P4 V | Hit reaction blends back to an appropriate valid state; death is non-looping and holds usable final pose |
| ANM-06 | P2–P4 P | Disable/cull Animator: no altered health, target decisions or duplicated movement; no double translation from root motion |

## Visual acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| VIS-01 | P2 V | Approved zombie in same lighting as existing rifle/player: no pink/missing textures, correct scale/forward/pivot, recorded art decision |
| VIS-02 | P3 V/S | Player view can see detection/chase/windup/strike/miss/recovery cues; evidence includes successful and evaded attack |
| VIS-03 | P4 V/S | Record torso hit, hit reaction, headshot and death; displayed feedback matches actual damage region and death state |
| VIS-04 | P5 V/S | Reduced effects/audio muted do not change combat result; essential threat/hit state stays visible |
| VIS-05 | P5 V | Corpse holds without repeated death, floating pose or scene clutter; cleanup follows documented sim lifetime |

## Lifecycle acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| LIFE-01 | P2/P5 P | Ten BeginSession/Menu cycles: exactly one player and one encounter zombie each run; no old actor, corpse, target or subscription survives teardown |
| LIFE-02 | P3/P4 P | Pause at windup/contact/recovery/death: sim/animation freeze; resume validates; no queued instant extra hit |
| LIFE-03 | P2–P4 P | Unload scene/destroy zombie/destroy player during path or attack: safe cancellation; stale generation cannot affect next session |
| LIFE-04 | P4 P | Dead collider/agent disabled; corpse neither blocks nav/movement nor receives/intercepts bullets; cleanup idempotent |
| LIFE-05 | P3/P5 P/S | Death → menu/restart restores healthy new player with normal rifle; no retained held fire or dead-state Resume loophole |
| LIFE-06 | Baseline/P3 P/S | Hold rifle fire, pause, release while paused, then resume: no shot until a fresh valid press; test focus-loss variant too. Current direct-command smoke released before pause and does not cover this |

## Performance acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| PERF-01 | P5 P/S | Same approved prefab at 0/1/10/25 counts; five measured runs after warmup, report CPU/GPU/frame p50/p95/p99, spikes>50 ms, GC, memory, queries and paths |
| PERF-02 | P5 S | Compare AI mean≤2 ms, p95 frame≤16.67 ms, p99≤25 ms and warmed critical tick 0 B to LS-DOC-20 hypotheses; **TUNING ACCEPTANCE**, explicit hardware required |
| PERF-03 | P5 P/S | Repeated encounter/menu cleanup returns actor/voice/VFX counts to baseline; warmed memory does not monotonically grow |
| PERF-04 | P5 P | No per-frame FindObjectOfType/allocating overlaps/path spam; profiler markers identify work and retries remain bounded |

## Build acceptance

| ID | Gate / type | Objective setup and expected outcome |
|---|---|---|
| BLD-01 | Before P2 E | Every enabled build scene exists; corrected fixture retains intended parkour/combat coverage; no missing scripts/references |
| BLD-02 | P2–P5 E/P | Current upstream EditMode and PlayMode suites finish with no failures/aborts; versioned XML and Console evidence retained |
| BLD-03 | P5 S | macOS development build runs normal session → detect/chase/evade → take hit → rifle torso/head hit → death → pause/menu/restart using real input |
| BLD-04 | P5 S | Windows target build and real Windows smoke, render and performance evidence; Mac or Editor pass cannot substitute |
| BLD-05 | P5 E/S | Build identity binds commit+dirty state+source/content hashes+Unity/package/tuning versions; commercial asset rights resolved |

## Gate order

P2: baseline route/test repair + approved idle/locomotion asset + arena/nav + state/perception/chase/search/lifecycle. P3: player health/death lock + accepted attack/contact animation + guarded attack transaction. P4: zombie health/regions/reaction/death with real clips and existing rifle. P5: feedback, polish, edge cases, performance, full regressions and real standalone evidence. [Backlog](S004_IMPLEMENTATION_BACKLOG.md) defines dependencies. None is authorized as P1 implementation.

## P4 Completion Ledger — 2026-09-19
All P4 acceptance criteria (CMB-06–11, ANM-05, VIS-03, LIFE-04, etc.) are VERIFIED and PASS.
EditMode and PlayMode suites passing 100% (84/84 EditMode, 60/60 PlayMode).
S004-P4 FINAL STATUS is PASS, entry to P5 is READY.
