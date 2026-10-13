# S021 weapon roles — authored, unverified

| Weapon | Role | Reach / cadence | Cost and risk | Asset decision |
|---|---|---|---|---|
| Machete (`weapon.blade`) | Fast close defense | 1.45 m; .13/.09/.29 s windup/active/recovery; 22 base damage | 13 stamina; shortest reach | Local Poly Haven model in project-owned FPS and Diesel wrappers |
| Crowbar (`weapon.crowbar`) | Medium utility melee | 1.8 m; .22/.12/.46 s; 35 damage | 25 stamina | Preserved production prefab and pose |
| Wooden axe (`weapon.axe`) | Committed heavy blow | 1.9 m; .48/.13/.78 s; 51 damage | 43 stamina; cannot start while exhausted | Local Poly Haven model in project-owned wrappers; candidate remains visual acceptance dependent |
| Assault rifle (`weapon.rifle`) | Versatile ranged combat | 200 m configured range; 600 RPM; 30 damage | 30-round `ammo.rifle` magazine, 2.5/3.33 s reload; gunshot noise | Preserved existing rifle |
| Tactical pistol (`weapon.pistol`) | Emergency secondary | 65 m configured range; semiauto 300 RPM cap; 22 damage | 15-round `ammo.9mm` magazine, starts with 8; 2.1/2.6 s reload; gunshot noise | Selected additional firearm; MR POLY mesh is interim until visual acceptance |
| Pump shotgun | Deferred | Would need pellets, spread and individual-shell reload | Distinct ammunition and noise economy unimplemented | Existing model alone does not satisfy combat semantics |

Only one new firearm was selected. Pistol reuses the proven magazine transaction model while keeping independent ammunition, state, condition and save identity. Melee definitions contain the role tuning; no new damage authority was introduced. Current gunshot noise uses the common S015 gameplay category, so range and pressure differentiation remain a measured balance decision.

Catalog masses: machete 650 g, crowbar 1800 g, axe 2200 g, pistol 850 g; rifle has no matching catalog item. The current player owns these combat prefabs at session start, so catalog mass is **not** an equipped-loadout penalty. This gap and any loadout dominance must be evaluated before claiming final survival balance.
