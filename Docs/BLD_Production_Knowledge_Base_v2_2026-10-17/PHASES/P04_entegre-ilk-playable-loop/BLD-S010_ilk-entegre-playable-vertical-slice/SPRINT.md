# BLD-S010 — İlk entegre playable vertical slice

**Parent:** `P04 — Entegre ilk playable loop` · **Giriş:** S003–S009. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** İlk entegre playable vertical slice.
- **Phase purpose:** İlk bağımsız bütünleşik dikey dilimi bitir ve gerçek oyuncu değerini ölç.
- **FirstTownCore reference scenario:** Orman -> odun -> tahta -> çit -> save/load ilk 20–35 dk deneyim.
- **Measurable phase exit:** G4: ağaç → tahta → defter → çit → save/load uçtan uca kabul.
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
| BLD-D073 | [Aç](DAYS/BLD-D073_ilk-alan-secimi.md) | İlk alan seçimi | Mevcut staged ve ana sahne değişiklikleri korunur. |
| BLD-D074 | [Aç](DAYS/BLD-D074_pilot-asset-kit.md) | Pilot asset kit | Vendor kaynakları değişmez. |
| BLD-D075 | [Aç](DAYS/BLD-D075_egitimsel-isaretler.md) | Eğitimsel işaretler | Oyuncu uzun metin okumadan hedefi kavrar. |
| BLD-D076 | [Aç](DAYS/BLD-D076_ilk-yap-tarifi.md) | İlk yapı tarifi | Sistem yalnız çite hard-code değildir. |
| BLD-D077 | [Aç](DAYS/BLD-D077_uctan-uca-oynans.md) | Uçtan uca oynanış | Oyuncu 15–30 dakikada somut yapı üretir. |
| BLD-D078 | [Aç](DAYS/BLD-D078_save-load-dongusu.md) | Save/load döngüsü | Her obje/ödül ve stok miktarı korunur. |
| BLD-D079 | [Aç](DAYS/BLD-D079_oynanabilirlik-olcumu.md) | Oynanabilirlik ölçümü | Kullanıcı hangi adımın sürtünme yarattığını ölçebilir. |
| BLD-D080 | [Aç](DAYS/BLD-D080_g4-urun-karar.md) | G4 ürün kararı | G4 PASS değilse modüler ev kapsamına geçiş bloke edilir. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D073**, sprint entry → BLD-D074: FirstTownCore safehouse/forest edge içinde küçük, güvenli ve gerçek rota belirle. **Acceptance:** Mevcut staged ve ana sahne değişiklikleri korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D074**, BLD-D073 → BLD-D075: Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir. **Acceptance:** Vendor kaynakları değişmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D075**, BLD-D074 → BLD-D076: Ağaç→tezgâh→defter→inşa akışında okunabilir ipuçları sun. **Acceptance:** Oyuncu uzun metin okumadan hedefi kavrar. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D076**, BLD-D075 → BLD-D077: Bir çit ve en az bir işlevsel farklı yapı için recipe finalleştir. **Acceptance:** Sistem yalnız çite hard-code değildir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D077**, BLD-D076 → BLD-D078: Kes, topla, üret, preview, yerleştir, iptal et, tekrar dene. **Acceptance:** Oyuncu 15–30 dakikada somut yapı üretir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D078**, BLD-D077 → BLD-D079: Üç yapı kur, bir geçersiz önizlemeyi iptal et, kaydet ve geri gel kontrol et. **Acceptance:** Her obje/ödül ve stok miktarı korunur. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D079**, BLD-D078 → BLD-D080: En az 3 bağımsız oynanış denemesinde süre/ret nedenini kaydet. **Acceptance:** Kullanıcı hangi adımın sürtünme yarattığını ölçebilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D080**, BLD-D079 → sprint gate: İlk loop keyif/performans/ret safety sonuçlarını değerlendir. **Acceptance:** G4 PASS değilse modüler ev kapsamına geçiş bloke edilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | Orman -> odun -> tahta -> çit -> save/load ilk 20–35 dk deneyim | Source scene korunur |
| Test | Corner/window/door/foundation join permutations | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S010-EDGE-1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S010-EDGE-2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S010-EDGE-3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S010
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P04](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
