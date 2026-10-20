# Pilot hero slice: safehouse → road → forest, 120×120 m

**Goal:** establish player-height believable forest and rural edge in an isolated, reversible visual stage, not re-author whole 500m town. Coordinates are selected after local scene inspection. Do not assume user screenshot selected object's exact world bounds; use actual scene content and safeguard IDs.

## Phase 0 — protected baseline

- Record current local branch/HEAD, `git status`, Unity 6000.5.0f1/URP17.5 package lock, current source scene GUID/hash, FirstTownCore TerrainData hash, list of local `ThirdParty` packs, dirty assets and current harvesting status.
- Save source/user work; create dedicated scene copy (`Assets/LastSignal/Scenes/Validation/LS_Realism_HeroSlice.unity`) via safe authoring once user chooses to execute. Create separate TerrainData derivative and checkpoint. Avoid Unity regenerating production or overwriting staged visual slice.
- Record six eye-level baseline shots + aerial observation, per timestamp/weather; include FOV, render settings and performance device.
- Register exclusions and source-hash preconditions before any change.

## Phase 1 — art lab

- Import **only** chosen asset subset into isolated test project or controlled ThirdParty path. Preserve store content + `.meta`; create project-owned material prefabs under `Assets/LastSignal/Art/Realism/...` and log original-to-owned mapping.
- Construct neutral material lighting benchmark: bark, canopy, soil, leaf litter, gravel, asphalt, wood, metal, rock, zombie/arms; include white/gray sphere and metre reference. Inspect at 1m, 5m, 20m, 100m; day and rain.
- Reject unsupported shaders, unsupported license, alpha halo, no usable cuttable trunk, missing LOD at forest density or unreasonable memory. Do not use one isolated pack sample to declare finished asset integration.

## Phase 2 — landscape shape and surfaces

- Survey baseline terrain, road and protected foundations; sculpt modest landform derivative with drainage; no arbitrary z-shift of roads, river or saved POIs. Build authentic shoulder geometry and blend.
- Apply coherent PBR terrain masks/layers plus mapped detail surface. Preserve nav corridor and side road clearance. Ensure road collider remains drivable and properly surfaced.
- Add leaf litter/needles/deadwood/root meshes near camera, and cheaper detail farther away.

## Phase 3 — foliage layers

- Select realistic tree family and age variation, protect hero trees, scatter canopy based on deterministic clustered fields and ecological constraints. Do not simply multiply count.
- Add saplings, shade-tolerant shrubs, ferns, branches and stumps with same art standard; build real geometry LOD, impostors if needed, no per-object Update for wind.
- Keep dangerous sight lines and zombie retreat routes readable; preserve traversability and deterministic vegetation digest.

## Phase 4 — light/weather/structures

- In existing TownAtmosphere presentation path, calibrate neutral daylight, overcast, wetness and dusk, with road and safehouse contrast. Distinct wetness per vegetation/bark/road/metal; no second manager.
- Art-dress existing safehouse/forecourt edge with actual roof/foundation and grounded props. Maintain existing canopy and gas-station colliders; any staged gas-station authoring is separate, not silently merged.
- Pilot tree physical chopping mesh: visible wedge, fall, collision, harvesting, persistence. If vendor tree unsuitable, replace only that pilot asset or use a specialized source/authoring derivative; do not fake completion.

## Phase 5 — owner acceptance and release of pilot

- Fixed owner gameplay captures as in `11_QA_VISUAL_ACCEPTANCE.md`, full travel, vehicle and zombie exposure, old saves, nav bake/validation, user-run focused EditMode/PlayMode, Windows build profiling.
- Deliver artifact set: stage scene, owned prefabs/materials, exact import registry/licenses, before/after image matrix, QA XMLs, perf CSV/captures, source hash and rollback mapping. Mark status `PASS / PARTIAL / BLOCKED` with evidence. On `PASS`, owner authorizes gradual production promotion; a visual screenshot alone doesn't authorize hash override.

## Out of scope in pilot

Whole map overhaul, new NPCs/quests, procedural infinite world, complete co-op implementation, third-party vendor overwrites, immediate HDRP switch, full AI overhaul, explicit game-map copying, arbitrary license enforcement claim. Later improvement work should reuse original authored logic where safe.
