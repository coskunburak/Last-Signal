# P01 asset ve authoring kaydı

S003: yeni external import/download/purchase **0**. Agent native Editor authoring **NOT_RUN**. Yeni scene, prefab, SO, material, terrain veya mesh asset'i bu çalışma sırasında üretilmedi. Hazırlanan şey C# authoring yoludur.

| Kaynak | Kullanım / sınır |
|---|---|
| `Assets/LastSignal/Data/Combat/S021/WoodenAxe.asset` | Mevcut combat definition; timing/grip/stamina aynen |
| `Assets/LastSignal/Data/Items/Definitions/weapon.axe.asset` | worldPrefab null; pickup eklenmedi; gerçek owned melee kullanılır |
| `Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/Log.prefab` | Dekoratif bark geometri, collider'sız; owner tooling görünür mesh/material ref'lerini yeni wrapper içinde kullanır |
| Aktif VisualSlice içindeki seçili Pine/Birch prefab roots | Tam 2 Pine + 1 Birch, Generated altında, HeroTrees dışında, >=7 m spacing; orman toplu dönüşmez |
| Mevcut material.scrap icon | Yeni log için açıkça provisional ikon; S004 UX/art kabulü değil |

Yerel türev asset varlığı yeni lisans doğrulaması değildir. Bağlı araştırma paketinin Poly Haven axe CC0 ve FantasyEnvironments provenance gözlemleri arka plandır; entitlement/release-clearance bu çalışmada web'den yeniden doğrulanmadı. S004 audio adayları indirilmedi, dinlenmedi, onaylanmadı: **AUDIO_ASSET_PENDING_OWNER**. Source/vendor shader/mesh/texture veya forest generation dosyası değiştirilmedi.

## Owner-run üretim çıktıları (henüz yok)

Menu: `Last Signal/BLD/P01/Create Isolated S003 Pilot From Three Selected Trees` → `LastSignal.Editor.HarvestPilotAuthoring.CreatePilot`.

Her açık owner çağrısı yeni timestamp+GUID suffix üretir:

- `Assets/LastSignal/Scenes/Validation/FirstTownHarvest_<stamp>.unity`
- `Assets/LastSignal/Data/Gathering/Pilot-<stamp>/Log.asset`, `PilotCatalog.asset`, `Pine.asset`, `Birch.asset`, `Manifest.asset`
- `Assets/LastSignal/Prefabs/Gathering/Pilot-<stamp>/HarvestLog.prefab`
- Bu native çıktıların yeni Unity `.meta` dosyaları. Eski GUID'ler değiştirilmez.

Save slot `harvest_<stamp>`. Yeni catalog eski bütün entries + log içerir, yalnız yeni sahnedeki SaveSession/WorldCellManager'a bağlanır. Kaynak catalog/scene kaydedilmez. Seçili ağaçlar **kopyada** Generated dışındaki overlay'e alınır; transform/foliage/material referansları korunur. Geri kalan dekoratif forest ve HeroTrees kopyada yerinde kalır. Otomatik forest regen, nav bake veya promote yok. Sahnenin authoring sayaçları kaynak provenance olarak kalır; stage'de forest generation aracı yeniden çalıştırılmamalıdır.

Aracın önkoşulu: tek açık sahne, kayıtlı/temiz `FirstTownCore_VisualSlice`, geçerli save/catalog, gerçek heavyMelee axe ve tam seçim. Şu an kaynak VisualSlice dirty olduğu için owner checkpoint/save kararı olmadan bu adım çalıştırılmaz. Owner mevcut değişikliklerini isteyerek kaydeder; araç dirty sahneyi kendisi kaydetmez. Source SHA256 copy öncesi/sonrası kontrolü vardır. Hata olursa yalnız yeni draft çıktıları kalabilir; otomatik cleanup/source restore yok. Aynı pilotu tekrar üretmek GUID migration değildir; yeniden üretim yeni pilot/saveSlot demektir.

Üretilmiş sahne sourceNavMeshData/terrain referanslarını kullanır. Native geometrik açıklık, nav yürünebilirlik, wind/LOD, ground placement, oyuncunun seçili ağaca erişimi ve art kabulü **PENDING_OWNER**. Bu adım shader, terrain veya production sahneye promote yetkisi vermez.
