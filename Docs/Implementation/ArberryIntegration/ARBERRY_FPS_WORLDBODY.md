# FPS, world-body, vehicle and persistence

Updated 2026-10-14. IMPLEMENTED / NOT_VERIFIED.

The Player uses Arberry_WorldBody and five Arberry FPS weapon prefab references. Rifle, DXT, Glock17, Custom9mm, crowbar, machete and wooden axe have complete project-owned FPS prefabs. Original weapon definitions, controller/state authority, inventory, health, capsule, look, survival and saves remain in place.

## Actual first-person character

Arberry_FirstPersonArms uses locally partitioned Body3 arms, Gloves and Jacket meshes from the actual source skeleton: three skinned renderers, 12,294 triangles. Source UVs, retained vertex weights and bindpose indices are preserved. Arm-only triangles require all vertices to have at least 65% arm-chain influence; gloves are retained whole. Arms, Gloves1 and Jacket1 are the same persistent materials used by the world body. The earlier palette-only component is removed from the body; its unused script and material assets remain as historical derivatives.

One FirstPersonWeaponGrip owns the procedural limbs and fingers. Legacy arm renderers are removed only from the new weapon copies, and their Animators/controllers are disabled. Rifle and crowbar now have the grip component they previously lacked. Rifle model, magazine, scope, muzzle and ejection references are retained. No new gameplay damage or ammunition owner was introduced.

The anatomical root follows the owner camera independently of weapon recoil and action lowering. Separate camera-space shoulder/elbow targets use Arberry proportions. Weapon-owned contacts prevent an unreachable hand from dragging the sights away from ADS. Pose restore includes root, arms, fingers, visual and magazine, including disable and variant replacement. Rifle support-hand reload motion reads authoritative reload time; it never transfers ammunition.

Local glove-surface measurement gives right palm depth 0.036470 m and left 0.036227 m, replacing the old generic 0.012 m on both Arberry rigs. Pistol finger poses are bind-relative to Arberry in both views. Profile resolution uses pistol variant identity. See palm-surface-authoring.txt, fps-partition-authoring.md and fps-contact-authoring.txt for authoring metadata, not gate results.

## Visibility and lifecycle

CharacterBodyVisibility hides the local body during its FPS/scope camera passes and restores it for external views. FPS meshes stay owner-camera-only on layer 29. F6/F7 inspection uses the existing inspector. SetLocalOwner(false) remains the remote-ownership presentation hook; no networking was added.

Vehicle presentation uses Arberry hips/hands and existing seat/wheel anchors. Save files store gameplay rather than transient IK. Wheel contact, death-in-seat, camera clipping, sleeve openings, extreme FOV, treatment, crouch/slide/sprint and all weapon transitions require user-run functional and visual acceptance. No Play session, benchmark or verification gate was executed.

## Reversible selection and authoring

Outside Play mode, `Last Signal > Character > Arberry > Restore Diesel presentation` or `Use Arberry presentation` replaces the world presentation, rebinds inspection/action references and selects the corresponding five FPS prefabs. Restart the session after changing the Player prefab. Original Diesel and weapon assets remain intact.

After regenerating materials/body/profiles with the existing Arberry menus, run `7 Author actual FPS arms and all weapon bindings` last. This rebuilds the partition, measured palms, seven weapon copies and Player FPS references. Profile regeneration alone does not preserve the final FPS calibration. Authoring is not verification or visual approval.
