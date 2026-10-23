# 10 — PERFORMANCE BUDGETS & VISUAL QA ACCEPTANCE SPECIFICATION (PHASE H / M10)

**Date:** 2026-10-20  
**Status:** `GATE_3_PASS` (Automated HDRP Compilation, 17/17 Shaders, 966 Materials, and 141/141 Automated EditMode/PlayMode Tests `PASS` under Active HDRP 17.5.0 · Windows Target Standalone Benchmark: `NOT_MEASURED` · Owner Interactive Visual Signoff: `PENDING_OWNER_VISUAL_REVIEW`)

---

## 1. Hardware & Frame-Time Budget Hypotheses

Per Section 11, performance numbers are **hypotheses to be verified on target hardware**, never pre-certified claims. Mac Apple Silicon (`M3 Pro`) Editor captures are development diagnostics only and are strictly separated from Windows PC Standalone player measurements.

| Metric | Target Budget (Windows PC 1080p Standalone Release Player) | Mac Dev Host Diagnostic Reference (M3 Pro 1080p Player/Editor) | Fail / Investigation Trigger |
|---|---|---|---|
| **Target Frame Rate / Mean Frame Time** | `>= 60 FPS` (`<= 16.67 ms` mean & `p50`) | Record baseline vs. candidate delta | Mean `> 16.67 ms` |
| **p95 Frame Time** | `<= 16.67 ms` (Sustained traversal) / `<= 18.5 ms` (Heavy rain + active tree fall) | Record `p95` (S004 Pine Editor baseline was `17.309 ms`) | `p95 > 20.0 ms` |
| **p99 Frame Time** | `<= 25.0 ms` | Record `p99` | `p99 > 28.0 ms` or visible hitches |
| **CPU Main Thread / Render Thread** | `<= 10.0 ms` Main / `<= 8.0 ms` Render | Record via `ProfilerRecorder` | CPU-bound stall or per-tree C# Update loop |
| **GPU Frame Time** | `<= 14.5 ms` @ 1920×1080 | Record via `GPU Frame Time` recorder | Alpha-overdraw or volumetric fog blowup |
| **Physics.Simulate (During Tree Fall)** | `<= 1.5 ms` (1 falling tree) / `<= 3.5 ms` (4 concurrent active trees max budget) | S004 1-tree Editor mean was `0.144 ms` | Dynamic non-convex MeshCollider or runaway contacts |
| **Draw Calls / Batches / SetPass** | `<= 1,500` Batches / `<= 250` SetPass calls (SRP Batcher + GPU Instancing active) | Record via `Draw Calls Count` / `Batches Count` | Broken SRP Batcher compatibility or material cloning |
| **Rendered Triangles** | `<= 2.5M` tris per frame (Camera + Shadow cascades combined) | Record via Profiler | Missing geometric LODs or unculled shadow casters |
| **Per-Frame Managed GC Allocation** | `0 B` steady-state traversal (`< 1 KB/frame` average) | Record via `GC Allocated In Frame` | Per-frame LINQ/array/material allocations |
| **VRAM / Texture Memory** | `<= 3.5 GB` total GPU residency (`<= 1.8 GB` textures at High tier) | Record `TextureRuntimeBytes` & `Total Used Memory` | Uncompressed 4K EXR/PNG vendor textures used raw |
| **Shader / VFX Compilation Errors** | `0` errors, `0` pink/fallback materials | Verify in `unity.log` | Any missing shader pass or compilation error |

---

## 2. Ten Mandatory Benchmark Scenarios (Section 11)

Every pipeline (URP vs. HDRP) and Hero Slice build must be profiled across these exact 10 scenarios (20 seconds steady-state after 5 seconds warmup):

| # | Scenario ID | Time / Weather | Gameplay / Scene Activity | Primary Bottleneck Tested |
|---:|---|---|---|---|
| **1** | `B01_DenseForest_Daylight` | Day `12:00` · Clear | Looking through dense Layer 1–5 conifer woodland (`50–100 m` sightline) | Foliage alpha overdraw, shadow cascades, LOD transitions |
| **2** | `B02_ForestEdge_Roadside` | Day `09:30` · Clear | Safehouse compound exit → road verge → gas station canopy view | Mixed architecture + terrain splat/decal + forest ecotone |
| **3** | `B03_GoldenHour_Shafts` | Sunset `18:15` · Clear | Low-angle sun through conifer trunks (backlit needles + volumetric shafts) | Volumetric fog / light shafts + foliage subsurface transmission |
| **4** | `B04_Overcast_Woodland` | Day `14:00` · Overcast (`clouds >= 0.75`) | Diffuse sky ambient probe + soft canopy AO | Ambient spherical harmonics / sky lighting & bark relief readability |
| **5** | `B05_HeavyRain_Wetness` | Dusk `17:30` · Rain (`rain = 1.0`, `wet = 1.0`) | 2,154 active rain particles + 23 wet surface material responses + 12 puddles | Particle collision, wet specular/SSR, transparent sorting |
| **6** | `B06_Night_Flashlight` | Night `23:00` · Clear/Rain | Player spot flashlight (`shadows = Soft`) + nearest 12 practical lamps (`2 shadowed`) | Local shadowed punctual lights + normal map micro-relief |
| **7** | `B07_RunningThroughVegetation` | Day `11:00` · Clear | Continuous sprinting along 120 m forest trail through ferns/understory | Near-plane alpha overdraw (fullscreen frond fill-rate) & LOD crossfade |
| **8** | `B08_TreeChopping_And_Falling` | Day `13:00` · Clear | 8 axe strikes (`16-level` notch mesh swaps) → `FixedJoint` break → dynamic fall → impact/splinters/rest | `Physics.Simulate`, compound capsule contacts, particle bursts, audio pool |
| **9** | `B09_Vehicle_Near_Forest` | Day `15:00` · Rain | Driving pickup (`VehicleActor`) at speed along western woodland bend | Terrain collider physics, camera motion, dynamic LOD streaming |
| **10** | `B10_ZombieChase_Woodland` | Dusk/Night · Flashlight | 3–5 active zombies (`NavMeshAgent` + `ZombieLocalizedWounds`) pursuing through trees | AI pathfinding around trunks/fallen logs + skinned mesh + wound shaders |

---

## 3. Visual Acceptance Matrix (`V01` – `V12`) & Signoff Criteria

No visual gate is ever marked `PASS` by the agent. The owner reviews matched 1920×1080 Game View screenshots (fixed camera transform, FOV `65°`, timestamp, weather) against these 12 criteria:

1. **Cohesive Material World:** Trees, terrain, rocks, cabins, gas station, vehicle, zombie, and FPS axe belong to one grounded PBR universe (no cartoon green grass next to photogrammetry bark).
2. **Credible Conifer Scale & Ecology:** Mature trunks (`18–26 m` tall, `0.4–0.8 m` diameter) with natural age distribution (mature canopy → sub-canopy → saplings → ferns → duff), free of grid rows.
3. **Close-Up Camera Integrity (`1.5–2.0 m` Eye Level):** Zero flat paper-cutout leaf cards cutting across the trunk; deep bark fissures with crisp normal/roughness response under both sun and flashlight (`V03`).
4. **3D Forest Floor Depth:** Layered ferns (`fern_02`), pine-needle clusters, mossy stumps (`tree_stump_01`), deadwood, and stones eliminate flat terrain stretches (`V04`).
5. **Organic Road & Structure Integration:** Graded road shoulders, asphalt edge cracks, gravel/mud transitions, and foundational weeds/debris anchor the road and safehouse into the earth (`V01`, `V02`).
6. **Atmospheric Depth & Separation:** Distance mist / volumetric fog naturally separates foreground trunks (`0–15 m`), mid-ground woodland (`15–60 m`), and distant tree lines (`60–150 m+`) (`V05`).
7. **Measurable Pipeline Value:** If HDRP is tested, side-by-side captures with URP at identical camera/weather coordinates must demonstrate unmistakable gains in volumetric light shafts, canopy transmission, and surface contact fidelity.
8. **All-Weather PBR Coherence:** Wet surfaces darken according to porosity (soil/bark darken more than metal; wet road reflects sky without turning trees into chrome) (`V06`, `V07`).
9. **Physical Notch & Fall Readability:** Each axe hit visibly deepens the 3D V-notch with believable interior wood grain (`V09`), and the felled trunk rotates under gravity, collides with terrain/obstacles, and remains permanently visible on the forest floor (`V10`, `V12`).
10. **Survival Gameplay Readability:** Approaching zombies remain readable at encounter range in dusk/rain/flashlight without being swallowed by crushed blacks or excessive fog (`V08`).
