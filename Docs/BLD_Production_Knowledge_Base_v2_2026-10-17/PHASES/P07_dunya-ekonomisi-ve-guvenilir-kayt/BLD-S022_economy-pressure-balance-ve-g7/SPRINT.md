# BLD-S022 — Economy/pressure balance ve G7

**Parent:** `P07 — Dünya ekonomisi ve güvenilir kayıt` · **Giriş:** S019–S021. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Economy/pressure balance ve G7.
- **Phase purpose:** Yenilenme, kaynak dengesi, unloaded cell, uzun kayıt zinciri ve recovery.
- **FirstTownCore reference scenario:** İki farklı araç/rota ile aynı yapıya kaynak getirme ekonomisi.
- **Measurable phase exit:** G7: regresyon, migration, exploit ve uzun oturum testleri.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Docs/Last_Signal_Docs_v0.2/sources/12_World_Pressure.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Assets/LastSignal/Scripts/Runtime/Inventory/PlayerInventory.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- Resource source/sink spreadsheet ve loot tables
- Harvest node replenishment ve world depletion flags
- World Pressure/event pacing ve geriye dönüş seyahat maliyeti

### Değiştirilemez davranışlar
- Gerekli tarife RNG kaynaklı kalıcı softlock yaratamaz.
- Kaynak azalması noise decay ile karıştırılmaz.
- Grind, ilerlemeye kıyasla ölçüsüz tekrar üretmez.

### Domain anlaşmaları
- Zombi spawn sayısı doğrudan inşa sayısına orantılanmaz; pressure bütçesi geçerli.
- Denge sayıları başlangıç hipotezidir; kullanıcı playtest verisi olmadan PASS yazılmaz.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D169 | [Aç](DAYS/BLD-D169_resource-economy-table.md) | Resource economy table | Aynı materyal hiç düşünmeden farm edilmiyor. |
| BLD-D170 | [Aç](DAYS/BLD-D170_weapon-tool-tradeoffs.md) | Weapon/tool tradeoffs | Farklı aletler anlamlı karar yaratır. |
| BLD-D171 | [Aç](DAYS/BLD-D171_build-restrictions-tuning.md) | Build restrictions tuning | Reddedilen yerleşim adil/anlaşılır. |
| BLD-D172 | [Aç](DAYS/BLD-D172_threat-feedback.md) | Threat feedback | Oyuncu saldırıyı sebepsiz saymıyor. |
| BLD-D173 | [Aç](DAYS/BLD-D173_softlock-exploit-audit.md) | Softlock/exploit audit | Tespit edilen S0/S1 kapanmadan gate PASS olmaz. |
| BLD-D174 | [Aç](DAYS/BLD-D174_long-session-playtest.md) | Long session playtest | Oyuncu kendi sonraki hedefini söyleyebilir. |
| BLD-D175 | [Aç](DAYS/BLD-D175_performance-budget-update.md) | Performance budget update | Baseline delta ve hardware raporu var. |
| BLD-D176 | [Aç](DAYS/BLD-D176_g7-trade-off-decision.md) | G7 trade-off decision | Gereksiz grind çıkarılır veya açıkça ertelenir. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D169**, sprint entry → BLD-D170: Log/plank/scrap/fastener input-output ve expected sefer sayısı hesapla. **Acceptance:** Aynı materyal hiç düşünmeden farm edilmiyor. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D170**, BLD-D169 → BLD-D171: Balta dayanımı, kesme sesi ve çantaya taşınabilir hacmi dengele. **Acceptance:** Farklı aletler anlamlı karar yaratır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D171**, BLD-D170 → BLD-D172: Build radius/slope/clearance ve yapı maliyetini oyuncu geri bildirimiyle ayarla. **Acceptance:** Reddedilen yerleşim adil/anlaşılır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D172**, BLD-D171 → BLD-D173: Kesim/inşa sesleri ve zombie baskısı için kullanıcıya okunabilir ipuçları. **Acceptance:** Oyuncu saldırıyı sebepsiz saymıyor. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D173**, BLD-D172 → BLD-D174: Zero-cost craft, dupe, infinite regrow, item swap, reload to refund. **Acceptance:** Tespit edilen S0/S1 kapanmadan gate PASS olmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D174**, BLD-D173 → BLD-D175: Üç seferlik 90 dakikalık rota, farklı üs gelişimi seçimleri. **Acceptance:** Oyuncu kendi sonraki hedefini söyleyebilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D175**, BLD-D174 → BLD-D176: Dynamic structures + NPC nav/memory maliyet analizi. **Acceptance:** Baseline delta ve hardware raporu var. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D176**, BLD-D175 → sprint gate: Ekoloji/ekonomi kısıtları ve kanıt özetini onay için hazırla. **Acceptance:** Gereksiz grind çıkarılır veya açıkça ertelenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Her kaynağın ilk source, expected yield, taşıma maliyeti ve craft sink’ini ayrı satıra çıkar.
2. **Sprint step 2:** Kritik ilerleme tarifleri için deterministic obtain/recovery yolu yaz; RNG softlock’u yasakla.
3. **Sprint step 3:** Yenilenme politikasını dünya saati, cell unload ve prior depletion receipt ile bağla.
4. **Sprint step 4:** Tamir/söküm iadesini net kaynak çıkışı pozitif döngü üretmeyecek şekilde hesapla.
5. **Sprint step 5:** Kesim/gürültü etkisini Pressure API’na bounded event olarak bağla; hostile spawn’ı doğrudan tetikleme.
6. **Sprint step 6:** Oyuncunun 15/30/60 dakika hedeflerinde kaynak ve risk dengesini telemetri planına dök.
7. **Sprint step 7:** Kıtlık ve uzun yürüyüş eğlenceyi engelliyorsa azaltılacak content/tuning öğesini belirle.
8. **Sprint step 8:** Katlanarak artan envanter/üretim ve yeniden ziyaret durumlarında conservation testlerini belirt.
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
| Domain sahibi | Resource source/sink spreadsheet ve loot tables | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Gerekli tarife RNG kaynaklı kalıcı softlock yaratamaz. | Partial mutation rollback |
| İstisna | Bütün yakın ağaçları kestikten sonra başlangıç barınağı yapılabiliyor mu? | Explicit reject receipt |
| World/scene | İki farklı araç/rota ile aynı yapıya kaynak getirme ekonomisi | Source scene korunur |
| Test | Source/sink conservation spreadsheet | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S022-EDGE-1:** Bütün yakın ağaçları kestikten sonra başlangıç barınağı yapılabiliyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S022-EDGE-2:** Yıkım/söküm kaynak iadesi sonsuz pozitif döngü yaratıyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S022-EDGE-3:** Uzun oturumda aynı bölgede sonsuz odun spawn oluyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Source/sink conservation spreadsheet
- **Focused test theme 2:** 10/30/60-minute survival pacing sessions
- **Focused test theme 3:** Softlock and dismantle/refund farming

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Bütün yakın ağaçları kestikten sonra başlangıç barınağı yapılabiliyor mu? Sorumlu alan: `economy`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Yıkım/söküm kaynak iadesi sonsuz pozitif döngü yaratıyor mu? Sorumlu alan: `economy`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Uzun oturumda aynı bölgede sonsuz odun spawn oluyor mu? Sorumlu alan: `economy`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S022
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P07](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
