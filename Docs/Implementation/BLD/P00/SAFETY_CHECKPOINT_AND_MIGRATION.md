# D016 — Koruma noktası ve migration fixture devir sözleşmesi

BLD-P00-DEC-016 · PROPOSED · Belge IMPLEMENTED / TEST_NOT_RUN. Girdi D016, [entry audit](ENTRY_AUDIT.md), [checksum manifest](SOURCE_PRESERVATION_MANIFEST.md), LS-DOC-17 §4–7. Sahip Burak. Uygulama P00 belge koruması; gelecek fixture ve migration BLD-S005/009/021. [Kart izi](BLD_S002_HANDOFF.md).

## Korunan kaynak ve geri alma

Branch/HEAD ve başlangıç dirty listesi ENTRY_AUDIT'tedir. P00 çıktıları yalnız `Docs/Implementation/BLD/P00/` altında yeni Markdown belgeleridir. Kaynak P00 phase/sprint/day/governance ve GDD kaynakları değiştirilmedi. Source manifest 30 paket dosyasının hash eşleşmesini, paket kimliğini, 92 ilgili dosyanın başlangıç checksum'unu ve tracked diff hash'ini saklar. [Son koruma raporu](PRESERVATION_REPORT.md) teslim anı farkını ayırır.

FirstTownCore, FirstTownCore_VisualSlice, terrain, prefabs, runtime/editor scripts, shaders, animations, .meta GUID ve ThirdParty üzerine işlem yapılmadı. Önceden dirty olan Lighting.asset, CanopyMetalwork.asset, CanopySheet.asset, VisualSlice scene, SliceRefinement05.cs, SliceVisualReview.cs ve REFINEMENT_05.md değiştirilmedi. Koruma kanıtı disk düzeyindedir; Unity unsaved editor state veya runtime regresyon PASS'i değildir.

Rollback yalnız bu yeni P00 belgelerini dosya listesine göre inceleyip kaldırma/ayrı arşivleme yoludur; kaynak KB untracked olduğu için klasör çapında clean/delete yapılmaz. `git reset --hard`, `git clean`, destructive checkout, otomatik commit/push/merge yok. Kaynaklarda sonradan drift görülürse eski hash'e zorla restore etmek yerine yeni user edit'i karşılaştır, etkilenen teknik kanıtı yenile. Mevcut dirty işler için bu görev yedek veya commit üretmedi; owner'ın mevcut koruma akışı sürer.

## Sonraki schema değişiminden önce fixture paketi

Gerçek save dosyaları bu görevde okunmadı/kopyalanmadı; mevcut fixture varmış gibi anlatılmaz. Owner yeni runtime phase öncesi bilinen çalışan build/catalog/source checksum ile **kopya** fixture üretir. Önerilen evidence konumu P00 klasöründe `Evidence/OWNER-YYYYMMDD-G0/`; gelecekte runtime kanıtı ilgili BLD sprintine ait ayrı dizinde tutulur. Bunlar henüz yaratılmış test çıktıları değildir.

| Fixture ID | Kaydedilecek durum | Migration sonrası invariant / gerekli kanıt |
|---|---|---|
| FX-LEGACY-EMPTY | Construction/node/new craft section olmayan eski save; mevcut storage/claim | Yeni sections empty; eski ID/quantity/clock aynı; source bytes checksum değişmez |
| FX-S010-RUN | Aktif legacy job, recipe revision, escrow, %0/%99 progress | Aynı job ID, recipe, duration, input escrow; yeni executor'a taşınmaz |
| FX-S010-WAIT | CompletedWaitingOutput ve CancelledWaitingRefund; dolu storage | Çıktı/iade bir kez collect; yer yoksa bekler; kayıp yok |
| FX-CELL | Resident + visited/unloaded cell + world pickup + clock | Unloaded delta korunur; tombstone/item duplicate yok |
| FX-NEW-STRUCT | Bir yapı, receipt, sınırlı instance registry; daha sonra tombstone | Aynı instanceId/owner/pose; removed yeniden doğmaz |
| FX-NODE | Depleted node, yield pickup, partially collected item | Node yield bütçesi/pickup/Inventory conservation, respawn kapalı |
| FX-ALIAS | Rename definition, moved cell/zone ve parent graph | Alias acyclic/birebir; IDs korunur; duplicate ret |
| FX-BAD | Bozuk checksum, truncated temp, future schema, missing critical definition | Controlled error, orijinal/backup korunur, autosave engelli |

Her fixture manifest: fixtureId, oluşturma tarihi/timezone, source branch/HEAD, build/catalog/schema/definition revisions, original SHA256, beklenen inventory vector/job/node/structure IDs, owner'ın üretim komutu ve receipt. Gizli veya kişisel veri gerekiyorsa paylaşım kapsamı ayrıca seçilir; P00 bunları toplamaz.

## Güvenli geçiş ve reversal

Önce copy-only fixture → decode/validate → deterministic schema steps → compatibility checks → quantity/ID invariants → yeni generation'a yaz → owner focused test → gerçek slot için ayrı release kararı. Migration başarısızsa original+backup korunur, sıfır state ile üstüne yazılmaz. Down-migration otomatik garanti değildir; yeni formatla oynanan süreyi silmeden eski binary'ye dönülemez. Eski build/definitions + eski generation birlikte saklanır; yeni definition kaldırılırsa explicit replacement/recovery gerekir.

P00 bağımlılık sırası: ADR/player/scope → domain ownership → schema → transaction → cell policy → QA → G0. Source drift bu zincirde etkilenen ilk belgeye dönülerek düzeltilir. Yeni runtime/schema zorunluluğu doğarsa bu phase'e gizlice eklenmez; ilgili owner kararı ve sonraki phase planına taşınır.

Seçim source-preserving additive dokumentasyon; alternatif kullanıcı dirty tree'sini temizleme veya GDD'yi peşinen onaylama reddedildi. Geri dönüş yeni belge paketiyle sınırlıdır. D016 kabulü: owner MS-01/02 ve fixture hazırlık sorumluluğunu doğrular; P01 şu anda BLOCKED_GATE/PENDING_OWNER. Bütün feasible P00 belge işi tamamlanmış olabilir, bu gelecek fixture'ların veya G0'ın PASS olduğu anlamına gelmez.
