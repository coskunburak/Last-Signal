# Production zombie asset inventory — S004-B0B

2026-09-18. **Selected: Hotstrike Studio — Free Stylized Dark Fantasy Zombie, Unity SK variant. Technical asset integration PASS.** [B0B report](S004_B0B_ZOMBIE_ASSET_INTEGRATION_REPORT.md) · [evidence](Evidence/20260918-B0B/README.md)

## Ownership and exact paths

| Item | Actual imported asset / decision |
|---|---|
| Selected skinned model | `Assets/LastSignal/Assets/SZombie/SZombie_Variant_1/Unity/SK_SZombie_Variant_1.fbx` |
| Avatar | `SK_SZombie_Variant_1Avatar`, sub-asset of that FBX; valid Humanoid |
| Other source models | `Assets/LastSignal/Assets/SZombie/SZombie_Variant_1/SK_SZombie_Variant_1.fbx`, `SM_SZombie_Variant_1.fbx`, `Cascadeur/SK_SZombie_Variant_1.fbx`; preserved, not selected |
| Blender source | `Assets/LastSignal/Assets/SZombie/Blend_SZombie.blend`; preserved |
| Vendor model prefab | No Hotstrike `.prefab`; selected FBX is a nested model prefab |
| Production adapter | `Assets/LastSignal/Enemies/Zombie/Prefabs/LS_Zombie_Shambler.prefab` |
| Animation package | `Assets/LastSignal/Assets/Kevin Iglesias/Zombie Animations/` |
| Animation skeleton / Avatar | `Model/ZombieModel.fbx` / `ZombieModelAvatar` |
| Vendor animation preview prefab | `Prefabs/Zombie.prefab`; not the production enemy |
| Vendor animation materials | `Materials/ZombieMaterial.mat`, `ZombieFloor.mat`; not used by production zombie |
| Production material | `Assets/LastSignal/Enemies/Zombie/Materials/M_Zombie_Shambler_URP.mat` |

Both source directories are **third-party content**, despite their LastSignal prefix. Their FBX, Blender, textures, prefabs and controllers remain byte-identical. Only the selected model's importer `.meta` changes Generic/NoAvatar to Human/CreateFromThisModel. No file moves/renames, source texture replacements, skeleton changes or vendor animation importer changes.

## License/source record

The user identified both packages and supplied/imported them. No acquisition channel/receipt, local license file, package manifest or imported package version was found. **LICENSE_EVIDENCE_PENDING** for local entitlement evidence; commercial release clearance is not certified.

- Model author: Hotstrike Studio. [Publisher product and terms](https://hotstrikestudio.itch.io/free-stylized-dark-fantasy-zombie) allow finished free/paid products and modification, optional credit, and prohibit standalone redistribution/repackaging and claiming authorship. Public source checked 2026-09-18; actual user acquisition channel is unknown. Do not substitute these terms for a different channel's terms.
- Animations author: Kevin Iglesias. [Unity Asset Store package 259680](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/zombie-monster-animations-free-259680) lists **Standard Unity Asset Store EULA**, Extension Asset, latest version 1.0. Local imported version **UNKNOWN**. Embedded commercial project usage is subject to applicable [Unity EULA](https://unity.com/legal/as-terms) and seats; no standalone redistribution permission. Attribution requirement is not separately evidenced locally.

See [source record](Evidence/20260918-B0B/license-source-record.md). Burak retains acquisition/entitlement evidence before commercial distribution. Documenting pending evidence is permitted by the B0B request; technical readiness is not legal certification.

## Measured technical audit

| Measure | Unity result |
|---|---|
| Triangles / vertices / submeshes | **8,284 / 5,468 / 1** (not the publisher's 8,286 marketing value) |
| Renderers / skin meshes / material slots | **1 / 1 / 1** |
| Skin bones / hierarchy transforms / Humanoid mappings | **101 / 103 / 52** |
| Skeleton root / pelvis | `RL_BoneRoot` / `CC_Base_Hip` |
| Spine / upper chest / head | `CC_Base_Waist` / `CC_Base_Spine02` / `CC_Base_Head` |
| Fingers | All five fingers, three joints each, both hands mapped |
| Import scale | globalScale 1, fileScale .01, useFileScale true; centimeter source converted to meters |
| Root / VisualRoot scale and rotation | unit scale, identity rotation; no 0.01/100 transform workaround |
| Reference standing skin bounds | y≈0 to **1.808236 m**, after settled import; player capsule **1.8 m** |
| Shoulder joint spacing / reference head | **.380310 m** between upper arms; head y **1.609738 m**, z .039066 |
| Idle hunched top / Walk top | **1.512–1.640 m / 1.598–1.703 m**, real authored posture |
| Facing / up / pivot | **+Z / +Y / ground origin**, confirmed anatomy, toes and side render |
| Shader | Universal Render Pipeline/Lit; supported and visually textured, opaque |
| Textures used | 3 unique 2048²: A Albedo, project OpenGL normal copy, linear packed metallic/AO/smoothness |
| Maps absent | No emission assigned; no transparency required |
| LOD / blend shapes | **MISSING / 0** |
| Draw estimate | 1 main color draw, plus URP shadow/depth passes; not a frame-debugger benchmark |
| Texture memory | 13.33 MiB estimated compressed GPU mip chains; Editor Profiler reports 27,965,184 bytes (~26.67 MiB) for three unique textures |
| Animator | 1 layer, 6 states, 0 parameters, 0 transitions; no gameplay authority |

101 skin bones (including twist/facial auxiliaries), 2K maps and missing LOD are future population risks. No premature mesh reduction or LOD generation. No colliders, health, AI, NavMeshAgent or runtime scripts on this prefab.

## Material pipeline

All ten imported PNGs were visually inventoried in `source-texture-contact-sheet.png`. Byte comparisons found exact `Smoothness = 255 - Roughness`; the source Unity mask exactly matches Metallic R, Occlusion G and Smoothness A. Project mask repacks those verified separate maps, preserves source files, and imports linear. OpenGL normal is copied into a project NormalMap importer, linear; DirectX normal is not used. Albedo A preserves the supplied green palette; B is the blue alternate. No repainting. One opaque URP material assigns base, normal, metallic/smoothness and AO, with corresponding shader keywords enabled.

## Historical candidates

The earlier alien/crew/captain audit remains in [P1 evidence](Evidence/20260918-P1/README.md). They are not the selected zombie. The original P1 lack-of-model/Idle/Walk blocker is superseded by this measured integration; upstream player/weapon source content remains intact.
