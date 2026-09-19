---
doc_id: LS-DOC-17
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Kayıt, yükleme, snapshot ve migration tasarımı

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §22; Ek B. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. En kritik ürün sözleşmesi

Bir survival oyununda saatlerce ilerleme save'e emanet edilir. Save sistemi sonradan eklenen scene dump değildir. İlk pickup+door prototipiyle birlikte kalıcı ID, ownership ve snapshot denenir. Schema formatı başlangıçta okunabilir JSON olabilir; performans ölçülmeden binary format veya özel serializer yazılmaz.

**REQ-SAVE-001:** Save yüklenmiş GameObject listesinin toplamı olamaz. Unloaded cell deltalari, player, jobs, objectives, pressure, population ve tombstone state'i aynı world generation'a aittir.

## 2. Header ve bölüm modeli

| Bölüm | İçerik |
|---|---|
| Header | SchemaVersion, contentVersion, buildId, worldId, seed, generation, timestamp, checksum |
| Profile | Difficulty snapshot, death rule, world time rate |
| Player | Region/cell/local transform, stats, wounds, containers, equipment |
| World | Clock, weather, phase, event instances, RNG streams |
| Cells | Baseline content reference, entity deltas, pressure, population |
| Shelters | Modules, storage, fuel, reservations/jobs |
| Progression | Objective states, knowledge, reward receipts |

Settings key bindings save world'ın parçası değildir; ayrı user preferences. Wall timestamp kullanıcıya tarih göstermek içindir, simülasyon tüketimi için değil.

## 3. Tutarlı snapshot

Simulation belirli sequence N'de snapshot bariyerine gelir. Açık kısa transactionlar tamamlanır veya ertelenir. Immutable DTO generation N oluşturulur. Sonraki frame mutationları N+1 dirty store'a yazılır. Worker yalnız N DTO'larını serialize eder. Snapshot tek frame'de pahalıysa copy-on-write/chunk revision yaklaşımı değerlendirilir; canlı koleksiyonun korumasız kopyası kabul edilmez.

**REQ-SAVE-002:** Item transfer sırasında kaynak/target ve world tombstone aynı sequence'te görünür. Save'in bir yarısı “önce”, diğer yarısı “sonra” olamaz.

## 4. Disk yazma protokolü

Yeni generation kendi geçici dizinine yazılır. Bölüm dosyalarının uzunluğu ve checksum'u hesaplanır; manifest tümünü listeler. Dosyalar kapatılıp platformun desteklediği flush/atomic publish davranışı doğrulanır. Tam generation hazır olduktan sonra current pointer/manifest güncellenir. Önceki geçerli generation backup olarak korunur.

Birden fazla cell dosyasını ayrı ayrı replace etmek global atomicity sağlamaz; generation manifest bu nedenle gereklidir. Aynı filesystem üzerindeki rename bile power-loss dayanıklılığı için tek başına yeterli varsayılmaz; Windows/macOS üzerinde fault injection yapılır. Temp dosyanın varlığı save completed demek değildir.

Checksum bozulmayı tespit içindir; hile koruması veya şifreleme iddiası değildir. Lokal tek oyunculu save'i oyuncudan saklamak ürün gereksinimi değildir.

## 5. Failure recovery

Disk full/permission error: eski save korunur, kullanıcıya “kayıt alınamadı” gösterilir. Current generation bozuksa backup validation denenir; kullanıcıya hangi tarihe dönüleceği söylenir. Load başarısızken autosave devre dışı kalır; boş dünya eski kaydın üstüne yazılmaz.

**REQ-SAVE-003:** Hatalı load orijinal dosyayı değiştiremez. Recovery denemeleri kopya/generation üzerinde yapılır. Başarısız migration sonrası eski sürümle açma olanağı korunur.

Quit sırasında bekleyen save için progress gösterilir; sonsuz bekleme yok. Force quit crash recovery guarantee'si vermez. Normal quit ancak persistent write success ile başarı bildirir; başarısızsa kullanıcıya tekrar deneme veya kaydetmeden çıkma seçeneği sunar.

## 6. Load sırası

Header/limits/checksum → schema migrate → catalog compatibility → DTO structural validation → world/player domain restore → target cell load → entity hydration → nav/collision ready → safe spawn → event subscription → UI → input enable. Restore sırasında eski eventler tekrar gameplay komutu olarak yayımlanmaz. UI initial snapshot okur.

Missing definition critical item ise load controlled error; cosmetic prop ise belgelenmiş fallback mümkün. Unknown field ileri uyumluluk için korunabilir ama future schema daha eski uygulamada sessizce kabul edilmez.

## 7. Migration

V1→V2→V3 deterministic zincir; fixture örnekleri source control'de. Definition rename alias; removed item replacement mapping; moved entity redirect; split/merged cells için ID korunumu. Migration world clock/ownership/reward receipts invariantlarını yeniden doğrular. Yeni schema numarası sırf balance değişti diye artmak zorunda değildir; content version ayrıdır.

Örnek: V1 weapon ammo tek sayı; V2 magazine/chamber. Migration açık kuralla ammoPool'u magazine'a aktarır, chamber=0 yapar ve maximum capacity'yi aşanı inventory/recovery container'a koyar. Fazla ammo silinmez. Bu örnek uygulanmış migration kodu değildir.

## 8. Checkpoint politikası

Shelter sleep, major objective, kontrollü region geçişi, normal quit. Manual save default güvenli koşullarda; suspend save gerekirse ayrı profile kuralıdır. Autosave aralığı performans ve kayıp toleransıyla belirlenir. Screenshot thumbnail failure ana save başarısını bozamaz.

## 9. Fault injection kabulü

**REQ-SAVE-004:** Serialize öncesi, temp yazma ortası, manifest tamamlandıktan önce/sonra, current pointer güncellemesi ve backup rotation noktalarında process interruption denenir. Her durumda en az önceki geçerli generation yüklenebilmelidir. Coverage yüzdesi yerine bu kesinti noktaları ayrı raporlanır.

Roundtrip: inventory unique IDs, kapı, empty generated loot, broken glass, dead zombie, dropped bag, craft escrow, fuel, objective reward ve migration ETA. 50 save/load + streaming döngüsünde semantic state aynı kalmalı; transient animation frame eşitliği aranmaz. Testler yapılmadığı için bu pakette PASS yoktur.
