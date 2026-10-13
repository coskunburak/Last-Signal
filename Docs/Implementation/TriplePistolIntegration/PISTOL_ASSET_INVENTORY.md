# Triple pistol asset inventory

State: IMPLEMENTED_NOT_VERIFIED. Asset inspection is not a compilation, gameplay or visual acceptance gate.
Unity 6000.5.0f1 (88b47c5e7076), URP. Branch `s012-integrated-graybox-slice`, starting HEAD `c41dcf1a0e0942377556227e938ef847391623bb`.
Pre-existing work is recorded in PREEXISTING_STATUS.txt; no commit was created.

| Package | Exact selected source under Assets/ThirdParty/Weapons/PISTOLS/ | Runtime triangles / vertices | Runtime meshes / materials | Source textures → runtime | Animations |
|---|---|---|---|---|---|
| DXT_ready | DXT_ready/dxt_ready.fbx | 8,781 / 5,463 | 5 / 1 | 2048² → 2048² | 9 Generic weapon clips, 24 fps |
| Glock17 | Glock17/Glock17.fbx | 4,460 / 4,161 | 4 / 1 | 2048² → 2048² | None |
| pistol-9mm-custom | pistol-9mm-custom/source/CustomPistol9mmPack.fbx | 12,114 / 11,537 selected geometry | 4 / 2 | 4096² → 2048² | None imported |

Runtime roots: `Assets/LastSignal/Art/TriplePistols/{DXT,Glock17,Custom9mm}/`.
Each contains `FPS.prefab`, `Visual.prefab` (also the world prefab), `Variant.asset`, `Pose.asset`, `Fingers_FPS.asset`, `Fingers_Diesel.asset`, rigid meshes and PBR material.
Custom magazine uses `Assets/LastSignal/Art/TriplePistols/CustomMagazine/PBR.mat`.

DXT: FBX + BLEND; one source skinned mesh, five bones named root/slide/mag/trigger/hammer. Runtime uses rigid derivatives split by bone ownership, with UV0/normals/tangents retained and unused vertices/skin channels removed. Mixed-bone triangles stay on frame. Source clips remain intact, without binding their Generic paths to the VAL armature.
Glock: FBX/OBJ/MTL/BLEND/Maya .ma alternatives; FBX selected once, other exports are not additional weapons. Source already has Frame (Glock17), Slide, Magazine, Trigger.
Custom: one FBX contains repeated assembled/exploded weapon layouts plus accessory meshes. Only CustomPistol9mm through .022 and Magazine9mm through .003 are selected. Muzzle points toward source -Z. Optics, rails, suppressors, lights, lasers, loose bullets and duplicated layouts are excluded. Objects with reload-related names are static source geometry, not imported animation clips.
No authored LODs or vendor grip/aim/muzzle sockets are reused. All runtime semantic sockets are project-owned. Source import settings and exact clip names/durations are in AUTHORED_ASSET_METADATA.txt. No missing source model was encountered.

## Provenance

Vendor identity, license, original marketplace URL, receipt and commercial redistribution permission are PENDING for all three. The only local acquisition evidence is the user-provided directory. No license document was found within the supplied packages. The names do not establish a manufacturer or a license. No external upload or AI generation occurred.
SOURCE_MANIFEST.json enumerates all package files; SOURCE_HASHES.json records original hashes, including meta files. Non-Unity hash comparison found no vendor changes. This does not establish legal clearance.
