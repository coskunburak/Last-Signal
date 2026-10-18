# BLD-S005 — Recipe engine ve malzeme işlemeyi genişlet

**Parent:** `P02 — Crafting çekirdeği ve Craft Defteri` · **Giriş:** S003. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Recipe engine ve malzeme işlemeyi genişlet.
- **Phase purpose:** Çok girdili tarifleri, güvenli rezervasyonu, tek UI akışını ve erişilebilir kontrolü geliştir.
- **FirstTownCore reference scenario:** Kütük + araç + station kullanarak plank üretimi ve escrow güvenliği.
- **Measurable phase exit:** G2: birden fazla malzemeli recipe, craft/iptal, notebook, save testleri.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterRecipe.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterProduction.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- S010 ShelterRecipe/ShelterProduction ve mevcut escrow refund semantiği
- StableItemId, stack sayımı, kaynak revision, tools/station gates
- Çok girdi ve çıktı tanım tasarımı ile recipe catalog

### Değiştirilemez davranışlar
- Bir girdi aynı anda iki crafting job tarafından kullanılamaz.
- Cancel, full-output, save/load hiçbir materyali yok etmez veya çoğaltmaz.
- Yeni tarifler eski ShelterRecipe ve legacy save davranışlarını değiştirmez.

### Domain anlaşmaları
- Craft notebook kaynak sayımı gösterir ama authoritative item mutation yapmaz.
- Time basis açık: handcraft simulation seconds; workstation world time (tasarım kararı).

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D033 | [Aç](DAYS/BLD-D033_tarif-semas.md) | Tarif şeması | Eski recipe/saveler çalışmaya devam eder. |
| BLD-D034 | [Aç](DAYS/BLD-D034_malzeme-listesi.md) | Malzeme listesi | Çakışan/negatif input reddedilir. |
| BLD-D035 | [Aç](DAYS/BLD-D035_kutukten-tahta.md) | Kütükten tahta | Net materyal miktarı dengeli ve izlenebilir. |
| BLD-D036 | [Aç](DAYS/BLD-D036_recipe-validator.md) | Recipe validator | Sonsuz bedava kaynak çevrimi oluşturulamaz. |
| BLD-D037 | [Aç](DAYS/BLD-D037_escrow-butunlestirme.md) | Escrow bütünleştirme | İptal/ölüm/kayıt doluluğu duplication yaratmaz. |
| BLD-D038 | [Aç](DAYS/BLD-D038_craft-islem-gunlugu.md) | Craft işlem günlüğü | İki kere submit aynı craftı iki kez üretmez. |
| BLD-D039 | [Aç](DAYS/BLD-D039_zaman-politikas.md) | Zaman politikası | Pause/skip/sleep sırasında süre sözleşmesi korunur. |
| BLD-D040 | [Aç](DAYS/BLD-D040_regression-koprusu.md) | Regression köprüsü | Önceki depolama ve modül davranışları bozulmaz. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D033**, sprint entry → BLD-D034: Tek-input ShelterRecipe yerine ek, revisionlu çok girdili RecipeDefinition tasarla. **Acceptance:** Eski recipe/saveler çalışmaya devam eder. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D034**, BLD-D033 → BLD-D035: Aynı tarifte çok input, araç, station ve unlock şartını tanımla. **Acceptance:** Çakışan/negatif input reddedilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D035**, BLD-D034 → BLD-D036: Log→plank için başlangıç dönüşüm maliyeti ve verimini kataloğa ekle. **Acceptance:** Net materyal miktarı dengeli ve izlenebilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D036**, BLD-D035 → BLD-D037: ID, cycle, overflows, item max stack, station ve output koşullarını test et. **Acceptance:** Sonsuz bedava kaynak çevrimi oluşturulamaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D037**, BLD-D036 → BLD-D038: S010 escrow/cancel/output full politikasını genelleştir. **Acceptance:** İptal/ölüm/kayıt doluluğu duplication yaratmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D038**, BLD-D037 → BLD-D039: Request ID, revision, accepted/rejected reason ve receipt üret. **Acceptance:** İki kere submit aynı craftı iki kez üretmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D039**, BLD-D038 → BLD-D040: Elde üretim simülasyon saniyesi, istasyon world time ayrımı yap. **Acceptance:** Pause/skip/sleep sırasında süre sözleşmesi korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D040**, BLD-D039 → sprint gate: Eski S010 recipe testlerini ve yeni multi-input testlerini hazırlamayı planla. **Acceptance:** Önceki depolama ve modül davranışları bozulmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** ShelterRecipe tek girdi yolunun korunduğunu doğrula; yeni tarifin ihtiyaç duyduğu çok girdiyi katalogla.
2. **Sprint step 2:** Her input için itemId, quantity, source, tool, station ve unlock koşullarını deterministik sıralama ile tanımla.
3. **Sprint step 3:** Craft talebi için inventory revision snapshot al, escrow rezervasyonuna yalnız bir domain API aç.
4. **Sprint step 4:** Başarısız station/input/output kontrolünde materyal değiştirmeyen reject branch tasarla.
5. **Sprint step 5:** İptal, dolu output, güç kesintisi, pause ve save/load arasında job lifecycle state diyagramı üret.
6. **Sprint step 6:** Log→plank veya ilgili üretimin kayıp/çıktı miktarını mass/stack sınırlarıyla simüle et.
7. **Sprint step 7:** Tarif listesinde zero-cost cycle, self-input output ve integer overflow doğrulamasını yap.
8. **Sprint step 8:** Tekrar submit/duplicate request için idempotent receipt ve focused QA beklenen sonucu tanımla.
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
| Domain sahibi | S010 ShelterRecipe/ShelterProduction ve mevcut escrow refund semantiği | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Bir girdi aynı anda iki crafting job tarafından kullanılamaz. | Partial mutation rollback |
| İstisna | Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli. | Explicit reject receipt |
| World/scene | Kütük + araç + station kullanarak plank üretimi ve escrow güvenliği | Source scene korunur |
| Test | Multi-input escrow/refund and overflow | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S005-EDGE-1:** Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S005-EDGE-2:** Station üretim ortasında güç kaybederse ne olur: pause/abort açık kural. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S005-EDGE-3:** Output doluyken CompletedWaitingOutput davranışı korunur. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Multi-input escrow/refund and overflow
- **Focused test theme 2:** Recipe cyclic free-resource conversion detection
- **Focused test theme 3:** Pause/quit/save/reload/double-submit

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli. Sorumlu alan: `craft`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Station üretim ortasında güç kaybederse ne olur: pause/abort açık kural. Sorumlu alan: `craft`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Output doluyken CompletedWaitingOutput davranışı korunur. Sorumlu alan: `craft`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S005
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P02](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
