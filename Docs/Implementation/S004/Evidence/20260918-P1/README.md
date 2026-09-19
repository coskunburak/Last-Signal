# S004-P1 evidence index

Local date 2026-09-18. Editor audit records are 2026-09-17 21:xx UTC (after midnight locally); final documentation/source checks were completed on 2026-09-18 UTC after the user's continuation. Project /Users/burakcoskun/Last Signal; Unity6000.5.0f1; target StandaloneOSX. Original dirty tree is part of the baseline; commit alone does not describe source content.

## Result and provenance

- **BASELINE PARTIAL**: compilation succeeds and live player/rifle path works; current regression/build routes are broken.
- **ASSET BLOCKER**: no approved production zombie; all five required clips missing.
- **P1 PARTIAL / P2 BLOCKED**. No P2 implementation.
- Unity Assistant MCP supplied direct Editor/asset/API evidence after successful identity check. Coplay unityMCP custom-tools returned none and instances returned zero; it was not used to claim runtime verification.
- Intermittent Assistant discovery loss occurred across PlayMode transitions. Computer Use observed the actual Unity Editor and clicked its Play button once; subsequent MCP verified the live runtime.
- No external source search or marketplace entitlement assumption was used.

## Artifact map

| Artifact | What it proves / limitation |
|---|---|
| baseline-commit.txt, initial-git-status.txt | Starting commit and dirty tree; not a clean release identity |
| initial-source-sha256.json | Initial exact bytes of Assets/Packages/ProjectSettings/Tools including meta files |
| final-source-comparison.json | Final byte comparison; no production source change; removes only P1-generated temporary scene pair(s) |
| compile-session-excerpts.txt | Requested compilation and domain reload, fresh EditMode count, real session starts; curated lines, not a wholesale Editor log |
| final-editor-state.txt | Correct project/version, no compile failure, edit mode, clean SampleScene; actual player/world-body layers and rifle controller/audio absence |
| editmode-results.xml | Current 34 tests:33 pass/1 fail, missing S001Acceptance; source-backed result |
| playmode-abort-console.json | Full suite missing CombatAcceptance and subsequent coroutine/runner errors; **ABORTED**, no result XML |
| playmode-stable-abort-console.json | Isolated movement subset also aborts on coroutine runner; **ABORTED**, no result XML |
| runtime-baseline.txt | SampleScene auto-session, one player/camera/rifle, 30→29 ammo/100→70 HP, reload30/119, pause/menu and fresh restart30/120 |
| current-rifle-runtime.png | Actual camera render of working VAL/MR POLY rifle; inspected; screen-overlay HUD not captured |
| imported-assets.txt | Seven imported FBX audits, actual Avatar/geometry/bones/materials/clip sub-assets and vendor prefab components |
| asset-audit-command.cs.txt | Reproducible transient MCP diagnostic source; not an imported .cs script or runtime helper |
| scene-prefab-navigation.txt | Current scene geometry/references, missing build scene paths, no nav data/triangulation, agent profile, layers/masks |
| scene-audit-command.cs.txt | Transient read-only Editor query that generated that report |
| vendor-materials-urp.png | Original vendor materials rendered under current URP: all three pink; left captain, center crew, right alien |
| material-render-command.cs.txt | Isolated temporary preview scene, camera/light/render; all temporary objects/textures cleaned, no scene/material save |
| archive-inventory.json | Unimported root Assets.zip contains the same seven model paths; no new candidate animation pack; not extracted |
| final-console.json | Closing Console entries, including diagnostic errors; not mislabeled clean |
| documentation-validation.json | Required-document/link/acceptance-ID validation; not gameplay acceptance |
| evidence-sha256.json | Content hashes for final package artifacts (excludes itself) |

Final validation: all nine required documents exist, 60 uniquely identified acceptance items cover all nine categories, and no local Markdown links are broken. All 336 snapshotted files under Assets/Packages/ProjectSettings/Tools retain their original bytes, with no additions or removals. These are documentation/source-preservation checks, not a fresh gameplay PASS.

## Baseline test procedure and limits

Forced script compilation via UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation, waited until Editor compileFailed=false/isCompiling=false. Existing TestRunnerApi started LastSignal.EditModeTests; existing CombatRegressionRunner.Results callback saved XML through LastSignal.RegressionPath redirected to this P1 folder. Historic evidence was not overwritten.

Full LastSignal.PlayModeTests run attempted next. Missing CombatAcceptance load plus runner exceptions caused abort without RunFinished XML. A second run isolated MovementAcceptanceTests with group filter `^LastSignal.Tests.MovementAcceptanceTests\\.(?![YZ]Integrated)`; it too aborted with a missing coroutine runner. No test count is inferred from a partial run. Its fixture teardown destroys scene roots; candidate cause only. Both current aborted runs remain baseline regressions. Earlier historical Combat PlayMode XML is14 pass/6 fail, not a fresh pass.

Runtime smoke used current SampleScene and public SessionFlow/WeaponController commands. It verified one bullet, tactical reload and two session starts. It did not simulate hardware input, test held-fire pause, certify ADS/empty reload, run ten sessions, or validate a standalone build. Historical Mac build existence is not current acceptance.

## Diagnostic caveats and cleanup

The first compile-request snippet had a dynamic namespace collision with Unity.CompilationPipeline; fully qualifying UnityEditor.Compilation fixed it. No project script was changed.

The first material-preview attempts emitted two “Releasing render texture that is set as Camera.targetTexture” diagnostics during cleanup. Final snippet clears targetTexture before release and reran successfully. Original and final diagnostics remain visible in final-console.json. The two other final warnings are stale Coplay test-job restoration and Assistant account timeout. Fresh PlayMode failure Console evidence is saved separately because Unity changed Console history across play sessions.

Raw imported-assets.txt labels `bakeXZ/bakeY` reflect the queried `keepOriginalPositionXZ/Y` properties; they are **not** proof of root baking. Root-motion conclusions here rely on actual absence of zombie clips and explicit hasRootCurves/hasMotionCurves, not those mislabeled diagnostic fields. Source metadata cannot verify retarget/foot-contact quality.

An initial preview log reversed screen left/right; image inspection corrected the index and retained command text: camera from +Z shows captain left, crew center, alien right. This does not affect the confirmed all-pink result.

Two temporary InitTestScene assets (and their metas) created by the P1 test attempts were identified as absent from initial-source-sha256.json and removed. A different pre-existing InitTestScene was preserved. AssetDatabase deletion through MCP was unavailable because that provider rejected an interaction-requiring operation; exact task-created files were cleaned through the filesystem patch tool. No missing S001Acceptance/CombatAcceptance file was restored or synthesized. Final source comparison is the verification.

All diagnostic code was executed transiently through MCP; saved .cs.txt files under Docs are reproducibility evidence, not compiled Unity code. No diagnostic MonoBehaviour or production gameplay script was added.
