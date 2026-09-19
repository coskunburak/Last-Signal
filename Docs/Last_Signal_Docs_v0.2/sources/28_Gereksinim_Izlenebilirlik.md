---
doc_id: LS-DOC-28
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Gereksinim matrisi, kaynak eşlemesi ve mevcut durum

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: GDD tamamı; v0.2 sistem sözleşmeleri. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Kullanım

Bu matris kaynak dosyalardaki açık REQ hükümlerinden üretilmiştir. Her satırın kimliği, tek ana belge sahibi ve başlangıç implementation/test durumu vardır. Gereksinim eksiksiz ürün backlog'u değildir; ayrıntılı bölüm kuralları da geçerlidir. Burada NOT_VERIFIED oyunun bozuk olduğunu değil, gerçek repository/Unity kanıtının incelenmediğini söyler.

Implementation durumları: NOT_VERIFIED → NOT_STARTED/IN_PROGRESS/IMPLEMENTED → DEPRECATED. Test durumları NOT_RUN/PASS/FAIL/BLOCKED/N/A. PASS için evidence path, build commit ve tarih eklenir. IMPLEMENTED tek başına PASS değildir. Belge link kontrolü bu satırların oyun doğrulaması sayılmaz.

## 2. Kanonik gereksinimler

| Gereksinim | Sözleşme | Sahip belge | Uygulama | Doğrulama |
|---|---|---|---|---|
| REQ-GOV-001 | Her görev kaynak belge kimliği, gereksinim kimliği, uygulama durumu ve test kanıtını ayrı taşır. “Tasarımda var” ile “oyunda çalışıyor” aynı sütunda tutulamaz. | [01_Kararlar_ve_Kapsam.md](01_Kararlar_ve_Kapsam.md) | NOT_VERIFIED | NOT_RUN |
| REQ-GOV-002 | Davranış değişikliğinde etkilenen requirement, balance key, save schema, test ve içerik kaydı birlikte gözden geçirilir. Sadece sohbet mesajında kalan karar yürürlükte sayılmaz. | [01_Kararlar_ve_Kapsam.md](01_Kararlar_ve_Kapsam.md) | NOT_VERIFIED | NOT_RUN |
| REQ-GOV-003 | Onaylanmamış öneri otomatik olarak CONFIRMED'a yükseltilmez. Bir ajan “GDD böyle diyor” gerekçesiyle kullanıcının son isteğini geçersiz kılamaz. | [01_Kararlar_ve_Kapsam.md](01_Kararlar_ve_Kapsam.md) | NOT_VERIFIED | NOT_RUN |
| REQ-VIS-001 | Her temel özellik hazırlık, risk değerlendirme, keşif, toparlanma veya ilerleme kararlarından en az birine hizmet eder. Yalnız menü sayısını artıran sistem ilk slice'a alınmaz. | [02_Vizyon_Donguler_ve_Dunya.md](02_Vizyon_Donguler_ve_Dunya.md) | NOT_VERIFIED | NOT_RUN |
| REQ-VIS-002 | Slice'ta aynı hedefe en az iki geçerli erişim yaklaşımı bulunur. Her iki yaklaşım da test edilir; yalnız tasarım çiziminde gösterilmesi yeterli değildir. | [02_Vizyon_Donguler_ve_Dunya.md](02_Vizyon_Donguler_ve_Dunya.md) | NOT_VERIFIED | NOT_RUN |
| REQ-VIS-003 | Ana ilerleme bir loot RNG sonucuyla kalıcı kilitlenemez. Kritik araç/parça için garantili kaynak veya açık recovery yolu gerekir. | [02_Vizyon_Donguler_ve_Dunya.md](02_Vizyon_Donguler_ve_Dunya.md) | NOT_VERIFIED | NOT_RUN |
| REQ-ARC-001 | Inventory, loot, damage hesabı ve save DTO'ları GameObject/Transform referansını kalıcı kimlik yerine kullanamaz. EditMode'da çoğu domain kuralı scene yüklemeden test edilebilir. | [03_Teknik_Mimari.md](03_Teknik_Mimari.md) | NOT_VERIFIED | NOT_RUN |
| REQ-ARC-002 | Session stop idempotent olmalıdır. Aynı iptal çağrısı iki kez gelirse asset handle iki kez release edilmez ve event listener exception üretmez. | [03_Teknik_Mimari.md](03_Teknik_Mimari.md) | NOT_VERIFIED | NOT_RUN |
| REQ-ARC-003 | Başarısız command inventory, ammo, health, pressure veya entity tombstone'unu kısmen değiştiremez. Yan etkili telemetry bile committed/rejected ayrımını taşır. | [03_Teknik_Mimari.md](03_Teknik_Mimari.md) | NOT_VERIFIED | NOT_RUN |
| REQ-DATA-001 | Scene duplication yeni authored entity kimliği üretir; prefab instance'larının aynı ID ile kayda girmesi build validation hatasıdır. Transform path, display name veya GetInstanceID kalıcı anahtar değildir. | [04_Veri_Kimlik_Command_Event.md](04_Veri_Kimlik_Command_Event.md) | NOT_VERIFIED | NOT_RUN |
| REQ-DATA-002 | Merge toplam miktarı/volümü yaratamaz. Split edilen stack yeni instance ID alır; iki parçanın kütle toplamı orijinale eşittir. | [04_Veri_Kimlik_Command_Event.md](04_Veri_Kimlik_Command_Event.md) | NOT_VERIFIED | NOT_RUN |
| REQ-DATA-003 | Event payload geçmişteki olayı temsil eden immutable bilgi taşır. Daha sonra değişen component'e bakarak olayın hasar miktarı yeniden hesaplanmaz. | [04_Veri_Kimlik_Command_Event.md](04_Veri_Kimlik_Command_Event.md) | NOT_VERIFIED | NOT_RUN |
| REQ-MOVE-001 | Hareket hızı render FPS'ine bağımlı olamaz; diagonal input normalize edilir, analog input büyüklüğü korunur. Aynı düz parkur 30/60/120 FPS'te kabul toleransında aynı sürede geçilir. | [05_Player_Input_Movement.md](05_Player_Input_Movement.md) | NOT_VERIFIED | NOT_RUN |
| REQ-MOVE-002 | Çömelmeden kalkarken tavan overlap kontrolü yapılır. İzin yoksa crouch korunur; kamera collider'ın üstüne çıkamaz. | [05_Player_Input_Movement.md](05_Player_Input_Movement.md) | NOT_VERIFIED | NOT_RUN |
| REQ-MOVE-003 | Normal/slope/step/low-ceiling parkuru gerçek buildde geçilmelidir. Kabul matrisi: spawn, look, WASD, jump, sprint depletion, recover, crouch head block, pause return, alt-tab, menu-return-repeat. | [05_Player_Input_Movement.md](05_Player_Input_Movement.md) | NOT_VERIFIED | NOT_RUN |
| REQ-INT-001 | HUD prompt ile commit aynı eligibility politikasını kullanır. Commit anında world revision/mesafe/occlusion yeniden doğrulanır; eskimiş preview hak vermez. | [06_Interaction_Door_WorldItem.md](06_Interaction_Door_WorldItem.md) | NOT_VERIFIED | NOT_RUN |
| REQ-INT-002 | Aynı world item iki komutla istenirse en fazla biri başarılı olur. Başarısız drop/pickup toplam item miktarını değiştirmez. | [06_Interaction_Door_WorldItem.md](06_Interaction_Door_WorldItem.md) | NOT_VERIFIED | NOT_RUN |
| REQ-INT-003 | Duvar arkasından loot, açık kapı üzerinden yanlış container seçimi ve kapının içine item bırakma testte reddedilmelidir. İki komutta tek pickup, full bag red, blocked drop, held interaction sırasında hasar, pause, target destroy ve unload senaryoları ölçülür. Hot query allocation testi yalnız warmed resolver üzerinde yapılır; yükleme sırasındaki allocationla karıştırılmaz. | [06_Interaction_Door_WorldItem.md](06_Interaction_Door_WorldItem.md) | NOT_VERIFIED | NOT_RUN |
| REQ-INV-001 | Her ItemInstanceId aynı anda yalnız bir owner'a aittir. Transfer kaynak ve hedefi tek transactionda değiştirir. UI'da listeden silmek ownership değişikliği değildir. | [07_Item_Inventory_Equipment.md](07_Item_Inventory_Equipment.md) | NOT_VERIFIED | NOT_RUN |
| REQ-INV-002 | Bag küçültme veya çıkarma eşya silemez. Yeni kapasite yeterli değilse işlem reddedilir ve oyuncuya önce boşaltma/drop seçeneği gösterilir. Otomatik dağılma slice dışında kalır. | [07_Item_Inventory_Equipment.md](07_Item_Inventory_Equipment.md) | NOT_VERIFIED | NOT_RUN |
| REQ-INV-003 | Capacity red, stale revision, missing target ve slot restriction farklı hata kodu olmalıdır. Hepsini “işlem başarısız” göstermek debug ve oyuncu öğrenmesini bozar. | [07_Item_Inventory_Equipment.md](07_Item_Inventory_Equipment.md) | NOT_VERIFIED | NOT_RUN |
| REQ-LOOT-001 | Kampanya görevini açan kritik parça yalnız rastgele çekilişe bağlı olamaz. Garantili yerleşim veya yeniden edinme fallback'i kayıtlıdır. | [08_Loot_Ekonomi_ve_Respawn.md](08_Loot_Ekonomi_ve_Respawn.md) | NOT_VERIFIED | NOT_RUN |
| REQ-LOOT-002 | First search retry aynı sonucu vermelidir. Generation başarıyla state'e girdikten sonra presentation/load hatası yeni roll tetikleyemez. | [08_Loot_Ekonomi_ve_Respawn.md](08_Loot_Ekonomi_ve_Respawn.md) | NOT_VERIFIED | NOT_RUN |
| REQ-LOOT-003 | Item drop/pickup, split/merge veya aynı container'a geri koyma reward/pressure farming üretemez. Loot acquired ile newly depleted world source ayrı eventlerdir. | [08_Loot_Ekonomi_ve_Respawn.md](08_Loot_Ekonomi_ve_Respawn.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SURV-001 | Offline geçen duvar saati, varsayılan solo kayıtta karakteri açlıktan öldürmez. Load, kayıtlı world time'dan devam eder. Sistem saati değiştirmek item tazeliğini sıfırlamaz. | [09_Survival_Health_Death.md](09_Survival_Health_Death.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SURV-002 | Aynı wound'a iki tedavi isteği aynı consumable'ı iki kez uygulayamaz. Treatment commit wound revision ve item revision doğrular. | [09_Survival_Health_Death.md](09_Survival_Health_Death.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SURV-003 | Death bir kez işlenir. Health=0 sonrası yeni consume/fire/loot reddedilir; inventory kaybı, bag entity ve recovery anchor tek tutarlı state geçişi oluşturur. | [09_Survival_Health_Death.md](09_Survival_Health_Death.md) | NOT_VERIFIED | NOT_RUN |
| REQ-CMB-001 | Bir ateş komutu ancak Ready state, geçerli ammo ve eylem izni varsa kabul edilir. Kabul ammo tüketimini ve shotId üretimini aynı committe yapar; reddedilen ateş NoiseEvent üretmez. | [10_Combat_Weapons_Damage.md](10_Combat_Weapons_Damage.md) | NOT_VERIFIED | NOT_RUN |
| REQ-CMB-002 | Reload iptal/save/load/equip change sırasında toplam ammo yalnız gerçek ateş veya açık discard ile azalabilir; kendiliğinden artamaz. | [10_Combat_Weapons_Damage.md](10_Combat_Weapons_Damage.md) | NOT_VERIFIED | NOT_RUN |
| REQ-CMB-003 | Kamera sarsıntısı ve gore kapatıldığında gameplay hasarı, hit timing ve AI hearing değişmez. Görsel seçenekler oyunun authority katmanına yazamaz. | [10_Combat_Weapons_Damage.md](10_Combat_Weapons_Damage.md) | NOT_VERIFIED | NOT_RUN |
| REQ-AI-001 | Görüş kaybolduğunda zombi oyuncunun anlık koordinatını sınırsız izleyemez. Chase'ten search'e geçiş memory confidence ve zamanla belirlenir. | [11_Zombie_AI_Perception_Population.md](11_Zombie_AI_Perception_Population.md) | NOT_VERIFIED | NOT_RUN |
| REQ-AI-002 | LOD düşürme health, loot identity veya ölüm durumunu sıfırlayamaz. Görüşteki agent'ın hitbox'ı performans gerekçesiyle gizlice kaybolamaz. Combat/targeted agent mesafeden bağımsız geçici yüksek öncelik alır. | [11_Zombie_AI_Perception_Population.md](11_Zombie_AI_Perception_Population.md) | NOT_VERIFIED | NOT_RUN |
| REQ-AI-003 | Geçiş öncesi logical+physical nüfus, geçiş sonrası logical+physical nüfusa eşittir; doğum/spawn/migration/death ayrıca ledger olayıdır. Bir grup iki komşu hücrede aynı anda materialize edilemez. | [11_Zombie_AI_Perception_Population.md](11_Zombie_AI_Perception_Population.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PRS-001 | Pressure bir spawn emri değildir. Director önce nüfus, yolculuk süresi, visibility, pacing ve performans bütçesini kontrol eder. | [12_World_Pressure.md](12_World_Pressure.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PRS-002 | Denge formülü idempotent event receipt ve bounded aggregation kullanır; save/load sonrasında eski sesler yeniden pressure üretmez. | [12_World_Pressure.md](12_World_Pressure.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PRS-003 | Testte horde'nun geliş kaynağı ve ETA açıklanabilmeli. Görüşte veya oyuncuya haksız tepki süresi bırakan noktada materialize edilmesi red nedenidir. | [12_World_Pressure.md](12_World_Pressure.md) | NOT_VERIFIED | NOT_RUN |
| REQ-WORLD-001 | CellReady ancak collision, persistent restore ve kritik navigation bağımlılıkları hazırsa yayımlanır. Asset load callback'i tek başına “oynanabilir” değildir. | [13_World_Streaming_POI.md](13_World_Streaming_POI.md) | NOT_VERIFIED | NOT_RUN |
| REQ-WORLD-002 | Silinmiş item tombstone'u unload/load ile kaybolamaz. Bir kapının state'i bir sonraki ziyarette authoring default'una dönmez. Load ve mutation yarışınca expected revision/generation kontrol edilir. | [13_World_Streaming_POI.md](13_World_Streaming_POI.md) | NOT_VERIFIED | NOT_RUN |
| REQ-WORLD-003 | Hızlı ileri-geri cell traversal, save sırasında unload, load error, duplicated POI dependency, session exit during activation, 50 yükle/boşalt döngüsü ve recovery spawn test edilir. Bellek sabit referans senaryoya döndüğünde plateau'ya yaklaşmalıdır; önbellek ısınması ile leak ayrılır. Uçurum veya yarım yüklenmiş kapı nedeniyle ilerleme kilidi release blocker'dır. | [13_World_Streaming_POI.md](13_World_Streaming_POI.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SHL-001 | Claim, dünyadaki zombileri silmez veya görünmez dokunulmazlık küresi yaratmaz. Güvenlik uygun kapılar, yakın threat kontrolü ve koruma modüllerinden hesaplanır. | [14_Siginak_Crafting_Progression.md](14_Siginak_Crafting_Progression.md) | NOT_VERIFIED | NOT_RUN |
| REQ-CRF-001 | İptal, save/load ve output capacity failure malzeme çoğaltamaz veya yok edemez. Output için ayrılan yer dolmuşsa CompletedWaitingOutput state veya workstation output container kullanılır; sonuç item'i silinmez. | [14_Siginak_Crafting_Progression.md](14_Siginak_Crafting_Progression.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SHL-002 | Unloaded shelter'daki yakıt, hava ve jobs world time boyunca doğru ilerler. Yeniden load “full fuel” veya tamamlanmamış sonsuz job üretmez. | [14_Siginak_Crafting_Progression.md](14_Siginak_Crafting_Progression.md) | NOT_VERIFIED | NOT_RUN |
| REQ-TIME-001 | Her timer'ın zaman tabanı adında veya şemasında belirtilir. duration=10 tek başına yeterli alan adı değildir. World time değişimi double/long çözünürlüğüyle uzun save'lerde precision kaybını sınırlamalıdır. | [15_Time_Weather_Environment.md](15_Time_Weather_Environment.md) | NOT_VERIFIED | NOT_RUN |
| REQ-TIME-002 | Time skip sırasında ölüm veya saldırı sınırına gelinirse kalan süre uygulanmaz. Geri dönüşte timer'lar eski hedef zamana göre sahte completion üretmez. | [15_Time_Weather_Environment.md](15_Time_Weather_Environment.md) | NOT_VERIFIED | NOT_RUN |
| REQ-TIME-003 | 60 world minute küçük tick ile AdvanceUntil sonucu belirlenen tolerans içinde eşleşir. Aynı save aynı weather sequence'i verir; UI beklemek günü ilerletmez. Roof boundary, coat exchange, campfire fuel exhaustion, sleep interruption, 100 gün ileri kayıtta precision ve session resume karşılaştırılır. Bu sonuçlar ölçülmeden sistem “deterministic” diye pazarlanmaz; fizik determinismi ayrıca garanti edilmez. | [15_Time_Weather_Environment.md](15_Time_Weather_Environment.md) | NOT_VERIFIED | NOT_RUN |
| REQ-OBJ-001 | Objective completion ve tek seferlik reward receipt aynı persistence transaction'ında yazılır. Load sonrası görev tamamlanmış görünürken ödülün iki kez alınması mümkün olamaz. | [16_Narrative_Objectives_Events.md](16_Narrative_Objectives_Events.md) | NOT_VERIFIED | NOT_RUN |
| REQ-OBJ-002 | Phase geçişi yüklenmiş ve yüklenmemiş bölgeler için aynı kuralları taşır. Bir cell eski sahne state'iyle açıldığında global phase'i geriye yazamaz. | [16_Narrative_Objectives_Events.md](16_Narrative_Objectives_Events.md) | NOT_VERIFIED | NOT_RUN |
| REQ-OBJ-003 | Sıra dışı oynanış desteklenir: fuse önce bulunur, radio sonra okunur; repair sırasında save; reward alındıktan sonra crash; kritik item drop; not tekrar okunur; POI önceden temizlenir. Bu yollar completion count'u ve reward sayısını ikiye katlayamaz. Voice line eksikliği objective'i kilitlemez; caption/journal fallback bulunur. | [16_Narrative_Objectives_Events.md](16_Narrative_Objectives_Events.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SAVE-001 | Save yüklenmiş GameObject listesinin toplamı olamaz. Unloaded cell deltalari, player, jobs, objectives, pressure, population ve tombstone state'i aynı world generation'a aittir. | [17_Save_Load_Migration.md](17_Save_Load_Migration.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SAVE-002 | Item transfer sırasında kaynak/target ve world tombstone aynı sequence'te görünür. Save'in bir yarısı “önce”, diğer yarısı “sonra” olamaz. | [17_Save_Load_Migration.md](17_Save_Load_Migration.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SAVE-003 | Hatalı load orijinal dosyayı değiştiremez. Recovery denemeleri kopya/generation üzerinde yapılır. Başarısız migration sonrası eski sürümle açma olanağı korunur. | [17_Save_Load_Migration.md](17_Save_Load_Migration.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SAVE-004 | Serialize öncesi, temp yazma ortası, manifest tamamlandıktan önce/sonra, current pointer güncellemesi ve backup rotation noktalarında process interruption denenir. Her durumda en az önceki geçerli generation yüklenebilmelidir. Coverage yüzdesi yerine bu kesinti noktaları ayrı raporlanır. | [17_Save_Load_Migration.md](17_Save_Load_Migration.md) | NOT_VERIFIED | NOT_RUN |
| REQ-UX-001 | Aynı işlemin başarı/red durumu UI, ses ve world representation'da çelişemez. Transfer reddedildiğinde başarı sesi çalmaz. | [18_UI_UX_Accessibility.md](18_UI_UX_Accessibility.md) | NOT_VERIFIED | NOT_RUN |
| REQ-UX-002 | Pause/menu politikası profile içinde sabittir ve HUD'da belirsiz değildir. Oyuncu pause sandığı ekranda gizlice açlık veya saldırıyla karşılaşmaz. | [18_UI_UX_Accessibility.md](18_UI_UX_Accessibility.md) | NOT_VERIFIED | NOT_RUN |
| REQ-UX-003 | Font fallback Türkçe ç, ğ, ı, İ, ö, ş, ü karakterlerini göstermeli. Pseudo-localization +%30 metin uzunluğunda kritik buton ve prompt kesilmemeli. Controller glyph metin gibi çevrilmez. | [18_UI_UX_Accessibility.md](18_UI_UX_Accessibility.md) | NOT_VERIFIED | NOT_RUN |
| REQ-ART-001 | Asset kabulü tek başına güzel görünmesine göre değil benchmark sahnesinde diğer assetlerle uyumuna göre yapılır. Aynı ölçek/ışıkta karşılaştırılmadan marketplace paketi üretim standardı sayılmaz. | [19_Art_Audio_Asset_Pipeline.md](19_Art_Audio_Asset_Pipeline.md) | NOT_VERIFIED | NOT_RUN |
| REQ-ART-002 | Lisans, kaynak URL, satın alma/erişim kaydı ve değişiklik geçmişi asset envanterinde tutulur. AI üretimi dahil yeniden dağıtım hakkı varsayılmaz. Bu belge lisans uygunluğu incelemesi yapılmış demek değildir; paket bazında doğrulanır. | [19_Art_Audio_Asset_Pipeline.md](19_Art_Audio_Asset_Pipeline.md) | NOT_VERIFIED | NOT_RUN |
| REQ-ART-003 | Low quality ve reduced gore modunda hit, wound, muzzle ve threat geri bildirimi hâlâ anlaşılırdır. Ses ayarı gameplay noise radius'ünü değiştiremez. | [19_Art_Audio_Asset_Pipeline.md](19_Art_Audio_Asset_Pipeline.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PERF-001 | Performans raporu Editor FPS'ini Windows retail performansı diye sunamaz. Development build profiling ile release candidate ölçümleri ayrı kaydedilir; deep profiling overhead belirtilir. | [20_Performance_Build_Operations.md](20_Performance_Build_Operations.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PERF-002 | Optimizasyon kararı ölçülmüş darboğaza dayanır. Draw call azalması gameplay correctness testlerini veya save state'i bozamaz. Yakındaki düşmanı görünmez yapmak performans düzeltmesi değildir. | [20_Performance_Build_Operations.md](20_Performance_Build_Operations.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PERF-003 | Memory leak, GC spike veya streaming hitch raporu tekrar üretim yolu ve capture taşır. “Optimize edildi” ifadesi öncesi/sonrası aynı koşullu sayı olmadan kullanılmaz. | [20_Performance_Build_Operations.md](20_Performance_Build_Operations.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SLICE-001 | “Vertical slice tamam” demek için gerçek buildde kesintisiz 30-45 dakikalık rota ve save/quit/load tekrarı kanıtlanır. Editor'de ayrı sahnelerde çalışan bileşenler yeterli değildir. | [21_Vertical_Slice_ve_Playtest.md](21_Vertical_Slice_ve_Playtest.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SLICE-002 | Eğlence değerlendirmesi yalnız geliştiricinin başarıyla bitirmesine dayanamaz. Yardımlı ve yardımsız completion ayrı raporlanır. | [21_Vertical_Slice_ve_Playtest.md](21_Vertical_Slice_ve_Playtest.md) | NOT_VERIFIED | NOT_RUN |
| REQ-SLICE-003 | Gate raporu tasarım önerisini kod kanıtı yerine koyamaz. Build ID, fixture, tester, tarih, log ve gözlem bağlanmalıdır. Şu anda bu kanıtlar üretilmemiştir. | [21_Vertical_Slice_ve_Playtest.md](21_Vertical_Slice_ve_Playtest.md) | NOT_VERIFIED | NOT_RUN |
| REQ-QA-001 | Çalıştırılmayan test NOT_RUN, erişim engeli BLOCKED, geçen gerçek test PASS, assertion/acceptance hatası FAIL olarak raporlanır. Kaynak kodu incelemek PlayMode PASS sayılmaz. | [22_QA_Acceptance_Traceability.md](22_QA_Acceptance_Traceability.md) | NOT_VERIFIED | NOT_RUN |
| REQ-QA-002 | Test kendisinin assert ettiği sonucu oyun kodunu çağırmadan kopyalayıp üretmemeli. Örneğin transfer testinde beklenen ownership uygulamanın private field'ını değiştirmekle hazırlanmaz; public contract kullanılır. | [22_QA_Acceptance_Traceability.md](22_QA_Acceptance_Traceability.md) | NOT_VERIFIED | NOT_RUN |
| REQ-QA-003 | Bir sistem DONE olmak için domain davranışı, data validation, kritik negatif akış, görünür feedback, save etkisi ve ilgili build doğrulamasını tamamlar. İlgisiz gate N/A yazılabilir; gerekçe olmalıdır. Render-only decal değişikliğinde save migration zorunlu tutulmaz. | [22_QA_Acceptance_Traceability.md](22_QA_Acceptance_Traceability.md) | NOT_VERIFIED | NOT_RUN |
| REQ-BAL-001 | Parametre adı birim ve zaman tabanı taşır. Bir değerin değişmesi save schema değişikliği mi yalnız content revision mı gerektirdiği belirtilir. Experimentler versioned profile ile yapılır. | [23_Balance_Katalog_ve_Ekonomi.md](23_Balance_Katalog_ve_Ekonomi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-BAL-002 | Aynı testte loot, düşman hasarı ve survival tüketimi aynı anda değiştirilmez; hangi değişkenin sonucu etkilediği izlenir. A/B playtest küçük ekipte sırayla yapılabilir; istatistiksel kesinlik iddiası gerekmez. | [23_Balance_Katalog_ve_Ekonomi.md](23_Balance_Katalog_ve_Ekonomi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-BAL-003 | Gıda/su/ammo kritik gereksinimleri economy simulation + insan koşusuyla kontrol edilir. 72 adayın hepsi aynı sıklıkta dağıtılmaz. İlk prototype 12 P item ile sınırlanabilir; koşu eğlenceli olmadan katalog büyütülmez. | [23_Balance_Katalog_ve_Ekonomi.md](23_Balance_Katalog_ve_Ekonomi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PROD-001 | Yeni iş paketi en az bir görünür sonuç veya kritik teknik riski azaltır. Aylar boyunca yalnız interface/event bus altyapısı üretmek kabul edilmez. | [24_Production_Roadmap_Backlog.md](24_Production_Roadmap_Backlog.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PROD-002 | Paket bitince “yüzde tamam” yerine tamamlanan davranış, kalan limitation ve kanıt listesi yazılır. Dosya sayısı veya yazılan satır gelişme metriği değildir. | [24_Production_Roadmap_Backlog.md](24_Production_Roadmap_Backlog.md) | NOT_VERIFIED | NOT_RUN |
| REQ-PROD-003 | Yeni özellik için kod+art+audio+UI+save+QA maliyeti birlikte hesaplanır. Sadece C# script süresiyle araç veya co-op kapsamı kabul edilmez. | [24_Production_Roadmap_Backlog.md](24_Production_Roadmap_Backlog.md) | NOT_VERIFIED | NOT_RUN |
| REQ-NET-001 | Single-player core'un çalışması ağ oturumu veya internet servisine bağımlı olamaz. “İleride lazım olur” gerekçesiyle bütün domain network serialization attribute'larıyla kaplanmaz. | [25_Coop_Gelecek_Mimarisi.md](25_Coop_Gelecek_Mimarisi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-NET-002 | Co-op deneyi single-player save ownership invariantlarını gevşetemez. Network duplicate testleri mevcut transfer sözleşmesine dayanır. | [25_Coop_Gelecek_Mimarisi.md](25_Coop_Gelecek_Mimarisi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-NET-003 | Co-op başarılı spike olmadan mağaza vaadi olamaz. Failed spike sonrası solo proje güvenli branch/content contract ile devam edebilmelidir. Bu doküman co-op kurulum talimatı veya uygulanmış entegrasyon raporu değildir. | [25_Coop_Gelecek_Mimarisi.md](25_Coop_Gelecek_Mimarisi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-KB-001 | Bir notebook aynı belge kimliğinin birden fazla yürürlükteki sürümünü aktif kaynak gibi tutmamalıdır. Tarihî karşılaştırma gerektiğinde sürüm filtresi açıkça sorulur. | [26_NotebookLM_Kaynak_Yonetimi.md](26_NotebookLM_Kaynak_Yonetimi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-KB-002 | Kaynak senkron kontrolü yalnız başlığa bakmaz: değişen bir requirement ID ve yeni değer sorgulanır. Eski cevap görünüyorsa uygulama görevi eski veriye dayanarak başlamaz; doğrudan repo belgesi okunur. | [26_NotebookLM_Kaynak_Yonetimi.md](26_NotebookLM_Kaynak_Yonetimi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-KB-003 | Notebook şu sorularda doğru ayrımı yapmalı: “Co-op bugün gerekiyor mu?” hayır; “Unity testleri geçti mi?” kanıt yok; “Forward+ kesin üstün mü?” deney; “Save ne zaman başlar?” W05; “Aynı eşya iki kez alınabilir mi?” invariant gereği hayır; “Bu kod yazıldı mı?” repo incelenmedi. Beş doğru cevap bağlantının her konuda doğru olduğu garantisi değildir; yalnız başlangıç smoke testidir. | [26_NotebookLM_Kaynak_Yonetimi.md](26_NotebookLM_Kaynak_Yonetimi.md) | NOT_VERIFIED | NOT_RUN |
| REQ-AGENT-001 | Tool listesini göremeyen veya doğru kaynak sürümünü doğrulayamayan ajan “bağlantı tamamlandı” diyemez. Sorun halinde mevcut yerel Markdown ile çalışma sürdürülür; kurulu olmayan araç adları çağrılmış gibi raporlanmaz. | [27_Codex_MCP_Runbook.md](27_Codex_MCP_Runbook.md) | NOT_VERIFIED | NOT_RUN |
| REQ-AGENT-002 | Her görev önce uygulamanın mevcut hâlini inceler. Sadece dokümandaki class isimlerini görmek o sınıfların repo'da bulunduğu anlamına gelmez. Aynı iş için paralel ikinci Inventory sistemi oluşturulmaz. | [27_Codex_MCP_Runbook.md](27_Codex_MCP_Runbook.md) | NOT_VERIFIED | NOT_RUN |
| REQ-AGENT-003 | Kullanıcı sadece W05'i istediyse ajan W10 firearm veya multiplayer paketini eklemez. Küçük gerekli yardımcı düzenlemeler kapsam içinde açıklanır; kapsamı büyüten ürün değişiklikleri ayrı önerilir. | [27_Codex_MCP_Runbook.md](27_Codex_MCP_Runbook.md) | NOT_VERIFIED | NOT_RUN |

## 3. İş paketi ve kabul ilişkisi

| İş grubu | Gereksinim önekleri | Kanıt |
|---|---|---|
| W00-W01 | GOV, ARC, DATA | Environment, lifecycle, validation |
| W02-W05 | MOVE, INT, INV, SAVE | Movement parkur, transactions, ilk roundtrip |
| W06-W10 | AI, CMB, SURV, SLICE | Encounter, wounds, ammo, micro-loop |
| W11-W16 | WORLD, PRS, TIME, SHL, CRF, OBJ | Streaming, pressure, sleep, craft, relay |
| W17-W23 | ART, UX, PERF, QA, BAL | Benchmark, platform, oyuncu testi |
| Dokümantasyon/tooling | KB, AGENT, PROD | Kaynak sürümü, görev kanıtı, kapsam |
| Gelecek | NET | Co-op kararı verilene kadar DEFERRED plan |

REQ-NET hükümleri bugünkü kapsam sınırları için dikkate alınır; networking implementation gerektirmez. Belge/iş paketi ilişkisi her iş grubunun bütün testlerini her gün çalıştırmak anlamına gelmez. Değişen risk alanı ve milestone gate'i belirleyicidir.

## 4. GDD kaynak kapsamı

| GDD bölümleri | Yeni dosyalar | Ayrıntı |
|---|---|---|
| Kontrol, 4, 28 | 01,24,28 | Karar durumu, scope, değişiklik |
| 1-6 | 02,16,21 | Vizyon, dünya, tam döngü |
| 7 | 05,06 | Input ve interaction |
| 8-9 | 09,15 | Stats, wounds, death, clocks |
| 10-11 | 04,07,08,23 | Items, economy, katalog |
| 12 | 10 | Combat ve weapon state |
| 13-14 | 11,12 | Hearing, AI, pressure, population |
| 15 | 13 | POI ve streaming |
| 16-17 | 14,16 | Shelter, crafting, objectives |
| 18 | 14,15 | Power, weather, time |
| 19-20 | 18,19 | UX, sanat, ses |
| 21-23 | 03,04,17,20,25 | Mimari, save, co-op |
| 24-27 | 20,21,22,24 | Playtest, kalite, plan, risk |
| Ek A-D | 04,21,23,29 | Slice, veri, katalog, sözlük |
| Kullanıcının yeni talebi | 26,27 | NotebookLM/Codex/MCP akışı |

## 5. Başlangıç durum kaydı

Unity repository: incelenmedi. Editor oturumu: bağlanılmadı. Gerçek test sayısı: bu görevde yok. Windows/macOS oyun buildi: bu görevde alınmadı. NotebookLM yüklemesi: yapılmadı. MCP kurulumu: yapılmadı. Tasarım dokümantasyonu: bu sürümde oluşturuldu. İleride bu paragraf yerine gerçek Current_State handoff bağlantısı eklenmelidir.
