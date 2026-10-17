# Design, source registry and generated outputs

## Sources

Base vendor: `Assets/ThirdParty/Environments/FantasyEnvironments/Environments/`.
Approved normalized family: `Assets/LastSignal/Art/S013/Production/Prefabs/`.

| Source | Native inspection | Choice |
|---|---|---|
| `Pine.prefab` → vendor `Ambient-Occlusion-Trees/Prefabs/Pine_tree1.prefab` | 1 mesh, 1,554 triangles, 2 materials; local bounds approx 11.66×19.38×11.84m; capsule h4/r.65; 2048 bark, 1024 clipped leaves | ADAPT_AND_USE; uniform scale .62–.94 |
| `Birch.prefab` → `Ambient-Occlusion-Trees/Prefabs/Birch_tree1.prefab` | 1 mesh, 1,192 triangles, 2 materials; approx 12.52×13.56×12.64m; capsule h4/r.26; 2048 bark, 1024 clipped leaves | ADAPT_AND_USE; uniform scale .60–.92; more birch near lowland |
| `Bush.prefab` → `Prefabs/Bush1.prefab` | 54 triangles, one clipped B_Plant material, 2048 texture, no collider | ADAPT_AND_USE; .5–.88 scale, below face-level understory |
| `Rock.prefab` → `Prefabs/Rock1.prefab` | 658 triangles, one material, 2048 texture, box collider | ADAPT_AND_USE; .16–.38 scale; static wetness shader variant |
| `Grass.prefab` → `Prefabs/Grass1.prefab` | 4 triangles, one clipped B_Plant material, no collider | ADAPT_AND_USE; single-mesh terrain detail prefab, instance-count scatter |
| Pine bark submesh only | Reuses existing mesh indices, normals, UV and bark material | ADAPT_AND_USE; small collision-free fallen debris, no new modeled logs claimed |
| `TerrainLayers/layer_ground_{grass,soil,road,rock}.terrainlayer` | Existing Fantasy diffuse textures; vendor layers inspected, no normal maps fabricated | ADAPT_AND_USE; owned copies at 4m tiling (rock 6m) |
| Silver Cats / Holotna / other plant families | Existing comparisons do not establish a mixed-family approval | DEFER; not added merely for quantity |
| Bespoke leaf litter, roots/stumps, reduced LOD/impostor assets | No approved family match established in this focused audit | PARTIAL; no white placeholder or unverified external asset |

All selected S013 material instances are URP Lit and GPU-instancing-enabled. Initial source LODGroups have a single renderer tier with cull threshold .018 and no fade; they do not reduce geometry. New derivatives retain source topology, enlarge bounds for <=.12m local wind and use URP-supported crossfade to cull. Rock/log variants disable wind. Shadow casters remain limited by the existing 50m URP shadow distance; bushes/grass do not cast shadows. No automatic decimation or invented LOD savings.

Shader sources are adapted from the **locally installed URP 17.5** Lit/Terrain Lit passes. `Shaders/UNITY_LICENSE.md` preserves the package license notice (Unity Companion License). Vendor content is untouched. Unity package updates require reviewing these derivatives. This audit does not certify third-party commercial licenses; preserve the project's existing license acquisition records.

## Terrain and zones

The terrain object and collider ownership stay in the scene; only their TerrainData reference moves to a generated derivative. Heights outside managed forest regions and inside existing structure/road protection are retained. Repeated generation rebuilds managed relief from the retained baseline rather than adding hills again. Owned changes cover west x<-140 and north z>198, with soft influence boundaries; the tile edge and river channel remain unchanged.

Landforms: southwest woodland shoulder near (-224,-60), ranger woodland rise near (-222,174), and low northern ridge near (-32,231). Secondary noise contributes only ±.11m. Roads/trails stay on the baseline profile with 10m graded shoulders; no river ford or new bridge is invented. Layer weights use forest density, existing/new path position, slope and river proximity. Four layers keep one terrain splat group.

Zone A is a sparse edge density ramp. Zone B mixes pine/birch with clustered bushes. Zone C increases canopy density on the west/north ridges, with three explicit 10–13m clearings. Zone D follows the trail registry. Zone E keeps the existing drainage and changes lowland species/soil transitions; the channel itself excludes vegetation. Zone F reduces the northeast density to 18% of the woodland mask, retaining farmland sightlines.

The vehicle route connects the safehouse/north street via a western woodland bend; minimum authored width7m. Ranger foot loop width2.4m; northern woodland/farm route width3m. Existing north-cross-street bridge and school avenue provide the continuous cross-river connection. No portal, teleport or scene transition is introduced.

## Sampling and protection

System.Random with a fixed profile seed drives candidate order, scale and yaw; Mathf.PerlinNoise forms spatial density clusters, not uniform random density. A spatial hash enforces 7.5m default trunk spacing. Moisture and elevation influence birch ratio. Trees reject slopes>26°, bushes>28°, rocks>32°, grass>27°. Each instance is grounded from the current derivative terrain, not y=0.

Exclusions include actual roadbed/deck bounds, non-trigger physical colliders, RainRoof footprints, existing route markers/encounters/player spawn, an 18×18m vehicle spawn pad, custom artist bounds and protected hero renderer bounds. Roads/structures have additional clearance. Expanding bounds are indexed into 16m cells for bounded local queries. Canopy candidates require another 3.2m clearance; foliage is thinned near trail edges. The static validator subsequently checks body-width corridor overlaps and sampled grades; runtime vehicle and AI clearance remain manual.

Bushes cluster around accepted canopy, rocks use the same density field with their own spacing. Grass uses a separate deterministic RNG and count cap, with <=2 units per detail cell; drawing distance45m. Existing custom detail layers are preserved when compatible; incompatible scatter mode/resolution is rejected. Regeneration replaces its own grass layer only within the forest mask.

## Outputs after owner execution

`Assets/LastSignal/Art/FirstTownForest/Layout.asset` — editable seed/budgets/clearings/trails/exclusions.

`.../Generations/G<timestamp>/` — owned tree/bush/rock/grass/log prefabs, copied/compacted meshes, shared weather/wind materials, four terrain layers, terrain material and TerrainData. Fresh immutable generations make rollback possible; do not delete old generation folders while an archived scene references them.

Scene root: `FirstTown Forest — owned authoring`; replaceable child `Generated — regenerate only this subtree` contains ForestEdge, MixedWoodland, DeepForest, GroundCover, RockClusters, TrailVegetation, Natural debris, route markers, clearings and disabled cameras. `HeroTrees — artist protected` is outside the replaceable subtree. Artist edits inside Generated intentionally do not survive regeneration: move those objects to HeroTrees or another protected scene root first.

WorldClock/WorldSimulation/RainRoof/save code and encounter counts are unchanged. New clearings are potential encounter/traversal spaces, not additional enemy spawners. The single ForestPresentation component derives wind cycles from authoritative time and wetness from TownAtmosphere, restoring previous shader globals on unbind/disable. Wind normals are an approximation for small displacement; no TAA/motion-vector animation support is claimed (current scene uses SMAA).
