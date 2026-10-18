# Risk kaydı ve değişiklik bütçesi

**LS-BLD v2 governance · PROPOSED · owner acceptance pending**

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Üst riskler
1. Scope creep: sınırsız inşa/çok katlı ev → P05 G5 öncesi dondur.
2. Data loss/dupe: item ownership + structure spawn → G3/G7 blocker.
3. Renderer pressure: çok parçalı yapı, ghost ve foliage → benchmark before optimization.
4. Navigation: çit kapısı/sığınak kapısı ve zombie paths → deny unsafe placement.
5. Existing save/prefab break: S010 fixed socket/recipe assumptions → adapter, migration fixtures.
6. Art shortfall: graybox accepted as final → FPS/day/night/weather review.
7. Artificial grind: kaynak çok kıt veya sürekli reset → source/sink simulation + playtest.
8. Incomplete user-facing messages: all reject states same text → typed error codes.

## Yönetim
- Riskin tetikleyici metriği, owner, azaltım aksiyonu, rollback ve sonraki gate kaydı tutulur.
- İlk 10 sprint sonunda eğlence doğrulanmazsa sonraki 18 sprinti otomatik başlatma.
- G0 ve G4 ayrı owner product signoff gerektirir.

## Uygulama zamanı
Bu sözleşme ilgili phase/sprint/day belgelerinde referans alınır. Gerçek kodu ve yerel scene state’i implementation gününde doğrulamadan hiçbir önerilen sınıfı mevcut sayma.
