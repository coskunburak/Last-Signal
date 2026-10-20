# 08 — REALISTIC FOREST TECHNICAL ARCHITECTURE & 120×120 m HERO SLICE SPEC (PHASE F)

**Date:** 2026-10-20  
**Status:** `PROPOSED` (To be executed in Gate 4 after Gate 2/3 pipeline & asset qualification)  
**Target Area:** 120 × 120 m bounded pilot slice inside a staged copy of `FirstTownCore` (extending the `SliceAuthoring` / `SliceSite` architecture without overwriting `FirstTownCore.unity` or `FirstTownCore_VisualSlice.unity`).

---

## 1. Hero Slice Spatial Envelope & Required Zones

Based on verified coordinates in `SliceSite.cs` (`02 Safehouse Compound` at `(-180, 0, -170)`, `S013 Production Cabin`, western woodland bend, and roadside approach), the **120 × 120 m Hero Slice** (`x ∈ [-245, -125]`, `z ∈ [-215, -95]`) encompasses all 7 required micro-environments:

1. **Safehouse Approach:** Cabin porch, compound yard, and perimeter gate (`(-180, 0, -170)`).
2. **Roadside & Graded Verge:** Drivable roadbed with 10 m graded shoulder, ditch/drainage depression, cracked asphalt edge decals (`asphalt_02_4k`), and gravel/soil splat transition.
3. **Forest Edge (Ecotone):** Dense understory shrubs, young conifer/birch saplings, brambles, and sunlit canopy transition.
4. **Deep Conifer Woodland:** Tall mature fir/pine canopy (`18–26 m`), filtered light shafts, damp pine-needle duff (`forrest_ground_03`), and high trunk density with organic clustering.
5. **Natural Forest Clearing (`12–15 m` diameter):** Fallen nurse log (`dead_tree_trunk`), mossy boulders (`mossy_rock_01`), fern rings (`fern_02`), and clear combat/sightline space.
6. **Terrain Elevation & Drainage:** Western woodland shoulder rise (`+1.5 to +4.5 m` macro relief) and natural drainage swale toward the river corridor (`Slope < 25°` for traversability).
7. **Interactive Physical Chopping Trees (2–3 Designated Nodes):** Full S004 v2 physical trees (`ResourceNodeView` + `TreePhysicsBody` + 16-depth × 8-direction pre-sliced meshes) placed with verified fall clearance (`>= 20 m` from buildings/roads).

---

## 2. Non-Destructive Terrain Architecture (Section 9.1)

1. **Derivative TerrainData Only:**
   - Never mutate `Assets/LastSignal/Data/FirstTown/Terrain.asset` or `FirstTownForest/.../Terrain.asset`.
   - Clone a dedicated `TerrainData` asset under `Assets/LastSignal/Art/Realism/Generations/G<timestamp>/Terrain.asset`.
2. **Hard Protection Mask (`SliceSite.cs` Contract):**
   - Zero height modification within roadbeds (`Roadbed`, `Clear junction`, `Continuous deck`, `Drive-through apron`, `Vehicle parking`), `RainRoof` footprints, building foundations, `PlayerSpawn`, `VehicleWorld` 18×18 m spawn pad, and `LootSpawnPoint` / `DoorInteractable` pads.
3. **Multi-Scale Relief:**
   - **Macro (15–60 m wavelength):** Ridge rise along the western forest boundary + subtle drainage channel (`±0.8 to +4.2 m`).
   - **Meso (2–8 m wavelength):** Root mounds around mature canopy trees (`+0.12 to +0.28 m`), roadside drainage ditch (`-0.22 m` outside road collider), and worn foot-trail depression (`-0.08 m`).
   - **Micro (1–25 cm):** Handled strictly via PBR Normal + Height-blended TerrainLayers (`forrest_ground_01`, `forrest_ground_03`, `forest_ground_04`, `asphalt_02` verge) and DBuffer decals — never micro-spikes in the heightmap that snag `CharacterController` or `VehicleActor`.

---

## 3. Five-Layer Ecological Forest Composition (Section 9.2)

All placement is **100% deterministic** (`System.Random(profile.seed)` + multi-octave Perlin/Simplex density fields + 16 m spatial hash cells), rejecting grid alignment:

| Layer | Ecological Role | Target Species / Assets | Spacing & Placement Rules | Collision & Physics |
|---|---|---|---|---|
| **Layer 1: Canopy Trees** | Dominant overstory (`16–26 m` height) | Mature Conifer (Fir/Spruce/Pine, 3 variants) | Clustered via low-frequency moisture/elevation field; min trunk spacing `5.5–7.5 m`; slope `< 25°`; road clearance `> 4.5 m`; scale `0.80–1.25` with ±3.5° natural lean. | Simple tapered `CapsuleCollider` on trunk (`r = 0.25–0.45 m`, `h = 4–6 m`); S004 nodes use `TreePhysicsBody`. |
| **Layer 2: Secondary & Sub-Canopy Trees** | Mid-story & ecotone (`7–14 m` height) | Birch (lowland/moist), Young Pine/Fir | Higher density along forest edge (`RoadDistance ∈ [5, 22] m`), clearing borders, and moist drainage zones; min spacing `3.5 m`. | Simple `CapsuleCollider` (`r = 0.14–0.22 m`). |
| **Layer 3: Shrubs & Saplings (Understory)** | Eye-level breakup (`0.8–2.8 m` height) | Conifer saplings, woodland shrubs (`Flora-Form` / realistic bush variants) | Clustered in canopy shade radii (`2.0–5.5 m` from Layer 1 trunks) and forest edges; thinned along foot trails (`TrailDistance > 1.8 m`) to preserve zombie sightlines. | **No solid colliders** (walk-through foliage; optional wind-bend shader). |
| **Layer 4: Forest Floor Foliage** | Ground cover (`0.15–0.85 m` height) | `fern_02` clumps, woodland grass tufts, brambles, 3D pine-needle/leaf clusters | Driven by moisture + canopy shade mask (`Slope < 27°`); GPU-instanced detail meshes within `0–45 m` camera distance (smooth dither fade `35–45 m`). | **No colliders**; zero shadow casting beyond `15 m`. |
| **Layer 5: Rocks, Roots, Logs & Stumps** | Structural ground anchors | `tree_stump_01` (CC0), `tree_stump_02` (CC0), `dead_tree_trunk` (CC0), `mossy_rock_01` (CC0) | Stumps/roots near mature trees; boulders on steeper slopes (`15–32°`); fallen logs in clearings and deep forest (`RoadDistance > 6 m`). | Simple `BoxCollider` / convex `MeshCollider` on large boulders/logs (`> 0.35 m` height) so players/zombies step or navigate cleanly. |

---

## 4. Rendering & Performance Optimization Contract (Section 9.3)

1. **True Geometric LOD Tiers (Mandatory — No Cull-Only Fake LODs):**
   - Unlike legacy `FantasyEnvironments` (which had 1 mesh tier until cull at `0.018`), every Hero Slice tree prefab **must** provide real triangle reduction:
     - `LOD0` (`100% – 18%` screen height, ~0–20 m): `3,500 – 5,500 tris` (full 3D trunk + layered branch/needle cards).
     - `LOD1` (`18% – 7%` screen height, ~20–50 m): `1,200 – 2,000 tris` (decimated trunk + merged outer branch cards).
     - `LOD2` (`7% – 2.5%` screen height, ~50–110 m): `400 – 750 tris` (simplified silhouette + reduced shadow caster).
     - `LOD3` (`2.5% – 0.8%` screen height, ~110–250 m): Octahedral Impostor or Cross-Billboard (`4–16 tris`).
2. **Alpha Overdraw & Shadow Caster Discipline:**
   - Trim empty UV alpha space tightly around needle/fern cards.
   - Layer 1 & 2 trees cast shadows up to the pipeline shadow distance (`50–70 m`), using `LOD1`/`LOD2` shadow proxies at distance.
   - Layer 3 (shrubs) cast contact shadows only; Layer 4 (ferns/grass) have `ShadowCastingMode.Off` (`ReceiveShadows = On`).
3. **SRP Batcher & GPU Instancing:**
   - All environment shaders must be 100% SRP Batcher compatible and support GPU Instancing / `BatchRendererGroup` without per-instance material cloning (`ForestPresentation.cs` sets `_LSForestWindPhase` and `_LSForestWetness` globally).
