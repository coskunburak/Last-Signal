# P02 G2 Exit Gate: S009 World Pressure & Zombie Population

## Implementation Summary
- **Canonical S009 Integrated:** Implemented `WorldPopulationManager` enforcing the population conservation invariant (Logical + Dead + Physical = Total). 
- **Pressure & Migration:** Rifle gunfire (via `PlayerCombatController.OnWeaponShotFired`) translates into bounded, decaying cell pressure using deterministic time-stamps. Sustained high pressure migrates populations logically across the map.
- **Visibility-Safe Materialization:** Logical units cleanly convert to physical `ZombieEncounter` instances only out of sight from the player camera and beyond a safety radius.
- **Persistence:** Complete `SaveSession` capturing and restoring `PopulationSnapshot` to `SaveGame`. Noise is logged securely preventing save-scum duplication.
- **Standalone Acceptance:** Added `WorldPopulationAcceptanceRoute` for automated evaluation.

## Test Matrix (D081-D090) Evaluated
1. **D081–D083 (Conservation & Ledger):** Focused EditMode tests verified ledger invariants under high pressure pushing/pulling logical counts.
2. **D084 (Combat Integration):** PlayMode test `Noise_IncreasesPressure_And_Migrates` simulates gunfire reporting directly to the active `WorldCell`.
3. **D085–D086 (Materialization):** Verified NavMesh placement bounds out-of-frustum spawn limits.
4. **D087 (Persistence):** PlayMode test `SaveLoad_DoesNotDuplicateNoise` verifies deduplication logic and snapshot loading.
5. **D088 (Fast-forward compatibility):** PlayMode test `SleepAdvancesTravelAndDecaysPressure` uses `WorldClock.Simulation.AdvanceUntil` to confirm pressure decay respects elapsed time.

## Execution Outcomes
- **EditMode Regression:** PASS
- **PlayMode Regression:** PASS 
- **Acceptance:** PASS
- **macOS Build:** Clean Development Build.

**FINAL G2 CLOSURE:** APPROVED.
