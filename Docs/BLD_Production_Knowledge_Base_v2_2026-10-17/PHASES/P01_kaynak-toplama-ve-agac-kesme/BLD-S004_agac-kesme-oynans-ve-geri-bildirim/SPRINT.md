# BLD-S004 — Ağaç kesme oynanış ve geri bildirim

**Parent:** `P01 — Kaynak toplama ve ağaç kesme` · **Giriş:** S003. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Ağaç kesme oynanış ve geri bildirim.
- **Phase purpose:** Oynanabilir, kalıcı ve dengelenebilir kaynak toplama mekaniklerini oluştur.
- **FirstTownCore reference scenario:** FPS balta ile ağaç vurma, kaynak alma, zombi duyması ve devrilme görünümü.
- **Measurable phase exit:** G1: ağaç kesme/kaynak toplama + mülkiyet, yükleme ve gürültü doğrulaması.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Assets/LastSignal/Scripts/Runtime/Inventory/PlayerInventory.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Docs/Last_Signal_Docs_v0.2/sources/12_World_Pressure.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- Harvestable ağaç/prototype seçimi ve dekoratif flora ayrımı
- Balta/alet kimliği, durumu ve vuruş geometrisi
- WorldItem spawn/pickup ve Pressure noise kanalı

### Değiştirilemez davranışlar
- Dekoratif ağaçların tamamı sessizce harvestable sayılmaz.
- Ödül yalnız authoritative kabul edilen bir vuruş dizisinden çıkar.
- Tükenmiş resource node, unload/load sonrası ücretsiz yenilenmez.

### Domain anlaşmaları
- Kesim fiziksel devrilmesi isteniyorsa sınırlı görsel efekt olarak değerlendirilir.
- Vuruş sesi frame başına değil olay ritmine göre World Pressure kaydı üretir.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D025 | [Aç](DAYS/BLD-D025_balta-etkilesim-animasyonu.md) | Balta etkileşim animasyonu | Görsel vuruşla hasar zamanı uyumludur. |
| BLD-D026 | [Aç](DAYS/BLD-D026_isabet-ve-mesafe.md) | İsabet ve mesafe | Yanlış hedef veya boşluk hasar almaz. |
| BLD-D027 | [Aç](DAYS/BLD-D027_ses-ve-world-pressure.md) | Ses ve world pressure | 60/120 FPS farklı event sayısı yaratmaz. |
| BLD-D028 | [Aç](DAYS/BLD-D028_kesim-asamalar.md) | Kesim aşamaları | Devrilme fizik sistemi sınırsız rigidbody yaratmaz. |
| BLD-D029 | [Aç](DAYS/BLD-D029_dusen-kutukler.md) | Düşen kütükler | Eşya arazi altında veya collider içinde doğmaz. |
| BLD-D030 | [Aç](DAYS/BLD-D030_kaynak-toplama-ux.md) | Kaynak toplama UX | Oyuncu niçin alamadığını açıkça anlar. |
| BLD-D031 | [Aç](DAYS/BLD-D031_yerlesim-kaynak-uyumu.md) | Yerleşim kaynak uyumu | Ağaç kesimi yol/nav/geçişi bozmaz. |
| BLD-D032 | [Aç](DAYS/BLD-D032_manuel-test-teslimi.md) | Manuel test teslimi | Kullanıcı test çalıştırmadıkça PASS verilmez. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D025**, sprint entry → BLD-D026: Mevcut FPS/TPS silah grip sistemini bozmadan chop swing bağla. **Acceptance:** Görsel vuruşla hasar zamanı uyumludur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D026**, BLD-D025 → BLD-D027: Ağaç collider'ı, açı ve occlusion ile isabeti kontrol et. **Acceptance:** Yanlış hedef veya boşluk hasar almaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D027**, BLD-D026 → BLD-D028: Kesme/balta kaynaklı bounded NoiseEvent, ses aralığı ve kategori üret. **Acceptance:** 60/120 FPS farklı event sayısı yaratmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D028**, BLD-D027 → BLD-D029: Gövde yara durumu, geri bildirim ve kontrollü devrilme kararı. **Acceptance:** Devrilme fizik sistemi sınırsız rigidbody yaratmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D029**, BLD-D028 → BLD-D030: Statik hasat/düşüş animasyonu ve kaynak drop alanını belirle. **Acceptance:** Eşya arazi altında veya collider içinde doğmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D030**, BLD-D029 → BLD-D031: Odun ağırlığı, miktar, envanter doluluğu mesajlarını göster. **Acceptance:** Oyuncu niçin alamadığını açıkça anlar. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D031**, BLD-D030 → BLD-D032: Harvest node colliders ve sahne terrain/foliage exclusion kayıtlarını koru. **Acceptance:** Ağaç kesimi yol/nav/geçişi bozmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D032**, BLD-D031 → sprint gate: Kesim-kaydet-yükle, alet kırılması ve kesim sonrası loot kanıt komutlarını sun. **Acceptance:** Kullanıcı test çalıştırmadıkça PASS verilmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** FirstTownCore vegetation arasında gerçek kaynak düğümleri ile salt dekorasyonu kesin kimlik ve etiketten ayır.
2. **Sprint step 2:** Vuruş menzili, yönü, hız sınırı ve alet yetkisini player domain’den doğrulat.
3. **Sprint step 3:** Node damage/stage/remaining-yield state’ini event/receipt ile monoton ilerlet.
4. **Sprint step 4:** Accepted hit başına animasyon/ses/FX ayrımını, world pressure event hız sınırıyla bağla.
5. **Sprint step 5:** Node depletion’ı stable ID/cell delta olarak yaz; save/load sonrasında tekrar ücretsiz kaynak üretme.
6. **Sprint step 6:** Output spawn point, terrain clearance, container ownership ve full inventory davranışını belirt.
7. **Sprint step 7:** Eşzamanlı iki hit, broken tool ve death/unload yarışında işlemi tekrar oynatmadan sonlandır.
8. **Sprint step 8:** Görsel hasat kanıtını FPS kamera ve uzak orman silueti açısından ayrı kabul ettir.
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
| Domain sahibi | Harvestable ağaç/prototype seçimi ve dekoratif flora ayrımı | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Dekoratif ağaçların tamamı sessizce harvestable sayılmaz. | Partial mutation rollback |
| İstisna | Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur? | Explicit reject receipt |
| World/scene | FPS balta ile ağaç vurma, kaynak alma, zombi duyması ve devrilme görünümü | Source scene korunur |
| Test | Valid/invalid tool-hit matrix | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S004-EDGE-1:** Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S004-EDGE-2:** Envanter doluyken kaynaklar sahnede güvenli ve kalıcı kalır mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S004-EDGE-3:** Aynı ağaca tek frame içinde iki geçerli hit gelirse iki ödül oluşur mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Valid/invalid tool-hit matrix
- **Focused test theme 2:** Harvest+save/load+cell roundtrip
- **Focused test theme 3:** Inventory full + duplicate hit + FPS invariance

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur? Sorumlu alan: `harvest`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Envanter doluyken kaynaklar sahnede güvenli ve kalıcı kalır mı? Sorumlu alan: `harvest`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Aynı ağaca tek frame içinde iki geçerli hit gelirse iki ödül oluşur mu? Sorumlu alan: `harvest`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S004
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P01](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
