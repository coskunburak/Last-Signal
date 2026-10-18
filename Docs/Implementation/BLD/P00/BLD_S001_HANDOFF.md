# BLD-S001 — Ürün sözleşmesi handoff

2026-10-18 · Branch `s012-integrated-graybox-slice` · HEAD `fe0f4c70f839b36db25f6ccefc6b3e4f8e9519dd`.

Belge üretimi IMPLEMENTED; sprint kabulü PENDING_OWNER / TEST_NOT_RUN. Aşağıdaki her satır yalnız dokümantasyon tamamlanmasını gösterir; oyun implementasyonu, PASS veya G0 onayı değildir. Özgün kartların ID, kabul ve bağımlılıkları değişmedi. Her kart önceki karta bağlıdır; D001 sprint girişine, D008 G0 kararına bağlanır.

## Kart matrisi — INPUT → DECISION → DELIVERABLE → ACCEPTANCE

| Kart / kaynak | Girdi | Yeni/değişen paragraf ve karar | Belge durumu | Test | Kabul cümlesi / kanıt |
|---|---|---|---|---|---|
| [BLD-D001](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D001_ilk-surum-snr-degisikligi.md) | LS-DOC-01 §5; LS-DOC-14:18/26; LS-PLAN-M02:42 | [ADR-001; Önce / sonra](BLD_S001_PRODUCT_SCOPE_CHANGE.md); ADR-001 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | Eski tasarım silinmez; değişiklik açık ve onaya hazırdır. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |
| [BLD-D002](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D002_kullanc-hikayeleri.md) | D001; LS-DOC-02 §1/3/7 | [US-01–12](PLAYER_PROMISE_AND_STORIES.md); DEC-002 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | Her hikâyede tetik, geri bildirim, başarısızlık ve ödül tanımlanır. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |
| [BLD-D003](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D003_insa-alan-politikas.md) | D002; LS-DOC-14 §2 | [Coğrafya; query/result; ret tablosu](BUILD_ZONE_POLICY.md); DEC-003 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | Başlangıç build zone coğrafyası ve açık istisnalar listelenir. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |
| [BLD-D004](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D004_kapsam-snflamas.md) | D003; LS-PLAN-M02 scope | [D004 SC-01–13 ve kesme sırası](SCOPE_MATRIX_AND_BUDGET.md); DEC-004 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | 1.0 kapsamı sınırsız voxel, tam yıkım ve co-op ile karışmaz. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |
| [BLD-D005](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D005_survival-entegrasyonu.md) | D004; LS-DOC-12 §1–3; LS-DOC-14 §1/5 | [Değer tablosu ve ekonomi fixture](SURVIVAL_VALUE_MAP.md); DEC-005 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | Her MVP öğesinin oyuncuya somut faydası belgelenir. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |
| [BLD-D006](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D006_ilk-demo-senaryosu.md) | D005; US-01–12 | [Mutlu yol; alternatifler; G4](FIRST_PLAYABLE_SCENARIO.md); DEC-006 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |
| [BLD-D007](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D007_kapsam-butcesi.md) | D006; risk governance | [D007 efor, risk, stop/go](SCOPE_MATRIX_AND_BUDGET.md); DEC-007 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | Efor aralıkları garanti değil; erteleme sınırları net. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |
| [BLD-D008](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D008_karar-kaps.md) | D001–007; G0 kaynak kabulü | [Tutarlılık; owner handoff](BLD_S001_HANDOFF.md); DEC-008 | IMPLEMENTED; PROPOSED/PENDING_OWNER | TEST_NOT_RUN | G0 altında kabul edilmeden release kapsamı güncellendi sayılmaz. Owner static review ve ilgili çıktı; henüz kabul kanıtı yok. |

## D008 — Tutarlılık ve karar kapısı

BLD-P00-DEC-008, PENDING_OWNER; sahibi Burak. Seçim: ADR ve bütçeyi açık G0 imzasına bağla. Alternatif belge yazılmasını release onayı sayma reddedildi. Geri dönüş: önerileri DEFER/REJECT olarak sakla; tarihî kaynak değişmedi.

- Vaat → US-05 kalıcı çit; SC-04 minimum çit; demo aynı çiti kullanıyor. Ev SC-06 sonraki hedef; ilk playable'a taşınmadı.
- Zone → claimed authored alan; US-07/08 retleri ve demo varyantları aynı enumları kullanıyor. Koordinatlar OPEN.
- Ekonomi → her MUST için değer ve alternatif yol var; sıfır materyalle başarı yok. Örnek tarif sayıları balance onayı değil.
- Survival → US-12 inşa etmeyen oyuncuyu korur; claim dokunulmazlık sağlamaz.
- Tarihî karar → kaynak no-free-building paragrafı duruyor; ADR yalnız önerilen istisnayı gösteriyor.
- Takvim → yeni MUST kapsamı için garantisiz ek efor ve kesme sırası görünür. Tam release tarihi türetilmedi.
- Teknik → S002 bu ürün önerisine koşullu hazırlanır; UI mutation, runtime harvest veya scene authoring bu teslimde yok.

Bunlar belge içi iz bağlantılarıdır; owner tutarlılık kabulü beklenir. S001'in resmî kapanışı G0'da OD-01/02/03 ve static receipt ile olur. Şu anda P01 yetkisi yoktur.

## Owner'a devir

[ADR](BLD_S001_PRODUCT_SCOPE_CHANGE.md), [oyuncu vaadi](PLAYER_PROMISE_AND_STORIES.md), [zone](BUILD_ZONE_POLICY.md), [scope/bütçe](SCOPE_MATRIX_AND_BUDGET.md), [survival değeri](SURVIVAL_VALUE_MAP.md), [demo](FIRST_PLAYABLE_SCENARIO.md) somut adaydır. [Manual komutlar](MANUAL_VERIFY_COMMANDS.md) ve [G0 karar formu](G0_GATE_RECEIPT.md) kullanılır. Unity compilation/EditMode/PlayMode/build/perf/visual gate bu docs-only değişiklik için N/A—NO_RUNTIME_CHANGE; mevcut dirty Unity işlerinin kabulü hakkında iddia yoktur.
