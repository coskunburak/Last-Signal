# BLD-S008 — Yeşil-kırmızı ghost ve etkileşim

**Parent:** `P03 — Yapı yerleştirme çekirdeği` · **Giriş:** S007. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Yeşil-kırmızı ghost ve etkileşim.
- **Phase purpose:** Genel amaçlı ghost, çok katmanlı validasyon, atomik inşa ve kalıcılık.
- **FirstTownCore reference scenario:** Yapı ghost’u serbest kamera/rotasyonda yeşil-kırmızı ve ret nedeni.
- **Measurable phase exit:** G3: geçerli/bozuk placement, kaynak korunumu ve save/load roundtrip.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSocket.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Docs/Last_Signal_Docs_v0.2/sources/13_World_Streaming_POI.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- BuildDefinition prototype footprint, anchor ve zone policy
- Physics.Raycast/OverlapBoxNonAlloc ve layer/trigger filtering
- Door/vehicle/player spawn/nav clearances

### Değiştirilemez davranışlar
- Kırmızı preview materyal tüketmez.
- Aynı transform ve same world snapshot aynı validation result üretmeli.
- Görsel ghost collider, AI veya persistent entity oluşturamaz.

### Domain anlaşmaları
- Green/red preview yalnız presentation; ConstructionService authoritative confirm tekrar kontrol eder.
- Physics layers user project gerçeğiyle denetlenir, sabit sayılar körlemesine kopyalanmaz.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D057 | [Aç](DAYS/BLD-D057_ghost-prefab-ayrm.md) | Ghost prefab ayrımı | Ghost gameplay event veya ses üretmez. |
| BLD-D058 | [Aç](DAYS/BLD-D058_yesil-krmz-malzeme.md) | Yeşil/kırmızı malzeme | Renderer kırılmaz, vendor materyal clone spam olmaz. |
| BLD-D059 | [Aç](DAYS/BLD-D059_rotation-controls.md) | Rotation controls | Döndürme işlemi stabil ve okunaklıdır. |
| BLD-D060 | [Aç](DAYS/BLD-D060_terrain-hizalama.md) | Terrain hizalama | Çit zemine gömülmez veya havada kalmaz. |
| BLD-D061 | [Aç](DAYS/BLD-D061_snap-noktalar.md) | Snap noktaları | Komşu parçalara yanlış ölçekli snap yapılmaz. |
| BLD-D062 | [Aç](DAYS/BLD-D062_gecersizlik-acklamas.md) | Geçersizlik açıklaması | Kullanıcı rengi görmeden hatayı öğrenir. |
| BLD-D063 | [Aç](DAYS/BLD-D063_frame-cost.md) | Frame cost | Preview aşaması sürekli GC üretmez. |
| BLD-D064 | [Aç](DAYS/BLD-D064_iptal-yasam-dongusu.md) | İptal/yaşam döngüsü | Oyunda hayalet prefab takılı kalmaz. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D057**, sprint entry → BLD-D058: Gerçek prefab collider/AI/script çalıştırmadan preview mesh türet. **Acceptance:** Ghost gameplay event veya ses üretmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D058**, BLD-D057 → BLD-D059: URP uyumlu saydam materyal ve obje sınıfına göre outline uygula. **Acceptance:** Renderer kırılmaz, vendor materyal clone spam olmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D059**, BLD-D058 → BLD-D060: Mouse wheel/R veya atanmış kontrollerle 90°/serbest açı politikasını uygula. **Acceptance:** Döndürme işlemi stabil ve okunaklıdır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D060**, BLD-D059 → BLD-D061: Preview ground normal/elevation, yapı tipi için tolerans. **Acceptance:** Çit zemine gömülmez veya havada kalmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D061**, BLD-D060 → BLD-D062: Çit uçları, zemin kenarı ve farklı blueprint anchor tipini göster. **Acceptance:** Komşu parçalara yanlış ölçekli snap yapılmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D062**, BLD-D061 → BLD-D063: Kırmızı + ikon + kısa neden + alternatif yön önerisi sun. **Acceptance:** Kullanıcı rengi görmeden hatayı öğrenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D063**, BLD-D062 → BLD-D064: Raycast/revalidate rate, cache ve yalnız dirty state güncellemesi. **Acceptance:** Preview aşaması sürekli GC üretmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D064**, BLD-D063 → sprint gate: Pause, death, inventory close ve scene unload ghost temizliği. **Acceptance:** Oyunda hayalet prefab takılı kalmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Yapı prefabının raycast anchor, footprint, normal ve rotation toleranslarını prefab metadata ile doğrula.
2. **Sprint step 2:** İnşa iznini zone, POI, cell, player/vehicle, door ve nav koridor kurallarıyla sırayla değerlendir.
3. **Sprint step 3:** OverlapBoxNonAlloc ve Physics.Raycast filtresini gerçek Layers/Trigger düzeninden türet.
4. **Sprint step 4:** Ghost’u yalnız render-only tut; component side effects, collider ve persistent ID üretimini yasakla.
5. **Sprint step 5:** Geçerli/ret nedenini structured enum + açıklama + ikon + yeşil/kırmızı görünümde göster.
6. **Sprint step 6:** Rotasyon, yükseklik/snap değişiminde preview invalidation ve deterministic result uygulamasını tasarla.
7. **Sprint step 7:** Confirm anında inventory ve world revision değişmişse aynı sorguyu yeniden çalıştır; stale preview’i reddet.
8. **Sprint step 8:** Cancel, pause, death, cell leave ve scene reload sırasında ghost/material geçici durumlarını temizle.
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
| Domain sahibi | BuildDefinition prototype footprint, anchor ve zone policy | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Kırmızı preview materyal tüketmez. | Partial mutation rollback |
| İstisna | Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı. | Explicit reject receipt |
| World/scene | Yapı ghost’u serbest kamera/rotasyonda yeşil-kırmızı ve ret nedeni | Source scene korunur |
| Test | Slope/overlap/door/road/vehicle rejection matrix | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S008-EDGE-1:** Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S008-EDGE-2:** Eğim sınırında preview geçerli ama confirm geçersiz olmamalı; son anda revalidate. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S008-EDGE-3:** Yerleştirme ortasında world cell unload olursa ghost kapanmalı. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Slope/overlap/door/road/vehicle rejection matrix
- **Focused test theme 2:** Rapid rotate + edge-of-zone + pause/cancel
- **Focused test theme 3:** Ghost leaks, GC budget and deterministic result

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı. Sorumlu alan: `placement`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Eğim sınırında preview geçerli ama confirm geçersiz olmamalı; son anda revalidate. Sorumlu alan: `placement`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Yerleştirme ortasında world cell unload olursa ghost kapanmalı. Sorumlu alan: `placement`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S008
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P03](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
