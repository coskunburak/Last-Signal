# AST-S001 — Legacy, tekrarlar ve lisans

**Hiçbir silme/dedup işlemi yapılmadı.** URP/Built-in kaynakların mesh, texture, animasyon ve ses içeriği korunur. Rendering script ve shader katmanı ayrı değerlendirilir. Materyaller topluca dönüştürülmedi.

| Paket | MiB | Referans sınıfı | Pipeline | Öneri |
| --- | --- | --- | --- | --- |
| AST-020 FantasyEnvironments | 218.67 | ACTIVE_PRODUCTION | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-021 HolotnaMountainURP | 32.72 | ACTIVE_DEVELOPMENT | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-022 Hand_Painted_Nature_Kit_LITE | 230.28 | ACTIVE_DEVELOPMENT | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-026 Fantasy Skybox FREE | 133.57 | ACTIVE_PRODUCTION | BUILTIN_LEGACY | Koruyun; inceleme gerekli |
| AST-028 Free Wood Door Pack | 662.28 | ACTIVE_PRODUCTION | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-033 Modular European House | 203.54 | ACTIVE_PRODUCTION | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-034 RainMaker | 16.07 | DYNAMIC_USAGE_POSSIBLE | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-040 Shipping Container Environment | 24.83 | ACTIVE_PRODUCTION | BUILTIN_LEGACY | Koruyun; inceleme gerekli |
| AST-042 TreeChopping-main | 0.02 | UNREFERENCED_CANDIDATE | UNKNOWN | Koruyun; inceleme gerekli |
| AST-044 Unity-Tree-Chopping-Simulation-main | 25.05 | UNREFERENCED_CANDIDATE | UNKNOWN | Koruyun; inceleme gerekli |
| AST-045 BloodDecals | 12.72 | DYNAMIC_USAGE_POSSIBLE | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-046 Free Blood VFX | 66.69 | ACTIVE_PRODUCTION | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-047 Countryside gas station | 487.44 | ACTIVE_PRODUCTION | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-048 CharacterController | 0.21 | UNREFERENCED_CANDIDATE | UNKNOWN | Koruyun; inceleme gerekli |
| AST-062 Zombie | 845.93 | DYNAMIC_USAGE_POSSIBLE | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-063 ZombieMale_AAB | 106.54 | DYNAMIC_USAGE_POSSIBLE | HDRP_MIGRATION_REQUIRED | Koruyun; inceleme gerekli |
| AST-068 forrest_ground_03_4k.blend | 92.45 | UNREFERENCED_CANDIDATE | PIPELINE_AGNOSTIC | Koruyun; inceleme gerekli |

Bu satırların toplamı disk tasarrufu değildir. Her adayın exact path/bytes/referencing_scenes/dynamic_usage_possible/license alanları katalogda; direct consumer ve path satırları reviewed inventory'de. UNREFERENCED_CANDIDATE kapsamı static+implicit taramadır; silme için runtime/editor/provenance incelemesi açık kalır. Legacy shader örnekleri ve vendor demoları bağımlılık kapanışı doğrulanmadan ayrılmamalı. Demo sahnelerinin GUID dependency kaynağı olması mümkün; sahne dosyası küçük olsa da texture silme izni doğurmaz.

**598 byte-identical grup; teorik tekrarlanan kaynak üst sınırı 117.821.650 bayt = 112,36 MiB.** Meta dosyaları tekrar hesabına dahil değil; aynı içerik farklı GUID ve importer ayarları taşıyabilir. Bu değerleri paket kaldırma varsayımlarıyla toplamayın. Güvenle silinebilir doğrulanmış miktar: **0 bayt**.

| Aynı içerik örneği | Teorik üst sınır MiB | Risk |
| --- | --- | --- |
| ['Assets/ThirdParty/Zombies/Zombie/textures/Texture1/2_Occlusion.tga', 'Assets/ThirdParty/Zombies/Zombie/textures/Texture3/1_Occlusion.tga'] | 96.00 | Farklı GUID/importer/referans; otomatik birleştirme yok |
| ['Assets/ThirdParty/Characters/Diesel/Diesel.fbx', 'Assets/ThirdParty/Characters/Diesel/S14 Source/Diesel.fbx'] | 12.51 | Farklı GUID/importer/referans; otomatik birleştirme yok |
| ['Assets/ThirdParty/Frameworks/FirstPersonARPGStarterPack/Models/Textures/URP/Marc B Weapons_Material.001_AlbedoTransparency.png', 'Assets/ThirdParty/Frameworks/FirstPersonARPGStarterPack/Models/Textures/HDRP/Marc B Weapons_Material.001_BaseColor.png'] | 1.00 | Farklı GUID/importer/referans; otomatik birleştirme yok |
| ['Assets/ThirdParty/Frameworks/FirstPersonARPGStarterPack/Models/Textures/URP/Marc B Weapons_Material.001_Normal.png', 'Assets/ThirdParty/Frameworks/FirstPersonARPGStarterPack/Models/Textures/HDRP/Marc B Weapons_Material.001_Normal.png'] | 0.91 | Farklı GUID/importer/referans; otomatik birleştirme yok |
| ['Assets/ThirdParty/Animation/AnimationLibraries/Universal Animation Library[Standard] 2/Godot_Setup.png', 'Assets/ThirdParty/Animation/AnimationLibraries/Universal Animation Library 2[Standard] 2/Godot_Setup.png'] | 0.48 | Farklı GUID/importer/referans; otomatik birleştirme yok |

Tam SHA-256, meta hash ve referans listeleri `Evidence/20261023T114820429441Z/duplicates.json` içinde. TGA tekrar grubu yaklaşık 96 MiB, Diesel FBX tekrarı yaklaşık 12,51 MiB; Diesel kopyalarının GUID'leri farklı, önceki mimari belgesi ikisini koruma kararını kaydediyor.

## Lisans/provenance

3 birimde yerel CC0 beyanı bulundu: iki Quaternius Universal Animation Library ve Kenney Input Prompts. Kenney License.txt credit zorunlu değil diyor. Bu yerel dosya kanıtıdır; dosya kapsamı ve sonradan eklenen içerik ayrıca incelenmeli. Kalan **69 birim LICENSE_UNVERIFIED**, üretim dağıtımı öncesi kanıt bekler. Source URL alanları yalnız yerel belgede görülen metindir; bağlantılar ziyaret edilmedi. Poly Haven benzeri dosya adlarından CC0 varsayılmadı.

Digital Ruby/Jeff Johnson (RainMaker), MutantGopher (Shipping Container) yerel readme ile ilişkilendirildi; creator kanıtı lisans hakkı kanıtı değildir. Lisanslar değiştirilmedi/yeniden adlandırılmadı. PDF readme'ler kanıt yolu olarak kaydedildi, lisans metni doğrulanmış sayılmadı.

Sonraki owner-onaylı cleanup için her aday ayrı manifest: exact path, hash, meta/GUID, shader/importer farkı, tüm runtime/editor tüketicileri, lisans/provenance kaydı, geri dönüş kopyası ve ölçülen byte miktarı. Ayrı onay olmadan silme yok.
