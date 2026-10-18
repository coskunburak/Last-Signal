# BLD-S011 — Temel ve zemin yapı seti

**Parent:** `P05 — Modüler ev ve yapı mekaniği` · **Giriş:** S010. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Temel ve zemin yapı seti.
- **Phase purpose:** Temel, duvar, kapı, çatı ve küçük barınak kurulumu; oynanabilirlik ve fiziksel mantık.
- **FirstTownCore reference scenario:** Eğimli zeminde foundation yerleştirme ve yükseklik farkları.
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
| BLD-D081 | [Aç](DAYS/BLD-D081_foundation-olcu-sistemi.md) | Foundation ölçü sistemi | Basit evi kapatmak için ölçü uyumsuzluğu yok. |
| BLD-D082 | [Aç](DAYS/BLD-D082_foundation-prefabs.md) | Foundation prefabs | Her tür collision+LOD+preview ayrı tanımlı. |
| BLD-D083 | [Aç](DAYS/BLD-D083_zemin-egimi-uyumu.md) | Zemin eğimi uyumu | Temel havada yüzmez veya toprağa gömülmez. |
| BLD-D084 | [Aç](DAYS/BLD-D084_terrain-clearance.md) | Terrain clearance | Sahne dekorunu sessizce silmez. |
| BLD-D085 | [Aç](DAYS/BLD-D085_yukseklik-ayar.md) | Yükseklik ayarı | Aralık dışı yükseklik net ret verir. |
| BLD-D086 | [Aç](DAYS/BLD-D086_foundation-link-id.md) | Foundation link ID | Save/load bağlantı şeması korunur. |
| BLD-D087 | [Aç](DAYS/BLD-D087_malzeme-dengeleme.md) | Malzeme dengeleme | Bir basit platform birkaç seferlik grind gerektirmez. |
| BLD-D088 | [Aç](DAYS/BLD-D088_test-bankas.md) | Test bankası | Çakışma ve jitter yok. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D081**, sprint entry → BLD-D082: 1m/2m/4m uyumlu başlangıç grid/snap ve modül ölçeklerini kilitle. **Acceptance:** Basit evi kapatmak için ölçü uyumsuzluğu yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D082**, BLD-D081 → BLD-D083: Düz temel, yükseltilmiş temel ve corner parçalarına karar ver. **Acceptance:** Her tür collision+LOD+preview ayrı tanımlı. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D083**, BLD-D082 → BLD-D084: Ayak yüksekliği, destek ayağı ve maximum slope politikası geliştir. **Acceptance:** Temel havada yüzmez veya toprağa gömülmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D084**, BLD-D083 → BLD-D085: Büyük taş, kök, yol ve mevcut world prop çakışmasını yönet. **Acceptance:** Sahne dekorunu sessizce silmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D085**, BLD-D084 → BLD-D086: Step nudge, ekran ipucu ve yerleştirme sınırları uygula. **Acceptance:** Aralık dışı yükseklik net ret verir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D086**, BLD-D085 → BLD-D087: Her temel için stable structure ID + bağlantı anchorları kaydet. **Acceptance:** Save/load bağlantı şeması korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D087**, BLD-D086 → BLD-D088: Tahta/scrap için maliyet ve taşıma ağırlığı belirle. **Acceptance:** Bir basit platform birkaç seferlik grind gerektirmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D088**, BLD-D087 → sprint gate: Tepelik, düz zemin, komşu temel ve border-edge senaryoları. **Acceptance:** Çakışma ve jitter yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | Eğimli zeminde foundation yerleştirme ve yükseklik farkları | Source scene korunur |
| Test | Corner/window/door/foundation join permutations | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S011-EDGE-1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S011-EDGE-2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S011-EDGE-3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S011
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P05](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
