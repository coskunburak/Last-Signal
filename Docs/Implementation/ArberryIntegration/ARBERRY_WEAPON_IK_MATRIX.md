# Arberry weapon matrix

Status: authoring calibration seeds implemented on actual Arberry bones; **visual and motion calibration NOT_VERIFIED**. Do not describe these stances as approved natural grips until tested and reviewed.

| Weapon | Own profile | Contact / calibration |
|---|---|---|
| Rifle | Profiles/Rifle.asset | Existing measured stock, pistol grip, support, ADS and reload sockets; new shoulder-pocket offsets and 35° torso heading; own jacket clearance envelope |
| Legacy pistol definition | Profiles/Pistol.asset | Own anatomical 15-joint bind-relative finger asset; original model contacts |
| DXT | Profiles/DXT.asset | Original DXT contact geometry; own DXT_Fingers; source width .026 m / depth .038 m |
| Glock17 | Profiles/Glock17.asset | Original Glock contact geometry; own Glock17_Fingers; width .029 / depth .042 m |
| Custom9mm | Profiles/Custom9mm.asset | Original Custom contact geometry; own Custom9mm_Fingers; width .030 / depth .045 m |
| WoodenAxe | Profiles/WoodenAxe.asset | TwoHandedHeavyMelee preserved; source separated primary/secondary handle contacts, own shoulder-relative ready target; existing anticipation/strike/recovery trajectory |
| Machete | Profiles/Machete.asset | Existing 75 cm identity/geometry, one hand, own ready target; no separate knife |
| Crowbar | Profiles/Crowbar.asset | Own cosmetic mesh prefab; palm contact transferred through the shared source mesh from actual FPS R_wrist/finger landmarks; one-hand heavy trajectory sampled from original melee timings |

`CharacterWeaponRig.PresentationOverride` maps source profile identity to character profile after the runtime pistol variant is selected. Mapping by WeaponDefinition alone would lose variant-specific geometry; this implementation does not. Defaults remain five weapon definitions; three extra pistol variants are selected by identity. FPS now resolves to the same character-specific profiles and actual Arberry finger rig while preserving weapon controller/state. Each world pistol finger asset is recalculated from Arberry's own bind transforms and the existing weapon dimensions, not copied Diesel local rotations.

Own right-arm length .5502 m drives pistol and melee reach seeds. Torso clearance offset=(0,.15,.015), radii=(.24,.30,.19) in existing calibrated torso axes. These envelope values are conservative authoring inputs, not measured collision/pose PASS. Rifle retains existing mesh scale; melee retains source geometry and dimensions. Shoulder assistance, maximum reach and forearm roll remain bounded by profiles. Reload cancellation, prior-frame pose restore, death/treatment/seat suppression and event cleanup are reused.

All seven currently requested presentations (rifle, three pistol variants, axe, machete, crowbar) are wired. Trigger placement, thumb intersections, axe two-hand attachment throughout strike, ADS, crouch/slide/sprint reach, support reload contact and stock penetration still require user-run tests and visual review. No final-pose play session or motion benchmark was executed. Crowbar's newly transferred contact requires especially careful review because its legacy FPS hand frame differs from the other weapon family.

No presentation path calls damage, ammo transfer, stamina spending, inventory mutation, or vehicle physics. Shared runtime changes include optional profile resolution, measured palm depth, FPS camera anchoring, explicit two-hand firearm contact and reload presentation. Diesel keeps its original prefabs and default palm depth; dedicated fallback tests remain authored.

## FPS follow-up

All seven FPS prefabs use actual Arberry arm skins. Rifle and crowbar previously lacked FirstPersonWeaponGrip; both now have it. Rifle keeps original optic/muzzle references and a separate magazine with reload contact. All profiles keep weapon-owned contacts and independent FPS elbow hints. Glove-surface depths are shared with the world rig (right 36.470 mm / left 36.227 mm). Pistol variant switching resolves both view and grip profiles without changing gameplay identity.

Local contact authoring uses the measured arm length, bounded maximum reach and a 12 mm reserve. Nine ready/equip/slide or melee trajectory samples fitted without additional translation for all seven prefabs. This metadata covers authoring samples only; ADS, recoil, reload interpolation, IK deformation and rendering remain NOT_RUN. See fps-contact-authoring.txt. Dedicated tests were authored for rifle two-hand contact, ADS, reload cancellation, paused pose stability and crowbar cancellation without duplicate damage authority.

2026-10-14 focused calibration follow-up: rifle HipEuler (14,-8,0); axe world HipGripOffset.z 0.42015326; machete FPS Euler (22,0,-25). Three variant finger routes are now fitted around actual rigid-frame sections using the measured palm depth. The latest user run passed 49/53 character-play tests before these changes; current values await rerun. No assertion threshold was relaxed.
