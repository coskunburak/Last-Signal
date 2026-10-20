# Asset sourcing and qualification — 2026-10-19 research shortlist

**Status:** REMOTE LISTING VERIFIED where noted; acquisition/import/license evidence is NOT VERIFIED locally. **Commercial production criterion:** Only use free-to-acquire assets with confirmed appropriate game-use terms. Costs shown on marketplaces are volatile; check again when acquiring. Source art is not repackaged in this ZIP. Use existing ThirdParty import when equivalent; no speculative duplication.

## Priority 0 sourcing matrix

| Need / kit | Verified URL | Listing at research | Candidate role | Import risk / mandatory check | State |
|---|---|---|---|---|---|
| Realistic mixed trees | [European Forests — Realistic Trees](https://assetstore.unity.com/packages/3d/vegetation/trees/european-forests-realistic-trees-229716) | FREE; Unity Standard Asset Store EULA; listed URP and HDRP compatible at historic editor version | hero and secondary tree candidate | actual 6000.5 import, alpha, LOD tiers, cuttable trunk separability, wind, license receipt | CANDIDATE |
| Bush/grass and understory | [Flora-Form](https://assetstore.unity.com/packages/3d/vegetation/flora-form-266407) | FREE; Unity Standard Asset Store EULA; listing URP-compatible and HDRP not compatible at original 2021.3 | ground understory/grass trial | source may be too simple; LOD, occlusion, texture quality and current render support must be tested | CANDIDATE |
| High-detail natural fir source | [Poly Haven Fir Tree 01](https://polyhaven.com/a/fir_tree_01) | CC0, contains high detail photogrammetry-like fir source | hero trunk/branch reference or optimized derivative | **not automatically game-ready**; create reduced mesh LODs, clean collision and bark cut surfaces | REFERENCE/CANDIDATE |
| Forest floor leafy soil | [Poly Haven Forest Ground 01](https://polyhaven.com/a/forrest_ground_01) | CC0 | forest soil / leaf litter shader material | tile scale, albedo energy, packed maps, texture residency | CANDIDATE |
| Pine litter floor | [Poly Haven Forest Ground 03](https://polyhaven.com/a/forrest_ground_03) | CC0 | pine needle layer / local scatter | terrain tiling and wetness response | CANDIDATE |
| Road material | [Poly Haven Asphalt 02](https://polyhaven.com/a/asphalt_02) | CC0 | asphalt base | scale normals, cracks and lane material response | CANDIDATE |
| Road variant | [Poly Haven Asphalt 01](https://polyhaven.com/a/asphalt_01) | CC0 | worn edge / road alternate | avoid hard boundaries, physically blended shoulder | CANDIDATE |
| Real world roofs | [Poly Haven Corrugated Iron](https://polyhaven.com/a/corrugated_iron) | CC0 | service buildings/station roof | existing roof physics and wetness shader ownership | CANDIDATE |
| Timber | [Poly Haven Wooden Planks](https://polyhaven.com/a/wooden_planks) | CC0 | cabin fencing/repair | texture scale and reuse with existing materials | CANDIDATE |

**Sources verified:** Unity Store product listings, Poly Haven pages, Unity render pipeline docs; see `17_REFERENCES_AND_SOURCES.md`. Product counts/features must not be inferred from marketing screenshots if not explicitly verified. Asset-store URLs use vendor declared historical compatibility, **not proof of Unity 6000.5/URP17 support**. No license entitlement or package SHA has been obtained from the user account.

## Secondary categories — SEARCH REQUIRED, no invented products

- Cuttable, deformable, bark-separated **game-ready** fir/pine/oak/birch tree sets with 2+ actual geometry LODs and impostor options; near-realism asset selection must be measured with tree cutting before purchasing/porting.
- Realistic understory with fern/bramble/sapling/grass variants, believable alpha and wind; separate hero and mass-scatter quality classes.
- Root/stump/torn wood/severed cross-section atlas matching the tree's existing geometry; physical cut wedge is not only a decal.
- Weathered road shoulder, gravel/ditch, roadside cable/wire, utility and modular rural architecture where license and pipeline comply.
- Photorealistic zombies / first-person hands / clothing material overhaul **later**; no silent replacement of current rig/animations.
- Storm/rain volumetric source only if needed after URP–HDRP comparison; NEVER import complete independently timed weather controllers into production.

## License decisions

- CC0 (`Poly Haven`): license applies to identified content; keep URL, capture date, downloaded archive SHA256 and source attribution record even if not required. Do not treat user-submitted variants as necessarily CC0.
- Unity Asset Store Standard EULA: embedded use under applicable account/seat terms; no reselling or publishing source packs. 'FREE' does not mean redistribution unrestricted. Record listing metadata, account receipt, package version and actual EULA snapshot/URL.
- Fab/itch/other: store **specific downloaded file license**, channel and grant. No entitlement from a different marketplace page.
- ThirdParty folder ignored or local-only: cannot assume availability in GitHub or rely on asset source being in this ZIP. Include an installation manifest, not vendor meshes.

## Asset technical qualification gate

1. **Source identity**: package publisher, URL, acquisition method/date/license, package SHA256, files count and version.
2. **Content**: source models, separate trunk/canopy/submeshes, per-asset triangle counts, submesh/material slots, UV channels, transparencies, texel density, compression.
3. **Pipeline**: Unity version, URP17 actual import; compilation warnings, pink materials, ShaderGraph portability, custom RP dependency, instancing, shadows/depth/motion, wind.
4. **Quality**: 1m / 5m / 20m / 50m / 100m views and 1080p screenshots, wet day & rainy night, bark cut wedge demo, canopy silhouettes.
5. **Runtime**: source vs project-owned prefab; geometry LODs and billboards, material cloning check, collider bounds, nav, deterministic spawn, physical cut/fall.
6. **Performance**: same scene, same weather/hardware baseline; GPU and CPU p95, shader variants, overdraw, video memory.
7. **Decision**: `KEEP / ADAPT / REPLACE / REJECT / DEFER`, signed screenshot+benchmark evidence. Vendor attribution does not substitute permission.

## Preservation guidance

Fantasy Landscape B/Pine/Birch/Bush/Rock/Grass were historically approved in S013 and integrated in FirstTownCore. Mark these **LEGACY_KEEP_AS_FALLBACK** pending pilot qualification. Do not bulk remove old prefabs or change their GUIDs; existing scenes and save slots may depend on them. If replacement successful, introduce a new owned realism variant and switch only pilot spawn entries/scene references through staged authoring; document replacement mapping. If the high-detail model lacks suitable LOD or is too heavy, reject mass-forest usage and keep as single hero specimen with measured cost.

## Future registry schema

| Key | Mandatory data |
|---|---|
| identity | asset ID; official package name; publisher; URL; acquired date; download SHA; files version |
| rights | platform/channel, account receipt ID stored privately, full terms, restrictions, attribution |
| tech | Unity source/test version; URP/HDRP; material/shader; geometry LOD costs; MB/texture |
| rendering | alpha overdraw, wind/depth, GPU instancing, shadow distance, wet response |
| physical | mesh split, trunk radius, collider, pivot, nav cut/fall, save integration |
| evidence | day/rain/night images; scene commit and camera; capture hardware; profiler XML/CSV |
| decision | ACCEPT/ADAPT/REJECT/DEFER + approver and date |
