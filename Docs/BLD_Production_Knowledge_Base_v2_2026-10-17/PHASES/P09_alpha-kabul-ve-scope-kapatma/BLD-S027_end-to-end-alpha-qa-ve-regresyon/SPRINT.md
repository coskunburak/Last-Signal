# BLD-S027 — End-to-end alpha QA ve regresyon

**Parent:** `P09 — Alpha kabul ve scope kapatma` · **Giriş:** S010,S015,S019,S022,S026. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** End-to-end alpha QA ve regresyon.
- **Phase purpose:** Sistemlerin birlikte çalışmasını kanıtla, stabilizasyon ve ürün kararını ver.
- **FirstTownCore reference scenario:** İlk komple oyun süreci, death/reload, legacy save ve hardware QA.
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
| BLD-D209 | [Aç](DAYS/BLD-D209_acceptance-matrix-freeze.md) | Acceptance matrix freeze | Kritik gereksinim atlanmaz. |
| BLD-D210 | [Aç](DAYS/BLD-D210_complete-gameplay-route.md) | Complete gameplay route | Eşya/malzeme/yapı state bütünlüğü korunur. |
| BLD-D211 | [Aç](DAYS/BLD-D211_death-recovery-tests.md) | Death/recovery tests | Ölüm bedeli dupe/kayıp oluşturmaz. |
| BLD-D212 | [Aç](DAYS/BLD-D212_concurrent-state-tests.md) | Concurrent state tests | Double spend ve yarım save yok. |
| BLD-D213 | [Aç](DAYS/BLD-D213_character-world-regressions.md) | Character/world regressions | Unrelated S013–S023 sorunları yeniden açılmaz. |
| BLD-D214 | [Aç](DAYS/BLD-D214_visual-matrix.md) | Visual matrix | Kullanıcı görsel kabulü ayrı ve açık. |
| BLD-D215 | [Aç](DAYS/BLD-D215_hardware-test.md) | Hardware test | Mac PASS Windows PASS anlamına gelmez. |
| BLD-D216 | [Aç](DAYS/BLD-D216_alpha-issue-triage.md) | Alpha issue triage | Bloklayıcılar kapanmadan G9'a ilerlenmez. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D209**, sprint entry → BLD-D210: Must requirements, evidence paths ve expected test behavior'ı kilitle. **Acceptance:** Kritik gereksinim atlanmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D210**, BLD-D209 → BLD-D211: Forest→inventory→workbench→notebook→build→sleep→reload. **Acceptance:** Eşya/malzeme/yapı state bütünlüğü korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D211**, BLD-D210 → BLD-D212: Build input sırasında ölüm, crafting pending, dismantle ve restore. **Acceptance:** Ölüm bedeli dupe/kayıp oluşturmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D212**, BLD-D211 → BLD-D213: Craft+building same resources, save during action, scene unload. **Acceptance:** Double spend ve yarım save yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D213**, BLD-D212 → BLD-D214: FPS/TPS grip, melee, movement, zombies, vehicle, NavMesh. **Acceptance:** Unrelated S013–S023 sorunları yeniden açılmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D214**, BLD-D213 → BLD-D215: Day, sunset, dusk, rain, interior, forest UI/structure görüntüleri. **Acceptance:** Kullanıcı görsel kabulü ayrı ve açık. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D215**, BLD-D214 → BLD-D216: Windows build, FPS, memory, input, shader compilation. **Acceptance:** Mac PASS Windows PASS anlamına gelmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D216**, BLD-D215 → sprint gate: S0/S1 blocker, S2 polish, S3 backlogs ve reproduction linkleri. **Acceptance:** Bloklayıcılar kapanmadan G9'a ilerlenmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | İlk komple oyun süreci, death/reload, legacy save ve hardware QA | Source scene korunur |
| Test | Targeted edit/play tests owner-run | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S027-EDGE-1:** Test Unity Editor açıkken batchmode kilitleniyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S027-EDGE-2:** Focused PASS sonrası full regression FAIL ise false closure yapılır mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S027-EDGE-3:** Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S027
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P09](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
