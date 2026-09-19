---
doc_id: LS-DOC-18
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# UI/UX, erişilebilirlik, localization ve oyuncu geri bildirimi

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §7, §19-20. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Ekran sahipliği

HUD okunabilir durum bilgisini verir; Inventory transfer/equip, Health wound/treatment, Map bilgi/rota, Crafting requirements, Shelter utilities, Journal objectives, Settings cihaz tercihlerinin sahibidir. Bu ekranlar state'in sahibi değildir. Domain snapshot/revision üzerinden çalışır ve command sonucu gösterir.

**REQ-UX-001:** Aynı işlemin başarı/red durumu UI, ses ve world representation'da çelişemez. Transfer reddedildiğinde başarı sesi çalmaz.

## 2. HUD yoğunluğu

Normal durumda kritik olmayan barlar düşük görünürlükte. Değer düşüşü/tehlike eşiği/context ile görünürlük artar. Health/wound uyarısı loot notification'dan üst önceliklidir. Renk, ikon, kısa metin ve ses birlikte kullanılır. Her saniye aynı “susadın” bildirimi üretilmez; threshold crossing ve hysteresis bulunur.

Crosshair prompt: action name, target name, key glyph, disabled reason. İki satırdan uzun sistem debug bilgisi HUD'a konmaz. Detay gerektiğinde inspect ekranındadır. Basit bir hareketi yapmak için “Domain command” gibi teknik terimler ürün arayüzüne girmez.

## 3. Inventory akışı

Sol player, sağ container; aralarında transfer actionları. Item satırı ad, miktar, condition, effective mass ve gerekli risk bilgisini gösterir. Filtre/sıralama selection'ı korur. Gamepad focus row'undan yeni ekrana geçişte geri dönüş noktası saklanır. Drag-drop dışında tek tuşla transfer/split/equip vardır.

Split miktarı sayı girişi ve kontrollü slider ile yapılabilir. Repeat key miktarı negatif yapamaz. Container unload/kapama sırasında eski panel kapanır, oyuncu input context'i geri gelir. Modal açılış click'i item kullanmaz.

## 4. Health ve treatment

Body zones grafik veya liste biçiminde erişilebilir. Her wound: severity, aktif sonuç, uygun tedavi, tedavi sonrası kalan risk. Painkiller ile bleeding'in çözülmediği açık. Tedavi iptal koşulları başlamadan görülebilir. Kritik warning yalnız bulanıklık veya kırmızı vignette değildir; motion sensitivity olan oyuncu aynı bilgiyi metinle alabilir.

## 5. Map ve knowledge

Map keşfedileni gösterir. World Pressure estimate'inin gözlem zamanı bulunur. Unvisited POI'nin exact loot listesi gösterilmez. Pin notları oyuncu kaynaklıdır; otomatik objective marker'dan görsel olarak ayrılır. Zoom/pan, mouse ve gamepad ile tam çalışır. Map'in açılması default solo profilde pause olabilir; Harsh modda değişiyorsa başlangıçta belirtilir.

**REQ-UX-002:** Pause/menu politikası profile içinde sabittir ve HUD'da belirsiz değildir. Oyuncu pause sandığı ekranda gizlice açlık veya saldırıyla karşılaşmaz.

## 6. Accessibility kabul matrisi

| İhtiyaç | Özellik | Test |
|---|---|---|
| Motion sensitivity | Bob/shake/blur kapatma, FOV | Combat bilgisi kaybolmuyor |
| Düşük görme | UI scale, kontrast, subtitle boyutu | 1080p ölçeklerde clipping yok |
| Renk algısı | İkon+metin ile eş anlam | Rarity/tehlike yalnız renkte değil |
| İşitme | Önemli ses caption/direction | Yakın threat fark edilebilir |
| Motor erişim | Rebind, hold/toggle, zorunlu mash yok | Tüm menüler yalnız klavyeyle |
| Gamepad | Odak görünürlüğü, deadzone | Mouse olmadan full loop |

Directional caption dünyadaki algılanabilir sese dayanır; duvar ötesindeki görünmez düşmanın kesin kimliğini ücretsiz radar gibi açıklamaz. Aim assist opsiyonel ve profil içinde görünürdür.

## 7. Localization

İlk kaynak diller Türkçe/İngilizce hedefi; shipping language kararı üretim bütçesine bağlı. Hardcoded player-facing string yerine key. Sayı formatı locale-aware; save sayıları invariant culture. String birleştirerek İngilizce cümle kurup Türkçede bozuk sıra üretme. Çoğul ve birim politikası localization katmanında çözülür.

**REQ-UX-003:** Font fallback Türkçe ç, ğ, ı, İ, ö, ş, ü karakterlerini göstermeli. Pseudo-localization +%30 metin uzunluğunda kritik buton ve prompt kesilmemeli. Controller glyph metin gibi çevrilmez.

## 8. Tutorial ve kabul

Context tutorial bir kez gösterilir; başarısızlıkta kısa tekrar, settings'te replay/reset. İlk pickup sonrası inventory anlatımı; ilk bleeding sonrası treatment; ilk gunshot sonrası noise consequence. Bir anda beş tutorial popup'ı yığılmaz.

Kabul rotası: yeni oyuncu ayar yap → rebinding → spawn → item al → inventory split/equip → wound treat → map pin → shelter save → quit/load. Mouse, yalnız klavye ve gamepad ayrı denenir. Ultrawide, 16:9, düşük resolution ve büyük UI scale testleri görünür kanıt ister.
