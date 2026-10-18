# Craft Defteri, ghost, etkileşim ve erişilebilirlik

**LS-BLD v2 governance · PROPOSED · owner acceptance pending**

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## UX ilkeleri
- Elde malzeme var/yok değil; her tarifte gerekli+mevcut adet, source, station, craft süreleri ve unlock koşulları görünür.
- Geçerli ghost yeşil, geçersiz kırmızı, fakat ret için ikon, text ve mümkünse düzeltme önerisi şart.
- Rotasyon/yükseklik/snap birbiriyle tutarlı; placement sırasında silah ateşlenmez veya ammo tüketilmez.
- Pause, death, scene unload, input focus ve Inventory açıkken modal/ghost durumları temizlenir.
- Category/search değişimi UI event tabanlı; her Update'te inventory taranmaz.
- Gamepad ve klavye için rebindable komutlar; ESC/Cancel güvenli, overlay pointer state eskisinden farklı davranmaz.
- Kullanıcı 'neden bu çiti buraya koyamıyorum?' sorusunu tek bakışta yanıtlayabilmeli.

## Uygulama zamanı
Bu sözleşme ilgili phase/sprint/day belgelerinde referans alınır. Gerçek kodu ve yerel scene state’i implementation gününde doğrulamadan hiçbir önerilen sınıfı mevcut sayma.
