# LAST SIGNAL — BUILDING & CRAFTING PRODUCTION KNOWLEDGE BASE

**Sürüm:** 2.0 · **Tarih:** 17 Ekim 2026 · **Program:** `LS-BLD` · **Durum:** `PROPOSED`, hiçbir yeni sprintin testi çalıştırılmadı.

Bu klasör **10 phase → 28 sprint → 224 günlük iş kartı** ile yönetilen üretim dokümantasyonudur. Kod, sahne veya Unity testi yürütmez; ajanların ve ürün sahibinin üzerinde çalışacağı teknik sözleşmeleri ve kanıt kapılarını düzenler.

## Çalışmaya nereden başlanır?

1. `00_GOVERNANCE/01_DECISION_LOG.md` mevcut GDD çatışmasını ve owner onayı gereken kapsam değişikliğini açıklar.
2. `00_GOVERNANCE/02_ARCHITECTURE_BASELINE.md` gerçek mevcut sınıfları ve **henüz olmayan** tasarım sınıflarını ayırır.
3. `00_GOVERNANCE/03_MANUAL_TEST_POLICY.md` testlerin kullanıcıda kalmasını bağlayıcı yapar.
4. `PHASES/P00_product_architecture/PHASE.md` ile başlamadan önce yerel worktree ve current branch kontrol edilir.
5. `SPRINT.md` ve `DAYS/BLD-Dxxx...md` dosyaları uygulanacak ayrıntılı iş emridir.

## Hangi kapsam ne zaman yapılacak?

- **G0–G3:** Ürün mimarisi, harvest, çok-input crafting, Craft Defteri, preview ve safe commit.
- **G4:** Orman → odun → tahta → defter → çit → save/load ilk oynanabilir deneyim.
- **G5–G6:** Temel/duvar/kapı/çatı ve üretim/sığınak utilities.
- **G7–G9:** Ekonomi, world save/cell, production sanat, optimizasyon ve alpha kabul.

## Kaynak gerçekliği

- Repo: private `coskunburak/Last-Signal`.
- İncelenen baseline: `s012-integrated-graybox-slice`, commit `fe0f4c70` (17.10.2026), main merge `a5749e4`.
- Mevcut: `ShelterRecipe`, `ShelterProduction`, `ShelterSite`, `ShelterSocket`, `PlayerInventory`, `WorldItem`, `SaveSession`, `WorldCellManager`, `WorldClock`.
- **Mevcut olduğu doğrulanmayan/önerilen:** Genel `BuildDefinition`, `ConstructionService`, `GhostPresenter`, harvest-node service ve çok-input crafting domain.
- Art pipeline: Meshy AI üretim sprinti ayrı ve ertelenmiştir; LS-BLD içinde zorunlu kılınmaz.

## Disiplin

Plan, test yapılmış uygulama yerine geçmez. Agent kullanıcı adına Unity EditMode/PlayMode, benchmark, build çalıştırmaz. Implementasyon sırasında yalnız görevle ilişkili doküman ve sınıfları context'e alır. Eski sahne, save, GUID ve vendor varlıkları korunur.

[Program indeksini aç](PROGRAM_INDEX.md) · [Eski master baseline](99_BASELINE/SOURCE_MASTER_PLAN_v1.md)
