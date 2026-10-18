# BLD-S002 — Teknik sözleşme handoff

2026-10-18 · Branch `s012-integrated-graybox-slice` · HEAD `fe0f4c70f839b36db25f6ccefc6b3e4f8e9519dd`.

Belge üretimi IMPLEMENTED; mimari kararlar PROPOSED / ürün ADR'sine koşullu; sprint kabulü PENDING_OWNER / TEST_NOT_RUN. `ConstructionService`, definition/state/reservation/lease/gate isimleri uygulanmış sınıflar değildir. D009 S001 tasarımına; D010–016 önceki karta bağlıdır. Kaynak kartlar değiştirilmedi.

## Kart matrisi — INPUT → DECISION → DELIVERABLE → ACCEPTANCE

| Kart / kaynak | Girdi | Yeni paragraf / karar / çıktı | Belge durumu | Test | Kabul cümlesi / kanıt |
|---|---|---|---|---|---|
| [BLD-D009](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D009_mevcut-kod-snr.md) | S010/S005; SaveSession; WorldCells; interaction/time | [API/sahiplik tablosu](CURRENT_CODE_BOUNDARIES.md); DEC-009 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Daha önce çalışan kodun yeniden yazılması engellenir. Belge adayı hazır; owner static evidence henüz yok. |
| [BLD-D010](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D010_domain-sahipligi.md) | D009; architecture governance | [Authority matrisi; event/restart](BLD_S002_DOMAIN_OWNERSHIP.md); DEC-010 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Her verinin tek authoritative owner'ı tanımlıdır. Belge adayı hazır; owner static evidence henüz yok. |
| [BLD-D011](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D011_veri-tanmlar.md) | D010; LS-DOC-07/14/17 | [Definition/state; adapter; migration](CONTRACT_DATA_MODELS.md); DEC-011 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Stable ID, revision, prefab ve bağımlılıklar doğrulanabilir. Belge adayı hazır; owner static evidence henüz yok. |
| [BLD-D012](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D012_islem-sralamas.md) | D011; transaction governance; gerçek Exchange/SaveStore | [Basamaklar; durability; fault tablosu](CONSTRUCTION_TRANSACTION_SEQUENCE.md); DEC-012 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir. Belge adayı hazır; owner static evidence henüz yok. |
| [BLD-D013](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D013_dunya-adresleme.md) | D012; CellCoordinate; LS-DOC-13/17 | [Adres; two-cell; restore/tombstone](PERSISTENCE_CELL_AND_ID_POLICY.md); DEC-013 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Restore duplicate yapı ve iki hücre sahipliği üretmez. Belge adayı hazır; owner static evidence henüz yok. |
| [BLD-D014](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D014_icerik-import-ilkeleri.md) | D013; S013/S023/forest kaynakları | [Asset gate; risk matrisi](ASSET_SOURCE_AND_IMPORT_POLICY.md); DEC-014 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Kaynak ve kullanım raporu hazırlanır. Belge adayı hazır; owner static evidence henüz yok. |
| [BLD-D015](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D015_test-matrisi.md) | D014; manual QA; Tools/asmdef yolları | [QA-Q01–21; risk/runner eşlemesi](QA_AND_FAULT_MATRIX.md); DEC-015 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Her risk bir hedefli teste eşlenmiştir. Belge adayı hazır; owner static evidence henüz yok. |
| [BLD-D016](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/DAYS/BLD-D016_koruma-noktas.md) | D015; entry/source manifest; LS-DOC-17 | [Koruma; FX fixture; reversal](SAFETY_CHECKPOINT_AND_MIGRATION.md); DEC-016 | IMPLEMENTED; PROPOSED | TEST_NOT_RUN | Kod üretimine girişte kaynak bütünlüğü korunabilir. Belge adayı hazır; owner static evidence henüz yok. |

## Teknik karar özeti ve kabul açıkları

- Legacy S010 resident/üç socket/tek input/world-time/storage-only üretim aynen kalır. Yeni recipe facade legacy işin ikinci sahibi değildir.
- Inventory reservation bütün ingredient vector'ünü kaynak revisions ile yönetir. Mevcut TryRemove/Exchange genel multi-owner rollback garantisi olarak kullanılmaz.
- Construction registry tek yapı sahibi; cell yalnız konum/lifecycle ve unloaded DTO custody sağlar. İlk playable single-cell; two-cell sonrası tek owner + neighbor lease.
- Proposed durable commit: inactive staging, no-throw tentative mutation, immutable generation + receipt, publish sonrası presentation. Callback exception ve ambiguous disk publish ayrı recovery dallarıdır.
- Definition ID/revision, mutable state revision, catalog hash, cell epoch ve save generation ayrı kavramlardır. ItemDefinition için mevcut olmayan source revision API'si varsayılmadı.
- World clock, restore event suppression, tombstone ve nonce high-water yeni repeat/reload kazancını engelleyen kontratlardır; kanıtları gelecek runtime QA'dadır.
- P00 yeni asset 0. Mevcut kaynaklar lisans/URP/native art kabulü yapılmış sayılmadı.

Kritik açıklar: global mutation gate'in bütün writer'lara future integration'ı; full-generation write latency; gerçek zone/corridor ölçüleri; forest stable identity authoring; iki-cell/nav dependency ve eski save fixture'ları. Bunlar P00 tasarım üretiminin değil gelecek runtime kabulünün eksikleridir. Owner'ın OD-04/05 kararları ve MS-01–04 kanıtı G0 için gereklidir.

## Kabul ve sonraki adım

[QA/fault matrisi](QA_AND_FAULT_MATRIX.md), [koruma raporu](PRESERVATION_REPORT.md), [manual komutlar](MANUAL_VERIFY_COMMANDS.md), [G0 receipt](G0_GATE_RECEIPT.md). Unity compilation/EditMode/PlayMode/build/perf/gameplay bu yalnız Markdown değişikliği için N/A—NO_RUNTIME_CHANGE. Tarihî S010/S023 PASS kayıtları devralınmadı. Runtime implemented, performance measured veya G0 approved iddiası yoktur.

S002 bu adayla devredilir; owner static sonuçları ve scope/architecture kararını bekler. P01'e otomatik geçiş yoktur.
