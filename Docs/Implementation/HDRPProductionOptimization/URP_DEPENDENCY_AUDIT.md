# URP dependency audit

## Latest checkpoint — Windows working-copy reduction, 2026-10-22 22:14 Europe/Istanbul

Owner explicitly requested removal of unused asset packs and transfer size reduction, then selected **manifest only** (no destination copy). Reviewed owner runs `20261022T190249895402Z-validate-shaders`, `20261022T190315113510Z-production-audit`, `20261022T190410078824Z-full-edit`: **21 shaders / 0 errors, 910/910 tests**, 47 production roots / 269 materials / 13,166 slots, no new findings or audit errors. The three disabled blood emitter empty slots and GUI/Text exception are unchanged. Frozen per-run reports and `Evidence/post-freeblood-verification-20261022.json` preserve results. All 216 integration guards and 28 archived asset/meta pairs verified after Unity exit.

### Executed reduction

With Unity closed, moved five proven unreferenced packs (kenney_impact-sounds, VisceralBulletImpacts, barn-village-abandoned-house, old Mountain distribution, ZombieMotionClips) and seven additional unused `.unitypackage` installers out of the active project. **756 files / 1,414,234,064 bytes (1.414 decimal GB)**, preserving metas, licensing and all originals. Archive: `/Users/burakcoskun/LastSignal-Recovery/UnusedPackages-20261022T191127Z`. Every file hash was verified before and after the move. No permanent deletion; no claim of equivalent free disk space. Checkpoint/Git/LFS unchanged, 216 protected hashes remain exact. Receipt: `Evidence/unused-package-retirement-20261022.json`.

The bounded package assessment used incoming GUID references, source/config path/name references, Resources and script exclusions. Final selected file GUIDs were searched across all Assets/Packages/ProjectSettings bytes including ignored/binary files, with zero external consumers. Packages with references, automatic Resources inclusion or scripts remain. In particular, the separate HolotnaMountainURP pack is used by S013 comparison/authoring and was NOT confused with the retired unreferenced Mountain distribution. This is not a claim that every remaining asset is used at runtime.

### Transfer

`transfer_manifest.py --profile windows-work` includes all remaining Assets (ignored assets and every meta), Packages, ProjectSettings, Tools, root source helpers, documentation Markdown and current top-level HDRP evidence JSON. It excludes Git/LFS, historical binary evidence/rollback archives, Library, build outputs and local caches from the transfer, preserving them on the Mac. This is a working project copy, not a repository/history/recovery backup; historical authoring/recovery operations may require the separate complete archive. `complete --include-git` remains available unchanged in scope. Windows-name/case validation and optional copy-to-new-directory with per-file destination hash checks were added. No filesystem deletion or mirror operation is implemented.

Manifest: `Evidence/windows-work-manifest.json`; source verification: `Evidence/windows-work-verification.txt`. Create the final manifest after these documents; inspect its exact file count/bytes. No disk audit restart, Unity/tests/build, Git maintenance or Windows copy performed by the agent. The old 107,016-file transfer verification describes the earlier complete snapshot only.

URP package dependencies remain absent. Remaining used Holotna comparison shader graphs and previously documented legacy vendor/recovery references are unresolved compatibility work; this reduction does not declare project-wide URP-reference zero or production completion. Owner import/validation after the final retirement, post-removal PlayMode/build, development-profile activation/benchmark and Windows visual acceptance remain pending. Do not run consumed migration plans.

Next owner validation, Unity saved and closed:

```bash
python3 Tools/HDRPProductionOptimization/manual_checks.py full-edit &&
python3 Tools/HDRPProductionOptimization/manual_checks.py production-audit &&
LASTSIGNAL_S004_STAGE='Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019163434-8e0f05b7.unity' python3 Tools/HDRPProductionOptimization/manual_checks.py full-play
```

If Unity changes any source files, regenerate the transfer manifest before copying. Windows verification must run before opening Unity. See WINDOWS_TRANSFER.md for copy commands.

## Previous record — historical


## Latest checkpoint — Free Blood shader import failure addressed, 2026-10-22 18:29 UTC

Owner `20261022T182114775083Z-validate-shaders` FAILED: 1,020 shader errors across six Free Blood URP-only shaders (missing removed Universal includes). The required 20 production shaders themselves passed. The runner correctly rejected the global shader errors; no filters or success criteria were relaxed. `production-audit` and `full-edit` did not run because the shell chain stopped. Frozen results: `Evidence/20261022T182114775083Z-validate-shaders/review.json` and `reviewed-shader-validation.json`.

Applied a bounded, reversible integration with Unity closed; no agent-run Unity/tests:

- Blood shader keeps its original GUID/name/entire Properties block and importer meta/default textures. Its implementation now uses the existing validated `LastSignal/VFX/Blood/ParticleSplat/FORWARDONLY` HDRP pass. All 16 vendor blood materials, VFX prefabs, source textures/models and authoring paths remain unchanged. The original full ASE shader and meta are backed up. The validator now explicitly includes the preserved vendor shader name (21 required shaders), and rejects zero-pass/subshader results.
- The other five shaders have no project/production/preload reachability in the existing vendor audit. Their complete incoming-reference closure is **28 assets: five shaders, 18 materials, three prefabs and two demo scenes**, all within this vendor's Demo/Extra content. A GUID search across all Assets/ProjectSettings/Packages bytes, including ignored/binary files, found no external consumers. Source/config name searches found only two same-stem FBX rollback entries; those FBX files remain imported. No runtime loader or build-scene consumer was found. The unused URP demo closure is retired outside Assets with every meta/GUID preserved; it is not an HDRP demo conversion. Shared meshes/textures, licensing/package source and other vendor content remain. No whole package deletion.
- All 28 original/retired asset+meta hashes verified, no archived GUID references remain in imported serialized source, and 216 protected hashes match (S004, ForestPrototype120, tree physics, package/graphics/quality settings, blood materials/prefabs/metas and native shader inputs). Git/LFS/checkpoint untouched. URP remains removed.

Receipt and original/retired files: `Evidence/freeblood-hdrp-integration-20261022T182846597988Z/integration.json`. This is **STATIC_INTEGRATION_COMPLETE_OWNER_SHADER_AUDIT_PENDING**, not verified rendering or production completion. The native blood pass predates this change; visual acceptance for the preserved vendor materials is still pending. Five Holotna comparison materials and previously documented vendor/recovery variants remain separate unresolved scope. Development activation/benchmark, post-removal PlayMode/build/visual gates remain open.

Next owner gate (save and close Unity normally):

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/manual_checks.py validate-shaders &&
python3 Tools/HDRPProductionOptimization/manual_checks.py production-audit &&
python3 Tools/HDRPProductionOptimization/manual_checks.py full-edit
```

Review the first available result if the chain stops. After the owner run, recheck integration receipt after-hash, metas, retired file integrity and protected hashes for importer side effects. Do not rerun consumed migration plans. Do not restore the retired demo assets into an HDRP-only project without porting their original URP shaders; original asset/meta pairs are retained solely for deliberate recovery/source access.

## Earlier checkpoints — historical


## Latest checkpoint — post-removal compile verified, 2026-10-22 18:19 UTC

Owner `20261022T181833283654Z-compile` completed successfully. C# compiler has no errors. The early log prints the old package set, then resolves the new set and removes obsolete Universal assembly artifacts. Final disk verification: no Universal/universal-config entries in manifest or lock, no Universal PackageCache directories, no Universal ScriptAssemblies, no URP GraphicsSettings registration and no temporary project URP global asset. All three removal after-hashes match. Explicit S004/ForestPrototype120/TreePhysicsBody/QualitySettings guards and all 36 vendor material/meta after-hashes remain exact. URP has actually been resolved out by Unity, not merely edited out of the manifest.

Review: `Evidence/urp-post-removal-compile-review-20261022.json`. Log includes licensing, importer/Blender memory, curl/usbmux diagnostics and two Realistic Tree graph newer-node warnings; these are not C# compiler failures. Shader/build/visual acceptance remains separate. Five Holotna comparison materials and two vendor Resources shaders remain known compatibility concerns. Do not claim full production completion or remove preserved vendor files.

Next owner gate (Unity saved and closed):

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/manual_checks.py validate-shaders &&
python3 Tools/HDRPProductionOptimization/manual_checks.py production-audit &&
python3 Tools/HDRPProductionOptimization/manual_checks.py full-edit
```

The package set changed, so prior shader/test passes are pre-removal evidence; this rerun is justified by the new package state. Review each available report even if a later command fails. The shader validator covers the 20 required production shaders, not every vendor shader or build variant. No agent-run Unity/tests. Do not rerun any consumed migration or old vendor plan. Development activation/benchmark and post-removal PlayMode/build/visual gates remain pending.

## Previous checkpoints (historical)

## Latest state — URP package declarations removed, 2026-10-22 18:17 UTC

Owner explicitly requested “kaldır artık”, then “kaldığın yerden devam et”. Applied reversible package removal now rather than another prerequisite audit. Removed com.unity.render-pipelines.universal from manifest and universal/universal-config entries from lock; no other package owned those dependencies. HDRP/Core/ShaderGraph and all other dependency entries remain. Removed only the URP global registration from GraphicsSettings and archived the temporary project URP global asset with its meta. Originals of all five files are preserved. Receipt: `Evidence/urp-package-removal-20261022T181748409752Z/removal.json`.

Status is **PACKAGE_DECLARATIONS_REMOVED_OWNER_UNITY_RESOLUTION_PENDING**, not full production completion. No agent-run Unity/tests/build. Library/PackageCache was not deleted; Unity will reconcile its imported package state on the owner's next launch. ForestPrototype120, explicit S004 scene, TreePhysicsBody and QualitySettings hashes match pre-change guards. No vendor assets, checkpoint, Git/LFS or ignored source files deleted.

Owner metadata cleanup `20261022T181334519440Z-cleanup-vendor-hdrp-metadata`: all 36 materials verified, zero post-exit changed inputs. Independently verified 36 after/meta pairs and 160 guards. Do not rerun consumed plans.

Known unresolved rendering/build scope: five comparison-only Holotna materials still use URP shader graphs; two vendor Free Blood shaders in Resources retain URP includes; other preserved vendor/demo/recovery variants retain URP serialized dependencies. These are NOT silently marked compatible, archived or converted. Package removal alone does not satisfy the final rendering/build gate. Next inspect actual post-removal compiler/import/shader evidence and resolve those dependencies while preserving source assets. Development activation/benchmark and visual/Windows/performance acceptance remain open.

Next owner command (Unity saved and closed):

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/manual_checks.py compile
```

Return the evidence directory even on failure. Do not run any previous cleanup/audit plan against the changed package state. Review package-resolution and compiler/shader diagnostics, current manifest/lock and GraphicsSettings after process exit before further validation.

## Previous checkpoints — historical, not current commands

## Latest checkpoint — 2026-10-22 18:08 UTC vendor closure reviewed

Owner `20261022T180815959017Z-audit-vendor-closure` completed with no errors and **zero post-exit changed inputs**. Independently rechecked all 111 asset/meta pairs and ten protected inputs. The restored project global setting prevented the previous vendor auto-upgrade side effect. Report: `Evidence/vendor-closure-20261022T1808368506460Z-96672a4fc96e451b96e1f88ee07f226b/audit.json`; review: `Evidence/vendor-closure-review-20261022.json`.

Imported dependency inventory: 2,377 project assets, 13 Resources assets, two enabled build scenes. Among the 111 planned consumers: 101 have no reachability in the checked roots (NOT disposal approval), five are comparison-only project dependencies, one is a production dependency and four are Resources inputs. The three used materials (Factory Tools BaseColor and ShirtlessZombie Body/Clothes) already have native HDRP shader contracts; their remaining dependency is obsolete URP editor version data. Five Holotna ShaderGraph materials are still URP-only and used by S013AssetComparison. Two Free Blood demo shaders are automatically included through Resources; their absence from enabled scenes does not make them safe to ignore. Source-only materials, shared textures/models, dynamic loads and native alternatives remain preservation/disposition concerns.

### Next owner action — 36 HDRP vendor metadata records

Prepared `cleanup-vendor-hdrp-metadata`. Reuses the verified project metadata-cleanup engine with a separate hash-pinned plan (`Evidence/vendor-hdrp-metadata-plan.json`, 36 items) and strict vendor HDRP/count preflight. Integration requirement: these already-HDRP materials retain Universal AssetVersion subassets that would become unresolved on package removal, including the three used production/Resources materials. No shader/material conversion is needed for this group.

All 36 originals and metas are backed up before mutation. Only the single obsolete Universal editor version object per file is detached and destroyed; surviving native objects' serialized data and persistent IDs are compared, preserving shader/texture/tuning values and meta GUIDs. HDRP version objects remain. 160 non-target/protected hashes must remain exact. The full batch is rechecked after all imports. The owner runner then independently verifies receipt after-hashes, metas and protected inputs after the Editor exits. Failure attempts rollback of touched originals. No vendor file deletion, scene/prefab/physics/package change or broad reimport is requested. This is an explicit HDRP integration cleanup, not permission to modify other vendor content.

Save and close Unity normally, then run only:

Owner completed this batch at 18:13 UTC; do not rerun. See the latest checkpoint above.

Review `vendor-hdrp-metadata-*/cleanup.json` and `postExitChangedInputs`. No repeat of the original 241-item cleanup, completed audit, full regressions or visual operations. The old vendor audit plan becomes stale after this mutation; do not rerun it unchanged. Agent checked Python syntax, source diff whitespace and current plan hashes; no Unity, compile or tests were run by the agent.

Remaining gates after metadata: native handling of five Holotna comparison materials, disposition of demo Resources shader dependencies and remaining vendor/recovery variants (with incoming-reference/source preservation checks), then atomic URP package/global-setting removal with post-exit verification. The temporary URP global setting stays installed until that final package step. Development profile activation/benchmark and visual/Windows/performance acceptance remain open.

## Previous checkpoints (historical)

## Latest checkpoint — 2026-10-22 18:02 UTC, post-exit recovery and vendor closure gate

Owner material migration `20261022T175620268905Z-migrate-legacy-default-material` is **VERIFIED**: all five scene after-hashes/metas and the native LegacyDefaultLit material/meta match the receipt. Exactly 23 slots changed; all other scene bytes were verified during the operation. Do not rerun this completed plan.

Owner settings retirement `20261022T175658171460Z-retire-legacy-urp-settings` passed its in-process checks, but **did not remain complete after Editor exit**. All seven archived asset/meta pairs were intact; GraphicsSettings had reverted exactly to its pre-run hash, including the original URP registration. The log explicitly warns that runtime GraphicsSettings.UnregisterRenderPipelineSettings is deprecated in favor of EditorGraphicsSettings. More importantly, the installed URP postprocessor calls UniversalRenderPipelineGlobalSettings.Ensure on domain reload/GraphicsSettings import; its Core utility searches existing global settings and can upgrade a vendor asset. During this run it upgraded Mountain's Pipeline.asset (version 3 to 10), generated Assets/DefaultVolumeProfile.asset, and queued additional imports. TagManager remains byte-identical to the checkpoint. The initial in-process retirement receipt is not a final certificate.

Recovery completed with Unity closed (process check returned no Unity; no lock): restored only the original project UniversalRenderPipelineGlobalSettings.asset/meta from the operation archive to keep URP's Ensure callback away from vendor fallback until package removal. Restored Mountain Pipeline.asset from the preserved checkpoint; its meta already matched the checkpoint. The auto-upgraded vendor version and meta were separately backed up. The newly generated empty DefaultVolumeProfile and its meta were archived outside Assets after confirming its sole consumer was that auto-upgraded vendor file. **Six old settings assets remain archived; the one project URP global setting is intentionally present again.** GraphicsSettings remained unchanged during this recovery. Checkpoint and Git/LFS were read/preserved, never modified. Original vendor content and the upgraded variant are both recoverable.

Exact recovery receipt: `Evidence/settings-postexit-recovery-20261022T180250066750Z/recovery.json`. Combined review: `Evidence/legacy-material-settings-completion-review-20261022.json`. Do not rerun `retire-legacy-urp-settings`; its pinned plan is consumed. Retire the temporary global setting and registration only in the final package-removal sequence, using the Editor registration API and post-process-exit checks.

### Next owner action — bounded, read-only vendor closure

Existing baseline pairs now identify **160 vendor/root-recovery references across 104 files**. These counts exclude the deliberately restored project global setting and are not the entire project's current reference count. Added the seven already-known vendor source consumers (six shaders and one editor factory), giving a fixed **111-file** audit plan: `Evidence/vendor-closure-plan.json`.

`audit-vendor-closure` queries Unity's imported dependency database to distinguish all project assets, the existing 47 production roots, enabled build scenes, every Resources root, preloaded assets and always-included shaders. Resources matters even for vendor demo folders because absence from a production scene alone does not prove build exclusion. Each planned input reports reachability, direct project consumers, URP dependency paths, shader identity/HDRP support, obsolete material version objects, and existing direct URP-to-HDRP sibling candidates. Candidates are not automatically treated as equivalent. It never saves assets, converts files, opens scenes, forces imports, builds or runs tests. Dynamic string/Addressables/AssetBundle loading and source/license preservation remain explicit limitations.

The runner verifies all 111 input/meta hashes and ten protected inputs both before Unity launch and **after the Editor process exits**, catching import/quit side effects that an executeMethod receipt alone cannot prove absent. Its old retirement action also now rejects a post-exit GraphicsSettings mismatch. Source syntax/whitespace and pinned hashes were checked statically; no agent-run Unity/tests.

Save and close Unity normally, then run only:

Completed by owner at 18:08 UTC with zero post-exit changed inputs. This command block is retired; use the latest checkpoint above.

Review the generated `vendor-closure-*/audit.json` and runner `postExitChangedInputs`. Do not repeat completed material, volume, camera/light or regression gates. Use actual reachability to prepare a consolidated preservation/migration/disposition plan for remaining vendor variants; do not delete whole vendor packages or shared textures/models. Package remains installed. Visual/performance/build acceptance and Development profile activation/benchmark are still pending.

## Previous checkpoints (historical)

## Latest checkpoint — 2026-10-22 17:47 UTC, TownVolume gate closed

Owner `20261022T174531001413Z-migrate-town-volume` completed successfully. Receipt `Evidence/town-volume-20261022T1746082860440Z-2c3b71674c2746cc8d290c11a0bd8160/migration.json` reports four native replacements with saved values and identities verified. Independently verified current profile after-hash, unchanged meta and all 73 protected hashes. The migration is applied; do not rerun its old plan. Visual acceptance remains pending.

Owner production audit `20261022T174612874736Z-production-audit`: 47 roots / 13,166 slots / 269 material objects; no serialized URP findings and no collection errors. Classifications unchanged: 153 native HDRP, 23 custom compatible, 17 source-unverified, 75 inactive/deferred and the one GUI/Text exception. Three previously known disabled particle-root empty slots remain. Scoped audit is not whole-project HDRP-only certification. Exact report pinned in that run's `reviewed-coverage.json`; review receipt: `Evidence/town-volume-completion-review-20261022.json`.

### Next owner batch — package material references and obsolete settings

1. `migrate-legacy-default-material`: creates project-owned `Assets/LastSignal/Settings/Rendering/HDRP/LegacyDefaultLit.mat` through native HDRP APIs, using the inspected package material's base color (0.5 gray), metallic 0, smoothness 0.5, opaque/non-emissive settings. Changes exactly **23 MeshRenderer slots across five known scenes** (S004 recovery, recovery 0, visual slice, explicit S004 validation, forest archive). SHA-pinned plan: `Evidence/legacy-default-material-plan.json`. Complete scene/meta backups precede mutation; 72 protected hashes, exact target renderer IDs/slot positions and native material identity/tuning are checked. No scene is opened or resaved. Only the old material reference tokens change; every other byte, including S004 physics/gameplay, is checked after all imports. On failure original scene bytes are restored; only the newly created material may then be removed. Visual equivalence remains unapproved.
2. `retire-legacy-urp-settings`: archives exactly seven obsolete project settings assets with their original metas outside Assets, under its unique evidence directory, and unregisters only the URP global-settings entry. SHA-pinned plan: `Evidence/retire-urp-settings-plan.json`. A targeted search for these seven GUIDs across **all Assets and ProjectSettings including ignored files** found only their own metadata/dependency group, GraphicsSettings and the historical rollback JSON. A source path/name search found only the historical recovery fallback. This was a focused dependency search, not another disk audit. HDRP default/quality assignments remain exact; GraphicsSettings may differ only by the single URP registration line. Nine protected hashes and all seven archive/meta hashes are verified after import. Full originals and GraphicsSettings are restored on failure.

The recovery menu now preflights the original URP default and every quality asset before touching settings. Missing/retired assets cause an explicit exception; no null pipeline is assigned. Recovery JSON is preserved as history and does not load assets automatically. To restore URP intentionally later, restore the archived assets/metas and package first. Asset archives are preserved, not discarded; vendor files, checkpoint and Git/LFS remain untouched.

Save and close Unity normally, then run:

Completed by owner at 17:56–17:57 UTC, with the post-exit exception and recovery described in the latest checkpoint. The old batch is retired.

Review both execution logs and the unique `legacy-default-material-*/migration.json` / `retired-urp-settings-*/retirement.json` receipts, verify current after-hashes and archives, and only then mark these stages applied. Agent has not executed either action, Unity, compilation or tests. Python runner syntax and source whitespace were checked. The already-passed regression suites and unchanged coverage audit are not requested again for this batch. Both methods compile through the owner runner; the methods perform their own scoped post-import checks.

URP package remains installed. After this batch, remaining known serialized dependencies are primarily preserved vendor/demo and root recovery assets; classify and resolve them before package cleanup. Do not claim a clean HDRP-only project from the production scope alone. Development profile activation/benchmark, visual checks (including Bloom/terrain) and Windows acceptance remain open.

Unity registration API reference used for the retirement method: https://docs.unity3d.com/cn/6000.0/ScriptReference/Rendering.GraphicsSettings.GetSettingsForRenderPipeline.html (includes register/unregister examples). Generic registration methods are invoked with the loaded URP type via reflection so no Universal assembly reference is reintroduced.

## Previous checkpoints (historical; use the commands above)

## Latest checkpoint — 2026-10-22 17:33 UTC owner gates reviewed

The legacy component migration is **APPLIED AND VERIFIED**, not pending or rolled back. Owner component-id-reuse-edit **4/4**, explicit S004 tree-stage **6/6**, presentation-play **28/28**, all zero skipped. Cleanup receipt `Evidence/urp-legacy-component-cleanup-20261022T1729476428440Z-9fa7cdfa7cc340928a903a396f932d70/cleanup.json`: 326 URP lights removed, 35 cameras replaced, 150 native HDRP light records added. Independently verified all 33 after-hashes, all 33 unchanged metas, the pinned plan and seven protected-input hashes. S004 gameplay/physics preservation is enforced by the successful document verifier and owner stage tests. Review: `Evidence/legacy-component-completion-review-20261022.json`.

Do not rerun the completed component migration, old plans or passed regression suites. Package remains installed: known references still include volume/settings assets, five project scene references to the package default Lit material, and vendor/demo consumers. Rechecking only the previously known pairs gives **336 pairs across 117 files** (176 project, 160 outside LastSignal). These are serialized reference pairs, not active runtime dependency counts or a new whole-project audit.

### Next owner action — TownVolume only

Added `migrate-town-volume`, backed by `HdrpTownVolumeMigration.cs` and hash-pinned `Evidence/town-volume-plan.json`. TownVolume is referenced by FirstTownCore and seven legacy/validation scenes, including S004; its four stored effects are still Universal types. Runtime TownAtmosphere already clones the profile and adds native HDRP atmosphere components. This migration replaces only the four persistent post-processing overrides with native HDRP types, preserving the main profile GUID/fileID and existing scene references. It does not add sky/fog/exposure overrides or regenerate atmosphere/scenes.

The 25 common parameter values and override flags are copied with native HDRP setters. Enums map by name; unsupported values or clamping stop before mutation. Bloom's URP-only skipIterations/clamp/highQualityFiltering/filter/downscale/maxIterations have no identical HDRP contract; source data remains in the complete backup, and native sampling defaults apply. HDRP-only parameters remain at non-overriding defaults. This matches the existing native controlled-ACES numeric tuning, but is **not a visual equivalence claim**: persistent effects now participate in HDRP and require visual acceptance.

The action backs up the profile/meta before mutation, validates all four source objects before saving, then verifies saved native script GUIDs, object count, main-profile identity, unchanged root fields, complete native parameter serialization and all protected hashes. Failure rolls the profile/meta back. All 33 migrated scene/prefab files and metas, ForestPrototype120, FirstTownCore, TreePhysicsBody and package manifests are guarded. No vendor/checkpoint/Git/LFS changes. Source was statically inspected; runner Python syntax parsed; no agent-run Unity, compilation or tests.

Save and close Unity normally, then run:

Completed by owner at 17:45–17:47 UTC. This old command block is retired; use the latest checkpoint above.

Review `town-volume-*/migration.json`, current asset/meta/guard hashes and the newly generated coverage report. `PROCESS_COMPLETED_REVIEW_REQUIRED` alone is not acceptance. This is the next narrow gate, not the final package-removal command. Do not repeat old profile/finger/mask/component authoring.

After this gate: resolve the five project package-material references, retire the unused legacy settings dependency group after checking incoming references (including the URP global-settings registration in GraphicsSettings), and classify preserved vendor/demo consumers before URP manifest/lock cleanup. Current path/GUID search proves only project-scope consumers; it does not yet prove old settings are unused outside LastSignal. Do not delete or move vendor assets to bypass those dependencies. Development activation, visual/performance and Windows build acceptance remain open.


> Current instruction (2026-10-22): Unity runs are owner-operated. See [CURRENT_HANDOFF.md](CURRENT_HANDOFF.md) for the next exact commands and latest reviewed results. Earlier next-step and authorization statements below are historical.


Current owner gates: full EditMode **894/894**, presentation **28/28**, explicit S004 **6/6**. Native material factory source changes compile and pass the EditMode suite. Development profile created, not activated/benchmarked. The historical inventory below is retained as a baseline, not current failure counts.

Current stage removes ten inactive URP SubShader blocks while preserving every corresponding HDRP block and Properties block. Backups/hashes: Evidence/hdrp-shader-source-stage-20261022T071544987083Z/manifest.json. Owner shader validation now passes 18/18; updated coverage has zero active repairs and 46 remaining URP camera/light components. Forest shader outer CustomEditor/AddPass/BasemapGen/fallback references remain, as do runtime/serialized/vendor dependencies. URP package removal is still blocked; no package or source asset was deleted.

Baseline: 1,232 literal occurrences across 59 source/package files. This is a search count, NOT a required runtime dependency count. Recovery JSON alone contributes hundreds of historical shader names. Docs were excluded. Runtime type imports occur in RenderPipelineAdapter; three project asmdefs directly reference Universal.Runtime. Both pipeline packages resolve to 17.5.0 in the saved lock. No other resolved package declares URP as a dependency.


Latest source stage (after owner component failure 075045): all Universal type imports/branches removed from RenderPipelineAdapter; all three project Universal.Runtime references and USING_URP version defines removed. Backups: Evidence/hdrp-runtime-source-20261022T075840325366Z/manifest.json. Owner compilation/regression is pending. Component failure rolled back all touched assets, verified against all five original file/meta hashes. Corrected cleanup preserves unrelated serialized content rather than accepting Unity default-field drift; the 46 components are still present until owner rerun succeeds. Material metadata removal remains complete (241/241). Remaining package blockers are the pending component gate, forest outer shader dependencies and legacy settings/archive/vendor references; historical inventory below is not a current count.

## Latest reviewed state — combined gate and forest source stage

Owner full EditMode 900/900 and PlayMode 472/472 passed. All 46 planned production-scope URP components are removed, current five file/meta hashes verified. Production audit has zero URP findings, zero active repairs and zero collection errors. Source runtime/three asmdef removal now verified. Do not rerun the completed cleanup plans.

Forest outer shader dependencies now replaced in source with project-owned helpers and inspector; owner 20-shader validation and three GPU basemap tests pending. Static reachable include closure depends only on Unity engine/Core/HDRP. Exact stage backups: Evidence/hdrp-forest-dependencies-20261022T083627800424Z/manifest.json.

Only existing baseline consumer/GUID pairs were rechecked: 373 pairs remain across 145 files, of which 213 pairs are project-owned and 160 outside LastSignal. This is not active-use classification. Other FPS prefabs, old production/validation scenes including S004, legacy settings and vendor/demo assets remain to migrate or archive after reference review. Exact list: Evidence/urp-remaining-known-references-20261022.json. These references explain why zero production-scope findings does not yet authorize a safe package removal. No package/vendor/checkpoint/physics mutation; migration remains PARTIAL.

## Latest owner forest gate / prepared legacy plan

Forest shader gate now 20/20 and GPU basemap 3/3. Import/GPU gate complete; visual acceptance remains separate. Prepared, not applied: 33 known project scene/prefab files with 326 legacy light and 35 camera records. 150 missing HDRP light records will be added with default data; existing Light values and all unrelated records remain exact. S004 rendering records can change; physics/gameplay records cannot. Plan/source hashes and owner commands are in CURRENT_HANDOFF.md. Nine preservation cases precede batch execution; six S004 and 28 presentation cases follow. No package removal yet; legacy volume/settings/package-material and vendor/demo consumers remain outside this component stage.

## Legacy batch retry status — 091042

Nine owner preservation tests pass. The 33-file batch was rolled back in full after the verifier rejected Unity reusing 30 URP light fileIDs for same-owner HDRP light data in the first archive scene. All 33 asset/meta and protected-input hashes match originals. Source now permits only same-kind/same-owner pipeline ID replacement; four targeted owner tests precede retry. No legacy batch removals are currently applied; the earlier five-file production cleanup remains complete. Package stays installed. Exact diagnostic evidence: Evidence/legacy-component-id-reuse-review-20261022.json.

## File inventory

Paths relative to project root. Evidence/baseline/urp-occurrences.json provides every matched line and text; this table gives the required replacement and removal decision. Existing adapter architecture is reused.

| File | Occurrences | Type | Replacement / current status | Removal eligible / risk |
|---|---:|---|---|---|
| `Assets/LastSignal/Art/FirstTownAtmosphere/TownSky.shader` | 2 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Art/FirstTownForest/Shaders/ForestLit.shader` | 31 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Art/FirstTownForest/Shaders/ForestLitInput.hlsl` | 1 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Art/FirstTownForest/Shaders/ForestTerrain.shader` | 22 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Art/FirstTownForest/Shaders/ForestTerrainBase.shader` | 17 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Art/FirstTownForest/Shaders/ForestTerrainPasses.hlsl` | 5 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Scripts/Editor/Character/ArberryCharacterAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/Character/DieselCharacterAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/FirstTown/FirstTownAssets.cs` | 4 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/FirstTown/FirstTownAtmospherePolish.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/FirstTown/FirstTownGeometry.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceAssets.cs` | 2 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceRefinement05.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/LastSignal.Editor.asmdef` | 3 | Assembly reference/version define | Remove URP reference and define only with adapter completion | No / high |
| `Assets/LastSignal/Scripts/Editor/Loot/LootAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/MRPolyCatalogAuthoring.cs` | 2 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/R01Authoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/RealAssetIntegration.cs` | 2 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/Rendering/HdrpMaterialMaskUtility.cs` | 2 | Migration/conversion utility | Retain conversion and rollback until acceptance; not a runtime assembly dependency | No / medium |
| `Assets/LastSignal/Scripts/Editor/Rendering/HdrpMaterialMigrationUtility.cs` | 9 | Migration/conversion utility | Retain conversion and rollback until acceptance; not a runtime assembly dependency | No / medium |
| `Assets/LastSignal/Scripts/Editor/S001Project.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/S013/LastSignal.Art.Editor.asmdef` | 3 | Assembly reference/version define | Remove URP reference and define only with adapter completion | No / high |
| `Assets/LastSignal/Scripts/Editor/S013/S013AssetEvaluation.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/ScopeOpticAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/ShelterAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/StudioNewPunchVisualAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/StudioNewPunchZombieValidator.cs` | 2 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/TreePhysicsAuthoring.cs` | 1 | URP-only interior wood check | Implemented active-pipeline opaque HDRP/Lit check; user verification pending | No / medium |
| `Assets/LastSignal/Scripts/Editor/TriplePistolAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/WorldTimeAuthoring.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Editor/ZombieAssetIntegration.cs` | 2 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Integrations/MotionCore/Editor/VehicleBenchmarkAuthoring.cs` | 2 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Integrations/MotionCore/Editor/VehicleDrivingAuthoring.cs` | 2 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Assets/LastSignal/Scripts/Runtime/LastSignal.Runtime.asmdef` | 3 | Assembly reference/version define | Remove URP reference and define only with adapter completion | No / high |
| `Assets/LastSignal/Scripts/Runtime/Rendering/RenderPipelineAdapter.cs` | 42 | Runtime camera/light/volume/render requests | Existing HDRP branches; parity must pass before removing URP branches | No / high |
| `Assets/LastSignal/Scripts/Tests/ArtEditor/S013PrefabValidationTests.cs` | 2 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Scripts/Tests/EditMode/ArberryIntegrationAssetTests.cs` | 1 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Scripts/Tests/EditMode/CharacterIntegrationAssetTests.cs` | 1 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Scripts/Tests/EditMode/MRPolyCatalogTests.cs` | 1 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Scripts/Tests/EditMode/TriplePistolTests.cs` | 1 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Scripts/Tests/EditMode/ZombieAssetTests.cs` | 1 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Scripts/Tests/EditMode/ZombieBloodVfxTests.cs` | 1 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Scripts/Tests/PlayMode/ZombieDamagePlayTests.cs` | 1 | Legacy shader expectations/test fixture | Update assertions to actual HDRP behavior with focused tests | No / medium |
| `Assets/LastSignal/Settings/Rendering/HDRP/MaterialRollbackManifest.json` | 436 | Recovery record, not runtime | Retain until final gate; checkpoint replaces rollback later | No / low |
| `Assets/LastSignal/Shaders/Optics/ScopeLens.shader` | 5 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Shaders/VFX/Blood/LS_Ballistic.shader` | 4 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Shaders/VFX/Blood/LS_BloodParticleSplat.shader` | 6 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Shaders/VFX/Blood/LS_BloodSurface.shader` | 4 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Shaders/VFX/Blood/LS_LocalizedWound.shader` | 4 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/LastSignal/Shaders/VFX/Particles/LS_ParticleUnlit.shader` | 4 | Custom shader/include, often dual pipeline | Keep HDRP passes; remove URP passes/includes only after shader/visual verification | No / high |
| `Assets/ThirdParty/VFX/Free Blood VFX/Demo/Resources/Shaders/SH_Vefects_URP_VFX_Character_Showoff.shader` | 97 | Vendor variant/sample | Resolve effective consumers and native HDRP replacements; preserve shared sources | No / high |
| `Assets/ThirdParty/VFX/Free Blood VFX/Demo/Resources/Shaders/SH_Vefects_URP_VFX_Opaque_01.shader` | 97 | Vendor variant/sample | Resolve effective consumers and native HDRP replacements; preserve shared sources | No / high |
| `Assets/ThirdParty/VFX/Free Blood VFX/Demo/Scene/Shaders/SH_Vefects_Grid_01.shader` | 97 | Vendor variant/sample | Resolve effective consumers and native HDRP replacements; preserve shared sources | No / high |
| `Assets/ThirdParty/VFX/Free Blood VFX/VFX/Shaders/SH_Vefects_URP_Free_Blood_01_Lit.shader` | 97 | Vendor variant/sample | Resolve effective consumers and native HDRP replacements; preserve shared sources | No / high |
| `Assets/ThirdParty/VFX/Free Blood VFX/_ Extra/Shaders/SH_Vefects_Extra_Billboard_01.shader` | 97 | Vendor variant/sample | Resolve effective consumers and native HDRP replacements; preserve shared sources | No / high |
| `Assets/ThirdParty/VFX/Free Blood VFX/_ Extra/Shaders/SH_Vefects_Extra_Grid_01.shader` | 97 | Vendor variant/sample | Resolve effective consumers and native HDRP replacements; preserve shared sources | No / high |
| `Assets/ThirdParty/Vehicles/CharacterController/Scripts/Editor/DemoVehicleFactory.cs` | 1 | Authoring shader factory or validator | Migrate properties, texture packing, transparency and keywords; name replacement is insufficient | No / high |
| `Packages/manifest.json` | 1 | Direct package/lock | UPM removal after prerequisites; no transitive owner in current lock | No / high |
| `Packages/packages-lock.json` | 3 | Direct package/lock | UPM removal after prerequisites; no transitive owner in current lock | No / high |

## Serialized dependencies invisible to namespace searches

Evidence/baseline/urp-serialized-references.json records 619 GUID consumer/target pairs, with 287 project-owned consumers. These are dependency candidates, not resolved effective renderer usage. They include **241 project material consumers of URP AssetVersion subassets**, 22 camera-data consumers, 20 light-data consumers, old volumes, pipeline/renderer/global-settings assets. Removing the package alone would leave serialized script references unresolved.

FirstTownCore still references UniversalAdditionalLightData; ForestPrototype120 has no direct URP GUID reference in this scan. Do not strip component YAML or delete metadata blindly. The owner-run production audit now records resolved URP MonoBehaviours, nested overrides, material slots and missing scripts. Its scope includes existing declared roots, FirstTownCore, ForestPrototype120 and enabled build scenes.

## Exact gates

1. Compile the corrected audit and run its six focused tests; obtain updated production coverage.
2. Resolve remaining source-map color space/channel issues and review UI/default shader exceptions, disabled blood-system root slots, runtime material assignments, characters, weapons, scopes and weather. Prior coverage had 129 NEEDS_REPAIR entries, but 100 normal-space warnings used the wrong keyword contract. Counts must be remeasured, not treated as 100 automatically approved materials.
3. Finish property-aware authoring conversion so generators cannot recreate URP materials. Verify camera/scope/VFX shader parity and S004/movement/save behavior manually.
4. With verified checkpoint and accepted results, remove old project URP components/subassets through Unity APIs, obsolete asmdef references/adapter branches and shader passes. Prove each obsolete asset has no required consumer before AssetDatabase deletion.
5. Remove direct package from manifest, let UPM write the lock, then compile and rerun dependency/reference/focused regression gates. Do not force-edit package caches.

Required URP runtime dependency count is **not zero**. HDRP-only completion is not claimed.

## 2026-10-22 source-factory update

TriplePistolAuthoring and VehicleBenchmark/VehicleDriving authoring now use native HDRP inputs, mask packing (pistol), transparency/emission semantics and HDMaterial.ValidateMaterial; VehicleAuthoring references HDRP Runtime. No generators executed; owner compilation/EditMode gate pending. Scoped source search under Scripts/Editor and MotionCore/Editor now finds URP shader names only in migration/coverage identification, not material creation. Runtime adapter/asmdefs, serialized URP data and shader/vendor dependencies in the baseline remain open. Presentation 28/28 and S004 6/6 are verified separately.

## 2026-10-22 material metadata stage

The existing serialized baseline lists 241 project material consumers of URP AssetVersion; current file/meta hashes are pinned in Evidence/urp-material-metadata-plan.json. Owner-only cleanup removes this exact editor metadata type only when the main material has a verified HDRP shader contract, preserving serialized content and identities of all remaining objects. Backups precede the first mutation; failure rolls back touched files. This stage is prepared, not executed. Non-HDRP/unresolved entries are retained for review; package-removal eligibility is not inferred from this count.

## 2026-10-22 verified metadata / prepared component stage

All 241 guarded material URP AssetVersion records removed successfully with unchanged surviving material content/IDs/metas; owner material tests 37/37. Next five-file plan addresses all 46 components reported in production scope (42 lights with existing HDRP data, four cameras needing native data). Components are not yet removed; owner cleanup/coverage/full-PlayMode gates pending. Historical out-of-scope assets, runtime branches, forest outer dependencies and vendor dependencies remain separate package-removal blockers.
