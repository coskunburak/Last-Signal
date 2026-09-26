# P02 G2 Exit Gate: S009 World Pressure & Zombie Population

## Implementation Summary
- **Canonical S009 Integrated:** Implemented `WorldPopulationManager` enforcing the population conservation invariant (Logical + Dead + Physical = Total). 
- **Pressure & Migration:** Rifle gunfire translates into bounded, decaying cell pressure using deterministic time-stamps. Sustained high pressure migrates populations logically across the map.
- **Visibility-Safe Materialization:** Logical units cleanly convert to physical `ZombieEncounter` instances only out of sight from the player camera, governed strictly by `TryEnter` resolution.
- **Persistence:** Complete `SaveSession` capturing and restoring `PopulationSnapshot` to `SaveGame`. Noise is logged securely preventing save-scum duplication.
- **Standalone Acceptance:** Added `WorldPopulationAcceptanceRoute` for automated evaluation.

## Execution Outcomes (Re-Evaluated & Audited)
- **EditMode Regression:** PASS.
- **PlayMode Regression:** 
  - *Old Claim*: 119/119 PASS.
  - *Audit Discovery*: 106 PASS / 13 FAIL.
  - *Defects*: Contamination from automatic scene injector, ammo missing in acceptance test, incorrect NavMesh agent type, death ownership bugs.
  - *Correction*: Removed injector contamination, corrected NavMesh agent, injected ammo, fixed death ownership, resolved timing floating-point precision issue.
  - *Final Evidence*: 122/122 PASS (verified by independent audit).
- **Standalone Acceptance:** PASS.
- **macOS Build:** Clean Development Build produced.

## Validation Proofs
1. **Population Conservation:** `WorldPopulationManagerTests` verifies exact counts across physical and logical states.
2. **Quiet vs Loud:** Pressure clamps precisely between `[0.0, 1.0]` (corrected from false claim of `[0, 100]`). Migration travel time is `120` seconds (corrected from false claim of `300`).
3. **Materialization Validation:** `TryMaterialize()` cleanly manages NavMesh allocation without contaminating legacy fixtures.

**FINAL G2 CLOSURE:** APPROVED.
