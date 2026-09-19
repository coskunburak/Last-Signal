# S004-P2 entry evidence

2026-09-18, approximately 07:34–07:35 UTC. **BLOCKED**, no gameplay implementation. [Entry report](../../S004_P2_ENTRY_REPORT.md).

| File | Provenance and limitation |
|---|---|
| unity-entry-audit.json | Fresh Unity Assistant command and logs: project identity, actual imported clip counts/durations, absent enabled build scenes. |
| unity-scene-asset-audit.json | Fresh timestamped Unity query: active scene, roots, nav data/agent defaults, layers, character Avatars/scale/materials, project AnimationClip search and pipeline. |
| editmode-results.xml | Fresh existing LastSignal.EditModeTests: 34 total, 33 passed, 1 failed. |
| console.json | Closing Console snapshot; retained diagnostics included, not a clean-Console claim. |
| source-revalidation.json | Current source compared to P1 initial snapshot: 336 files, zero changes/additions/removals. |
| initial-source-sha256.json | Entry snapshot for final preservation comparison. |
| final-source-comparison.json | Post-check source preservation result. |
| git-status.txt | Existing dirty working-tree identity, not a clean release. |
| scene-history.txt | Local git log --all exact missing-scene paths: no history found. No restoration inferred. |
| scene-references.txt | Current build, authoring and test references to missing scenes. |
| evidence-sha256.json | Hash manifest for this package and both P2 reports, excluding itself. |

EditMode procedure: existing TestRunnerApi with assembly LastSignal.EditModeTests; SessionState LastSignal.RegressionPath pointed to this folder so the existing CombatRegressionRunner callback wrote fresh XML without replacing earlier evidence.

PlayMode was not rerun after hard blockers were confirmed. Preserve [P1 full abort](../20260918-P1/playmode-abort-console.json) and [isolated abort](../20260918-P1/playmode-stable-abort-console.json) as unresolved baseline regressions. No new PlayMode XML exists here. Source equality does not certify current runner stability.

[P1 runtime smoke](../20260918-P1/runtime-baseline.txt) and [material render](../20260918-P1/vendor-materials-urp.png) remain historical evidence. Current source/asset equality supports an unchanged content finding, not new runtime or visual acceptance. No production zombie preview, profiling, arena, bake, scene regeneration or animation synthesis was performed.
