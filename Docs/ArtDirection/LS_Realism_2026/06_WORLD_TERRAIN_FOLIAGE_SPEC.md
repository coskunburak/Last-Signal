# World art specification — terrain, roads, forests and horizon

## 1. Map scope / ownership

The existing `FirstTownCore` is a 500m × 500m resident terrain according to FirstTown source audit. Keep same origin/scale and existing forest/road/river/shelter/school/gas-POI identities. The **120×120m pilot** is a staged presentation/test area, not a new streamable world with portals. Do not alter authoritative weather/navigation/loot cells while visualizing. Landform changes require a TerrainData derivative and nav rebake, with legacy saves tested for collision.

## 2. Terrain geometry

Macroform: believable local ridges, drainage channels, slight roadside crown, embankments/ditches, forest mounds and occasional exposed rock; avoid monotonic sine-wave hills. Mesoscale: 0.5–5m depressions in unconstrained forest, root plates and worn trail. Micro: 1–30cm normals/height-based shader displacement or decals **not** collider modifications that snag player. All values are artist-proposed; derive exact amplitude from surveyed local topology, not universal offsets.

Protect: cabin foundations, gas canopy roof & forecourt, road driveable profile, bridges, spawn/vehicle exit zones, world edges, stream beds, saved persistent object transforms, safe trail width. TerrainData changes must pass source-hash and clear reconciliation for any local user edits; keep original untouched.

## 3. Surface layers and physically plausible transitions

Minimum benchmark families: dark conifer duff, leaf litter, damp forest soil, gravel/stone, trampled trail dirt, sparse grass, asphalt shoulder. Not all need independent TerrainLayer; use optimized masks/decal/geometry where splat budget limits. Texture scale calibrated with metre reference and FPS camera; neutral PBR maps, not blanket darkening. Blend at roads via sculpted verge + splat + small debris; do not bury road collider or move lane widths. Wetness depends on existing weather interval with material-specific response; avoid adding separate water state.

## 4. Biome and placement system

Use a hierarchical mask stack: exclusion (hard), macro slope/height/moisture, canopy cluster density, tree age/species, understory shade, erosion/drainage, anthropogenic disturbance, trail/POI clearance, visual landmarks. Same deterministic seed + version/profile creates reproducible placement. Local artist-painted override and HeroTrees protected zone always win over generated scatter. Avoid statistically uniform placement: spatial correlation for groups, nonuniform clumps, isolated specimens and human foot/road edge disturbance.

Suggested groups: mature conifer core, mixed conifer/birch ecotone, young regeneration, sparse traveled clearing and wet lowland shrubs. Species mix is **PROPOSED** until assets match chosen geographic identity. Build distribution by age: tall canopy, mid-height trees, seedlings/understory, herbs/ferns and debris. Minimum game readability: continuous vehicle corridors, visible landmarks, interpretable zombie threat silhouettes. Tree clustering must preserve existing forest navigation marker connections.

## 5. Forest floor

Implement layered instanced meshes (litter clumps, fern, moss, low bush, roots) with bounded scatter and fade distance. Important: high-quality texture alone does not create believable depth. Integrate 3D leaf/needle clusters near paths and hero tree bases, with cheaper shader/terrain detail farther away. Prevent overdraw 'walls' in FPS line of sight. Footprint and texture memory depend on quality tier. For fallen logs, distinguish purely decorative from interactable salvage; ownership must be explicit.

## 6. Canopy and wind

All tree prefabs require true mesh LOD tiers (not merely fade-to-cull); impostor/billboard if 100–200m silhouettes show unacceptable popping. Materials obey SRP batch/instancing profiles; test one rendering submission strategy at a time. Wind phase comes from authoritative presentation bridge, with trunk stiffness near base, amplitude toward tips, branch distribution and coherent shadow/depth deformation. No independent per-tree update. New colliders should be simple and correct, never triangle-for-triangle meshes by default.

## 7. Roads and built edge

PBR material, edge cracks, drainage, deposits and vegetation ingress; side path obeys existing vehicle traction/collision, not visual shader alone. Existing gas-station canopy staged work is separate and remains guarded. Large enclosing fence must be treated as temporary world-boundary visual debt unless narrative/level design specifically justifies it. Alternative final boundary concept: terrain slopes, river/ravine, impassable structures and narrative edge dressing with clearly communicated limits. No invisible player blockers as surprise.

## 8. Generated art ownership

`GeneratedEnvironment` owns only tagged generated surfaces and vegetation in staged scene; `HeroTrees` and user-authored structures are protected. Material/prefab source variants are project-owned. Deterministic authored results need manifest counts, seed, terrain source SHA, placed-ID digest, operation log and restore checkpoint. Regeneration must be idempotent and fail on unexpected geometry or changed source hash.
