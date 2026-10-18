# P09 — Alpha kabul ve scope kapatma

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 2
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** Sistemlerin birlikte çalışmasını kanıtla, stabilizasyon ve ürün kararını ver.
- **G9 exit:** G9: alpha playtest, release scope ve teknik devir kararı.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Önceki phase `P08` gate evidence gerektirir; istisna owner tarafından ADR olarak yazılmalı.
- İncelenen branch/commit/dirty state ve prefab/scene checkpoints güncel olmalı.
- Her sprintte source/owner ayrımı yapılmalı; mevcut sistemler clone edilmemeli.
- Altyapı için yalnız ilgili GDD veya runtime sembollerinin ihtiyaç duyulan bölümü okunmalı.
- Sprintte yeni binary asset gerekiyorsa lisans/URP/pivot/LOD/authoring doğrulanmalı.

### Bu phase için kaynak referanslar
- `Docs/Last_Signal_Docs_v0.2/sources/17_Save_Load_Migration.md`
- `Docs/Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md`
- `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs`

## 3. Sprint breakdown ve phase sonucu

| Sprint | Görev | Günlük kartlar | Kanıt türü |
|---|---|---|---|
| [BLD-S027](BLD-S027_end-to-end-alpha-qa-ve-regresyon/SPRINT.md) | End-to-end alpha QA ve regresyon | BLD-D209–BLD-D216 | Change diff + focused QA + gerekli game capture |
| [BLD-S028](BLD-S028_stabilizasyon-gdd-guncelleme-teslim/SPRINT.md) | Stabilizasyon, GDD güncelleme, teslim | BLD-D217–BLD-D224 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S027` giriş: S010,S015,S019,S022,S026.; çıkış: Bloklayıcılar kapanmadan G9'a ilerlenmez.
- `BLD-S028` giriş: S027.; çıkış: Bir sonraki phase kapsamı tahmin değil veriyle belirlenir.

## 4. Phase implementation strategy

1. **Phase operation 1:** İlgili mevcut assembly, Tools runner ve test isimlerini yerel branch’te doğrulat.
2. **Phase operation 2:** Görevi component contract, regression ve gameplay etkisi olarak üç test düzeyine ayır.
3. **Phase operation 3:** Başarısızlık senaryoları için input fixture, initial snapshot ve beklenen state farkını yaz.
4. **Phase operation 4:** Kullanıcı tarafından çalıştırılacak en küçük focused test sırasını oluştur.
5. **Phase operation 5:** Focused sonuç PASS ise ilgili integration ve regression gate’ini ayrıca iste.
6. **Phase operation 6:** Source-preservation diff, asset GUID, save schema ve backup kontrollerini doğrulama listesine al.
7. **Phase operation 7:** Performance ölçümlerini median/p95/p99 CPU/GPU, GC ve memory bağlamında raporla.
8. **Phase operation 8:** Test çalıştırmadan, görsel owner kabulü olmadan veya eksik logla PASS yazılmasını engelle.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- Agent manual test çalıştırmaz; kullanıcı evidence ile PASS verir.
- Sadece eski PASS yeni sürüm için otomatik PASS değildir.
- Kritik save/item duplication/regression açıkken gate kapatılamaz.
- Komut gerçek Tools/ test assembly dosyasından türetilir; hayalî --filter verilmez.
- Editor test, PlayMode, build, perf ve art gate birbirinden ayrıdır.
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | Sistemlerin birlikte çalışmasını kanıtla, stabilizasyon ve ürün kararını ver. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G9-RISK-1:** Test Unity Editor açıkken batchmode kilitleniyor mu? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G9-RISK-2:** Focused PASS sonrası full regression FAIL ise false closure yapılır mı? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G9-RISK-3:** Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi? Expected receipt ve kart mapping’ini phase exit raporunda göster.
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
- Targeted edit/play tests owner-run
- Source preservation and diff audit
- Evidence integrity, perf counter and gate receipt review

## 9. Risk, kesme, rollback

- **RP09-1:** Test Unity Editor açıkken batchmode kilitleniyor mu? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP09-2:** Focused PASS sonrası full regression FAIL ise false closure yapılır mı? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP09-3:** Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
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

- **Gate:** G9; **Phase:** `P09`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
