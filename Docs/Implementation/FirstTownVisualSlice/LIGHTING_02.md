# Lighting 02 — 17 October 2026

**IMPLEMENTED:** source integration and the existing stage's lighting profile. **NOT_RUN:** Unity import/compilation verification, renders, gameplay, tests, builds and benchmarks. **PARTIAL:** visual acceptance until the owner supplies current captures. No PASS is claimed.

## Targeted audit

The saved stage contains 43 Light components: 2 directional, 35 spot and 6 point lights. `TownAtmosphere` references the sun, moon, the existing global ACES volume, rain presentation, 12-light local budget and a bounded runtime reflection probe. `WorldTimePresentation` delegates to this director when bound; it returns before its fallback environment code. The legacy S013 presentation script is absent from the stage. WorldClock/WorldSimulation remain the time/weather authorities.

The existing `TownVolume.asset` contains ACES, fixed post-exposure, contrast 4, bloom 0.12/threshold 1.2 and vignette 0.08. It is shared and was not edited. The director already clones its volume and sky for the session. No auto-exposure system was added.

The source had a day-to-night ambient interpolation with no separately authored blue-hour state. Direct sunlight was multiplied by solar altitude and cloud/rain attenuation, reducing low-sun illumination further. Exterior fixtures shared one activation curve and were selected purely by distance. The comparison window duplicated lighting/material formulas, increasing the risk of preview/runtime disagreement. These are code/data findings; the new reference images are art targets, not current stage captures. Exposure clipping or silhouette quality has not been measured from a new Unity render.

## Implemented refinement

- The existing stage `Lighting.asset` now opts into four smoothly blended solar-altitude phases: daylight, sunset, dusk and night. Legacy/null profiles retain the earlier presentation. No new controller, scene or clock was introduced.
- Daylight: stronger directional/skylight balance at the existing morning camera time, cooler sky fill, shadow strength 0.78, restrained contrast and reduced sky exposure. These are calibration choices awaiting visual review, not verified exposure results.
- Sunset: warm key with cool ambient/fog, shadow strength 0.84 and gradual practical-light activation. Solar orientation still comes from the authoritative time. The key is smoothly extinguished at the horizon.
- Dusk: separate cool ambient, moon fill and exponential-squared depth haze; the foreground stays less fogged than distant layers. The sunless existing panorama supplies blue hour with a restrained horizon afterglow, avoiding a photographed sunset disc remaining above the horizon. No volumetric fog/shafts are claimed.
- Practical hierarchy: existing `Gas forecourt`, `service-shop interior`, `Gas approach`, `Shelter warm light`, `Safehouse gate`, `Safehouse yard` and `Checkpoint` receive explicit color/gain/phase levels/selection priorities. The 12-light, 100 m selection bound and maximum two nearby practical shadow casters remain. Inactive objects do not consume the calibrated light budget. No new lights or prefabs were added.
- Existing child `Fixture` emission is controlled with preserved MaterialPropertyBlocks. Its phase-driven glow remains visible when the corresponding real-time light is culled by the budget, keeping distant landmarks from losing their visible bulbs. Fixtures without an existing emission-enabled material are left alone. Gas-sign artwork/materials were not replaced or converted into invented emissive signs.
- Session/preview volume clones receive fixed exposure between 0 and +0.10 EV, phase contrast/bloom, bloom clamp 8, and vignette 0.05. The existing shared volume asset stays unchanged. There is no adaptive exposure/pumping algorithm.
- Asphalt keeps dry smoothness 0.18 and receives a 0.46 wet target. Added wet gloss/darkening is bounded separately from puddles, preserving existing higher dry gloss where present. Rain and drying history still come from the original simulation logic.
- Runtime and the owner-operated comparison window now share sun/ambient/fog, sky, post, materials and practical-light samplers. Light colors, strengths, transforms and fixture property blocks are restored after preview/session teardown.

## Changed assets and files

1. `Assets/LastSignal/Scripts/Runtime/Atmosphere/TownLightingProfile.cs` — optional phase calibration and exact named practical overrides.
2. `Assets/LastSignal/Scripts/Runtime/Atmosphere/TownAtmosphere.cs` — integration and shared samplers; session/preview restoration.
3. `Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538/Lighting.asset` — actual enabled calibration used by the already-saved stage; original GUID retained.
4. `Assets/LastSignal/Art/FirstTownAtmosphere/TownSky.shader` — opt-in twilight blending; `_Twilight=0` preserves legacy behavior.
5. `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceVisualReview.cs` — dusk/damp-dusk conditions, shared sampling, richer evidence and optional archived-lighting A/B.
6. `Assets/LastSignal/Art/FirstTownVisualSlice/LightingReview/BeforeLighting.asset` plus its new folder/asset metadata — pre-overhaul stage lighting retained for comparison on the same refined geometry.

Documentation/evidence are listed in `lighting-02-changed-files.txt`. No scene object/reference/transform was rewritten. The original/staged scenes, terrain, prior four refined meshes, sky material, shared volume and WorldClock/WorldTimePresentation files match their pre-pass hashes. The original scene does not opt into the new profile. This changes the existing stage through its already-linked calibration asset; the raw EditMode scene is not continuously driven by a new preview MonoBehaviour.

Reused assets: existing `Sky.mat`, sunless `FS003_Day_Sunless.png`, `kloppenheim_01_puresky_4k.exr`, `qwantani_sunset_puresky_4k.exr`, `qwantani_night_puresky_4k.exr`, `RainSnowCloudEffect/Textures/T_CloudLayer.png`, `TownVolume.asset`, `Fixture.mat`, and the stage's `Asphalt.mat`. Their texture/material source contents were not edited in this pass. All are existing project assets; no download or new third-party dependency was used.

## Minimal owner actions

Keep the existing **FirstTownCore_VisualSlice**. Allow Unity to import changed code/assets; use **Assets → Refresh** only if automatic refresh is disabled. **Do not create/regenerate a stage, reinstall atmosphere, bake navigation or promote for this review.**

Open **Last Signal → First Town → Visual Slice → 2 Visual comparison studio**. The existing fixed pose profile loads automatically. Leave **Archived lighting comparison** off for the new treatment. Press **Capture selected comparison + settings manifest** for this initial six-frame set:

| View | Condition | Assess |
|---|---|---|
| 04 Gas station | Day 08:20 | Sky highlights, canopy shade, forecourt/vehicle readability |
| 04 Gas station | Sunset 17:40 | Warm key, cool fill, long shadows, early practical hierarchy |
| 04 Gas station | Dusk 18:30 | Canopy focal point, road silhouettes, blue-hour depth |
| 01 Safehouse exterior | Dusk 18:30 | Warm refuge against readable cool surroundings |
| 02 Forest trail | Day 08:20 | Trunk/leaf/ground forms in shade, readable path |
| 03 Roadside approach | Sunset 17:40 | Perspective, utility/road silhouettes and distant haze |

For a controlled before/after lighting pair, enable **Archived lighting comparison** and capture the same view/condition once. It temporarily samples the archived stage calibration on **the same current geometry**, then restores the current profile. It is not an older screenshot or a replacement scene. Baseline and current file names are distinct. No before/after renders have been created by the assistant.

PNG/JSON output: `Docs/Implementation/FirstTownVisualSlice/Evidence/Captures/`. Manifests include lighting-profile hash, variant, sun/moon intensity, shadow strength, fog, exposure and enabled-practical count. Send the six pairs first, or the first import/Console error. No repeat of stage generation is needed.

After that review, the remaining acceptance matrix is views 01/02/03/04 under all three primary conditions, plus 04 **Damp dusk 18:30 (wetness only)** and night/overcast/rain checks where necessary. Damp dusk is a material/lighting sample; it does not simulate rain particles or change save data.

For runtime acceptance, use the already isolated review save slot, then inspect the same first-person route and natural clock/weather transitions. Verify pause/load restoration, indoor/outdoor readability, actual rain/roof response and practical selection while moving. The director's existing 30-second runtime reflection update remains; preview captures do not refresh the reflection probe and explicitly report that limitation. Fresh dynamic reflections, actual shadow performance and transition appearance require owner verification. No global test/build/benchmark request is needed just to inspect the initial six frames.

## Evidence and promotion safety

`Evidence/Lighting02/before-files.zip` preserves the five changed source/asset files before this pass. `before.json` and `implementation-evidence.json` record exact hashes and the saved scene light inventory. Source delimiter/YAML/reference inspection was performed; it is not a Unity compilation or test result.

The generated lighting asset's old generation hash now differs **intentionally** because of this authorized calibration. This is recorded; its saved generation hash was not silently rewritten. The earlier source-scene and generated-Terrain differences still require reconciliation. Regeneration safeguards remain in place, including the prior render-mesh refinement protection. Promotion remains unauthorized; source reconciliation and explicit visual/gameplay acceptance are still required.

Remaining limits: no current rendered evidence establishes clipped highlights, black crush, final haze density or reference matching; exponential fog is not volumetric lighting; existing asset silhouettes and practical fixture coverage constrain the result; light-budget selection changes, URP shadow quality, shader import and wet reflections are unverified. Performance was not measured. The appropriate status is IMPLEMENTED with visual/runtime acceptance pending.
