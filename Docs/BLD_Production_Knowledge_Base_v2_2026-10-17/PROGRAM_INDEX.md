# LS-BLD v2 — Program Index

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 10 phase / 28 sprint / 224 gün kartı

| Phase | Adı | Sprint | Günlük kart aralığı | Gate |
|---|---|---:|---|---|
| [P00](PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/PHASE.md) | Ürün sözleşmesi ve güvenli mimari | 2 | BLD-D001–BLD-D016 | G0 |
| [P01](PHASES/P01_kaynak-toplama-ve-agac-kesme/PHASE.md) | Kaynak toplama ve ağaç kesme | 2 | BLD-D017–BLD-D032 | G1 |
| [P02](PHASES/P02_crafting-cekirdegi-ve-craft-defteri/PHASE.md) | Crafting çekirdeği ve Craft Defteri | 2 | BLD-D033–BLD-D048 | G2 |
| [P03](PHASES/P03_yap-yerlestirme-cekirdegi/PHASE.md) | Yapı yerleştirme çekirdeği | 3 | BLD-D049–BLD-D072 | G3 |
| [P04](PHASES/P04_entegre-ilk-playable-loop/PHASE.md) | Entegre ilk playable loop | 1 | BLD-D073–BLD-D080 | G4 |
| [P05](PHASES/P05_moduler-ev-ve-yap-mekanigi/PHASE.md) | Modüler ev ve yapı mekaniği | 5 | BLD-D081–BLD-D120 | G5 |
| [P06](PHASES/P06_sgnakla-birlesen-uretim-ve-bakm/PHASE.md) | Sığınakla birleşen üretim ve bakım | 4 | BLD-D121–BLD-D152 | G6 |
| [P07](PHASES/P07_dunya-ekonomisi-ve-guvenilir-kayt/PHASE.md) | Dünya ekonomisi ve güvenilir kayıt | 3 | BLD-D153–BLD-D176 | G7 |
| [P08](PHASES/P08_sanat-deneyim-ve-optimizasyon/PHASE.md) | Sanat, deneyim ve optimizasyon | 4 | BLD-D177–BLD-D208 | G8 |
| [P09](PHASES/P09_alpha-kabul-ve-scope-kapatma/PHASE.md) | Alpha kabul ve scope kapatma | 2 | BLD-D209–BLD-D224 | G9 |

## Sprint navigation

### P00 — Ürün sözleşmesi ve güvenli mimari

- [BLD-S001 — GDD kapsam ve oyuncu vaadi](PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/SPRINT.md) · BLD-D001–BLD-D008
- [BLD-S002 — Mimari sözleşme ve risk temeli](PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/SPRINT.md) · BLD-D009–BLD-D016

### P01 — Kaynak toplama ve ağaç kesme

- [BLD-S003 — Harvestable resource nodes](PHASES/P01_kaynak-toplama-ve-agac-kesme/BLD-S003_harvestable-resource-nodes/SPRINT.md) · BLD-D017–BLD-D024
- [BLD-S004 — Ağaç kesme oynanış ve geri bildirim](PHASES/P01_kaynak-toplama-ve-agac-kesme/BLD-S004_agac-kesme-oynans-ve-geri-bildirim/SPRINT.md) · BLD-D025–BLD-D032

### P02 — Crafting çekirdeği ve Craft Defteri

- [BLD-S005 — Recipe engine ve malzeme işlemeyi genişlet](PHASES/P02_crafting-cekirdegi-ve-craft-defteri/BLD-S005_recipe-engine-ve-malzeme-islemeyi-genislet/SPRINT.md) · BLD-D033–BLD-D040
- [BLD-S006 — Craft Defteri UI/UX](PHASES/P02_crafting-cekirdegi-ve-craft-defteri/BLD-S006_craft-defteri-ui-ux/SPRINT.md) · BLD-D041–BLD-D048

### P03 — Yapı yerleştirme çekirdeği

- [BLD-S007 — Placement validation engine](PHASES/P03_yap-yerlestirme-cekirdegi/BLD-S007_placement-validation-engine/SPRINT.md) · BLD-D049–BLD-D056
- [BLD-S008 — Yeşil-kırmızı ghost ve etkileşim](PHASES/P03_yap-yerlestirme-cekirdegi/BLD-S008_yesil-krmz-ghost-ve-etkilesim/SPRINT.md) · BLD-D057–BLD-D064
- [BLD-S009 — Yapı commit ve kalıcı kayıt](PHASES/P03_yap-yerlestirme-cekirdegi/BLD-S009_yap-commit-ve-kalc-kayt/SPRINT.md) · BLD-D065–BLD-D072

### P04 — Entegre ilk playable loop

- [BLD-S010 — İlk entegre playable vertical slice](PHASES/P04_entegre-ilk-playable-loop/BLD-S010_ilk-entegre-playable-vertical-slice/SPRINT.md) · BLD-D073–BLD-D080

### P05 — Modüler ev ve yapı mekaniği

- [BLD-S011 — Temel ve zemin yapı seti](PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S011_temel-ve-zemin-yap-seti/SPRINT.md) · BLD-D081–BLD-D088
- [BLD-S012 — Modüler duvar ve snap graph](PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S012_moduler-duvar-ve-snap-graph/SPRINT.md) · BLD-D089–BLD-D096
- [BLD-S013 — Kapı, pencere ve çatı paketi](PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S013_kap-pencere-ve-cat-paketi/SPRINT.md) · BLD-D097–BLD-D104
- [BLD-S014 — Yapı dayanımı, bağlantılar, inşa güvenliği](PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S014_yap-dayanm-baglantlar-insa-guvenligi/SPRINT.md) · BLD-D105–BLD-D112
- [BLD-S015 — Küçük barınak oynanabilir demo](PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S015_kucuk-barnak-oynanabilir-demo/SPRINT.md) · BLD-D113–BLD-D120

### P06 — Sığınakla birleşen üretim ve bakım

- [BLD-S016 — İşlevsel istasyonlar ve depolama](PHASES/P06_sgnakla-birlesen-uretim-ve-bakm/BLD-S016_islevsel-istasyonlar-ve-depolama/SPRINT.md) · BLD-D121–BLD-D128
- [BLD-S017 — Onarım, söküm ve yer değiştirme](PHASES/P06_sgnakla-birlesen-uretim-ve-bakm/BLD-S017_onarm-sokum-ve-yer-degistirme/SPRINT.md) · BLD-D129–BLD-D136
- [BLD-S018 — Sığınak utility entegrasyonu](PHASES/P06_sgnakla-birlesen-uretim-ve-bakm/BLD-S018_sgnak-utility-entegrasyonu/SPRINT.md) · BLD-D137–BLD-D144
- [BLD-S019 — Uzun vadeli üs progression ve G6](PHASES/P06_sgnakla-birlesen-uretim-ve-bakm/BLD-S019_uzun-vadeli-us-progression-ve-g6/SPRINT.md) · BLD-D145–BLD-D152

### P07 — Dünya ekonomisi ve güvenilir kayıt

- [BLD-S020 — Kaynak yenilenmesi ve ekoloji](PHASES/P07_dunya-ekonomisi-ve-guvenilir-kayt/BLD-S020_kaynak-yenilenmesi-ve-ekoloji/SPRINT.md) · BLD-D153–BLD-D160
- [BLD-S021 — World streaming, save ve migration](PHASES/P07_dunya-ekonomisi-ve-guvenilir-kayt/BLD-S021_world-streaming-save-ve-migration/SPRINT.md) · BLD-D161–BLD-D168
- [BLD-S022 — Economy/pressure balance ve G7](PHASES/P07_dunya-ekonomisi-ve-guvenilir-kayt/BLD-S022_economy-pressure-balance-ve-g7/SPRINT.md) · BLD-D169–BLD-D176

### P08 — Sanat, deneyim ve optimizasyon

- [BLD-S023 — Production asset kit ve animasyon](PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S023_production-asset-kit-ve-animasyon/SPRINT.md) · BLD-D177–BLD-D184
- [BLD-S024 — Ses, VFX ve kullanıcı hissi](PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S024_ses-vfx-ve-kullanc-hissi/SPRINT.md) · BLD-D185–BLD-D192
- [BLD-S025 — Craft Defteri UX ve erişilebilirlik](PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S025_craft-defteri-ux-ve-erisilebilirlik/SPRINT.md) · BLD-D193–BLD-D200
- [BLD-S026 — CPU/GPU ve büyük dünya optimizasyonu](PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S026_cpu-gpu-ve-buyuk-dunya-optimizasyonu/SPRINT.md) · BLD-D201–BLD-D208

### P09 — Alpha kabul ve scope kapatma

- [BLD-S027 — End-to-end alpha QA ve regresyon](PHASES/P09_alpha-kabul-ve-scope-kapatma/BLD-S027_end-to-end-alpha-qa-ve-regresyon/SPRINT.md) · BLD-D209–BLD-D216
- [BLD-S028 — Stabilizasyon, GDD güncelleme, teslim](PHASES/P09_alpha-kabul-ve-scope-kapatma/BLD-S028_stabilizasyon-gdd-guncelleme-teslim/SPRINT.md) · BLD-D217–BLD-D224

## İş planı / tahminler

- 28 sprint × 8 kart, her sprint ~40–80 odak saat başlangıç hipotezi: **1120–2240 saat**, takvim vaadi değildir.
- Kapasite ve ilk G4 scope, kullanıcı playtest/veriyle her 2–3 sprintte güncellenir.
- G4 ilk inşa deneyimi / G5 modüler barınak / G6 utility progression / G7 güvenli dünya / G8 art ve perf / G9 alpha.

## Mevcut GDD tasarım kuralının değiştirilmesi

- GDD v0.2 LS-DOC-14 authored sockets ve claim existing buildings standardıdır. Controlled build zones + modular building bir **öneri** olarak G0 owner kararı gerektirir.
- S010 recipe yapısı tek ana input; bunu çok girdili hale dönüştürmek adapter/migration ile yapılır.
- S008/S010/S013/S023 implementasyonuna ait geçmiş testler bu yeni program için PASS kanıtı değildir.
