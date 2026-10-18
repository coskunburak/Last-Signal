# P00 — Ürün sözleşmesi ve güvenli mimari

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 2
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** Serbest yapı inşası kapsam kararını resmileştir; güvenli sistem sınırlarını ve migration stratejisini kilitle.
- **G0 exit:** G0: onaylı kapsam farkı, teknik tasarım, şema ve bağımlılık matrisi.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Program girişi: ürün sahibi controlled free building kararını onaylamalı; GDD önce değişiklik kartıyla güncellenir.
- İncelenen branch/commit/dirty state ve prefab/scene checkpoints güncel olmalı.
- Her sprintte source/owner ayrımı yapılmalı; mevcut sistemler clone edilmemeli.
- Altyapı için yalnız ilgili GDD veya runtime sembollerinin ihtiyaç duyulan bölümü okunmalı.
- Sprintte yeni binary asset gerekiyorsa lisans/URP/pivot/LOD/authoring doğrulanmalı.

### Bu phase için kaynak referanslar
- `Docs/Last_Signal_Docs_v0.2/sources/02_Vizyon_Donguler_ve_Dunya.md`
- `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md`
- `Docs/Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md`

## 3. Sprint breakdown ve phase sonucu

| Sprint | Görev | Günlük kartlar | Kanıt türü |
|---|---|---|---|
| [BLD-S001](BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/SPRINT.md) | GDD kapsam ve oyuncu vaadi | BLD-D001–BLD-D008 | Change diff + focused QA + gerekli game capture |
| [BLD-S002](BLD-S002_mimari-sozlesme-ve-risk-temeli/SPRINT.md) | Mimari sözleşme ve risk temeli | BLD-D009–BLD-D016 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S001` giriş: Program girişi: GDD/branch audit.; çıkış: G0 altında kabul edilmeden release kapsamı güncellendi sayılmaz.
- `BLD-S002` giriş: S001.; çıkış: Kod üretimine girişte kaynak bütünlüğü korunabilir.

## 4. Phase implementation strategy

1. **Phase operation 1:** Var olan GDD kararını satır/sürüm bazında bul; uyumlu olanı ve açık çelişkiyi ayrı tabloya yaz.
2. **Phase operation 2:** Bu kartın oyuncuya verdiği eylem→geri bildirim→sonuç zincirini beş dakikalık bir senaryoda ifade et.
3. **Phase operation 3:** Yeni kararın inşa alanı, oyuncu güvenliği ve single-player kapsamına etkisini kaydet.
4. **Phase operation 4:** Must/Should/Could/Out değişiminin mevcut S010/S023/FirstTownCore planlarıyla bağını kur.
5. **Phase operation 5:** Kapsamı bozabilecek iki alternatif tasarımı kıyasla ve maliyet/gain gerekçesini yaz.
6. **Phase operation 6:** Bağımlılık ve görünür demo düzeyi için başarı/başarısızlık eşiklerini ölçülebilir tanımla.
7. **Phase operation 7:** Bu kartın tamamlanması için gerekli ADR, traceability ve sahibi tanımlarını doldur.
8. **Phase operation 8:** Son tasarımı owner onayından önce “proposed” bırak; başka belgeyi sessizce yeniden yazma.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- Mevcut no-free-building kararı kayıtsız silinmez; ADR olarak açıkça değişir.
- Yeni Must eklenirse eşdeğer işin çıkması veya takvimin uzaması kayıtlanır.
- UI veya teknik özellik tek başına oyuncu değeri sayılmaz.
- Karar sahibi ürün tasarımını onaylar; teknik agent onay verilmiş gibi GDD değiştirmez.
- Mevcut GDD içindeki tek-player ve yoğun harita önceliği korunur.
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | Serbest yapı inşası kapsam kararını resmileştir; güvenli sistem sınırlarını ve migration stratejisini kilitle. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G0-RISK-1:** Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür. Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G0-RISK-2:** Oyuncu inşa etmek istemediğinde temel survival rotası kilitleniyor mu? Kilitlenmemeli. Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G0-RISK-3:** İlk sürüme büyük serbest yıkım veya multiplayer sızıyor mu? Kapsam dışı. Expected receipt ve kart mapping’ini phase exit raporunda göster.
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
- GDD contradiction scan
- MUST→oyuncu değeri ve kabul izi
- Sprint→bağımlılık→kaynak bütçesi tutarlılık incelemesi

## 9. Risk, kesme, rollback

- **RP00-1:** Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP00-2:** Oyuncu inşa etmek istemediğinde temel survival rotası kilitleniyor mu? Kilitlenmemeli. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP00-3:** İlk sürüme büyük serbest yıkım veya multiplayer sızıyor mu? Kapsam dışı. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
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

- **Gate:** G0; **Phase:** `P00`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
