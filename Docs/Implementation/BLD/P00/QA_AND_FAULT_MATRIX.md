# D015 — Risk ve owner-run kabul matrisi

BLD-P00-DEC-015 · PROPOSED · Belge IMPLEMENTED / TEST_NOT_RUN. Girdi D015, [işlem](CONSTRUCTION_TRANSACTION_SEQUENCE.md), [schema](CONTRACT_DATA_MODELS.md), [cell](PERSISTENCE_CELL_AND_ID_POLICY.md), [ürün](BLD_S001_HANDOFF.md); [kart izi](BLD_S002_HANDOFF.md).
Karar sahibi ve bütün testlerin çalıştırıcısı: **Burak**. Agent gelecekte fixture/implementasyonu hazırlayabilir; bu görevde test yazmadı, çalıştırmadı. Aşağıdaki QA-Q kimlikleri plan senaryolarıdır; mevcut NUnit test adları değildir.

## P00 ve gelecekteki gate ayrımı

P00 manual static MS-01 git/preservation, MS-02 kaynak hash/ID/link, MS-03 içerik/traceability, MS-04 owner karar formu: NOT_RUN_BY_OWNER. [Komutlar](MANUAL_VERIFY_COMMANDS.md).
Unity compilation, EditMode, PlayMode, build, performance benchmark ve gameplay/visual: **N/A—NO_RUNTIME_CHANGE**. Yalnız Markdown eklenmiştir; bu N/A mevcut kirli VisualSlice işlerinin testi için geçerli değildir. Gelecek runtime matrisi TEST_NOT_RUN, performance NOT_MEASURED, görsel kabul NOT_APPROVED.

## Risk → girdi → beklenen sonuç → seviye / owner / sprint

Her satırın executor'ı Burak'tır; domain sütunu implementasyon sorumlusunu gösterir. Kanıt en az branch/HEAD, source hash, fixture ID, runner/filter, actual/expected, log/receipt içerir. Gameplay/perf satırlarında gerçek cihaz/kamera/ışık/instance sayısı da gerekir.

| ID / risk | Fixture / hata enjeksiyonu | Beklenen assertion / receipt | Seviye | Domain / future sprint |
|---|---|---|---|---|
| QA-Q01 multi-input kayıp/dupe | A=2,B=1 maliyet; A=2,B=0 ve tam stok; cancel | Eksikte her item delta 0; başarıda tam vector; escrow+inventory+output korunumu | EditMode domain | Inventory/Crafting, BLD-S005 |
| QA-Q02 revision ve craft/build race | Preview rev n; stok n+1; aynı frame craft/build | SourceChanged; yalnız bir geçerli rezervasyon; toplam harcama available'ı aşmaz | EditMode + PlayMode | Inventory/Construction, BLD-S005/009 |
| QA-Q03 invalid zone/claim | No claim, missing policy, bounds dışı footprint, su | Aynı query/commit enum; delta 0; metin | EditMode + gameplay | Construction, BLD-S007 |
| QA-Q04 slope/overlap/snap | Eşik alt/eşit/üst; rotated clearance; occupied/unknown/cyclic snap; buffer saturation | Policy deterministik; invalid ret, 0 tüketim; doygun query fail-closed | EditMode + PlayMode | Construction, BLD-S007/012 |
| QA-Q05 nav/kapı/kaçış | Kapı sweep, POI/loot, player/vehicle spawn reserve ve zombie corridor | PathBlocked; açık alternatif rota; geçerli yapıda nav/collision ready | PlayMode + visual/gameplay | WorldCells/Construction, BLD-S007/010/013 |
| QA-Q06 cancel/refund/full output | İş 0%, %99; output/refund dolu; completion ile cancel aynı boundary | Running cancel escrow'u tek kez bekletir; completed output bir kez; legacy state değişmez | EditMode + PlayMode persistence | Crafting/Shelter, BLD-S005 |
| QA-Q07 nonce/retry | Same nonce iki kez, aynı key farklı payload, lost response, pruned nonce | Tek receipt/instance/maliyet; NonceConflict; expired nonce yeniden harcamaz | EditMode + persistence integration | Construction/Persistence, BLD-S009 |
| QA-Q08 prefab/observer exception | Staging Awake/prepare throw, tentative callback throw, postcommit observer throw | Önce abort+exact cleanup; tentative rollback; durable sonrası refund yok, projection retry | PlayMode + fault integration | Construction, BLD-S009 |
| QA-Q09 death/pause/restart/disable | Step 0/4/6/8/10'da lifecycle olayı; stale epoch callback | Yeni command ret veya queued lifecycle; ne stranded gate ne ghost/collider kalır; durable state korunur | PlayMode | Session/Presentation, BLD-S008/009 |
| QA-Q10 save/unload/reload | Build→save→unload/load üç tekrar→remove→save/load; resident ve unloaded | 1 unique active ID sonra 0 active+1 tombstone; maliyet tekrar uygulanmaz | Persistence integration + PlayMode | WorldCells/Persistence, BLD-S009/021 |
| QA-Q11 two-cell/snap owner | x=64 sınırı ve negatif x; root A/child B; B önce load; A unload isteği | MVP UnsupportedCellSpan; destek açıldığında tek root owner, leases, 1 projection, graph restore | EditMode + PlayMode | WorldCells/Construction, BLD-S012/021 |
| QA-Q12 old save/schema | Legacy job Running/WaitingOutput/WaitingRefund; yeni section yok; bad revision, unknown schema, alias cycle | Legacy miktar/ID/süre korunur; yeni state empty; invalid controlled ret; kaynak dosya değişmez | EditMode + persistence integration | Persistence/Crafting, BLD-S005/009/021 |
| QA-Q13 crash/disk failure | Her D012 boundary'de kill/fault; disk full, truncated temp, publish sonucu belirsiz | Bir tam eski veya yeni generation; iki taraflı debit/structure; no auto-overwrite; receipt reconciliation | Persistence fault integration (owner ortamında) | Persistence, BLD-S009/021 |
| QA-Q14 instance/perf bütçesi | Aynı cihaz/kamera: 0,16/cell,32/world; foliage+ghost+unloaded delta | CPU/GPU/GC/save byte/latency baseline/candidate raporu; owner eşikleri ölçüm öncesi sabitler; eşik yoksa NOT_MEASURED/NO_PERF_ACCEPTANCE | Performance | Art/Construction/Persistence, BLD-S010/026 |
| QA-Q15 FPS preview/erişilebilirlik | Select/rotate/cancel 100 döngü; renk ayırmadan kullanım; pause/disable/input focus | Collider/AI/loot mutation 0; ghost/material/listener büyümesi yok; metin/ikon okunur, input geri gelir | PlayMode + visual/gameplay | Presentation, BLD-S008/025 |
| QA-Q16 node tükenme/respawn | Son vuruş duplicate; save/load; forest layout revision; sleep | Node generation yield bütçesi aşılmaz; yeni world pickup iki kez oluşmaz; P01 respawn kapalı; reauthor alias gerekirse ret | EditMode + persistence | Gathering, BLD-S003/004/020 |
| QA-Q17 clock/job/offscreen | Aynı elapsed tek büyük ve çok küçük adım; pause/sleep/unload | World job eşdeğer sonuç; sim job unloaded ilerlemez; lastProcessed tek tüketilir; legacy duration korunur | EditMode + PlayMode | Crafting/WorldTime, BLD-S005/018/021 |
| QA-Q18 ürün değeri/grind | Demo mutlu yol + hiç inşa etmeyen rota; sıfır materyalle confirm | 15–30 dk hedef kaydı, failure sebepleri; ana survival açık; ücretsiz yapı yok; çitin faydası gözlenir | Gameplay/visual | Product, BLD-S010/015/019/022 |
| QA-Q19 legacy regresyon | Üç socket/resident; storage-only recipe; eski pickup/drop/save/cell fixtures | S010/S005 davranışı ve GUID aynı; new facade duplicate authority yaratmaz | EditMode + PlayMode integration | Shelter/Inventory/Persistence, BLD-S005/009/021 |
| QA-Q20 malformed data | Null definition, negatif/overflow quantity, NaN pose, duplicate ID, foreign world | Mutation 0; typed controlled ret, orijinal save bytes aynı | EditMode + persistence | Catalog/Persistence, BLD-S003/005/009 |
| QA-Q21 source/license/art drift | Exact URL/license eksik; source hash değişimi; wrong pivot/PBR | Asset gate açık kalır; vendor korunur, staged variant; gündüz/dusk/yağmur/gece owner kabulü | Static + visual | Art/owner, BLD-S004/011/023 |

## Açık risk ve kapatma sahibi

| Risk | Seviye / kapanış | Mitigation / kabul eksiği |
|---|---|---|
| R01 crash-safe source+registry commit | Kritik, G3 öncesi | QA-Q01/02/07/08/13; yeni gate bütün writer'lara bağlanmadı; full-save latency ölçülmedi |
| R02 stable forestry identity/kalıcılık | Yüksek, G1 | QA-Q16/20; dekoratif forest ile resource manifest authoring eşlemesi yapılmadı |
| R03 many structures/cell span/nav | Yüksek, G3/G5/G7 | QA-Q04/05/10/11/14; single-cell MVP, konservatif corridor; dinamik nav algoritması OPEN |
| R04 legacy schema/job uyumu | Kritik, G2/G3 | QA-Q06/12/17/19; fixtures kullanıcıda, additive section code yok |
| R05 erişilebilir UX/lifecycle | Yüksek, G3/G4 | QA-Q09/15; ghost render-only, typed ret, input ownership |
| R06 sanat/lisans ve VisualSlice çatışması | Yüksek, asset gate | QA-Q21 + MS-01; mevcut dirty source, native görsel/lisans kabulü devralınmadı |
| R07 scope/takvim/grind | Yüksek, G0/G4/G5/G6 | QA-Q18 + MS-03/04; budget düşük güven, OD-01/02 kararı gerekir |
| R08 incomplete generation/unknown catalog | Kritik, G3/G7 | QA-Q12/13/20; fail-closed load, immutable originals, recovery UX |

## Gerçek tooling ve yürütme sınırı

Yerel [Tools/s010-verify.sh](../../../../Tools/s010-verify.sh) Unity batchmode kullanır ve tarihî evidence klasörüne yazar; P00 için çağrılmayacak. [Tools/s023-verify.py](../../../../Tools/s023-verify.py) mevcut owner runner'ıdır; burada çalıştırılmadı. Gelecekte yeni fixture eklendiğinde gerçek runner/filter ve ayrı evidence path tekrar doğrulanmalıdır; tarihî kayıtların üzerine yazılmaz.

Mevcut assemblies: [EditMode](../../../../Assets/LastSignal/Scripts/Tests/EditMode/LastSignal.EditModeTests.asmdef), [PlayMode](../../../../Assets/LastSignal/Scripts/Tests/PlayMode/LastSignal.PlayModeTests.asmdef). Referans test dosyaları: ShelterDomainTests, InventorySnapshotTests, WorldCellTests, ShelterIntegrationTests, WorldCellIntegrationTests; bu dosyaların tam test adları veya yeni QA-Q senaryolarını kapsadığı iddia edilmez. P00 Unity runner komutu sunulmaz.

Seçilen yöntem risk başına focused fixture, sonra integration/regression; alternatif her değişimde bütün Unity suite'i koşma P00 için N/A. FAIL'de yalnız ilgili kanıt → root cause → en küçük belge/kod düzeltmesi (izinli kapsamda) → **yalnız başarısız kontrolün exact retest komutu** → owner sonucu bekleme. Eski PASS yeni HEAD'e taşınmaz. Geri dönüş hatalı fixture/hipotezi revize eder, güvenlik assertion'ını gevşetmez.
