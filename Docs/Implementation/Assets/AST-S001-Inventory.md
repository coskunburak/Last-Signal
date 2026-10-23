# AST-S001 — Envanter / 23 Ekim 2026

**AUDIT_COMPLETE (statik kapsam)** · **MOVE_PLAN_READY (engelli satırlar dahil)** · **OWNER_VALIDATION_PENDING**. Canlı görsel kalite, import/derleme ve oyun kabulü tamamlanmadı; sprint **ACCEPTED değil**.

35 kök giriş altında **72 paket/kaynak birimi**, **9.981 asset**, **10.814 meta**. Toplam **9.136.566.941 bayt (8,51 GiB)**; dosya sistemi tahsis toplamı **9.188.454.400 bayt**. Tahsis değerleri APFS paylaşılan bloklarının fiziksel benzersiz maliyetini göstermez. Gizli işletim sistemi dosyaları hariçtir. 72 sayısı ticari paket sayısı değildir: koleksiyonlar ve bağımsız EXR kaynakları da birimdir. NOX alt koleksiyonları ve vendor iç klasörleri katalogdaki `nested_entries` alanında tutuldu; paket sınırları belirsiz olduğunda kaynak ağacı bölünmedi.

Makinece aranabilir [katalog](AST-S001-Catalog.json), [taşıma planı](AST-S001-MovePlan.json), tam [Evidence/20261023T114820429441Z/inventory.csv](Evidence/20261023T114820429441Z/inventory.csv). Ham materyal/GUID/bağımlılık verileri [Evidence/20261023T114820429441Z](Evidence/20261023T114820429441Z) altındadır; Evidence Git tarafından hariç tutulur ve raporlarla birlikte yerel olarak korunmalıdır. `python3 Tools/AssetAudit/audit.py` yeni zaman damgalı kanıt üretir; eski kanıtı değiştirmez. `reviewed-inventory.json` bu sprintin manuel değerlendirmesini de içerir; tekrar tarama manuel değerlendirmeyi otomatik yenilemez.

Branch: `s012-integrated-graybox-slice`; HEAD: `3fa3161f0d3d22f40d422fd5b404605f25088ce0`. İlk tam status kaydı 1.837 satırdır; bunlar asset sayısı değildir. Mevcut değişiklikler commitlenmedi/stashlenmedi. ThirdParty ignore kuralına rağmen **1.204 dosya önceden izleniyor**, **19.591 dosya izlenmiyor**; Git tek başına kurtarma kaynağı değildir.

| ID | Gerçek paket/kaynak yolu (ThirdParty altında) | MiB | Asset | Prefab / Model / Mat / Texture | Kullanım |
| --- | --- | --- | --- | --- | --- |
| AST-001 | Abandoned buildings | 58.09 | 49 | 12 / 11 / 9 / 13 | DYNAMIC_USAGE_POSSIBLE |
| AST-002 | Animation/AnimationLibraries/Universal Animation Library 2[Standard] 2 | 69.46 | 13 | 0 / 7 / 0 / 3 | DYNAMIC_USAGE_POSSIBLE |
| AST-003 | Animation/AnimationLibraries/Universal Animation Library[Standard] 2 | 60.72 | 9 | 0 / 4 / 0 / 3 | DYNAMIC_USAGE_POSSIBLE |
| AST-004 | Animation/AnimationLibraries/Zombie Animation | 4.58 | 8 | 0 / 8 / 0 / 0 | ACTIVE_PRODUCTION |
| AST-005 | Animation/CrouchAndSlide | 2.27 | 6 | 0 / 6 / 0 / 0 | ACTIVE_PRODUCTION |
| AST-006 | Animation/KevinIglesias/Zombie Animations | 7.29 | 18 | 1 / 7 / 2 / 2 | ACTIVE_PRODUCTION |
| AST-007 | Animation/ZOMBİE ATTACK ANIMATION | 51.88 | 233 | 16 / 102 / 3 / 1 | ACTIVE_PRODUCTION |
| AST-008 | AssetsStore/Garage_props | 294.63 | 74 | 37 / 13 / 6 / 16 | ACTIVE_PRODUCTION |
| AST-009 | Audio/Essentials_Series_NOX_SOUND | 1094.06 | 1661 | 0 / 0 / 0 / 14 | ACTIVE_PRODUCTION |
| AST-010 | Audio/FreeWeaponSFX_v1.1 | 11.55 | 48 | 0 / 0 / 0 / 0 | ACTIVE_PRODUCTION |
| AST-011 | Audio/Horror_Music_Pack_Starter_Kit | 234.56 | 10 | 0 / 0 / 0 / 0 | ACTIVE_PRODUCTION |
| AST-012 | Audio/Tree | 5.38 | 5 | 0 / 0 / 0 / 0 | ACTIVE_DEVELOPMENT |
| AST-013 | Audio/ZombieHorrorPackageFree | 13.58 | 44 | 0 / 0 / 0 / 0 | ACTIVE_PRODUCTION |
| AST-014 | Characters/Diesel | 25.04 | 2 | 0 / 2 / 0 / 0 | DYNAMIC_USAGE_POSSIBLE |
| AST-015 | Characters/FirstPersonArms | 16.30 | 5 | 0 / 1 / 0 / 4 | DYNAMIC_USAGE_POSSIBLE |
| AST-016 | Characters/Fps Arms Pack [Updated] FBX | 88.95 | 40 | 8 / 9 / 7 / 14 | DYNAMIC_USAGE_POSSIBLE |
| AST-017 | Characters/Survival Char | 599.05 | 71 | 0 / 1 / 0 / 70 | ACTIVE_PRODUCTION |
| AST-018 | Characters/VAL | 6.50 | 1 | 0 / 1 / 0 / 0 | ACTIVE_PRODUCTION |
| AST-019 | Environments/BarnExtracted | 12.44 | 17 | 0 / 1 / 0 / 16 | ACTIVE_DEVELOPMENT |
| AST-020 | Environments/FantasyEnvironments | 218.67 | 393 | 154 / 154 / 33 / 35 | ACTIVE_PRODUCTION |
| AST-021 | Environments/HolotnaMountainURP | 32.72 | 68 | 15 / 10 / 14 / 25 | ACTIVE_DEVELOPMENT |
| AST-022 | Environments/Silver_Cats/Hand_Painted_Nature_Kit_LITE | 230.28 | 64 | 8 / 8 / 3 / 29 | ACTIVE_DEVELOPMENT |
| AST-023 | Environments/TT3D/LowPolySci-FiStarterPack | 1.14 | 24 | 3 / 3 / 8 / 8 | DYNAMIC_USAGE_POSSIBLE |
| AST-024 | Environments/cupboard_pack.fbx | 8.27 | 1 | 0 / 1 / 0 / 0 | ACTIVE_DEVELOPMENT |
| AST-025 | Factory Tools | 1.14 | 55 | 22 / 22 / 1 / 3 | ACTIVE_PRODUCTION |
| AST-026 | Fantasy Skybox FREE | 133.57 | 121 | 0 / 0 / 50 / 62 | ACTIVE_PRODUCTION |
| AST-027 | Frameworks/FirstPersonARPGStarterPack | 7.54 | 118 | 7 / 3 / 0 / 18 | ACTIVE_PRODUCTION |
| AST-028 | Free Wood Door Pack | 662.28 | 291 | 105 / 4 / 111 / 64 | ACTIVE_PRODUCTION |
| AST-029 | Loot/MedicinePack_JimmyPV | 9.53 | 10 | 0 / 1 / 0 / 9 | ACTIVE_PRODUCTION |
| AST-030 | Loot/Polygon-Lite Survival Collection | 4.50 | 154 | 69 / 69 / 11 / 1 | ACTIVE_PRODUCTION |
| AST-031 | Loot/stylooFoodAndKitchenAssetPackFreePack | 84.37 | 109 | 0 / 106 / 0 / 0 | DYNAMIC_USAGE_POSSIBLE |
| AST-032 | MarpaStudio | 332.74 | 319 | 23 / 37 / 43 / 214 | ACTIVE_PRODUCTION |
| AST-033 | Modular European House | 203.54 | 117 | 25 / 22 / 21 / 40 | ACTIVE_PRODUCTION |
| AST-034 | RainMaker | 16.07 | 48 | 3 / 0 / 7 / 11 | DYNAMIC_USAGE_POSSIBLE |
| AST-035 | RainSnowCloudEffect | 2.48 | 37 | 15 / 0 / 12 / 8 | ACTIVE_PRODUCTION |
| AST-036 | S23 NEW ASSETS/Industrial Props | 5.47 | 50 | 11 / 11 / 18 / 7 | ACTIVE_PRODUCTION |
| AST-037 | S23 NEW ASSETS/Industrial building | 9.57 | 29 | 0 / 22 / 1 / 4 | ACTIVE_PRODUCTION |
| AST-038 | S23 NEW ASSETS/factory_brick_4k.blend | 94.13 | 5 | 0 / 1 / 0 / 4 | DYNAMIC_USAGE_POSSIBLE |
| AST-039 | Samples/UnityURPTemplate | 0.06 | 2 | 0 / 0 / 0 / 0 | DYNAMIC_USAGE_POSSIBLE |
| AST-040 | Shipping Container Environment | 24.83 | 78 | 35 / 35 / 2 / 4 | ACTIVE_PRODUCTION |
| AST-041 | Standard Assets | 0.30 | 8 | 0 / 0 / 0 / 1 | REFERENCED_LEGACY |
| AST-042 | TreeChopping-main | 0.02 | 5 | 1 / 0 / 0 / 0 | UNREFERENCED_CANDIDATE |
| AST-043 | UI/kenney_input-prompts_1.5 | 17.29 | 4667 | 0 / 0 / 0 / 3056 | DYNAMIC_USAGE_POSSIBLE |
| AST-044 | Unity-Tree-Chopping-Simulation-main | 25.05 | 8 | 0 / 0 / 0 / 0 | UNREFERENCED_CANDIDATE |
| AST-045 | VFX/BloodDecals | 12.72 | 17 | 0 / 0 / 4 / 12 | DYNAMIC_USAGE_POSSIBLE |
| AST-046 | VFX/Free Blood VFX | 66.69 | 155 | 62 / 7 / 18 / 63 | ACTIVE_PRODUCTION |
| AST-047 | VGtiXN/Countryside gas station | 487.44 | 292 | 59 / 53 / 31 / 135 | ACTIVE_PRODUCTION |
| AST-048 | Vehicles/CharacterController | 0.21 | 17 | 0 / 0 / 0 / 0 | UNREFERENCED_CANDIDATE |
| AST-049 | Vehicles/Pack_Pickup | 0.88 | 12 | 1 / 1 / 5 / 2 | ACTIVE_PRODUCTION |
| AST-050 | Vehicles/Stylized Vehicles Pack Free | 1.96 | 22 | 5 / 5 / 6 / 3 | DYNAMIC_USAGE_POSSIBLE |
| AST-051 | Weapons/CrowbarLowPoly | 0.55 | 3 | 0 / 0 / 0 / 2 | DYNAMIC_USAGE_POSSIBLE |
| AST-052 | Weapons/Gece Studio | 209.30 | 11 | 2 / 1 / 2 / 5 | DYNAMIC_USAGE_POSSIBLE |
| AST-053 | Weapons/MRPoly | 0.68 | 41 | 16 / 3 / 19 / 1 | ACTIVE_PRODUCTION |
| AST-054 | Weapons/PISTOLS/DXT_ready | 36.53 | 11 | 0 / 2 / 0 / 8 | DYNAMIC_USAGE_POSSIBLE |
| AST-055 | Weapons/PISTOLS/Glock17 | 16.45 | 9 | 0 / 3 / 0 / 4 | DYNAMIC_USAGE_POSSIBLE |
| AST-056 | Weapons/PISTOLS/pistol-9mm-custom | 499.74 | 73 | 0 / 1 / 0 / 72 | DYNAMIC_USAGE_POSSIBLE |
| AST-057 | Weapons/machete_4k.blend | 56.47 | 5 | 0 / 1 / 0 / 4 | ACTIVE_PRODUCTION |
| AST-058 | Weapons/wooden_axe_4k.blend | 33.14 | 5 | 0 / 1 / 0 / 4 | ACTIVE_PRODUCTION |
| AST-059 | Zombies/ArtStore3D/Fat Zombie(Low Poly) | 60.03 | 19 | 2 / 2 / 2 / 8 | DYNAMIC_USAGE_POSSIBLE |
| AST-060 | Zombies/NewPunch/ShirtlessZombieFree | 92.13 | 34 | 6 / 2 / 11 / 11 | ACTIVE_PRODUCTION |
| AST-061 | Zombies/SZombie | 42.51 | 15 | 0 / 5 / 0 / 10 | ACTIVE_DEVELOPMENT |
| AST-062 | Zombies/Zombie | 845.93 | 42 | 1 / 14 / 8 / 15 | DYNAMIC_USAGE_POSSIBLE |
| AST-063 | Zombies/ZombieMale_AAB | 106.54 | 58 | 6 / 2 / 20 / 25 | DYNAMIC_USAGE_POSSIBLE |
| AST-064 | Zombies/Zombie_Survival_AssetPack_LowPoly | 1.00 | 4 | 0 / 3 / 0 / 1 | DYNAMIC_USAGE_POSSIBLE |
| AST-065 | asphalt_02_4k.blend | 84.44 | 5 | 0 / 1 / 0 / 4 | ACTIVE_DEVELOPMENT |
| AST-066 | fern_02_4k.blend | 16.30 | 5 | 0 / 1 / 0 / 4 | ACTIVE_DEVELOPMENT |
| AST-067 | fir_tree_01_4k.blend (1) | 912.31 | 19 | 0 / 1 / 0 / 18 | ACTIVE_DEVELOPMENT |
| AST-068 | forrest_ground_03_4k.blend | 92.45 | 5 | 0 / 1 / 0 / 4 | UNREFERENCED_CANDIDATE |
| AST-069 | kloppenheim_01_puresky_4k.exr | 67.34 | 1 | 0 / 0 / 0 / 1 | ACTIVE_PRODUCTION |
| AST-070 | qwantani_night_puresky_4k.exr | 69.69 | 1 | 0 / 0 / 0 / 1 | ACTIVE_PRODUCTION |
| AST-071 | qwantani_sunset_puresky_4k.exr | 66.45 | 1 | 0 / 0 / 0 / 1 | ACTIVE_PRODUCTION |
| AST-072 | tree_stump_01_4k.blend | 51.55 | 4 | 0 / 1 / 0 / 3 | DYNAMIC_USAGE_POSSIBLE |

Dosya türü sayımları dosyadır; FBX içindeki gömülü animasyon/mesh/material subasset sayısını temsil etmez. SVG/fontlar extension tablosunda bulunur, raster texture sütununa dahil değildir. Sahne kapalı olsa da Resources ve ProjectSettings kökleri ile kodda yol kullanımları ayrıca işaretlenmiştir. `UNREFERENCED_CANDIDATE` kullanılmıyor veya silinebilir demek değildir.
