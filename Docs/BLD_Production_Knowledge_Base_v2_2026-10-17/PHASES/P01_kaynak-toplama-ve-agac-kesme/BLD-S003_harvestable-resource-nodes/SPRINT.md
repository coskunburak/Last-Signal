# BLD-S003 — Harvestable resource nodes

**Parent:** `P01 — Kaynak toplama ve ağaç kesme` · **Giriş:** S002. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Harvestable resource nodes.
- **Phase purpose:** Oynanabilir, kalıcı ve dengelenebilir kaynak toplama mekaniklerini oluştur.
- **FirstTownCore reference scenario:** İlk seçilmiş pine/birch ağaçtan odun, dolu envanter ve tekrar yükleme.
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
| BLD-D017 | [Aç](DAYS/BLD-D017_kaynak-taksonomisi.md) | Kaynak taksonomisi | Katalog ID çakışması ve bedava kaynak dönüşü yok. |
| BLD-D018 | [Aç](DAYS/BLD-D018_harvest-node-modeli.md) | Harvest node modeli | Bir kez kesilen ağacın durumu aynı yüklemede korunur. |
| BLD-D019 | [Aç](DAYS/BLD-D019_agac-secim-politikas.md) | Ağaç seçim politikası | Orman görselleri istemeden tüketilmez. |
| BLD-D020 | [Aç](DAYS/BLD-D020_etkilesim-mesafesi.md) | Etkileşim mesafesi | Duvar arkasından veya uzaktan kesim reddedilir. |
| BLD-D021 | [Aç](DAYS/BLD-D021_arac-yetkinligi.md) | Araç yetkinliği | Tool yoksa işlem açıklanabilir ret alır. |
| BLD-D022 | [Aç](DAYS/BLD-D022_hit-harvest-dogrulamas.md) | Hit/harvest doğrulaması | Tek vuruş yalnız tek hasar olayı üretir. |
| BLD-D023 | [Aç](DAYS/BLD-D023_spawn-ve-envanter-sahipligi.md) | Spawn ve envanter sahipliği | Full inventory durumunda eşya kaybolmaz. |
| BLD-D024 | [Aç](DAYS/BLD-D024_kalc-node-durumu.md) | Kalıcı node durumu | Save/load sonrası ağaç kendiliğinden yeniden büyümez. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D017**, sprint entry → BLD-D018: Log, branch, plank, stone, scrap, rope/fastener örneklerini kategori ve kütleyle eşle. **Acceptance:** Katalog ID çakışması ve bedava kaynak dönüşü yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D018**, BLD-D017 → BLD-D019: ResourceNodeDefinition/State ile sağlık, verim, cooldown, tool tanımla. **Acceptance:** Bir kez kesilen ağacın durumu aynı yüklemede korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D019**, BLD-D018 → BLD-D020: Her dekoratif ağacı kesilebilir kılma; seçilen ağacı özel node olarak etiketle. **Acceptance:** Orman görselleri istemeden tüketilmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D020**, BLD-D019 → BLD-D021: Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle. **Acceptance:** Duvar arkasından veya uzaktan kesim reddedilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D021**, BLD-D020 → BLD-D022: Balta türü, durability ve kesme çarpanlarını mevcut item yapısıyla bağla. **Acceptance:** Tool yoksa işlem açıklanabilir ret alır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D022**, BLD-D021 → BLD-D023: Aynı vuruşun iki kez sayılmasını, negatif sağlık ve eş zamanlı etkileşimi engelle. **Acceptance:** Tek vuruş yalnız tek hasar olayı üretir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D023**, BLD-D022 → BLD-D024: Kesim ödülünü mevcut WorldItem/InventoryContainer ownership işlemleriyle bağla. **Acceptance:** Full inventory durumunda eşya kaybolmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D024**, BLD-D023 → sprint gate: Kesilmiş node/stump/tombstone snapshot taslağını cell'e bağla. **Acceptance:** Save/load sonrası ağaç kendiliğinden yeniden büyümez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | İlk seçilmiş pine/birch ağaçtan odun, dolu envanter ve tekrar yükleme | Source scene korunur |
| Test | Valid/invalid tool-hit matrix | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S003-EDGE-1:** Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S003-EDGE-2:** Envanter doluyken kaynaklar sahnede güvenli ve kalıcı kalır mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S003-EDGE-3:** Aynı ağaca tek frame içinde iki geçerli hit gelirse iki ödül oluşur mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S003
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P01](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
