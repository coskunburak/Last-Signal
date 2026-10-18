# BLD-S028 — Stabilizasyon, GDD güncelleme, teslim

**Parent:** `P09 — Alpha kabul ve scope kapatma` · **Giriş:** S027. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Stabilizasyon, GDD güncelleme, teslim.
- **Phase purpose:** Sistemlerin birlikte çalışmasını kanıtla, stabilizasyon ve ürün kararını ver.
- **FirstTownCore reference scenario:** Blocker çözümü, scope freeze ve sonraki release artıkları.
- **Measurable phase exit:** G9: alpha playtest, release scope ve teknik devir kararı.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Docs/Last_Signal_Docs_v0.2/sources/17_Save_Load_Migration.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Docs/Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- Planın phase gates ve değişiklik kayıtları
- S010/S023 mevcut automated test entry points
- GitHub branch/commit, owner manual testing policy

### Değiştirilemez davranışlar
- Agent manual test çalıştırmaz; kullanıcı evidence ile PASS verir.
- Sadece eski PASS yeni sürüm için otomatik PASS değildir.
- Kritik save/item duplication/regression açıkken gate kapatılamaz.

### Domain anlaşmaları
- Komut gerçek Tools/ test assembly dosyasından türetilir; hayalî --filter verilmez.
- Editor test, PlayMode, build, perf ve art gate birbirinden ayrıdır.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D217 | [Aç](DAYS/BLD-D217_critical-fix-sprint.md) | Critical fix sprint | Focused re-test ve full regression kanıtı olur. |
| BLD-D218 | [Aç](DAYS/BLD-D218_save-migration-signoff.md) | Save migration signoff | Kaydetme kaybı release blocker sayılır. |
| BLD-D219 | [Aç](DAYS/BLD-D219_content-cut-review.md) | Content cut review | Kritik player loop korunur. |
| BLD-D220 | [Aç](DAYS/BLD-D220_balance-final-pass.md) | Balance final pass | Prolonged grind ve exploitable farm yok. |
| BLD-D221 | [Aç](DAYS/BLD-D221_docs-governance.md) | Docs governance | GDD çalışan sistemlerle uyumlu ama geçmiş karar korunur. |
| BLD-D222 | [Aç](DAYS/BLD-D222_manual-qa-bundle.md) | Manual QA bundle | Kullanıcı her gate'i tekrar çalıştırabilir. |
| BLD-D223 | [Aç](DAYS/BLD-D223_release-readiness.md) | Release readiness | READY veya BLOCKED kanıtla ifade edilir. |
| BLD-D224 | [Aç](DAYS/BLD-D224_program-retrospective.md) | Program retrospective | Bir sonraki phase kapsamı tahmin değil veriyle belirlenir. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D217**, sprint entry → BLD-D218: S027'den kalan S0/S1 root-cause düzeltmeleri. **Acceptance:** Focused re-test ve full regression kanıtı olur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D218**, BLD-D217 → BLD-D219: Eski save, yeni structures ve rollback akışını son kez test et. **Acceptance:** Kaydetme kaybı release blocker sayılır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D219**, BLD-D218 → BLD-D220: Gerçek maliyete göre COULD/SHOULD kapsamını kes veya ertele. **Acceptance:** Kritik player loop korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D220**, BLD-D219 → BLD-D221: Kaynak oranları, üretim süreleri, geçerli alan retleri. **Acceptance:** Prolonged grind ve exploitable farm yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D221**, BLD-D220 → BLD-D222: LS-DOC-14, LS-DOC-17 ve plan değişiklik kartını güncelle. **Acceptance:** GDD çalışan sistemlerle uyumlu ama geçmiş karar korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D222**, BLD-D221 → BLD-D223: Tek komut akışları, screenshot seti, sample saves, evidence index. **Acceptance:** Kullanıcı her gate'i tekrar çalıştırabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D223**, BLD-D222 → BLD-D224: Open issues, perf ve Windows sonuçları, source state, ownership riski. **Acceptance:** READY veya BLOCKED kanıtla ifade edilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D224**, BLD-D223 → sprint gate: Yapılan iş, ertelenen özellikler, sonraki üretim ve asset planı. **Acceptance:** Bir sonraki phase kapsamı tahmin değil veriyle belirlenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** İlgili mevcut assembly, Tools runner ve test isimlerini yerel branch’te doğrulat.
2. **Sprint step 2:** Görevi component contract, regression ve gameplay etkisi olarak üç test düzeyine ayır.
3. **Sprint step 3:** Başarısızlık senaryoları için input fixture, initial snapshot ve beklenen state farkını yaz.
4. **Sprint step 4:** Kullanıcı tarafından çalıştırılacak en küçük focused test sırasını oluştur.
5. **Sprint step 5:** Focused sonuç PASS ise ilgili integration ve regression gate’ini ayrıca iste.
6. **Sprint step 6:** Source-preservation diff, asset GUID, save schema ve backup kontrollerini doğrulama listesine al.
7. **Sprint step 7:** Performance ölçümlerini median/p95/p99 CPU/GPU, GC ve memory bağlamında raporla.
8. **Sprint step 8:** Test çalıştırmadan, görsel owner kabulü olmadan veya eksik logla PASS yazılmasını engelle.
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
| Domain sahibi | Planın phase gates ve değişiklik kayıtları | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Agent manual test çalıştırmaz; kullanıcı evidence ile PASS verir. | Partial mutation rollback |
| İstisna | Test Unity Editor açıkken batchmode kilitleniyor mu? | Explicit reject receipt |
| World/scene | Blocker çözümü, scope freeze ve sonraki release artıkları | Source scene korunur |
| Test | Targeted edit/play tests owner-run | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S028-EDGE-1:** Test Unity Editor açıkken batchmode kilitleniyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S028-EDGE-2:** Focused PASS sonrası full regression FAIL ise false closure yapılır mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S028-EDGE-3:** Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Targeted edit/play tests owner-run
- **Focused test theme 2:** Source preservation and diff audit
- **Focused test theme 3:** Evidence integrity, perf counter and gate receipt review

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Test Unity Editor açıkken batchmode kilitleniyor mu? Sorumlu alan: `qa`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Focused PASS sonrası full regression FAIL ise false closure yapılır mı? Sorumlu alan: `qa`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi? Sorumlu alan: `qa`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S028
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P09](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
