# Production asset ve görsel referans kuralları

**LS-BLD v2 governance · PROPOSED · owner acceptance pending**

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Hedef
- Last Signal stylized realism; FirstTownCore world art ve S013 Asset Bible referans alınır.
- Çit/duvar/kapı/çatı prefabları gerçek ölçü, pivot, collider, snap anchor, LOD ve materyal tutarlılığı sağlamalı.
- Yeşil/kırmızı preview saydam olsa bile ilgili blueprint'in siluetini doğru gösterir; render-only olduğu için gerçek nesnenin gameplay scripts/physics'i çalışmaz.
- Red/green tek iletişim kanalı değildir; ikon/kısa mesaj zorunlu.
- Art review day, dusk, rain, forest edge, cluttered safehouse ve FPS bakışlarından yapılır.
- AI mood reference final Unity asset screenshot kanıtı değildir.
- ThirdParty dosyaları vendor kaynaklarıdır, project-owned variant üret; ticari kullanım lisansını kontrol et.
- Meshy AI üretimi bağımsız DEFERRED sprinttir, bu programın source requirement'ı değildir.

## Asset acceptance receipt
- Source/license, path/.meta GUID, dimensions, triangle/renderers, texture settings, SRP/URP shader, collider, LOD, pivot/snap anchors, first-person silhouette, shadow, wetness, performance delta, replacement path.

## Uygulama zamanı
Bu sözleşme ilgili phase/sprint/day belgelerinde referans alınır. Gerçek kodu ve yerel scene state’i implementation gününde doğrulamadan hiçbir önerilen sınıfı mevcut sayma.
