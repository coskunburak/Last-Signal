# R03 — Zombie auditory perception, memory and investigate

Status: verification in progress. No R03 PASS claim until all required exit gates are recorded below.

## Baseline and documents

Branch `s009-audit-continuation-20260925`, HEAD `01a050f530fc1f86516eef2ebbf77fe1fd6696aa`; Unity 6000.5.0f1. R01 and R02 enter closed, R03 entry ready, S010 not ready. Existing dirty source/art/evidence was preserved. Baseline status and patch are in `Evidence/R03/20260927-foundation/`. No commit, push, reset or clean performed.

Read: pasted R03 mission; R02_IMPLEMENTATION_REPORT.md; PRE10_DECISIONS.md; production-plan sprints/S005.md; source 11_Zombie_AI_Perception_Population.md, 12_World_Pressure.md, 22_QA_Acceptance_Traceability.md. Inspected actual event/service, controller/state/perception/navigation/search/definition/health/presentation, SessionFlow, encounter/cell/population spawning, SaveSession/SaveGame, existing authoring/build/acceptance tests. No recursive GDD ingestion.

## Architecture and hearing

R02 `IGameplayNoiseListener` remains unchanged. `ZombieNoiseListener` registers by stable process-local ID, updates its spatial index position, receives only canonical candidates and rejects inactive, paused, dead, foreign-epoch, replayed, invalid or expired events. It calls a pure deterministic hearing evaluator, optionally one world-only raycast, then submits a value snapshot to `ZombieController`. The listener never changes state, navigation or damage. Session dependency is explicit through the player's GameplayNoiseContext. There is no service lookup or secondary noise distribution.

Hearing origin is `ZombiePerception.Origin`. Strength is intensity × category sensitivity × clamp01(1 − 3D distance / radius) × transmission. Quiet candidates below threshold skip physics. Walls multiply by .4 rather than silencing all categories. Collider layers on the listener's actor must be excluded from the authored world mask. Weather factor remains 1; no existing trivial production weather-acoustic seam was identified.

Initial production tuning (not final balance): threshold .12; walk/sprint/melee/gunshot sensitivity 1/1.1/1.2/1.5; memory 15 s; investigation 12 s; arrival .6 m. Canonical R02 profiles are unchanged. Definition validation rejects nonfinite/nonpositive tuning and transmission outside (0,1].

## Memory, priority and anti-omniscience

Memory owns the original immutable event, effective strength, occlusion, approach direction and age. It retains no source/player object, transform or callback. Confidence decays linearly over the authored memory duration. Incoming strength competes against current decayed strength; ties within .0001 use newer event time then sequence. The canonical ordered-stream epoch/high-water mark rejects duplicate and old identities even after consumption. Rejected weaker events cannot be replayed later to farm timers.

Only accepted event positions set investigation destinations. The player's current position/facing remains confined to existing visual sampling and combat validation. Auditory code never invokes `Observe(true,...)`. Search exposes its anchor/direction for evidence, and auditory search is explicitly seeded independently of visual memory.

## States, navigation and combat

`Investigating` is appended without renumbering existing states. Idle/Search can enter from sound; chase can fall back after its existing loss grace; confirmed sight promotes investigation to Chase. Hearing does not interrupt committed attack flow. Recovery can choose Chase, Investigation or Search according to legitimate evidence. HitReact preserves an active investigation and its budget; death remains terminal.

Navigation remains ZombieNavigation.MoveTo with its original destination refresh, path retry and stuck policies. New accepted events can retarget sufficiently displaced snapshots, but do not reset investigation age. Arrival, timeout, memory expiry or exhausted navigation hand off to auditory Search. Search completes its normal bounded duration even after auditory memory is consumed. No teleports or acoustic NavMesh solver were added.

## Lifecycle and persistence

Pause freezes controller time, memory and movement; receipt also checks canonical emission permission. Death/shutdown/rebind clears hearing and unregisters. Listener disable unregisters; listener reenable restores one registration in a still-valid actor binding. Whole-actor shutdown requires the existing Initialize/Bind lifecycle. New sessions use new R02 epochs.

Save inspection found health/pose and population persistence, not physical perception memory. No schema migration is needed or introduced. Load reconstructs safe fresh actors and fresh noise service; old events cannot replay.

## Verification

Pending final evidence aggregation. Initial focused EditMode: 30/30 PASS. Initial three PlayMode routes: 3/3 PASS. Subsequent expanded fixtures are under verification; these are not final release counts.

Executed anti-omniscience proof: muted real rifle event at A=(3.07,1.50,2.81), unseen player moved to B=(6,0,6), destination remained A and visual position/direction were unchanged. Executed state traces include Idle → Investigating → HitReact → Investigating → Searching and Investigating → Chasing on sight.

Historical evidence was backed up before test execution to `/tmp/r03-historical-evidence.tar`; final restoration and hash verification pending.

## Files and authoring scope

Added runtime: `AI/ZombieAuditoryMemory.cs` (snapshot, pure evaluator, memory), `AI/ZombieNoiseListener.cs`, `Noise/GameplayNoiseContext.cs`, development-only `AI/R03AcceptanceRoute.cs` and `AI/R03Development.cs`. Added Editor `R03Authoring.cs`; tests `EditMode/R03AuditoryTests.cs` and `PlayMode/R03AuditoryPlayTests.cs`; corresponding Unity metadata and this report/evidence.

Modified runtime: ZombieController, ZombieRuntimeState, ZombieDefinition, ZombieSearch and SessionFlow. Modified assets: Shambler.asset (nine hearing values) and Resources/LS_Zombie_Runtime.prefab (one listener). No new art, animation, model, navigation implementation, damage implementation or state-machine framework.

Narrow existing-tool/test adaptations: R02NoiseBuild retains its R02 method and gains BuildR03 using the same BuildPipeline. R02NoiseAcceptanceRoute now accounts for legitimate physical listeners in its probe-only historical listener-count assertions; it still checks empty old authority, no event replay, exact probe registration delta and producer/pressure behavior. ZombieLogicTests includes the new approved state edges while retaining old-edge checks. ZombieMeleeTests' replacement-player fixture explicitly carries its session noise context; the strike-transfer assertion is unchanged. PRE10_DECISIONS records 007–011.

Missing noise context/service or invalid/duplicate listener authoring fails explicitly. There is no silent global fallback. R02 GameplayNoiseEvent, GameplayNoiseSystem, IDs/grid/producer hooks/pressure adapter remain unchanged. R01 combat and persistence authority remain unchanged.

## Development observability

Markers: `LastSignal.Zombie.Hearing` and `LastSignal.Zombie.Investigate`, alongside existing AI/navigation/perception markers. Listener counters expose candidates, heard/accepted stimuli, queries and before/after state. Controller exposes auditory memory, investigation age/destination and search source; Search exposes anchor/direction. Editor gizmos mark the heard snapshot. Opt-in `-hearingDebug` or `-hearingAcceptance <directory>` installs an overlay; standalone acceptance additionally captures a temporary overhead view with yellow heard-location and cyan current-player markers. This instrumentation does not drive production decisions.

## Fixture findings and fixes

The initial integration compile caught Unity 6.5's removed GetInstanceID API; listener identity now uses a process-local monotonic high-bit namespace, avoiding collision with legacy low-ID acceptance probes. Resolved before acceptance.

The first expanded walk fixture sat outside its actual 3D hearing threshold; the route was moved closer, without changing production tuning. The test impact target originally used world layer 0 and therefore represented an acoustic wall; it now uses production hit-region layer 8. The manually stepped stationary-agent stress fixture legitimately invokes existing stuck recovery, so its assertion now enforces the configured bounded retry budget, not an impossible zero-retry condition. The original state-machine regression enumerated only pre-R03 legal edges; its contract now includes the requested new transitions. All findings remain visible in intermediate logs.

Review found one additional behavior edge: an accepted sound arriving during HitReact from Idle/Search must become eligible when the reaction finishes. Controller now considers that memory after reaction commitment ends, and a dedicated real-pipeline PlayMode test verifies HitReact → Investigating. An already active investigation preserves its budget and uses the latest accepted snapshot after reaction.

## Limitations and deliberate deferrals

Single world-mask line transmission is intentionally simple: no portal/material propagation, echoes, multi-bounce sound, weather acoustics or pack coordination. Values are initial production tuning. A heard muzzle-height or unreachable point may exhaust the existing path policy and seed a stationary bounded search; no false reachability or teleport is introduced. World Pressure remains independent. No player stealth meter, encounter redesign, Howler, networking or R04 integrated balancing has been started.
