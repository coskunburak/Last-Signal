# 00 — ENTRY AUDIT (PHASE A: READ-ONLY DISCOVERY)

**Date:** 2026-10-20  
**Project:** Last Signal (`/Users/burakcoskun/Last Signal`)  
**Audit Mode:** READ-ONLY (`CONFIRMED`)  
**Execution Status:** `IMPLEMENTED` (Audit Document) · Unity Runtime/Tests: `TEST_NOT_RUN`

---

## 1. Executive Summary & Core Coordinates

| Audit Dimension | Verified Local Value | Source / Evidence | Status |
|---|---|---|---|
| **Unity Editor Version** | `6000.5.0f1 (88b47c5e7076)` | `ProjectSettings/ProjectVersion.txt` | `CONFIRMED` |
| **Git Branch** | `s012-integrated-graybox-slice` | `git branch --show-current` | `CONFIRMED` |
| **Git HEAD Commit** | `84aa7358be7b3f47517b7e083ff0734e762e210f` | `git rev-parse HEAD` | `CONFIRMED` |
| **Working Tree State** | **DIRTY** (42 modified tracked files, 52 untracked top-level/nested paths) | `git status --short --untracked-files=all` | `CONFIRMED` |
| **Backup Verification State** | `BACKUP_NOT_VERIFIED` (User stated a backup copy exists, but external backup integrity/completeness cannot be verified from inside the workspace) | Workspace boundary rule | `BLOCKED` (Requires checkpoint before destructive/global changes) |
| **Active Render Pipeline** | **URP 17.5.0** (`com.unity.render-pipelines.universal: 17.5.0`) | `Packages/manifest.json` | `CONFIRMED` |
| **HDRP Package Installed?** | **NO** (`com.unity.render-pipelines.high-definition` absent) | `Packages/manifest.json`, `Packages/packages-lock.json` | `CONFIRMED` |
| **Default Custom RP Asset** | `Assets/LastSignal/Settings/Rendering/PC_RPAsset.asset` (GUID `4b83569d67af61e458304325a23e5dfd`) | `ProjectSettings/GraphicsSettings.asset` | `CONFIRMED` |
| **Quality Settings RP Overrides** | Quality 0 (`Mobile`): `Mobile_RPAsset.asset` (`5e6cbd92db86f4b18aec3ed561671858`)<br>Quality 1 (`PC`, Active Default for Standalone): `PC_RPAsset.asset` (`4b83569d67af61e458304325a23e5dfd`) | `ProjectSettings/QualitySettings.asset` | `CONFIRMED` |
| **RP Global Settings** | `Assets/LastSignal/Settings/Rendering/UniversalRenderPipelineGlobalSettings.asset` (GUID `18dc0cd2c080841dea60987a38ce93fa`) | `ProjectSettings/GraphicsSettings.asset` | `CONFIRMED` |
| **Active PC Renderer Path** | `PC_Renderer.asset` (GUID `f288ae1f4751b564a96ac7587541f7a2`) — **Forward+** (`m_RenderingMode: 2`), Native RenderPass ON, SSAO RendererFeature active | `Assets/LastSignal/Settings/Rendering/PC_Renderer.asset` | `CONFIRMED` |
| **Active Post-Processing** | URP Volume Framework (`UnityEngine.Rendering.Universal` Volume components: `Tonemapping` ACES, `ColorAdjustments`, `Bloom`, `Vignette`). Note: Legacy `com.unity.postprocessing: 3.5.4` is also in `manifest.json` for older vendor packs. | `TownAtmosphere.cs`, `FirstTownAtmosphereAuthoring.cs`, `Packages/manifest.json` | `CONFIRMED` |
| **Production Town Scene** | `Assets/LastSignal/Scenes/Production/FirstTownCore.unity` (12 MB, 500×500 m resident world) | `Assets/LastSignal/Scenes/Production/` | `CONFIRMED` |
| **Visual Staging Scene** | `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity` (14.88 MB, currently modified in working tree) | `Assets/LastSignal/Scenes/Validation/` | `CONFIRMED` |
| **Tree Harvesting Stage Scenes** | `FirstTownHarvest_20261018150846-032db4e9.unity` (S003)<br>`FirstTownTreePhysics_20261019103726-320e36cd.unity` (Birch S004)<br>`FirstTownTreePhysics_20261019163434-8e0f05b7.unity` (Pine S004) | `Assets/LastSignal/Scenes/Validation/` | `CONFIRMED` |
| **New Art Direction Docs** | Present locally under `Docs/ArtDirection/LS_Realism_2026/` (`00_MASTER_INDEX.md` – `18_ACCEPTANCE_RECEIPTS.md`) + `Docs/05_ASSET_ART_RULES.md`, `Docs/19_Art_Audio_Asset_Pipeline.md`, `Docs/20_Performance_Build_Operations.md`, `Docs/S013_ART_BIBLE.md` | `Docs/` | `CONFIRMED` |

---

## 2. Baseline Cryptographic Checksums (SHA-256)

Recorded prior to any modification:

| File Path | SHA-256 | Git Status |
|---|---|---|
| `Packages/manifest.json` | `e4fcd5497a2ed7c2bb3586d74f8981f02b691b6b4f3aa95b419774aa3b215ac4` | Modified (`M`) |
| `Packages/packages-lock.json` | `610dafd63a251831ebbe7825e3c0675f368d01c7fd5224c04fb423812fb2e800` | Modified (`M`) |
| `ProjectSettings/GraphicsSettings.asset` | `4ac0d402c463fc830a46a1ddfab132e617bb2ee00dbc9834784d6df415014768` | Clean |
| `ProjectSettings/QualitySettings.asset` | `6814d6cc3eb850dd7d2a069a8b17ba9f7591deba8b1758a855978bd9c8627643` | Clean |
| `Assets/LastSignal/Scenes/Production/FirstTownCore.unity` | `dad614f052afa3bb64845e5977404d0364702cce0e47e6ac1df72b43680f70b8` | Clean |
| `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity` | `28f9e5ea8e5f7cc113f416e10e327a296be6029a8fa85bbc613d5d2556828cf1` | Modified (`M`) |
| `Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity` | `a80fd7fb8021c8b115a424edab8cbdd3ca9575a683b23338b60f3c0f69b77154` | Untracked (`??`) |
| `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019163434-8e0f05b7.unity` | `9dec41ef9b6c5772ecd21e5847265d33978a3515faf98f987c6a145ac94ad74d` | Untracked (`??`) |
| `Assets/LastSignal/Data/FirstTown/Terrain.asset` | `3b8988fa6d32f3d05a06abc485c35bfe9fcd740be760b1329664e5cd93ece72e` | Clean |
| `Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/Terrain.asset` | `c7fceb5a6c35a545adacff4faf057297ae6ff42b1cb5b96d8ce9e96361b2e387` | Clean |
| `Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538/Terrain.asset` | `18e707790f900dc250840bdbb29272ce30affd8bc3d5e885f29ac25cd56163a0` | Clean |

---

## 3. Required Audit Findings

### 3.1. `CURRENT_STATE`
1. **Engine & Pipeline:** Unity `6000.5.0f1` running URP `17.5.0` with Forward+ rendering (`PC_RPAsset` / `PC_Renderer`), SRP Batcher enabled (`m_UseSRPBatcher: 1`), 4 shadow cascades up to 50 m (`PC_RPAsset`), and a single SSAO renderer feature (`Intensity: 0.4`, `Radius: 0.3`).
2. **World & Forest Architecture:** `FirstTownCore.unity` is a 500×500 m resident world (`Terrain` at `(-250, -10, -250)`, size `(500, 20, 500)`). Forest generation (`FirstTownForest`) uses `FantasyEnvironments` normalized derivatives (`Pine.prefab` 1,554 tris, `Birch.prefab` 1,192 tris, `Bush.prefab` 54 tris, `Rock.prefab` 658 tris, `Grass.prefab` 4 tris) with custom URP HLSL shaders (`ForestLit.shader`, `ForestTerrain.shader`) driven by `ForestPresentation.cs`.
3. **Atmosphere Architecture:** `WorldClock -> WorldTimePresentation -> TownAtmosphere` is the single authoritative presentation chain. `TownAtmosphere` blends 4 sky panoramas (`TownSky.shader`), drives sun/moon directional lights and custom spherical harmonics ambient probes, manages 2,154 bounded rain particles (`TownRain`), swaps 23 exterior surface materials for wetness darkening/smoothness, toggles 12 puddle meshes, and budgets 41 local practical lights (max 12 active within 100 m, max 2 shadow-casting within 30 m).
4. **Tree Chopping Architecture (Uncommitted Local Work — BLD P01 S003/S004):** Active uncommitted work implements a full physics-based chopping and harvesting system under `Assets/LastSignal/Scripts/Runtime/Gathering/` and `Assets/LastSignal/Scripts/Editor/TreePhysicsAuthoring.cs`, using `com.dgreenheck.openfracture` in Editor-only mode to pre-slice 16 depth levels × 8 radial directions (128 mesh pairs of upper trunk + lower stump) with a `FixedJoint` whose anchors shift to the support centroid as wood is removed.
5. **Documentation State:** `Docs/ArtDirection/LS_Realism_2026/` was recently added as untracked files defining ADR `LS-ART-2026-001` (Grounded High-Fidelity Realism).

### 3.2. `PRESERVATION_RISKS`
1. **Extensive Uncommitted Work in Working Tree:**
   - **S003/S004 Physical Tree Harvesting:** 10 new runtime scripts in `Scripts/Runtime/Gathering/`, 3 new editor tools (`HarvestPilotAuthoring.cs`, `TreePhysicsAuthoring.cs`, `TreeVerificationRunner.cs`), 8 new test suites (`P01*Tests.cs`), modified combat/noise/audio/persistence/world-cell scripts (`MeleeAttackResolver.cs`, `MeleeWeaponController.cs`, `ProductionAudio.cs`, `SaveCodec.cs`, `SaveGame.cs`, `SaveSession.cs`, `WorldCellManager.cs`), and 4 generated validation scenes (`FirstTownHarvest_*`, `FirstTownTreePhysics_*`).
   - **Visual Slice Refinement 05b:** Uncommitted modifications to `FirstTownCore_VisualSlice.unity`, `CanopySheet.asset`, `CanopyMetalwork.asset`, `Lighting.asset`, `SliceRefinement05.cs`, and `SliceVisualReview.cs`.
   - **Character/Weapon Adjustments:** Modified `WoodenAxe.asset` profile and grip/character play tests.
2. **Git-Ignored Vendor & Evidence Assets:** `.gitignore` excludes `/Assets/ThirdParty/`, `/Assets/ThirdParty.meta`, `/Assets/Noto_Sans/`, `/Assets/_Recovery/`, `/Assets/TestScene/`, and `/Docs/Implementation/**/Evidence/`. Any `git clean` or fresh clone without local copy loses 10,337 vendor files (~6.3 GB) and all local test evidence.
3. **Root-Level Loose Third-Party Folders:** `Assets/TreeChopping-main` and `Assets/Unity-Tree-Chopping-Simulation-main` are currently untracked at the root of `Assets/` (including `.exe` and `.wmv` files in `Unity-Tree-Chopping-Simulation-main` that trigger macOS video importer warnings).

### 3.3. `RENDER_DEPENDENCIES`
- **3 Assembly Definitions** reference `Unity.RenderPipelines.Universal.Runtime`: `LastSignal.Runtime.asmdef`, `LastSignal.Editor.asmdef`, `LastSignal.Art.Editor.asmdef`.
- **12 C# Scripts** (6 Runtime, 6 Editor) directly import `UnityEngine.Rendering.Universal` (`TownAtmosphere.cs`, `ScopeOpticPresenter.cs`, `CharacterInspectionCamera.cs`, `S013EnvironmentPresentation.cs`, `S013Performance.cs`, `S014PopulationCapture.cs`, plus 6 Editor authoring/verification scripts).
- **11 Custom Shader / HLSL Files** under `Assets/LastSignal/` target `UniversalPipeline` / `UniversalForward` (`ForestLit.shader`, `ForestLitInput.hlsl`, `ForestWind.hlsl`, `ForestTerrain.shader`, `ForestTerrainBase.shader`, `ForestTerrainPasses.hlsl`, `TownSky.shader`, `ScopeLens.shader`, `LS_Ballistic.shader`, `LS_BloodSurface.shader`, `LS_LocalizedWound.shader`).
- **259 Project-Owned Materials** under `Assets/LastSignal/` use URP shaders (235 `URP/Lit`, 7 `LastSignal/Forest/Lit Wind`, 4 `URP/Particles/Unlit`, 3 `URP/Simple Lit`, 5 Blood VFX, 2 `TownSky`, 1 `ForestTerrain`, 1 `ScopeLens`, 1 `URP/Terrain/Lit`).

### 3.4. `ART_DIRECTION_CONFLICTS`
1. **Primary Forest Family (`FantasyEnvironments`):** Currently active in `FirstTownCore` and `FirstTownCore_VisualSlice`. Its 1,554-tri Pine and 1,192-tri Birch use stylized/mid-frequency painted textures without normal/roughness fidelity, single-tier LOD (cull-only at 0.018–0.035 screen height, zero geometric decimation), and repetitive conical silhouettes that conflict with the Sons of the Forest grounded realism target.
2. **Ground & Undergrowth:** Current terrain uses 4 diffuse-only `FantasyEnvironments` terrain layers (`layer_ground_grass`, `soil`, `road`, `rock`) with flat green appearance and 4-triangle grass cards, lacking 3D forest litter, deadwood, root plates, ferns, moss, and PBR normal/roughness micro-relief.
3. **Stylized / Low-Poly Vendor Packs in `ThirdParty`:** `Silver_Cats`, `HolotnaMountainURP`, `TT3D`, `Polygon-Lite Survival Collection`, `Stylized Vehicles Pack Free`, `CrowbarLowPoly`, and `Zombie_Survival_AssetPack_LowPoly` conflict visually with grounded PBR realism.

### 3.5. `MISSING_EVIDENCE`
1. **External Backup Verification (`BACKUP_NOT_VERIFIED`):** Must create a verified local snapshot/manifest of dirty files before any pipeline or scene mutation.
2. **Commercial License Receipts (`LICENSE_PENDING`):** Local `.meta` files and READMEs do not contain Unity Asset Store seat/order receipts or Fab license grants for several imported packs (`FantasyEnvironments`, `Holotna`, `Barn`, `VGtiXN Countryside gas station`, `MarpaStudio`, `Free Wood Door Pack`, `Abandoned buildings`, `Modular European House`, `Audio/Tree` clips).
3. **Production Realistic Conifer / Foliage Pack:** No photorealistic/grounded conifer tree pack with multi-tier geometry LODs and separable bark/canopy topology is currently imported in `Assets/ThirdParty`.
4. **Target Windows Hardware Baseline:** Only macOS Apple Silicon (M3 Pro) Editor measurements exist (`16.695 ms` median / `17.309 ms` p95 in S004 Pine test); no Windows Standalone 1080p GPU/CPU p95/p99 benchmark has been captured.

### 3.6. `MIGRATION_BLOCKERS`
1. **Owner Approval Gate (`APPROVE HDRP MIGRATION`):** Required before installing `com.unity.render-pipelines.high-definition`, altering `GraphicsSettings.asset`/`QualitySettings.asset`, or converting shaders/materials.
2. **Dirty Working Tree Protection:** Uncommitted S003/S004 tree physics and VisualSlice Refinement 05b changes must be preserved intact; global material upgrade wizards (`Edit > Rendering > Materials > Convert...`) would corrupt vendor folders and active staged diffs if run unchecked.
3. **Custom Shader Parity (`ForestLit`, `ForestTerrain`, `TownSky`, `ScopeLens`, `LS_Blood*`):** Unity's automatic material upgrader cannot convert custom URP HLSL shaders (`ForestLit.shader` with `_LSForestWindPhase`/`_LSForestWetness`, `TownSky.shader` 4-panorama blend, `ScopeLens.shader`, and procedural blood shaders). Project-owned HDRP equivalents must be authored and validated before switching the active pipeline.
4. **Hardcoded Shader Name Check in `TownAtmosphere.cs`:** Line 354 checks `m.shader.name == "Universal Render Pipeline/Lit"` and `_Surface`/`_Metallic`/`_BaseColor`/`_Smoothness` properties directly.
