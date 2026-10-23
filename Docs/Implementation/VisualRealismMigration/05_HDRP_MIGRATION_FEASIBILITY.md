# 05 — HDRP MIGRATION FEASIBILITY & ARCHITECTURE EVALUATION (PHASE D)

**Date:** 2026-10-20  
**Engine Target:** Unity `6000.5.0f1` · Current: `com.unity.render-pipelines.universal@17.5.0` · Candidate: `com.unity.render-pipelines.high-definition@17.5.0`  
**Status:** `CONFIRMED` (Technical Assessment Complete · Implementation Awaiting Owner Gate)

---

## 1. HDRP 17.5.0 Technical Evaluation for Last Signal

### 1.1. Feature Necessity Matrix (Sons of the Forest Target vs. Bloat Avoidance)

Per Section 7.2, advanced HDRP features must only be enabled if they directly serve the survival-horror forest target within our 16.67 ms (60 FPS @ 1080p) budget:

| HDRP 17.5.0 Feature | Relevance to Sons of the Forest Target | Recommendation | Technical Rationale |
|---|---|---|---|
| **HDRP Lit & Subsurface Scattering (SSS / Transmission)** | **CRITICAL (High Value)** | **ENABLE (Thin Object Transmission for Foliage)** | Pine needles, fern fronds, and birch leaves in dense forests rely heavily on backlit solar transmission; flat Lambertian/standard URP Lit looks plastic when backlit at dawn/dusk. |
| **HDRP Terrain Lit (Up to 8 Layers + Height Blend)** | **HIGH** | **ENABLE (4–8 Layers, Height-Based Splat Blend)** | Eliminates soft linear splat transitions between mud, pine duff, leaf litter, and exposed roots/rocks without requiring extra passes. |
| **Volumetric Fog (Local Height Fog + Light Shafts)** | **CRITICAL (High Value)** | **ENABLE (Bounded Resolution / Froxel Budget)** | Essential for Sons of the Forest atmospheric depth: tree-trunk god rays at golden hour, damp low-lying woodland mist, and flashlight cones in rain/night (`V05`, `V06`, `V08`). |
| **Physically Based Sky + Volumetric Clouds** | **MEDIUM-HIGH** | **HYBRID / SELECTIVE** | HDRP Volumetric Clouds provide natural dynamic cloud shadows and overcast transitions, OR we can retain our deterministic 4-panorama HDRI sky blend (`TownSky`) with HDRP Volumetric Fog to save ~1.0–1.5 ms GPU time. |
| **HDRP Decals (DBuffer + Decal Layers)** | **HIGH** | **ENABLE** | Crucial for road cracks, asphalt-to-mud shoulder transitions, moss patches on cabins, blood splatters, and axe-chop bark damage decals. |
| **Physical Light Units (Lux / Lumens) & Exposure** | **HIGH** | **ENABLE (Fixed / Bounded Histogram Exposure)** | Prevents blown-out highlights in daylight while keeping night/interior practical lamps (`41 lamps` in `TownAtmosphere`) physically grounded. Keep exposure range tightly clamped so night remains dark and readable, avoiding eye-adaptation pumping. |
| **Screen Space Global Illumination (SSGI) / Ray Tracing (DXR)** | **LOW (High GPU Cost)** | **DISABLE BY DEFAULT** | Hardware DXR is unsupported on macOS Apple Silicon (Metal development host) and breaks the 16.67 ms budget on mid-range Windows GPUs. Use Probe Volumes (APV) / Baked + Ambient Probe instead. |
| **Screen Space Reflections (SSR)** | **MEDIUM** | **ENABLE ONLY ON WET ROADS / PUDDLES (Medium Tier)** | Enhances wet asphalt and the 12 authored road puddles during rain; disable in dense dry forest to save GPU ms. |

### 1.2. Development Host (macOS Apple Silicon) vs. Target Platform (Windows PC / Steam)

- **macOS Apple Silicon (Metal) Compatibility:** Unity `6000.5.0f1` HDRP `17.5.0` runs natively on Apple Silicon via Metal (Forward/Deferred, Compute Shaders, Volumetric Fog, Decals, SSS Transmission, SRP Batcher).
- **Platform Caveat:** Hardware Ray Tracing (DXR) is Windows D3D12-only and must not be required. Furthermore, Apple Silicon Unified Memory and tile-based deferred architecture exhibit different alpha-overdraw and bandwidth curves than discrete Windows PC GPUs (NVIDIA RTX / AMD Radeon). Therefore, **macOS Editor frame times are diagnostic only and never certify Windows 1080p 60 FPS PASS**.

---

## 2. Preservation of Core Gameplay & Simulation Authorities

Regardless of whether URP or HDRP is active, the following runtime invariants are **strictly non-negotiable**:

1. **Single Time & Weather Authority:** `WorldClock` owns `WorldSimulation` (time of day, weather state `0=clear, 1=rain`, wetness accumulation, roof protection via `RainRoof`).
2. **Single Presentation Chain:** `WorldClock -> WorldTimePresentation -> TownAtmosphere` (+ `ForestPresentation`). Neither HDRP `VisualEnvironment` nor any third-party weather asset may introduce a competing clock or weather state machine.
3. **Physical Tree Harvesting Authority:** `GatheringWorld`, `GatheringState`, `TreeStructure`, `TreePhysicsBody`, and `SaveSession` (`ResourceNodeState.treeVersion = 1`, `TreeCutSnapshot` v2) remain 100% pipeline-agnostic.
4. **Audio, Noise & AI Authority:** `ProductionAudio`, `GameplayNoiseSystem`, `WorldPressureNoiseAdapter`, and `ZombieAuditoryMemory` remain untouched.
5. **Entity & Scene Identity Preservation:** `PersistentEntityId` GUIDs, `StableItemId` definitions, `ItemCatalog`, and `FirstTownCore.unity` object identities must never be regenerated.

---

## 3. Comparison of the Three Migration Options (Section 7.4)

| Evaluation Criterion | OPTION A: Retain URP 17.5.0 + Realistic Assets, Custom Shaders & Lighting Overhaul | OPTION B: Direct In-Place Conversion of Active Project to HDRP 17.5.0 | OPTION C: Pipeline-Independent Art Architecture + Rendering Adapters + Staged HDRP Migration (Recommended) |
|---|---|---|---|
| **Visual Ceiling (Sons of the Forest Target)** | **Medium-High (75–80%)**<br>Can achieve good PBR surfaces and foliage with custom shaders, but lacks native froxel volumetric fog/god-rays, DBuffer decals, and native foliage SSS transmission without third-party plugins or heavy custom render passes. | **High (90–95%)**<br>Native volumetric fog, light shafts through conifer canopy, subsurface foliage transmission, height-blended terrain, and DBuffer road/bark decals. | **High (90–95%)**<br>Identical visual ceiling to Option B once the HDRP stage is activated, plus allows direct A/B visual and performance comparison on the 120×120 m Hero Slice. |
| **Pipeline-Specific Rework Effort** | **Low-Medium (~12–16h)**<br>Zero C# pipeline migration; requires writing custom URP foliage subsurface/transmission + height-fog/decal shaders. | **Medium-High (~20–28h)**<br>Requires immediate conversion of all 12 URP C# scripts, 11 shaders, and 259 materials before the project can render cleanly across all scenes. | **Controlled / Phased (~8h Adapter + ~12h Stage Conversion)**<br>Step 1 isolates the 12 C# URP scripts behind clean adapters (`RenderPipelineAdapter`) and keeps URP working. Step 2 installs HDRP and converts the Hero Slice & custom shaders in a reversible checkpointed stage. |
| **Windows PC Performance Potential (1080p 60 FPS)** | **High Scalability**<br>Lower baseline GPU overhead (~1.5–2.5 ms lighter base frame setup), though dense alpha overdraw still dominates forest cost. | **Moderate-High Overhead**<br>HDRP base pass + volumetrics carry a ~2.5–4.0 ms fixed GPU overhead; requires disciplinedLOD/impostor and shadow distance (`50–70 m`) tuning. | **Best Measured Control**<br>Allows measuring the exact GPU frame-time delta (URP Forward+ vs. HDRP) on the same 120×120 m realistic forest asset set before irreversible full-project promotion. |
| **Shader & Asset Compatibility** | High with existing 259 project materials; requires custom work for realistic foliage transmission. | Breaks existing URP materials (`Pink` shaders) if switched globally before all 11 custom shaders and 259 materials have HDRP equivalents. | **Zero Breakage:** Project-owned materials and custom shaders get explicit HDRP variants alongside URP variants; `MarpaStudio` already includes native `URP/` and `HDRP/` folders; Poly Haven CC0 sources are pipeline-neutral. |
| **Rollback Cost (Returning to URP)** | Zero (already on URP). | **High Risk** if scenes/materials are overwritten in place on a dirty working tree. | **Near-Zero Risk (`R2`/`R4` Rollback):** Dirty working tree is checkpointed first; URP assets (`PC_RPAsset`, URP materials/shaders) are preserved untouched; switching back is a single `GraphicsSettings`/`QualitySettings` restore. |

---

## 4. Technical Verdict & Recommendation

- **Why Raw FantasyEnvironments on HDRP Fails:** Switching to HDRP alone will **not** make the current 1,554-tri painted `FantasyEnvironments` pine trees or 4-tri flat grass look like *Sons of the Forest*. The #1 visual bottleneck observed in the owner's 2026-10-19 screenshot is **source asset geometry, texture fidelity, and ecological scatter layering**, followed by **volumetric atmospheric depth and foliage transmission**.
- **Why Option C → Controlled Option B Is the Optimal Production Path:**
  1. We have a **heavily dirty working tree** (42 modified files, 52 untracked paths including S003/S004 physical tree chopping and VisualSlice Refinement 05b).
  2. Only **6 Runtime C# scripts**, **6 Editor C# scripts**, and **11 custom shaders** in `Assets/LastSignal/` depend on URP.
  3. By executing **Option C's pipeline abstraction + checkpointing first**, and then executing **Controlled HDRP Migration (Phase E)** on the project with parallel project-owned HDRP material/shader derivatives (preserving URP assets intact for instant rollback), we achieve HDRP's volumetric fog, god-rays, decals, and foliage transmission **without** risking a single line of S003/S004 tree physics or FirstTownCore gameplay state.

---

## 5. Gate 2B Deliverable B — Dual Pipeline Compatibility Report (B1 & B2 Verification)

### 5.1. Direct Inspection of Local Unity 6000.5.0f1 Built-In HDRP Package
Read-only inspection of `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/` and `Library/PackageCache/` confirmed the following exact package and assembly contracts:

| Package / Assembly Dimension | Verified Local Specification (`Unity 6000.5.0f1`) | Coexistence & Compatibility Status |
|---|---|---|
| **Built-In HDRP Package Path** | `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.high-definition` | `CONFIRMED` (`version: "17.5.0"`, `unity: "6000.5"`) |
| **Built-In HDRP Config Path** | `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.high-definition-config` | `CONFIRMED` (`version: "17.5.0"`, `unity: "6000.5"`) |
| **Shared SRP Dependencies** | `com.unity.render-pipelines.core: 17.5.0` and `com.unity.shadergraph: 17.5.0` are **already resolved** in `Library/PackageCache` (`com.unity.render-pipelines.core@f43b729f1f6e`). HDRP additionally pulls built-in `com.unity.visualeffectgraph: 17.5.0` and `com.unity.render-pipelines.high-definition-config: 17.5.0`. | `CONFIRMED` (Zero version conflict with `com.unity.render-pipelines.universal: 17.5.0`) |
| **HDRP Runtime Assembly Name** | `Unity.RenderPipelines.HighDefinition.Runtime` (`Runtime/Unity.RenderPipelines.HighDefinition.Runtime.asmdef`, `autoReferenced: true`) | `CONFIRMED` |
| **HDRP Editor Assembly Name** | `Unity.RenderPipelines.HighDefinition.Editor` (`Editor/Unity.RenderPipelines.HighDefinition.Editor.asmdef`) | `CONFIRMED` |
| **Verified HDRP 17.5.0 C# APIs in `RenderPipelineAdapter.cs`** | Inspected in built-in package source:<br>• `HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing`<br>• `HDAdditionalCameraData.SMAAQualityLevel.Medium`<br>• `HDAdditionalCameraData.volumeLayerMask`<br>• `HDAdditionalCameraData.customRenderingSettings`, `renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Postprocess]`, `renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false)`<br>• `Tonemapping` (`TonemappingMode.ACES`), `ColorAdjustments` (`postExposure`, `contrast`, `saturation`), `Bloom` (`intensity`, `threshold`, `scatter`), `Vignette` (`intensity`, `smoothness`) | `CONFIRMED` (100% signature match against Unity 6000.5.0f1 HDRP 17.5.0 source) |

### 5.2. Separation of Conditional Compilation vs. Runtime Pipeline Selection (Objective B2)
In `Assets/LastSignal/Scripts/Runtime/Rendering/RenderPipelineAdapter.cs`, compile-time symbols (`USING_URP`, `USING_HDRP`) and runtime pipeline dispatch (`IsHdrpActive` / `UseHdrpPath`) are strictly separated:

| Dependency / Runtime State | `USING_URP` | `USING_HDRP` | `UseHdrpPath` | Resulting Adapter Execution Path |
|---|---|---|---|---|
| **1. URP-Only Installed (Current Gate 2A)** | `true` | `false` | `false` | Compiles only URP types; executes URP presentation path. |
| **2. Both Installed · URP Active in `GraphicsSettings`** | `true` | `true` | `false` | Compiles both via type aliases (`UrpTonemapping` vs. `HdrpTonemapping`); `UseHdrpPath` is `false`, so 100% of URP presentation behavior runs with zero duplicate volume/camera writes. |
| **3. Both Installed · HDRP Active in Stage/Quality** | `true` | `true` | `true` | Compiles both; `UseHdrpPath` is `true`, executing HDRP camera/volume/shader paths and returning before any URP fallback. |
| **4. HDRP-Only Installed (Future Post-Migration)** | `false` | `true` | `true` | `#if USING_HDRP && !USING_URP` forces `UseHdrpPath = true`; compiles and runs without any URP assembly dependency. |

> **Critical Assembly Reference Rule:** Adding `versionDefines` (`USING_HDRP`) does **not** automatically add an assembly reference when `"overrideReferences": false` with explicit `"references"` is used in `.asmdef`. Therefore, `"Unity.RenderPipelines.HighDefinition.Runtime"` is added to `"references"` in `LastSignal.Runtime.asmdef` **atomically in Step 1 of Gate 2B** at the exact same time `"com.unity.render-pipelines.high-definition": "17.5.0"` is added to `Packages/manifest.json` (and never before, so Gate 2A URP-only compilation has zero missing-assembly errors).

---

## 6. Gate 2B Deliverable D — Project-Owned HDRP Settings Specification

When Gate 2B is approved and package resolution succeeds, project-owned HDRP configuration assets will be created under `Assets/LastSignal/Settings/Rendering/HDRP/` **without** assigning them to `GraphicsSettings.asset` or `QualitySettings.asset` (`HDRP_GLOBAL_ACTIVATION_NOT_APPROVED`):

| Setting Category | Target Asset / Field (`PC_HDRPAsset.asset`) | Configured Value | Rationale |
|---|---|---|---|
| **Asset Paths** | `Assets/LastSignal/Settings/Rendering/HDRP/PC_HDRPAsset.asset`<br>`Assets/LastSignal/Settings/Rendering/HDRP/HDRenderPipelineGlobalSettings.asset`<br>`Assets/LastSignal/Settings/Rendering/HDRP/DefaultHDRPVolumeProfile.asset` | Project-owned assets created via Unity HDRP Editor API | Isolates all HDRP configuration from `Assets/LastSignal/Settings/Rendering/PC_RPAsset.asset` (URP). |
| **Lit Shader Mode** | `currentPlatformRenderPipelineSettings.supportedLitShaderMode` | `Both` (or `ForwardOnly` during initial parity test) | Supports deferred decals/lighting while allowing forward foliage transmission and MSAA comparison. |
| **SRP Batcher** | `useSRPBatcher` | `true` | Mandatory for CPU render-thread batching efficiency. |
| **Volumetrics** | `supportVolumetrics` / `increaseResolutionOfVolumetrics` | `true` / `false` (Medium froxel budget) | Enables volumetric fog and golden-hour light shafts through forest canopy without 4K froxel cost. |
| **Volumetric Clouds** | `supportVolumetricClouds` | `true` (Available in FrameSettings; disabled on secondary cameras) | Available for overcast/storm validation alongside `TownSky` panorama fallback. |
| **Decals** | `supportDecals` / `supportDecalLayers` | `true` / `false` (`maxDecalDistance = 50m`) | Enables asphalt cracks, road shoulder transitions, moss, and blood/chop decals. |
| **Subsurface Scattering & Transmission** | `supportSubsurfaceScattering` | `true` (Thin-object transmission for foliage) | Essential for backlit pine needles and fern fronds. |
| **Dynamic Shadows** | Directional: 4 cascades, `60m` max distance<br>Punctual: bounded to match `TownAtmosphere` (`localLightBudget = 12`, max `2` shadowed within `30m`) | Controlled shadow atlas (`2048–4096`) | Prevents shadow-map VRAM and GPU frame-time blowups at night/rain. |
| **Deliberately Disabled Features** | `supportRayTracing = false`<br>`supportSSGI = false`<br>`supportWater = false`<br>`supportMotionVectors` (camera/object only where required) | **DISABLED** | Hardware Ray Tracing is unsupported on macOS Apple Silicon dev host and exceeds our 16.67 ms budget; SSGI and Water are unnecessary for initial forest slice validation. |
