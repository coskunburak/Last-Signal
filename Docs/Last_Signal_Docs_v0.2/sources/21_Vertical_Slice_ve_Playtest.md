---
doc_id: LS-DOC-21
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Vertical slice, golden path ve oyuncu test protokolü

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §6, §24, §26; Ek A. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Tek ölçülebilir amaç

Yeni oyuncu küçük alanda hazırlanma → keşif → loot → tehdit → dönüş → yükseltme → save/load döngüsünü tamamlar. Slice bir teknoloji vitrini değildir. Bu döngünün ilk çıplak sürümü 12 item, bir zombi ve iki yapı ile yapılabilir; 60-80 item polished slice'a doğru eklenir.

**REQ-SLICE-001:** “Vertical slice tamam” demek için gerçek buildde kesintisiz 30-45 dakikalık rota ve save/quit/load tekrarı kanıtlanır. Editor'de ayrı sahnelerde çalışan bileşenler yeterli değildir.

## 2. Alan ve rotalar

400×400 m grid, cabin merkezli başlangıç. Kuzeyde benzinlik, doğuda küçük market, batıda orman kısa yolu, yüksekte relay overlook. Yol hızlı/açık; orman yavaş/kısmen görüş kapalı. Spawn'dan ilk suya 1-3 dakika, cabin'e 5-8 dakika, cabin-benzinlik arası riskli yürüyüş 3-5 dakika hedeflenir. Mesafeler hareket hızıyla gerçek sahnede ölçülür; harita ölçeğiyle çelişirse rota dolambaçlılığı yapayca artırılmaz.

## 3. Golden path

| Süre | Oyuncu eylemi | Gözlenen sistem | Kritik başarısızlık |
|---|---|---|---|
| 0-3 | Uyan, hareket et, su al | Input/interact | Prompt görünmüyor |
| 3-7 | Bag tak, ilk loadout | Inventory/equipment | Eşya kayboluyor |
| 7-12 | Cabin'e iki yoldan yaklaş | Stealth/hearing | Kaçınılamaz saldırı |
| 12-16 | Shambler ile karşılaş | Melee/stamina | Hit feedback tutarsız |
| 16-20 | Yarayı tedavi et | Wound/item commit | Tedavi nedeni anlaşılmıyor |
| 20-24 | Radio clue bul | Objective/map | Görev sırası kilitleniyor |
| 24-29 | Fuse al, ses bedelini yaşa | Gun/noise/population | Görüşte horde doğuyor |
| 29-33 | Yük altında dön | Encumbrance/weather | Tam yükte çıkış imkânsız |
| 33-35 | Shelter upgrade, save/load | Craft/persistence | Dünya sıfırlanıyor |

Yara almak şart koşulmaz; hasarsız oyuncuya sahte damage verilmez. Sağlık sistemini doğrulamak için test varyantında kontrollü yara fixture'ı kullanılabilir. Playtest ile teknik acceptance ayrı oturumlar olabilir.

## 4. Alternatif rotalar ve failure

A: Crowbar ile ön kapı, yüksek ses ve net loot. B: Arka kilit anahtarını bul, daha uzun süre ama az combat. C: Taş/distraction sonra kısa pickup-run. Bu üçüncü seçenek distraction implemented değilse ilk slice'a test şartı olarak eklenmez; backlog'da EXPERIMENT olur.

Ölüm varyantı ilk shelter öncesi, shelter sonrası, fuse taşırken ve ikinci death bag varken çalıştırılır. Save varyantı benzinlik emptied, broken door, incoming group, partially loaded weapon durumlarını kapsar. Ana oturumda oyuncuya zorla bütün failure'lar yaptırılmaz; QA kontrollü koşular kullanır.

## 5. Pilot test tasarımı

5-8 hedef oyuncuyla ilk tur; oranlar istatistiksel pazar kanıtı değildir. Ön görüşmede survival/FPS deneyimi ve kontrol cihazı kaydedilir. Geliştirici yönlendirmesi olmadan 30-45 dakika, ardından 10 dakika görüşme. İlk koşuda tutorial açıklaması için araya girilmez; gerçek blocker olursa zamanı ve yardım kaydedilir.

Observer verisi: ilk hedefini söylediği zaman, inventory'de durma süresi, bırakılan eşya, ilk threat detection, ölüm/hasar sebebini anlama, geri dönüş kararı. Think-aloud bazı davranışları değiştirir; doğal oturum ve yorumlu oturum ayrılabilir.

**REQ-SLICE-002:** Eğlence değerlendirmesi yalnız geliştiricinin başarıyla bitirmesine dayanamaz. Yardımlı ve yardımsız completion ayrı raporlanır.

## 6. Başlangıç ölçütleri

Pilot hedefi: 5 kişiden en az 4'ü ilk su/inventory görevini yardım almadan; en az 3'ü ana seferi blocker olmadan; çoğu ölüm sebebini ve sonraki planını anlatabilir. Bunlar küçük örneklem yön göstericidir, kesin shipping threshold değildir. “Fun” tek 1-10 skoruna indirgenmez; en çok hatırlanan an, en sıkıcı an, tekrar oynamak için hedef sorulur.

Performans donanım profiliyle birlikte ölçülür. Test günü input sensitivity veya FOV sorunu varsa temel loop başarısızlığı diye etiketlemek yerine confounder kaydı yapılır. Negatif geri bildirim sırf oyuncu türü farklı diye elenmez; hangi segmentte çıktığı belirtilir.

## 7. Exit gate

S0/S1 açık hata yok; duplicate item/save corruption yok; bütün zorunlu sistemlerde NOT_RUN kalmamış; bir final-quality POI; tüm asıl input yolları; gözlenen iki geçerli çözüm; ölçülen frame bütçesi; GDD/MD çelişki kaydı güncel. Her failure için sonraki iş paketi veya kontrollü kapsam kesintisi belirlenir.

**REQ-SLICE-003:** Gate raporu tasarım önerisini kod kanıtı yerine koyamaz. Build ID, fixture, tester, tarih, log ve gözlem bağlanmalıdır. Şu anda bu kanıtlar üretilmemiştir.
