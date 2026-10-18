# BLD-S017 — Onarım, söküm ve yer değiştirme

**Parent:** `P06 — Sığınakla birleşen üretim ve bakım` · **Giriş:** S014,S016. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Onarım, söküm ve yer değiştirme.
- **Phase purpose:** İşlevsel istasyonlar, onarım/söküm, güvenlik, elektrik-su ve ilerleme.
- **FirstTownCore reference scenario:** İnşa edilmiş çitin onarılması, taşınması veya sökülmesi.
- **Measurable phase exit:** G6: yaşayan sığınak + kapsamlı craft ve maintenance loop.
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
| BLD-D129 | [Aç](DAYS/BLD-D129_build-tool-interface.md) | Build tool interface | Silah grip/reload state'ini bozmaz. |
| BLD-D130 | [Aç](DAYS/BLD-D130_repair-preview.md) | Repair preview | Eksik tool/stok anlaşılıyor. |
| BLD-D131 | [Aç](DAYS/BLD-D131_repair-transaction.md) | Repair transaction | Düşman hasarıyla yarışta kaynak iki kez gitmez. |
| BLD-D132 | [Aç](DAYS/BLD-D132_dismantle-transaction.md) | Dismantle transaction | Save/load sonrası sökülmüş parça geri gelmez. |
| BLD-D133 | [Aç](DAYS/BLD-D133_relocation.md) | Relocation | İçerik kaybolmaz ve bedava taşıma exploit yok. |
| BLD-D134 | [Aç](DAYS/BLD-D134_authorization.md) | Authorization | Kasaba bina parçaları yanlışlıkla sökülmez. |
| BLD-D135 | [Aç](DAYS/BLD-D135_cancel-undo.md) | Cancel/undo | Cancel işleminde hiçbir inventory değişmez. |
| BLD-D136 | [Aç](DAYS/BLD-D136_qa-matrix.md) | QA matrix | Kaynak invariants sağlam. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D129**, sprint entry → BLD-D130: Hammer/build mode, repair/dismantle seçimlerini aynı eylem sistemiyle bağla. **Acceptance:** Silah grip/reload state'ini bozmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D130**, BLD-D129 → BLD-D131: Hasarlı parça, gerekli item, beklenen condition artışı göster. **Acceptance:** Eksik tool/stok anlaşılıyor. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D131**, BLD-D130 → BLD-D132: Maliyet, araç condition, target expected revision aynı commit. **Acceptance:** Düşman hasarıyla yarışta kaynak iki kez gitmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D132**, BLD-D131 → BLD-D133: Parçayı kaldır, iade malzemesi üret, tombstone oluştur. **Acceptance:** Save/load sonrası sökülmüş parça geri gelmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D133**, BLD-D132 → BLD-D134: Sandık/istasyon taşınmasında doluluk ve bağlı modül kuralları. **Acceptance:** İçerik kaybolmaz ve bedava taşıma exploit yok. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D134**, BLD-D133 → BLD-D135: Yalnız oyuncuya ait/izinli yapı sökülür, POI varlıkları korunur. **Acceptance:** Kasaba bina parçaları yanlışlıkla sökülmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D135**, BLD-D134 → BLD-D136: Ghost ve confirm aşamasında kaçış; tamamlanan yapı geri alma kuralı. **Acceptance:** Cancel işleminde hiçbir inventory değişmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D136**, BLD-D135 → sprint gate: Dolu sandık, hasarlı çit, cancel/switch/save/load. **Acceptance:** Kaynak invariants sağlam. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

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
| World/scene | İnşa edilmiş çitin onarılması, taşınması veya sökülmesi | Source scene korunur |
| Test | Corner/window/door/foundation join permutations | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S017-EDGE-1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S017-EDGE-2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S017-EDGE-3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Sprint:** BLD-S017
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P06](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
