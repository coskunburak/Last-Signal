# 07 — PRESERVATION & ROLLBACK PROTOCOL

**Date:** 2026-10-20  
**Status:** `CONFIRMED` · Backup Verification State: `BACKUP_NOT_VERIFIED` (External user copy not inspectable from sandbox; internal SHA-256 checkpoint mandatory).

---

## 1. Non-Negotiable Preservation Rules

1. **Zero Destructive Git Commands:**
   - Prohibited: `git reset --hard`, `git clean -fd`, `git checkout -- .`, unapproved branch switching, or deleting untracked files.
2. **Zero Vendor Asset Mutation:**
   - `Assets/ThirdParty/` (10,337 files, 11,176 GUIDs) is read-only. Never modify vendor `.fbx`, `.mat`, `.prefab`, `.png`, `.exr`, or `.meta` files in place.
3. **GUID & Persistent Identity Invariance:**
   - Never delete or regenerate `.meta` files for existing scenes, prefabs, ScriptableObjects, `ItemDefinition` (`StableItemId`), or `PersistentEntityId` components.
4. **Uncommitted Local Work Protection:**
   - The current working tree contains 42 modified tracked files and 52 untracked paths representing **BLD P01 S003/S004 Physical Tree Chopping** and **FirstTownVisualSlice Refinement 05b**. Every file in this dirty set must remain intact.

---

## 2. Protected Baseline Checksums (SHA-256)

Any operation that unintentionally alters the following production/source files without an explicit promotion gate is an immediate **STOP & ROLLBACK** trigger:

| Protected Asset | Baseline SHA-256 (`2026-10-20`) | Role |
|---|---|---|
| `Assets/LastSignal/Scenes/Production/FirstTownCore.unity` | `dad614f052afa3bb64845e5977404d0364702cce0e47e6ac1df72b43680f70b8` | Production 500×500 m town scene (MUST NOT be overwritten during pilot/migration) |
| `Assets/LastSignal/Data/FirstTown/Terrain.asset` | `3b8988fa6d32f3d05a06abc485c35bfe9fcd740be760b1329664e5cd56163a0` (`3b8988fa6d32f3d05a06abc485c35bfe9fcd740be760b1329664e5cd93ece72e`) | Base FirstTown TerrainData |
| `Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/Terrain.asset` | `c7fceb5a6c35a545adacff4faf057297ae6ff42b1cb5b96d8ce9e96361b2e387` | Active Production Forest TerrainData |
| `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity` | `28f9e5ea8e5f7cc113f416e10e327a296be6029a8fa85bbc613d5d2556828cf1` | Staged Visual Slice (contains Refinement 05b) |
| `Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538/Terrain.asset` | `18e707790f900dc250840bdbb29272ce30affd8bc3d5e885f29ac25cd56163a0` | Staged Visual Slice TerrainData |
| `Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity` | `a80fd7fb8021c8b115a424edab8cbdd3ca9575a683b23338b60f3c0f69b77154` | S003 Harvest Pilot Source Scene |
| `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019163434-8e0f05b7.unity` | `9dec41ef9b6c5772ecd21e5847265d33978a3515faf98f987c6a145ac94ad74d` | S004 Pine Physics Validation Scene |
| `Packages/manifest.json` | `e4fcd5497a2ed7c2bb3586d74f8981f02b691b6b4f3aa95b419774aa3b215ac4` | Pre-migration Package Manifest |
| `ProjectSettings/GraphicsSettings.asset` | `4ac0d402c463fc830a46a1ddfab132e617bb2ee00dbc9834784d6df415014768` | Pre-migration URP GraphicsSettings |
| `ProjectSettings/QualitySettings.asset` | `6814d6cc3eb850dd7d2a069a8b17ba9f7591deba8b1758a855978bd9c8627643` | Pre-migration URP QualitySettings |

---

## 3. Recommended Snapshot Strategy Before Gate 2 Execution

Because the working tree has valuable uncommitted work across S003/S004 and VisualSlice, we recommend the following **two-layer local checkpoint** before any code or pipeline change in Gate 2:

1. **Layer 1 — File-Level Checkpoint Archive (`Checkpoints/PreGate2_DirtyTree_Backup.tar.gz`):**
   - Archive all 42 modified tracked files, all 52 untracked project-owned paths (`Assets/LastSignal/...`, `Docs/...`, `Tools/...`, `Packages/...`, `ProjectSettings/...`), and `SHA256SUMS.txt`.
   - This guarantees 1-second file-exact restoration of individual files without touching Git history or `Assets/ThirdParty/`.
2. **Layer 2 — Optional Owner-Approved Git Branch (`feat/visual-realism-migration`):**
   - Only if approved by the owner: commit current S004/Refinement05 state on `s012-integrated-graybox-slice` (or a new branch) so Git diffing during Gate 2 shows **only** Visual Realism / HDRP changes.

---

## 4. Five-Tier Rollback Matrix (`R0` – `R4`)

| Rollback Tier | Scope | Trigger Condition | Exact Rollback Procedure |
|---|---|---|---|
| **`R0` — Documentation Only** | `Docs/Implementation/VisualRealismMigration/` | Owner requests revision to migration docs/plan | Edit or remove only files inside `Docs/Implementation/VisualRealismMigration/`. Zero runtime/editor impact. |
| **`R1` — Asset Lab / Candidate Quarantine** | `Assets/LastSignal/Art/Realism/` & `Assets/ThirdParty/RealismCandidates/` | Imported candidate tree/foliage pack fails polycount, overdraw, S004 slicing, or license check | Delete only the isolated candidate subfolder and its project-owned derivative folder under `Art/Realism/`. Existing scenes never referenced it. |
| **`R2` — Hero Slice Stage (`120×120 m`)** | `Assets/LastSignal/Scenes/Validation/LS_Realism_HeroSlice.unity` & its generated `TerrainData` derivative | Hero Slice terrain/foliage placement breaks road clearance, NavMesh, or visual acceptance | Delete/regenerate only the isolated `LS_Realism_HeroSlice.unity` stage and its `Art/Realism/Generations/G<stamp>/` folder. `FirstTownCore.unity` and `FirstTownCore_VisualSlice.unity` remain at baseline SHA-256. |
| **`R3` — Pipeline Wiring (`HDRP -> URP`)** | `GraphicsSettings.asset`, `QualitySettings.asset`, `Packages/manifest.json`, `RenderPipelineAdapter` | HDRP exhibits unacceptable GPU frame-time regression on Windows PC target or unresolved shader parity issue | 1. Restore `ProjectSettings/GraphicsSettings.asset` (`4ac0d402...`) and `ProjectSettings/QualitySettings.asset` (`6814d6cc...`) pointing back to `PC_RPAsset.asset` (`4b83569d67af61e458304325a23e5dfd`).<br>2. Restore `Packages/manifest.json` (`e4fcd549...`).<br>3. Because URP shaders/materials were never deleted, the project immediately renders in URP 17.5.0 Forward+ again. |
| **`R4` — Production Scene Promotion** | `Assets/LastSignal/Scenes/Production/FirstTownCore.unity` | Post-promotion regression in save/load, navigation, or full-map performance | Restore `FirstTownCore.unity` (`dad614f0...`) and `Terrain.asset` (`c7fceb5a...`) from checkpoint copy and verify SHA-256 match. |
