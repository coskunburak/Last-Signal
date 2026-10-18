# BLD-S019 — Uzun vadeli üs progression ve G6

**Parent:** `P06 — Sığınakla birleşen üretim ve bakım` · **Giriş:** S016–S018. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Uzun vadeli üs progression ve G6.
- **Phase purpose:** İşlevsel istasyonlar, onarım/söküm, güvenlik, elektrik-su ve ilerleme.
- **FirstTownCore reference scenario:** Oyuncunun bir sefer sonrası üssündeki somut kazanım ve hedef yenileme.
- **Measurable phase exit:** G6: yaşayan sığınak + kapsamlı craft ve maintenance loop.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterProduction.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- ShelterSite claim/installed socket ve ShelterProduction
- Power/fuel/generator noise ve world time
- BuildZone, structure function and room shelter evaluation

### Değiştirilemez davranışlar
- Üs claim zombies’i silmez veya invulnerability eklemez.
- Mevcut sabit modüller ve storage erişim koşulları geriye uyumlu kalır.
- Power kaybı yatak/claim/ownership kaybı anlamına gelmez.

### Domain anlaşmaları
- Kullanılabilir çalışma tezgâhı varsa yeni crafting ownership kopyası oluşturma.
- Sığınak güvenliği kararlı ve anlaşılır; zorunlu nightly raids kapsam dışıdır.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D145 | [Aç](DAYS/BLD-D145_progression-graph.md) | Progression graph | Ana sefer için yeni hedef görünür. |
| BLD-D146 | [Aç](DAYS/BLD-D146_craft-unlock-policy.md) | Craft unlock policy | Kilitli tarif nedeni UI'da açık. |
| BLD-D147 | [Aç](DAYS/BLD-D147_safehouse-value.md) | Safehouse value | Kurulmayan üs oynanışı imkânsız kılmaz. |
| BLD-D148 | [Aç](DAYS/BLD-D148_base-defense-loop.md) | Base defense loop | Her gece zorunlu raid dayatılmaz. |
| BLD-D149 | [Aç](DAYS/BLD-D149_recipe-catalog-budget.md) | Recipe catalog budget | Her tarif farklı işe yarar. |
| BLD-D150 | [Aç](DAYS/BLD-D150_repair-economics.md) | Repair economics | Onarım oyuncuya angarya hissettirmez. |
| BLD-D151 | [Aç](DAYS/BLD-D151_3060-min-scenario.md) | 30–60 min scenario | Yapılan yapı somut fayda sağlar. |
| BLD-D152 | [Aç](DAYS/BLD-D152_g6-review.md) | G6 review | Tamamlanmamış faydalar PARTIAL kalır. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D145**, sprint entry → BLD-D146: 4 aşamalı barınak gelişimi, unlock maliyeti ve player reward. **Acceptance:** Ana sefer için yeni hedef görünür. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D146**, BLD-D145 → BLD-D147: Knowledge, station level ve exploration items ile recipe açma. **Acceptance:** Kilitli tarif nedeni UI'da açık. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D147**, BLD-D146 → BLD-D148: Daha güvenli stash, kuruluk, üretim, dinlenme faydalarını dengele. **Acceptance:** Kurulmayan üs oynanışı imkânsız kılmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D148**, BLD-D147 → BLD-D149: Zombi davranışı ve ses tehdidi kontrollü üs etkileşimi. **Acceptance:** Her gece zorunlu raid dayatılmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D149**, BLD-D148 → BLD-D150: Başlangıç katalog sayısı ve gereksiz varyant budama. **Acceptance:** Her tarif farklı işe yarar. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D150**, BLD-D149 → BLD-D151: Bakım maliyeti/gelir dengesi; bir haftalık kaynak öğütme önlenir. **Acceptance:** Onarım oyuncuya angarya hissettirmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D151**, BLD-D150 → BLD-D152: Player setup→harvest→build→utilize→defend→save döngüsü. **Acceptance:** Yapılan yapı somut fayda sağlar. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D152**, BLD-D151 → sprint gate: Stability, player satisfaction ve art/perf kabul raporu. **Acceptance:** Tamamlanmamış faydalar PARTIAL kalır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Mevcut ShelterSite claim/Bed/Storage/Workbench soketlerini değiştirmeden yeni yapının faydasını tanımla.
2. **Sprint step 2:** Utility state için unlocked capability ile currently operational flag ayrımını uygula.
3. **Sprint step 3:** İstasyon uzaklık/erişim kontrolünü depo, player ve hangi cell loaded bilgisine bağla.
4. **Sprint step 4:** Jeneratör/ışık/radyo/su gibi kullanımların yakıt, world time ve gürültü maliyetlerini açıkla.
5. **Sprint step 5:** Sığınak çatı/kapı/yağmur güvenliği için görselden bağımsız fiziksel doğrulama ve anlamlı kullanıcı mesajı seç.
6. **Sprint step 6:** Unloaded shelter üzerinde üretim/enerji/su simülasyonu için world time analytical advancement kararı yaz.
7. **Sprint step 7:** Fayda kaybında ownership/storage/claim korunumu ve recovery yolu belirt.
8. **Sprint step 8:** Yeni yapı etkisi için öncesi/sonrası gameplay kıyasını, kaynak ve tehdit maliyetleriyle tanımla.
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
| Domain sahibi | ShelterSite claim/installed socket ve ShelterProduction | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Üs claim zombies’i silmez veya invulnerability eklemez. | Partial mutation rollback |
| İstisna | Kısmi çatıda RainRoof/inside flag yanlışlıkla kuru kabul ediyor mu? | Explicit reject receipt |
| World/scene | Oyuncunun bir sefer sonrası üssündeki somut kazanım ve hedef yenileme | Source scene korunur |
| Test | Claim+storage+socket regression | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S019-EDGE-1:** Kısmi çatıda RainRoof/inside flag yanlışlıkla kuru kabul ediyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S019-EDGE-2:** Jeneratör kapalıyken crafting istasyonu çalışıyor mu; tasarım kuralları ne? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S019-EDGE-3:** Yeni depo iki farklı authoritative envanter açıyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Claim+storage+socket regression
- **Focused test theme 2:** Roof/wetness/power status transitions
- **Focused test theme 3:** Unloaded shelter fuel/jobs deterministic recovery

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Kısmi çatıda RainRoof/inside flag yanlışlıkla kuru kabul ediyor mu? Sorumlu alan: `shelter`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Jeneratör kapalıyken crafting istasyonu çalışıyor mu; tasarım kuralları ne? Sorumlu alan: `shelter`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Yeni depo iki farklı authoritative envanter açıyor mu? Sorumlu alan: `shelter`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S019
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P06](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
