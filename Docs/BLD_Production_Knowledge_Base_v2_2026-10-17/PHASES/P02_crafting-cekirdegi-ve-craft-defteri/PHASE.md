# P02 — Crafting çekirdeği ve Craft Defteri

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 2
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** Çok girdili tarifleri, güvenli rezervasyonu, tek UI akışını ve erişilebilir kontrolü geliştir.
- **G2 exit:** G2: birden fazla malzemeli recipe, craft/iptal, notebook, save testleri.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Önceki phase `P01` gate evidence gerektirir; istisna owner tarafından ADR olarak yazılmalı.
- İncelenen branch/commit/dirty state ve prefab/scene checkpoints güncel olmalı.
- Her sprintte source/owner ayrımı yapılmalı; mevcut sistemler clone edilmemeli.
- Altyapı için yalnız ilgili GDD veya runtime sembollerinin ihtiyaç duyulan bölümü okunmalı.
- Sprintte yeni binary asset gerekiyorsa lisans/URP/pivot/LOD/authoring doğrulanmalı.

### Bu phase için kaynak referanslar
- `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterRecipe.cs`
- `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterProduction.cs`
- `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs`

## 3. Sprint breakdown ve phase sonucu

| Sprint | Görev | Günlük kartlar | Kanıt türü |
|---|---|---|---|
| [BLD-S005](BLD-S005_recipe-engine-ve-malzeme-islemeyi-genislet/SPRINT.md) | Recipe engine ve malzeme işlemeyi genişlet | BLD-D033–BLD-D040 | Change diff + focused QA + gerekli game capture |
| [BLD-S006](BLD-S006_craft-defteri-ui-ux/SPRINT.md) | Craft Defteri UI/UX | BLD-D041–BLD-D048 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S005` giriş: S003.; çıkış: Önceki depolama ve modül davranışları bozulmaz.
- `BLD-S006` giriş: S005.; çıkış: Oyuncu tarifin niçin kilitli olduğunu söyleyebilir.

## 4. Phase implementation strategy

1. **Phase operation 1:** ShelterRecipe tek girdi yolunun korunduğunu doğrula; yeni tarifin ihtiyaç duyduğu çok girdiyi katalogla.
2. **Phase operation 2:** Her input için itemId, quantity, source, tool, station ve unlock koşullarını deterministik sıralama ile tanımla.
3. **Phase operation 3:** Craft talebi için inventory revision snapshot al, escrow rezervasyonuna yalnız bir domain API aç.
4. **Phase operation 4:** Başarısız station/input/output kontrolünde materyal değiştirmeyen reject branch tasarla.
5. **Phase operation 5:** İptal, dolu output, güç kesintisi, pause ve save/load arasında job lifecycle state diyagramı üret.
6. **Phase operation 6:** Log→plank veya ilgili üretimin kayıp/çıktı miktarını mass/stack sınırlarıyla simüle et.
7. **Phase operation 7:** Tarif listesinde zero-cost cycle, self-input output ve integer overflow doğrulamasını yap.
8. **Phase operation 8:** Tekrar submit/duplicate request için idempotent receipt ve focused QA beklenen sonucu tanımla.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- Bir girdi aynı anda iki crafting job tarafından kullanılamaz.
- Cancel, full-output, save/load hiçbir materyali yok etmez veya çoğaltmaz.
- Yeni tarifler eski ShelterRecipe ve legacy save davranışlarını değiştirmez.
- Craft notebook kaynak sayımı gösterir ama authoritative item mutation yapmaz.
- Time basis açık: handcraft simulation seconds; workstation world time (tasarım kararı).
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | Çok girdili tarifleri, güvenli rezervasyonu, tek UI akışını ve erişilebilir kontrolü geliştir. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G2-RISK-1:** Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli. Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G2-RISK-2:** Station üretim ortasında güç kaybederse ne olur: pause/abort açık kural. Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G2-RISK-3:** Output doluyken CompletedWaitingOutput davranışı korunur. Expected receipt ve kart mapping’ini phase exit raporunda göster.
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
- Multi-input escrow/refund and overflow
- Recipe cyclic free-resource conversion detection
- Pause/quit/save/reload/double-submit

## 9. Risk, kesme, rollback

- **RP02-1:** Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP02-2:** Station üretim ortasında güç kaybederse ne olur: pause/abort açık kural. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP02-3:** Output doluyken CompletedWaitingOutput davranışı korunur. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
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

- **Gate:** G2; **Phase:** `P02`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
