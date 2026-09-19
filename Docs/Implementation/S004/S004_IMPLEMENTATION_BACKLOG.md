# S004 P2–P5 implementation backlog

**Recovery status 2026-09-19: B01/B02 and B03 technical acceptance retained. P2-01 through P2-05 implemented; P2-G awaits this recovery run’s final regression/visual review. P3–P5 remain NOT_STARTED.** P2 technical entry is READY; license entitlement evidence remains pending for commercial clearance. P2 recovery and completion is explicitly authorized by the current task; P3 implementation is not authorized. Existing useful code and source GUIDs must be preserved.

Existing directories referenced below are real; Actual P2 paths are Runtime/AI/, Enemies/Zombie/Prefabs/LS_Zombie_Runtime.prefab and Scenes/ZombieAcceptance.unity; original proposal paths below are historical. Each task should produce a small reviewable change plus the cited acceptance evidence. Test IDs refer to S004_ACCEPTANCE.md.

| ID / title | Dependencies | Files / systems affected | Acceptance | Test type | Risk | Expected output |
|---|---|---|---|---|---|---|
| B01 — Reconcile baseline scene routes | P1 findings | EditorBuildSettings; current SampleScene; old S001Project/CombatAcceptanceProject paths; existing scene tests | BLD-01; maintain both actual parkour and combat fixture intent, no blanket alias | E/P | R09 | Explicit chosen scene ownership, valid build paths, restored/reconciled fixtures |
| B02 — Restore reproducible regression runner | B01 | Existing PlayMode test teardown/runner path; no weakened assertions | BLD-02; full XML completion and no runner-destruction abort | E/P | R10 | Reproduced cause, minimal correction, fresh baseline report |
| B03 — Obtain and accept one Shambler asset | P1 decision; product/source input | Asset intake manifest; project adapter/material/controller proposal | ANM-01/VIS-01; license, rig, idle/walk and all attack/react/death sources identified; no placeholders | E/V | R01–R05 | Approved model and animation set, measured contact/stride/corpse data |
| P2-01 — Author bounded nav acceptance arena | B01/B03 | Proposed Scenes/Combat/ZombieAcceptance; installed AI Navigation; agent settings | NAV-01/02/03; actual baked walkability, fixed wall/corner/corridor/unreachable island | P/V | R07/R08 | Deterministic geometry, spawn and NavMesh data |
| P2-02 — Define tuning and plain state | B02/B03 | Proposed Runtime/AI/ZombieDefinition and ZombieRuntimeState | LOG-01/02/07; Idle/Chasing/Searching; no damage behavior | E | R05/R23 | Validated config snapshot, finite memory and legal transition tests |
| P2-03 — Perception adapter | P2-02 | Runtime/AI/ZombiePerception, injected player/LOS anchors | AI-01–06 | E/P | R14/R16 | Bounded cone/range/LOS, last-known memory, no wall tracking |
| P2-04 — Navigation adapter | P2-01/02 | Runtime/AI/ZombieNavigation + NavMeshAgent | NAV-01–07; no warp, guarded invalid agent, bounded requests | P/V | R05/R07/R20 | One position authority, stop/turn/repath/stuck handling |
| P2-05 — Compose production actor and encounter lifecycle | P2-03/04, B03 | Proposed controller/prefab; SessionFlow explicit participant hook; ZombieEncounter; SampleScene | AI-02/05/06, LIFE-01/03; same prefab/path in normal scene and arena | P/V | R16/R21 | One animated chase/search actor with safe session ownership |
| P2-G — Core AI gate | All P2 | Evidence + P1 decision updates | All P2 E/P/V criteria; upstream regressions complete; no damage code claimed | E/P/V | R01/R10 | P2 report and concrete P3 readiness |
| P3-01 — Player health and terminal session contract | P2-G | PlayerHealth/HealthState, Player prefab, SessionFlow/AcceptanceHud death action integration | LOG-04/05, CMB-05, LIFE-05; Resume cannot unlock dead player | E/P | R06/R17 | Health reduction, one death, explicit restart/menu; no survival inventory |
| P3-02 — Attack transaction | P3-01 + accepted attack timing | ZombieCombat + state extensions | LOG-03/04/08, CMB-01–04; one attempt, LOS/range at contact, preserved recovery | E/P | R18 | Windup/commit/miss/recovery/cooldown with generation protection |
| P3-03 — Attack presentation and evasions | P3-02 | ZombieAnimationPresenter/controller, approved attack clip | ANM-03/04, VIS-02, LIFE-02/03; event absence/duplication harmless | P/V | R02/R18 | Recorded contact alignment, readable evasion window |
| P3-G — Enemy-to-player gate | All P3 | Evidence + tuning record | Enemy hit/miss/death-lock route in both scenes and no through-wall damage | E/P/V | R06/R18 | P3 report; no player-to-zombie completion claim |
| P4-01 — One enemy health and region proxies | P3-G | ZombieHealth/HealthState, ZombieHitbox, prefab bones; actual weapon hitMask; collision layers | LOG-05/06, CMB-06–09; source damage path retained | E/P | R11–R15 | One owner, non-trigger region colliders and correct masks |
| P4-02 — Reactions and anti-stunlock | P4-01 + accepted reaction clip | Runtime state/presenter | LOG-09/CMB-11/ANM-05 | E/P/V | R19 | Bounded reaction, continuous valid damage, no timer-reset exploit |
| P4-03 — Death and corpse lifecycle | P4-01/02 + accepted death clip | State/health/navigation/presentation/encounter | CMB-10/LIFE-02–04/VIS-03/05; one death, disabled collision, timed cleanup | E/P/V | R16/R18/R24 | Final corpse pose and terminal combat state, no loot/save |
| P4-G — Reciprocal combat gate | All P4 | Evidence + matrices | Rifle torso/head/limb + reaction/death, zombie hit/miss, preserved upstream behavior | E/P/V | R11–R19 | Complete functional single-zombie route |
| P5-01 — Production audio/VFX and animation polish | P4-G, licensed feedback content | Zombie presenters; scoped rifle feedback wiring if needed; assets | VIS-02–05, ANM-02/03/05; cue concurrency and reduced-effect parity | V/P | R02/R25 | Approved feedback content, contact/stride/pose evidence |
| P5-02 — Lifecycle and combat edge closure | P5-01 | Existing/new tests and small proven fixes | LIFE-01–05, CMB-02/03/07/09; 30/60/120 FPS, pause/unload/missing target | E/P/S | R10/R13/R16–R19 | No unresolved core failures or stale callbacks |
| P5-03 — Profile 1/10/25 | P5-01/02 | Profiler markers, representative scenes; optimization only as measured | PERF-01–04 with target hardware, warmup, 5 repeats | P/S | R20 | Captures, percentile/query/GC/memory report and justified fixes |
| P5-04 — Standalone build and acceptance | P5-02/03, Windows device/CI | Existing build utility after B01; Mac/Windows outputs and identity | BLD-02–05, full normal input route, no missing shaders/scripts | E/P/S/V | R04/R22 | Versioned builds, platform-specific results, playable evidence |
| P5-G — Production closure | All P5 | Final acceptance, risk/status and planning update | No critical mandatory NOT_RUN/FAIL; unresolved noncritical risks explicitly owned | Review | All | Honest PASS/PARTIAL/BLOCKED report, no premature release |

## Dependencies deliberately deferred

Inventory, chamber/staged magazine redesign, survival wounds/infection, death bags, shelter recovery, corpse loot, persistence/streaming/population director, hearing/noise, co-op, alternate archetypes and ragdolls are not prerequisites for the requested one-zombie transient combat route. Their future integration requires separate scope and migration decisions. This does not mark old S002/S003 roadmap requirements complete.

## P2 entry checklist

- B01/B02 close scene and test baseline problems with full recorded regression results.
- B03 supplies an approved production character and genuine idle/locomotion; complete animation sources must be accounted for, with P3/P4 timing audits scheduled as explicit gates.
- Actual Avatar/material/scale/forward/hitbox anchor intake is recorded and license uncertainty resolved for the intended use.
- Agree scene ownership, agent profile and named-layer/query contract; bake and scene creation are P2 work, not falsely claimed P1 completion.
- Follow current user-defined S004-P2 scope; this backlog is not an instruction to start it during P1.


## B0B completion ledger — 2026-09-18

| Work | Status / evidence |
|---|---|
| Exact imported model/animation discovery, source ownership | COMPLETE — imported-before.txt, source-safety.json |
| License/source record | COMPLETE AS STATUS RECORD — LICENSE_EVIDENCE_PENDING locally; no legal clearance claim |
| Model/scale/forward/Humanoid/T-pose audit | COMPLETE — model-performance-audit.txt, avatar.txt, reference PNG |
| Project URP material and channel/normal validation | COMPLETE — channel comparison, material JSON, actual renders |
| Five role retarget, loop/root audit, contact/stride timing | COMPLETE — matrix, samples, three-view sequence |
| Death retarget floor correction | COMPLETE — only RootT.y adapted,129 other curves preserved |
| Nested production prefab and presentation-only Animator | COMPLETE — LS_Zombie_Shambler and AC_Zombie_Shambler |
| Editor-only preview and visual evidence | COMPLETE — acceptance scene, preview window, PNGs/video |
| Full regression / Console / docs | COMPLETE —38/38 EditMode,23/23 PlayMode,0 project errors |

Future: P2 agent-speed/foot contacts; P3 attack timing validation/authority; P4 reactions/hit regions/flat-vs-sloped death; P5 101-bone/2K crowd profile and LOD if required. No B0B completion implies these gameplay tasks are implemented.


## P3 implementation ledger (2026-09-19)

P3-01 implemented: production health, existing damage contract, death input/weapon lock, minimal HUD and fresh session health.
P3-02 implemented: measured timer authority, bounded windup turn, locked commitment, one contact attempt, collider-aware range/arc/world validation, true misses and visible recovery.
P3-03 implemented: real Attack presentation synchronization, actual motor evasion, pause/lifecycle/normal-route tests and visual captures.
P3-G closure and P4 entry are recorded only in S004_P3_IMPLEMENTATION_REPORT.md after full regression. P4 source is not implemented. P5 balance/crowd/audio/platform and commercial-release gates remain future work.
