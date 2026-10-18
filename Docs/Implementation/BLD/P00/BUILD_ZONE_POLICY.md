# Controlled build zone ve ret sözleşmesi

BLD-D003 · BLD-P00-DEC-003 · PROPOSED · Belge IMPLEMENTED / TEST_NOT_RUN.
Girdi: D003, [ADR-001](BLD_S001_PRODUCT_SCOPE_CHANGE.md), LS-DOC-14 §2. Sahip: Burak (ürün), gelecek Construction/WorldCells uygulaması (teknik). Uygulama: BLD-S007–009; iki-cell genişlemesi en geç BLD-S021. Alternatif dünya çapında yerleştirme reddedildi; revert authored sockets. Kaynak kart bağlantısı [handoff](BLD_S001_HANDOFF.md) içindedir.

## Coğrafya ve izin

İlk alan yalnız FirstTownCore'da claim edilmiş mevcut sığınağa bağlı, authored bir BuildZone'dur. Claim çevresinde otomatik yarıçap açılmaz. Zone polygon/volume, origin/rotation, izinli yüzey ve kategori, slope/clearance/instance limitleri, protected corridor volumes ve policyRevision olarak versionlanır. Shelter ID ile bağlantı zorunludur; bütün footprint ve güvenlik clearance'ı izinli alan içinde kalır. Bir noktanın zone içinde olması yeterli değildir. Komşu birden fazla zone'u birleştirerek izin genişletilmez.

Gerçek koordinat, ölçek, slope eşiği, collider toleransı, kapı açıklığı, yol genişliği: OPEN / NOT_INSPECTED. Bu phase'te sahne açılmadı. BLD-S007 öncesi owner staged scene üzerinde player capsule, kapı sweep'i, zombie corridor ve olası araç/spawn rezerviyle authoring ölçümü yapar; policy revision'a bağlar. Eksik policy fail-closed: ZoneNotConfigured. P00 kararını vermek için koordinat uydurulmaz.

İstisnalar: yollar ve POI etkileşim/loot alanları; acil kaçış; player ve varsa gelecekteki araç spawn rezervleri; kapı açılma hacmi; zombie geçiş/nav koridorları; su ve world bounds; başka oyuncu/yapı collider'ı. Claim bunları geçersiz kılamaz, zombileri silmez. Araç rezervi bir authoring exclusion'dır, araç runtime sözü değildir. Zone dışı veya komşu cell hazır değilse “görünürde boş” kabul edilmez.

## Ortak query/result şeması (öneri)

`PlacementQuery`: worldId, zoneId/policyRevision, definitionId/revision, finite pose, actorId, expected source revisions, expected cell epochs, optional parentInstanceId/socketId + parent revision. `PlacementResult`: allowed, primaryReason, ordered reasons[], messageKey, messageArgs, validatorRevision, observed revisions, affected cell IDs. Query resource ayırmaz; onay aynı validator ile güncel authoritative veriyi tekrar okur. Stale preview bir izin belgesi değildir. İki yolda aynı koşul aynı enum/messageKey üretir; commit'te sonradan oluşan koşullar sonucu değiştirebilir.

| Öncelik / enum | Ret koşulu | Oyuncu metni / toparlanma |
|---|---|---|
| 1 InvalidRequest / DefinitionMismatch | NaN/Infinity, bozuk ID/pose/revision | “Seçim geçersiz; yapıyı yeniden seç.” |
| 2 ActorUnavailable / BusyCell / SaveBusy | Ölüm/pause/restore, unload/readiness veya mutation barrier | “İşlem şu anda kullanılamıyor / alan hazırlanıyor”; state düzelince yeni query |
| 3 ZoneNotConfigured / NotClaimed / OutOfZone | Eksik policy, claim yok veya footprint sınır dışı | “Bu alan inşaya açık değil / önce sığınağı sahiplen / alan içine taşı.” |
| 4 UnsupportedSurface / Slope | Su/bounds/yüzey sınıfı veya slope eşiği | “Bu yüzeye kurulamaz / daha düz bir yer seç.” |
| 5 UnsupportedSnap | Uyuşmayan, dolu, eksik veya döngü yaratan bağlantı | “Bu bağlantı uygun değil.” |
| 6 PathBlocked | Kapı, yol, spawn, nav/critical access exclusion | “Geçiş alanını açık bırak.” |
| 7 Overlap | Footprint + clearance yetkisiz collider/structure ile çakışır | “Başka bir nesneyle çakışıyor.” |
| 8 LimitReached | Zone/world instance budget dolu | “Bu alanın yapı sınırına ulaşıldı.” |
| 9 SourceChanged / MissingMaterials | Kaynak revision farklı / güncel maliyet yetersiz | “Malzemeler değişti / gereken: {item}, eksik: {count}.” |
| 10 Allowed | Yukarıdaki retlerden hiçbiri yok | “Yerleştirilebilir”; renk + ikon + metin |

ToolRequired ve CapacityFull gathering/crafting sonuçlarıdır; placement tool şartı varsa validator aynı enum ailesinde ToolRequired döndürür. NonceConflict/PersistenceFailed transaction sonuçlarıdır; preview geometrisi ret kodu gibi sunulmaz. Ret listesi deterministik öncelik ve ID sırasındadır. DEBUG ayrıntısı oyuncu mesajına dökülmez.

## Geometri ve navigation invariantları

Preview render-only, transient, save dışıdır; collider, loot, AI, physics impulse veya nav registration yapmaz. Rotation footprint'i yeniden hesaplatır. Commit sırasında stock, zone, overlap, slope, snap, adjacency ve access tekrar değerlendirilir. Dynamic nav update algoritması/performance'u OPEN'dır; ilk çözüm authored corridor exclusion + konservatif clearance. Runtime nav gerekirse başarılı staging/readiness sağlanmadan commit olmaz. Ghost'un yeşil olması nav kanıtı değildir.

İlk playable'da iki-cell footprint `UnsupportedCellSpan` ile reddedilir; sonraki genişleme [tek-owner cell politikası](PERSISTENCE_CELL_AND_ID_POLICY.md) ve lease fixture'larıyla açılabilir. Bu ek enum aynı preview/commit validator'ındadır; geçici fallback iki hücreye kopya kayıt olamaz.

## Kabul

Owner US-05/07/08/09'u bu tabloyla karşılaştırır: sınırın hemen içi/dışı, slope eşitliği, dönen dar footprint, kapı ve POI exclusion, eksik claim, unload sırasında commit. Beklenti: geçerli durumda tek receipt; ret durumunda kaynak ve yapı delta 0, erişilebilir mesaj. Mevcut sahnede yer ölçümü bu belgeyle yapılmış sayılmaz. Policy OPEN değerleri BLD-S007 için blokerdır, P00 belge tamamlanmasını engellemez.
