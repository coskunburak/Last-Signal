# P01 — Kaynak toplama ve ağaç kesme

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 2
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** Oynanabilir, kalıcı ve dengelenebilir kaynak toplama mekaniklerini oluştur.
- **G1 exit:** G1: ağaç kesme/kaynak toplama + mülkiyet, yükleme ve gürültü doğrulaması.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Önceki phase `P00` gate evidence gerektirir; istisna owner tarafından ADR olarak yazılmalı.
- İncelenen branch/commit/dirty state ve prefab/scene checkpoints güncel olmalı.
- Her sprintte source/owner ayrımı yapılmalı; mevcut sistemler clone edilmemeli.
- Altyapı için yalnız ilgili GDD veya runtime sembollerinin ihtiyaç duyulan bölümü okunmalı.
- Sprintte yeni binary asset gerekiyorsa lisans/URP/pivot/LOD/authoring doğrulanmalı.

### Bu phase için kaynak referanslar
- `Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs`
- `Assets/LastSignal/Scripts/Runtime/Inventory/PlayerInventory.cs`
- `Docs/Last_Signal_Docs_v0.2/sources/12_World_Pressure.md`

## 3. Sprint breakdown ve phase sonucu

| Sprint | Görev | Günlük kartlar | Kanıt türü |
|---|---|---|---|
| [BLD-S003](BLD-S003_harvestable-resource-nodes/SPRINT.md) | Harvestable resource nodes | BLD-D017–BLD-D024 | Change diff + focused QA + gerekli game capture |
| [BLD-S004](BLD-S004_agac-kesme-oynans-ve-geri-bildirim/SPRINT.md) | Ağaç kesme oynanış ve geri bildirim | BLD-D025–BLD-D032 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S003` giriş: S002.; çıkış: Save/load sonrası ağaç kendiliğinden yeniden büyümez.
- `BLD-S004` giriş: S003.; çıkış: Kullanıcı test çalıştırmadıkça PASS verilmez.

## 4. Phase implementation strategy

1. **Phase operation 1:** FirstTownCore vegetation arasında gerçek kaynak düğümleri ile salt dekorasyonu kesin kimlik ve etiketten ayır.
2. **Phase operation 2:** Vuruş menzili, yönü, hız sınırı ve alet yetkisini player domain’den doğrulat.
3. **Phase operation 3:** Node damage/stage/remaining-yield state’ini event/receipt ile monoton ilerlet.
4. **Phase operation 4:** Accepted hit başına animasyon/ses/FX ayrımını, world pressure event hız sınırıyla bağla.
5. **Phase operation 5:** Node depletion’ı stable ID/cell delta olarak yaz; save/load sonrasında tekrar ücretsiz kaynak üretme.
6. **Phase operation 6:** Output spawn point, terrain clearance, container ownership ve full inventory davranışını belirt.
7. **Phase operation 7:** Eşzamanlı iki hit, broken tool ve death/unload yarışında işlemi tekrar oynatmadan sonlandır.
8. **Phase operation 8:** Görsel hasat kanıtını FPS kamera ve uzak orman silueti açısından ayrı kabul ettir.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- Dekoratif ağaçların tamamı sessizce harvestable sayılmaz.
- Ödül yalnız authoritative kabul edilen bir vuruş dizisinden çıkar.
- Tükenmiş resource node, unload/load sonrası ücretsiz yenilenmez.
- Kesim fiziksel devrilmesi isteniyorsa sınırlı görsel efekt olarak değerlendirilir.
- Vuruş sesi frame başına değil olay ritmine göre World Pressure kaydı üretir.
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | Oynanabilir, kalıcı ve dengelenebilir kaynak toplama mekaniklerini oluştur. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G1-RISK-1:** Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G1-RISK-2:** Envanter doluyken kaynaklar sahnede güvenli ve kalıcı kalır mı? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G1-RISK-3:** Aynı ağaca tek frame içinde iki geçerli hit gelirse iki ödül oluşur mu? Expected receipt ve kart mapping’ini phase exit raporunda göster.
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
- Valid/invalid tool-hit matrix
- Harvest+save/load+cell roundtrip
- Inventory full + duplicate hit + FPS invariance

## 9. Risk, kesme, rollback

- **RP01-1:** Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP01-2:** Envanter doluyken kaynaklar sahnede güvenli ve kalıcı kalır mı? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP01-3:** Aynı ağaca tek frame içinde iki geçerli hit gelirse iki ödül oluşur mu? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
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

- **Gate:** G1; **Phase:** `P01`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
