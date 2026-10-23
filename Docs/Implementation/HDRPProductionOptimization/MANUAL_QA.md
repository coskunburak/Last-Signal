# Manual QA — current gate

> Current instruction (2026-10-22): Unity runs are owner-operated. See [CURRENT_HANDOFF.md](CURRENT_HANDOFF.md) for the next exact commands and latest reviewed results. Earlier next-step and authorization statements below are historical.


Latest gate: owner shader validation **18/18 PASS, zero errors**. The previous shader command below is completed history. Current next command: `python3 Tools/HDRPProductionOptimization/manual_checks.py prepare-mask-candidates`. See [MASK_CANDIDATES_HANDOFF.md](MASK_CANDIDATES_HANDOFF.md) for exact scope and safeguards.

Latest owner evidence: compilation PASS; focused audit tests **6/6 PASS**, zero skipped; production audit collected successfully but found **45 active materials needing repair** and **46 URP component instances**. See [AUDIT_RESULTS_REVIEW.md](AUDIT_RESULTS_REVIEW.md). The earlier “run now” commands below are completed history; next gate is `python3 Tools/HDRPProductionOptimization/manual_checks.py validate-shaders`. Do not rerun passing suites without affected changes.

Status: no Unity authoring, EditMode, PlayMode, benchmark or build was executed by the agent. Historical 4/4, 18/18 and 36/36 reports are not new validation.

Transfer verification is complete: 107,016 files, zero mismatches. The 108,402-file recovery checkpoint remains preserved. Current status: **PARTIAL — awaiting owner verification**. URP removal and the M3 Pro Development profile follow dependency fixes and the staged verification gates.

## Run now: compile, focused audit tests, then collect production coverage

Save and close Unity normally. Commands run one gate each; stop at any failure and return its log/XML/receipt. Do not remove lock files. The runner discovers Unity 6000.5.0f1; override with `--unity /path/to/Unity` or `UNITY_EDITOR`.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/manual_checks.py compile
python3 Tools/HDRPProductionOptimization/manual_checks.py audit-edit
python3 Tools/HDRPProductionOptimization/manual_checks.py production-audit
```

Return the three timestamped folders under `Docs/Implementation/HDRPProductionOptimization/Evidence/` and `production_material_coverage.json`. `audit-edit` selects the new `LastSignal.Tests.HdrpMaterialCoverageTests` (six tests). Expected: no compiler/package errors; exactly six focused tests passed, zero failed/skipped; production coverage report available. Run each command separately and continue only after it succeeds. `AUDIT_COMPLETE_REVIEW_REQUIRED` is NOT a migration PASS. Pause here for findings review before conversion or package removal.

Windows equivalents, from the copied project root (Python 3.10+ and matching Unity installed):

```powershell
& .\Tools\HDRPProductionOptimization\manual-checks.ps1 compile
& .\Tools\HDRPProductionOptimization\manual-checks.ps1 audit-edit
& .\Tools\HDRPProductionOptimization\manual-checks.ps1 production-audit
```

## Later focused checks — only after current audit and fixes are reviewed

```bash
python3 Tools/HDRPProductionOptimization/manual_checks.py validate-shaders
bash Tools/ForestPrototype120/manual-checks.sh sources
bash Tools/ForestPrototype120/manual-checks.sh references
bash Tools/ForestPrototype120/manual-checks.sh edit
bash Tools/ForestPrototype120/manual-checks.sh play
bash Tools/ForestPrototype120/manual-checks.sh regression-edit
bash Tools/ForestPrototype120/manual-checks.sh regression-play
python3 Tools/HDRPProductionOptimization/manual_checks.py world-edit
python3 Tools/HDRPProductionOptimization/manual_checks.py world-play
```

Exact verified filters: ForestPrototypeTests; ForestPrototypePlayTests; P01TreeStructureTests/P01TreePersistenceTests/FirstTownPersistenceTests; MovementAcceptanceTests/P01TreePhysicsTests; WorldTimeTests/FirstTownPersistenceTests; WorldTimeIntegrationTests/FirstTownPlayTests. All are under LastSignal.Tests and their source files exist. Run only affected suites at each phase, not every command repeatedly. `generate` remains available but is NOT part of this gate and would regenerate the forest.

The forest wrapper now writes timestamped evidence directories instead of fixed filenames. All actions omit `-nographics`; test actions also omit `-quit` so the Test Runner controls completion. Zero tests, skipped/failed tests, missing/malformed XML or error diagnostics fail the runner. C# compilation cannot be certified by Python/static checks.

## Manual scene and rendering acceptance

Open FirstTownCore and ForestPrototype120 separately without saving migration edits. Check Console, missing scripts/prefabs, Game view materials and the visual subjects listed in HDRP_VALIDATION.md. In an existing S004 validation scene, exercise chopping, notch, detachment, falling/settling, resource collection and save/reload with unchanged identities. Confirm movement/crouch/jump and physical collision. Review the authoring Wood material accepts HDRP/Lit without rebuilding a stage merely to test a UI gate.

After corrected rendering: inspect zombie AI, inventory/equipment, character animation and grip/ADS, vehicles, audio, time/weather and save/load. Forest F1–F8 views/presets and F9 profiling remain user-operated. See PERFORMANCE_REPORT.md.

## Windows build gate

Install matching Editor and Windows Build Support via Hub. The current enabled build scenes are S001Acceptance and CombatAcceptance; FirstTownCore is not automatically a production build entry. Review an explicit Windows build profile and intended scene list before building. Manually build Windows x86-64 Development with High quality, then launch and inspect player.log, rendering, controls, save/load and shaders. No production build is prepared or claimed complete here.

## Evidence already supported

Static storage/LFS/package/quality inspections, hash-verified checkpoint, bounded cache deletion, preserved-source comparison, Python AST and shell syntax, command dry runs, existing test-filter source lookup, and runner XML fixture validation. These are distinct from Unity tests.

Final dependency scan after actual removal must show no URP package/direct or transitive owner, no URP assembly/type/shader includes and no required URP serialized component/subasset GUIDs. Historical recovery/docs references can remain. Re-run static storage inventory after import/build to measure steady-state cache return.
