# Durum, kanıt ve sprint closure kontratı

**LS-BLD v2 governance · PROPOSED · owner acceptance pending**

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Status tanımları
- `PROPOSED`: Tasarım var, resmi ürün onayı yok.
- `READY`: bağımlılıklar ve owner kararı kapandı.
- `IMPLEMENTED`: code/data/asset diff mevcut ancak test kanıtı olmayabilir.
- `VERIFYING`: kullanıcı manuel test çalıştırıyor.
- `PASS`: özellikle belirtilmiş kabul için gerçek geçer evidence mevcut.
- `PARTIAL`: bir veya birden fazla acceptance eksik ama teknik ilerleme var.
- `FAIL`: ilgili test/acceptance başarısız.
- `BLOCKED`: gerekli kaynak/asset/decision mevcut değil.
- `TEST_NOT_RUN`: çalıştırma yok; eskiden kalma PASS ile doldurulmaz.

## Evidence minimum
- Commit/branch source state; diff/file hashes; focused test receipts; Unity logs; PNG/vid when needed; saved game fixtures; performance samples and machine context.
- Source file/folder isimlerinden sandbox link uydurulmaz.
- Sprint closure test coverage % değil, risk matrix dahilinde severity anahtarları ve gerçek bug fix roundtrip gösterir.

## Uygulama zamanı
Bu sözleşme ilgili phase/sprint/day belgelerinde referans alınır. Gerçek kodu ve yerel scene state’i implementation gününde doğrulamadan hiçbir önerilen sınıfı mevcut sayma.
