# Model normalization and art policy

IMPLEMENTED_NOT_VERIFIED under LS-DOC-19. All derivatives are project-owned. Vendor FBX/BLEND/texture/meta files remain unchanged.

| Model | Source origin subtracted | Y rotation | Uniform multiplier | Result width × height × length (m) |
|---|---|---|---|---|
| DXT | (-.034,-.043,-.04084) | -90° | 1.8 | .02481 × .16578 × .20766 |
| Glock17 | (0,-.003,.002) | 180° | 3.1 | .02815 × .13650 × .20239 |
| Custom9mm | (0,.060,.224) | 180° | 1.1 | .02996 × .13956 × .20561 |

Transforms are baked into derived mesh vertices. Root and mechanical child scales are (1,1,1), +Z barrel-forward, +Y up, no negative scales. The scaling is an authored proportion choice based on the imported geometry, not a certified manufacturer measurement. Mechanical pivots use actual geometry/bone locations; no new visible part was fabricated. Custom frame/slide submeshes are merged by moving-part semantics, preserving material/UV boundaries. Runtime skinned weapons are unnecessary; only existing arms remain skinned.

Each visual root contains Frame, Slide, Magazine, Trigger, PrimaryGrip, AimReference, Muzzle and EjectionPort. DXT additionally has Hammer. ReloadReference is under the real Magazine so the assist target follows extraction. Single-handed sockets do not require LeftGrip or StockContact.

`Tools/pistol-integration-textures.py` creates local 2K derivatives from selected maps. URP/Lit uses sRGB BaseMap; linear MetallicGlossMap red=metallic, alpha=255−roughness; normal-map import; optional linear OcclusionMap. DXT explicitly uses its OpenGL normal. Glock/Custom normal handedness is not identified by filenames; flashlight/material visual approval remains required. Height, redundant DirectX normals, unused opacity/emissive/attachment maps are excluded. No global lighting change.

Mesh draw-call upper bounds per visible weapon are 5/4/4 before batching, with 1/1/2 shared material assets. GPU memory has not been measured. Assuming BC1 color/AO and BC5 normals/BC3 mask with mipmaps: approximately 16 MiB per four-map 2K set; Glock approximately 13.3 MiB; Custom's two sets approximately 32 MiB. Actual platform compression and simultaneous loaded references can change this. Catalog references can retain all three models; low-quality settings use the normal Unity texture mip limits. Dedicated LOD meshes and performance acceptance are DEFERRED pending user profiling; no FPS/GC claims.

Authoring: first run the local texture utility if rebuilding from supplied textures, then use Unity menu `Last Signal/Combat/Author Triple Pistol Integration`. Existing profiles, finger poses, materials and prefabs are preserved on rerun, so manual calibration is not erased. It repairs Player.secondaryWeapon to DXT/FPS without rebuilding scenes. Existing generated outputs are intentionally not silently rebuilt when the source changes. Mesh correction helpers operate only on the new derivatives and retain GUIDs.
