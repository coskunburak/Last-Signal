# S004-P3 evidence index

Real Unity Editor / production prefab evidence, 2026-09-19. Status/counts: [implementation report](../../S004_P3_IMPLEMENTATION_REPORT.md).

| Evidence | Purpose |
|---|---|
| entry-git-status.txt / entry-git-diff-stat.txt | Pre-change dirty worktree record |
| entry-source-hashes.json | Pre-change C# identities |
| architecture-plan.md | Minimal extension plan based on existing source |
| baseline-editmode.xml / baseline-playmode.xml | Fresh pre-P3 full baseline: 52 and 35 passed |
| clip-audit/README.md / samples.csv / clip.txt | Exact clip, rig sample positions and timing decision |
| clip-audit/baked-*.png | Accepted measured key poses |
| clip-audit/frame-*.png | Rejected stale-skin render attempt retained for transparency |
| health-editmode.xml | Seven focused health cases |
| combat-gates-editmode.xml | 59 then-existing EditMode cases after core integration |
| focused-playmode-01.xml | First run: 6/7, real normal-route range failure |
| focused-playmode-02.xml | Corrected and expanded 11/11 focused cases |
| pre-lifecycle-audit-playmode.xml | First full post-P3 pass, 46/46 |
| focused-playmode-final.xml | Final focused lifecycle/rate audit |
| final-editmode.xml / final-playmode.xml | Final complete suites; use these for closure |
| contact-rate-audit.txt | 30/60/120 Hz controller/actual Animator contact timing |
| visual/*-fps.png | Actual production first-person view of tested scenarios |
| visual/*-side.png | External camera; gray player capsule is a QA-only visual aid |
| visual/trace.txt | Real state, sequence, timer, validation, distance, angle and health at capture |
| combat-tuning.yaml | Serialized production tuning snapshot |
| performance.txt | Warm 1/10-actor active combat, CPU markers and direct managed allocation |
| fix-ledger.md | Defects discovered and actual corrective changes |
| changed-source-files.txt | Existing C# files changed by P3, based on entry hashes |

Screenshots are taken inside actual PlayMode tests, not generated illustrations. Side-view capsule has its collider removed and is destroyed immediately after rendering; it never participates in damage queries. Existing first-person weapon meshes can appear in external views. The first rejected audit images are explicitly excluded from visual acceptance.

Trace appends across completed runs; image filenames contain the latest rerun of each scenario. XML files retain prior outcomes, including the initial failure. Existing P2 full-suite tests also write their own visual/performance evidence under the original P2 evidence directory; the P3 XML records their actual execution.

No standalone or Windows certification is implied by Editor results. Deferred P4 gameplay and outstanding balance/platform risks remain in the report.
