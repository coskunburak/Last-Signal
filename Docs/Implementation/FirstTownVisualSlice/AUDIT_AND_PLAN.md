# FirstTown visual slice — focused audit and coordinate plan

2026-10-16. Local branch `s012-integrated-graybox-slice`, HEAD `9191a5cd7bd245ee52e3bd9dab11466702ac2931`. The workspace already contained extensive uncommitted character, weapon, persistence, FirstTown, forest and weather work. Those changes are retained. No Git commit, push, reset or clean was performed. No applicable repository AGENTS.md was found.

The supplied master brief is authorized by the owner's subsequent implementation message. The owner also explicitly selected **preserve both endpoints, allow a narrow connecting corridor and focused detail around each endpoint** when the audit found that they cannot fit in a single 120 m square. The scope is therefore an endpoint-and-corridor exemplar, not a 500 m town rebuild.

## Local evidence

- Scene: `Assets/LastSignal/Scenes/Production/FirstTownCore.unity`. Static YAML confirms safehouse compound and cabin at **(-180, 0, -170)**; gas station at **(65, 0, -82)**; service room at **(70, .03, -55)**. Endpoint separation is approximately 260 m.
- Terrain: existing 500 × 20 × 500 m Unity Terrain at **(-250, -10, -250)**. The installed forest derivative is already referenced, with four terrain layers and instance-count grass. Do not replace it with a flat plane or regenerate the original world.
- Existing roads: safehouse approach to (-180,-140), south cross street to (50,-140), gas access to (50,-82). The route crosses the existing south bridge. The creek follows approximately x = -112 + 7 sin(.018 z). No ford, relocated bridge or portal.
- Current forest authoring points to `Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052`. Its saved tree count is 297; older screenshots are not an exact substitute for the current saved scene.
- Existing `TownAtmosphere` is the sky/lighting/wetness/rain presentation owner. `WorldClock`/`WorldSimulation` remain simulation owners. `ForestPresentation` already reads their wind/wetness state.
- Existing player prefab camera FOV: **75 degrees**. The actual runtime camera is spawned with the player, not inferred from the editor Scene View.
- Current geometry uses real gas vendor canopy, pumps, office and sign; service-room wall modules and gameplay doors already exist. The workshop roof is a thin flat collision slab with fascia; road surfaces are UV-mapped quads, while some aprons/junctions are cube-rendered slabs. Their colliders remain authoritative.
- The source screenshot's cyan is an editor navigation/debug presentation, not evidence of a cyan material. Loot labels in that view are not removed from gameplay identity data.

## Priorities and evidential limits

| Priority | Evidence | Response |
|---|---|---|
| P0 | Safehouse and station cannot share one 120 m square | Owner-approved corridor; keep all POIs and stable IDs in place |
| P0 | Scene and many dependencies are already uncommitted | Stage copy, checkpoints, source/TerrainData hashes, protected-component comparison, explicit promotion |
| P1 | Current Game View shows reversed large sign lettering | Rebuild scoped existing sign lettering on both board faces, fit to board; preserve board and gameplay UI |
| P1 | Broad exposed ground and abrupt road/soil edges in supplied Game View | Local four-layer splats, grass/earth shoulders, shallow relief and drainage; keep roads/pads at existing heights |
| P1 | Flat workshop roof is confirmed in authoring and scene hierarchy | Pitched metal roof, gable infill, seams, ridge cap, bargeboards, corner boards and drainage; existing doors/interior retained |
| P1 | Earlier canopy repetition; saved forest pass already exists | Replace only scoped generated plant instances, reuse approved family, vary age/height and retain existing terrain details elsewhere |
| P1 | Supplied view has bright sky and very dark ground | Optional calibration of the existing director; lowered sky exposure and increased daytime ambient fill; defaults elsewhere unchanged |
| P2 | Few anchored human-scale stories in supplied view | Two proposed abandoned vehicle contexts, maintenance props, fence fragments and ground clutter; placements respect masks and report exclusions |
| P2 | Distant empty city / unrelated district blockout | Outside this exemplar; not disguised or globally filled |

These are evidence-backed implementation priorities, not proof that the generated result already looks good. Asset silhouette, normals, shader appearance, source UVs, actual scale/contact and composition require the owner's native generation and captures.

## Coordinate plan

All coordinates are Unity X/Z in metres.

| Element | Footprint / location | Composition and source | Collision / navigation |
|---|---|---|---|
| Safehouse focus | x -235…-135; z -216…-116 | Existing cabin, shed, fences and starter supplies; maintained refuge with neglected outer woodland | Cabin, door, vehicle spawn, shelter bounds, loot and route pads protected |
| Station focus | x 12…120; z -139…-31 | Existing canopy at (72,-83), pumps and office; detailed service shop (70,-55) | Existing apron and gameplay entrances retained |
| Road connector | Route (-180,-170) → (-180,-140) → (50,-140) → (50,-82), 14 m half-width focus band | Existing asphalt carriageway, terrain shoulders and restrained vegetation | Existing road/bridge collider widths unchanged; no vegetation in travel corridor |
| Forest foot trail | (-180,-138) → (-195,-130) → (-213,-137) → (-222,-151) → (-225,-178), 2.4 m nominal width | Painted existing terrain, low understory and taller trunks outside trail | Does not invent a new portal or quest; route and original forest trails protected |
| Service roof | Existing 10 × 8 m room; roof 11.3 × 9.3 m, eave 3.4 m, ridge 5.25 m above room origin | Existing S013 RoofMetal/PaintedTimber/DarkTimber/Metal, UV-scaled authored mesh | New roof collider + RainRoof; old collision slab and playable room preserved |
| Workshop entrance | Existing front door, canopy at local (0,2.65,-4.55) | Existing imported Awning, wall extinguisher, side electrical panel | Decoration collision off; door swing and escape pads reserved |
| Safehouse wreck | Proposed (-207,-150), length 4.7 m, yaw 28° | Existing Wreck wrapper; abandoned yard context | Must clear actual solids, road, trail and pads; skipped and logged if incompatible |
| Station wreck | Proposed (100,-99), length 4.5 m, yaw -24° | Existing Wreck wrapper; edge of abandoned forecourt | Same clearance policy; never silently place over an encounter |
| Service yard | Proposed pallets/tyre/bin around (91…98,-69…-61); fence fragments near (102…108,-52) | Existing project props plus selected vendor maintenance objects | Grounded by terrain and bounds; large solid props enter nav bake |

Placement pivots are normalized from actual renderer bounds with uniform scale. Source proportions are retained. Large props retain their existing collision only when explicitly requested; plants retain trunk capsules, not leaf mesh collision. The clearance policy is conservative and does not claim tested pickup swept paths or agent passage.

## Ownership

`FirstTown visual slice — owned authoring` contains:

- `Generated — replaceable, move hand edits to Artist additions`
  - 01 roads/aprons; 02 workshop architecture; 03 roadside stories; 04 canopy; 05 understory; 06 signs; 07 disabled comparison cameras.
- `Artist additions — preserved`: not destroyed on regeneration; its renderer bounds are protected.

Original scoped renderers are disabled with restoration records. Original generated vegetation is disabled, never deleted. Terrain modifications use a copy of the existing TerrainData. Original forest hero trees, scene gameplay objects, vendor sources and source asset GUIDs remain intact. Generation folders are immutable to support checkpoint restoration; repeated generation replaces the scene subtree without accumulating visible instances. It retains prior generation assets intentionally.

## Fidelity gaps

The approved pine/birch family is stylized, with limited canopy topology and no reduced-geometry LOD tiers. Ground clutter reuses bush/grass/rock/bark assets; it is not a high-resolution root, fern or leaf-litter scan library. Two existing gas-side structures and the safehouse provide the architecture in scope; no new fake accessible residential houses are added. Volumetric sun shafts, dynamic cloud shadows and new photoreal facade assets are not implemented. Commercial acquisition/license records still need to accompany publication; local presence is not a license certificate.
