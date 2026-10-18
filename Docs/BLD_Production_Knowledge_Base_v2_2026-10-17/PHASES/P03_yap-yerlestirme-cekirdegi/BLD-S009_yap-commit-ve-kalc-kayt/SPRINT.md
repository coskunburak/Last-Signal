# BLD-S009 — Yapı commit ve kalıcı kayıt

**Parent:** `P03 — Yapı yerleştirme çekirdeği` · **Giriş:** S007,S008. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Yapı commit ve kalıcı kayıt.
- **Phase purpose:** Genel amaçlı ghost, çok katmanlı validasyon, atomik inşa ve kalıcılık.
- **FirstTownCore reference scenario:** Çit kurma onayı ile inventory çıkarma ve stable structure snapshot.
- **Measurable phase exit:** G3: geçerli/bozuk placement, kaynak korunumu ve save/load roundtrip.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Assets/LastSignal/Scripts/Runtime/Persistence/PersistentEntityId.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `Docs/Last_Signal_Docs_v0.2/sources/17_Save_Load_Migration.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- SaveSession save/capture/hydrate önkoşulları
- WorldCellManager loaded/unloaded delta ve readiness
- PersistentEntityId, tombstone, schema migration

### Değiştirilemez davranışlar
- Aynı structureId iki aktif yapı sahibi olamaz.
- Unloaded cell authoritative deltas kaybolamaz.
- Eski save construction bölümü olmadığı için bozuk sayılmaz.

### Domain anlaşmaları
- Sahne GameObject listesi save için global truth kabul edilmez.
- Migration yanlışsa original save değişmez; backup korunur.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D065 | [Aç](DAYS/BLD-D065_build-request-veri-paketi.md) | Build request veri paketi | Belirsiz veya tekrarlı komut reddedilir. |
| BLD-D066 | [Aç](DAYS/BLD-D066_atomic-kaynak-tuketimi.md) | Atomic kaynak tüketimi | Başarısız create malzeme eksiltmez. |
| BLD-D067 | [Aç](DAYS/BLD-D067_spawn-tescili.md) | Spawn tescili | Aynı konum/ID tekrar spawn edilmez. |
| BLD-D068 | [Aç](DAYS/BLD-D068_rollback-senaryolar.md) | Rollback senaryoları | İşlem ya tamamlanır ya geri alınır. |
| BLD-D069 | [Aç](DAYS/BLD-D069_placed-structure-snapshot.md) | Placed structure snapshot | Save/load yapı sayısı ve açısı eşittir. |
| BLD-D070 | [Aç](DAYS/BLD-D070_cell-owner-secimi.md) | Cell owner seçimi | Çift kayıt ve kaybolma yaşanmaz. |
| BLD-D071 | [Aç](DAYS/BLD-D071_migration-stratejisi.md) | Migration stratejisi | Eski save geriye uyumlu açılabilir. |
| BLD-D072 | [Aç](DAYS/BLD-D072_focused-test-altyaps.md) | Focused test altyapısı | Kullanıcı komutla çalıştırabilecek hedefli testler vardır. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D065**, sprint entry → BLD-D066: DefinitionId, zoneId, transform, expected inv revision, requestNonce. **Acceptance:** Belirsiz veya tekrarlı komut reddedilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D066**, BLD-D065 → BLD-D067: Validasyon sonrası OwnershipTransaction ile materyal commit uygula. **Acceptance:** Başarısız create malzeme eksiltmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D067**, BLD-D066 → BLD-D068: StructureInstance stable ID, ownership ve yapı registry oluştur. **Acceptance:** Aynı konum/ID tekrar spawn edilmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D068**, BLD-D067 → BLD-D069: Prefab bozuk, instantiate başarısız, envanter conflict ve scene unload durumları. **Acceptance:** İşlem ya tamamlanır ya geri alınır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D069**, BLD-D068 → BLD-D070: DefinitionId, world transform, owner zone, condition, revision, tombstone. **Acceptance:** Save/load yapı sayısı ve açısı eşittir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D070**, BLD-D069 → BLD-D071: Yapı cell sınırındaysa tek authoritative cell ve lease politikası. **Acceptance:** Çift kayıt ve kaybolma yaşanmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D071**, BLD-D070 → BLD-D072: Construction içermeyen save dosyasına boş section varsayımı tanımla. **Acceptance:** Eski save geriye uyumlu açılabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D072**, BLD-D071 → sprint gate: Ghost→commit, duplicate nonce, save/load, full inventory ve blocked spawn. **Acceptance:** Kullanıcı komutla çalıştırabilecek hedefli testler vardır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Entity/snapshot için stable identity, worldId, cellId, transform, definition revision ve tombstone şemasını yaz.
2. **Sprint step 2:** Capture öncesi busy/OwnershipTransaction/cell readiness ve world clock durumlarını denetle.
3. **Sprint step 3:** Yüklü sahne içeriği yerine authoritative world delta ve dynamic structure registry kullanımını tasarla.
4. **Sprint step 4:** Yeni construction section yoksa legacy save için boş koleksiyon migration davranışı belirle.
5. **Sprint step 5:** Insert/remove/relocate yapısını tek mutation receipt ve request nonce ile idempotent hale getir.
6. **Sprint step 6:** Crash, disk error ve load failure durumunda eski geçerli generation korunmasını sağlayacak protokol yaz.
7. **Sprint step 7:** Cross-cell neighbor lease/owner seçimini deterministik tek hücreye sabitle, çift hydrated structure’ı engelle.
8. **Sprint step 8:** Save/load, unload/reload, delete/tombstone ve duplicate identity için focused test fixture üret.
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
| Domain sahibi | SaveSession save/capture/hydrate önkoşulları | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Aynı structureId iki aktif yapı sahibi olamaz. | Partial mutation rollback |
| İstisna | Build commit sırasında exception ve disk write kesintisi olursa ikincil yapı kalır mı? | Explicit reject receipt |
| World/scene | Çit kurma onayı ile inventory çıkarma ve stable structure snapshot | Source scene korunur |
| Test | Cross-cell 50 unload/reload cycles | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S009-EDGE-1:** Build commit sırasında exception ve disk write kesintisi olursa ikincil yapı kalır mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S009-EDGE-2:** Delete→save→load sonrası tombstone sayesinde yapı geri gelir mi? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S009-EDGE-3:** Cell sınırında build iki kere serialize edilir mi? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Cross-cell 50 unload/reload cycles
- **Focused test theme 2:** Crash injection at transaction/save boundaries
- **Focused test theme 3:** Legacy fixture/missing definition/duplicate ID load

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Build commit sırasında exception ve disk write kesintisi olursa ikincil yapı kalır mı? Sorumlu alan: `persistence`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Delete→save→load sonrası tombstone sayesinde yapı geri gelir mi? Sorumlu alan: `persistence`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Cell sınırında build iki kere serialize edilir mi? Sorumlu alan: `persistence`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S009
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P03](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
