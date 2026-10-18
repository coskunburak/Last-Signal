# Item/craft/build atomiklik ve dünya kalıcılığı

**LS-BLD v2 governance · PROPOSED · owner acceptance pending**

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Tek doğruluk kaynağı
- Inventory item aynı anda tek owner; world item'ları persistentId ile izlenir.
- Build confirm aynı input/world revision üzerinde validasyondan geçer; success receipt aynı anda malzeme azaltımı + structure creation + registry kaydı anlamına gelir.
- Preview ve UI hiçbir resource mutation yapmaz.
- Duplicate request nonce yalnız ilk accepted sonuç üretir.
- Yapı stable ID, definition version, cell owner, transform, condition, runtime flags ve tombstone içerir.
- SaveLoaded world GameObject enumeration bütün dünyanın kaydı yerine geçmez: authoritative unloaded cell delta korunmalıdır.

## Başarısızlık senaryoları
- Frame değişimi, inventory changed between preview/commit → fail/revalidate, malzeme korunur.
- Prefab spawn exception → partial structure cleanup + inventory rollback.
- Cell unload sırasında mutation → defer/busy; suspended cell state kaybolmaz.
- Double-click confirm → nonce dedupe.
- Output full/escrow cancel → S010 refund/output waiting korunur.
- Old save construction section missing → migration gives empty collection; old content unchanged.
- Crash/disk write → previous valid save generation remains loadable.

## Gate fixtures
- cancel 0%, near complete, full output, death, sleep, queue, single/nonexisting station.
- build success/invalid/duplicate and save-load boundary.
- new→save→unload→load→delete→save→load tombstone.
- special: two adjacent cells, one structure ID, no double hydration.

## Uygulama zamanı
Bu sözleşme ilgili phase/sprint/day belgelerinde referans alınır. Gerçek kodu ve yerel scene state’i implementation gününde doğrulamadan hiçbir önerilen sınıfı mevcut sayma.
