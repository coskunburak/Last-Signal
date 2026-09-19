---
doc_id: LS-DOC-01
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Karar yönetimi, kapsam ve kaynak hiyerarşisi

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: Belge Kontrolü; §4, §23, §25, §28. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Bu paketin yetkisi

Bu paket GDD v0.1'i uygulama seviyesinde açar. Oyun reposu, Unity projesi, gerçek paket manifesti ve MacBook bellek kapasitesi incelenmemiştir. Dolayısıyla IMPLEMENTED/PASS etiketi hiçbir sisteme verilmez. Önceki On Hold projesindeki test sayıları bu survival projesine taşınmaz. Dokümantasyon üretmek yeni Unity projesi kurulduğunu göstermez.

**REQ-GOV-001:** Her görev kaynak belge kimliği, gereksinim kimliği, uygulama durumu ve test kanıtını ayrı taşır. “Tasarımda var” ile “oyunda çalışıyor” aynı sütunda tutulamaz.

Kaynak sırası: kullanıcının güncel açık talimatı; kabul edilmiş proje kararları; yürürlükteki sistem sözleşmeleri; tarihi GDD; araştırma ve öneriler. Kod mevcut davranışı gösterir, istenen davranışı tek başına belirlemez. NotebookLM cevabı bir açıklamadır, yeni karar değildir. Çelişki varsa iki kaynak alıntılanır ve hangi varsayımla ilerlenebildiği yazılır.

## 2. Durum sözlüğü

| Durum | Anlam | Uygulama davranışı |
|---|---|---|
| CONFIRMED | Kullanıcı açıkça belirtti | Temel kısıt olarak uygula |
| BASELINE | GDD'den alınan çalışma tercihi | Mevcut görevle uyumluysa kullan; kullanıcı onayı gibi anlatma |
| PROPOSED | Bu pakette eklenen ayrıntı | Küçük geri alınabilir işlerde varsayımı kaydederek ilerle |
| EXPERIMENT | Eğlence/performans için ölçülecek hipotez | Önce küçük prototip, sonra karar |
| DEFERRED | Gelecek aşama | Şimdi implementation üretme |
| OUT | Tanımlı kapsamın dışında | İlgili görevde ekleme |
| OPEN | Kapatılması gereken belirsizlik | Sahip, son karar kapısı ve geçici tercih belirt |

MUST sözleşmenin normatif kuralıdır; CONFIRMED anlamına gelmez. Kod örnekleri API önerisidir. Bütün sayısal denge değerleri aksi açıkça yazılmadıkça başlangıç hipotezidir.

## 3. Temel karar kayıtları

| ADR | Konu | Durum | Gerekçe / yeniden değerlendirme |
|---|---|---|---|
| ADR-001 | Önce single-player; co-op opsiyonel | CONFIRMED | Kullanıcı talebi; co-op bir yayın sözü değildir |
| ADR-002 | Unity 6 + URP | BASELINE | Görsellerde 6000.5.0f1 var; gerçek ProjectVersion/manifest ilk görevde okunmalı |
| ADR-003 | Forward+ | EXPERIMENT | GDD kilitli yazmıştı; hedef sahnede Forward ile CPU/GPU ölçülmeden teknik üstünlük kesinleşmez |
| ADR-004 | First-person | BASELINE / D-001 OPEN | Hareket prototipi kapanışında teyit; üçüncü şahıs maliyeti ayrı |
| ADR-005 | Slot + weight inventory | BASELINE | Tetris grid kapsam maliyetini azaltır; UX testine açık |
| ADR-006 | World Pressure + Shelter Network | EXPERIMENT | GDD signature önerileri; özgünlük ve eğlence henüz ölçülmedi |
| ADR-007 | Save altyapısını erkene al | PROPOSED | ID, transaction ve snapshot item geliştirmesiyle aynı anda gerekir |
| ADR-008 | Kalıcı kimlik ile runtime/network kimliğini ayır | PROPOSED | Streaming ve gelecekte networking aynı kimlik ömrünü paylaşmaz |
| ADR-009 | Araç, NPC fraksiyon, serbest inşa ilk slice dışında | BASELINE | Önce tamamlanabilir tek rota |
| ADR-010 | Dosyalardaki v0.2 numaralarını tek balance tablosunda yönet | PROPOSED | Denge drift'ini önle |
| ADR-011 | Tek oyunculu simülasyonda network server başlatma | PROPOSED | Local authority, socket veya host zorunluluğu değildir |
| ADR-012 | NotebookLM bilgi erişimi; repo Markdown asıl kaynak | PROPOSED | Sürüm ve değişiklik izi korunur |

## 4. GDD'den açıklığa kavuşturulan farklılıklar

GDD §25'te save işi S076'ya kadar görünür biçimde gecikiyordu. Bu paketin W iş paketleri persistent ID ve küçük save/load'u ilk dolaşılabilir sahneye getirir; eski sprintler yapılmış veya iptal edilmiş sayılmaz. W kodları yeni plan önerisidir.

GDD §4 co-op'u 1.0 dışında tutarken §23 slice sonrasında prototipe izin verir. Bir çelişki olmak zorunda değildir: prototip kararı ile piyasaya çıkacak mod ayrı kararlardır. Varsayılan hâlâ single-player üründür.

GDD §9 zombi enfeksiyonunu yüksek/fatal risk diye açık bırakır. Bu pakette slice için wound infection uygulanır; ölümcül zombi enfeksiyonu D-002 kapanmadan aktif olmaz. Bu, erken oyunda açıklanamayan koşu sonlarını azaltmak için PROPOSED kapsam daraltmasıdır.

GDD §4 ve §25 içerik tablolarında slice silahları farklıdır. Burada ilk prototipte crowbar+pistol; polished slice'ta üç melee ve iki firearm hedefi esas alınır. 60-80 item hedefi microprototype şartı değildir.

GDD'nin 20-30+ ay notu doğrulanmış tahmin değildir. Faz süreleri toplanınca yaklaşık 21-32 ay eder; kapsam, çalışma saati ve asset hazırlığı bilinmeden teslim tarihi verilemez. Üretim dosyası ölçülen haftalık kapasiteyi kullanır.

## 5. Değişiklik işlemi

**REQ-GOV-002:** Davranış değişikliğinde etkilenen requirement, balance key, save schema, test ve içerik kaydı birlikte gözden geçirilir. Sadece sohbet mesajında kalan karar yürürlükte sayılmaz.

Karar kaydı: ID, problem, seçenekler, seçilen çalışma varsayımı, durum, sahip, tarih, etkilenen dosyalar, geri dönüş maliyeti, doğrulama yöntemi. Düşük etkili düzeltme için bürokratik onay kuyruğu kurulmaz. Kapsam, veri kaybı, ticari yayın veya büyük tasarım yönü değişirse Burak'ın kararı gerekir.

**REQ-GOV-003:** Onaylanmamış öneri otomatik olarak CONFIRMED'a yükseltilmez. Bir ajan “GDD böyle diyor” gerekçesiyle kullanıcının son isteğini geçersiz kılamaz.

## 6. Açık kararlar ve kapanışları

| ID | Belirsizlik | Geçici tercih | En geç |
|---|---|---|---|
| D-001 | Kamera | First-person | W02 |
| D-002 | Isırık kalıcı ölüm doğurur mu? | Slice'ta hayır | W08 playtest |
| D-004 | Ölümden dönüş ve çanta | Sığınakta recovery; no-shelter fallback | W08 |
| D-009 | Dünya alanı | Slice 400×400 m; 1.0 yaklaşık 4 km² üst hedef | M2 sonrası |
| D-010 | Ticari isim | Last Signal yalnız kod adı | Store hazırlığı |
| D-011 | M3 Pro RAM / Windows test makinesi | Ölçülmedi | W00 |
| D-012 | NotebookLM MCP sağlayıcısı | Seçilmedi, bağlı değil | Entegrasyon görevi |
| D-013 | UI teknolojisi | Mevcut repo varsa onu koru; yeni projede uGUI spike | W04 |

## 7. Kabul

Verilen bir kuralın CONFIRMED mı öneri mi olduğu tek dosyadan bulunabilmeli. Bir tester “kaç test geçti?” sorusuna bu paket üzerinden sayı uydurmamalı. Paket sürümü güncellendiğinde eski GDD korunmalı ve sapmalar bu kayıtta görünmelidir.
