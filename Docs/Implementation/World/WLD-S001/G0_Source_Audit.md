# WLD-G0 — kaynak ve koruma denetimi

**IMPLEMENTED / OWNER_ACCEPTANCE_PENDING — 24 Ekim 2026.** Sahne/terrain mutasyonu yok. Kaynaklar güncel yerel dosyalardan ve açık sahnenin salt okunur native envanterinden doğrulandı. Test, Play, benchmark, bake ve world generation NOT_RUN.

## Çalışma tabanı

- Branch: `s012-integrated-graybox-slice`; HEAD: `a446d8d9daab09bdb7a9f99332cd1d51ad181581`.
- WLD başlamadan Git çalışma ağacı temizdi. Önceki SCN işi artık bu yerel commit'te; uzaktan branch varsayılmadı.
- Başlangıç boş alanı: 13.81 GiB. G0/G1 büyük asset kopyası oluşturmaz. Sonraki G2 için disk/geri dönüş maliyeti yeniden kontrol edilmeli.
- Açık sahne: FirstTownMain_HDRP; ilk native kontrolde saved/Play=false/tek sahne. Kod importu sonrasındaki envanterde scene.isDirty=true görüldü. Kaydedilmedi veya atılmadı. ProtectedObjectInventory bellekteki durumdur; diskteki SourceReceipt'ten ayrı işaretlenmiştir.
- Yeni WLD stage adı ve Generations dizini başlangıçta yok. Build ayarı ve production başlangıç dünyası değiştirilmedi.

## Kaynak karşılaştırması

| Sahne | SessionFlow | ResourceNode / PhysicsTreeBody | Aktif TerrainData GUID |
|---|---:|---:|---|
| FirstTownCore.unity | 1 | 0 / 0 | `4ed9e3eebc069497cbc7f24d4e7cd414` |
| FirstTownMain_HDRP.unity | 1 | 3 / 0 | `e4ed8e8a1e0a4468874f95beffdfc638` |
| ForestPrototype120.unity | 0 | 0 / 0 | `13c8ab97319d24a7c8f7bfb0f8a91725` |
| FirstTownTreePhysics_20261019103726-320e36cd.unity | 1 | 3 / 1 | `49b9c2adb309845b3ad35ccb4336a6f6` |
| FirstTownTreePhysics_20261019102748-99bfd179.unity | 1 | 3 / 0 | `49b9c2adb309845b3ad35ccb4336a6f6` |
| FirstTownTreePhysics_20261019163434-8e0f05b7.unity | 1 | 3 / 1 | `49b9c2adb309845b3ad35ccb4336a6f6` |

Tam yollar, scene GUID/hash ve TerrainData yolları SourceScenes.json içinde. Main'de 53 persistent identity, 39 kapı, 346 serialized VegetationShadowController var. Core kaynak/üretim referansı olarak korunuyor. Bu sprint için önerilen görsel staging tabanı mevcut Main_HDRP: önceki prototype aktarımını yeniden kaybetmemek için. Main'in adı, üretim kabulü verildiği anlamına gelmiyor.

İki ileri S004 stage'inde yalnız birer fizik gövdesi bağlı. İlk tarihsel stage ve Main'deki üç ResourceNode.physicsBody=null. “En eski stage en gelişmiş” veya “tree cutting çalışıyor” sonucu çıkarılmadı. Yeni modele cutter/notch/physics aktarımı ayrı eşleme/owner kontrolü gerektirir.

## Arazi ve mekân

Main aktif TerrainData: `Assets/LastSignal/Art/FirstTownMain_HDRP/Isolated/TerrainData/Terrain_49b9c2ad.asset`; GUID `e4ed8e8a1e0a4468874f95beffdfc638`. Terrain ve TerrainCollider aynı assete bağlı. Köken (-250,-10,-250), boyut (500,20,500), heightmap 513². Native dünya yükseklik aralığı **-3.499969…6.353537 m**. Önceki küçük sırtlar makro dünya problemini çözmüş sayılmıyor.

Native merkez doğrulamaları: safehouse (-180,-170), town (0,30), gas (65,-82), industry (168,-65), residential (125,115); player (-178.2,.05,-169), vehicle (-160,.2,-156). School/ranger/farm tasarım zarfları HANDOFF/generator kaynaklı; son collider temelli sınırlar native maskelerden gelir, eski tablonun her sınırı ölçülmüş sayılmaz. +Z kuzey koordinat sözleşmesi HANDOFF'tan; kullanıcının kesit görselinin arka kenarı kuzey kabul edilmedi.

`FirstTownGeometry.GroundAndRoads`: çoğunlukla plato, x=-112+7sin(.018z) dere, düşük sınır tepeleri. `Road`: dört köşeli sabit .021m mesh ve düz box roadbed. Yol üzerindeki çizgiler/pavements/junction collider'ları da ileride birlikte ele alınmalı. İki köprü z=-140/80; korunur.

`ForestTerrain` ve `ForestSite` sabit -250/500/239 varsayımları taşıyor. `ForestDistribution` deterministik RNG, spacing bins, kümeli understory ve ayrı generated root kullanıyor. Yeni planner aynı fikirleri kullanır; eski Build/Shape/Scatter/Promote çağrılmaz. ForestSources eski S013 stylized shader/tek cull-LOD üretir; yeni gerçekçi ağaca kaynak olarak tekrar kullanılmaz. Main'in PrototypeVisuals türevleri sonraki G5 için kaynak adayıdır.

## Korunan nesneler ve perimeter

Native salt okunur envanter: **5290 koruma bound'u, 295 kimlik/route/spawn kaydı**, tek SessionFlow, missing script=0. Inactive kimlikler de kayıt altındadır. Mevcut aktif non-trigger/non-terrain collider'lar korunur; yalnız belgeli generated dekor alt ağaçları hariç. HeroTrees, artistContent, ResourceNode ve artistExclusions yeniden koruma katmanına alınır. Plan ek olarak 14 yol + 3 orman rotası ve 13 temel/bridge zarfı içerir.

`00 Landscape` doğrudan altındaki ±245 koordinatlı **196 perimeter adayı** GlobalObjectId ile kaydedildi. Şimdilik hiçbiri kaldırılmadı. Bu aday listesini gelecekte exact source→stage eşlemesiyle doğrulamadan silme yetkisi yok. Safehouse, farm, industrial güvenlik çitleri/gate'ler kapsam dışı ve korunur. Sonraki üretim reçetesinde perimeter geri üretilmemeli.

## Mimari engel — G2/G3'e geçmeden çözülmeli

VehicleWorld.residentBounds = center(0,10,0), size(500,100,500). `SaveSession.ValidateTopology` dışarıdaki kayıtlı pickup için **Pickup is outside resident ownership** döndürür. Batı tile sahiden oynanabilir olacaksa aynı resident otoritesinin sahneye özgü sınırını genişletmek, kayıt/yükleme, araç, resident pressure ve NavMesh davranışını doğrulamak gerekir. Yeni cell/portal/weather manager çözüm değildir. Bu nedenle talimat §6 gereği gerçek tile oluşturma mimari kapıda durur; sessizce desteklenmeyen dünya sahipliği eklenmez.

## Koruma matrisi ve geri dönüş

| İçerik | Politika / geri dönüş |
|---|---|
| Altı kaynak sahne + bağlı dört terrain + meta | SourceReceipt içinde SHA-256; 23 seçili dosya (package/build config dahil), unchanged |
| Mevcut runtime, legacy authoring, graphics, save | Hiçbiri edit edilmedi; tüm WLD kodu yeni Editor dizininde |
| Scene mutable bağımlılıklar | G2 öncesi TerrainData/TerrainLayer/Volume/material/NavMesh/authoring configs kopya ve referans receipt'i zorunlu; SCN izolasyon listesi başlangıç kanıtıdır |
| Vendor kaynakları | Taşıma, import, conversion, silme yok. Örnek Spruce prefab Git'te; fern/ground vendor dizinleri Git'te değil. Tam vendor recovery iddia edilmez |
| Checkpoint | Önceki SCN PromotionCheckpoint.json 184-dosya before-image yolu içeriyor; WLD öncesi HEAD ve seçili source receipt ayrıca kaydedildi. SourceReceipt içerik yedeği değildir |
| Yeni planner | Yalnız CHANGED_FILES listesindeki WLD dosyaları kaldırılarak geri alınabilir; source scene reset/restore gerekmez. Sonraki owner düzenlemelerini önce diff ile koruyun |

AST-066 fern, AST-068 forest ground: pipeline-agnostic / LICENSE_UNVERIFIED; AST-067 ağır Fir yoğun dağıtım için uygun değil; AST-022 hand-painted kaynak realism adayı değil. European spruce/birch kaynak provenance'i SCN belgesinde hâlâ bekliyor. Yeni asset indirilmedi, katalog yeniden taranmadı. Art Bible, Terrain/Foliage Spec ve Performance Budgets okundu; yeni genişleme talebi eski 120m pilot sınırını plan düzeyinde genişletir, gameplay sahipliği otomatik genişlemez.
