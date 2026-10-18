# Oyuncu vaadi ve ölçülebilir hikâyeler

BLD-D002 · BLD-P00-DEC-002 · PROPOSED, ADR-001'e bağlı · IMPLEMENTED (belge) / TEST_NOT_RUN.
Girdi: [ADR](BLD_S001_PRODUCT_SCOPE_CHANGE.md), LS-DOC-02 §1/3/7, LS-DOC-14 §1/3; özgün kart [S001 handoff](BLD_S001_HANDOFF.md) içinde bağlıdır. Karar sahibi Burak; uygulama P01–P04, genişleme P05/P06. Alternatif yalnız menüden otomatik üs yükseltme reddedildi: kaynak seferinin dünyada görünür sonucunu vermez. Geri dönüş: inşa isteğe bağlı kalır, loot/claim rotası korunur.

## Vaat

“Bir sefere hazırlanır, taşıyabileceğim kadar kaynak toplar, sığınağıma döner ve seçtiğim güvenli yerde kalıcı bir iyileştirme yaparım. Neden yapamadığımı anlarım; iptal veya yeniden yükleme malzememi yok etmez.” İnşa, suya, ana göreve veya ilk geceye erişmenin zorunlu anahtarı değildir. Crafting'in gerekçesi daha güvenilir depolama, daha planlı dönüş ve riskli seferlerden görünür fayda almaktır.

## Hikâye sözleşmeleri

Tablodaki sayaçlar gelecekte owner-run kabul için hedeflerdir; oynanışta ölçülmedi. Authority isimleri önerilen domainlerdir. Her kabulde beklenen receipt, inventory/node/structure revision ve önce/sonra miktarları kaydedilir.

| ID / tetik | Birinci kişi eylemim | Authority | Feedback | Başarısızlık / iptal | Ödül | Ölçülebilir kabul |
|---|---|---|---|---|---|---|
| US-01 / kaynak hedefi seçtim | Garantili authored alet kaynağından baltayı edinir, uygun ağacı seçerim | Inventory pickup; Gathering uygunluk | Alet sınıfı, erişim ve hasat simgesi | Alet yoksa ToolRequired; alet kaynağına dönüş veya loot rotası | Yeni kaynak erişimi | Aynı alet pickup ID'si iki kez vermez; RNG başarısızlığı ana rotayı kapatmaz |
| US-02 / erişimde uygun ağaç | Baltayla vurur, tükenene kadar hasat ederim | Gathering node revision; Inventory yalnız pickup'ta | Vuruş, dayanım/yield feedback'i; gameplay noise receipt | Menzil/alet/stamina uygun değilse ret; ara vermek ekstra yield vermez | Sonlu odun pickup'ları | Bir node generation'ın toplam yield'i katalog bütçesini aşmaz; son vuruş tekrarı 0 ek yield |
| US-03 / odun yerde | Boş yerimi ve yükümü görür, odunu alır ve stoklarım | Inventory transfer | Alınan/kalan adet ve ağırlık | Kapasite doluysa kabul edilen miktar açık; kalan world owner'da | Taşınabilir materyal | Alınan + yerde kalan = önceki miktar; encumbrance mevcut sözleşmeyi izler |
| US-04 / sığınağa döndüm | Tezgâhta odunu işler, Craft Defteri'nde maliyeti ve çıktıyı incelerim | Crafting; legacy işler ShelterProduction | Tarif, timeBasis, gereken istasyon, iş/refund durumu | Girdi/istasyon eksikse başlangıç ret; iptal tüketilmemiş escrow'u bekletir/iade eder | Kullanılabilir yapı materyali | Bir job bir çıktı; output full iken CompletedWaitingOutput; iptal+collect dupe üretmez |
| US-05 / materyal hazır | Defterden çit seçer, preview'u döndürür, uygun yerde onaylarım | Construction validator/coordinator; Inventory reservation | Geçerli ikon+metin ve renk; commit sonrası receipt | Preview ücretsiz iptal; stale stokta SourceChanged, tekrar göster | Dönüş yaklaşımını düzenleyen kalıcı çit | Bir nonce ile tam bir instance, tam bir maliyet; preview hiçbir quantity/revision değiştirmez |
| US-06 / maliyetim yetersiz | Eksik adetleri görür, toplama hedefi seçerim | Inventory query + Construction | MissingMaterials ve kalem başına eksik miktar | Onayda ret; zorunlu grind görevi açılmaz | Sonraki sefer için net amaç | Başarısız commit'te inventory delta 0, instance delta 0 |
| US-07 / dar kapı veya yol | Ghost'u döndürür ya da boş alana taşırım | Construction + cell clearance | Overlap veya PathBlocked; hangi açıklık engellendi | Hiçbir uygun alan yoksa iptal | Güvenli giriş/çıkış korunur | Kapı sweep veya acil erişimi kesen her aday ret; başka adayda yeniden query |
| US-08 / dik veya su içindeki zemin | Daha düz izinli yüzey ararım | Construction surface policy | Slope veya UnsupportedSurface metni | Kazı/terrain düzleştirme seçeneği yok; iptal | Açık, öğrenilebilir konum kuralı | Eşik üstü/eşit/altı ayrı fixture; reddedilen aday delta 0 |
| US-09 / düşman yaklaşır | Defteri/ghost'u kapatıp kaçışa ve savunmaya dönerim | Presentation lifecycle; actor authority | Seçim kapanır, gameplay input geri gelir | Ölüm/disable sırasında yeni commit kabul edilmez; sonuç belirsizse nonce sorgulanır | Hayatta kalma seçimi | Preview collider/nav/AI nesnesi bırakmaz; önceden durable commit edilmiş yapı geri alınmaz |
| US-10 / yerleştirdim ve geri döndüm | Save alır, sahneden ayrılır, yükleyip aynı yapıyı görürüm | Persistence + WorldCells + Construction registry | Save başarı/ret durumu, hydration tamamlanınca görünür yapı | Save busy ise başarı sözü yok; bozuk son generation'da recovery bildirimi | Dünyanın yaptığı işi hatırlaması | 3 unload/load döngüsünde 1 instance ID; maliyet bir kez; depleted node yeniden yield vermez |
| US-11 / seçimimden vazgeçtim | Preview'u iptal eder veya çalışan işi durdururum | Presentation (preview), Crafting (job) | Preview kapandı / refund bekliyor ayrımı | Dolu stokta refund kaybolmaz; tekrar collect ayrı komut | Kaynakları koruyarak yön değiştirme | Preview iptal maliyeti 0; escrow refund yalnız bir kez; completion sonrası iptal ikinci çıktı vermez |
| US-12 / inşa istemiyorum | Claim edilmiş mevcut sığınaktan klasik loot seferine çıkar, su/yiyecek ve görev kaynağıyla dönerim | Mevcut survival/loot/quest | Mevcut sefer ödülleri ve recovery | Yeni yapı gerektiren görev kapısı konamaz; alternatif garantili kritik kaynak yolu | Ana survival ve progression | Hiç BuildConfirm göndermeden ilk gece recovery ve ana rota bir sonraki hedefi erişilebilir |

Defter, zamanın durduğu varsayımına dayanmaz. Preview ve yeni defter önerisi canlı gameplay içinde çalışır; mevcut S010 modal/pause davranışı aynen kalır. Pause/restore sırasında authoritative command kabulü ayrıca [transaction sözleşmesindedir](CONSTRUCTION_TRANSACTION_SEQUENCE.md). Renk tek ret göstergesi olamaz; klavye/gamepad eşlemesi sonraki input tasarımının konusu, P00 yeni binding yaratmaz.

## Kabul izi

INPUT → LS-DOC-02 sefer döngüsü ve D002. DECISION → isteğe bağlı inşa, görünür kaynak dönüşümü, açık ret. DELIVERABLE → US-01–12. ACCEPTANCE → owner her satırda tetik/eylem/sahip/feedback/iptal/ödül/ölçüt bulunduğunu inceler; gelecekte [QA](QA_AND_FAULT_MATRIX.md) senaryoları çalıştırılır. Şu anda oyun davranışı uygulanmış veya kabul edilmiş değildir.
