# RFC — URP vs HDRP decision, not a migration order

**Current:** Unity 6000.5.0f1 and URP 17.5.0 in `Packages/manifest.json` on `main`. Existing production scene has custom `TownAtmosphere`, URP material/wetness variants, forest wind/foliage shader derivatives, Forward+ renderer path and custom sky blend. **Decision:** URP remains shipping baseline; HDRP trial is experiment, not implemented or approved.

## Key technical reality

- URP and HDRP cannot be active as a combined production render pipeline. Unity's feature sets, lighting, volume and shader APIs differ; scene appearance changes during conversion. Rebuilding atmospheric lighting and shader graph behavior is not a material checkbox.
- HDRP tends to provide a richer out-of-box route for volumetric fog, clouds, physical sky, advanced material/light features on capable desktop GPUs, but that does not give ecological placement, quality meshes or realistic tree cutting.
- URP can deliver a high-fidelity environment with high-quality assets and appropriate shaders/lighting; complex volumetrics might need custom code, vendor plugins and benchmarking.
- Metal supports HDRP on macOS compatible hardware, but macOS M3 Pro benchmark is **not** evidence of Windows desktop Steam performance. Not all high-end features, in particular DXR hardware ray tracing, translate to Mac Metal.

## Pipeline-neutral asset architecture

Make source geometry, textures, cut-cap topology, LOD meshes, collider data, persistent IDs and scatter profiles independent of any pipeline. Derive separate project-owned URP and HDRP materials/shaders per *isolated project*, mapping basecolor, normal, metallic/smoothness, AO, masks, transparency/alpha cutoff, wetness and wind. Separate visual implementation from authoritatively simulated game weather and damage. Do not write production code that directly hard-wires `UnityEngine.Rendering.HighDefinition` in URP assembly.

## Controlled A/B experiment — 120×120m

**Preconditions:** freeze 1 Git commit and 1 scene snapshot; preserve owner's dirty tree; define the same asset set and seed, camera intrinsics and route. Use separate Unity projects or clone; do not install HDRP into active repository.

1. URP arm: existing 17.5 baseline + same realistic meshes, textures and density; best credible URP implementation, not artificially default unlit.
2. HDRP arm: same geometry/scatter, converted equivalents, analogous physically plausible exposure/color grade and weather states; record conversion labor and feature parity gaps.
3. Camera matrix: eye-level 1.6–1.8m; 2m bark close-up; 15m forest entry; 50m route; safehouse/road view; dense cluster; noon, overcast, wet dusk, night flashlight and rain. Use identical fixed camera transforms/FOV/resolution/seed/weather timestamps.
4. Representative content: 3 tree variants, undergrowth, debris, 1 cuttable tree, 1 road shoulder, 1 roof wetness area, 1 zombie and FPS arms, 1 vehicle. Maintain equal collider/nav/gameplay footprint.
5. Capture release-equivalent Windows player and Mac dev machine separately. VRAM, CPU/GPU p50/p95/p99 frame time, draw calls, overdraw, material/lighting conversion effort, artifact counts and quality differences.
6. Score by owner-approved reference images and performance, not aesthetic claims by the implementer.

## Decision gate (proposal, requires target GPU definition)

| Gate | Go for HDRP | Keep URP |
|---|---|---|
| Quality | owner identifies repeatable, material gain across >=3 fixed scenes, absent or impractical in URP | difference small / tuned URP comparable |
| Performance | on declared minimum target Windows GPU, release player 1080p target p95 <=16.67ms with representative density; p99 and VRAM also acceptable | HDRP exceeds budgets or relies on reducing gameplay truth |
| Feature parity | weather/clock, persistent IDs, cutting, roof audio, save, nav and vehicle traversal unchanged | any mandatory regression unresolved |
| Schedule | conversion work explicitly estimated, scoped and acceptable | high-risk shader / VFX / vendor incompatibility |
| Rollback | separate branch/project, captured baseline, no irreversible vendor/source mutation | no reproducible comparison |

Do not announce HDRP selection before owner signoff. If HDRP wins, create separate migration ADR and multi-sprint plan; list incompatible shader/material/terrain/atmosphere/post/VFX dependencies with per-item time. Original URP branch remains valid fallback.

## Feature and evidence caution

Features vary with Unity minor/package versions, render settings, hardware and quality profile. Use the exact Unity 6000.5 documentation at time of test, not a blog screenshot or a latest-version comparison without checking compatibility. Relevant Unity official links: https://docs.unity.com/en-us/engine/6000.5/manual/render-pipelines/choose-a-render-pipeline/pipeline ; https://docs.unity.com/en-us/engine/6000.5/manual/render-pipelines/choose-a-render-pipeline/feature-comparison ; https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/cross-platform-features/graphics-apis/metal/requirements-and-compatibility .
