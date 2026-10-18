# LAST SIGNAL — CONSTRUCTION & CRAFTING PRODUCTION MASTER PLAN

**Sürüm:** 1.0 — PROPOSED · **Tarih:** 2026-10-17 · **Kod adı:** `LS-BLD` · **Kapsam:** Single-player, Unity 6000.5.0f1/URP, mevcut FirstTownCore ve sığınak mimarisi.

> **Durum ayrımı:** Bu belgedeki phase/sprint/gate durumları tasarım hedefidir; yeni construction kodunun yazıldığı, çalıştırıldığı veya PASS olduğu iddia edilmez. İncelenen temel depo `coskunburak/Last-Signal`, branch `s012-integrated-graybox-slice`, 2026-10-17 push baseline `fe0f4c70` (main üzerinde merge `a5749e4`). Yerel dirty working tree ve sahne state'i implementation öncesi ayrıca incelenecek.

## A. Ürün vaadi ve neden

Oyuncu zombi öldürmek için değil, zombi salgını sonrası bir hayata tutunmak ve bunu adım adım yeniden inşa etmek için dışarı çıkar: **keşfet → kes/topla → işle → craft et → kur → kullan/onar → genişlet**. Zombiler, hava durumu, gürültü ve kaynak kıtlığı bu sistemlere risk/karar ekler. GDD'nin mevcut `LS-DOC-14` sürümü authored socket ve *no free building* kapsamına sahiptir; buradaki öneri **resmî ürün değişikliği (ADR) gerektirir**. İncelenen kod S010 escrow, ShelterSite/ShelterSocket, PlayerInventory, WorldCellManager, SaveSession ve WorldClock altyapısını sağlıyor. Genel yapı yerleştirme ve harvest node runtime'ı doğrulanmış değildir.

**Scope guardrails:** Sınırsız voxel kazma/inşa, tüm nesneleri yıkma, ağır statik fizik simülasyonu, oyuncu yapısı başına MonoBehaviour Update, multiplayer/networking ve devasa procedural world builder başlangıç kapsamı dışındadır. Projenin mevcut FirstTownCore_VisualSlice çalışması korunur; bu program onun asset/prefab çalışma hattını yeniden yazmaz.

## B. Kaynak gerçekliği ve kabul disiplini

- Kaynak önceliği: güncel kullanıcı kararı → GDD kapsam sözleşmesi → çalışan kod ve test kanıtı → bu öneri.
- **Kullanıcı manuel test sahibi:** Ajan Unity EditMode/PlayMode, build, benchmark ve performans capture'ı çalıştırmaz. Ajan dar test ve authoring komutlarını hazırlar; kullanıcı çalıştırıp log verir; ajan hatayı düzeltir ve *yalnız* ilgili testi yeniden ister. Test isimlerini gerçek kodda doğrulamadan komut uydurulmaz.
- Context: `Docs` topluca okunmaz; yalnız hedef phase için gereken belgeler/sınıflar yüklenir. Vendor prefab/material yerinde değiştirilmez. `git reset/clean/commit/push` izinsiz yapılmaz.
- **Status:** PROPOSED → READY → IMPLEMENTED → VERIFYING → PASS / PARTIAL / BLOCKED. Kod hazır ama test yok ise `IMPLEMENTED; TEST_NOT_RUN`. Kullanıcı görsel kabul verene dek `VISUAL_NOT_APPROVED`.
- **Sprint akışı:** audit → contract → implementation → test definitions → source/diff review → owner commands → owner evidence → focused bugfix/retest → gate receipt.

## C. Mimari hedef — domain ve kaynak sahipliği

| Alan | Ürün sınıfı/kontrat | Kaynak ve sorumluluk |
|---|---|---|
| Resource | `ResourceNodeDefinition`, `ResourceNodeState`, `HarvestReceipt` | Harvestable tree/stone node; dekoratif flora ayrı; world ID ve tükenme kalıcı |
| Inventory | `ItemDefinition`, `InventoryContainer`, `OwnershipTransaction` | Mevcut tek item ownership yetkisi korunur |
| Craft | Yeni `RecipeDefinition` çok-girdi modeli; S010 adaptörü | Revision/escrow/job/receipt; eski ShelterRecipe bozulmaz |
| Notebook | Craft Defteri UI + build-selection request | Sadece komut gönderir, envanter veya world nesnesi mutate etmez |
| Build | `BuildDefinition`, `PlacementQuery`, `PlacementResult` | Prefab, footprint, anchor, cost, restrictions, rejection code |
| Placement | `PlacementValidator`, `GhostPresenter`, `ConstructionService` | Preview input, validasyon, authority recheck, tek commit |
| Structures | `StructureInstance` + registry/ID/connection graph | Tanım + transform + condition + owner/cell ve tombstone |
| Shelter | Mevcut `ShelterSite/ShelterSocket` + `BuildZone` adaptörü | 3 sabit modülü kırmadan dış dünyada kontrollü yerleştirme |
| Persistence | `SaveSession`, `WorldCellManager`, world delta + migration | Stable ID; unloaded cell; save atomicity; eski save compatibility |
| Threat | `WorldClock`, `NoiseEvent`, `WorldPressure` | Kesim/inşa ve jeneratör sesi kontrollü cadence ile işlensin |

### Placement contract — kritik sıra

1. Notebook'tan `BuildDefinition` seçilir; hiçbir resource tüketilmez.
2. Ghost prefab UI-only materyalle gösterilir; collider/AI/gameplay scriptleri devre dışı.
3. Renderer yeşil/kırmızı, ikon/desen ve **ret nedeni** gösterir. Preview **authoritative değildir**.
4. Kullanıcı confirm'e bastığında inventory revision, build-zone, collision, slope, nav/door/vehicle clearance ve structure occupancy **yeniden** doğrulanır.
5. Unique request nonce altında **tek atomik commit**: malzemeyi rezerve/eksilt, yapı oluştur, stable entity ID ve authoritative world owner kaydet. Hata olursa eksilen kaynaklar geri dönsün; ortada nesne kalmasın.
6. Save/load ve cell unload/reload bütün yapı ve inventory state'ini tutarlı yükler; silinen yapı tombstone olarak saklanır.

**İlk sprint modelinde anlık placement commit seçilir.** Çok aşamalı inşa, blueprint üzerinde ayrı malzeme biriktirme ve co-op yarış koşulları ileride ayrı ADR olmadan otomatik eklenmez.

### Yapı grupları

- **Basit:** ahşap çit, çit kapısı, sandık, küçük platform.
- **Modüler barınak:** temel, zemin, tam/yarım duvar, kapı, pencere, çatı.
- **Üretim:** iş tezgâhı, kesim tezgâhı, su toplayıcı, kamp ateşi, aydınlatma.
- **Gelişmiş (koşullu):** elektrik dağıtımı, gözetleme kulesi, savunma, tarım alanı, büyük barınak — ayrı kalite/performans kararı.

### İlk oyuncu akışı (G4)

Oyuncu FirstTownCore sığınağından çıkar → belirlenmiş *harvestable* ağaçla balta etkileşimi → log/branch dünya item'ları → envanter ağırlık/kısıtları → log'u tezgâhta plank'e dönüştürür → Craft Defteri'nden Fence/Platform seçer → önizlemede yapı renk+mesajla uygunluk gösterir → geçersiz konumda kaynak tüketmeden vazgeçer → geçerli konumda inşa eder → kayıt alır → yükler → aynı yapı ve envanter miktarları geri gelir.

## D. Phase/gate özeti

| Phase | Sprint | Hedef ve exit gate |
|---|---|---|
| P00 | BLD-S001–BLD-S002 | Ürün sözleşmesi ve güvenli mimari; G0: onaylı kapsam farkı, teknik tasarım, şema ve bağımlılık matrisi |
| P01 | BLD-S003–BLD-S004 | Kaynak toplama ve ağaç kesme; G1: ağaç kesme/kaynak toplama + mülkiyet, yükleme ve gürültü doğrulaması |
| P02 | BLD-S005–BLD-S006 | Crafting çekirdeği ve Craft Defteri; G2: birden fazla malzemeli recipe, craft/iptal, notebook, save testleri |
| P03 | BLD-S007–BLD-S009 | Yapı yerleştirme çekirdeği; G3: geçerli/bozuk placement, kaynak korunumu ve save/load roundtrip |
| P04 | BLD-S010–BLD-S010 | Entegre ilk playable loop; G4: ağaç → tahta → defter → çit → save/load uçtan uca kabul |
| P05 | BLD-S011–BLD-S015 | Modüler ev ve yapı mekaniği; G5: küçük modüler barınak inşası, dolaşım, kapı, barınak durumu |
| P06 | BLD-S016–BLD-S019 | Sığınakla birleşen üretim ve bakım; G6: yaşayan sığınak + kapsamlı craft ve maintenance loop |
| P07 | BLD-S020–BLD-S022 | Dünya ekonomisi ve güvenilir kayıt; G7: regresyon, migration, exploit ve uzun oturum testleri |
| P08 | BLD-S023–BLD-S026 | Sanat, deneyim ve optimizasyon; G8: görsel kabul + performans + erişilebilirlik doğrulaması |
| P09 | BLD-S027–BLD-S028 | Alpha kabul ve scope kapatma; G9: alpha playtest, release scope ve teknik devir kararı |

**Kapasite planı:** Her sprint *varsayılan 8 iş kartı*, başlangıç tahmini **40–80 odak saat**; çok araç/asset işlerinde daha fazla olabilir. 28 sprint ≈ 1.120–2.240 odak saat yalnızca koşullu rough-order estimate'tir; gerçek üretim kapasitesi ölçülene kadar takvim vaadi değildir. Her 2–3 sprintte gerçekleşen efor, risk ve kesilecek kapsam yeniden hesaplanır. *İlk playable loop* S010'da, *modüler barınak* S015'te, *utility-progress* S019'da teslim hedefidir.

## E. Sprint bazlı ayrıntılı backlog — 224 kart

**Her kartın formatı:** `Görev — Yapılacak işlem — Kabul ölçütü`. Test ve görsel kabul kullanıcı tarafından çalıştırıldıktan sonra statüye yansır. Kod tarafında agent implement edip retest komutunu teslim eder.

## P00 — Ürün sözleşmesi ve güvenli mimari

**Mission:** Serbest yapı inşası kapsam kararını resmileştir; güvenli sistem sınırlarını ve migration stratejisini kilitle.

**Exit gate:** G0: onaylı kapsam farkı, teknik tasarım, şema ve bağımlılık matrisi.

### BLD-S001 — GDD kapsam ve oyuncu vaadi

**Giriş bağımlılığı:** Program girişi: GDD/branch audit. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D001 — İlk sürüm sınır değişikliği.** LS-DOC-14 soketle sınırlı inşa kararına yeni controlled-building önerisini ADR olarak yaz. **Kabul:** Eski tasarım silinmez; değişiklik açık ve onaya hazırdır.
- **BLD-D002 — Kullanıcı hikâyeleri.** Ağaç kesme, odun taşıma, craft defteri, yerleştirme ve üs büyütme için akışlar yaz. **Kabul:** Her hikâyede tetik, geri bildirim, başarısızlık ve ödül tanımlanır.
- **BLD-D003 — İnşa alanı politikası.** Claimed shelter çevresi, fiziksel yollar, POI, nav ve koruma alanlarını tasarla. **Kabul:** Başlangıç build zone coğrafyası ve açık istisnalar listelenir.
- **BLD-D004 — Kapsam sınıflaması.** Must/Should/Could/Out listelerini yapılar ve süreçler için sabitle. **Kabul:** 1.0 kapsamı sınırsız voxel, tam yıkım ve co-op ile karışmaz.
- **BLD-D005 — Survival entegrasyonu.** Yapıların barınma, depolama, risk, gürültü ve seyahat faydasını yaz. **Kabul:** Her MVP öğesinin oyuncuya somut faydası belgelenir.
- **BLD-D006 — İlk demo senaryosu.** Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla. **Kabul:** Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir.
- **BLD-D007 — Kapsam bütçesi.** Solo ekip üretim/asset/QA tahmini ve kesme sırasını oluştur. **Kabul:** Efor aralıkları garanti değil; erteleme sınırları net.
- **BLD-D008 — Karar kapısı.** GDD farkı, tasarım kabul ve ADR kimliği ile onay paketi hazırla. **Kabul:** G0 altında kabul edilmeden release kapsamı güncellendi sayılmaz.

**Sprint exit:** D001–D008 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S002 — Mimari sözleşme ve risk temeli

**Giriş bağımlılığı:** S001. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D009 — Mevcut kod sınırı.** S010 ShelterSite/ShelterProduction, envanter, cell ve SaveSession giriş/çıkışlarını haritala. **Kabul:** Daha önce çalışan kodun yeniden yazılması engellenir.
- **BLD-D010 — Domain sahipliği.** Gathering, Crafting, Construction, Shelter ve Persistence sorumluluklarını ayır. **Kabul:** Her verinin tek authoritative owner'ı tanımlıdır.
- **BLD-D011 — Veri tanımları.** ResourceNodeDefinition, RecipeDefinition, BuildDefinition ve StructureState taslağı çiz. **Kabul:** Stable ID, revision, prefab ve bağımlılıklar doğrulanabilir.
- **BLD-D012 — İşlem sıralaması.** Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et. **Kabul:** Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir.
- **BLD-D013 — Dünya adresleme.** World/cell/build-zone ID ve yapı kimliği üretme politikasını tanımla. **Kabul:** Restore duplicate yapı ve iki hücre sahipliği üretmez.
- **BLD-D014 — İçerik import ilkeleri.** Vendor assetlere dokunmama, project-owned variant, lisans/LOD gereksinimi kaydet. **Kabul:** Kaynak ve kullanım raporu hazırlanır.
- **BLD-D015 — Test matrisi.** EditMode/PlayMode/save fault injection/visual QA kabul stratejisini kur. **Kabul:** Her risk bir hedefli teste eşlenmiştir.
- **BLD-D016 — Koruma noktası.** Branch, dirty tree, save fixtures ve migration checkpoint planını kaydet. **Kabul:** Kod üretimine girişte kaynak bütünlüğü korunabilir.

**Sprint exit:** D009–D016 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P01 — Kaynak toplama ve ağaç kesme

**Mission:** Oynanabilir, kalıcı ve dengelenebilir kaynak toplama mekaniklerini oluştur.

**Exit gate:** G1: ağaç kesme/kaynak toplama + mülkiyet, yükleme ve gürültü doğrulaması.

### BLD-S003 — Harvestable resource nodes

**Giriş bağımlılığı:** S002. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D017 — Kaynak taksonomisi.** Log, branch, plank, stone, scrap, rope/fastener örneklerini kategori ve kütleyle eşle. **Kabul:** Katalog ID çakışması ve bedava kaynak dönüşü yok.
- **BLD-D018 — Harvest node modeli.** ResourceNodeDefinition/State ile sağlık, verim, cooldown, tool tanımla. **Kabul:** Bir kez kesilen ağacın durumu aynı yüklemede korunur.
- **BLD-D019 — Ağaç seçim politikası.** Her dekoratif ağacı kesilebilir kılma; seçilen ağacı özel node olarak etiketle. **Kabul:** Orman görselleri istemeden tüketilmez.
- **BLD-D020 — Etkileşim mesafesi.** Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle. **Kabul:** Duvar arkasından veya uzaktan kesim reddedilir.
- **BLD-D021 — Araç yetkinliği.** Balta türü, durability ve kesme çarpanlarını mevcut item yapısıyla bağla. **Kabul:** Tool yoksa işlem açıklanabilir ret alır.
- **BLD-D022 — Hit/harvest doğrulaması.** Aynı vuruşun iki kez sayılmasını, negatif sağlık ve eş zamanlı etkileşimi engelle. **Kabul:** Tek vuruş yalnız tek hasar olayı üretir.
- **BLD-D023 — Spawn ve envanter sahipliği.** Kesim ödülünü mevcut WorldItem/InventoryContainer ownership işlemleriyle bağla. **Kabul:** Full inventory durumunda eşya kaybolmaz.
- **BLD-D024 — Kalıcı node durumu.** Kesilmiş node/stump/tombstone snapshot taslağını cell'e bağla. **Kabul:** Save/load sonrası ağaç kendiliğinden yeniden büyümez.

**Sprint exit:** D017–D024 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S004 — Ağaç kesme oynanış ve geri bildirim

**Giriş bağımlılığı:** S003. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D025 — Balta etkileşim animasyonu.** Mevcut FPS/TPS silah grip sistemini bozmadan chop swing bağla. **Kabul:** Görsel vuruşla hasar zamanı uyumludur.
- **BLD-D026 — İsabet ve mesafe.** Ağaç collider'ı, açı ve occlusion ile isabeti kontrol et. **Kabul:** Yanlış hedef veya boşluk hasar almaz.
- **BLD-D027 — Ses ve world pressure.** Kesme/balta kaynaklı bounded NoiseEvent, ses aralığı ve kategori üret. **Kabul:** 60/120 FPS farklı event sayısı yaratmaz.
- **BLD-D028 — Kesim aşamaları.** Gövde yara durumu, geri bildirim ve kontrollü devrilme kararı. **Kabul:** Devrilme fizik sistemi sınırsız rigidbody yaratmaz.
- **BLD-D029 — Düşen kütükler.** Statik hasat/düşüş animasyonu ve kaynak drop alanını belirle. **Kabul:** Eşya arazi altında veya collider içinde doğmaz.
- **BLD-D030 — Kaynak toplama UX.** Odun ağırlığı, miktar, envanter doluluğu mesajlarını göster. **Kabul:** Oyuncu niçin alamadığını açıkça anlar.
- **BLD-D031 — Yerleşim kaynak uyumu.** Harvest node colliders ve sahne terrain/foliage exclusion kayıtlarını koru. **Kabul:** Ağaç kesimi yol/nav/geçişi bozmaz.
- **BLD-D032 — Manuel test teslimi.** Kesim-kaydet-yükle, alet kırılması ve kesim sonrası loot kanıt komutlarını sun. **Kabul:** Kullanıcı test çalıştırmadıkça PASS verilmez.

**Sprint exit:** D025–D032 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P02 — Crafting çekirdeği ve Craft Defteri

**Mission:** Çok girdili tarifleri, güvenli rezervasyonu, tek UI akışını ve erişilebilir kontrolü geliştir.

**Exit gate:** G2: birden fazla malzemeli recipe, craft/iptal, notebook, save testleri.

### BLD-S005 — Recipe engine ve malzeme işlemeyi genişlet

**Giriş bağımlılığı:** S003. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D033 — Tarif şeması.** Tek-input ShelterRecipe yerine ek, revisionlu çok girdili RecipeDefinition tasarla. **Kabul:** Eski recipe/saveler çalışmaya devam eder.
- **BLD-D034 — Malzeme listesi.** Aynı tarifte çok input, araç, station ve unlock şartını tanımla. **Kabul:** Çakışan/negatif input reddedilir.
- **BLD-D035 — Kütükten tahta.** Log→plank için başlangıç dönüşüm maliyeti ve verimini kataloğa ekle. **Kabul:** Net materyal miktarı dengeli ve izlenebilir.
- **BLD-D036 — Recipe validator.** ID, cycle, overflows, item max stack, station ve output koşullarını test et. **Kabul:** Sonsuz bedava kaynak çevrimi oluşturulamaz.
- **BLD-D037 — Escrow bütünleştirme.** S010 escrow/cancel/output full politikasını genelleştir. **Kabul:** İptal/ölüm/kayıt doluluğu duplication yaratmaz.
- **BLD-D038 — Craft işlem günlüğü.** Request ID, revision, accepted/rejected reason ve receipt üret. **Kabul:** İki kere submit aynı craftı iki kez üretmez.
- **BLD-D039 — Zaman politikası.** Elde üretim simülasyon saniyesi, istasyon world time ayrımı yap. **Kabul:** Pause/skip/sleep sırasında süre sözleşmesi korunur.
- **BLD-D040 — Regression köprüsü.** Eski S010 recipe testlerini ve yeni multi-input testlerini hazırlamayı planla. **Kabul:** Önceki depolama ve modül davranışları bozulmaz.

**Sprint exit:** D033–D040 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S006 — Craft Defteri UI/UX

**Giriş bağımlılığı:** S005. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D041 — UI bilgi mimarisi.** Yapılar, aletler, kaynak işleme, istasyonlar, savunma kategorilerini tasarla. **Kabul:** Kullanıcı 2–3 adımda yapı tarifine ulaşabilir.
- **BLD-D042 — Recipe kartları.** İkon, isim, özet, miktar, eksik malzeme ve unlock nedenini göster. **Kabul:** Stok ile gereken miktar yan yana ve günceldir.
- **BLD-D043 — Notebook aç/kapat.** Mouse, klavye ve gelecekte gamepad için ortak input policy. **Kabul:** Pause/reload ve gameplay input çakışmaz.
- **BLD-D044 — Blueprint seçimi.** BuildDefinition seçimi sonrası notebook→ghost controller komutu üret. **Kabul:** UI doğrudan world objesi spawn etmez.
- **BLD-D045 — Erişilebilir geri bildirim.** Renk ve ikon/metin ile craft uygunluğu bildir. **Kabul:** Renk körlüğünde bilgi kaybı olmaz.
- **BLD-D046 — Search/filter/scroll.** Liste filtreleme, kategori ve detay sayfasını hafif implement et. **Kabul:** Yüzlerce öğe varken her frame tam liste kurulmaz.
- **BLD-D047 — Ses ve görsel his.** Sayfa geçiş, seçim, hata ve onay feedback'ini düşük maliyetle ekle. **Kabul:** Bildirimler oyun seslerini bastırmaz.
- **BLD-D048 — Örnek senaryo kabulü.** Odun mevcut/yetersiz, tool eksik ve istasyon yok akışlarını prova et. **Kabul:** Oyuncu tarifin niçin kilitli olduğunu söyleyebilir.

**Sprint exit:** D041–D048 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P03 — Yapı yerleştirme çekirdeği

**Mission:** Genel amaçlı ghost, çok katmanlı validasyon, atomik inşa ve kalıcılık.

**Exit gate:** G3: geçerli/bozuk placement, kaynak korunumu ve save/load roundtrip.

### BLD-S007 — Placement validation engine

**Giriş bağımlılığı:** S002,S006. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D049 — BuildDefinition kontratı.** Prefab, bounds, anchor, snap class, slope, cost ve placed ID alanlarını belirle. **Kabul:** Kaydedilemeyen veya tanımsız blueprint reddedilir.
- **BLD-D050 — İnşa bölgeleri.** Claimed shelter volume ve izin politikası uygula. **Kabul:** POI, yol, spawn, kritik rota korunur.
- **BLD-D051 — Yüzey örnekleme.** Raycast normal, terrain height, hit layer ve polygon sınır doğrulaması yap. **Kabul:** Zemin olmayan veya dik yüzey placement almaz.
- **BLD-D052 — Çakışma analizi.** Oriented footprint + OverlapBoxNonAlloc + collider filtreleri geliştir. **Kabul:** Oyuncu, kapı, yapı ve dünya geometry blokları ayrılır.
- **BLD-D053 — Sığınak güvenliği.** Kapı erişimi, araç parkı, sığınak giriş/çıkış clearance hesabı. **Kabul:** İnşa oyuncuyu veya araç çıkışını hapsetmez.
- **BLD-D054 — Navigation politika.** Temel yapı engelleri için nav validation ve rebake stratejisini seç. **Kabul:** Yeni yapı zombies'in tamamını nav dışı bırakmaz.
- **BLD-D055 — Ret nedenleri.** InvalidSlope, Overlap, OutOfZone, BlockedDoor, MissingResources vb enum tanımla. **Kabul:** Her geçersiz durum farklı açıklama döner.
- **BLD-D056 — Validator determinismi.** Preview ve commit için ortak saf validation API oluştur. **Kabul:** Kamera hareketi dışında aynı girdiye aynı gerekçe döner.

**Sprint exit:** D049–D056 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S008 — Yeşil-kırmızı ghost ve etkileşim

**Giriş bağımlılığı:** S007. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D057 — Ghost prefab ayrımı.** Gerçek prefab collider/AI/script çalıştırmadan preview mesh türet. **Kabul:** Ghost gameplay event veya ses üretmez.
- **BLD-D058 — Yeşil/kırmızı malzeme.** URP uyumlu saydam materyal ve obje sınıfına göre outline uygula. **Kabul:** Renderer kırılmaz, vendor materyal clone spam olmaz.
- **BLD-D059 — Rotation controls.** Mouse wheel/R veya atanmış kontrollerle 90°/serbest açı politikasını uygula. **Kabul:** Döndürme işlemi stabil ve okunaklıdır.
- **BLD-D060 — Terrain hizalama.** Preview ground normal/elevation, yapı tipi için tolerans. **Kabul:** Çit zemine gömülmez veya havada kalmaz.
- **BLD-D061 — Snap noktaları.** Çit uçları, zemin kenarı ve farklı blueprint anchor tipini göster. **Kabul:** Komşu parçalara yanlış ölçekli snap yapılmaz.
- **BLD-D062 — Geçersizlik açıklaması.** Kırmızı + ikon + kısa neden + alternatif yön önerisi sun. **Kabul:** Kullanıcı rengi görmeden hatayı öğrenir.
- **BLD-D063 — Frame cost.** Raycast/revalidate rate, cache ve yalnız dirty state güncellemesi. **Kabul:** Preview aşaması sürekli GC üretmez.
- **BLD-D064 — İptal/yaşam döngüsü.** Pause, death, inventory close ve scene unload ghost temizliği. **Kabul:** Oyunda hayalet prefab takılı kalmaz.

**Sprint exit:** D057–D064 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S009 — Yapı commit ve kalıcı kayıt

**Giriş bağımlılığı:** S007,S008. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D065 — Build request veri paketi.** DefinitionId, zoneId, transform, expected inv revision, requestNonce. **Kabul:** Belirsiz veya tekrarlı komut reddedilir.
- **BLD-D066 — Atomic kaynak tüketimi.** Validasyon sonrası OwnershipTransaction ile materyal commit uygula. **Kabul:** Başarısız create malzeme eksiltmez.
- **BLD-D067 — Spawn tescili.** StructureInstance stable ID, ownership ve yapı registry oluştur. **Kabul:** Aynı konum/ID tekrar spawn edilmez.
- **BLD-D068 — Rollback senaryoları.** Prefab bozuk, instantiate başarısız, envanter conflict ve scene unload durumları. **Kabul:** İşlem ya tamamlanır ya geri alınır.
- **BLD-D069 — Placed structure snapshot.** DefinitionId, world transform, owner zone, condition, revision, tombstone. **Kabul:** Save/load yapı sayısı ve açısı eşittir.
- **BLD-D070 — Cell owner seçimi.** Yapı cell sınırındaysa tek authoritative cell ve lease politikası. **Kabul:** Çift kayıt ve kaybolma yaşanmaz.
- **BLD-D071 — Migration stratejisi.** Construction içermeyen save dosyasına boş section varsayımı tanımla. **Kabul:** Eski save geriye uyumlu açılabilir.
- **BLD-D072 — Focused test altyapısı.** Ghost→commit, duplicate nonce, save/load, full inventory ve blocked spawn. **Kabul:** Kullanıcı komutla çalıştırabilecek hedefli testler vardır.

**Sprint exit:** D065–D072 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P04 — Entegre ilk playable loop

**Mission:** İlk bağımsız bütünleşik dikey dilimi bitir ve gerçek oyuncu değerini ölç.

**Exit gate:** G4: ağaç → tahta → defter → çit → save/load uçtan uca kabul.

### BLD-S010 — İlk entegre playable vertical slice

**Giriş bağımlılığı:** S003–S009. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D073 — İlk alan seçimi.** FirstTownCore safehouse/forest edge içinde küçük, güvenli ve gerçek rota belirle. **Kabul:** Mevcut staged ve ana sahne değişiklikleri korunur.
- **BLD-D074 — Pilot asset kit.** Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir. **Kabul:** Vendor kaynakları değişmez.
- **BLD-D075 — Eğitimsel işaretler.** Ağaç→tezgâh→defter→inşa akışında okunabilir ipuçları sun. **Kabul:** Oyuncu uzun metin okumadan hedefi kavrar.
- **BLD-D076 — İlk yapı tarifi.** Bir çit ve en az bir işlevsel farklı yapı için recipe finalleştir. **Kabul:** Sistem yalnız çite hard-code değildir.
- **BLD-D077 — Uçtan uca oynanış.** Kes, topla, üret, preview, yerleştir, iptal et, tekrar dene. **Kabul:** Oyuncu 15–30 dakikada somut yapı üretir.
- **BLD-D078 — Save/load döngüsü.** Üç yapı kur, bir geçersiz önizlemeyi iptal et, kaydet ve geri gel kontrol et. **Kabul:** Her obje/ödül ve stok miktarı korunur.
- **BLD-D079 — Oynanabilirlik ölçümü.** En az 3 bağımsız oynanış denemesinde süre/ret nedenini kaydet. **Kabul:** Kullanıcı hangi adımın sürtünme yarattığını ölçebilir.
- **BLD-D080 — G4 ürün kararı.** İlk loop keyif/performans/ret safety sonuçlarını değerlendir. **Kabul:** G4 PASS değilse modüler ev kapsamına geçiş bloke edilir.

**Sprint exit:** D073–D080 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P05 — Modüler ev ve yapı mekaniği

**Mission:** Temel, duvar, kapı, çatı ve küçük barınak kurulumu; oynanabilirlik ve fiziksel mantık.

**Exit gate:** G5: küçük modüler barınak inşası, dolaşım, kapı, barınak durumu.

### BLD-S011 — Temel ve zemin yapı seti

**Giriş bağımlılığı:** S010. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D081 — Foundation ölçü sistemi.** 1m/2m/4m uyumlu başlangıç grid/snap ve modül ölçeklerini kilitle. **Kabul:** Basit evi kapatmak için ölçü uyumsuzluğu yok.
- **BLD-D082 — Foundation prefabs.** Düz temel, yükseltilmiş temel ve corner parçalarına karar ver. **Kabul:** Her tür collision+LOD+preview ayrı tanımlı.
- **BLD-D083 — Zemin eğimi uyumu.** Ayak yüksekliği, destek ayağı ve maximum slope politikası geliştir. **Kabul:** Temel havada yüzmez veya toprağa gömülmez.
- **BLD-D084 — Terrain clearance.** Büyük taş, kök, yol ve mevcut world prop çakışmasını yönet. **Kabul:** Sahne dekorunu sessizce silmez.
- **BLD-D085 — Yükseklik ayarı.** Step nudge, ekran ipucu ve yerleştirme sınırları uygula. **Kabul:** Aralık dışı yükseklik net ret verir.
- **BLD-D086 — Foundation link ID.** Her temel için stable structure ID + bağlantı anchorları kaydet. **Kabul:** Save/load bağlantı şeması korunur.
- **BLD-D087 — Malzeme dengeleme.** Tahta/scrap için maliyet ve taşıma ağırlığı belirle. **Kabul:** Bir basit platform birkaç seferlik grind gerektirmez.
- **BLD-D088 — Test bankası.** Tepelik, düz zemin, komşu temel ve border-edge senaryoları. **Kabul:** Çakışma ve jitter yok.

**Sprint exit:** D081–D088 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S012 — Modüler duvar ve snap graph

**Giriş bağımlılığı:** S011. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D089 — Wall types.** Tam duvar, yarım duvar, pencere boşluğu için tip sistemi kur. **Kabul:** Prefab ölçüleri foundation kit ile eşleşir.
- **BLD-D090 — Snap graph.** Node/socket edge adjacency ve parent-child yaşam döngüsü. **Kabul:** Tüm parçalar tek GameObject hiyerarşisine zorlanmaz.
- **BLD-D091 — Ghost alignment.** Duvar zemin/temel edge'ine doğru yükseklikle yakalanır. **Kabul:** 90° dönüşte kayma olmaz.
- **BLD-D092 — Occupancy rules.** Aynı edge'e iki duvar kurmayı reddet; tür değiştirme politikası. **Kabul:** Duplicate placement yok.
- **BLD-D093 — Structural semantics.** Destek ihtiyacı minimal kuralda tasarlanır; voxel statics yok. **Kabul:** Havada sonsuz duvar dizisi belirgin biçimde sınırlandırılır.
- **BLD-D094 — Collision/navigation.** Duvarları agent/player kapatma testlerine entegre et. **Kabul:** NavMesh statik/dinamik risk belgelidir.
- **BLD-D095 — Destroy/rebuild graph.** Sökülen duvarın bağlantıları deterministik yenilenir. **Kabul:** Yetim anchor veya bozuk ID kalmaz.
- **BLD-D096 — Edit/play test seti.** Foundation+wall farklı yönler, pause, save/load. **Kabul:** Tekrar yüklemede gap/rotation farkı yok.

**Sprint exit:** D089–D096 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S013 — Kapı, pencere ve çatı paketi

**Giriş bağımlılığı:** S012. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D097 — Doorframe parçaları.** Kapı kasası ve açılır kapı modülleri ölçülerini çıkar. **Kabul:** Çerçeve giriş açıklığı karaktere uygundur.
- **BLD-D098 — Door runtime integration.** Mevcut DoorInteractable ya da uyumlu adaptör seç. **Kabul:** Yeni kapılar sığınak/world kapılarıyla tutarlı davranır.
- **BLD-D099 — Pencere variant.** Kapalı/açık çerçeve, görüş ve collision politikası. **Kabul:** İçeri görünmeyen boşluk/mesh çakışması yok.
- **BLD-D100 — Roof modular pieces.** Tek eğim, çift eğim, mahya ve köşe sınırı başlangıç seti. **Kabul:** En az bir küçük evin çatısı kapanabilir.
- **BLD-D101 — Roof overlap.** Çatı eğimi, tavandan yükseklik ve bitiş edge kontrolü. **Kabul:** Duvar ve çatıda görünür seam kabul standardı var.
- **BLD-D102 — Roof shelter flag.** Yağmurdan koruma için fiziksel üst yüzey/coverage doğrulaması. **Kabul:** Çatı altında wetness state doğru değişir.
- **BLD-D103 — Kapı/perf politikası.** Kapı collider lifecycle ve nav carving/obstacle maliyetini sınırla. **Kabul:** Spawn/door blocked hataları regresyona alınır.
- **BLD-D104 — Save gates.** Açık/kapalı kapı, kırık/onarım, bağlı çatı roundtrip. **Kabul:** Dünya restore edilince işlev korunur.

**Sprint exit:** D097–D104 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S014 — Yapı dayanımı, bağlantılar, inşa güvenliği

**Giriş bağımlılığı:** S012,S013. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D105 — Durability schema.** Condition, damage rules ve maxCondition versiyon politikası. **Kabul:** Yalnız planlanan hasar türleri işler.
- **BLD-D106 — Yapı hitboxes.** Zombie melee/araç hasarı entegrasyonu için basit ayrı collider. **Kabul:** Yapı FPS damage sistemini yanlış hedeflemez.
- **BLD-D107 — Support integrity.** Temel/duvar/çatı bağımlılığı için bounded DAG kontrolü. **Kabul:** Döngü veya sonsuz structural collapse yok.
- **BLD-D108 — Destruction safety.** İlk aşama kırık prefab/tombstone; pahalı fizik döküntüsü opsiyonel. **Kabul:** 50 duvar eşzamanlı fizik spawn etmez.
- **BLD-D109 — Onarım ve kaynak.** Durability restore ve maliyetin atomik transaction olması. **Kabul:** Eksik materyal ücretsiz tamir yaratmaz.
- **BLD-D110 — Demolish policy.** Oyuncu eliyle sökme, iade yüzdesi, sahiplik ve süre. **Kabul:** İptal/başarısızlık malzeme kaybı yaratmaz.
- **BLD-D111 — Threat interactions.** Zombilerin kapı/çit hedefleme riski ve path update stratejisi. **Kabul:** AI bütün zombi nüfusunu yapı collider'ına kilitlemez.
- **BLD-D112 — Test senaryoları.** Temel kaldırma, duvar sökme, çatı bağımlılık, save/load. **Kabul:** Bağlantılar tutarlıdır.

**Sprint exit:** D105–D112 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S015 — Küçük barınak oynanabilir demo

**Giriş bağımlılığı:** S011–S014. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D113 — Ev blueprint seti.** 2x2/3x3 foundation, duvar, kapı, çatı için ilk plan oluştur. **Kabul:** Mevcut kit ile çatılı yapı kurulabilir.
- **BLD-D114 — Kapsamlı builder UX.** Eğik zemin, çit, temel snap, giriş clearance testleri. **Kabul:** Oyuncu parçaları elle hizalamaktan yorulmaz.
- **BLD-D115 — Interior viability.** Player capsule, kamera, yatak/depo yaklaşımı ve içeride görüş testleri. **Kabul:** İçeride sıkışma veya clipping yok.
- **BLD-D116 — Rain and shelter.** Built roof için RainRoof/query entegrasyonunu güvenli adapt et. **Kabul:** Oyuncu kurduğu çatı altında kuru kalabilir.
- **BLD-D117 — Save/load integration.** Herkesin kendi stable ID/struct graph ile restore denemesi. **Kabul:** Kapı ve bağlı elemanları rastgele kaybolmaz.
- **BLD-D118 — Surface polish.** Modüler bağlantı dikişleri ve materyal standartlarını düzelt. **Kabul:** Sadece gri primitive'lerle demo kabul edilmez.
- **BLD-D119 — Manual experience pass.** Tam bir barınağı hasatla toplayıp kurma süresini ölç. **Kabul:** Efor/oyuncu ödülü dengelenir.
- **BLD-D120 — G5 kabul raporu.** Inşa edilebilir ev/yağmur/kapı/perf akışını raporla. **Kabul:** G5 onayı yoksa ilave yapı çeşitliliğine geçilmez.

**Sprint exit:** D113–D120 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P06 — Sığınakla birleşen üretim ve bakım

**Mission:** İşlevsel istasyonlar, onarım/söküm, güvenlik, elektrik-su ve ilerleme.

**Exit gate:** G6: yaşayan sığınak + kapsamlı craft ve maintenance loop.

### BLD-S016 — İşlevsel istasyonlar ve depolama

**Giriş bağımlılığı:** S015. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D121 — Station identity.** WorkBench, sawbench, storage, campfire definition/fonksiyon ayrımı. **Kabul:** Her yerleştirilen öğe hangi işlevi açtığını belirtir.
- **BLD-D122 — İstasyon erişimi.** Player radius/line of sight ve access policy. **Kabul:** Uzaktan sınırsız craft yapılamaz.
- **BLD-D123 — Kütük işleme.** Sawbench log→plank hız ve maliyet politikasını uygula. **Kabul:** Tool/station gereksinimi UI'da görünür.
- **BLD-D124 — World container.** Yerleştirilen sandık için InventoryContainer ownership kullan. **Kabul:** Sandık içerikleri kendiliğinden yok olmaz.
- **BLD-D125 — Station state save.** Çalışan iş, stash, kapatıldı/destroyed durumu snapshotı. **Kabul:** Unloaded cell sonrası job ilerlemesi tutarlı.
- **BLD-D126 — Station audio/FX.** Craft işlem ses ve gözle görünür progress. **Kabul:** Oyuncu uzun crafting'in devam ettiğini anlar.
- **BLD-D127 — Station upgrade.** Anlamlı tek yükseltme (ör. işlem hızı) ile sınırlı başla. **Kabul:** Yüzlerce anlamsız tier yok.
- **BLD-D128 — Integration tests.** Station move/remove blocked while job active, full outputs, transfer. **Kabul:** Dupe ve item kaybı yok.

**Sprint exit:** D121–D128 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S017 — Onarım, söküm ve yer değiştirme

**Giriş bağımlılığı:** S014,S016. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D129 — Build tool interface.** Hammer/build mode, repair/dismantle seçimlerini aynı eylem sistemiyle bağla. **Kabul:** Silah grip/reload state'ini bozmaz.
- **BLD-D130 — Repair preview.** Hasarlı parça, gerekli item, beklenen condition artışı göster. **Kabul:** Eksik tool/stok anlaşılıyor.
- **BLD-D131 — Repair transaction.** Maliyet, araç condition, target expected revision aynı commit. **Kabul:** Düşman hasarıyla yarışta kaynak iki kez gitmez.
- **BLD-D132 — Dismantle transaction.** Parçayı kaldır, iade malzemesi üret, tombstone oluştur. **Kabul:** Save/load sonrası sökülmüş parça geri gelmez.
- **BLD-D133 — Relocation.** Sandık/istasyon taşınmasında doluluk ve bağlı modül kuralları. **Kabul:** İçerik kaybolmaz ve bedava taşıma exploit yok.
- **BLD-D134 — Authorization.** Yalnız oyuncuya ait/izinli yapı sökülür, POI varlıkları korunur. **Kabul:** Kasaba bina parçaları yanlışlıkla sökülmez.
- **BLD-D135 — Cancel/undo.** Ghost ve confirm aşamasında kaçış; tamamlanan yapı geri alma kuralı. **Kabul:** Cancel işleminde hiçbir inventory değişmez.
- **BLD-D136 — QA matrix.** Dolu sandık, hasarlı çit, cancel/switch/save/load. **Kabul:** Kaynak invariants sağlam.

**Sprint exit:** D129–D136 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S018 — Sığınak utility entegrasyonu

**Giriş bağımlılığı:** S015–S017. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D137 — Claim+build zone.** ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir. **Kabul:** Eski 3 socket doğrulaması aynen çalışır.
- **BLD-D138 — Yatak ve konfor.** İnşa edilmiş barınak için yatak erişimi ve sleep validasyonu. **Kabul:** Açık arazide bed koymak bedava güvenlik yaratmaz.
- **BLD-D139 — Su toplama.** Yağmur suyu varili, kaynak girişi ve temizleme gereksinimi tasarla. **Kabul:** Hava durumu/ölçüm değişince su mantığı tutarlı.
- **BLD-D140 — Elektrik rolü.** Jeneratör tüketicileri için sınırlı konumlu producer/consumer modeli. **Kabul:** İlk aşamada fiziksel kablo simülasyonu zorunlu değil.
- **BLD-D141 — Lighting payoff.** Kurulan lambanın gerçek kullanımı, yakıt/akü gereksinimi. **Kabul:** Sığınak gelişimi oyuncuya görünür.
- **BLD-D142 — Workstation access.** Placeable station ile eski socket workstation recipe ikilemini çöz. **Kabul:** Tek crafting output authority korunur.
- **BLD-D143 — Threat/shelter safety.** Çit/kapı koruma sağlar ama zombie silmez, invulnerability yaratmaz. **Kabul:** World Pressure ve AI gerçek tehdidi korur.
- **BLD-D144 — Utility roundtrip.** Fuel, water, condition ve connected station state kayıtları. **Kabul:** Save ve world time advance sonucu tutarlı.

**Sprint exit:** D137–D144 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S019 — Uzun vadeli üs progression ve G6

**Giriş bağımlılığı:** S016–S018. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D145 — Progression graph.** 4 aşamalı barınak gelişimi, unlock maliyeti ve player reward. **Kabul:** Ana sefer için yeni hedef görünür.
- **BLD-D146 — Craft unlock policy.** Knowledge, station level ve exploration items ile recipe açma. **Kabul:** Kilitli tarif nedeni UI'da açık.
- **BLD-D147 — Safehouse value.** Daha güvenli stash, kuruluk, üretim, dinlenme faydalarını dengele. **Kabul:** Kurulmayan üs oynanışı imkânsız kılmaz.
- **BLD-D148 — Base defense loop.** Zombi davranışı ve ses tehdidi kontrollü üs etkileşimi. **Kabul:** Her gece zorunlu raid dayatılmaz.
- **BLD-D149 — Recipe catalog budget.** Başlangıç katalog sayısı ve gereksiz varyant budama. **Kabul:** Her tarif farklı işe yarar.
- **BLD-D150 — Repair economics.** Bakım maliyeti/gelir dengesi; bir haftalık kaynak öğütme önlenir. **Kabul:** Onarım oyuncuya angarya hissettirmez.
- **BLD-D151 — 30–60 min scenario.** Player setup→harvest→build→utilize→defend→save döngüsü. **Kabul:** Yapılan yapı somut fayda sağlar.
- **BLD-D152 — G6 review.** Stability, player satisfaction ve art/perf kabul raporu. **Kabul:** Tamamlanmamış faydalar PARTIAL kalır.

**Sprint exit:** D145–D152 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P07 — Dünya ekonomisi ve güvenilir kayıt

**Mission:** Yenilenme, kaynak dengesi, unloaded cell, uzun kayıt zinciri ve recovery.

**Exit gate:** G7: regresyon, migration, exploit ve uzun oturum testleri.

### BLD-S020 — Kaynak yenilenmesi ve ekoloji

**Giriş bağımlılığı:** S019. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D153 — Kaynak scarcity.** Finite tree/stone nodes, alternate sites ve guaranteed critical supply kuralları. **Kabul:** Oyuncu loot RNG yüzünden softlock olmaz.
- **BLD-D154 — Regrowth policy.** Ağaç yeniden büyüme isteği/uygulama maliyetini ölç; MVP kapalı olabilir. **Kabul:** Yenilenme world-time deterministik ve görünür.
- **BLD-D155 — Forest art compatibility.** Harvestable node ile görsel foliage ayrı yönetim. **Kabul:** Ağaç kesimi 500m ormanı hasarlı sahneye çevirmeden çalışır.
- **BLD-D156 — World Pressure integration.** Chop/build/dismantle ses olaylarını bounded cadence ile bağla. **Kabul:** Kayıt sonrası hayalet ses tekrar oluşmaz.
- **BLD-D157 — Respawn/ownership.** Drop ve item tombstone'ları aynı harvested node'a bağla. **Kabul:** Harvest reward tekrar yüklemede çoğalmaz.
- **BLD-D158 — Economy fallback.** Yedek ahşap kaynağı, görev kritik supply ve recovery yöntemi. **Kabul:** Bir bölge boşalsa da oyuncu oyunu bitirebilir.
- **BLD-D159 — Resource telemetry.** Saat başı kaynak girişi/gideri ve yapı tercihlerini izle. **Kabul:** Grind ve exploit veriyle görülebilir.
- **BLD-D160 — Biome footprint.** Kesilebilir ağaç oranı ve performance cap düzenini doğrula. **Kabul:** Zemin/vegetation pipeline'ı kırılmaz.

**Sprint exit:** D153–D160 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S021 — World streaming, save ve migration

**Giriş bağımlılığı:** S009,S015,S020. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D161 — Construction snapshot.** Yapı koleksiyonu world/cell sections ve schema version seç. **Kabul:** Eski save bozulmadan upgrade olur.
- **BLD-D162 — Boundary ownership.** Yapı iki cell'i keserse owner cell ve lease doğrula. **Kabul:** Double load ve kaybolma yok.
- **BLD-D163 — Unloaded simulation.** Firing generator, craft, regrowth gibi state'ler world time'a bağlı. **Kabul:** Yüklenmemiş sığınakta ücretsiz reset yok.
- **BLD-D164 — Atomic write interaction.** Save sırasında build/repair/dismantle transaction blok/kuyruk politikası. **Kabul:** Yarı kurulmuş yapı kaydedilmez.
- **BLD-D165 — Crash fault injection.** Temp write, manifest, quit ve yanlış schema durumlarını test et. **Kabul:** Önceki sağlam save korunur.
- **BLD-D166 — Migration fixtures.** Pre-construction ve mevcut S010 save fixture'larını otomatik dönüştür. **Kabul:** GUID/ID değişimi saveleri kırmaz.
- **BLD-D167 — 50 cycle soak.** Sahne load/unload + build/dismantle + save/load tekrarları. **Kabul:** Büyüyen duplicate referans ve memory leak yok.
- **BLD-D168 — Recovery UI.** Missing prefab/content version durumunda error/recovery mesajı. **Kabul:** Hatalı kayıt sessizce sıfırlanmaz.

**Sprint exit:** D161–D168 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S022 — Economy/pressure balance ve G7

**Giriş bağımlılığı:** S019–S021. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D169 — Resource economy table.** Log/plank/scrap/fastener input-output ve expected sefer sayısı hesapla. **Kabul:** Aynı materyal hiç düşünmeden farm edilmiyor.
- **BLD-D170 — Weapon/tool tradeoffs.** Balta dayanımı, kesme sesi ve çantaya taşınabilir hacmi dengele. **Kabul:** Farklı aletler anlamlı karar yaratır.
- **BLD-D171 — Build restrictions tuning.** Build radius/slope/clearance ve yapı maliyetini oyuncu geri bildirimiyle ayarla. **Kabul:** Reddedilen yerleşim adil/anlaşılır.
- **BLD-D172 — Threat feedback.** Kesim/inşa sesleri ve zombie baskısı için kullanıcıya okunabilir ipuçları. **Kabul:** Oyuncu saldırıyı sebepsiz saymıyor.
- **BLD-D173 — Softlock/exploit audit.** Zero-cost craft, dupe, infinite regrow, item swap, reload to refund. **Kabul:** Tespit edilen S0/S1 kapanmadan gate PASS olmaz.
- **BLD-D174 — Long session playtest.** Üç seferlik 90 dakikalık rota, farklı üs gelişimi seçimleri. **Kabul:** Oyuncu kendi sonraki hedefini söyleyebilir.
- **BLD-D175 — Performance budget update.** Dynamic structures + NPC nav/memory maliyet analizi. **Kabul:** Baseline delta ve hardware raporu var.
- **BLD-D176 — G7 trade-off decision.** Ekoloji/ekonomi kısıtları ve kanıt özetini onay için hazırla. **Kabul:** Gereksiz grind çıkarılır veya açıkça ertelenir.

**Sprint exit:** D169–D176 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P08 — Sanat, deneyim ve optimizasyon

**Mission:** Production-quality modeller, animasyon, ses, erişilebilir UI, profiler hedefleri.

**Exit gate:** G8: görsel kabul + performans + erişilebilirlik doğrulaması.

### BLD-S023 — Production asset kit ve animasyon

**Giriş bağımlılığı:** S015. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D177 — Visual style sheet.** Last Signal stylized-realistic palet, ölçü, texel, grunge seviyesi ve silhouette kuralları. **Kabul:** Paketler bir arada tutarlı görünür.
- **BLD-D178 — Asset mapping.** Mevcut ThirdParty ağaç/çit/duvar/kapı/tezgâh/araç ve project-owned kaynakları tara. **Kabul:** Eksik asset kanıtsız talep edilmez.
- **BLD-D179 — Hero blueprints.** Çit, kapı, temel, duvar, çatı, sandık, masa için yüksek kaliteli prefab varyantları. **Kabul:** Primitive-only content final PASS almaz.
- **BLD-D180 — LOD/collider policy.** Prefab bazında LOD, collider, shader ve batching kararları. **Kabul:** Visual ve performance birlikte düşünülür.
- **BLD-D181 — Harvest animations.** Balta, ağaç vuruşu, wood pickup ve FPS/TPS hands pipeline. **Kabul:** Mevcut karakter grip onarımları bozulmaz.
- **BLD-D182 — Build animations.** Hammer placement/repair/dismantle kısa feedback animasyonu. **Kabul:** Animasyon süreç durumunu gizlemez.
- **BLD-D183 — Icons/preview kit.** Tarif iconları ve ghost outline malzemeleri uyumlu set. **Kabul:** UI'da art placeholder kalmaz.
- **BLD-D184 — Art acceptance.** Game View sabit kamera gündüz/yağmur/gece kalite karşılaştırması. **Kabul:** Referansla gerçek oyun footage yan yana incelenir.

**Sprint exit:** D177–D184 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S024 — Ses, VFX ve kullanıcı hissi

**Giriş bağımlılığı:** S023. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D185 — Wood hit feedback.** Vuruş noktasında kontrollü chip/dust VFX. **Kabul:** VFX ilgili hit sonucuyla eşleşir.
- **BLD-D186 — Tree fall effects.** Kesim ve düşüş sesini ambient/environment ile karıştır. **Kabul:** Oyuncu düşüş yönünü işitebilir.
- **BLD-D187 — Harvest progress.** Kaynak miktar popup, hit reaction ve tool condition uyarısı. **Kabul:** Spam olmadığı gibi geri bildirim gecikmez.
- **BLD-D188 — Build placement feedback.** Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları. **Kabul:** Yanlış konum nedeninin görsel/ses karşılığı var.
- **BLD-D189 — Construction completion.** Yapı belirme/kurulum kısa animasyon politikası. **Kabul:** Anlık teleporter hissi azaltılır.
- **BLD-D190 — Damaged visual states.** New/damaged/repaired/ruined durumları ve material variants. **Kabul:** Condition görsel olarak okunabilir.
- **BLD-D191 — Weather audio interplay.** Yağmur altında tahta/metal ses düzeyi ve outdoor ambience dengesi. **Kabul:** Gürültü gameplay sisteminde ses mix'e bağlı değil.
- **BLD-D192 — Audio/VFX perf budget.** Pool, culling, max emit rate ve GC kontrolü. **Kabul:** 50 placement particle spam'e dönüşmez.

**Sprint exit:** D185–D192 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S025 — Craft Defteri UX ve erişilebilirlik

**Giriş bağımlılığı:** S006,S024. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D193 — Notebook navigation.** Arama, favoriler, kategoriler, locked/unlocked recipes. **Kabul:** Kritik tarif kaybolmaz.
- **BLD-D194 — Color accessibility.** Yeşil/kırmızı yerine ikon + metin + desen/outline seçeneği. **Kabul:** Renk körlüğü ve düşük renk modunda geçerlilik okunur.
- **BLD-D195 — Input rebinding.** Yerleştir/rotasyon/yükseklik/snap/cancel komutları binding tabanlı. **Kabul:** Kontroller hardcode tuşlarla sınırlı değil.
- **BLD-D196 — Controller path.** Analog ghost movement, rotate, UI focus ve confirm/back. **Kabul:** Mouse olmadan tam temel döngü yapılır.
- **BLD-D197 — Craft progression feedback.** Queued, in progress, waiting output, cancelled ve failed durumlarını göster. **Kabul:** Kaynak escrow yanlış anlaşılmaz.
- **BLD-D198 — Localized strings.** Türkçe/İngilizce terimler, kısa açıklama ve item isimleri veri modelinde. **Kabul:** UI literal string birikimi sınırlı.
- **BLD-D199 — Error resolution UX.** No resource, blocked zone, sloped ground, duplicate edge, busy system. **Kabul:** Ret sonrası kullanıcı nasıl düzelteceğini bilir.
- **BLD-D200 — Usability session.** 3 yeni oyuncu ile tutorial süre, yanlış denemeler ve comprehension ölç. **Kabul:** UI iyileştirmesi gözlemle kararlaştırılır.

**Sprint exit:** D193–D200 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S026 — CPU/GPU ve büyük dünya optimizasyonu

**Giriş bağımlılığı:** S023–S025. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D201 — Profiling baseline.** 1080p Mac development + Windows hedefindeki frame-time profile planı. **Kabul:** Donanım, görüntü ayarları ve koşullar belgelenir.
- **BLD-D202 — Ghost cost.** Raycast/nonalloc overlap/material property updates profile. **Kabul:** Placement UI per-frame GC üretmez.
- **BLD-D203 — Structure renderer budget.** Modüler parçalarda batching, draw call ve LOD stratejisi. **Kabul:** 200+ yapı parçasında kontrolsüz setpass artışı yok.
- **BLD-D204 — Physics cost.** BoxCollider, occlusion, dynamics ve tree fall bütçesi. **Kabul:** Yapı sonrası FixedUpdate spike yok.
- **BLD-D205 — AI/nav cost.** Sonradan eklenen engel/dinamik nav update maliyeti. **Kabul:** Zombiler sıkışmaz ve FPS dalgalanmaz.
- **BLD-D206 — Streaming memory.** Resident vs unloaded cell structure registry/memory kontrolü. **Kabul:** Önceki yüklemede kaynak bırakmayan leak yok.
- **BLD-D207 — Build stress scenarios.** 10/50/200 yapı, farklı canopy ve yağmur koşulları benchmark matrisi. **Kabul:** Sonuçlar hedef platform için gerçek ölçümdür.
- **BLD-D208 — Optimization gate.** G8 metrik eşikleri, açık perf borçları ve sadeleştirme kararları. **Kabul:** Bütçe aşılırsa güvenli kapsam azaltma vardır.

**Sprint exit:** D201–D208 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## P09 — Alpha kabul ve scope kapatma

**Mission:** Sistemlerin birlikte çalışmasını kanıtla, stabilizasyon ve ürün kararını ver.

**Exit gate:** G9: alpha playtest, release scope ve teknik devir kararı.

### BLD-S027 — End-to-end alpha QA ve regresyon

**Giriş bağımlılığı:** S010,S015,S019,S022,S026. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D209 — Acceptance matrix freeze.** Must requirements, evidence paths ve expected test behavior'ı kilitle. **Kabul:** Kritik gereksinim atlanmaz.
- **BLD-D210 — Complete gameplay route.** Forest→inventory→workbench→notebook→build→sleep→reload. **Kabul:** Eşya/malzeme/yapı state bütünlüğü korunur.
- **BLD-D211 — Death/recovery tests.** Build input sırasında ölüm, crafting pending, dismantle ve restore. **Kabul:** Ölüm bedeli dupe/kayıp oluşturmaz.
- **BLD-D212 — Concurrent state tests.** Craft+building same resources, save during action, scene unload. **Kabul:** Double spend ve yarım save yok.
- **BLD-D213 — Character/world regressions.** FPS/TPS grip, melee, movement, zombies, vehicle, NavMesh. **Kabul:** Unrelated S013–S023 sorunları yeniden açılmaz.
- **BLD-D214 — Visual matrix.** Day, sunset, dusk, rain, interior, forest UI/structure görüntüleri. **Kabul:** Kullanıcı görsel kabulü ayrı ve açık.
- **BLD-D215 — Hardware test.** Windows build, FPS, memory, input, shader compilation. **Kabul:** Mac PASS Windows PASS anlamına gelmez.
- **BLD-D216 — Alpha issue triage.** S0/S1 blocker, S2 polish, S3 backlogs ve reproduction linkleri. **Kabul:** Bloklayıcılar kapanmadan G9'a ilerlenmez.

**Sprint exit:** D209–D216 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

### BLD-S028 — Stabilizasyon, GDD güncelleme, teslim

**Giriş bağımlılığı:** S027. **Durum:** PROPOSED. **Handoff:** 8 kartın changed-file kaydı, test taslağı, manuel komut ve kanıt indeksi.

- **BLD-D217 — Critical fix sprint.** S027'den kalan S0/S1 root-cause düzeltmeleri. **Kabul:** Focused re-test ve full regression kanıtı olur.
- **BLD-D218 — Save migration signoff.** Eski save, yeni structures ve rollback akışını son kez test et. **Kabul:** Kaydetme kaybı release blocker sayılır.
- **BLD-D219 — Content cut review.** Gerçek maliyete göre COULD/SHOULD kapsamını kes veya ertele. **Kabul:** Kritik player loop korunur.
- **BLD-D220 — Balance final pass.** Kaynak oranları, üretim süreleri, geçerli alan retleri. **Kabul:** Prolonged grind ve exploitable farm yok.
- **BLD-D221 — Docs governance.** LS-DOC-14, LS-DOC-17 ve plan değişiklik kartını güncelle. **Kabul:** GDD çalışan sistemlerle uyumlu ama geçmiş karar korunur.
- **BLD-D222 — Manual QA bundle.** Tek komut akışları, screenshot seti, sample saves, evidence index. **Kabul:** Kullanıcı her gate'i tekrar çalıştırabilir.
- **BLD-D223 — Release readiness.** Open issues, perf ve Windows sonuçları, source state, ownership riski. **Kabul:** READY veya BLOCKED kanıtla ifade edilir.
- **BLD-D224 — Program retrospective.** Yapılan iş, ertelenen özellikler, sonraki üretim ve asset planı. **Kabul:** Bir sonraki phase kapsamı tahmin değil veriyle belirlenir.

**Sprint exit:** D217–D224 için uygulanma durumu, bu sprintin risklerine uygun owner-run tests ve en az bir görünür/reproducible davranış kanıtı. Teknik blocker varsa PARTIAL/BLOCKED ve sonraki kart.

## F. Gate kabul paketi

Her gate için aşağıdaki yapı zorunludur:

| Gate | Teknik doğrulama | Ürün/QA kanıtı | En yüksek risk |
|---|---|---|---|
| G0 | GDD ADR, domain contract, test plan | Director kapsam onayı | İstemsiz scope creep |
| G1 | Node-state, ownership, save, noise tests | Balta kesimi + resource pickup | Tekrarlı drop / zombie spawn uyumu |
| G2 | Multi-input craft, escrow, cancel, migration | Craft Defteri kullanılabilirlik | Dupe, item kaybı, UI split authority |
| G3 | Placement validator, commit, structure snapshot | Yeşil/kırmızı nedeni, blocked path | Zombi nav, actor clipping, save mismatch |
| G4 | İlk tam loop + 15–30 dk manual playtest | Gerçek Game View, oyuncu anlayışı | Eğlencesiz kaynak grind |
| G5 | Snap graph, door/roof/shelter tests | Küçük ev + FPS kullanımı | Yapı seam, collision, stuck |
| G6 | Shelter utility, craft job, repair tests | İşlevsel üs turu | İkinci craft authority / pahalı etkileşim |
| G7 | 50 cell/save döngüsü, migration, exploit | Uzun sefer dengesi | Yarım save ve kaynak çoğaltma |
| G8 | Frame-time, memory, visual QA | Day/rain/night screenshot kıyaslama | Art spam/perf regression |
| G9 | Full regression, Windows build, owner QA | Alpha ilerleme ve release kararı | Yayın engelleyici S0/S1 |

**Gating discipline:** Olmayan test sonucu PASS değildir. G4 başarısızsa modüler ev çeşitliliğine genişleme ertelenir; G5 başarısızsa yeni utility backlog'una kör geçiş yapılmaz. Her gate `READY`, `PARTIAL`, `BLOCKED` veya `PASS` durumuyla evidence tutar.

## G. Yeni sistemin önceki projeye etkisi

1. **GDD:** `LS-DOC-02`, `LS-DOC-14`, `LS-DOC-17`, `LS-DOC-13` ve kapsam karar yönetiminde yeni yapı üretim anlaşması (ADR).
2. **Envanter/recipe:** `PlayerInventory`/`InventoryContainer` sahipliğini koru; S010 `ShelterRecipe` tek girdi yapısını kırmak yerine geriye uyumlu adaptör yaz.
3. **Sığınak:** `ShelterSite.Validate()` üç socket (`Bed/Storage/Workbench`) kuralını bozma. `BuildZone` ile dış alan construction yetkisini *ayrı* yönet.
4. **Dünya:** `WorldCellManager` unload/hydration, `PersistentEntityId` authored ID ve runtime dynamic stable ID politikası ile uyum.
5. **Save:** Yeni yapı listesi, connection ID, condition ve tombstone için schema version; old save fixture ve recovery zorunlu.
6. **AI/Nav/Vehicle:** Kapı ve araç çıkışı bloklanamaz; path footprint içermeyen naif overlap yeterli değildir. Runtime nav carving pahalıysa bounded offline/repath yaklaşımı seç.
7. **World Pressure:** Balta ve inşa sesleri işitilebilir tehdittir; renderer/audio mixer gameplay NoiseEvent yetkisi değildir.
8. **FirstTownCore:** Ana sahne/VisualSlice, terrain, art varlıkları ve güncel owner editleri korunur; önce mini pilot alan, sonra yayılım.

## H. Test stratejisi — kullanıcı çalıştırır

### EditMode sözleşme testleri

- Recipe catalog ID/cycle/overflow; ingredient count; invalid cost.
- Placement pure functions (slope, transform, occupied edge, zone, clearance, preview vs commit).
- Resource mass preservation; ownership uniqueness; duplicate nonce; rollback.
- Structure ID/owner/connection graph/snapshot/migration.
- Save pre-construction/legacy S010 fixture with empty structure section.

### PlayMode ve entegrasyon testleri

- Chop damage timing and item spawn; no reward duplication.
- Inventory full and dropped logs; sawbench processing; UI close/cancel.
- Placing near a door/zombie path/vehicle spawn denied without consumption.
- Ghost lifecycle during pause/death/cell unload; correct camera visuals.
- Build/repair/dismantle/save/load in different sequences; cell boundary.
- Roof/rain, door usage, collision, zombie nav, generator/noise.

### Manuel görsel/oynanış kabul

- Fixed FPS camera: valid green, invalid red, colorblind fallback text+icon, snap rotate.
- Forest chopping, town repair, sheltered storage, utility payoff.
- Day, sunset, dusk, rain and night conditions.
- Windows standalone perf validation separately; Mac dev run does not substitute Windows release.

**Komut politikası:** Uygulama sırasında gerçek Unity assembly/test platform isimleri tespit edilecek. Var olmayan `Tools/bld-verify.py` gibi komutları çalışıyormuş gibi yazmak yasaktır. Her sprint kapanışında en dar, çalıştırılabilir komut kullanıcının onayına sunulur. Kullanıcı log gönderir; ajan sadece ilgili kök nedeni düzeltip focused retest ister; ardından regression gate'e geçilir.

## I. Duruma göre scope azaltma ve risk tablosu

| Risk | Şiddet | Önleme / çıkış |
|---|---|---|
| Dinamik yapı ile save kaybı/dupe | Kritik | Atomic placement + stable ID + fault injection; olmadan G3/G7 geçme |
| Mevcut S010 craft davranışının bozulması | Kritik | Yeni recipe adapter, old-save regression |
| NavMesh/stuck/araç bloklama | Kritik | Clearances + gerçek kapsül testi + nav acceptance |
| Foliage hasatı nedeniyle yüzlerce pahalı entity | Yüksek | Decoration vs harvestable ayrı; bounded list, pool/LOD |
| Modüler yapı snapping karmaşıklığı | Yüksek | 1–2 module grid, ilk ev kitini küçük tut |
| Art kalitesinin düşük kalması | Yüksek | Vendor/AI assetler için Blender/URP ve gerçek Game View kabulü |
| Oyuncu girdi ekonomisinin grind'e dönüşmesi | Yüksek | Erken G4 oynanış, kaynak/builder süre telemetry |
| Dynamic structure başına runtime AI/per-frame work | Yüksek | Spatial registry, dirty-only validation, bounded events |
| UI karmaşıklığı | Orta | Tek defter, progressive reveal ve erişilebilir hata metni |
| Co-op kapsamının sızması | Orta | Single-player authority, network SDK yok |

### Ertelemeye uygun özellikler

- Tam yapı yıkımı/physics collapse, voxel sculpting, complex fluid simulation, serbest yükseklikte çok katlı megabase, NPC işçileri, co-op, savunma raid dalgaları ve her ağacı kesilebilir yapmak.

**Kesilmemesi gereken ürün çekirdeği:** Kaynak toplama → crafting → gerçek yerleştirme → malzeme güvenliği → kalıcılık → anlaşılır feedback. Bu unsurlar kesilirse yeni program amacını kaybeder.

## J. Üretime geçmeden önce ilk adım

**BLD-S001 yalnız plan+tasarım onayıdır.** Director GDD scope ADR'ını kabul edip ürün sınırını kesinleştirdikten sonra BLD-S002 minimum repo audit ile başlar. Ajan hiçbir test/build çalıştırmaz. Program mevcut `S0xx` kimliklerini yeniden adlandırmaz. Hangi phase'in eskisiyle çakıştığı release karar kaydında yazılır.

### Source index (inspected GitHub)

- `Docs/Last_Signal_Docs_v0.2/sources/02_Vizyon_Donguler_ve_Dunya.md`
- `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md`
- `Docs/Last_Signal_Docs_v0.2/sources/07_Item_Inventory_Equipment.md`
- `Docs/Last_Signal_Docs_v0.2/sources/12_World_Pressure.md`
- `Docs/Last_Signal_Docs_v0.2/sources/13_World_Streaming_POI.md`
- `Docs/Last_Signal_Docs_v0.2/sources/17_Save_Load_Migration.md`
- `Docs/Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md`
- `Docs/Implementation/S010/IMPLEMENTATION_REPORT.md`
- `Assets/LastSignal/Scripts/Runtime/Shelter/{ShelterSite,ShelterSocket,ShelterRecipe,ShelterProduction}.cs`
- `Assets/LastSignal/Scripts/Runtime/Inventory/{PlayerInventory,WorldItem}.cs`
- `Assets/LastSignal/Scripts/Runtime/Persistence/{SaveSession,PersistentEntityId}.cs`
- `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs`

*Bu belge öneri roadmap'idir; repository'ye veya FirstTownCore'a henüz uygulanmış değildir.*
