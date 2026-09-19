# Real VAL animation matrix

Inspected through Unity Editor on 2026-09-17. All production motions reference sub-assets of `Assets/LastSignal/VAL.fbx`; no generated animation curves. Source takes run at 24 fps. Import ranges were copied from `ModelImporter.defaultClipAnimations`, not inferred from names.

| Actual clip | Seconds / frames | Loop | Weapon movement m | Magazine movement relative to body m | Left / right hand movement m | Use |
|---|---|---|---|---|---|---|
| `LVA4_Armature|wpn_val_draw` | 0.500 / 0–12 | False | 0.09903 | 0.00000 | 0.17052 / 0.06985 | Equip |
| `LVA4_Armature|wpn_val_hide` | 0.500 / 0–12 | False | 0.11083 | 0.00000 | 0.17390 / 0.08642 | Unequip motion available |
| `LVA4_Armature|wpn_val_idle` | 8.333 / 0–200 | True | 0.00400 | 0.00000 | 0.04007 / 0.02065 | Ready / hip idle |
| `LVA4_Armature|wpn_val_idleAim` | 1.667 / 0–40 | True | 0.00000 | 0.00000 | 0.00000 / 0.00000 | Available authored ADS pose |
| `LVA4_Armature|wpn_val_inAim` | 0.500 / 0–12 | False | 0.15508 | 0.00000 | 0.15109 / 0.15522 | Available; sight alignment uses adapter |
| `LVA4_Armature|wpn_val_outAim` | 0.500 / 0–12 | False | 0.18626 | 0.00000 | 0.18414 / 0.18805 | Available; sight alignment uses adapter |
| `LVA4_Armature|wpn_val_reload` | 2.500 / 0–60 | False | 0.09076 | 0.35013 | 0.43269 / 0.09259 | Tactical reload; live sequence inspected |
| `LVA4_Armature|wpn_val_reload_full` | 3.333 / 0–80 | False | 0.09077 | 0.34341 | 0.42946 / 0.09420 | Empty reload; bolt bone moves but MR POLY has no separate bolt |
| `LVA4_Armature|wpn_val_shoot` | 0.500 / 0–12 | False | 0.02063 | 0.00000 | 0.02426 / 0.02084 | Fire |
| `LVA4_Armature|wpn_val_shootAim` | 0.500 / 0–12 | False | 0.00809 | 0.00000 | 0.00832 / 0.00945 | Available authored ADS shot |
| `LVA4_Armature|wpn_val_sprint` | 1.000 / 0–24 | True | 0.05258 | 0.00000 | 0.09297 / 0.03330 | Available; movement currently uses existing bob |
| `LVA4_Armature|wpn_val_walk` | 1.000 / 0–24 | True | 0.02389 | 0.00000 | 0.02754 / 0.02412 | Available; movement currently uses existing bob |

Movement values are maximum displacement from the first sample across 61 evenly spaced samples. They establish actual animated content; they are not visual acceptance by themselves. Slide travel: shoot 0.04833 m, aimed shoot 0.05132 m, empty reload 0.06088 m. Tactical reload does not translate the slide.

The ten `BlouseAction`/`.001` clips are 1-frame setup/visibility takes, not gameplay substitutes. Full audit: `Evidence/20260917-RealAssets/clip-motion-audit.csv`. All 22 non-preview imported clips were inspected.

The full skeleton, including both IK hands, finger chains, pole targets, camera nodes, weapon body, magazine/ammo and slide, is retained. `VAL_Model` and `Environment` are inactive renderer branches. The imported camera component is disabled.

Missing: `MISSING_PRODUCTION_ANIMATION` for tt-3d world locomotion and world weapon pose. There is no independently rendered MR POLY bolt/charging handle to animate. No replacement clip was fabricated.
