# SCN-S001 M0 — kaynak sahne denetimi

23 Ekim 2026. **VERIFIED (statik/native asset okuma)**. Görsel, oynanış ve performans kabulü **NOT_RUN / OWNER_ACCEPTANCE_PENDING**. M0 incelemesi ve M1 korumalı aday uygulandı; M2 yalnız ön seçimdir. M3–M9 uygulanmadı.

Başlangıç branch: `s012-integrated-graybox-slice`; HEAD: `3fa3161f0d3d22f40d422fd5b404605f25088ce0`. Başlangıçtaki dirty envanter `Evidence/git-status-before.txt`; mevcut değişiklikler commit/reset/stash yapılmadan korundu. Yeni branch oluşturulmadı. Unity 6000.5.0f1 ve paket 17.5.0 yerel ayarlardan, aktif `LastSignal_HDRP_High` açık Editor'den doğrulandı. Editor salt okunur inceleme sırasında `Validation/ç.unity` açıktı, dirty=false, Play kapalıydı. Sahne değiştirilmedi.

## Kaynak kimlikleri ve sonuç

| Kaynak | Gerçek yol | GUID |
|---|---|---|
| A: istenen tarihsel dünya | `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019102748-99bfd179.unity` | `0ffe2d465e8384551b3f451b724f94a6` |
| B: Core | `Assets/LastSignal/Scenes/Production/FirstTownCore.unity` | `94300539ce69847138029a5680eb161b` |
| C: HDRP deney alanı | `Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity` | `d84cde2c502e341cd955c86fcceab00f` |

`TreePhysicsAuthoring.cs` CreateStage, adı `DateTime.UtcNow yyyyMMddHHmmss` + GUID'in ilk sekiz karakteriyle üretir. Bu adlandırma üretilmiş fizik deneme sahnesi biçimidir; aktif ana dünya veya başarılı tamamlanma kanıtı değildir. **A sahnesinde 3 ResourceNodeView, 1 GatheringWorld, fakat 0 TreePhysicsBody bulunuyor; üç physicsBody alanı da null.** Bugünkü `GatheringWorld.TryHit` böyle köklerde `PhysicsNotReady` döndürür. Ağaç kesme tamamlanmış varsayılamaz.

Ek kaynak kontrolü: `...103726-320e36cd.unity` bir fiziksel Birch, `...163434-8e0f05b7.unity` bir fiziksel Pine içeriyor. `Assets/LastSignal/Data/Gathering/Physics-<aynı-ek>/AUTHORING.txt` kayıtları bunların aynı FirstTownHarvest kaynağından üretildiğini, 16 kesim seviyesini, QA ve ses onayının beklediğini gösteriyor. Her iki sahne de korunuyor; M1'e doğrulanmamış fizik hiyerarşisi aktarılmadı. Bunlar sonraki fizik entegrasyonunun somut kaynaklarıdır.

## Karşılaştırma

Aşağıdaki sayılar serialized belge/sahne referanslarıdır; prefabların tüm aktif runtime örnek sayısı veya çizim maliyeti değildir. Ayrıntılar `SourceSceneComparison.json`, yerel AssetDatabase bağımlılık listesi `Evidence/native-dependencies.tsv` içindedir.

| Konu | A: tarihsel dünya | B: Core | C: Forest120 |
|---|---|---|---|
| Yerleşim / ölçek | 500 × 500 × 20 m; VisualSlice + orman + kasaba | 500 × 500 × 20 m; daha temel kasaba/orman | 120 × 120 × 18 m; ayrı koordinat sistemi |
| Aktif TerrainData | VisualSlice `49b9c2adb309845b3ad35ccb4336a6f6` | Forest `4ed9e3eebc069497cbc7f24d4e7cd414` | `13c8ab97319d24a7c8f7bfb0f8a91725` |
| Terrain ayrıntıları | 513 heightmap, 512 holes, 512 detail resolution, 4 katman, 0 terrain tree instance, 1 detail prototype | Aynı boyut/çözünürlük; başka yükseklik/kaplama verisi | 513 heightmap, 512 holes, 256 detail resolution, 4 katman, 0 tree instance, 1 detail prototype |
| Spawn | (-178.20001, 0.05, -169), root Transform 276961210 | Aynı spawn | (48.64,3.9439018,13); doğrudan rig |
| Bootstrap | Tek SessionFlow, aynı Player.prefab | Tek SessionFlow, aynı Player.prefab | SessionFlow yok; ForestPrototypeExplorer |
| Kamera | 16 disabled serialized kamera; gameplay kamera Player prefabından spawn olur | 10 disabled serialized kamera; aynı runtime oyuncu | 1 enabled kamera, 1 AudioListener |
| Işık/atmosfer | Sun + Moon/TownAtmosphere/WorldClock/TownRain; ayrı TownLightingProfile | Aynı otorite; lightingProfile null | ForestPrototypeLighting, Morning/Overcast |
| Global Volume | Tek scene Volume, TownVolume | Aynı ortak TownVolume | Tek scene Volume, Morning; Overcast ayrı referans |
| Yol/yapılar | VisualSlice asfalt/çevre ekleri; güvenli ev, benzinlik, kasaba | Aynı ana POI ve yol bağlantıları; daha az visual-slice içerik | Kasaba/güvenli ev yok |
| Bitki örtüsü | ForestAuthoringState + FirstTownSliceState; 2399 prefab instance belgesi | ForestAuthoringState; 1892 prefab instance belgesi | 2300 prefab instance belgesi; önceki raporda 290 canopy + 1 fir + 1500 fern |
| Ağaç etkileşimi | 3 anchor, fizik bağlantısı yok | GatheringWorld / ResourceNodeView yok | Harvest sistemi yok |
| Save/load | SaveSession, PilotCatalog, world.first-town.v1; harvest_20261018150846-032db4e9 slot | SaveSession, ItemCatalog; first-town-v1 slot | World save yok; prototip grafik/capture araçları |
| Zombi / loot | 13 serialized ZombieEncounter, 39 LootSpawnPoint; WorldPopulationManager | Aynı sayılar/otorite | Kasaba population otoritesi yok |
| Survival / crafting / building | enableSurvival=1; ShelterSite/ShelterLoop/WorldCellManager/RelayMission/VehicleWorld/ProductionAudio mevcut | Aynı otoriteler | Keşif rig'i; bu sistemler yok |
| Materyal / prefab | 64 doğrudan materyal referansı; yolları JSON'da | 47 doğrudan materyal referansı | 2 doğrudan materyal + generated prefab bağımlılıkları |
| Native dependency closure | 1235 | 1181 | 134 |
| Risk | Kaynaktan gelen iki eksik Avatar; eski fizik authoring; tam 500 m dünya maliyeti | Güncel treeShadowRoots var ama gathering yok | Ağır bitki/gölge maliyeti; görsel/performans kabulü yok |

A/B terrain world origin (-250,-10,-250), scale (1,1,1); C origin (0,0,0), scale (1,1,1). Üst Transform zincirleri identity olarak doğrulandı.

Terrain yolları: A `Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538/Terrain.asset`; B `Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/Terrain.asset`; C `Assets/LastSignal/Art/ForestPrototype120/Generations/G20261021101239485/Terrain.asset`. A ayrıca iki baseline TerrainData tutar; bunların ikisi de M1'de ayrıldı. Geometry, holes ve boyama özetlerini yeniden üretmek yerine native verileri okuyup aynı kalmalarını doğruladık.

A/B'nin `SessionFlow.cs` ve oyuncu prefabı ortak ve güncel olduğundan eski sahne adı eski runtime kod anlamına gelmez. Inventory runtime bağlanır; survival mevcut katalog üzerinden eklenir. ShelterSite bulunması bütün crafting/building akışlarının doğrulandığı anlamına gelmez. Yeni bootstrap veya streaming sistemi oluşturulmadı.

## Yerleşim sözleşmesi

Güvenli ev POI (-180,0,-170), benzinlik POI (65,0,-82); yol referansları Safehouse approach (-180,0,-155), Gas station access (50,0,-70). POI üst hiyerarşisi ve tüm Transform belgeleri adayda aynen korunur.

İlk düzenleme alanı **X [-240,-120], Z [-185,-65]**, merkez (-180,-125), 120 × 120 m. Spawn, güvenli ev merkezi, batı patikası, yol kenarı ve Pine anchor (-158.52252,0.43171057,-69.85575) içerir. Birch (-173.46353,1.160572,-57.99179) ve ikinci Pine (-149.11389,0.34835622,-64.937775) sınır dışıdır; taşınmadılar. Benzinlik yaklaşık 245 m doğuda olduğu için 120 m alana sığmaz. Yaklaşım yönü korunur; benzinliğe kadar kaplama/dressing bu dilimin kabulüne dahil değildir.

Bu sınır bir authoring sözleşmesidir; M1 sahnesini kırpmaz veya çevre nesnelerini silmez. Tüm dünya referansları henüz korunduğu için 120 m'lik rendering workload/FPS iddiası yapılamaz.

## Kritik engeller

- `852f9a79f7bf832408d4ed1eb088311a` native AssetDatabase'de çözülmüyor; A'daki iki Animator'ın Avatar referansı. Controller null. Aynı hata sonraki iki fizik sahnesinde de var. Kaynakta gizlenmedi/silinmedi; adayda devralınan hata olarak kayıtlı.
- A'nın mevcut üç anchor'ında güncel fizik authoring yok. Pine/Birch aktarımı için sonraki sahnelerin fizik gövdesi, manifest/definition/profil, ses, root ve collider eşlemesi gerekir. M1 fonksiyonel ağaç kesme kabulü **BLOCKED**.
- Core'un üç treeShadowRoots referansı A'da yoktu. Eşleşen Transform kimlik/konum/rotasyon/scale/parent doğrulanarak yalnız bu alan adayda taşındı. VisualSlice'ın ek ağaç grupları için tam shadow kapsaması M4'te denetlenecek.
- TownVolume yalnız Vignette/Bloom/ColorAdjustments/Tonemapping içerir. HDRP sky/exposure/fog temeli sahne+default profil+TownAtmosphere birlikte incelenmeden görsel kabul verilemez. M5 yapılmadı.
- Vendor lisansı ve shader referansı ayrı kapılardır. AST-S001 taşımaları uygulanmış varsayılmadı; envanter/hash taraması tekrarlanmadı.

Seçim: **A yaklaşımı — belirtilen tarihsel sahneyi ve wiring'i koruyan kontrollü türev**. Core veya Forest120'yi tam hiyerarşi olarak birleştirmek gathering, yerleşim veya otorite kaybı/çoğalması yaratır. Sonraki fizik sahneleri doğrulanmış authoring kaynağıdır, kabul edilmiş shipping içeriği değildir. M1 yalnız geri alınabilir entegrasyon tabanıdır.
