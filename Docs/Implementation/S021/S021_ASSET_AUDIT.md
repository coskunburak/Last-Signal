# S021 asset audit

| Content | Source and wrapper | Status / dependency |
|---|---|---|
| Assault rifle and crowbar | Existing project combat prefabs and Diesel profiles | Preserved; regression pending |
| Tactical pistol | Existing `Assets/LastSignal/Prefabs/Combat/MRPoly/World/Tactical Pistol (Black).prefab`; new `Prefabs/Combat/S021/Weapon_TacticalPistol.prefab` and `Diesel_WorldPistol.prefab` | Interim mesh. First-person hand grip, magazine geometry, materials and third-person pose require visual review |
| Machete | Existing local `Assets/ThirdParty/Weapons/machete_4k.blend/machete_4k.blend`; new FPS/world wrappers | [Poly Haven machete](https://polyhaven.com/a/machete) lists CC0. Local source was already present; provenance and final material/import review pending |
| Wooden axe | Existing local `Assets/ThirdParty/Weapons/wooden_axe_4k.blend/wooden_axe_4k.blend`; new FPS/world wrappers | [Poly Haven wooden axe](https://polyhaven.com/a/wooden_axe) lists CC0. Local source was already present; grip/pivot/weight visual review pending |
| Pistol audio | Existing local `Assets/ThirdParty/Audio/FreeWeaponSFX_v1.1/Handgun` | Pistol fire, reload and equip fallbacks bound. Confirm local package license provenance and listening acceptance before release |
| Melee audio | Existing local `Assets/ThirdParty/Audio/ZombieHorrorPackageFree/WAV` | Distinct foley/impact fallbacks bound; source provenance and listening acceptance pending |
| 9 mm pickup | `Prefabs/Items/ammo.9mm.prefab` | Uses current generic ammo pickup/icon; replace visual after review |
| Pistol, machete and axe inventory icons | Project-owned silhouettes in `Art/ItemIcons/weapon.*.png`, authored by `Tools/s021-item-icons.py` | Distinct 128px transparent sprites matching the existing cream silhouette style; references bound in item assets and authoring code. Static image inspection complete; Unity import and in-game UI acceptance pending |

Editor menu `Last Signal/S021/Author Combat Assets` regenerates project-owned S021 definitions, prefabs and Diesel pose references without editing imported vendor source files. It was used for authoring; Unity verification gates were not run. 4K source textures may be costly; profile texture memory in the user-owned build before final art approval.
