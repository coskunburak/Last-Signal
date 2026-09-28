# PROJECT LAST SIGNAL — R02 IMPLEMENTATION REPORT

## 1. Baseline
*   **Branch:** `s009-audit-continuation-20260925`
*   **HEAD Commit:** `01a050f530fc1f86516eef2ebbf77fe1fd6696aa`
*   **Unity Version:** 6000.5.0f1
*   **Input System Version:** 1.19.0
*   **Date:** 2026-09-27

## 2. R01 Inherited Exception Baseline
There is one inherited Unity Input System batch-test infrastructure exception (`IndexOutOfRangeException: Index was outside the bounds of the array` from `InputManager.AddStateChangeMonitor`) occurring in `LastSignal.Tests.R01CombatPlayTests.RealInputSwitchCancelSoakAndSaveLoad` during the full PlayMode batch suite. The test passes correctly when run independently in an isolated Unity process (isolated XML evidence confirms 1/1 PASS). This is definitively classified as a known batch-only `KNOWN_INHERITED_R01_INPUT_SYSTEM_TEST_INFRASTRUCTURE_EXCEPTION` and does not represent an R02 defect.

## 3. Documents Actually Read
During this final closure, the following artifacts were directly parsed:
*   Project working tree state via Git.
*   Standalone runtime evidence (`standalone-smoke.txt`).
*   Test evidence XMLs (Full PlayMode, Full EditMode, Focused and Regression tests, Isolated inherited test).
*   Performance and soak evidence (`performance.txt`, `footstep-performance.txt`, `soak.txt`).
*   R02 Architecture implementation code (`GameplayNoiseEvent.cs`, `GameplayNoiseSystem.cs`).
*   Existing PRE-S010 decisions (`PRE10_DECISIONS.md`).
*   Mac development build log (`build.log`).

## 4. Pre-R02 Architecture Discovered
Before R02, the firearm firing path directly hooked into the world pressure system without establishing generic auditory distribution:
`ShotFired → PlayerCombatController → WorldPopulationManager.ReportShot → ReportNoise`.
This path correctly updated regional disturbance and population migration consequences but did NOT supply local physical hearing candidates to zombies. 

## 5. Architecture Implemented
R02 implemented a centralized canonical gameplay-noise pipeline that distributes physical noise candidates to a listener grid, while seamlessly acting as a facade for the underlying regional world pressure systems. 

### GameplayNoiseEvent Contract
The canonical `GameplayNoiseEvent` is immutable and captures the state at exactly the time of commit. It includes:
*   `EventId`: `ulong Epoch`, `ulong Sequence`.
*   `SourceId`, `ActionId`.
*   `Position`: `Vector3` captured at the time of commit.
*   `Category`: `GameplayNoiseCategory` (Footstep, SprintFootstep, MeleeImpact, Gunshot).
*   `BaseRadiusMeters`, `Intensity`: Float values defined by the source's noise profile.
*   `SimulationTime`, `ExpiresAt`: High-resolution simulation seconds.

### Event ID Strategy
A session `epoch` combined with an ordered monotonic `sequence` serves as a stable, process-unique session event ID.

### Dedupe Policy
`GameplayNoiseSystem` guarantees strict delivery using an O(1) high-water mark sequence tracker within the current epoch, completely disregarding duplicate/old sequence IDs and rejecting future/foreign-epoch identities. It also contains a diagnostic rolling history buffer capped at 64 entries.

### Spatial Distribution
Listeners register to an XZ grid mapping world coordinates. The grid bucket size is strictly bounded to `16` meters (`CellSize = 16`). Candidate scans iterate nearby bucket arrays, minimizing allocation overhead.

### Listener Contract
Entities requiring real-time noise updates implement `IGameplayNoiseListener`. They supply a unique `ListenerId` and implement `ReceiveNoise(in GameplayNoiseEvent noise)`.

## 6. Producers
*   **Footstep:** Bounded to a 1.6m grounded displacement threshold (walking/sprinting).
*   **Sprint:** Bound to the same stride logic but leverages a stronger footprint (SprintFootstep profile).
*   **Crowbar:** Hooks directly to committed physical impacts. Air swings and misses are strictly ignored and remain silent.
*   **Firearm:** Captures origin *at the muzzle at shot commit*. A committed round into a wall accurately produces a gunshot noise, completely isolated from hitscan success against an organic target.

## 7. Footstep Cadence
Footstep cadences are integrated into the player motor. The threshold is defined as a `1.6m` grounded stride. During development, a route failing to yield a footstep (due to traveling only `1.52m` when facing a wall) correctly identified that route as non-compliant and verified that stationary/short movement avoids noisy emissions. Open-route travel successfully verified normal displacement limits.

## 8. World Pressure Adapter & Category Policy
Local physical hearing candidate distribution and World Pressure are separate consumers of the identical canonical event. 
*   **Footstep, Sprint, MeleeImpact:** Local spatial distribution only. No regional pressure contribution.
*   **Gunshot:** Local candidate distribution *and* World Pressure adapter contribution (1-to-1 exact receipt forward to `WorldPopulationManager`).

## 9. Audio Independence
PlayMode routing safely confirmed that audio playback is disconnected from gameplay noise. Whether audio presentation is muted or missing, gameplay noise occurrences remain active. No components derive logic from `AudioSource.isPlaying` or Animator/mixer volumes.

## 10. Lifecycle & Save/Load
`GameplayNoiseEvent` traces are fundamentally transient and session-bound.
*   **Session Lifecycle:** Returning to the menu safely wipes all active listener hooks, buckets, and sequences. No stale noise callbacks persist upon a fresh session cycle.
*   **Save/Load:** The game's save/load operation *does not* replay historical local raw events. World pressure continues to persist itself in legacy ledgers, without expanding the save schema for transient events.

## 11. Performance & Soak Results
Performance limits satisfy all structural requirements with no FindObjects broadcast bounds nor unbounded dictionary growth:
*   **Delivery Overhead:** 100 registered listeners, 1000 warmed emissions -> `0.605 ms`, `0 managed bytes`, scanning `10 candidates/event` in `4 cells/event`.
*   **Substep Footstep Calculation Overhead:** 10,000 continuous warmed movement substeps -> `0 managed bytes`.
*   **Session Soak Cycles:** 6 continuous heavy session loops spanning footstep and combat transitions perfectly released memory without duplicating callbacks. Epoch rollover and cleanup were fully intact.

## 12. Test Result Matrix
*   **R02 Focused EditMode:** 24 / 24 PASS
*   **R02 Focused PlayMode:** 2 / 2 PASS
*   **R01 Regression EditMode:** 12 / 12 PASS
*   **R01 Regression PlayMode:** 3 / 3 PASS
*   **Full EditMode:** 300 / 300 PASS
*   **Full PlayMode:** 127 / 128 PASS (The 1 failing test is the isolated inherited Input System batch framework failure, proven passing directly in isolation. 0 project-owned R02 tests failed).
*   **Isolated R01 Test Result:** 1 / 1 PASS 

## 13. Build & Standalone Acceptance
*   **Mac Development Build:** PASS. Build log reported `Build Finished, Result: Success`. Application built at `./Builds/R02/LastSignal.app`.
*   **Standalone Acceptance:** PASS. Standalone smoke tests proved application launches, UI and inputs work, footprints register upon movement and strictly stop when stationary, sprint out-ranges walk limits, rifle/crowbar successfully execute their unique profiles, world pressure accurately updates on gunshot, and noise observability UI captures the traces correctly. (0 runtime exceptions).

## 14. Known Limitations
None beyond deliberately deferred logic. The implementation achieves structural intent without compromising legacy boundaries.

## 15. Deferred to R03
R02 is explicitly constrained to delivering physical candidates. No logic alters existing population behavior yet. The following are aggressively deferred to **R03**:
*   Actual zombie hearing decisions (thresholds, decibels)
*   Auditory memory and timestamp caching
*   Investigate state
*   Suspicious state
*   Confidence decay and stimulus prioritization
*   Environment acoustic transmission simulation and refinement
*   Vision/hearing fusion tracking
*   Stealth encounter mechanics and balancing

## 16. Final Verdict

R02: PASS
R03 ENTRY: READY
S010 ENTRY: NOT YET READY
