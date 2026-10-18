# BLD-S006 — Craft Defteri UI/UX

**Parent:** `P02 — Crafting çekirdeği ve Craft Defteri` · **Giriş:** S005. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Craft Defteri UI/UX.
- **Phase purpose:** Çok girdili tarifleri, güvenli rezervasyonu, tek UI akışını ve erişilebilir kontrolü geliştir.
- **FirstTownCore reference scenario:** Craft Defteri -> Yapılar -> Çit: malzeme gösterimi -> seç ve kapat.
- **Measurable phase exit:** G2: birden fazla malzemeli recipe, craft/iptal, notebook, save testleri.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Assets/LastSignal/Scripts/Runtime/Inventory/PlayerInventory.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Docs/Last_Signal_Docs_v0.2/sources/07_Item_Inventory_Equipment.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- Inventory quantity snapshots ve revision events
- Category/recipe/build definition registry
- Mevcut UI input/cursor/pause kuralları ve accessibility

### Değiştirilemez davranışlar
- UI doğrudan item eksiltmez veya world prefab instantiate etmez.
- Aynı seçimi çift tıklamak iki build/craft isteği başlatmaz.
- Geçersizliğin nedeni yalnız yeşil/kırmızı renk ile anlatılmaz.

### Domain anlaşmaları
- Büyük catalog her frame rebuild edilmez; diff/event update.
- Kullanıcının verdiği konsept görsel UI davranışı referansıdır, birebir hazır asset değil.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D041 | [Aç](DAYS/BLD-D041_ui-bilgi-mimarisi.md) | UI bilgi mimarisi | Kullanıcı 2–3 adımda yapı tarifine ulaşabilir. |
| BLD-D042 | [Aç](DAYS/BLD-D042_recipe-kartlar.md) | Recipe kartları | Stok ile gereken miktar yan yana ve günceldir. |
| BLD-D043 | [Aç](DAYS/BLD-D043_notebook-ac-kapat.md) | Notebook aç/kapat | Pause/reload ve gameplay input çakışmaz. |
| BLD-D044 | [Aç](DAYS/BLD-D044_blueprint-secimi.md) | Blueprint seçimi | UI doğrudan world objesi spawn etmez. |
| BLD-D045 | [Aç](DAYS/BLD-D045_erisilebilir-geri-bildirim.md) | Erişilebilir geri bildirim | Renk körlüğünde bilgi kaybı olmaz. |
| BLD-D046 | [Aç](DAYS/BLD-D046_search-filter-scroll.md) | Search/filter/scroll | Yüzlerce öğe varken her frame tam liste kurulmaz. |
| BLD-D047 | [Aç](DAYS/BLD-D047_ses-ve-gorsel-his.md) | Ses ve görsel his | Bildirimler oyun seslerini bastırmaz. |
| BLD-D048 | [Aç](DAYS/BLD-D048_ornek-senaryo-kabulu.md) | Örnek senaryo kabulü | Oyuncu tarifin niçin kilitli olduğunu söyleyebilir. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D041**, sprint entry → BLD-D042: Yapılar, aletler, kaynak işleme, istasyonlar, savunma kategorilerini tasarla. **Acceptance:** Kullanıcı 2–3 adımda yapı tarifine ulaşabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D042**, BLD-D041 → BLD-D043: İkon, isim, özet, miktar, eksik malzeme ve unlock nedenini göster. **Acceptance:** Stok ile gereken miktar yan yana ve günceldir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D043**, BLD-D042 → BLD-D044: Mouse, klavye ve gelecekte gamepad için ortak input policy. **Acceptance:** Pause/reload ve gameplay input çakışmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D044**, BLD-D043 → BLD-D045: BuildDefinition seçimi sonrası notebook→ghost controller komutu üret. **Acceptance:** UI doğrudan world objesi spawn etmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D045**, BLD-D044 → BLD-D046: Renk ve ikon/metin ile craft uygunluğu bildir. **Acceptance:** Renk körlüğünde bilgi kaybı olmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D046**, BLD-D045 → BLD-D047: Liste filtreleme, kategori ve detay sayfasını hafif implement et. **Acceptance:** Yüzlerce öğe varken her frame tam liste kurulmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D047**, BLD-D046 → BLD-D048: Sayfa geçiş, seçim, hata ve onay feedback'ini düşük maliyetle ekle. **Acceptance:** Bildirimler oyun seslerini bastırmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D048**, BLD-D047 → sprint gate: Odun mevcut/yetersiz, tool eksik ve istasyon yok akışlarını prova et. **Acceptance:** Oyuncu tarifin niçin kilitli olduğunu söyleyebilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Tarif/yapı tanımından yalnız read-only UI modeli üret; katalog ile runtime inventory’yi karıştırma.
2. **Sprint step 2:** Kategori, arama, malzeme gereği ve unlock durumlarını gerçek inventory revision event’iyle güncelle.
3. **Sprint step 3:** Mouse/keyboard/controller odağını, Inventory/Reload/Pause ile çakışmayacak modal kontrata bağla.
4. **Sprint step 4:** Seçim callback’ini build-selection request veya craft request’e bağla; UI’dan Instantiate çağırma.
5. **Sprint step 5:** Hatalı/eksik malzemede sadece renk değil ikon ve kısa anlaşılır gerekçe göster.
6. **Sprint step 6:** Kategori değişimi ve hızlı seçimin stale UI state üretmemesi için version/selection guard tanımla.
7. **Sprint step 7:** UI kalıcılığını değil domain state kalıcılığını koru; ölüm/scene switch sırasında geçici UI’ı kapat.
8. **Sprint step 8:** En az bir düşük çözünürlük, uzun tarif ismi ve gamepad senaryosuyla erişilebilirliği kontrol ettir.
9. Kartların kaynak/owner dönüşümü ve geriye uyumlu data alanlarının field-level diff’ini kontrol et.
10. Kullanıcı test komutları gerçek local tooling’e göre hazırlanır; implementation sonrası kullanıcıdan evidence gelmeden DONE denmez.

### Definition of Ready

- [ ] Bütün gerçek sınıf/asset yolları yerel çalışma ağacında mevcut veya net blocked.
- [ ] Eski çalışmalardaki önemli dirty değişiklikler korunmuş ve kaynak commit kaydı tutulmuş.
- [ ] Her kartın implementasyon sahibi, çıktısı, bağımlılığı ve kabul metriği belli.
- [ ] Save/scene/asset destructiveness için rollback/checkpoint hazır.
- [ ] User manuel test ownership ve klasör/evidence konvansiyonu doğrulandı.

## 6. Sprint-level integration design

| Etki | Planlanan kontrol | Fail durumundaki karar |
|---|---|---|
| Domain sahibi | Inventory quantity snapshots ve revision events | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | UI doğrudan item eksiltmez veya world prefab instantiate etmez. | Partial mutation rollback |
| İstisna | Craft Defteri açıkken reload, ADS, movement ve pause birbirini bozar mı? | Explicit reject receipt |
| World/scene | Craft Defteri -> Yapılar -> Çit: malzeme gösterimi -> seç ve kapat | Source scene korunur |
| Test | Keyboard/mouse/controller input focus tests | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S006-EDGE-1:** Craft Defteri açıkken reload, ADS, movement ve pause birbirini bozar mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S006-EDGE-2:** Kategori boşsa veya tarif unlock edilmemişse UI ne gösterir? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S006-EDGE-3:** 1920×1080 dışı çözünürlükte, gamepad ve klavyede kartlar erişilir mi? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-REG-1:** Mevcut S010 shelter inventory/recipe/claim davranışı hatalı değişmiyor.
- **S-REG-2:** Save/load veya scene loading sonucu kayıp/dupe yok.
- **S-REG-3:** Aynı girdi/seed ile deterministik davranış kanıtlanıyor.
- **S-REG-4:** Çatışmasız cancel veya retry sonrası transient obje kalmıyor.

## 8. Sprint verification order — owner run only

1. Agent yalnız stage authoring/test tanımı/kod/prefablarını hazırlar; tests/build çalıştırmaz.
2. Kullanıcı test alanını koruyan ayrı Editor session açar ve hazırlanan authoring komutunu gerekiyorsa çalıştırır.
3. Kullanıcı focused EditMode, ardından focused PlayMode komutlarını çalıştırır.
4. FAIL halinde agent yalnız failure receipt ve gerekli sınıfları okuyup root cause düzeltir.
5. Kullanıcı başarısız testi yeniden çalıştırır; PASS sonrası integration/regression genişletilir.
6. Save-sensitive sprintte legacy fixture, crash/rollback ve cell tests zorunlu.
7. Görsel konu ise fps/day/dusk/rain karşılaştırması kullanıcı tarafından alınır.
8. Gerekli benchmark son kapıda ve gerçek cihaz bilgisiyle çalıştırılır.

- **Focused test theme 1:** Keyboard/mouse/controller input focus tests
- **Focused test theme 2:** Missing materials and unlock reason readability
- **Focused test theme 3:** UI open/close and life-cycle without side effects

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Craft Defteri açıkken reload, ADS, movement ve pause birbirini bozar mı? Sorumlu alan: `notebook`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Kategori boşsa veya tarif unlock edilmemişse UI ne gösterir? Sorumlu alan: `notebook`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** 1920×1080 dışı çözünürlükte, gamepad ve klavyede kartlar erişilir mi? Sorumlu alan: `notebook`; kanıt olmadan sessiz workaround yasak.
- **Regression risk:** Yeniden test etmeden eski PASS bu branch için transfer edilemez.
- **Visual risk:** Low-poly/graybox görünüm sadece asset sayısıyla production kabul almaz.
- **Schedule risk:** Her kart ertelenirse neden, gereken girdi ve downstream sprint etkisi kaydedilir.

## 10. Sprint exit gate / closure checklist

- [ ] Bütün sekiz kart için öncesi/sonrası davranış ve changed files kayıtlı.
- [ ] No duplicate item/structure ID ve hiçbir yarım transaction bulunmuyor.
- [ ] Kaynak GDD ve domain sözleşmesiyle çelişki yok veya onaylı ADR mevcut.
- [ ] Owner focused tests PASS; başarısız varsa sprint PARTIAL/BLOCKED.
- [ ] Gereken PlayMode, save migration, navigation ve visual checks tamamlandı.
- [ ] User has signed specific art acceptance where needed.
- [ ] Kritik perf metric baseline’ı ölçüldü veya `NOT_MEASURED` denildi.
- [ ] `../PHASE.md` gate’e doğru kanıt bağlantısı eklendi.

## 11. Handoff receipt

- **Sprint:** BLD-S006
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P02](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
