# P08 — Sanat, deneyim ve optimizasyon

**Program:** LS-BLD 2.0 · **Status:** PROPOSED / TEST_NOT_RUN · **Sprint sayısı:** 4
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün gerekçesi ve phase scope

- **Mission:** Production-quality modeller, animasyon, ses, erişilebilir UI, profiler hedefleri.
- **G8 exit:** G8: görsel kabul + performans + erişilebilirlik doğrulaması.
- FirstTownCore/S010/S023 var olan içerik bu phase yüzünden kendiliğinden korunmuş/uygulanmış sayılmaz; implementation agent başta güncel durum denetler.
- Kapsam bu phase sprintlerini kapsar; önceki tamamlanmamış gate’ler otomatik geçilmiş sayılmaz.

## 2. Giriş sözleşmesi ve bağımlılıklar

- Önceki phase `P07` gate evidence gerektirir; istisna owner tarafından ADR olarak yazılmalı.
- İncelenen branch/commit/dirty state ve prefab/scene checkpoints güncel olmalı.
- Her sprintte source/owner ayrımı yapılmalı; mevcut sistemler clone edilmemeli.
- Altyapı için yalnız ilgili GDD veya runtime sembollerinin ihtiyaç duyulan bölümü okunmalı.
- Sprintte yeni binary asset gerekiyorsa lisans/URP/pivot/LOD/authoring doğrulanmalı.

### Bu phase için kaynak referanslar
- `Docs/Implementation/S013/S013_ART_BIBLE.md`
- `Docs/Implementation/S013/S013_MODULAR_STANDARD.md`
- `Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs`

## 3. Sprint breakdown ve phase sonucu

| Sprint | Görev | Günlük kartlar | Kanıt türü |
|---|---|---|---|
| [BLD-S023](BLD-S023_production-asset-kit-ve-animasyon/SPRINT.md) | Production asset kit ve animasyon | BLD-D177–BLD-D184 | Change diff + focused QA + gerekli game capture |
| [BLD-S024](BLD-S024_ses-vfx-ve-kullanc-hissi/SPRINT.md) | Ses, VFX ve kullanıcı hissi | BLD-D185–BLD-D192 | Change diff + focused QA + gerekli game capture |
| [BLD-S025](BLD-S025_craft-defteri-ux-ve-erisilebilirlik/SPRINT.md) | Craft Defteri UX ve erişilebilirlik | BLD-D193–BLD-D200 | Change diff + focused QA + gerekli game capture |
| [BLD-S026](BLD-S026_cpu-gpu-ve-buyuk-dunya-optimizasyonu/SPRINT.md) | CPU/GPU ve büyük dünya optimizasyonu | BLD-D201–BLD-D208 | Change diff + focused QA + gerekli game capture |

### Sprint bağımlılık zinciri

- `BLD-S023` giriş: S015.; çıkış: Referansla gerçek oyun footage yan yana incelenir.
- `BLD-S024` giriş: S023.; çıkış: 50 placement particle spam'e dönüşmez.
- `BLD-S025` giriş: S006,S024.; çıkış: UI iyileştirmesi gözlemle kararlaştırılır.
- `BLD-S026` giriş: S023–S025.; çıkış: Bütçe aşılırsa güvenli kapsam azaltma vardır.

## 4. Phase implementation strategy

1. **Phase operation 1:** S013 stylized-realism palette ve Unity URP shader/material standardına uygun görsel kriter yaz.
2. **Phase operation 2:** Asset envanterinden gerçek prefab/mesh/texture yollarını doğrula; mevcut vendor dosyasını değiştirme.
3. **Phase operation 3:** Hero asset ve repetition çeşitlerini silhouette, UV, normal, scale ve pivot açısından incele.
4. **Phase operation 4:** Viewmodel/FPS oyuncu eli ve aynı anda dünyada görünen modelin tutarlılığını değerlendir.
5. **Phase operation 5:** LOD, collider, shadow, alpha overdraw ve shader variation maliyetlerini test planına bağla.
6. **Phase operation 6:** Rain, day, sunset, dusk, flashlight altında değişen materyal/FX okumasını denetle.
7. **Phase operation 7:** Yerleştirme geçerli/ret materyallerini kontrast ve renk körlüğü açısından kalibre ettir.
8. **Phase operation 8:** Bütün gerçek ekran görüntülerini kullanıcı alır; konsept resimleri Unity acceptance kanıtı sayma.
9. İlgili sprintler arasında `definitionId`, stable entity ID, version ve data owner sözleşmelerini tek kaynakta tut.
10. Phase gate’te world/user-facing senaryoyu önce/sonra ve manual evidence ile sun; kapsam genişlemesini owner kararına bağla.

## 5. Phase boyunca korunacak mimari sınırlar

- Production source asset korunur; project-owned prefab/material varyantları kullanılır.
- İlk kişi kamera ölçeğinde kalite kabulü olmadan final art onayı verilmez.
- Görsel varlık teknik çatışma/collider/LOD gereksinimini karşılamalı.
- Meshy sprinti ayrı ve ertelenmiş; yeni asset zorunlu değil.
- Mevcut free assetlerde ticari lisans kontrolü yapılmadan release onayı yok.
- Yeni craft/build yerleştirme için GDD karar hiyerarşisini sessiz değiştirme.
- Test owner kullanıcı; S013/FirstTownCore_VisualSlice hash ve native authoring guardları korunur.

## 6. Veri, servis ve sahne entegrasyon matrisi

| Katman | Phase ile temas | Kabul / kilit riski |
|---|---|---|
| Product / GDD | Production-quality modeller, animasyon, ses, erişilebilir UI, profiler hedefleri. | ADR sahibi kararı |
| Inventory | single authoritative ownership | miktar kaybı/dupe ret |
| Craft jobs | S010 escrow veya yeni adapter | transaction conflict |
| Construction | yalnız phase kapsamı kadar BuildDefinition/placement | unsafe placement ret |
| Scene/Cell | FirstTownCore ve resident/unloaded cell | save/collision/nav readiness |
| Persistence | stable ID, change receipts, old save | schema + migration fixture |
| Presentation | Craft Defteri, ghost, FPS art | owner visual acceptance |
| QA | owner-run tests | NO fabricated PASS |

## 7. Phase acceptance scenario bank

- **G8-RISK-1:** Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır. Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G8-RISK-2:** Yeşil ghost materyali alpha ve z-fighting sorunu yaratıyor mu? Expected receipt ve kart mapping’ini phase exit raporunda göster.
- **G8-RISK-3:** Yağmur/günbatımı ile malzemelerin tepkileri tutarlı mı? Expected receipt ve kart mapping’ini phase exit raporunda göster.
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
- FPS screenshot before/after matrix
- LOD/material/UV/pivot/collider inspection
- Shader/URP/night/weather quality check

## 9. Risk, kesme, rollback

- **RP08-1:** Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır. Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP08-2:** Yeşil ghost materyali alpha ve z-fighting sorunu yaratıyor mu? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
- **RP08-3:** Yağmur/günbatımı ile malzemelerin tepkileri tutarlı mı? Mitigation: related sprint owner, blocker/receipt ve rollback ayrı yazılır.
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

- **Gate:** G8; **Phase:** `P08`
- **Branch / HEAD / date:** ___
- **Sprints pass/partial/fail:** ___
- **Changed/added files and GUIDs:** ___
- **Manual test evidence paths:** ___
- **User visual acceptance:** ___
- **Save migration status:** ___
- **Risks / postponed cards / owner decision:** ___
- **Next phase readiness:** ___

[Üst program](../../PROGRAM_INDEX.md) · [QA standardı](../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
