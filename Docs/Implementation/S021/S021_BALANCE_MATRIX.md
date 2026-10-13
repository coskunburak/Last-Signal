# S021 balance experiments — PLAYTEST_PENDING

## Authored parameters

| Weapon | Base damage | Windup/active/recovery s or RPM | Stamina | Range m | Magazine / reload s | Tuning source |
|---|---:|---|---:|---:|---|---|
| Machete | 22 | .13/.09/.29 | 13 | 1.45 | — | `Data/Combat/S021/Machete.asset` |
| Crowbar | 35 | .22/.12/.46 | 25 | 1.8 | — | `Data/Combat/Crowbar/Crowbar.asset` |
| Axe | 51 | .48/.13/.78 | 43 | 1.9 | — | `Data/Combat/S021/WoodenAxe.asset` |
| Rifle | 30 | 600 RPM | — | 200 | 30; 2.5/3.33 | `Data/Combat/WeaponDefinition_AssaultRifle.asset` |
| Pistol | 22 | 300 RPM, semiauto | — | 65 | 15; 2.1/2.6 | `Data/Combat/S021/Weapon_TacticalPistol.asset` |

Condition scales firearm damage from 100% at 1000 to 80% above zero; zero prevents firing. Values above are authored inputs, not measured kill counts or effective engagement ranges. Head/limb multipliers and spawn health can change actual shots to kill. Both firearms currently emit the same gunshot gameplay-noise profile; this may make the pistol's noise tradeoff too weak.

All five combat prefabs are available at session start; item catalog masses are currently metadata, not carried equipment weight. Evaluate whether ammunition and stamina constraints alone make switching a meaningful choice. Equipment slot/weight gating requires a separate inventory ownership design and is not claimed here.

## Reproducible scenario sheet

Use one fixed encounter seed, initial health/stamina, difficulty and ammo allocation per row. Reset the session and restore the same seed before each weapon. Record accepted shots, confirmed hits, misses, kills, time to neutralize, rounds spent, stamina spent, damage received, noise events and encounter pressure. Repeat at 30 and 120 FPS where swing behavior matters. Record the seed and build evidence directory next to the results.

| ID | Setup | Main comparison |
|---|---|---|
| B01 | One slow zombie, 2 m | Melee time and stamina |
| B02 | Two adjacent zombies, 3 m | Recovery exposure |
| B03 | Crowded close group, 1–4 m | Axe stopping power versus commitment |
| B04 | Narrow indoor corridor | Obstruction and muzzle safety |
| B05 | Medium outdoor distance, 25 m | Rifle versus pistol accuracy/resources |
| B06 | Two magazines total, no further loot | Ammo scarcity and melee viability |
| B07 | 15 stamina at contact | Heavy rejection and retreat |
| B08 | Reload before/after commit under pressure | Transaction and exposure |
| B09 | Alternate melee and firearm | Switching costs and dominant loadout |
| B10 | Retreat while firing or swinging | Noise pressure and survival |

All measured cells are **PLAYTEST_PENDING**. Do not tune from theoretical DPS alone. The selected loadout, 9 mm Security loot weight 40 and starting pistol magazine 8 are provisional until user evidence exists.
