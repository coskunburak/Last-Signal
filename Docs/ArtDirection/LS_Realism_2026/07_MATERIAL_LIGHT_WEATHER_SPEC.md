# Materials, rendering, light and weather technical art specification

## 1. Shader/material acceptance

Target PBR pipeline independent source maps; physically plausible ranges validated under neutral light. Required material metadata: shader/RP version, albedo map and sRGB, normal import/strength, packed mask channels, metal/roughness/smoothness conversion, UV tiling in meters, cutoff/mip/alpha mode, two-sided normals, SRP batching/instancing, tint/exposure and wetness semantics. No global tint used to conceal poor source textures.

For each vendor import: create project-owned material derivatives, avoid vendor overwrites; verify texture channel flips and normal map handedness. Distinguish smoothness from roughness, linear mask sampling, cutoff mip edge stability and lightmap/static compatibility. Log GPU shader compilation warnings. Recheck shadow/depth/motion vector passes on custom foliage shaders and tree-cut stumps under sun and flashlight.

## 2. Rendering baseline

Pipeline at present URP 17.5 Forward+ (verify actual local renderer settings). Preserve existing ACES post, baseline global volume, optional SMAA, known bounded light budgets while benchmarking. Render quality improvements by evidence: physically calibrated scene exposure, directional light shadow quality, contact detail, ambient/probe handling, fog and LUT. Don't stack SSAO+contrast+vignette to hide low-quality geometry. Avoid baked shadow maps on leaves swaying dynamically unless the visual interaction is acceptable.

## 3. Environment authority map

`WorldClock -> WorldTimePresentation -> TownAtmosphere` owns simulated date/time to presented sun/sky/fog/rain/wetness. Existing `WorldSimulation` owns weather states; roofs/rain protection and `ProductionAudio` retain authority. Realistic weather visuals must be passive consumers, with no separate 'rain manager' changing gameplay state. Temporary weather override is for benchmark fixture only; it must never write player save data.

## 4. Sky/volumetric path

Existing FirstTownAtmosphere uses panoramic sky blending; it is not a volumetric cloud simulation. Current fog is exponential distance haze; not height/volume scattering. Set visual acceptance first with well-art-directed light, density and geometry. Advanced height fog, sunlight shafts, local clouds and cloud shadows are future candidates (URP custom or HDRP) to compare in isolation, with frame cost and platform constraints recorded. Don't assert shipped volumetrics.

## 5. Wetness and rain

Existing project already manages 22+ adapted exterior material variants, twelve puddle patches and bounded rain emitters per documented handoff. Treat these as **existing presentation implementation, visual acceptance pending**. All realism additions should register surface families with that same director; damp foliage should not turn into chrome. Test forest canopy cover, porch and gas canopy roof for drops appearing inside; verify audio, roof detection, first-person and vehicle view. Avoid generating per-instance materials at runtime; use shared surfaces/controlled property data.

## 6. Time-of-day matrix

| Sample | Artistic target | Fail examples |
|---|---|---|
| clear 08:00 | varied PBR colors, soft horizon, legible road | green carpet, burnt white sky |
| cloudy noon | layered crowns with nonflat shadow variation | everything equally gray, no bark relief |
| overcast/rain | material-specific wetness and forest depth | wet roof but dry soil everywhere; droplets indoors |
| golden hour | warm low sun and cold ambient balance | orange global wash; black tree trunks |
| dusk | safehouse light readable, zombie silhouette visible | extreme black crush, false fog occlusion |
| night + flashlight | contact depth and material response | shiny cloth, popped transparent foliage |

Record matched FOV/aspect/resolution; verify scene view and actual Game View independently. Owner's close-up references outrank aesthetic metrics; visual signoff remains human.
