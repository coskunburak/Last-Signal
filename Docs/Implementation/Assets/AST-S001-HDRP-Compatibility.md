# AST-S001 — HDRP, kalite ve öncelikler

Unity 6000.5.0f1 / HDRP 17.5.0 yerel dosyalarda doğrulandı. GraphicsSettings GUID `15722f09b35214c7cac8dee7a2a4c2e3` High profile işaret eder; kalite seviyelerinin override GUID'leri ayrıca korunur. Pipeline değiştirilmedi.

**26 birimde 383 materyal HDRP shader referansı taşıyor.** Bu çalışan görsel çıktı veya orijinal native provenance kanıtı değildir. Bu nedenle test edilmeden `NATIVE_HDRP` / `CONVERTED_HDRP` sertifikasyonu yapılmadı. 36 PIPELINE_AGNOSTIC, 11 HDRP_MIGRATION_REQUIRED, 2 BUILTIN_LEGACY, 23 UNKNOWN. Agnostik model sınıfı ham geometri/tekstür/animasyon tekrar kullanımını anlatır; gömülü FBX materyallerinin HDRP uyumluluğu doğrulanmış değildir.

| Birim | Sınıf | Mat shader sayımı | Sonraki iş |
| --- | --- | --- | --- |
| AST-001 Abandoned buildings | UNKNOWN | {"HDRP": 9} | Keep as-is pending verification |
| AST-002 Universal Animation Library 2[Standard] 2 | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-003 Universal Animation Library[Standard] 2 | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-004 Zombie Animation | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-005 CrouchAndSlide | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-006 Zombie Animations | UNKNOWN | {"HDRP": 2} | Keep as-is pending verification |
| AST-007 ZOMBİE ATTACK ANIMATION | UNKNOWN | {"UNKNOWN": 3} | Keep as-is pending verification |
| AST-008 Garage_props | UNKNOWN | {"HDRP": 6} | Keep as-is pending verification |
| AST-009 Essentials_Series_NOX_SOUND | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-010 FreeWeaponSFX_v1.1 | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-011 Horror_Music_Pack_Starter_Kit | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-012 Tree | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-013 ZombieHorrorPackageFree | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-014 Diesel | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-015 FirstPersonArms | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-016 Fps Arms Pack [Updated] FBX | UNKNOWN | {"HDRP": 7} | Keep as-is pending verification |
| AST-017 Survival Char | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-018 VAL | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-019 BarnExtracted | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-020 FantasyEnvironments | HDRP_MIGRATION_REQUIRED | {"BUILTIN": 13, "HDRP": 20} | Keep source; review HDRP adaptation |
| AST-021 HolotnaMountainURP | HDRP_MIGRATION_REQUIRED | {"URP": 14} | Keep source; review HDRP adaptation |
| AST-022 Hand_Painted_Nature_Kit_LITE | HDRP_MIGRATION_REQUIRED | {"HDRP": 2, "BUILTIN": 1} | Keep source; review HDRP adaptation |
| AST-023 LowPolySci-FiStarterPack | UNKNOWN | {"HDRP": 8} | Keep as-is pending verification |
| AST-024 cupboard_pack.fbx | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-025 Factory Tools | UNKNOWN | {"UNKNOWN": 1} | Keep as-is pending verification |
| AST-026 Fantasy Skybox FREE | BUILTIN_LEGACY | {"BUILTIN": 50} | Keep source; review HDRP adaptation |
| AST-027 FirstPersonARPGStarterPack | UNKNOWN | {} | Keep as-is pending verification |
| AST-028 Free Wood Door Pack | HDRP_MIGRATION_REQUIRED | {"BUILTIN": 1, "HDRP": 110} | Keep source; review HDRP adaptation |
| AST-029 MedicinePack_JimmyPV | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-030 Polygon-Lite Survival Collection | UNKNOWN | {"HDRP": 11} | Keep as-is pending verification |
| AST-031 stylooFoodAndKitchenAssetPackFreePack | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-032 MarpaStudio | UNKNOWN | {"HDRP": 43} | Keep as-is pending verification |
| AST-033 Modular European House | HDRP_MIGRATION_REQUIRED | {"UNKNOWN": 1, "HDRP": 20} | Keep source; review HDRP adaptation |
| AST-034 RainMaker | HDRP_MIGRATION_REQUIRED | {"HDRP": 1, "UNKNOWN": 3, "BUILTIN": 3} | Keep source; review HDRP adaptation |
| AST-035 RainSnowCloudEffect | UNKNOWN | {"HDRP": 12} | Keep as-is pending verification |
| AST-036 Industrial Props | UNKNOWN | {"HDRP": 18} | Keep as-is pending verification |
| AST-037 Industrial building | UNKNOWN | {"HDRP": 1} | Keep as-is pending verification |
| AST-038 factory_brick_4k.blend | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-039 UnityURPTemplate | UNKNOWN | {} | Keep as-is pending verification |
| AST-040 Shipping Container Environment | BUILTIN_LEGACY | {"BUILTIN": 2} | Keep source; review HDRP adaptation |
| AST-041 Standard Assets | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-042 TreeChopping-main | UNKNOWN | {} | Keep as-is pending verification |
| AST-043 kenney_input-prompts_1.5 | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-044 Unity-Tree-Chopping-Simulation-main | UNKNOWN | {} | Keep as-is pending verification |
| AST-045 BloodDecals | HDRP_MIGRATION_REQUIRED | {"UNKNOWN": 3, "HDRP": 1} | Keep source; review HDRP adaptation |
| AST-046 Free Blood VFX | HDRP_MIGRATION_REQUIRED | {"BUILTIN": 1, "HDRP": 17} | Keep source; review HDRP adaptation |
| AST-047 Countryside gas station | HDRP_MIGRATION_REQUIRED | {"HDRP": 28, "BUILTIN": 3} | Keep source; review HDRP adaptation |
| AST-048 CharacterController | UNKNOWN | {} | Keep as-is pending verification |
| AST-049 Pack_Pickup | UNKNOWN | {"HDRP": 5} | Keep as-is pending verification |
| AST-050 Stylized Vehicles Pack Free | UNKNOWN | {"HDRP": 6} | Keep as-is pending verification |
| AST-051 CrowbarLowPoly | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-052 Gece Studio | UNKNOWN | {"HDRP": 2} | Keep as-is pending verification |
| AST-053 MRPoly | UNKNOWN | {"HDRP": 19} | Keep as-is pending verification |
| AST-054 DXT_ready | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-055 Glock17 | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-056 pistol-9mm-custom | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-057 machete_4k.blend | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-058 wooden_axe_4k.blend | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-059 Fat Zombie(Low Poly) | UNKNOWN | {"HDRP": 2} | Keep as-is pending verification |
| AST-060 ShirtlessZombieFree | UNKNOWN | {"HDRP": 11} | Keep as-is pending verification |
| AST-061 SZombie | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-062 Zombie | HDRP_MIGRATION_REQUIRED | {"UNKNOWN": 3, "HDRP": 5} | Keep source; review HDRP adaptation |
| AST-063 ZombieMale_AAB | HDRP_MIGRATION_REQUIRED | {"HDRP": 17, "UNKNOWN": 2, "BUILTIN": 1} | Keep source; review HDRP adaptation |
| AST-064 Zombie_Survival_AssetPack_LowPoly | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-065 asphalt_02_4k.blend | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-066 fern_02_4k.blend | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-067 fir_tree_01_4k.blend (1) | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-068 forrest_ground_03_4k.blend | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-069 kloppenheim_01_puresky_4k.exr | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-070 qwantani_night_puresky_4k.exr | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-071 qwantani_sunset_puresky_4k.exr | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |
| AST-072 tree_stump_01_4k.blend | PIPELINE_AGNOSTIC | {} | Keep as-is pending verification |

## Üretim öncelikleri

1. **European Forests / Assets/Realistic Tree** ThirdParty dışında. Mevcut ForestPrototype120/HANDOFF bu kimliği kaynak yolu ile ilişkilendiriyor; paket kaynağı/lisansı ayrıca doğrulanmalı. HDRP prefabları ve proje türevleri korunmalı. Canlı görsel kalite henüz kabul edilmedi.
2. **Fir / fern / forest ground** gerçek kaynakları ThirdParty altında. Geometri ve PBR kaynakları yeniden kullanılabilir. Proje türevleri mevcut. Devam aşamasında üç eski authoring sabiti gerçek ThirdParty yollarına düzeltildi; dosya varlığı doğrulandı. Yeniden üretim yapılmadı, Editor yükleme doğrulaması bekliyor.
3. **FantasyEnvironments** town/forest kaynak ailesi, karışık shader durumu. Aktif kullanıldığı için eski tarz nedeniyle çıkarılmaz. Kaya/çalı/çim atlası için önceki forest raporu gerçekçilik sınırı belirtiyor: **PRODUCTION_READY_WITH_FIXES önerisi yalnız entegrasyon değerlendirmesi**, görsel kabul değildir.
4. **Asphalt, ground, Garage_props, Modular European House, MarpaStudio, Countryside gas station, Shipping Container** town materyal/mesh bağımlılıkları. Karışık pipeline dağıtımlarını bölmeyin; model ve tekstürleri koruyun.
5. **RainMaker / RainSnowCloudEffect / Fantasy Skybox** özel shader ve eski skybox davranışları ayrıca kontrol edilmeli; HDRP materyal atanması weather kodunun/efektin doğru çalıştığını kanıtlamaz.
6. **Diesel, VAL, Survival Char, zombie, weapon ve animasyon paketleri** proje türevleri, Resources ve oluşturma araçları nedeniyle korunmalı. Ses/animasyon görsel stil üzerinden derecelendirilmedi.

## Orman için ölçüm ve iyileştirme kuyruğu

Önceki `Docs/Implementation/ForestPrototype120/HANDOFF.md` ölçümleri: fir kaynak LOD0 **4.176.819**, LOD1 **177.444**, LOD2 **70.661** üçgen; bu sprintte yeniden ölçülmedi. Mevcut `ForestPrototypeAssets.cs` hero için yalnız a_LOD1/a_LOD2 seçiyor, LOD0'ı kullanmıyor; 180.000 üçgen üstü için guard var. Fir capsule collider ve iki LOD kullanıyor. Fern geometrileri önceki ölçümde 784/2.384/2.248/816 üçgen; tek cull LOD, gölge kapalı ve collider yok. European spruce dört LOD, düzey başına üç renderer; proje türevlerinde capsule collider mevcut.

Sonraki performans sprinti: ağaç shadow mesafesi/cascade maliyeti, LOD başına renderer/material sayısı, tek fir maliyeti, fern gerçek geometri LOD seçenekleri. Bu sprint geometri, shadow, collider veya materyal ayarı değiştirmedi; klasör taşımak FPS kazandırmaz.

Texture boyutları Pillow header okumasıyla `reviewed-inventory.json` içine kaydedildi. EXR/okunamayan başlıklar UNKNOWN; normal/PBR dosya adı işaretleri doğru kanal içeriğini kanıtlamaz. Çoğu pakette görsel inceleme olmadığı için kalite **UNKNOWN_VISUAL_REVIEW_REQUIRED** bırakıldı; keyfi PROTOTYPE_ONLY/UNSUITABLE kararı verilmedi. Owner Editor audit güncel prefab triangle toplamı (tüm LOD'lar birlikte, frame maliyeti değil), LOD eşikleri, shader adları, collider ve shadow bilgisi üretir.
