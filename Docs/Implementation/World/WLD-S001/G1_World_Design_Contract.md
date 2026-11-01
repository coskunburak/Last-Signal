# WLD-G1 — uygulanmış planlama temeli

**IMPLEMENTED / G1 ARCHITECTURE ACCEPTED.** Owner 24 Ekim'de saved-source preview JSON'unu ve üç pencere görüntüsünü gönderdi, ardından “uygulamanı onaylıyorum” diyerek batı genişleme ve aday sahne resident sözleşmesini onayladı. G2 aracı bu kapsamda hazırlandı; çalıştırılması ve Unity kabulü owner'a ait. Yeni TerrainData/scene/orman henüz üretilmedi. Bu bir oynanabilir 1 km dünya teslimi değildir. Aşağıdaki onay bekleme notları ilk G1 incelemesinin tarihsel kaydıdır; son karar bu paragraftadır.

## İlk genişleme kararı

Batı önerisi: core X[-250,250], Z[-250,250]; ek tile X[-750,-250], Z[-250,250]. Toplam 1000×500m, ortak edge x=-250. Kuzey +Z, doğu +X. Mevcut koordinatlar sabit. Batı seçimi mevcut ForestLayout yoğunluk maskesi, batı trail loop'u ve doğudaki yoğun POI'ler nedeniyle; kuzey farm/ranger rotalarını ve mevcut dere uçlarını yeniden kurmaktan daha az başlangıç çatışması yaratır. Görseldeki sırt–açıklık–vadi–tepe ilişkisi tasarım referansıdır, ölçülmüş heightmap değildir.

Batı seçimi G1 önerisi; owner kabulü yok. FirstTownCore üretim referansı korunarak görselce daha güncel FirstTownMain_HDRP staging tabanı önerilir.

## Veri ve yükseklik modeli

Plan.json seed=261024, generatorVersion=WLD-G1-preview-v1. Kaynak scene/terrain GUID ve hashler sabitlenir; değişmiş kaynak için sessiz receipt yenilemesi yok. 7 isimli form: batı watershed sırtı, güney orman omzu, kuzey seyir tepesi, foothill terrace, iki core geçişi, tributary valley. Öneri amplitüdleri 5–44m; bunlar üretilmiş ölçüm değil plan parametreleridir. .6m bütçeli düşük frekanslı ikincil değişim, collider mikro-gürültüsünün yerine geçmez.

Eski normalize yükseklik önce `sourceOrigin.y + normalized * sourceSize.y` ile metreye çevrilir. Koruma etkisi sıfırsa aynı dünya yüksekliği döner. Önerilen yeni düşey interval, preview örneklerinin minimum/maksimumuna 8m tasarım payı eklenerek hesaplanır; production için tam513²/tile araması gerekir. ProjectWorldHeight aralık dışındaki yüksekliği clamp ederek gizlemez; hata verir. 16-bit quantization adımı raporlanır. Yeni origin/range terrain ve collider ile birlikte G3'te uygulanmalıdır.

Batı tile edge, core edge dünya fonksiyonunu ve iç komşu örnekten türetilen eğimi kullanır; 32m geçişte makroforma bağlanır. Bu **analitik seam sözleşmesidir**, gerçek TerrainData seam testi değildir. G3'te iki 513² grid oluşturulup world-height örnekleri, normal/slope ve splat seam ölçülmeli; `SetNeighbors` batı tile sağ=core, core sol=batı olarak iki tarafta ayarlanmalı. Otomatik height düzeltmesi varsayılmaz.

Drenaj kontrol noktaları yukarıdan aşağıya monoton hedef taban kotu içerir. Mevcut dere/bridge koruma bandı önceliklidir. Yol/foundation maskesi ile kesişen drenajın sürekliliği, culvert/bridge gereksinimi ve gerçek akış G3/G4 değerlendirmesidir. Mevcut su ribbon'u değiştirilmedi; yeni su/akış mekaniği yok.

## Yollar, ekoloji ve sınırlar

14 mevcut yol +3 orman rotası G1'de korunur. İki yeni route polyline'ı yalnız koridor planıdır: batı servis bağlantısı ve ridge walking loop. Road mesh üretimi yok; spline, turning radius, roadbed/shoulder ve vehicle grade sonraki G4 işi. `maximumGrade=0` doğrulanmış limit olmadığı anlamına gelir; “düz yol” limiti değildir. FirstPersonMotor slopeLimit yalnız yaya/exit çözümüne kanıt, pickup sürüş eğimi kabulü değildir.

13 foundation zarfına sahneden türeyen collider/identity/artist maskeleri eklenir. Yamaç, yükseklik, aspect, moisture/canopy kümeleri G5'in planlanan ekoloji katmanlarıdır; henüz scatter kodu yazılmadı. İlk ek canopy üst sınırı120, fern240; bu bir FPS garantisi değil kontrollü deneme bütçesi. Şimdiki346 ağacı çoğaltma yok. Mevcut Low/Medium/High/Ultra ve Global High + Tree Off otoritesi aynen korunur.

Sınır reçetesi G2/G3'te exact perimeter sahipliği doğrulandıktan sonra batıdaki eski çit hattını açmalı. Yeni dış edge doğal sırt/orman/ravine ile okunmalı; invisible wall veya ölüm uçurumu kullanılmaz. Safehouse/farm/industry çitleri ayrı korunur. Çitler bu gate'te hâlâ yerlerinde.

## Kullanım ve kabul

1. Unity import/compile tamamlanınca mevcut sahnedeki kaydedilmemiş çalışmayı inceleyin. Kullanıcı değişikliğini atmayın. Yalnız kayıtlı Main_HDRP açıkken preview çalışır. Kaydetmek kaynak hashini değiştirirse hata beklenir; diff'i gönderin, receipt'i körlemesine güncellemeyin.
2. Menü: **Last Signal → World Reconstruction → WLD-S001 → Open G0-G1 planner**.
3. **1 • Audit Source — salt okunur**. Sahne dirty ise envanter bellekteki durum olarak işaretlenir. Kaynak GUID/hash, bounds, tek session, missing scripts ve açık blockers raporlanır. Otomatik save/open yok.
4. **2 • Preview World Plan — yalnız bellekte**. Kaynak saved + receipt eşleşmeli. Beklenen: 257×129 plan görseli, batı solda, yeni form bölgeleri; protected delta=0, türetilmiş interval ve quantization raporu. Bu örnekleme final terrain veya native seam kabulü değildir.
5. **Rapor JSON'unu panoya kopyala**. Pencerenin tamamını ve Console sonucunu gönderin. Plan görselinde sırtlar, koridorlar, drenaj ve POI korumasını değerlendirin. Domain import sonrası bir hata varsa tam stack'i iletin.

Salt okunur terminal: `python3 Tools/WorldReconstruction/verify_foundation.py`. Beklenen VERIFIED_STATIC_ONLY; 23 seçili kaynak değişmemiş, stage/Generations yok, plan footprint1000×500,17 korunanroute. Unity açmaz.

G0/G1 acceptance bekliyor. G2/G3 öncesi resident bounds/save restore sözleşmesi owner ile doğrulanmalı; yalnız ondan sonra sahne/asset izolasyon aracı hazırlanmalı. Bu ilk araçta Generate/Promote düğmesi veya otomatik callback yok.

## 24 Ekim — dönen kanıt ve mimari karar

`Evidence/G1_OwnerPreview_20261024.json` owner'ın gönderdiği JSON'un değiştirilmemiş kopyasıdır. Kaynak scene/terrain/plan SHA-256 eşleşti. `sourceHasUnsavedChanges=false`; önceki dirty-source koşulu bu çalıştırmada yok. 295 protected object kaydının kimlik, hierarchy ve position alanları; 196 perimeter ID ve 5290 koruma bounds sayısı önceki G0 kaydıyla birebir aynı. Bunlar 295 ayrı ağaç veya 5290 ayrı oynanış objesi anlamına gelmez; envanter kategorileri ve koruma hacimleridir.

Önizleme 257×129 örnekte −7.31…47.77m, önerilen düşey interval[-16,56]m ve 8174 korunan örnekte delta0 raporluyor. Beklenen G1 araç davranışı için owner kanıtı alındı. Kaba grid aralarındaki yükseklikler, gerçek TerrainData quantization/seam, navmesh, sürüş ve görsel tasarım kabulü hâlâ doğrulanmadı. Console'un tamamı gönderilmedi; tüm Console temizliği iddiası yok. Aynı preview çalıştırmasını tekrar istemiyoruz.

### İncelenen sahiplik kapsamı

- `VehicleWorld.ContainsResidentPosition` serialized Bounds.Contains çağırır; `SaveSession.ValidateTopology` dışarıdaki pickup kaydını reddeder. Kaynak bounds X/Z[-250,250], Y[-40,60]. Batı tile bu sınırın dışındadır.
- Main sahnesindeki tek `WorldCellManager` component'inin serialized `definitions` listesi boştur. `OwnerForPosition`, tanımlı cell eşleşmezse `resident` döndürür. Şu an batı için ayrı stream edilmiş cell bulunmuyor. Bu, resident yaklaşımını mümkün kılan kaynak bulgusudur; runtime restore kabulü değildir.
- `VehicleActor.ResidentReady` mevcut cell/session hazır olmasını bekler; boyut büyütmek collider, NavMesh veya rota üretmez. Mevcut spawnlar ve persistent ID'ler sabit kalmalıdır.

### Onaya sunulan somut aday sözleşmesi — uygulanmadı

Yalnız `Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_Stage.unity` için:

1. Mevcut tek VehicleWorld/WorldCellManager/SessionFlow korunur. İkinci cell otoritesi veya streaming sistemi eklenmez. Runtime `ContainsResidentPosition` ve save validation gevşetilmez.
2. Stage VehicleWorld resident bounds min(-750,-40,-250), max(250,80,250); Unity center(-250,20,0), size(1000,120,500). Y aralığı eski hacmi kapsar ve kaba üst interval56m üzerinde pay bırakır. G3 tam örnekleme/araç yüksekliği payı bu aralığa sığmazsa üretim durmalı; sessiz yeniden genişletme yapılmamalı.
3. Stage SaveSession `saveSlot=wld_s001_naturalworld_stage`. Main'in `scn_s001_main_hdrp_review` yuvası ve `current` korunur. Eski kayıtlar kopyalanmaz veya otomatik migrate edilmez. Ayrı yuva varsayılan dosyaları ayırır; manuel farklı dosya yüklemeye karşı ayrı scene kimlik doğrulaması değildir.
4. G2 authoring aracı owner tarafından çalıştırılmalı: source hash/clean-state preflight, yeni scene ve mutable asset izolasyonu, source→stage reference manifest, protected local IDs/positions doğrulaması, yalnız bu işlemde üretilmiş dosyaların rollback listesi. G2 arazisi başlangıçta aynı kalır; bounds sözleşmesi genişlemeyi hazırlar, oynanabilirlik sağlamaz.
5. G3/G4'te iki tile terrain/collider ve iki taraflı neighbors, tam seam/grounding kontrolü, yeni dış sınır/rota, izole NavMesh bake ve route kabulü gerekir. G8'de hem core hem batıda araçla/araç dışında save→load, batıdan core'a geri dönüş ve gerçek dış sınırda kayıt reddi owner tarafından doğrulanmalı. Üç null harvest physicsBody ayrı baseline işi olarak kalır.

Onay gereği aracın kendi uyarısından değil, kullanıcı WLD brief §6 cümlesinden gelir: “If an adjacent playable tile requires changes to world persistence, navigation, spawns or loading, stop at the architecture gate and document the necessary work.” Bu bölüm o mimari incelemeyi tamamlar. Owner G1 batı yönü ve bu sınırlı sözleşmeyi kabul edene kadar G2/G3 BLOCKED; main scene veya runtime sahiplik kodu değiştirilmedi.
