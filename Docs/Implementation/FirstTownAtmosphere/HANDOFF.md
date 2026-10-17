# FirstTownCore atmosphere — implementation and manual acceptance

2026-10-16 · Unity 6000.5.0f1 · URP 17.5.0 · branch `s012-integrated-graybox-slice` · entry HEAD `9191a5cd7bd245ee52e3bd9dab11466702ac2931`.

**Overall: PARTIAL, awaiting owner visual/gameplay/performance acceptance.** Native scene authoring IMPLEMENTED. No Unity tests, PlayMode, build, bake, profiler, benchmark or performance capture run by the agent. Editor import and native authoring command compilation occurred as authoring prerequisites; these are not acceptance evidence. No PASS claim.

Open `Assets/LastSignal/Scenes/Production/FirstTownCore.unity`. Installation and polish are already saved; **do not rerun Install or regenerate the town**. Tune the `FirstTown Atmosphere` component, child lights and project-owned materials/profile in place. Explicit authoring menu commands refuse duplicate installation.

## Entry audit and decisions

The working tree was already extensively dirty: character/weapon work, FirstTown creation/persistence, Regions/S023 and user-imported weather packages. Those changes were retained. The scene initially used Unity's built-in sky, one directional sun, two cabin lights, four street lights, one local rain emitter and `ProductionVolume.asset`. That volume had two null component references on disk. The existing `S013EnvironmentPresentation` was already absent from this town. The source S013 scene and its volume were not changed.

`WorldTimePresentation` originally wrote sun rotation/intensity, ambient and rain each frame. `WorldClock` owns `WorldSimulation`, roof protection, time and restoration. Persisted weather remains 0/1 clear/rain. `ProductionAudio` already owns eight ambience/music loops, including light/heavy rain, wind, night and interior. Its shelter blend previously covered only the cabin box.

The selected PC URP asset already has HDR, depth texture, Forward+ (`m_RenderingMode: 2`), reflection probe blending/box projection and additional lights. It was not changed. No render pipeline migration or global quality change.

During installation the scene became dirty from URP adding its light-data components and serialization of the new optional fields. A copy was preserved at `Temp/FirstTownAtmosphere-before.unity`; the difference was inspected before saving. No unrelated object changes appeared in that difference. This temporary snapshot is not a permanent backup.

| Existing asset | Decision | Applied rationale |
|---|---|---|
| Fantasy Skybox FREE / FS003 Day Sunless | ADAPT_AND_USE | Day panorama without a second baked sun; sampled by owned blending shader. Other package skies remain unused. |
| `kloppenheim_01_puresky_4k.exr` | ADAPT_AND_USE | Overcast panorama, exposed as Texture2D; existing importer caps at 2048. No vendor importer modification. |
| `qwantani_sunset_puresky_4k.exr` | ADAPT_AND_USE | Dawn/golden-hour blend, azimuth follows sun; HDR exposure/alignment still requires visual calibration. |
| `qwantani_night_puresky_4k.exr` | ADAPT_AND_USE | Anchored night panorama, controlled exposure and separate weak moon directional light. |
| RainMaker | USE_AS_REFERENCE_ONLY | Its controller independently follows camera, drives rain intensity/wind and creates audio loops. That ownership is unsuitable here; no RainMaker controller or demo imported into scene. Runtime visual/performance suitability not benchmarked. |
| RainSnowCloudEffect | ADAPT_AND_USE | Existing URP rain/splash material derivatives and streak/splash/cloud alpha textures. Owned bounded emitter layout replaces package group prefabs. Snow and fog group prefabs unused. |
| ThirdParty Modular European House built-in sky candidate | REJECT for this scene | Another built-in sky path is unnecessary beside the selected URP-compatible owned shader. No broad package conversion. |
| S015AudioCatalog and existing production audio | USE | Preserve clip/bus/voice ownership; blend existing rain loops with director rain level and existing roof protection. |
| Existing normalized road/metal/wood/stone materials | ADAPT_AND_USE | 22 project-owned weather variants, scoped to 2,957 exterior renderers before puddle polish (23 materials / 2,969 renderers including the puddles). Vendor/shared source assets unchanged. |
| Existing S013 ProductionVolume | REJECT for this scene | Null component references replaced by a town-owned profile with persisted component subassets. Source asset retained. |
| Existing LightPole wrapper | USE | Mount new exterior fixtures with existing town art; no additional collision/navigation obstruction. |

These are source/authoring suitability decisions, not visual acceptance results. No new external asset was required or downloaded.

## Visual architecture and implementation

`WorldClock -> WorldTimePresentation -> TownAtmosphere` is the sole environment presentation path in this scene. The optional director reference defaults null elsewhere, preserving the legacy scene behavior. HUD text still belongs to WorldTimePresentation. Bind/refresh/unbind follow the existing session lifecycle; restoration snaps presentation to the restored simulation. No WorldClock, WorldSimulation, RainRoof, save schema, weather enum, gameplay wetness or rain damage logic changed.

- **Sky:** owned URP background shader blends four existing panoramas, adds two drifting samples of existing cloud alpha and a time-aligned day sun disc. Stars stay anchored. Day, dawn, afternoon, golden hour, overcast, rain and night are continuous blends. Overcast is a deterministic visual swell and pre-boundary cloud build-up, not a persisted state or a prediction that rain is guaranteed next. Panorama clouds remain photographic layers; this is not volumetric cloud simulation.
- **Sun/ambient:** existing sun angle remains clock-driven. Warm low sun, cloud/rain attenuation, weak cool moon and explicit ambient spherical harmonics support visibility. No per-frame GI rebake. Mood uses lighting and sky rather than a uniform blue filter.
- **Rain:** one near emitter, eight surrounding emitters and a separate impact pool. Maximum live particle capacity 2,154 (650 + 8×180 + 64). Middle tiles update their surface height at 5 Hz; collision terminates drops on static roofs/ground. Four top-down surface samples per 0.2 s place impacts on the highest hit surface. Near rain clears on roof entry; exterior rain remains visible. Wind is modest fixed lateral presentation velocity. Player/car rain protection remains authoritative in WorldClock.
- **Wetness:** material response uses the current authoritative weather interval, independent of player body wetness. Wetting takes 900 world seconds; drying takes 2,400 world seconds with a short real-time visual smoothing. Initial clear world starts dry. On load/sleep, the current interval reconstructs surface appearance; no extra saved weather state. Material variants preserve texture/normal/metal maps, with distinct darkening/smoothness targets for asphalt, metal, roof, masonry, timber and soil. Fully roof-contained renderer bounds are excluded at authoring. Runtime metallic vehicle materials are registered when the existing VehicleWorld actor appears. This is selective coverage, not all-material classification.
- **Puddles:** 12 small authored road-edge depressions, shared mesh/material, no colliders. Hidden while dry, fade in with wetness. No puddles on every surface, no screen-space reflection dependency.
- **Fog:** exponential distance haze increases at dawn/rain/night; density range approximately .0014–.0068. It is distance fog, not volumetric/local height fog. Tune against 10/25/50/100m readability during owner review.
- **Lights:** 41 total lamp entries, including the original six. Sixteen new exterior landmarks and nineteen playable-room downlights. At most twelve within 100m are enabled; at most two eligible lights within 30m cast local soft shadows, chosen by distance. Sun shadows remain URP-owned. Nearest-light budgeting is presentation-only.
- **Reflections:** one 128px realtime local probe, six faces time-sliced, no more often than every 30 active real seconds outdoors. Particle/enemy layers excluded. No baked probes/APV/lightmaps were generated. Probe costs and stale-reflection transitions require profiling; there are no per-building interior reflection captures.
- **Post:** independent persisted profile with ACES, contrast 4, bloom .12 / threshold 1.2, vignette .08, fixed exposure with a small .15-stop night readability lift. No eye-adaptation exposure pump. Main player camera gets SMAA Medium and post-processing during the bound session.
- **Audio:** existing wind, rain, heavy rain, night and interior loops retain their mix buses/voice ownership. All gameplay-recognized roofs can now attenuate rain, rather than cabin bounds only. Rain blend follows the visual director. No new storm simulation, lightning or thunder loop.
- **Lifetime:** sky, volume and wet materials are session clones. Original renderer materials, light state, camera settings and RenderSettings are restored on unbind. No renderer.material per-frame allocation and no vendor material edits. Environmental materials/lighting refresh at 5 Hz; sky/time lighting tracks presentation every frame.

## District pass

| District | Authored intent |
|---|---|
| Safehouse | Existing warm cabin/porch plus amber gate and yard pools; readable refuge |
| Gas station | Warm broad forecourt and road approach; service-room interior light |
| Town center | Shop-side pool and town-hall approach; diner/hall interior downlights |
| Industrial | Cooler loading/gate contrast and service/dispatch interior treatment |
| Residential | Restrained junction light and warm annex interiors |
| Clinic | Subtle green-white entrance and clean interior color |
| Motel | Amber court/highway approach and individual room interiors |
| School | Neutral path and playable interior lights |
| Forest edge | Local warm ranger porch; darker surrounding forest retained |
| Farmland edge | Sparse warm yard landmark, no blanket field lighting |

## Open issues / acceptance limits

1. **NOT_RUN:** owner visual acceptance across all times/weather, player camera modes and districts. Exposures, HDRI sun/horizon alignment, moon-image/light alignment and values need in-game review. Concept boards are art direction, not proof of rendered equivalence.
2. **NOT_RUN:** material classification and roof-bound exclusion need traversal review. Mixed-material exterior shells, dynamic roof collision and canopy edges can require explicit overrides. Metal-only vehicle adaptation excludes glass, tyres and nonmetallic paint. Existing roof bounds, not a new indoor model, drive protection.
3. **NOT_RUN:** runtime material swaps on the preexisting static batching, particle collision, extra-light budget and realtime probe require target-device capture. No measured FPS, memory or draw-call claim.
4. **PARTIAL:** volumetric shafts, local height fog, physically accumulated puddles, rain-on-window droplets, wind-driven foliage, dynamic cloud shadows and per-room baked lighting are not implemented. The requested core pipeline uses bounded URP presentation instead; evaluate whether these advanced effects are needed after owner QA.
5. **NOT_RUN:** all gameplay/save/session regressions. FirstTown navigation remains the previous user-owned bake task. This atmosphere pass does not bake or repair navigation.
6. No new automated tests were run or authored. The added reference validator is an explicit user-run check; existing time/persistence/play tests are supplied in the manual runner.

See `MANUAL_QA.md` for executable commands, routes and the acceptance matrix. `authoring.json` / `polish.json` record authored counts only.
