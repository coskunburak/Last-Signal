# S003 native pilot receipt — 2026-10-18

## A. EXECUTION STATUS

BLOCKED_INTEGRITY_CONFLICT — native pilot generation succeeded, but final preservation check detected ProjectSettings.asset drift. No further Editor actions after discovery; files retained for owner review. This is not S003 or G1 acceptance. Tests/builds/gameplay: NOT_RUN_BY_AGENT.

Existing focused EditMode and PlayMode PASS are owner-reported, respectively:
- Docs/Implementation/S021/Evidence/20261018T145052Z-s021-edit-52bd068d
- Docs/Implementation/S021/Evidence/20261018T145305Z-s021-play-f68c8cc7

## B. SOURCE PROTECTION

Branch: s012-integrated-graybox-slice
HEAD: 84aa7358be7b3f47517b7e083ff0734e762e210f

Preflight: one loaded, clean Unity source scene; Editor outside Play Mode and idle. One main Editor process plus its two import workers; no separate batch test Editor. Git-modified source scene was preserved as the input.

Source: Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity
Before and after SHA-256: 28f9e5ea8e5f7cc113f416e10e327a296be6029a8fa85bbc613d5d2556828cf1

72 baseline files compared; 71 unchanged, including protected VisualSlice work, production FirstTownCore and the reused forest generation. Exception:
- ProjectSettings/ProjectSettings.asset
- Before: ebf9a6d41a67eb43f2d649abf92a7783d25ec599b3bdb71d428d253a6dd289b6
- After: 1c55c01473831e1702ba4efbb61715b2262aabd2ea69b3346b62be39eabbc146
- Modification timestamp: 2026-10-18T18:08:48+0300, during authoring.
- Current Git diff against HEAD is only Standalone scripting define ordering: APP_UI_EDITOR_ONLY;SENTIS_ANALYTICS_ENABLED;UNITY_POST_PROCESSING_STACK_V2 becomes APP_UI_EDITOR_ONLY;UNITY_POST_PROCESSING_STACK_V2;SENTIS_ANALYTICS_ENABLED.
- Baseline captured its hash, not contents; that Git diff cannot establish the exact session delta against already modified baseline.
- Suspected automatic Editor/package settings persistence; cause not established. No explicit settings write was requested. No reset/restore attempted. Owner must review before gameplay acceptance continues.

## C. SELECTED TREES

Source hierarchy prefix: /FirstTown Forest — owned authoring[27]/Generated — regenerate only this subtree[1]/ForestEdge[0]/

| Tree | Species / source leaf | World position | Reason / observation |
|---|---|---|---|
| 1 | Pine[45] | (-158.5225, 0.4317, -69.8558) | Active generated prefab root; forest edge; terrain slope 1.75 degrees |
| 2 | Pine[56] | (-149.1139, 0.3484, -64.9378) | Active generated prefab root; forest edge; terrain slope 0.41 degrees |
| 3 | Birch[12] | (-173.4635, 1.1606, -57.9918) | Active generated prefab root; forest edge; terrain slope 2.89 degrees; nearby rock |

Prefab roots/species were inspected against Pine.prefab and Birch.prefab in Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/. No HeroTrees selected. Selected source trees were not moved.

Measured pair distances: Pine45–Pine56 10.6168m; Pine45–Birch12 19.0924m; Pine56–Birch12 25.3340m. All exceed 7m.
Distances from PlayerSpawn approximately 101.08m, 108.05m, 111.11m respectively.
Local root-pivot offsets above sampled ground approximately .235m, .322m, .324m; pivot offsets alone do not prove floating.
Edit Mode source and generated Scene View inspections show forest-edge ground, open approach strip and river farther to the right; no obvious building entrance occupation. Exact first-person access, detailed root penetration and navigation clearance NOT_VERIFIED. Nearby 4m collider scan found no nonterrain/nonself obstacle for the Pines; a rock was found near Birch.

## D. NATIVE AUTHORING OUTPUT

Existing menu invoked once: Last Signal > BLD > P01 > Create Isolated S003 Pilot From Three Selected Trees.
Scene: Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity
Data directory: Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/
Assets: Log.asset, PilotCatalog.asset, Pine.asset, Birch.asset, Manifest.asset.
Prefab: Assets/LastSignal/Prefabs/Gathering/Pilot-20261018150846-032db4e9/HarvestLog.prefab
Save slot: harvest_20261018150846-032db4e9
Manifest ID: harvest-manifest:05d547a197534334b0322d2879e5041e

Actual Console receipt:
```
S003_STAGE_PREPARED sourceSHA256=28f9e5ea8e5f7cc113f416e10e327a296be6029a8fa85bbc613d5d2556828cf1 scene=Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity manifest=Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Manifest.asset clearanceApproved=false. Owner QA NOT_RUN.
```

Latest Console retrieval at authoring inspection: 0 errors, 8 warnings (seven obsolete API editor warnings and one AI Toolkit account API timeout). Not a zero-warning or compilation test claim.
A protective preflight earlier stopped before menu dispatch because unrelated persistent assets were dirty; it created no pilot. The successful menu was not repeatedly invoked.

## E. INTEGRATION INSPECTION

VERIFIED by native Edit Mode reference inspection:
- Active generated scene saved and clean; three ResourceNodeViews linked to GatheringWorld and manifest.
- Manifest valid property true; clearanceApproved=false.
- Both SaveSession and WorldCellManager use the generated PilotCatalog; isolated save slot above.
- Each node has active standing visual, one enabled trunk collider, disabled retry collider.
- Birch node: harvest:f01ca8cb88a74783b46ada5c7112e482
- Pine45 node: harvest:c9f22cf3b0f34ac1bfb3a050bd08e64d
- Pine56 node: harvest:b255d940478d46898fe2871a2ed19e68
- Definitions: health100, damage25, yield3; Log output and HarvestLog WorldItem definition agree, quantity3.
- HarvestLog has one renderer and one enabled collider; mesh and materials equal the source Log.prefab references.
- Saved Log.asset contains its prefab GUID reference, maxStack8, massGrams2000 and provisional icon.
- Existing Player prefab and FPS_WoodenAxe.prefab binding retained; weapon definition weapon.axe.
- HeroTrees child count0 before/after; generated decorative subtree ResourceNodeView count0 before/after.
- Terrain still references VisualSlice G20261016-112859-538/Terrain.asset.
- NavMeshSurface references VisualSlice G20261016-112859-538/Navigation.asset; not rebaked.

NOT_VERIFIED: runtime harvesting, navigation paths, camera reach, old-save gameplay, cell streaming, performance and full visual acceptance. Static navigation reference does not certify navigation.

## F. CHANGED FILES

New generated artifacts are listed exhaustively below, including normal Unity metas. Existing local (already untracked) editor script changed at HarvestPilotAuthoring.CreatePilot, line133:
```diff
- AssetDatabase.SaveAssets();
+ AssetDatabase.SaveAssetIfDirty(item); // Only the newly created Log gained a post-creation prefab reference; preserve unrelated dirty assets.
```
Reason: avoid globally saving unrelated dirty assets; Log alone gains a post-CreateAsset prefab reference. Guards and runtime behavior were not changed. Native menu succeeded using the imported correction. No explicit compile command or test was run. ProjectSettings drift nevertheless requires owner review.

This receipt is the only new documentation. No runtime edits this turn, vendor edits, Git writes, promotion or S004 work. Cannot claim no unrelated changes because of the settings drift.

## G. MANUAL ACCEPTANCE HANDOFF

First resolve/accept the settings integrity finding. Then:
1. Open the exact generated scene above (currently active, clean). Inspect all three anchors, save slot and forest-edge approaches, especially Birch's nearby rock.
2. Only the owner may approve spatial clearance by changing the generated Manifest clearanceApproved after review. Save only approved pilot changes. While false, the pilot is intentionally not ready for harvesting.
3. Enter Play Mode manually with the intended new-game/session flow and equip the existing Wooden Axe. A fresh axe/tree should show health100→75→50→25→0, revision0→1→2→3→4, durability100→99→98→97→96. These are expected values, not observations.
4. Final accepted hit: one deterministic log reward quantity3; repeated swings/interactions must not duplicate it.
5. Wrong tool: no state mutation or free logs. Full inventory: reward remains recoverable without loss/duplication.
6. Save a partially damaged tree, use the actual Load flow and confirm damage persists. Deplete, save, Load twice and confirm yield does not replenish.
7. Reopen scene and verify state through intended save/session behavior. Merely reopening a scene is not proof a persisted checkpoint was loaded.
8. Check decorative trees remain non-harvestable and an obstructing harvest target does not allow zombie damage through rejection; inspect grounding, collider alignment, reachability and scene continuity.
9. Nonresident-cell persistence, wider regression, navigation, performance and S004 presentation remain pending.

Previously owner-passed focused suites were not rerun. No additional test execution requested merely to reconfirm those passes.

## H. OPEN RISKS AND NEXT ACTION

Immediate owner action: review the ProjectSettings integrity finding and decide whether current settings are acceptable or require an owner-controlled recovery. No automated restoration is safe without the pre-existing contents.
After that, perform pilot spatial clearance review before gameplay QA. Birch rock proximity, approximately100–111m spawn approach and static-only navigation inspection remain relevant.
S003/G1 remain unaccepted. Provisional icon and final felling/audio feedback are not art approval. S004 not started.


## Exact generated file inventory

```text
Assets/LastSignal/Data/Gathering.meta
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9.meta
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Birch.asset
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Birch.asset.meta
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Log.asset
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Log.asset.meta
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Manifest.asset
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Manifest.asset.meta
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/PilotCatalog.asset
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/PilotCatalog.asset.meta
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Pine.asset
Assets/LastSignal/Data/Gathering/Pilot-20261018150846-032db4e9/Pine.asset.meta
Assets/LastSignal/Prefabs/Gathering.meta
Assets/LastSignal/Prefabs/Gathering/Pilot-20261018150846-032db4e9.meta
Assets/LastSignal/Prefabs/Gathering/Pilot-20261018150846-032db4e9/HarvestLog.prefab
Assets/LastSignal/Prefabs/Gathering/Pilot-20261018150846-032db4e9/HarvestLog.prefab.meta
Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity
Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity.meta
```

## Original working-tree status

```text
 M Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538/Lighting.asset
 M Assets/LastSignal/Art/FirstTownVisualSlice/Refinement05/CanopyMetalwork.asset
 M Assets/LastSignal/Art/FirstTownVisualSlice/Refinement05/CanopySheet.asset
 M Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity
 M Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceRefinement05.cs
 M Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceVisualReview.cs
 M Assets/LastSignal/Scripts/Runtime/Combat/MeleeAttackResolver.cs
 M Assets/LastSignal/Scripts/Runtime/Combat/MeleeWeaponController.cs
 M Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs
 M Assets/LastSignal/Scripts/Runtime/Persistence/SaveCodec.cs
 M Assets/LastSignal/Scripts/Runtime/Persistence/SaveFileStore.cs
 M Assets/LastSignal/Scripts/Runtime/Persistence/SaveGame.cs
 M Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs
 M Assets/LastSignal/Scripts/Runtime/Persistence/SaveValidation.cs
 M Assets/LastSignal/Scripts/Runtime/Session/SessionFlow.cs
 M Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs
 M Docs/Implementation/FirstTownVisualSlice/REFINEMENT_05.md
 M ProjectSettings/ProjectSettings.asset
?? Assets/LastSignal/Scripts/Editor/HarvestPilotAuthoring.cs
?? Assets/LastSignal/Scripts/Editor/HarvestPilotAuthoring.cs.meta
?? Assets/LastSignal/Scripts/Runtime/Gathering.meta
?? Assets/LastSignal/Scripts/Runtime/Gathering/
?? Assets/LastSignal/Scripts/Tests/EditMode/P01HarvestTests.cs
?? Assets/LastSignal/Scripts/Tests/EditMode/P01HarvestTests.cs.meta
?? Assets/LastSignal/Scripts/Tests/PlayMode/P01HarvestPickupTests.cs
?? Assets/LastSignal/Scripts/Tests/PlayMode/P01HarvestPickupTests.cs.meta
?? Docs/Implementation/BLD/P01/
?? "Docs/MeshyAI LAST SIGNAL/"
```
