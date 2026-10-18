# LAST SIGNAL — P00 production execution teslimi

2026-10-18 · **Dokümantasyon IMPLEMENTED; kararlar PROPOSED/PENDING_OWNER; owner doğrulaması TEST_NOT_RUN; G0 PENDING_OWNER.**

BLD-S001 ve BLD-S002'nin tüm 16 günlük kartı ürün/architecture çıktısına dönüştürüldü. Üretim paketi otoriter baseline olarak kullanıldı; yerel kaynakla aynı olduğu için yeniden import edilmedi. Yeni Unity davranışı, sahne, prefab, runtime/editor kodu veya asset üretilmedi. P01'e geçilmedi.

Başlangıç: branch `s012-integrated-graybox-slice`, HEAD `fe0f4c70f839b36db25f6ccefc6b3e4f8e9519dd`. Önceden mevcut dirty işler [entry audit](ENTRY_AUDIT.md) içinde, teslim karşılaştırması [preservation raporunda](PRESERVATION_REPORT.md). Commit/push yok.

## Tam değişen belge listesi

Bu görev **yalnız aşağıdaki 22 yeni Markdown dosyasını** ekledi; var olan belge düzenlenmedi. Göreli yolların tamamı `Docs/Implementation/BLD/P00/` altındadır. Kaynak plan/GDD/Unity dosyaları bu listeye dahil değildir ve değiştirilmedi.

| Yeni belge | Somut çıktı / kart |
|---|---|
| [README.md](README.md) | Teslim indeksi, tam dosya ve durum matrisi |
| [ENTRY_AUDIT.md](ENTRY_AUDIT.md) | Branch/HEAD/dirty, kaynak entegrasyonu, mevcut/hedef/gap |
| [SOURCE_PRESERVATION_MANIFEST.md](SOURCE_PRESERVATION_MANIFEST.md) | 30 baseline source ve 92 protected dosya SHA256; tracked diff kimliği |
| [BLD_S001_PRODUCT_SCOPE_CHANGE.md](BLD_S001_PRODUCT_SCOPE_CHANGE.md) | D001, kontrollü inşa ADR'si, GDD önce/sonra, P00–P09 etki |
| [PLAYER_PROMISE_AND_STORIES.md](PLAYER_PROMISE_AND_STORIES.md) | D002, US-01–12, trigger/action/authority/feedback/cancel/reward/acceptance |
| [BUILD_ZONE_POLICY.md](BUILD_ZONE_POLICY.md) | D003, coğrafya/policy, ortak preview/commit ret enumu |
| [SCOPE_MATRIX_AND_BUDGET.md](SCOPE_MATRIX_AND_BUDGET.md) | D004/D007, SC-01–13, kesme sırası, solo efor/güven/stop-go |
| [SURVIVAL_VALUE_MAP.md](SURVIVAL_VALUE_MAP.md) | D005, ihtiyaç/maliyet/fayda/alternatif rota/sefer nedeni |
| [FIRST_PLAYABLE_SCENARIO.md](FIRST_PLAYABLE_SCENARIO.md) | D006, 15–30 dk hedefli rota ve ret kabulü |
| [BLD_S001_HANDOFF.md](BLD_S001_HANDOFF.md) | D008, kaynak/çıktı/kabul izi ve ürün karar paketi |
| [CURRENT_CODE_BOUNDARIES.md](CURRENT_CODE_BOUNDARIES.md) | D009, gerçek API/satır kanıtı ve yeni contract farkı |
| [BLD_S002_DOMAIN_OWNERSHIP.md](BLD_S002_DOMAIN_OWNERSHIP.md) | D010, tek state owner, command/event/lifecycle diyagramı |
| [CONTRACT_DATA_MODELS.md](CONTRACT_DATA_MODELS.md) | D011, node/recipe/build/structure schemas ve legacy adapter |
| [CONSTRUCTION_TRANSACTION_SEQUENCE.md](CONSTRUCTION_TRANSACTION_SEQUENCE.md) | D012, reserve/stage/commit/durable receipt/rollback ve fault akışları |
| [PERSISTENCE_CELL_AND_ID_POLICY.md](PERSISTENCE_CELL_AND_ID_POLICY.md) | D013, 64 m mevcut cell, tek owner/lease/tombstone/restore |
| [ASSET_SOURCE_AND_IMPORT_POLICY.md](ASSET_SOURCE_AND_IMPORT_POLICY.md) | D014, yeni asset 0; next-phase lisans/URP/pivot/LOD gate |
| [QA_AND_FAULT_MATRIX.md](QA_AND_FAULT_MATRIX.md) | D015, QA-Q01–21, risk ve future sprint/owner eşlemesi |
| [SAFETY_CHECKPOINT_AND_MIGRATION.md](SAFETY_CHECKPOINT_AND_MIGRATION.md) | D016, koruma/rollback ve FX-* legacy fixture rehberi |
| [BLD_S002_HANDOFF.md](BLD_S002_HANDOFF.md) | Mimari sprint acceptance ve açık risk devri |
| [G0_GATE_RECEIPT.md](G0_GATE_RECEIPT.md) | G0 N/A gerekçeleri, OD-01–05 owner karar ve imza formu |
| [MANUAL_VERIFY_COMMANDS.md](MANUAL_VERIFY_COMMANDS.md) | Owner-run MS-01–04, read-only Python kontrolü ve focused retest |
| [PRESERVATION_REPORT.md](PRESERVATION_REPORT.md) | Önce/sonra no-unrelated-changes kanıtı ve sınırları |

## BLD-D001–D016 durum matrisi

IMPLEMENTED = somut belge yazıldı. Hiçbir satır runtime implementasyonu veya PASS değildir. Özgün kabul, girdi, değişen paragraf ve çıktı linkleri ayrıntılı [S001](BLD_S001_HANDOFF.md) / [S002](BLD_S002_HANDOFF.md) matrislerinde bulunur.

| Kart | Çıktı | Belge | Karar | Owner test/kabul |
|---|---|---|---|---|
| BLD-D001 | ADR-001 / GDD change | IMPLEMENTED | PENDING_OWNER | TEST_NOT_RUN |
| BLD-D002 | US-01–12 | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |
| BLD-D003 | Zone/ret policy | IMPLEMENTED | PROPOSED; koordinatlar OPEN | TEST_NOT_RUN |
| BLD-D004 | SC-01–13 scope | IMPLEMENTED | PENDING_OWNER | TEST_NOT_RUN |
| BLD-D005 | Survival value | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |
| BLD-D006 | Playable demo contract | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |
| BLD-D007 | Budget/cut/dependency | IMPLEMENTED | PENDING_OWNER | TEST_NOT_RUN |
| BLD-D008 | S001 owner handoff | IMPLEMENTED | PENDING_OWNER | TEST_NOT_RUN |
| BLD-D009 | Current API map | IMPLEMENTED | PROPOSED integration boundary | TEST_NOT_RUN |
| BLD-D010 | Domain ownership | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |
| BLD-D011 | Data schemas/adapter | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |
| BLD-D012 | Transaction/durability | IMPLEMENTED | PROPOSED / PENDING_OWNER OD-04 | TEST_NOT_RUN |
| BLD-D013 | Cell/ID/persistence | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |
| BLD-D014 | Asset gate | IMPLEMENTED | PROPOSED; P00 asset 0 | TEST_NOT_RUN |
| BLD-D015 | QA/risk matrix | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |
| BLD-D016 | Checkpoint/migration | IMPLEMENTED | PROPOSED | TEST_NOT_RUN |

## Karar ve teknik öz

ADR-001 yalnız claimed shelter yakınındaki authored controlled zones için ilk ürün istisnasını önerir; eski no-free-building cümlesi yerinde durur. Voxel, tam yapısal çökme, dünya çapında sınırsız inşa/yıkım ve co-op OUT. İlk BLD-S010 playable çit + mevcut shelter/bench/storage kullanır; modüler ev BLD-S011–015, uzun vadeli üs BLD-S019 hedefidir. Yeni MUST takvim maliyetini gizlemez.

Gathering node'u, Inventory miktarı, Crafting işi, Construction yapıyı, Shelter claim/utility'yi sahiplenir. Persistence/WorldCells durable generation ve konum/lifecycle taşır; UI request/preview üretir. Mevcut S010 üç socket, recipe GUID/revision, storage-only üretim ve escrow davranışı korunur. Yeni source revisions/reservation/registry/lease API'leri öneridir.

En önemli kalan mimari riskler: bütün writer'ları kapsayan mutation gate; full-generation write latency ve crash reconciliation; node stable-ID authoring; nav/door clearance ve two-cell dependencies; legacy save fixtures ve schema uyumu. [QA matrisi](QA_AND_FAULT_MATRIX.md) her riskin kapanış sahibi/sprintini verir.

## G0 owner devir

OD-01 controlled scope, OD-02 takvim/MUST trade-off, OD-03 isteğe bağlı survival/demo ve initial limit yaklaşımı, OD-04 durability/freeze tercihi, OD-05 legacy adapter/schema/respawn yaklaşımı açık karar bekler. Gerçek zone ölçüleri ve balance değerleri ayrıca future authoring gate'lerinde kapanır.

[Manual doğrulama](MANUAL_VERIFY_COMMANDS.md): önce git görünümü, sonra tek read-only Python komutuyla sources/outputs/preservation, ardından içerik spot-check ve [G0 formu](G0_GATE_RECEIPT.md). Unity/runtime gate'leri bu P00 docs-only değişiklik için N/A—NO_RUNTIME_CHANGE. Performance NOT_MEASURED. Owner sonucu olmadan PASS veya G0 APPROVED ilan edilmez. Burada durulur; P01 için ayrı açık talimat gerekir.
