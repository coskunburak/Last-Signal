# P0 — HDRP material integrity recovery

## P0 follow-up — active coverage and owner visual review

**Status: IMPLEMENTED / TEST_NOT_RUN for the new audit and review UI; P0_VISUAL_NOT_APPROVED.** The previously accepted nine-recipe technical repair is still intact. No repair, reimport, Unity compilation, test, screenshot or gameplay execution was repeated by Codex.

### Current independent findings

- Branch/HEAD unchanged: `s012-integrated-graybox-slice` / `3fa3161f0d3d22f40d422fd5b404605f25088ce0`.
- HDRP High is still assigned in GraphicsSettings and current QualitySettings; HDRP package remains 17.5.0.
- All 54 output hashes from the successful owner repair and protected plan-input hashes still match after this follow-up.
- Static scope: FirstTownCore, 19 explicitly named prefab roots (including repaired props and pistol presentations), and the runtime `Resources/S022/ZombieCatalog` asset. The resource zombie prefab is an explicit root as well. Scripts reviewed include TownAtmosphere, CharacterViewmodelPalette, WorldPopulationManager and ZombieLocalizedWounds. Actual PlayMode spawning/property-block behavior remains unverified.
- Static dependency graph: 590 relevant visited asset paths, 181 standalone material candidates, 106 model dependencies, 106 prefab dependencies. Of the 181 materials, 159 use HDRP/Lit; 22 use existing custom shader families with an HDRP tag in their source. These are source observations, not shader/material compatibility PASS.
- Two vendor zombie material originals appear in the raw dependency graph through the nested vendor prefab. The repaired project-owned prefab overrides them. A flat dependency list would incorrectly treat those originals as effective renderer assignments; the new owner audit resolves actual project prefab contents instead.
- The only unresolved non-built-in GUID found by the static graph is `852f9a79f7bf832408d4ed1eb088311a`, in `Art/S013/Production/Prefabs/Pine.prefab`'s `Animator.m_Avatar`. This is not a missing material. Its production effect is unverified and it was not changed.
- Of 508 vendor-scope legacy manifest records, 142 paths exist at Git HEAD and currently differ byte-for-byte from HEAD; these are restore candidates requiring provenance/reference review, not automatically verified originals. The other 366 have no Git HEAD file and are marked SOURCE_RECOVERY_REQUIRED until an authentic full source is found. This is not proof that recovery from vendor archives/backups is impossible. No vendor restoration was performed.

Full static data: `Evidence/active_material_static_audit.json`. Reproducible non-Unity tool: `Tools/hdrp-active-material-audit.py`. It follows the selected dependency graph and does not re-audit all unused vendor assets. It writes the reviewable owner-audit scope to `MaterialCoverageScope.json`; it does not overwrite the successful repair plan or receipt.

### Medicine source geometry

`Tools/hdrp-medicine-geometry-audit.py` parses only the Medicine binary FBX and writes `Evidence/medicine_geometry_source_audit.json` without altering any Unity asset.

- Model: PharmaSyringe2, FBX model ID 674947606.
- Connected geometry: Cube.008, FBX geometry ID 326580093.
- Expected Unity mesh local ID from the project prefab: 8586387579714453275.
- Source: 298 vertices, 391 polygons. Polygon sizes: 234 triangles, 150 quads, 2 pentagons, 2 heptagons, and one each with 6, 12 and 14 vertices.
- Source-index topology has zero boundary edges and zero edges shared by more than two polygons. This does not certify the imported triangulated result.
- Zero-based polygon 378 has vertex order [297, 287, 288, 292, 294]. Its projected non-adjacent edges 0 and 2 intersect. It is also nonplanar, with maximum plane deviation approximately 0.0022631 in source units.
- This is a concrete candidate for the owner's importer warning; it does not prove exactly which polygon Unity discarded or whether the resulting defect is visible. Source units are not asserted to equal world meters.
- UV/normal mapping modes, source bounds, channel counts and candidate coordinates are recorded in the JSON. The new Unity audit records imported Medicine mesh GUID/local ID, vertex/index counts, submesh topology, bounds, normal/UV attributes and readability without changing the importer.

**Decision:** no speculative welding, triangle removal, FBX modification or corrective mesh has been performed. Inspect the syringe tip, plunger/flange and rear/underside. If a visible hole exists, scope a project-owned derivative; if there is no relevant visible risk, obtain explicit owner acceptance before deferring the warning.

### Implemented owner tools

1. `Assets/LastSignal/Scripts/Editor/Rendering/HdrpActiveMaterialCoverageUtility.cs`:
   - Opens the production scene as a disposable preview scene and closes it without saving.
   - Loads declared/project-referenced prefabs with PrefabUtility, resolving nested overrides.
   - Records each material as GUID + local file ID, every effective renderer slot, hierarchy, renderer type, enabled/active state, LOD association and override flag.
   - Separately records terrain templates, tree/detail prefab prototypes and serialized runtime material inputs.
   - Discovers gameplay prefab fields in components/data assets; does not independently activate every nested vendor base prefab just because GetDependencies includes it.
   - Enumerates all material subassets of reachable models; keeps unreferenced import defaults separate from effective uses.
   - Inspects HDRP subshader tags, current-platform shader support/errors, map keywords/importers and remap-range validity. Legitimate zero metallic ranges are not automatically treated as defects.
   - Emits ACTIVE_HDRP_COMPATIBLE, ACTIVE_CUSTOM_COMPATIBLE, ACTIVE_NEEDS_REPAIR, ACTIVE_UNSUPPORTED, INACTIVE_DEFERRED or SOURCE_UNVERIFIED with explicit scope limits. Compatible is limited to inspected technical checks; it is not visual or original-source-channel acceptance.
   - Hashes reachable asset files/metas before and after audit to detect unintended persistence. Calls no Save, Import, material assignment or restoration API.
   - A completed audit always requires report review; missing/unsupported materials do not become accepted merely because evidence collection exited successfully.
2. `Assets/LastSignal/Scripts/Editor/Rendering/HdrpMaterialReviewWindow.cs`:
   - Menu: **Last Signal > Rendering > HDRP Integrity > 5. Owner Visual Review**.
   - Opens existing repaired prefabs; the syringe button selects PharmaSyringe2 and frames it in Scene view.
   - Does not generate a scene, change materials/transforms, capture screenshots, run gameplay or save assets.
3. `Tools/hdrp-migration.py`: adds only `audit-active-materials` for the new owner boundary. The existing repair is not repeated.

Technical API references: [OpenPreviewScene](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/scenemanagement/editorscenemanager/openpreviewscene), [EditorSceneManager 6000.5](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/scenemanagement/editorscenemanager), [Shader.FindSubshaderTagValue 6000.5](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/shader/findsubshadertagvalue). The installed project/package sources were also checked. New C# has not been compiled by Codex.

### Current owner boundary — do this instead of repeating the repair

**CURRENT PHASE:** P0-A visual evidence / P0-B Medicine geometry / P0-C active material coverage / P0-D vendor provenance.

**IMPLEMENTATION STATUS:** new audit and review UI implemented; Unity execution TEST_NOT_RUN. Prior repair remains SCOPED_TECHNICAL_PASS.

**VERIFIED EVIDENCE:** previous repair hashes; bounded static dependency/source recovery audit; Medicine binary topology report. These do not constitute visual approval.

**CHANGED FILES:** the two new Editor C# files and their .meta files, two new non-Unity Python audit tools, runner action, MaterialCoverageScope.json, this document, and the two new local evidence JSON files. No material, prefab, model, importer or production scene was modified in this follow-up.

**ISSUES RESOLVED:** implemented the missing model-subasset/effective-slot inspection path and a direct existing-prefab visual review workflow. Actual Unity results remain pending.

**OPEN ISSUES:** owner visual evidence for all five subject families; imported effect of the syringe polygon; active unresolved/unsupported material coverage; vendor source recovery; nonmaterial Pine avatar reference; runtime/production lighting and Windows-specific checks.

**NEXT OWNER ACTION / EXACT TERMINAL COMMANDS:** save work and close Unity normally, then run this one new read-only collection command:

```bash
cd "/Users/burakcoskun/Last Signal" &&
python3 Tools/hdrp-migration.py audit-active-materials
```

Do not run repair-material-integrity again. This new command compiles the added Editor code through the owner runner and audits authored references; it does not run PlayMode/EditMode suites or mutate material assignments.

**EXPECTED RESULT:** process exit 0 and `AUDIT_COMPLETE_REVIEW_REQUIRED` in the audit JSON. This means collection completed, not that all findings passed. A nonzero exit or AUDIT_FAILED stops the sequence for log analysis.

**EVIDENCE LOCATION:** `Evidence/<timestamp>-audit-active-materials/{unity.log,command.json,process.json}` plus `Evidence/active_material_coverage.json`. Provide the terminal evidence directory; Codex can read the local files.

**UNITY EDITOR INSTRUCTIONS:** after successful compilation/collection, open Unity normally, remain outside Play Mode and open **Last Signal > Rendering > HDRP Integrity > 5. Owner Visual Review**. No new scene needs to be authored. For each button, use Scene view in Shaded mode, 2D off and Scene Lighting off for the default inspection light. Keep this condition consistent and state it with the capture. F frames the selected subject again if necessary. Do not change/save prefab transforms or material properties.

First image set:
- Medicine complete-pack overview, then PharmaSyringe2 close-up from a front three-quarter angle and rear/underside. Inspect both tip and plunger/flange.
- Custom9mm three-quarter view including its magazine (the CustomMagazine material is on this presentation).
- DXT three-quarter view.
- Shirtless zombie front and rear with body and clothes visible.

Then provide space crew with helmet and one repaired prop (the panel's water flask is representative). Keep relevant renderer material slots visible in at least one capture when reporting a material-reference defect. The panel opens these exact existing assets:
- `Assets/LastSignal/Art/FirstTown/Prefabs/Medicine.prefab`
- `Assets/LastSignal/Art/TriplePistols/Custom9mm/Visual.prefab`
- `Assets/LastSignal/Art/TriplePistols/DXT/Visual.prefab`
- `Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab`
- `Assets/LastSignal/Prefabs/Combat/WorldBody_tt3d.prefab`
- `Assets/LastSignal/Prefabs/Items/drink.water.prefab`

Default inspection lighting is an initial geometry/material check, not representative FirstTownCore lighting acceptance. Actual first-person camera, skinning/animation, wetness and production lighting must still receive owner captures at their respective acceptance boundary. Every subject currently remains VISUAL_NOT_APPROVED.

**FAILURE CONDITIONS:** compiler errors, invalid scope/missing roots, failure to resolve materials or model subassets, changed dependency hashes, audit exceptions, missing review subject, visible geometry holes, pink/missing surfaces, incorrect metallic/AO/alpha/normal response. Return relevant logs/images; do not continue into P1 or forest production as though P0 passed.

**NEXT TASK AFTER PASS:** evaluate the resolved active-use report and images, repair only confirmed remaining defects, request the smallest focused retest, then close P0 only when the full acceptance criteria are met. P1 documentation/implementation begins at that gate; no P1 PASS or forest readiness is claimed here.

**Static checks performed:** Python AST syntax, existing scope paths (1 scene / 19 prefab roots / 1 data root), source geometry report invariants and hashes, prior repair preservation hashes, source whitespace, absence of Save/Import APIs in the new audit/review scripts, and runner --help registration. Unity compile/runtime behavior remains owner-verifiable.


2026-10-20, owner run at 22:33 Europe/Istanbul. **SCOPED_TECHNICAL_PASS / VISUAL_NOT_APPROVED. P0 remains open.**

Latest owner evidence: `Evidence/20261020-223341-087333-repair-material-integrity/process.json` reports exit code 0 / PASS; `Evidence/material_integrity_execution.json` reports TECHNICAL_CHECKS_PASS, 9 material recipes, 12 prefabs and 44 replaced renderer slots. Compilation produced no `error CS` diagnostics. This does not represent EditMode/PlayMode suite execution or overall P0 acceptance.

After Unity exited, Codex independently compared all 54 receipt output hashes and all protected plan-input hashes: zero mismatches. The owner-run utility verified packed channel samples, linear import settings, material/prefab GUIDs, 15 Medicine slots after FBX reimport, and the sixty preserved-material checks. Recovery snapshot: `Evidence/material-integrity-backups/20261020-193359-1121310/` (UTC directory name).

A source geometry warning remains: Medicine FBX mesh `PharmaSyringe2` has a self-intersecting polygon that Unity discarded during reimport. Source bytes remain unchanged. Inspect the syringe for visible holes during owner visual review; do not claim the geometry is accepted from the material check.

Next boundary: focused owner screenshots of Medicine (including the syringe), repaired first-person pistols and shirtless zombie. No repeat technical command is needed unless new changes or evidence justify it. Vendor restoration, skipped/embedded-material usage review and remaining visual checkpoints are still open.

The following implementation notes and first-run instructions document the original handoff state before that successful owner run.
No Unity compilation, tests, builds, scene generation, asset repair execution or visual acceptance was performed by Codex.

## Verified entry state

- Branch `s012-integrated-graybox-slice`; HEAD `3fa3161f0d3d22f40d422fd5b404605f25088ce0`.
- Entry status: 378 modified tracked files, 40 untracked entries/directories; 310 modified tracked `.mat` files, including 67 under ThirdParty. Existing work was preserved.
- `ProjectVersion.txt`: Unity 6000.5.0f1. `Packages/manifest.json`: HDRP 17.5.0.
- Graphics pipeline GUID `15722f09b35214c7cac8dee7a2a4c2e3` resolves to `LastSignal_HDRP_High.asset`. Current quality index 1 references the same asset; quality index 0 references HDRP Medium. HDRP global-settings reference exists. This is serialized configuration evidence, not a live rendering claim.
- `00_MASTER_INDEX.md` still says URP CURRENT; that statement is historical and superseded by the inspected settings, not grounds to restart migration.

## Evidence and confirmed findings

Read `material_migration_audit.json`, `material_migration_execution.json`, `MaterialRollbackManifest.json`, current materials, original Git HEAD materials, texture importer metadata and source PNG channels. The generated `Evidence/material_integrity_static_audit.json` records every historical source GUID, category, intended target, restore availability and relevant texture channel extrema.

Historical execution accounting is exact:

| Disposition | Count |
|---|---:|
| HDRP Lit | 731 |
| HDRP Unlit | 3 |
| Dual-pipeline particles | 15 |
| HDRP terrain | 1 |
| Custom dual-pipeline retained | 19 |
| Existing HDRP validated | 9 |
| Other/skipped | 188 |
| Errors | 0 |
| Total | 966 |

The old audit's 189 `otherShaderCount` includes the terrain material subsequently converted. Neither historical PASS means visual acceptance. `FindAssets` followed by `LoadAssetAtPath<Material>` counted one material per asset GUID, not every material subasset in a model. Two hundred historical records have non-`.mat` asset paths. They require embedded-material accounting, not an assumption that 966 is the full material-object population.

Primary source-category counts (mutually exclusive; ownership/embedded/surface precedence): A=10, B=44, C=3, D=1, E=142, F=24, G=507, H=200, I=35. F includes two AO-only Arberry materials; all three AO-only cases were inspected. These categories are accounting, not acceptance. Unsupported source/workflow and historical skip status remain explicit in each record.

**Already corrected locally:** All 46 inspected project-owned source metallic-map cases with a source metallic scalar of zero have `_MetallicRemapMax=1`. The three remaining zero-max project materials with a Mask Map are Arberry `Body_Arkit_Eye`, `Brows_Leashes`, and `Hair3`; their metallic response is intentionally zero. Their AO PNGs contain the intended green channel, and constant smoothness remaps preserve scalar smoothness. Nine CharacterIntegration packed maps plus one project zombie packed map already have valid R/G/A layout and AO remaps. No blanket remap or repacking was applied.

**Confirmed remaining scope:**

| Source | Correction implemented for owner execution |
|---|---|
| TriplePistols Custom9mm, CustomMagazine, DXT | Pack source metallic R + separate AO G + neutral detail B + existing smoothness A; restore AO remap |
| MainMaterialFree | Project-owned derivative; replace exact source GUID/local-ID references in nine item/shelter prefabs |
| space_crew_man and helmet | Two project-owned derivatives; scoped renderer slots in WorldBody_tt3d |
| ShirtlessZombie Body and Clothes | Two project-owned derivatives with packed AO and metallic remaps; nested overrides in LS_Zombie_Shirtless_Visual |
| Medicine | Project-owned HDRP Lit + packed metallic R / neutral AO G / neutral detail B / 1−roughness A; replace 15 slots in Medicine.prefab |

Medicine FBX GUID: `9e26e8933fa6f45cfb24021bc01d97a0`; material local ID: `2372058336338979677`. Its ModelImporter has an empty externalObjects map. The implementation uses supported project-prefab renderer overrides and leaves the vendor importer unchanged. All 15 slots refer to the same embedded source. The historical audit identifies that material as opaque/non-clipped; the derivative preserves that surface decision. Separate vendor alpha and displacement maps are not newly enabled. Visual review must decide whether further authored alpha/displacement work is needed.

Medicine's vendor metallic/roughness importers currently say sRGB. Packing intentionally reads the named grayscale data PNG bytes into linear CPU textures and leaves those vendor settings untouched. Other packed inputs are required to have linear importers. Generated PNGs are linear, uncompressed, mipmapped, preserve alpha and use stable paths/GUIDs. The output cap is 2048 (1024 for Medicine); the 4096 zombie body source is sampled down to 2048, matching its source importer cap. No claim of pixel-identical Unity compression/downsampling or Windows performance is made.

The installed HDRP `Editor/AssetProcessors/MaterialPostProcessor.cs` contains `MetallicRemapping`, which copies `_Metallic` into `_MetallicRemapMax` during its versioned upgrade. This confirms the overwrite mechanism; it does not prove the historical timing of every earlier zero value. The shared converter now sets both consistently for mapped metallic inputs, respects source keywords, combines separate AO, preserves scalar/alpha smoothness semantics, and rejects unsupported workflows. It no longer enables emission merely because a disabled source emission color is nonzero.

## Implementation and preservation

- `Assets/LastSignal/Scripts/Editor/Rendering/HdrpMaterialIntegrityUtility.cs`: dry-run, preflight, scoped repair, snapshots, nested prefab overrides, checks after import, repeat-run validation, guarded rollback.
- `Assets/LastSignal/Scripts/Editor/Rendering/HdrpMaterialMaskUtility.cs`: shared channel packing and remap logic.
- `Assets/LastSignal/Scripts/Editor/Rendering/HdrpMaterialMigrationUtility.cs`: authoritative conversion uses the shared logic, captures additional source keyword/workflow properties, excludes protected vendor/embedded sources, and refuses replay of broad migration when its historical manifest exists. Historical manifest and execution evidence were not rewritten.
- `Tools/hdrp-migration.py`: four focused integrity actions added.
- `Tools/hdrp-material-integrity-audit.py`: non-Unity source-evidence/plan generator; requires Pillow; changes no Unity assets. Refuses regeneration once an execution receipt exists.
- `MaterialIntegrityRepairPlan.json`: reviewable fixed scope with source GUID/local IDs, Git/property-record provenance, source hashes, target paths and sixty preservation checks. Kept outside ignored Evidence so it can be versioned.

Expected asset changes on successful owner execution: **3 existing materials, 6 new material derivatives, 6 new packed maps, targeted slots in 12 project prefabs**. Applied count at the original handoff was **0**. The subsequent owner run applied all nine recipes; see the latest evidence above.

Every source input is hash-guarded. On repair, all existing target files and `.meta` files are copied before mutation to `Evidence/material-integrity-backups/<UTC timestamp>/Assets/...`; a plan copy and receipt accompany them. Vendor materials/textures/importers are not modified by the implementation. Their protected hashes are checked again after reimport. Existing target GUIDs and all prefab GUIDs are checked after reimport. Unrelated dirty assets are not globally saved.

Subsequent successful repair calls verify the same approved outputs and perform no writes. Modified inputs or outputs stop execution for review. A failed run preserves a FAIL receipt and snapshot; it is not automatically retried over partial results. A crash before a final receipt may require manual snapshot recovery.

Optional owner-only rollback action: `python3 Tools/hdrp-migration.py rollback-material-integrity`. Use only after inspecting the receipt. It refuses changed outputs/corrupt snapshots, restores existing affected files, and retains new derivatives/maps and `.meta` files for recovery. It never deletes assets or restores vendor data. Original bytes are checked after reimport. A rolled-back receipt intentionally requires review before a new repair cycle.

## First owner test boundary

**CURRENT PHASE:** P0 material integrity, first focused repair/verification.

**IMPLEMENTED CHANGES (original handoff):** Nine recipes above; channel packing, prefab overrides, original-converter correction, snapshots and validation. The owner has now completed this first-run command successfully; see the latest evidence above.

**MODIFIED FILES:** The source/plan paths listed above, this document, and two new C# `.meta` files. The static audit JSON is additional local evidence. Existing material/prefab assets were not changed by Codex.

**EXACT OWNER COMMAND:** Save your work and close the Unity Editor normally. From the project root run only:

```bash
python3 Tools/hdrp-migration.py repair-material-integrity
```

This compiles through the normal owner-operated runner, performs read-only preflight, snapshots, applies the bounded repair, reimports generated/project materials and the Medicine FBX, checks pixel samples/remaps/GUIDs/renderer slots/source hashes, and returns. It does not run EditMode/PlayMode suites, builds, navigation or gameplay. If you want a no-write preview first, `audit-material-integrity` is available; it is not required before the command above because the same preflight is included.

**EXPECTED RESULT:** Exit code 0, timestamped process receipt PASS, `material_integrity_execution.json` status `TECHNICAL_CHECKS_PASS`, no missing slots, all 15 Medicine slots on the external project derivative, all six referenced derivatives present, sixty preserved materials unchanged. `VISUAL_NOT_APPROVED` remains. This is not P0 acceptance.

**EVIDENCE OUTPUT:**

- `Docs/Implementation/VisualRealismMigration/Evidence/<timestamp>-repair-material-integrity/unity.log`
- Same directory: `command.json`, `process.json`
- `Docs/Implementation/VisualRealismMigration/Evidence/material_integrity_execution.json`
- Receipt's `snapshot` directory: original bytes, plan, before/after hashes and receipt.

**FAILURE CONDITIONS:** Compile error, changed guarded source, missing GUID/local-ID/texture, unexpected source shader, missing material slot, prefab-save failure, channel/remap mismatch, reference loss after FBX reimport, source mutation, or nonzero exit. Stop and return the log/process receipt and execution receipt if one exists. Preflight failures can occur before an execution receipt is created. Do not run broad migrate/rollback commands or continue to later phases.

**NEXT ACTION AFTER TECHNICAL PASS:** Return those receipts. Then capture focused owner screenshots of Medicine, three pistols, space crew, item props, and the shirtless zombie (including first-person weapon distance), followed by the P1 checkpoint list. If needed, use the separate `verify-material-integrity` action for read-only repeat verification. Broader gameplay regression is requested only if evidence indicates a relevant risk.

## Remaining P0 blockers

- Owner compile/repair/reimport evidence is now present for this scope; visual approval remains absent.
- Historical vendor mutation is accounted for, not restored: 508 vendor-scope rollback entries are incomplete property records, not full backups. Space-crew materials lack Git HEAD originals. Restoring any vendor source needs a verified original plus a complete reference analysis. No bulk vendor restoration was attempted.
- The 188 historical skipped entries, additional model subassets (including nested zombie models outside this scoped case), and production-scene overrides still need usage-specific review. The scoped verification does not certify every scene or imported asset.
- Pistol and Medicine visual appearance, alpha decisions, wetness, wounds, scopes and physical-tree rendering remain subject to owner evidence. No forest production or scene promotion is authorized by a technical repair receipt alone.

## Static checks by Codex

Python source syntax, plan JSON, all guarded hashes, recipe uniqueness, ownership/path boundaries, 966-entry accounting and original/source-channel evidence were checked without Unity. C# source was inspected against installed package APIs; it had **not been compiled at the original handoff**. The subsequent owner command completed compilation and scoped actual-asset verification successfully, as recorded above.

Repository-wide whitespace checking also reported pre-existing Unity YAML/document whitespace in the inherited migration changes; those files were left intact. Scoped edited-source whitespace checks passed.
