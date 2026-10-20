# LAST SIGNAL — Grounded Realism Art Bible v1.0 (production standard)

## 1. Identity

**Creative statement:** *A believable abandoned rural settlement at the edge of a hostile, damp, overgrown forest. Human structures show purpose and history; nature advances without decorative randomness. The shelter feels built and warm; the wild feels wet, deep, unpredictable and dangerous.* Inspirations are visual analysis references only. Distinctive architecture, story and asset ownership remain Last Signal's.

## 2. Visual pillars

1. **Natural truth:** shape and density follow ecology, soils, drainage, slope, disturbance and season. No regular grids, identical crowns or uniform green carpet.
2. **Physically plausible materials:** albedo carries color, normals microform, roughness variable by substance and wetness. Avoid baked highlights, exaggerated fake AO, broad painted brush strokes and crushed black.
3. **Scale fidelity:** player eye ~1.6–1.8m, building/road/trunk ratios credible; rooted contacts; avoid giant grass, miniature roads and floating boulders. Exact eye height verified in live controller.
4. **Environmental storytelling:** erosion, tire wear, repeated foot traffic, repaired gates, leaking roofs, cleared paths, debris caught at fences; not random uniform damage.
5. **Readability amid richness:** threats and actionable objects remain discernible while foliage retains believability; fog should evoke distance, not cover design problems.
6. **Weather integration:** wetness changes material response and atmosphere without introducing a second time/weather simulation.
7. **Coherent full game:** tree/bark/road/structure/weapon/zombie texture and lighting frequency within one standard, not a photogrammetric forest around cartoon zombies.

## 3. Palette/lighting guidance (not fixed albedo targets)

- Region: cool muted conifer canopy, organic browns, gray stone, wet asphalt, moss. Shelter: limited tungsten/amber warmth. Hazard colors localized to identifiable context; no global cyan-green tint.
- Baseline at neutral midday: retain midtone separation and physically coherent specular; no global over-saturation or crushed shadows.
- Clouds/fog: restrained distance and height response; foreground always readable at gameplay threat ranges. Emission cannot wash out rain or flashlight.
- Use neutral reference calibration material spheres and known exposure; validate daylight, overcast, rain, dusk, flashlight/night with fixed screenshots.

## 4. Material families and consistency

| Family | Surface expectations | Wetness target | Common rejection |
|---|---|---|---|
| Tree bark | continuous plausible bark relief, no seams, radius/height variation | darker, slightly lower roughness; no chrome bark | painted bands, repetitive UV seams |
| Leaves/needles | physically scaled clusters, alpha clipping/TAA control, layered branch hierarchy | subtle value/roughness shifts | opaque cardboard crowns, edge halos |
| Forest floor | soil, needles, duff, twigs, moss, exposed roots, gravel; linked transitions | damp darkening and restrained shine | uniform neon-green carpet |
| Rock | terrain-embedded weathered form, slope/bedrock consistency | darker crevices, mild sheen | loose random boulders, uniform gloss |
| Asphalt/concrete | lane wear, crack/patch distribution, road shoulder blending | specular and puddles by drainage | slab pasted onto dirt |
| Timber/metal | scale-consistent roughness, directionally correct scratches, rust at plausible contacts | material-specific response | every surface uniformly wet/rusted |
| Skin/cloth/weapons | realistic relative skin/cloth/metal response for FPS/TPS | no universal smoothness | glossy cloth, toy-like weapons |

Texture standards are **benchmarked hypotheses**, not universal 4K mandates: environment texel density baseline 256–512px/m where practical, hero close-ups selectively higher; texture budget based on camera occupancy and GPU residency. BaseColor sRGB; normals/roughness/metal/height packed linear as appropriate. Convert OpenGL/DirectX normal orientation if needed; audit channels. Prefer tileable textures/trim sheets, instance material palette, shared atlases for foliage.

## 5. Trees and vegetation: appearance acceptance

- Minimum hero canopy **3 visibly distinct** archetypes or high-quality authored variants with age/height/crown/branch asymmetry; **candidate count, not blind quantity**. Aim for young/juvenile, intermediate and mature stages.
- Trunk close-up at 1–2m has meaningful bark surface, not flat color. Leaf clusters avoid obvious cross-plane duplication at 2–5m.
- Forest vertical strata: canopy, secondary trees, understory, herb layer, litter/debris. Coverage follows habitat masks; preserve negative space and readable movement corridors.
- Ground base blends into road shoulders, roots, path wear, stump and fallen branches. Foot-level and 30m canopy frames both required.
- Wind has high-level timing continuity across foliage, tree mesh passes and shadows; not every part of trunk sways equally.

## 6. Human built environment

Safehouse/gas station/town center/clinic/industrial must have coherent architectural period, materials and wear, geometry connected to actual ground. A roof has believable pitch/drainage and rain-cover behavior; gas station canopy closes visibly. Perimeter 'endless fence' is a temporary prototype boundary unless narratively supported. Final boundary method requires safe traversal and gameplay communication without visibly boxing the landscape.

## 7. Composition and environmental mood

**Hero framing recipe:** 30% navigable negative space + varied silhouettes + foreground-to-background separation + at least one authored focal landmark (not hard quota). Validate eye-level approach, not only high Scene View. Weather/time can alter contrast, never hide missing geometry. Single assets should not dominate benchmark via isolated spotlighting.

## 8. Anti-patterns (REJECT for final render)

- Fantasy painting/obvious hand-painted noise on hero trees; vivid single green terrain; repeated uniform pine silhouettes.
- Hard road-edge borders, floating structures, tiled photo textures at visibly different scales.
- 'Performance win' achieved by removing gameplay collision, enemies, cuttable trees or mandatory cover.
- HDRP conversion as substitute for art content; asset copied from Sons of the Forest; generated image presented as Unity screenshot.

## 9. Quality gates and review

Minimum gates for every production hero asset: lawful source, provenance, metric scale/pivot, URP preview, close/medium/far silhouette, material packing and color, LOD geometry evidence, alpha/shadow/wind pass fidelity, collision/interaction, wetness, 1080p neutral daylight and rainy-night screenshots, on-device cost. Gate `ACCEPT`, `ADAPT`, `REJECT` or `DEFER`, never default PASS.
