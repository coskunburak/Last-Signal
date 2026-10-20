# 04 — URP DEPENDENCY MAP (PHASE D)

**Date:** 2026-10-20  
**Scope:** Complete static dependency audit of `UnityEngine.Rendering.Universal`, URP shaders, URP volume components, and render settings across `Assets/LastSignal/`.  
**Status:** `CONFIRMED`

---

## 1. Summary of URP Dependencies in `Assets/LastSignal/`

| Dependency Type | Count | Affected Scope |
|---|---:|---|
| **Assembly Definitions (`.asmdef`)** | 3 | `LastSignal.Runtime.asmdef`, `LastSignal.Editor.asmdef`, `LastSignal.Art.Editor.asmdef` |
| **Runtime C# Scripts (`.cs`)** | 6 | `TownAtmosphere.cs`, `ScopeOpticPresenter.cs`, `CharacterInspectionCamera.cs`, `S013EnvironmentPresentation.cs`, `S013Performance.cs`, `S014PopulationCapture.cs` |
| **Editor C# Scripts (`.cs`)** | 6 | `FirstTownAtmosphereAuthoring.cs`, `FirstTownAtmosphereVerification.cs`, `SliceVisualReview.cs`, `S013AssetEvaluation.cs`, `S013ProductionAuthoring.cs`, `S013PrefabValidation.cs` |
| **Custom Shaders & HLSL (`.shader`, `.hlsl`)** | 11 | `ForestLit.shader`, `ForestLitInput.hlsl`, `ForestWind.hlsl`, `ForestTerrain.shader`, `ForestTerrainBase.shader`, `ForestTerrainPasses.hlsl`, `TownSky.shader`, `ScopeLens.shader`, `LS_Ballistic.shader`, `LS_BloodSurface.shader`, `LS_LocalizedWound.shader` |
| **Project-Owned Materials (`.mat`)** | 259 | 235 `URP/Lit`, 7 `LastSignal/Forest/Lit Wind`, 4 `URP/Particles/Unlit`, 3 `URP/Simple Lit`, 5 Blood VFX, 2 `TownSky`, 1 `ForestTerrain`, 1 `ScopeLens`, 1 `URP/Terrain/Lit` |
| **Pipeline Settings Assets (`.asset`)** | 6 | `PC_RPAsset.asset`, `PC_Renderer.asset`, `Mobile_RPAsset.asset`, `Mobile_Renderer.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `DefaultVolumeProfile.asset` |

> **Important Architectural Finding:** Core gameplay, simulation, physics, AI, inventory, save/load, and tree chopping (`WorldClock`, `WorldSimulation`, `WorldTimePresentation`, `ForestPresentation`, `GatheringWorld`, `TreeStructure`, `TreePhysicsBody`, `ProductionAudio`, `VehicleWorld`) have **zero direct `UnityEngine.Rendering.Universal` dependencies**.

---

## 2. File-by-File C# Source Dependency Audit

### 2.1. Runtime C# Scripts

| File Path | URP Symbols Used | Purpose | HDRP Equivalent | Full Rewrite Needed? | Pipeline-Independent Adapter Strategy | Effort & Regression Risk |
|---|---|---|---|---|---|---|
| `Assets/LastSignal/Scripts/Runtime/Atmosphere/TownAtmosphere.cs` | `UnityEngine.Rendering.Universal`, `UniversalAdditionalCameraData`, `AntialiasingMode`, `AntialiasingQuality`, `ColorAdjustments`, `Bloom`, `Vignette`, `"Universal Render Pipeline/Lit"` string check, `_BaseColor`, `_Smoothness` | Session presentation of sky, sun/moon, fog, wetness materials, practical light budgeting, camera AA/post, and volume overrides | `HDAdditionalCameraData`, `HDAdditionalLightData`, `Exposure`, `ColorAdjustments`, `Bloom`, `Vignette`, `Fog` / `VisualEnvironment`, `"HDRP/Lit"` (`_BaseColor`, `_Smoothness` are identical in HDRP Lit!) | **No** (Adapter / conditional compile `#if USING_HDRP`) | Extract pipeline-specific camera/volume/light/shader-name binding behind an `IAtmosphereRenderBackend` or `#if` adapter while keeping 100% of time/weather/wetness/lamp-ranking math in `TownAtmosphere.cs`. | **Medium Effort (4–6h) · Medium Risk** (Must preserve `Unbind()` restoration contract and light intensities). |
| `Assets/LastSignal/Scripts/Runtime/Combat/ScopeOpticPresenter.cs` | `UniversalAdditionalCameraData`, `CameraRenderType.Base`, `SetRenderer(...)`, `renderShadows`, `renderPostProcessing`, `volumeLayerMask` | Configures secondary Picture-in-Picture RenderTexture camera for rifle scope optics without double-tonemapping | `HDAdditionalCameraData` (`customRenderingSettings = true`, disable Postprocess / Tonemapping on scope RT camera, set `volumeLayerMask = 0`) | **No** (10-line camera configuration helper) | Route camera setup through a `RenderPipelineCameraAdapter.ConfigureScopeCamera(Camera, ...)` helper. | **Low Effort (1h) · Low Risk** (Covered by `ScopeOpticTests` / `ScopeOpticPlayTests`). |
| `Assets/LastSignal/Scripts/Runtime/Character/CharacterInspectionCamera.cs` | `GetUniversalAdditionalCameraData()`, `CameraRenderType.Base`, `cameraStack?.Clear()` | Configures debug/inspection 3rd-person camera (`Digit6`/`Digit7`) | `HDAdditionalCameraData` (copy camera settings, standard base camera) | **No** (5-line helper) | Use `RenderPipelineCameraAdapter.ConfigureInspectionCamera(Camera)`. | **Low Effort (0.5h) · Low Risk**. |
| `Assets/LastSignal/Scripts/Runtime/Slice/S013EnvironmentPresentation.cs` | `GetUniversalAdditionalCameraData()`, `renderPostProcessing`, `AntialiasingMode.SubpixelMorphologicalAntiAliasing` | Legacy S013Cabin camera post-processing / SMAA enabler | `HDAdditionalCameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing` | **No** (3-line helper) | Use `RenderPipelineCameraAdapter.EnableGameplayPostAndAA(Camera)`. | **Low Effort (0.5h) · Low Risk**. |
| `Assets/LastSignal/Scripts/Runtime/Slice/S013Performance.cs` | `UniversalRenderPipelineAsset` (`shadowDistance`, `shadowCascadeCount`, `msaaSampleCount`), `UniversalAdditionalCameraData` | Diagnostic benchmark runner for S013Cabin quality tier overrides and golden screenshot camera | `HDRenderPipelineAsset` / `HDShadowInitParameters` / Volume `HDShadowSettings` + `HDAdditionalCameraData` | **No** | Adapter method for benchmark tier overrides and capture camera initialization. | **Low Effort (1h) · Low Risk** (Editor/DevBuild only). |
| `Assets/LastSignal/Scripts/Runtime/Slice/S014PopulationCapture.cs` | `UniversalRenderPipelineAsset` | Reads current pipeline asset in S014 diagnostic capture | `RenderPipelineAsset` base type or `HDRenderPipelineAsset` | **No** | Inspect via base `RenderPipelineAsset` + pipeline adapter. | **Trivial (0.5h) · Low Risk**. |

### 2.2. Editor C# Scripts

| File Path | URP Symbols Used | Purpose | Adaptation Strategy | Effort & Risk |
|---|---|---|---|---|
| `Assets/LastSignal/Scripts/Editor/FirstTown/FirstTownAtmosphereAuthoring.cs` | `Tonemapping`, `TonemappingMode.ACES`, `ColorAdjustments`, `Bloom` | Authors `FirstTownAtmosphere` volume profile | Support both URP and HDRP volume component authoring (or keep URP authoring intact and add `FirstTownHdrpAtmosphereAuthoring.cs`). | **Low (1.5h) · Low Risk** |
| `Assets/LastSignal/Scripts/Editor/FirstTown/FirstTownAtmosphereVerification.cs` | `Tonemapping`, `TonemappingMode.ACES` | Validates `TownAtmosphere` volume profile | Check active pipeline's `Tonemapping` component type. | **Low (0.5h) · Low Risk** |
| `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceVisualReview.cs` | `UniversalAdditionalCameraData`, `AntialiasingMode`, `ColorAdjustments` | Captures fixed comparison views in `FirstTownCore_VisualSlice` | Use `RenderPipelineCameraAdapter` so comparison studio works identically in URP and HDRP! | **Low (1h) · Low Risk** |
| `Assets/LastSignal/Scripts/Editor/S013/S013AssetEvaluation.cs` | `Tonemapping`, `UniversalAdditionalCameraData` | Legacy S013 asset comparison scene builder | Route through camera/volume adapter. | **Low (0.5h) · Low Risk** |
| `Assets/LastSignal/Scripts/Editor/S013/S013ProductionAuthoring.cs` | `Tonemapping`, `ColorAdjustments`, `UniversalAdditionalCameraData` | Legacy S013Cabin authoring | Route through camera/volume adapter. | **Low (0.5h) · Low Risk** |
| `Assets/LastSignal/Scripts/Editor/S013/S013PrefabValidation.cs` | `m.GetTag("RenderPipeline", false, "") != "UniversalPipeline"` | Validates S013 prefab materials match active pipeline | Accept `"HDRenderPipeline"` when HDRP is active, or check active pipeline tag dynamically. | **Low (0.5h) · Low Risk** |

---

## 3. Custom Shader & HLSL Dependency Audit

**Critical Rule:** Unity's built-in Material Upgrader (`Edit > Rendering > Materials`) **cannot** upgrade custom HLSL shaders. Every custom shader below requires an explicit project-owned HDRP counterpart (or Shader Graph derivative) if migrating to HDRP.

| Shader / HLSL Path | Shader Name | Material Count | Current URP Implementation | HDRP Migration Requirement | Effort & Risk |
|---|---|---:|---|---|---|
| `Assets/LastSignal/Art/FirstTownForest/Shaders/ForestLit.shader`<br>+ `ForestLitInput.hlsl`<br>+ `ForestWind.hlsl` | `LastSignal/Forest/Lit Wind` | 7 | Adapted from URP 17.5 `Lit.shader`. Adds vertex wind (`_LSForestWindPhase`, `_LS_FOREST_STATIC` keyword for cut/falling S004 trees) and global wetness (`_LSForestWetness` darkening albedo by 14% and raising smoothness to >=0.32). | **Author HDRP Shader Graph or HDRP Lit Subshader:** Create an HDRP Lit Shader Graph (with foliage subsurface diffusion profile support) that reads the exact global properties `_LSForestWindPhase`, `_LSForestWetness`, and `_LS_FOREST_STATIC` in Vertex/Fragment stages. | **Medium Effort (3–4h) · High Visual Impact** (Improves foliage transmission while preserving `ForestPresentation.cs` & S004 static bark contract). |
| `Assets/LastSignal/Art/FirstTownForest/Shaders/ForestTerrain.shader`<br>+ `ForestTerrainBase.shader`<br>+ `ForestTerrainPasses.hlsl` | `LastSignal/Forest/Terrain Lit Wet` | 1 | Adapted from URP 17.5 `TerrainLit.shader`. Modulates terrain splat albedo/smoothness by `_LSForestWetness`. | **Author HDRP Terrain Lit Derivative or Shader Graph Terrain:** Adapt HDRP `TerrainLit` (or Custom Pass / Global Wetness property) to apply `_LSForestWetness` darkening & smoothness. | **Medium Effort (3–4h) · Medium Risk**. |
| `Assets/LastSignal/Art/FirstTownAtmosphere/TownSky.shader` | `LastSignal/FirstTown/AtmosphereSky` | 2 | Unlit background skybox (`RenderPipeline = UniversalPipeline`) blending 4 panoramas (`_DayTex`, `_OvercastTex`, `_SunsetTex`, `_NightTex`) + drifting `_CloudTex` + sun disc. Only includes URP `Core.hlsl` for `TransformObjectToHClip` and texture macros. | **Dual-Option:**<br>1. **Direct Port (1h):** Add HDRP Sky / Background pass or convert to a pipeline-agnostic sky shader / HDRP Custom Sky Volume component.<br>2. **HDRP Native Upgrade:** Drive HDRP `PhysicallyBasedSky` + `VolumetricClouds` + `VolumetricFog` from `TownAtmosphere` while keeping panorama blend as fallback. | **Low–Medium Effort (2–4h) · Low Risk**. |
| `Assets/LastSignal/Shaders/Optics/ScopeLens.shader` | `LastSignal/Optics/ScopeLens` | 1 | Unlit reticle + pupil shadow + `_ScopeTex` sampler (`UniversalForwardOnly`, `DepthOnly`, `DepthNormalsOnly`). | **Add HDRP Pass / Unlit Shader Graph:** Port vertex/fragment HLSL (which is pure math + 1 texture sample) to an HDRP `ForwardOnly` / `DepthForwardOnly` subshader or HDRP Unlit Shader Graph. | **Low Effort (1.5h) · Low Risk**. |
| `Assets/LastSignal/Shaders/VFX/Blood/LS_Ballistic.shader`<br>`LS_BloodSurface.shader`<br>`LS_LocalizedWound.shader` | `LastSignal/VFX/Blood/*` | 5 | Transparent procedural blood/wound shaders (`UniversalForward` + URP `Core.hlsl` / `Lighting.hlsl`). | **Add HDRP Transparent Subshader or Shader Graph:** Keep identical material property names (`_BaseColor`, `_WoundMask`, etc.) so `ZombieLocalizedWounds.cs` and `ZombieBloodVfxPresenter.cs` require zero C# changes. | **Medium Effort (3h) · Low-Medium Risk** (Verified by `ZombieWoundTests` / `ZombieBloodVfxTests`). |

---

## 4. Project-Owned Standard Materials Audit (259 `.mat` Files)

| Shader GUID | Shader Name | Material Count | Property Compatibility (`URP/Lit` ↔ `HDRP/Lit`) | Conversion Strategy |
|---|---|---:|---|---|
| `933532a4fcc9baf4fa0491de14d08ed7` | `Universal Render Pipeline/Lit` | 235 | `_BaseColor`, `_BaseMap`, `_Metallic`, `_Smoothness`, `_BumpMap`, `_BumpScale`, `_OcclusionMap`, `_EmissionColor`, `_Cutoff` are **shared** between URP Lit and HDRP Lit. However, HDRP Lit prefers packed `_MaskMap` (`R=Metallic, G=AO, B=Detail, A=Smoothness`) when using textures. | Use a controlled, scripted project-only material converter (scoped strictly to `Assets/LastSignal/`, **never** `Assets/ThirdParty/`) that upgrades `URP/Lit` materials and generates packed `_MaskMap` textures where needed. |
| `0406db5a14f94604a8c57ccfbc9f3b46` | `Universal Render Pipeline/Particles/Unlit` | 4 | Rain streaks, splashes, dust bursts. | Map to `HDRP/Unlit` (or HDRP Particle shader) with identical alpha blend state. |
| `a8cdd4a1effaca7469894c508c106b9b` | `Universal Render Pipeline/Simple Lit` | 3 | Secondary simple props. | Map to `HDRP/Lit`. |
| `69c1f799e772cb6438f56c23efccb782` | `Universal Render Pipeline/Terrain/Lit` | 1 | Legacy S013 terrain material. | Map to `HDRP/TerrainLit`. |
