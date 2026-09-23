# S004-B0B evidence index

Run: 20260918-B0B, Unity6000.5.0f1, actual Last Signal project. Technical integration PASS. Local entitlement evidence pending. No AI/combat/health gameplay.

- `imported-before.txt`: actual AssetDatabase paths, importer JSON, hierarchy and original clip settings before changes.
- `source-before.json`, `source-safety.json`: SHA256 preservation audit; only selected model importer and two old test-path constants change existing files.
- `avatar.txt`: initial valid mapping/anatomy audit (its immediate-import baked bounds are transient; final settled measurements are authoritative).
- `model-importer-final.json`, `model-performance-audit.txt`: final scale/rig/mesh/material/texture/performance metrics.
- `source-skeleton-compatibility.txt`: all six animation FBXs match52/52 mapped paths of their own Kevin source Avatar.
- `animation-inventory.tsv`, `animator-mapping.tsv`: exact names, source paths, GUID/file IDs, loop/root/event settings and selected states.
- `texture-channel-verification.txt`, `source-texture-contact-sheet.png`, `material-final.json`: map identification, byte-perfect channel comparisons and final URP setup.
- `motion-samples.csv`, `motion-summary.txt`:121 samples per candidate/adapted clip, root probes and target-rig bounds/anchors.
- `death-adapter-provenance.txt`:129 original curves unchanged, only RootT.y differs.
- `01_zombie_model_front.png` through `08_material_lighting.png`: eight requested actual model renders. `09_reference_pose.png`: reference skeleton pose. `death_side.png`: corrected finish.
- `*_contact-sheet.jpg`, `frames/`: seven times × three views for all eight candidates/adapters; raw death failures intentionally retained for comparison.
- `retarget-continuous-three-views.mp4`, `continuous-preview-ledger.txt`: actual production Animator evaluated at24fps; front/side/rear; two Idle/Walk loops, Attack, HitReact and Death hold. Deterministic Editor playback, not standalone gameplay.
- `editmode-initial.xml`:32/34 before synchronizing two stale vendor-path assertions; exact failures retained.
- `editmode-results.xml`:38/38 PASS,0 skipped; existing34 plus4 asset checks.
- `playmode-initial-interruption.txt`: focus-paused initial run without completion; NOT counted as PASS.
- `playmode-results.xml`:23/23 PASS,0 skipped; existing21 including three-session smoke plus2 production-controller checks.
- `console-final.json`:0 errors; one Unity AI account-service timeout warning, separately classified.
- `license-source-record.md`: public source links and local evidence limitation.

Reproduce assets using `ZombieAssetIntegration.Build()`, then `CreateScene()`. Existing project Death adapter is retained on rebuild. If the source rig/clip changes, rerun `AuditMotion()` (includes the raw source), `AdaptDeath()`, then `AuditMotion()` and all visual/tests; never reuse the old correction without revalidation. Preview: Unity menu Last Signal → Zombie Assets → Animation preview; open acceptance scene, select motion, Play/Pause/scrub. The tool is Editor-only.

Regression: `CombatRegressionRunner.Run(false/true)` uses full existing assemblies; set `SessionState[LastSignal.RegressionPath]` to this run's target XML immediately afterward. Keep Unity/GameView focused through PlayMode, because intentional application-focus handling pauses the real session. Do not force simulation clocks during a test.
