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
