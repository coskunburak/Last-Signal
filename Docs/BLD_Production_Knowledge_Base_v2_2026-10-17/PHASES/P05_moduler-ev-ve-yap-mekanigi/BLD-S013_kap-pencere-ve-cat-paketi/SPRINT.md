# BLD-S013 — Kapı, pencere ve çatı paketi

**Parent:** `P05 — Modüler ev ve yapı mekaniği` · **Giriş:** S012. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Kapı, pencere ve çatı paketi.
- **Phase purpose:** Temel, duvar, kapı, çatı ve küçük barınak kurulumu; oynanabilirlik ve fiziksel mantık.
- **FirstTownCore reference scenario:** Kapılı duvar, pencere ve roof tiplerinde collision/rain tutarlılığı.
- **Measurable phase exit:** G5: küçük modüler barınak inşası, dolaşım, kapı, barınak durumu.
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
- Modüler prefab dimension/socket/snap metrikleri
- Collision, cover, roof/weather ve NavMesh impact
- Build placement domain stable ID, condition, graph

### Değiştirilemez davranışlar
- 1 Unity unit = 1 metre; karakter ve araç boyutları sabit baseline referansıdır.
- Yapıların snap koordinatı render mesh değil authoring anchor üzerinden belirlenir.
- Kritik geçişleri inşa kapatamaz; yapı sökülünce geometry/nav tutarlı yenilenir.

### Domain anlaşmaları
- Voxel veya ağır runtime physics structural simulation ilk scope dışında.
- Geometri authoring onarımında vendor prefab materyali doğrudan değiştirilmez.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D097 | [Aç](DAYS/BLD-D097_doorframe-parcalar.md) | Doorframe parçaları | Çerçeve giriş açıklığı karaktere uygundur. |
| BLD-D098 | [Aç](DAYS/BLD-D098_door-runtime-integration.md) | Door runtime integration | Yeni kapılar sığınak/world kapılarıyla tutarlı davranır. |
| BLD-D099 | [Aç](DAYS/BLD-D099_pencere-variant.md) | Pencere variant | İçeri görünmeyen boşluk/mesh çakışması yok. |
| BLD-D100 | [Aç](DAYS/BLD-D100_roof-modular-pieces.md) | Roof modular pieces | En az bir küçük evin çatısı kapanabilir. |
| BLD-D101 | [Aç](DAYS/BLD-D101_roof-overlap.md) | Roof overlap | Duvar ve çatıda görünür seam kabul standardı var. |
| BLD-D102 | [Aç](DAYS/BLD-D102_roof-shelter-flag.md) | Roof shelter flag | Çatı altında wetness state doğru değişir. |
| BLD-D103 | [Aç](DAYS/BLD-D103_kap-perf-politikas.md) | Kapı/perf politikası | Spawn/door blocked hataları regresyona alınır. |
| BLD-D104 | [Aç](DAYS/BLD-D104_save-gates.md) | Save gates | Dünya restore edilince işlev korunur. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D097**, sprint entry → BLD-D098: Kapı kasası ve açılır kapı modülleri ölçülerini çıkar. **Acceptance:** Çerçeve giriş açıklığı karaktere uygundur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D098**, BLD-D097 → BLD-D099: Mevcut DoorInteractable ya da uyumlu adaptör seç. **Acceptance:** Yeni kapılar sığınak/world kapılarıyla tutarlı davranır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D099**, BLD-D098 → BLD-D100: Kapalı/açık çerçeve, görüş ve collision politikası. **Acceptance:** İçeri görünmeyen boşluk/mesh çakışması yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D100**, BLD-D099 → BLD-D101: Tek eğim, çift eğim, mahya ve köşe sınırı başlangıç seti. **Acceptance:** En az bir küçük evin çatısı kapanabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D101**, BLD-D100 → BLD-D102: Çatı eğimi, tavandan yükseklik ve bitiş edge kontrolü. **Acceptance:** Duvar ve çatıda görünür seam kabul standardı var. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D102**, BLD-D101 → BLD-D103: Yağmurdan koruma için fiziksel üst yüzey/coverage doğrulaması. **Acceptance:** Çatı altında wetness state doğru değişir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D103**, BLD-D102 → BLD-D104: Kapı collider lifecycle ve nav carving/obstacle maliyetini sınırla. **Acceptance:** Spawn/door blocked hataları regresyona alınır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D104**, BLD-D103 → sprint gate: Açık/kapalı kapı, kırık/onarım, bağlı çatı roundtrip. **Acceptance:** Dünya restore edilince işlev korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Modüler 1m/2m/4m geometry kararını mevcut asset ölçüleri ve 1 Unity unit = 1 metre sözleşmesiyle karşılaştır.
2. **Sprint step 2:** Prefab pivot, snapping anchor, support/connection metadata ve görsel collider ayrımını doğrula.
3. **Sprint step 3:** Temel→duvar→açıklık→çatı bağlarını uyumlu blueprint kümesiyle tasarla, kusurlu kombinasyonu reddet.
4. **Sprint step 4:** Kapı/giriş/player ve araç clearance alanlarını örnek staging sahnesinde boyutlarla kaydet.
5. **Sprint step 5:** Yapının sağlamlık, bağlantı ve özelliği için yalnız gerekli stable state alanlarını öner.
6. **Sprint step 6:** Roof ve shelter/wetness kararlarını mevcut WorldClock/RainRoof kurallarına bağla; yeni weather authority kurma.
7. **Sprint step 7:** Yıkım ve onarımda ilgili graph/physics/nav etkisini bounded işle, performans regresyonu doğurma.
8. **Sprint step 8:** Gece/yağmur/FPS görüş açısı ve döndürme+save/load açısından gerçek gameplay kanıtını planla.
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
| Domain sahibi | Modüler prefab dimension/socket/snap metrikleri | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | 1 Unity unit = 1 metre; karakter ve araç boyutları sabit baseline referansıdır. | Partial mutation rollback |
| İstisna | Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? | Explicit reject receipt |
| World/scene | Kapılı duvar, pencere ve roof tiplerinde collision/rain tutarlılığı | Source scene korunur |
| Test | Corner/window/door/foundation join permutations | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S013-EDGE-1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S013-EDGE-2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S013-EDGE-3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Corner/window/door/foundation join permutations
- **Focused test theme 2:** Collision/nav walkable egress routes
- **Focused test theme 3:** Build/repair/demolish/save/load roundtrip

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? Sorumlu alan: `construction`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? Sorumlu alan: `construction`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? Sorumlu alan: `construction`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S013
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P05](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
