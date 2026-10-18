# P03 — Yapı yerleştirme çekirdeği

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 3
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** Genel amaçlı ghost, çok katmanlı validasyon, atomik inşa ve kalıcılık.
- **G3 exit:** G3: geçerli/bozuk placement, kaynak korunumu ve save/load roundtrip.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Önceki phase `P02` gate evidence gerektirir; istisna owner tarafından ADR olarak yazılmalı.
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
| [BLD-S007](BLD-S007_placement-validation-engine/SPRINT.md) | Placement validation engine | BLD-D049–BLD-D056 | Change diff + focused QA + gerekli game capture |
| [BLD-S008](BLD-S008_yesil-krmz-ghost-ve-etkilesim/SPRINT.md) | Yeşil-kırmızı ghost ve etkileşim | BLD-D057–BLD-D064 | Change diff + focused QA + gerekli game capture |
| [BLD-S009](BLD-S009_yap-commit-ve-kalc-kayt/SPRINT.md) | Yapı commit ve kalıcı kayıt | BLD-D065–BLD-D072 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S007` giriş: S002,S006.; çıkış: Kamera hareketi dışında aynı girdiye aynı gerekçe döner.
- `BLD-S008` giriş: S007.; çıkış: Oyunda hayalet prefab takılı kalmaz.
- `BLD-S009` giriş: S007,S008.; çıkış: Kullanıcı komutla çalıştırabilecek hedefli testler vardır.

## 4. Phase implementation strategy

1. **Phase operation 1:** Yapı prefabının raycast anchor, footprint, normal ve rotation toleranslarını prefab metadata ile doğrula.
2. **Phase operation 2:** İnşa iznini zone, POI, cell, player/vehicle, door ve nav koridor kurallarıyla sırayla değerlendir.
3. **Phase operation 3:** OverlapBoxNonAlloc ve Physics.Raycast filtresini gerçek Layers/Trigger düzeninden türet.
4. **Phase operation 4:** Ghost’u yalnız render-only tut; component side effects, collider ve persistent ID üretimini yasakla.
5. **Phase operation 5:** Geçerli/ret nedenini structured enum + açıklama + ikon + yeşil/kırmızı görünümde göster.
6. **Phase operation 6:** Rotasyon, yükseklik/snap değişiminde preview invalidation ve deterministic result uygulamasını tasarla.
7. **Phase operation 7:** Confirm anında inventory ve world revision değişmişse aynı sorguyu yeniden çalıştır; stale preview’i reddet.
8. **Phase operation 8:** Cancel, pause, death, cell leave ve scene reload sırasında ghost/material geçici durumlarını temizle.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- Kırmızı preview materyal tüketmez.
- Aynı transform ve same world snapshot aynı validation result üretmeli.
- Görsel ghost collider, AI veya persistent entity oluşturamaz.
- Green/red preview yalnız presentation; ConstructionService authoritative confirm tekrar kontrol eder.
- Physics layers user project gerçeğiyle denetlenir, sabit sayılar körlemesine kopyalanmaz.
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | Genel amaçlı ghost, çok katmanlı validasyon, atomik inşa ve kalıcılık. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G3-RISK-1:** Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı. Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G3-RISK-2:** Eğim sınırında preview geçerli ama confirm geçersiz olmamalı; son anda revalidate. Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G3-RISK-3:** Yerleştirme ortasında world cell unload olursa ghost kapanmalı. Expected receipt ve kart mapping’ini phase exit raporunda göster.
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
- Slope/overlap/door/road/vehicle rejection matrix
- Rapid rotate + edge-of-zone + pause/cancel
- Ghost leaks, GC budget and deterministic result

## 9. Risk, kesme, rollback

- **RP03-1:** Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP03-2:** Eğim sınırında preview geçerli ama confirm geçersiz olmamalı; son anda revalidate. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP03-3:** Yerleştirme ortasında world cell unload olursa ghost kapanmalı. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
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

- **Gate:** G3; **Phase:** `P03`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
