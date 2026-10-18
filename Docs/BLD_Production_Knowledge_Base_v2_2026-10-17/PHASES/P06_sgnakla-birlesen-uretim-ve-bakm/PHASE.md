# P06 — Sığınakla birleşen üretim ve bakım

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 4
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** İşlevsel istasyonlar, onarım/söküm, güvenlik, elektrik-su ve ilerleme.
- **G6 exit:** G6: yaşayan sığınak + kapsamlı craft ve maintenance loop.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Önceki phase `P05` gate evidence gerektirir; istisna owner tarafından ADR olarak yazılmalı.
- İncelenen branch/commit/dirty state ve prefab/scene checkpoints güncel olmalı.
- Her sprintte source/owner ayrımı yapılmalı; mevcut sistemler clone edilmemeli.
- Altyapı için yalnız ilgili GDD veya runtime sembollerinin ihtiyaç duyulan bölümü okunmalı.
- Sprintte yeni binary asset gerekiyorsa lisans/URP/pivot/LOD/authoring doğrulanmalı.

### Bu phase için kaynak referanslar
- `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs`
- `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterProduction.cs`
- `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md`

## 3. Sprint breakdown ve phase sonucu

| Sprint | Görev | Günlük kartlar | Kanıt türü |
|---|---|---|---|
| [BLD-S016](BLD-S016_islevsel-istasyonlar-ve-depolama/SPRINT.md) | İşlevsel istasyonlar ve depolama | BLD-D121–BLD-D128 | Change diff + focused QA + gerekli game capture |
| [BLD-S017](BLD-S017_onarm-sokum-ve-yer-degistirme/SPRINT.md) | Onarım, söküm ve yer değiştirme | BLD-D129–BLD-D136 | Change diff + focused QA + gerekli game capture |
| [BLD-S018](BLD-S018_sgnak-utility-entegrasyonu/SPRINT.md) | Sığınak utility entegrasyonu | BLD-D137–BLD-D144 | Change diff + focused QA + gerekli game capture |
| [BLD-S019](BLD-S019_uzun-vadeli-us-progression-ve-g6/SPRINT.md) | Uzun vadeli üs progression ve G6 | BLD-D145–BLD-D152 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S016` giriş: S015.; çıkış: Dupe ve item kaybı yok.
- `BLD-S017` giriş: S014,S016.; çıkış: Kaynak invariants sağlam.
- `BLD-S018` giriş: S015–S017.; çıkış: Save ve world time advance sonucu tutarlı.
- `BLD-S019` giriş: S016–S018.; çıkış: Tamamlanmamış faydalar PARTIAL kalır.

## 4. Phase implementation strategy

1. **Phase operation 1:** Mevcut ShelterSite claim/Bed/Storage/Workbench soketlerini değiştirmeden yeni yapının faydasını tanımla.
2. **Phase operation 2:** Utility state için unlocked capability ile currently operational flag ayrımını uygula.
3. **Phase operation 3:** İstasyon uzaklık/erişim kontrolünü depo, player ve hangi cell loaded bilgisine bağla.
4. **Phase operation 4:** Jeneratör/ışık/radyo/su gibi kullanımların yakıt, world time ve gürültü maliyetlerini açıkla.
5. **Phase operation 5:** Sığınak çatı/kapı/yağmur güvenliği için görselden bağımsız fiziksel doğrulama ve anlamlı kullanıcı mesajı seç.
6. **Phase operation 6:** Unloaded shelter üzerinde üretim/enerji/su simülasyonu için world time analytical advancement kararı yaz.
7. **Phase operation 7:** Fayda kaybında ownership/storage/claim korunumu ve recovery yolu belirt.
8. **Phase operation 8:** Yeni yapı etkisi için öncesi/sonrası gameplay kıyasını, kaynak ve tehdit maliyetleriyle tanımla.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- Üs claim zombies’i silmez veya invulnerability eklemez.
- Mevcut sabit modüller ve storage erişim koşulları geriye uyumlu kalır.
- Power kaybı yatak/claim/ownership kaybı anlamına gelmez.
- Kullanılabilir çalışma tezgâhı varsa yeni crafting ownership kopyası oluşturma.
- Sığınak güvenliği kararlı ve anlaşılır; zorunlu nightly raids kapsam dışıdır.
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | İşlevsel istasyonlar, onarım/söküm, güvenlik, elektrik-su ve ilerleme. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G6-RISK-1:** Kısmi çatıda RainRoof/inside flag yanlışlıkla kuru kabul ediyor mu? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G6-RISK-2:** Jeneratör kapalıyken crafting istasyonu çalışıyor mu; tasarım kuralları ne? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G6-RISK-3:** Yeni depo iki farklı authoritative envanter açıyor mu? Expected receipt ve kart mapping’ini phase exit raporunda göster.
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
- Claim+storage+socket regression
- Roof/wetness/power status transitions
- Unloaded shelter fuel/jobs deterministic recovery

## 9. Risk, kesme, rollback

- **RP06-1:** Kısmi çatıda RainRoof/inside flag yanlışlıkla kuru kabul ediyor mu? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP06-2:** Jeneratör kapalıyken crafting istasyonu çalışıyor mu; tasarım kuralları ne? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP06-3:** Yeni depo iki farklı authoritative envanter açıyor mu? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
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

- **Gate:** G6; **Phase:** `P06`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
