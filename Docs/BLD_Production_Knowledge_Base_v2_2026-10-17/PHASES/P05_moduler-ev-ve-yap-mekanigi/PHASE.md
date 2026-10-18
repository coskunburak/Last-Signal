# P05 — Modüler ev ve yapı mekaniği

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 5
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** Temel, duvar, kapı, çatı ve küçük barınak kurulumu; oynanabilirlik ve fiziksel mantık.
- **G5 exit:** G5: küçük modüler barınak inşası, dolaşım, kapı, barınak durumu.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Önceki phase `P04` gate evidence gerektirir; istisna owner tarafından ADR olarak yazılmalı.
- İncelenen branch/commit/dirty state ve prefab/scene checkpoints güncel olmalı.
- Her sprintte source/owner ayrımı yapılmalı; mevcut sistemler clone edilmemeli.
- Altyapı için yalnız ilgili GDD veya runtime sembollerinin ihtiyaç duyulan bölümü okunmalı.
- Sprintte yeni binary asset gerekiyorsa lisans/URP/pivot/LOD/authoring doğrulanmalı.

### Bu phase için kaynak referanslar
- `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSocket.cs`
- `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs`
- `Docs/Last_Signal_Docs_v0.2/sources/13_World_Streaming_POI.md`

## 3. Sprint breakdown ve phase sonucu

| Sprint | Görev | Günlük kartlar | Kanıt türü |
|---|---|---|---|
| [BLD-S011](BLD-S011_temel-ve-zemin-yap-seti/SPRINT.md) | Temel ve zemin yapı seti | BLD-D081–BLD-D088 | Change diff + focused QA + gerekli game capture |
| [BLD-S012](BLD-S012_moduler-duvar-ve-snap-graph/SPRINT.md) | Modüler duvar ve snap graph | BLD-D089–BLD-D096 | Change diff + focused QA + gerekli game capture |
| [BLD-S013](BLD-S013_kap-pencere-ve-cat-paketi/SPRINT.md) | Kapı, pencere ve çatı paketi | BLD-D097–BLD-D104 | Change diff + focused QA + gerekli game capture |
| [BLD-S014](BLD-S014_yap-dayanm-baglantlar-insa-guvenligi/SPRINT.md) | Yapı dayanımı, bağlantılar, inşa güvenliği | BLD-D105–BLD-D112 | Change diff + focused QA + gerekli game capture |
| [BLD-S015](BLD-S015_kucuk-barnak-oynanabilir-demo/SPRINT.md) | Küçük barınak oynanabilir demo | BLD-D113–BLD-D120 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S011` giriş: S010.; çıkış: Çakışma ve jitter yok.
- `BLD-S012` giriş: S011.; çıkış: Tekrar yüklemede gap/rotation farkı yok.
- `BLD-S013` giriş: S012.; çıkış: Dünya restore edilince işlev korunur.
- `BLD-S014` giriş: S012,S013.; çıkış: Bağlantılar tutarlıdır.
- `BLD-S015` giriş: S011–S014.; çıkış: G5 onayı yoksa ilave yapı çeşitliliğine geçilmez.

## 4. Phase implementation strategy

1. **Phase operation 1:** Modüler 1m/2m/4m geometry kararını mevcut asset ölçüleri ve 1 Unity unit = 1 metre sözleşmesiyle karşılaştır.
2. **Phase operation 2:** Prefab pivot, snapping anchor, support/connection metadata ve görsel collider ayrımını doğrula.
3. **Phase operation 3:** Temel→duvar→açıklık→çatı bağlarını uyumlu blueprint kümesiyle tasarla, kusurlu kombinasyonu reddet.
4. **Phase operation 4:** Kapı/giriş/player ve araç clearance alanlarını örnek staging sahnesinde boyutlarla kaydet.
5. **Phase operation 5:** Yapının sağlamlık, bağlantı ve özelliği için yalnız gerekli stable state alanlarını öner.
6. **Phase operation 6:** Roof ve shelter/wetness kararlarını mevcut WorldClock/RainRoof kurallarına bağla; yeni weather authority kurma.
7. **Phase operation 7:** Yıkım ve onarımda ilgili graph/physics/nav etkisini bounded işle, performans regresyonu doğurma.
8. **Phase operation 8:** Gece/yağmur/FPS görüş açısı ve döndürme+save/load açısından gerçek gameplay kanıtını planla.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- 1 Unity unit = 1 metre; karakter ve araç boyutları sabit baseline referansıdır.
- Yapıların snap koordinatı render mesh değil authoring anchor üzerinden belirlenir.
- Kritik geçişleri inşa kapatamaz; yapı sökülünce geometry/nav tutarlı yenilenir.
- Voxel veya ağır runtime physics structural simulation ilk scope dışında.
- Geometri authoring onarımında vendor prefab materyali doğrudan değiştirilmez.
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | Temel, duvar, kapı, çatı ve küçük barınak kurulumu; oynanabilirlik ve fiziksel mantık. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G5-RISK-1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G5-RISK-2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G5-RISK-3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **Happy path:** Oyuncu kaynakların sahne/dünya/sığınak faydasını gerçekten görebilir.
- **Invalid path:** İşlem reddi belirgin; item/resource/structure delta sıfırdır.
- **Repeat path:** Çift submit ve tekrar yükleme duplicate içerik doğurmaz.
- **Boundary path:** Cell/scene/readiness / owned state kararlı kalır.
- **Fallback path:** Kritik kaynağa erişim kaybı oyunu softlock’ta bırakmaz.

## 8. Manual test planı — testler kullanıcıda

1. Sprint sonlarında agent verified focused test names/commands yazacak, çalıştırmayacak.
2. Owner focused EditMode ve PlayMode, ardından integration/regression sonuçlarını döndürecek.
3. Asset authoring veya VisualSlice açılması gerekiyorsa owner ayrı native authoring adımını uygulayacak.
4. FAIL, source preserve evidence ve minimal fix/retest ile kapatılacak.
5. Save-sensitive phase backup/migration fixture olmadan G gate PASS alamaz.
6. Art-sensitive phase FPS/Game View gündüz, dusk, yağmur ve gece kabulünü owner alır.
7. Performance counters baseline ve candidate karşılaştırması olmadan kazanım iddia edilmez.
8. Sonra GDD karar ve backlog/replan güncellenir.

### Odaklı test alanları
- Corner/window/door/foundation join permutations
- Collision/nav walkable egress routes
- Build/repair/demolish/save/load roundtrip

## 9. Risk, kesme, rollback

- **RP05-1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP05-2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP05-3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **Destructive change:** Ana sahne veya vendor değişimi için doğrudan mutasyon yapılmaz; snapshot ve ayrı authorized promotion gerekir.
- **Budget:** İlk 8 kart x sprint bazlı estimasyon gerçek eforla her 2–3 sprintte revize edilir.
- **Scope cut:** Cosmetical variant → side utility → optional larger building; core inventory+save integrity kesilemez.

## 10. Phase exit gate — Definition of Done

- [ ] Sprintlerin kart durumları kanıta dayalı.
- [ ] Owner product decision/ADR gerekiyorsa tamam.
- [ ] GDD ile gerçek code contract çelişkileri kaydedildi.
- [ ] Scope creep/asset/animation drift açıkça değerlendirildi.
- [ ] Resource ownership, craft cancellation ve build invalid paths güvenli.
- [ ] Save/cell/entity mutation audit geçerli.
- [ ] User authored tests ve gerekli visual/UX acceptance PASS.
- [ ] Focused→integration→regression sırası completed veya eksik için BLOCKED.
- [ ] Performance ölçülmüş veya dürüstçe NOT_MEASURED.
- [ ] Sonraki phase için READY/BLOCKED ve gerçek gerekçe verildi.

## 11. Gate receipt / devir formu

- **Gate:** G5; **Phase:** `P05`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
