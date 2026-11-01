# WLD-G3 — iki tile makro arazi aracı

**OWNER_GENERATED / VISUAL_ACCEPTANCE_PENDING.** Owner `G3-20261024T113734691-a45a0a6c` üretimini tamamladı; başarılı G3 receipt'i güncel dosyalarla eşleşiyor. Üretilmiş sonuçlar: −7.304818…47.776363m; seam0m; splat seam0; korunan zemin maxdelta.001099825m; seam teğet farkı.766153°. Görsel/rota kabulü verilmedi. Owner Play denemesi NavMesh eksikliğiyle başarısız oldu; sıradaki adım G4_Navigation_Foundation.md menü7/8'dir. Aşağıdaki üretim yönergesi tarihsel kullanım kaydıdır; G3'ü yeniden üretmeyin. Agent üretim veya Play çalıştırmadı.

## Uygulanan üretim

Kaynak: kabul edilmiş `Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_Stage.unity`. Çıktı: **`Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_TerrainStage.unity`**. Ayrı G3 adayı G2 scene ve 602 assetini geri dönüş noktası olarak korur. G3 save slot `wld_s001_naturalworld_terrain_stage`; eski kayıtlar taşınmaz. Mevcut resident bounds ve tek session/cell/vehicle otoritesi korunur. Main, Core, prototype ve tarihsel sahnelere yazılmaz.

- `WorldTerrainSampling.cs`: G1 dünya yüksekliği fonksiyonunu iki tile için tam513×513 grid üzerinde örnekler; 526.338 örnek (ortak kenar iki gridde de vardır). Minimum/maksimumdan 8m padding ile düşey interval çıkarır. Yeni interval kaynak normalize yüksekliğini ölçekleyerek uygulanmaz; dünya metreleri yeni intervale yeniden projekte edilir. Onaylı resident Y aralığı ve 8m araç payına sığmayan interval üretimi durdurur.
- Mevcut collider/POI/route/hero maskelerine aktif Renderer footprint'leri, iki heightmap hücresi payla eklenir. G3 mevcut dekorasyonların altındaki zemini korur; mevcut nesneleri taşımadan gömülme/havada kalma riskini sınırlar. Bu konservatif ek koruma nedeniyle G3 core değişimi G1 kaba önizlemesinden daha küçük olabilir. Batı makroforms aynı onaylı planı kullanır. G5 sahipliği belirlenmiş yeniden yerleştirmeyle bu maskeleri değerlendirebilir.
- `WorldTerrainAuthoring.cs`: G2 core TerrainData'sının yeni kopyasını ve boş batı TerrainData'sını üretir; her ikisi aynı düşey intervali, 513 çözünürlüğü ve terrain katmanlarını kullanır. Core'un splat/detail dağılımı korunur. Batıda eski şehir/orman detail yoğunluğu kopyalanmaz; yeni canopy/fern scatter yoktur. Source native tree instance sayısı0 olmalıdır; GameObject ağaçları korunur.
- Batı başlangıç zemini G2 manifestinden çözülen prototype Woodland/soil/rock katmanlarını kullanır (GUID adlı kopyalardan semantik isim tahmini yapılmaz). Sarp yerde rock ağırlığı yükselir; seam'e32m geçişte core kenarındaki katman ağırlıkları kullanılır. Bu G6 final yüzey/biome kabulü değildir.
- `WorldTerrainNeighbors.cs`: yalnız G3 link nesnesine bağlı küçük runtime component. Scene yüklenince ve Play başlangıcında batı.right=core ve core.left=batı bağlantılarını tekrar kurar. `[ExecuteAlways]` yalnız SetNeighbors içindir; terrain verisi/asset üretmez, akış/cell sistemi değildir. Her iki tarafın scene, boyut ve konumunu denetler. Otomatik komşuluk kapatılır.

İlk G3'te 17 mevcut yol korunur; yeni yol mesh'i yoktur. Mevcut creek/bridgeler korunur; yeni dere yatağı henüz su yüzeyi içermez. 196 perimeter adayı bu geçişte değiştirilmez: batı geçişi mevcut çit hattı tarafından hâlâ engellenebilir. Çitlerin seçici açılması, yeni dış sınırın okunması ve güvenli geçişler G4 ile ele alınmalıdır. Bu araç bitmiş oynanabilir1000×500 dünya veya doğal orman teslimi değildir; topografya ve izolasyon adımıdır.

## Kaydedilen kontrol sonuçları

Oluşturma öncesi G2 saved-candidate doğrulaması ve exact G2 acceptance eşleşmesi gerekir. Kayıtlı G3 scene/receipt varsa üzerine yazılmaz. Native yükseklikler SetHeights sonrası tekrar okunur: tüm örnekler hesaplanan gride uymalıdır. Korunan örneklerin kaynak dünya yüksekliğinden farkı en fazla `2*range/65535 + .0001m` olabilir; native nicemleme farkı sıfır diye raporlanmaz.

Ortak kenarda513 yükseklik çifti kontrol edilir: tolerans .0001m. İlk hücrelerdeki teğet açısı farkı en fazla5 derece, alphamap kenarı farkı en fazla2/255+.0001 olmalıdır. Bu eşikler ilk teknik reddetme sınırlarıdır; görsel normal/slope kabulünün yerine geçmez. Terrain holes iki gridde de bulunmamalıdır. Hata olursa korunan zemini otomatik yumuşatmak yerine işlem durur.

G2'nin18.994 nesnesi local ID/parent/name/active/local transform/component tipleri ve stable ID açısından karşılaştırılır. İzinli değişiklik yalnız core terrain transform Y değerinin yeni origin'e taşınmasıdır; bu da aynı dünya yüksekliğine projeksiyonla birlikte yapılır. İki yeni GameObject vardır: batı terrain ve link root. İki collider kendi üretilmiş TerrainData'sını kullanmalıdır. Layer/material bağlantıları ve iki yönlü neighbors scene yeniden açıldıktan sonra doğrulanır.

Topoloji değiştiğinden G3 kopyasındaki NavMeshSurface verileri kaldırılır; kaynak NavMeshData assetleri silinmez/değişmez. G3'te düşman rotası veya araç oynanış testi bu aşamada kabul edilmez. Owner G4 bake/route kabulünü tamamlamalıdır. Mevcut Low/Medium/High/Ultra ve bağımsız ağaç gölgesi otoritesi değişmez; yeni terrain mevcut terrain renderer ayarlarını devralır, ek grafik yöneticisi kurulmaz.

Sonuç: `Docs/Implementation/World/WLD-S001/G3_Latest.json`; her işlem journal'ı `Evidence/G3-<utc>-<id>.json`. G2 receipt/scene ve602 asset/source hashleri tekrar kontrol edilir. G3 assetleri `Generations/G3-.../CoreTerrain.asset` ve `WestTerrain.asset` altındadır. Hata durumunda yalnız yeni G3 scene/asset/boş klasörler toplu rollback ile kaldırılır, G2 yeniden açılır. Crash durumunda journal kalan yolları teşhis için tutar; otomatik yeniden açılış silmesi yoktur.

## Owner çalıştırması

1. Kaydedilmiş **WLD_S001_NaturalWorld_Stage** açık olsun (kabul edilen G2). Main veya eski prototype üzerinde çalıştırmayın. Kaydedilmemiş değişiklik varsa araç durur; otomatik save/discard yoktur.
2. **Last Signal → World Reconstruction → WLD-S001 → 5 Generate G3 terrain candidate**. Tam örnekleme sırasında iptal edilebilir ilerleme çubuğu gösterilir; bu hesap bitene kadar asset yazılmaz.
3. Başarıda **WLD_S001_NaturalWorld_TerrainStage** açık kalır. Console `WLD G3 TERRAIN_CREATED` ve gerçek minimum/maksimum/seam/protected delta değerlerini verir. Scene View'da batı (-X) tarafında yeni tepe/sırt/vadi beklenir; yoğun batı ormanı henüz yoktur.
4. Başarı mesajı geldikten sonra **6 Validate saved G3 terrain**. Beklenen `WLD G3 SAVED_TERRAIN_VERIFIED`. Araziyi/bake'i bu doğrulamadan önce değiştirmeyin; hash guard değişikliği reddeder.
5. `G3_Latest.json`, iki başarı mesajı, ortak terrain kenarından yakın görüntü ve batı arazisinin genel Scene View görüntüsünü gönderin. Hata olursa ilk exception ve ilgili yeni journal yeterli; doğrulama menüsüne geçmeyin.

Statik terminal: `python3 Tools/WorldReconstruction/verify_foundation.py`. G2 kabulü ACCEPTED; G3 üretimden önce NOT_RUN, başarılı receipt sonrası RECEIPT_CHECKED_STATIC_ONLY. G3 görsel/oynanış kabulü bu statik durumdan türetilmez.

## Bu teslimdeki doğrulama sınırı

G2'nin user-returned başarıları yerel receipt/source hashleriyle eşleşti. Salt okunur native incelemede G2 terrain: native trees0, holes0, detail prototype1, alphamap512, dört prototype layer. Yeni C# import/compile durumu incelenir; authoring, terrain generation, EditMode/PlayMode, bake veya benchmark agent tarafından çalıştırılmaz. G3 gerçek native seam/slope/nicemleme sonuçları owner çalıştırmasında oluşacak; G1 −7.31…47.77m önizlemesi G3 ölçümü olarak kullanılmaz.

Son salt okunur compile gözleminde WorldTerrainAuthoring, WorldTerrainReceipt ve WorldTerrainNeighbors tipleri yüklü, Generate metodu mevcut ve compiling=false idi. Aktif G2 scene dirty=false. Statik verifier VERIFIED_STATIC_ONLY, G2 ACCEPTED, G3 NOT_RUN döndü. Araç terrain GameObject'inde çocuk nesne varsa origin değişimini reddeder; aksi halde dolaylı çocuk taşıma riski oluşurdu.
