# 06 — CONTROLLED HDRP MIGRATION EXECUTION PLAN (PHASE E)

**Date:** 2026-10-20  
**Gate Status:** `PROPOSED` · **LOCKED BEHIND OWNER APPROVAL (`APPROVE HDRP MIGRATION`)**  
**Rule:** None of Steps E1–E5 below will be executed until the owner explicitly replies with `APPROVE HDRP MIGRATION` (or selects the Option C/A baseline path).

---

## Pre-Migration Invariants

1. **No Destructive Git Operations:** Do not run `git reset`, `git clean`, or discard any of the 42 modified / 52 untracked paths (S003/S004 tree physics, VisualSlice Refinement 05b, character/weapon profiles).
2. **No Vendor Mutation:** Never run Unity's global "Convert All Built-in/URP Materials to HDRP" wizard across `Assets/ThirdParty/`. All material and shader conversions are strictly scoped to project-owned directories under `Assets/LastSignal/`.
3. **Preserve URP Assets for Instant Rollback:** Keep `PC_RPAsset.asset`, `PC_Renderer.asset`, `Mobile_RPAsset.asset`, `UniversalRenderPipelineGlobalSettings.asset`, and the original URP shaders intact so rolling back to URP requires only restoring `GraphicsSettings.asset` and `QualitySettings.asset`.

---

## Step-by-Step Execution Sequence (Post-Approval)

### Phase E1 — Migration Checkpoint & Dirty-Tree Snapshot (`Est: 15 min`)
1. **Create Non-Destructive Checkpoint Archive:**
   - Save a timestamped backup bundle of all modified/untracked project-owned files, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`, `FirstTownCore.unity`, `FirstTownCore_VisualSlice.unity`, and `FirstTownTreePhysics_*.unity` under a local checkpoint manifest (`Docs/Implementation/VisualRealismMigration/Checkpoints/`).
   - If the owner approves committing or branching the current dirty work first, create a dedicated Git branch `feat/hdrp-realism-migration` from current working state; otherwise maintain a file-level SHA-256 snapshot manifest so uncommitted S004 work is 100% recoverable.
2. **Verify `ThirdParty` Integrity:**
   - Record exact file count (`10,337`) and GUID digest of `Assets/ThirdParty/` before any package change.

### Phase E2 — Pipeline Abstraction & HDRP 17.5.0 Dependency Installation (`Est: 1.5–2h`)
1. **Isolate C# URP Calls Behind Pipeline Adapter (Before Removing/Switching Pipeline):**
   - Introduce `Assets/LastSignal/Scripts/Runtime/Rendering/RenderPipelineAdapter.cs` to encapsulate camera post-processing/AA (`UniversalAdditionalCameraData` vs. `HDAdditionalCameraData`), scope camera setup (`ScopeOpticPresenter.cs`), inspection camera setup (`CharacterInspectionCamera.cs`), and benchmark tier overrides (`S013Performance.cs`, `S014PopulationCapture.cs`).
   - Update `LastSignal.Runtime.asmdef`, `LastSignal.Editor.asmdef`, and `LastSignal.Art.Editor.asmdef` to support `Unity.RenderPipelines.HighDefinition.Runtime` alongside `Unity.RenderPipelines.Universal.Runtime` via `versionDefines` (`USING_URP`, `USING_HDRP`).
2. **Add HDRP Package (`com.unity.render-pipelines.high-definition: 17.5.0`):**
   - Add `"com.unity.render-pipelines.high-definition": "17.5.0"` to `Packages/manifest.json` while **retaining** `"com.unity.render-pipelines.universal": "17.5.0"` during the transition so zero compilation errors occur on load.

### Phase E3 — HDRP Pipeline Asset & Global Settings Wiring (`Est: 1h`)
1. **Create Project-Owned HDRP Settings under `Assets/LastSignal/Settings/Rendering/HDRP/`:**
   - `PC_HDRPAsset.asset` (`HDRenderPipelineAsset`):
     - Rendering Path: **Forward** or **Both (Forward/Deferred)** tuned for MSAA/Forward foliage transparency and Decal support.
     - SRP Batcher: **Enabled**.
     - Lit Shader Mode: **Both** or **Forward** (verify GPU frame cost on Hero Slice).
     - Volumetrics: **Enabled** (High/Medium reprojection, bounded depth).
     - Decals: **Enabled** (DBuffer, 50 m draw distance).
     - Transmission / SSS: **Enabled** (Foliage thin-object transmission).
     - Shadows: Directional 4 cascades, `50–70 m` distance, Punctual lights max 2 shadowed within 30 m (matching `TownAtmosphere` budget).
     - Ray Tracing (DXR) / SSGI: **Disabled**.
   - `HDRenderPipelineGlobalSettings.asset`: Default volume profile, diffusion profiles (Foliage & Skin), FrameSettings defaults.
2. **Controlled Single-Switch Wiring:**
   - Provide an Editor menu command `Last Signal/Rendering/Switch Active Pipeline (URP <-> HDRP)` that atomically updates `GraphicsSettings.defaultRenderPipeline` and `QualitySettings` (`PC` quality level) between `PC_RPAsset` (URP) and `PC_HDRPAsset` (HDRP) while logging before/after SHA-256 hashes.

### Phase E4 — Project-Owned Shader & Material Conversion (`Est: 4–6h`)
1. **Custom Shaders (11 files -> Project-Owned HDRP Derivatives):**
   - `LastSignal/Forest/Lit Wind` (`ForestLit.shader`) -> Create HDRP variant (`ForestLitHDRP.shadergraph` / `.shader`) reading global `_LSForestWindPhase`, `_LSForestWetness`, and `_LS_FOREST_STATIC`, adding foliage subsurface transmission.
   - `LastSignal/Forest/Terrain Lit Wet` (`ForestTerrain.shader`) -> Create HDRP Terrain Lit derivative with `_LSForestWetness` response.
   - `LastSignal/FirstTown/AtmosphereSky` (`TownSky.shader`) -> Port to HDRP Sky / Background shader (or bridge `TownAtmosphere` to HDRP `PhysicallyBasedSky` + HDRI Sky).
   - `LastSignal/Optics/ScopeLens` (`ScopeLens.shader`) -> Add HDRP `ForwardOnly` / `DepthForwardOnly` subshader so `ScopeOpticPresenter` works seamlessly in both URP and HDRP.
   - `LastSignal/VFX/Blood/*` (`LS_Ballistic`, `LS_BloodSurface`, `LS_LocalizedWound`) -> Add HDRP transparent subshaders preserving all material property IDs for `ZombieLocalizedWounds`.
2. **Project-Owned Materials (259 `.mat` files under `Assets/LastSignal/`):**
   - Generate HDRP material derivatives (or upgrade project-owned materials in a staged branch/copy) mapping `_BaseMap`, `_BaseColor`, `_BumpMap`, `_Metallic`, `_Smoothness`, `_EmissionColor`, and packing `_MaskMap` (`R=Metallic, G=AO, B=Detail, A=Smoothness`) where metallic/AO/smoothness textures exist.
   - For `MarpaStudio` Farm assets, reference the vendor's native `Assets/ThirdParty/MarpaStudio/HDRP/` materials via project-owned wrapper prefabs.

### Phase E5 — `TownAtmosphere` & Volume Adaptation (`Est: 3–4h`)
1. **Preserve Authority Chain (`WorldClock -> WorldTimePresentation -> TownAtmosphere`):**
   - `TownAtmosphere.cs` keeps its exact deterministic time-of-day (`day`), solar altitude (`altitude`), cloud swell (`clouds`), rain blend (`rainBlend`), wetness accumulation/drying (`900s` wet / `2400s` dry), and 41-lamp distance/priority ranking (`localLightBudget = 12`, max 2 shadowed within 30 m).
2. **HDRP Presentation Backend in `TownAtmosphere`:**
   - Drive HDRP `VisualEnvironment` + `Fog` (Exponential/Volumetric height fog with anisotropy for forest light shafts) instead of legacy `RenderSettings.fog` when running under HDRP.
   - Drive HDRP `Exposure` (Fixed mode calibrated to daylight/night phases), `Tonemapping` (ACES), `ColorAdjustments`, and `Bloom` on the session-cloned `VolumeProfile`.
   - Update `BindVehicle()` material check (line 354) to recognize both `"Universal Render Pipeline/Lit"` and `"HDRP/Lit"`.
   - Ensure `Unbind()` cleanly restores all HDRP light data, volume profiles, and shared materials on session exit.
