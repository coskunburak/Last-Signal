# Arberry material mapping

Status: IMPLEMENTED / NOT_VERIFIED. All new assets live in `Assets/LastSignal/Art/CharacterIntegration/Arberry/`. Existing user materials and source remaps were preserved. The new world-body prefab assigns the new external materials directly.

| Material | Supplied base / normal / surface source |
|---|---|
| Head | Body_BaseColor / Body_Normal / Body_OcclusionRoughnessMetallic `.1001.png` |
| Body2 | Same family, `.1002.png` |
| Arms | Same family, `.1003.png` |
| Backpack2 | Backpack/Backpack_BaseColor, Normal, OcclusionRoughnessMetallic `.jpg` |
| Gloves1 | Gloves/Gloves_BaseColor, Normal, OcclusionRoughnessMetallic `.jpg` |
| Jacket1 | Jacket/Jacket_BaseColor, Normal, OcclusionRoughnessMetallic `.jpg` |
| Jeans1 | Jeans/Jeans_BaseColor, Normal, OcclusionRoughnessMetallic `.jpg` |
| Shoes1 | Shoes/Shoes_BaseColor, Normal, OcclusionRoughnessMetallic `.jpg` |
| Mouth | Mouth/TeethBaseColor.png, Teet_Normal.png, Teeth_ORM.png |
| Body_Arkit_Eye | Eye_BaseColor.png, Eye_Normal.jpg, Eye_AO.jpg, Eye_SpecularColourBrown.png RGB + Eye_Gloss.jpg alpha |
| Hair3 | HairStyle/HairBrown_Diffuse_2.jpg, HairBrown_Normal.jpg, HairBrown_Alpha.jpg, HairBrown_AO.jpg |
| Brows_Leashes | Eyebrow_EyeLeashes/Eyebrows_BaseColor, Normal, Opacity, AO `.jpg`; shared by brows and lashes |

Paths in the table are relative to the texture root in SOURCE_AUDIT. All maps are actually assigned. White material base tint preserves the supplied color/detail; no flat substitute or global lighting adjustment was made. S013's restrained reflection direction is expressed through supplied roughness, not identical scalar roughness on every part.

## Packing and import

Local deterministic conversion interprets source R=AO, G=roughness, B=metallic and writes **URP R=metallic, G=AO, B=0, A=1−roughness**. URP samples occlusion from G; the same linear packed asset serves both slots. Smoothness multiplier=1, albedo-alpha smoothness disabled. [texture-channel-diagnostics.txt](texture-channel-diagnostics.txt) records min/max/mean statistics. Clothing roughness averages .76–.91 and metal averages .005–.065; face/arms have zero blue and constant green .498. These diagnostics corroborate the named layout. Mouth ORM interpretation is consistent with zero blue and low green, but no original material graph documents it: retain this as an inference requiring visual acceptance, not conclusive channel-semantic proof.

Normals are imported as NormalMap, linear, BC5. Encoded normal channels are preserved; DirectX/OpenGL Y convention is **NOT_VERIFIED**, not guessed from vendor origin. Base/specular color maps are sRGB, data maps linear. Derived textures are not CPU readable at runtime, use mipmaps, trilinear filtering, anisotropy 4 and repeat wrapping. Original 8K jacket/backpack files remain unchanged. Selected derived maps are 2048²; eye, mouth, brows/lashes use 1024². Standalone uses BC7 for color/packed maps and BC5 for normals. Color+coverage packs preserve RGB and write supplied opacity into alpha. Only coverage textures enable alpha-is-transparency; eye gloss is data alpha.

Hair and brows/lashes use opaque alpha clipping at .45, back-face culling and depth writes. No blanket two-sided setting was introduced; silhouette and missing back faces need review. Eye uses the supplied colored specular + gloss in URP's specular workflow with no emission; no invented iris or pupil.

No SSS, hair anisotropy, parallax or specialist shader was added without demonstrated need. Jacket stitching and jeans variation now come from supplied maps. Backpack hardware/fabric response shares the supplied packed map because the imported renderer has one material slot; independent strap/fastener masks were not supplied or fabricated.

## Budget

37 generated textures; theoretical BC5/BC7 allocation with mipmaps ≈157.33 MiB, shared by world and new FPS arms, excluding weapon maps and driver/platform overhead. This is an arithmetic asset estimate, **not a runtime benchmark or approved budget**. 13 slots imply at least 13 base draws before passes, shadows and batching effects. Source hair and backpack dominate triangle count. No compatible lower LOD mesh was supplied; no unsafe decimation or global quality changes were made. Choose further resolution/LOD reductions after user-operated measurements, preserving source files.
