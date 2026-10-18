# BLD-S020 — Kaynak yenilenmesi ve ekoloji

**Parent:** `P07 — Dünya ekonomisi ve güvenilir kayıt` · **Giriş:** S019. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Kaynak yenilenmesi ve ekoloji.
- **Phase purpose:** Yenilenme, kaynak dengesi, unloaded cell, uzun kayıt zinciri ve recovery.
- **FirstTownCore reference scenario:** Ormandan kaynak toplanması sonrası kalan/depleted node ve makul dönüş.
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
| BLD-D153 | [Aç](DAYS/BLD-D153_kaynak-scarcity.md) | Kaynak scarcity | Oyuncu loot RNG yüzünden softlock olmaz. |
| BLD-D154 | [Aç](DAYS/BLD-D154_regrowth-policy.md) | Regrowth policy | Yenilenme world-time deterministik ve görünür. |
| BLD-D155 | [Aç](DAYS/BLD-D155_forest-art-compatibility.md) | Forest art compatibility | Ağaç kesimi 500m ormanı hasarlı sahneye çevirmeden çalışır. |
| BLD-D156 | [Aç](DAYS/BLD-D156_world-pressure-integration.md) | World Pressure integration | Kayıt sonrası hayalet ses tekrar oluşmaz. |
| BLD-D157 | [Aç](DAYS/BLD-D157_respawn-ownership.md) | Respawn/ownership | Harvest reward tekrar yüklemede çoğalmaz. |
| BLD-D158 | [Aç](DAYS/BLD-D158_economy-fallback.md) | Economy fallback | Bir bölge boşalsa da oyuncu oyunu bitirebilir. |
| BLD-D159 | [Aç](DAYS/BLD-D159_resource-telemetry.md) | Resource telemetry | Grind ve exploit veriyle görülebilir. |
| BLD-D160 | [Aç](DAYS/BLD-D160_biome-footprint.md) | Biome footprint | Zemin/vegetation pipeline'ı kırılmaz. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D153**, sprint entry → BLD-D154: Finite tree/stone nodes, alternate sites ve guaranteed critical supply kuralları. **Acceptance:** Oyuncu loot RNG yüzünden softlock olmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D154**, BLD-D153 → BLD-D155: Ağaç yeniden büyüme isteği/uygulama maliyetini ölç; MVP kapalı olabilir. **Acceptance:** Yenilenme world-time deterministik ve görünür. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D155**, BLD-D154 → BLD-D156: Harvestable node ile görsel foliage ayrı yönetim. **Acceptance:** Ağaç kesimi 500m ormanı hasarlı sahneye çevirmeden çalışır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D156**, BLD-D155 → BLD-D157: Chop/build/dismantle ses olaylarını bounded cadence ile bağla. **Acceptance:** Kayıt sonrası hayalet ses tekrar oluşmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D157**, BLD-D156 → BLD-D158: Drop ve item tombstone'ları aynı harvested node'a bağla. **Acceptance:** Harvest reward tekrar yüklemede çoğalmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D158**, BLD-D157 → BLD-D159: Yedek ahşap kaynağı, görev kritik supply ve recovery yöntemi. **Acceptance:** Bir bölge boşalsa da oyuncu oyunu bitirebilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D159**, BLD-D158 → BLD-D160: Saat başı kaynak girişi/gideri ve yapı tercihlerini izle. **Acceptance:** Grind ve exploit veriyle görülebilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D160**, BLD-D159 → sprint gate: Kesilebilir ağaç oranı ve performance cap düzenini doğrula. **Acceptance:** Zemin/vegetation pipeline'ı kırılmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | Ormandan kaynak toplanması sonrası kalan/depleted node ve makul dönüş | Source scene korunur |
| Test | Source/sink conservation spreadsheet | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S020-EDGE-1:** Bütün yakın ağaçları kestikten sonra başlangıç barınağı yapılabiliyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S020-EDGE-2:** Yıkım/söküm kaynak iadesi sonsuz pozitif döngü yaratıyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S020-EDGE-3:** Uzun oturumda aynı bölgede sonsuz odun spawn oluyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S020
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P07](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
