# BLD-S015 — Küçük barınak oynanabilir demo

**Parent:** `P05 — Modüler ev ve yapı mekaniği` · **Giriş:** S011–S014. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Küçük barınak oynanabilir demo.
- **Phase purpose:** Temel, duvar, kapı, çatı ve küçük barınak kurulumu; oynanabilirlik ve fiziksel mantık.
- **FirstTownCore reference scenario:** Orman kenarında dört duvar/kapı/çatı ile yürünebilir küçük barınak.
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
| BLD-D113 | [Aç](DAYS/BLD-D113_ev-blueprint-seti.md) | Ev blueprint seti | Mevcut kit ile çatılı yapı kurulabilir. |
| BLD-D114 | [Aç](DAYS/BLD-D114_kapsaml-builder-ux.md) | Kapsamlı builder UX | Oyuncu parçaları elle hizalamaktan yorulmaz. |
| BLD-D115 | [Aç](DAYS/BLD-D115_interior-viability.md) | Interior viability | İçeride sıkışma veya clipping yok. |
| BLD-D116 | [Aç](DAYS/BLD-D116_rain-and-shelter.md) | Rain and shelter | Oyuncu kurduğu çatı altında kuru kalabilir. |
| BLD-D117 | [Aç](DAYS/BLD-D117_save-load-integration.md) | Save/load integration | Kapı ve bağlı elemanları rastgele kaybolmaz. |
| BLD-D118 | [Aç](DAYS/BLD-D118_surface-polish.md) | Surface polish | Sadece gri primitive'lerle demo kabul edilmez. |
| BLD-D119 | [Aç](DAYS/BLD-D119_manual-experience-pass.md) | Manual experience pass | Efor/oyuncu ödülü dengelenir. |
| BLD-D120 | [Aç](DAYS/BLD-D120_g5-kabul-raporu.md) | G5 kabul raporu | G5 onayı yoksa ilave yapı çeşitliliğine geçilmez. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D113**, sprint entry → BLD-D114: 2x2/3x3 foundation, duvar, kapı, çatı için ilk plan oluştur. **Acceptance:** Mevcut kit ile çatılı yapı kurulabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D114**, BLD-D113 → BLD-D115: Eğik zemin, çit, temel snap, giriş clearance testleri. **Acceptance:** Oyuncu parçaları elle hizalamaktan yorulmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D115**, BLD-D114 → BLD-D116: Player capsule, kamera, yatak/depo yaklaşımı ve içeride görüş testleri. **Acceptance:** İçeride sıkışma veya clipping yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D116**, BLD-D115 → BLD-D117: Built roof için RainRoof/query entegrasyonunu güvenli adapt et. **Acceptance:** Oyuncu kurduğu çatı altında kuru kalabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D117**, BLD-D116 → BLD-D118: Herkesin kendi stable ID/struct graph ile restore denemesi. **Acceptance:** Kapı ve bağlı elemanları rastgele kaybolmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D118**, BLD-D117 → BLD-D119: Modüler bağlantı dikişleri ve materyal standartlarını düzelt. **Acceptance:** Sadece gri primitive'lerle demo kabul edilmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D119**, BLD-D118 → BLD-D120: Tam bir barınağı hasatla toplayıp kurma süresini ölç. **Acceptance:** Efor/oyuncu ödülü dengelenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D120**, BLD-D119 → sprint gate: Inşa edilebilir ev/yağmur/kapı/perf akışını raporla. **Acceptance:** G5 onayı yoksa ilave yapı çeşitliliğine geçilmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | Orman kenarında dört duvar/kapı/çatı ile yürünebilir küçük barınak | Source scene korunur |
| Test | Corner/window/door/foundation join permutations | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S015-EDGE-1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S015-EDGE-2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S015-EDGE-3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S015
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P05](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
