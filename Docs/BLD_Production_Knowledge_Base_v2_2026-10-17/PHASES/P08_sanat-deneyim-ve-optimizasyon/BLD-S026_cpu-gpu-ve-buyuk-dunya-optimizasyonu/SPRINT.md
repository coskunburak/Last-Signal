# BLD-S026 — CPU/GPU ve büyük dünya optimizasyonu

**Parent:** `P08 — Sanat, deneyim ve optimizasyon` · **Giriş:** S023–S025. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** CPU/GPU ve büyük dünya optimizasyonu.
- **Phase purpose:** Production-quality modeller, animasyon, ses, erişilebilir UI, profiler hedefleri.
- **FirstTownCore reference scenario:** 128+ yapı varken ghost/culling/nav, p95/p99 ve memory.
- **Measurable phase exit:** G8: görsel kabul + performans + erişilebilirlik doğrulaması.
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
| BLD-D201 | [Aç](DAYS/BLD-D201_profiling-baseline.md) | Profiling baseline | Donanım, görüntü ayarları ve koşullar belgelenir. |
| BLD-D202 | [Aç](DAYS/BLD-D202_ghost-cost.md) | Ghost cost | Placement UI per-frame GC üretmez. |
| BLD-D203 | [Aç](DAYS/BLD-D203_structure-renderer-budget.md) | Structure renderer budget | 200+ yapı parçasında kontrolsüz setpass artışı yok. |
| BLD-D204 | [Aç](DAYS/BLD-D204_physics-cost.md) | Physics cost | Yapı sonrası FixedUpdate spike yok. |
| BLD-D205 | [Aç](DAYS/BLD-D205_ai-nav-cost.md) | AI/nav cost | Zombiler sıkışmaz ve FPS dalgalanmaz. |
| BLD-D206 | [Aç](DAYS/BLD-D206_streaming-memory.md) | Streaming memory | Önceki yüklemede kaynak bırakmayan leak yok. |
| BLD-D207 | [Aç](DAYS/BLD-D207_build-stress-scenarios.md) | Build stress scenarios | Sonuçlar hedef platform için gerçek ölçümdür. |
| BLD-D208 | [Aç](DAYS/BLD-D208_optimization-gate.md) | Optimization gate | Bütçe aşılırsa güvenli kapsam azaltma vardır. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D201**, sprint entry → BLD-D202: 1080p Mac development + Windows hedefindeki frame-time profile planı. **Acceptance:** Donanım, görüntü ayarları ve koşullar belgelenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D202**, BLD-D201 → BLD-D203: Raycast/nonalloc overlap/material property updates profile. **Acceptance:** Placement UI per-frame GC üretmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D203**, BLD-D202 → BLD-D204: Modüler parçalarda batching, draw call ve LOD stratejisi. **Acceptance:** 200+ yapı parçasında kontrolsüz setpass artışı yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D204**, BLD-D203 → BLD-D205: BoxCollider, occlusion, dynamics ve tree fall bütçesi. **Acceptance:** Yapı sonrası FixedUpdate spike yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D205**, BLD-D204 → BLD-D206: Sonradan eklenen engel/dinamik nav update maliyeti. **Acceptance:** Zombiler sıkışmaz ve FPS dalgalanmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D206**, BLD-D205 → BLD-D207: Resident vs unloaded cell structure registry/memory kontrolü. **Acceptance:** Önceki yüklemede kaynak bırakmayan leak yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D207**, BLD-D206 → BLD-D208: 10/50/200 yapı, farklı canopy ve yağmur koşulları benchmark matrisi. **Acceptance:** Sonuçlar hedef platform için gerçek ölçümdür. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D208**, BLD-D207 → sprint gate: G8 metrik eşikleri, açık perf borçları ve sadeleştirme kararları. **Acceptance:** Bütçe aşılırsa güvenli kapsam azaltma vardır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | 128+ yapı varken ghost/culling/nav, p95/p99 ve memory | Source scene korunur |
| Test | Targeted edit/play tests owner-run | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S026-EDGE-1:** Test Unity Editor açıkken batchmode kilitleniyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S026-EDGE-2:** Focused PASS sonrası full regression FAIL ise false closure yapılır mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S026-EDGE-3:** Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S026
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P08](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
