# Owner-run gate review — 2026-10-21

> Current instruction (2026-10-22): Unity runs are owner-operated. See [CURRENT_HANDOFF.md](CURRENT_HANDOFF.md) for the next exact commands and latest reviewed results. Earlier next-step and authorization statements below are historical.


Latest gate: owner shader validation **18/18 PASS, zero errors**. The previous shader command below is completed history. Current next command: `python3 Tools/HDRPProductionOptimization/manual_checks.py prepare-mask-candidates`. See [MASK_CANDIDATES_HANDOFF.md](MASK_CANDIDATES_HANDOFF.md) for exact scope and safeguards.

Compilation PASS: process exit 0, no blocking compilation/package diagnostic in the supplied log. Six focused audit tests PASS: 6 passed, 0 failed/skipped, XML inspected. Production audit completed with no collection errors. Evidence IDs and SHA-256 hashes are in Evidence/owner-gate-review.json. These results apply to the source the owner tested, not future edits.

The audit resolves 47 roots, 13,166 renderer slots and 267 material objects. Default High, both effective quality pipelines and registered HDRP global settings are valid. No Unity process was started by the agent.

| Finding | Count | Decision |
|---|---:|---|
| Active HDRP compatible | 108 | Technical inspection only; visual acceptance outstanding |
| Active custom compatible | 23 | Custom shader gate and visual parity still required |
| Active needs repair: mask input | 43 | Original Git metallic-workflow provenance resolved; pack correct project-owned linear masks |
| Active needs repair: normal input | 2 | Embedded machete/axe materials; own copies and project prefab overrides required |
| Active unsupported classification | 1 | Built-in GUI/Text font material; inspect disabled label renderer usage, not proof of a pink gameplay surface |
| Source unverified | 17 | Embedded/runtime source uses need individual review |
| Inactive deferred | 73 | Not globally unused; no deletion authorized by this classification |
| Missing slots | 3 | Disabled blood-particle root renderers; inspect runtime enable/assignment before changing |
| Serialized URP light components | 42 | FirstTownCore; preserve until verified replacement authoring |
| Serialized URP camera components | 4 | Player and three FPS pistol prefabs; preserve until camera parity verified |

The old 100 normal-space-keyword warnings no longer appear. Because this audit has broader scope and additional valid normal-import checks, do not describe the new material count as a direct subtraction from the old report.

## Prepared repair inputs

MaterialRepairCandidates.json contains 45 candidates and 170 source/material/meta SHA-256 guards. The read-only material_repair_plan.py reproduces it from the owner report and current HEAD. All 43 mask cases have the original URP Lit metallic workflow, metallic-map keyword, smoothness from map alpha, no AO-map keyword, and the same texture GUID as the current mask input. Original scalar smoothness and source bytes are recorded. Material filenames alone were not used to infer channel semantics.

Inputs comprise 26 PNG, 14 TIFF and 3 TGA material cases (shared textures exist). The current HdrpMaterialMaskUtility only decodes verified PNG and rejects sRGB data inputs. Do not bypass this guard or use a global converter. A scoped packing extension must safely decode the additional formats, explicitly resolve raw-data intent versus previous sRGB sampling, preserve alpha/UV/detail behavior, validate packed R/G/B/A values, and write only project-owned assets. Keep original vendor importers unchanged. This candidate inventory is intentionally not executable conversion input.

The two active normal failures are embedded materials in machete_4k.blend and wooden_axe_4k.blend, used by Diesel_WorldMachete and Diesel_WorldWoodenAxe project prefabs. WeaponGripAuthoring creates those prefabs; repairs must survive that authoring path. The fir source normal warnings occur outside ACTIVE_NEEDS_REPAIR and are not evidence of a broken authored forest hero.

## Next owner command

Save and close Unity, then:

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/manual_checks.py validate-shaders
```

Return its timestamped process.json/unity.log and the shader report in Docs/Implementation/VisualRealismMigration/Evidence. No repeat of the six passing tests is needed without changes to their affected code. Shader validation is an explicit user-operated import/compile operation; it does not establish visual acceptance.

URP remains installed. No Unity assets, scene/prefab data, settings, Git/LFS objects, ignored vendor files, forest generation or S004 simulation were changed during this review. Only the read-only plan helper, candidate inventory, evidence review and documentation were added. M3 Pro Development and URP removal remain downstream of the scoped material/shader/source fixes and required user acceptance.
