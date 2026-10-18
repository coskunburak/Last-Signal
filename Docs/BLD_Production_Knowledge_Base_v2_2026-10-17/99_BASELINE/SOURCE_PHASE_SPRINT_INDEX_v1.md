# LAST SIGNAL — BLD PROGRAM PHASE SUMMARY

**28 sprint · 224 iş kartı · 10 gate**

| Phase | Sprint | Hedef | Gate |
|---|---|---|---|
| P00 | BLD-S001–BLD-S002 | Ürün sözleşmesi ve güvenli mimari | G0: onaylı kapsam farkı, teknik tasarım, şema ve bağımlılık matrisi |
| P01 | BLD-S003–BLD-S004 | Kaynak toplama ve ağaç kesme | G1: ağaç kesme/kaynak toplama + mülkiyet, yükleme ve gürültü doğrulaması |
| P02 | BLD-S005–BLD-S006 | Crafting çekirdeği ve Craft Defteri | G2: birden fazla malzemeli recipe, craft/iptal, notebook, save testleri |
| P03 | BLD-S007–BLD-S009 | Yapı yerleştirme çekirdeği | G3: geçerli/bozuk placement, kaynak korunumu ve save/load roundtrip |
| P04 | BLD-S010–BLD-S010 | Entegre ilk playable loop | G4: ağaç → tahta → defter → çit → save/load uçtan uca kabul |
| P05 | BLD-S011–BLD-S015 | Modüler ev ve yapı mekaniği | G5: küçük modüler barınak inşası, dolaşım, kapı, barınak durumu |
| P06 | BLD-S016–BLD-S019 | Sığınakla birleşen üretim ve bakım | G6: yaşayan sığınak + kapsamlı craft ve maintenance loop |
| P07 | BLD-S020–BLD-S022 | Dünya ekonomisi ve güvenilir kayıt | G7: regresyon, migration, exploit ve uzun oturum testleri |
| P08 | BLD-S023–BLD-S026 | Sanat, deneyim ve optimizasyon | G8: görsel kabul + performans + erişilebilirlik doğrulaması |
| P09 | BLD-S027–BLD-S028 | Alpha kabul ve scope kapatma | G9: alpha playtest, release scope ve teknik devir kararı |

## Sprint index

- **BLD-S001:** GDD kapsam ve oyuncu vaadi — BLD-D001–BLD-D008 · Bağımlılık: program girişi
- **BLD-S002:** Mimari sözleşme ve risk temeli — BLD-D009–BLD-D016 · Bağımlılık: S001
- **BLD-S003:** Harvestable resource nodes — BLD-D017–BLD-D024 · Bağımlılık: S002
- **BLD-S004:** Ağaç kesme oynanış ve geri bildirim — BLD-D025–BLD-D032 · Bağımlılık: S003
- **BLD-S005:** Recipe engine ve malzeme işlemeyi genişlet — BLD-D033–BLD-D040 · Bağımlılık: S003
- **BLD-S006:** Craft Defteri UI/UX — BLD-D041–BLD-D048 · Bağımlılık: S005
- **BLD-S007:** Placement validation engine — BLD-D049–BLD-D056 · Bağımlılık: S002,S006
- **BLD-S008:** Yeşil-kırmızı ghost ve etkileşim — BLD-D057–BLD-D064 · Bağımlılık: S007
- **BLD-S009:** Yapı commit ve kalıcı kayıt — BLD-D065–BLD-D072 · Bağımlılık: S007,S008
- **BLD-S010:** İlk entegre playable vertical slice — BLD-D073–BLD-D080 · Bağımlılık: S003–S009
- **BLD-S011:** Temel ve zemin yapı seti — BLD-D081–BLD-D088 · Bağımlılık: S010
- **BLD-S012:** Modüler duvar ve snap graph — BLD-D089–BLD-D096 · Bağımlılık: S011
- **BLD-S013:** Kapı, pencere ve çatı paketi — BLD-D097–BLD-D104 · Bağımlılık: S012
- **BLD-S014:** Yapı dayanımı, bağlantılar, inşa güvenliği — BLD-D105–BLD-D112 · Bağımlılık: S012,S013
- **BLD-S015:** Küçük barınak oynanabilir demo — BLD-D113–BLD-D120 · Bağımlılık: S011–S014
- **BLD-S016:** İşlevsel istasyonlar ve depolama — BLD-D121–BLD-D128 · Bağımlılık: S015
- **BLD-S017:** Onarım, söküm ve yer değiştirme — BLD-D129–BLD-D136 · Bağımlılık: S014,S016
- **BLD-S018:** Sığınak utility entegrasyonu — BLD-D137–BLD-D144 · Bağımlılık: S015–S017
- **BLD-S019:** Uzun vadeli üs progression ve G6 — BLD-D145–BLD-D152 · Bağımlılık: S016–S018
- **BLD-S020:** Kaynak yenilenmesi ve ekoloji — BLD-D153–BLD-D160 · Bağımlılık: S019
- **BLD-S021:** World streaming, save ve migration — BLD-D161–BLD-D168 · Bağımlılık: S009,S015,S020
- **BLD-S022:** Economy/pressure balance ve G7 — BLD-D169–BLD-D176 · Bağımlılık: S019–S021
- **BLD-S023:** Production asset kit ve animasyon — BLD-D177–BLD-D184 · Bağımlılık: S015
- **BLD-S024:** Ses, VFX ve kullanıcı hissi — BLD-D185–BLD-D192 · Bağımlılık: S023
- **BLD-S025:** Craft Defteri UX ve erişilebilirlik — BLD-D193–BLD-D200 · Bağımlılık: S006,S024
- **BLD-S026:** CPU/GPU ve büyük dünya optimizasyonu — BLD-D201–BLD-D208 · Bağımlılık: S023–S025
- **BLD-S027:** End-to-end alpha QA ve regresyon — BLD-D209–BLD-D216 · Bağımlılık: S010,S015,S019,S022,S026
- **BLD-S028:** Stabilizasyon, GDD güncelleme, teslim — BLD-D217–BLD-D224 · Bağımlılık: S027

Ayrıntılı görev ve kabul ölçütleri için `LastSignal_Construction_Crafting_Master_Production_Plan_v1_2026-10-17.md` dosyasına bakın.
