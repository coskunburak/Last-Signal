# BLD-S025 — Craft Defteri UX ve erişilebilirlik

**Parent:** `P08 — Sanat, deneyim ve optimizasyon` · **Giriş:** S006,S024. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Craft Defteri UX ve erişilebilirlik.
- **Phase purpose:** Production-quality modeller, animasyon, ses, erişilebilir UI, profiler hedefleri.
- **FirstTownCore reference scenario:** Mouse/klavye/gamepad ve renk körlüğü için Craft Defteri usability.
- **Measurable phase exit:** G8: görsel kabul + performans + erişilebilirlik doğrulaması.
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
| BLD-D193 | [Aç](DAYS/BLD-D193_notebook-navigation.md) | Notebook navigation | Kritik tarif kaybolmaz. |
| BLD-D194 | [Aç](DAYS/BLD-D194_color-accessibility.md) | Color accessibility | Renk körlüğü ve düşük renk modunda geçerlilik okunur. |
| BLD-D195 | [Aç](DAYS/BLD-D195_input-rebinding.md) | Input rebinding | Kontroller hardcode tuşlarla sınırlı değil. |
| BLD-D196 | [Aç](DAYS/BLD-D196_controller-path.md) | Controller path | Mouse olmadan tam temel döngü yapılır. |
| BLD-D197 | [Aç](DAYS/BLD-D197_craft-progression-feedback.md) | Craft progression feedback | Kaynak escrow yanlış anlaşılmaz. |
| BLD-D198 | [Aç](DAYS/BLD-D198_localized-strings.md) | Localized strings | UI literal string birikimi sınırlı. |
| BLD-D199 | [Aç](DAYS/BLD-D199_error-resolution-ux.md) | Error resolution UX | Ret sonrası kullanıcı nasıl düzelteceğini bilir. |
| BLD-D200 | [Aç](DAYS/BLD-D200_usability-session.md) | Usability session | UI iyileştirmesi gözlemle kararlaştırılır. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D193**, sprint entry → BLD-D194: Arama, favoriler, kategoriler, locked/unlocked recipes. **Acceptance:** Kritik tarif kaybolmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D194**, BLD-D193 → BLD-D195: Yeşil/kırmızı yerine ikon + metin + desen/outline seçeneği. **Acceptance:** Renk körlüğü ve düşük renk modunda geçerlilik okunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D195**, BLD-D194 → BLD-D196: Yerleştir/rotasyon/yükseklik/snap/cancel komutları binding tabanlı. **Acceptance:** Kontroller hardcode tuşlarla sınırlı değil. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D196**, BLD-D195 → BLD-D197: Analog ghost movement, rotate, UI focus ve confirm/back. **Acceptance:** Mouse olmadan tam temel döngü yapılır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D197**, BLD-D196 → BLD-D198: Queued, in progress, waiting output, cancelled ve failed durumlarını göster. **Acceptance:** Kaynak escrow yanlış anlaşılmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D198**, BLD-D197 → BLD-D199: Türkçe/İngilizce terimler, kısa açıklama ve item isimleri veri modelinde. **Acceptance:** UI literal string birikimi sınırlı. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D199**, BLD-D198 → BLD-D200: No resource, blocked zone, sloped ground, duplicate edge, busy system. **Acceptance:** Ret sonrası kullanıcı nasıl düzelteceğini bilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D200**, BLD-D199 → sprint gate: 3 yeni oyuncu ile tutorial süre, yanlış denemeler ve comprehension ölç. **Acceptance:** UI iyileştirmesi gözlemle kararlaştırılır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | Mouse/klavye/gamepad ve renk körlüğü için Craft Defteri usability | Source scene korunur |
| Test | Keyboard/mouse/controller input focus tests | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S025-EDGE-1:** Craft Defteri açıkken reload, ADS, movement ve pause birbirini bozar mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S025-EDGE-2:** Kategori boşsa veya tarif unlock edilmemişse UI ne gösterir? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S025-EDGE-3:** 1920×1080 dışı çözünürlükte, gamepad ve klavyede kartlar erişilir mi? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S025
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P08](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
