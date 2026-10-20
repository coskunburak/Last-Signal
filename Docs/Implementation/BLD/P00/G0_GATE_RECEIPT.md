# G0 — P00 kabul ve owner karar receipt'i

**Durum: ACCEPTED — G0 DESIGN/DOCUMENTATION GATE.** Owner kararı: 2026-10-18, Europe/Istanbul; kullanıcı bu sohbet içinde G0 kapanışını istedi. Branch `s012-integrated-graybox-slice`; özgün P00 hazırlık baz HEAD'i `fe0f4c70f839b36db25f6ccefc6b3e4f8e9519dd`; P00 belgelerinin push commit'i `76fe1cb66a3309f7fd9c42782bbf2a0549be9d03`. Bu geçmiş baz kanıtı değiştirilmez.

P00 D001–D016 belge çıktıları hazırlanmış, owner tarafından sağlanan `git status`/`git diff --check` kanıtı ile `P00 STATIC all: 0 issue(s)` sonucu (üç çalıştırma) değerlendirilmiş ve OD-01–05 için aşağıdaki aday kararlar G0 tasarım bazına kabul edilmiştir. IMPLEMENTED yalnız belge üretimi demektir; yeni harvesting/construction runtime uygulanmış veya test edilmiş değildir. **P01 araştırma ve sprint hazırlığına geçilebilir; P01 Unity/kod uygulaması ayrıca açık yürütme talimatı gerektirir.**

Kaynak: [özgün G0 kabulü](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/GATES/G0_ACCEPTANCE.md), [P00 phase](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/PHASE.md). Kaynak şablondaki runtime gate'leri, kullanıcının açık docs-only/manual-verification talimatına göre aşağıda gerekçeli N/A olarak ele alınır; şablonun kendisi değiştirilmedi.

## Kabul receipt'i

| Alan | Teslim / durum | Owner kanıtı |
|---|---|---|
| Kapsam farkı ve ADR | [ADR-001](BLD_S001_PRODUCT_SCOPE_CHANGE.md), tarihî GDD korunmuş; G0 OWNER_APPROVED | Owner scope approval (OD-01), 2026-10-18 |
| Oyuncu vaadi/alternatif survival | [US-01–12](PLAYER_PROMISE_AND_STORIES.md), [değer](SURVIVAL_VALUE_MAP.md), [demo](FIRST_PLAYABLE_SCENARIO.md) | Owner design approval (OD-03), 2026-10-18 |
| MUST/OUT ve takvim | [SC-01–13/bütçe](SCOPE_MATRIX_AND_BUDGET.md), düşük güven aralıkları | Owner scope/budget approval (OD-02), 2026-10-18 |
| Teknik tasarım/schema/dependency | [S002](BLD_S002_HANDOFF.md), DEC-009–016 | Owner architecture direction (OD-04/05), 2026-10-18 |
| Günlük kartlar | D001–D016 belge IMPLEMENTED; [S001](BLD_S001_HANDOFF.md)/[S002](BLD_S002_HANDOFF.md) acceptance matrisi | MS-02 PASS, 0 issues ×3; MS-03 content cross-check + owner design acceptance |
| Branch/source preservation | [Entry](ENTRY_AUDIT.md), [manifest](SOURCE_PRESERVATION_MANIFEST.md), [son rapor](PRESERVATION_REPORT.md) | MS-01 evidence provided (7 pre-existing dirty VisualSlice files); MS-02 PASS |
| Compilation/EditMode/PlayMode/build | N/A—NO_RUNTIME_CHANGE; sadece Markdown, test veya assembly/runtime değişmedi | P00 için Unity komutu yok |
| S010/S023 regression/gameplay/visual | N/A—NO_RUNTIME_CHANGE; legacy davranış/scene değişmedi; mevcut dirty işler ayrıca owner kapsamı | Geçmiş PASS taşınmadı |
| Save/load/cancel/duplicate/unloaded | P00 yürütmesi N/A—NO_RUNTIME_CHANGE; future QA-Q01–13/16–20 TEST_NOT_RUN | Runtime phase owner kanıtı henüz yok |
| Performance | NOT_MEASURED; P00 benchmark N/A—NO_RUNTIME_CHANGE | Gelecekte QA-Q14 |
| Yeni asset/GUID | Yeni harici asset 0; GUID değişimi 0; [asset policy](ASSET_SOURCE_AND_IMPORT_POLICY.md) | MS-01 |
| Migration | Strateji/fixture rehberi hazır; schema/fixture implementasyonu yok | FX-* future; source dosyalar korunacak |
| Sonraki phase | G0 ACCEPTED / P01 research READY | P01 runtime execution separate authorization required |

## G0'da kabul edilen owner kararları

| ID / durum | Somut karar adayı | Alternatif / trade-off | Son karar noktası |
|---|---|---|---|
| OD-01 ACCEPTED | ADR-001 kontrollü, claimed shelter yakınındaki authored zone istisnası; voxel/co-op/sınırsız inşa OUT | Yalnız legacy socket kapsamını sürdürmek yeni oyuncu vaadini daraltır | G0 |
| OD-02 ACCEPTED | SC-01–09 MUST, modüler barınak sonraki hedef; ek takvim ve COULD kesme sırası | Hangi MUST ertelenecekse ID ve downstream etkisi açıkça seçilir; veri güvenliği kesilmez | G0, yeniden G4/G5 |
| OD-03 ACCEPTED | İnşa isteğe bağlı; 15–30 dk demo hedefi, gerçek koordinatlar BLD-S007'de; ilk playable tek-cell/tek çit | Daha büyük ilk demo art/QA maliyetini yükseltir; 16/cell,32/world limitleri ölçülene kadar yalnız başlangıç hipotezi | G0 yaklaşım; sayılar BLD-S007/G4 |
| OD-04 ACCEPTED | Candidate full generation + mutation freeze + durable receipt; kısa bekleme UX ve disk maliyeti | WAL/journal daha karmaşık; latency uygunsuzsa ayrı ADR, güvenliği gevşetme yok | G0 yaklaşım; ölçüm G3 |
| OD-05 ACCEPTED | Legacy recipe adapter/facade, yeni versionlı sections, node respawn başlangıçta kapalı; future two-cell leases | Legacy rewrite yüksek risk; respawn/graph genişlemesi sonraki gate kararı | G0; BLD-S005/020/021 öncesi detay |

OPEN implementation parametreleri: gerçek BuildZone koordinat/ölçek/slope/clearance, catalog maliyet/ID/source revision değerleri, nav algoritması, receipt retention/latency ve perf eşikleri, resource authoring ID map, license entitlement ve gerçek legacy fixtures. Her birinin sahibi Burak + ilgili future domain/sprint; [QA risk matrisi](QA_AND_FAULT_MATRIX.md) kapanış gate'lerini verir. Bunlar onaylanmış oyun değerleri veya P00 içinde yapılmış runtime çalışma değildir.

## G0 üç ürün riski

- Sıfır materyalle upgrade: SC-02/04, US-06, QA-Q01/18 → ret ve delta 0. Belge karşılığı var; oyun sonucu yok.
- İnşa istemeyen oyuncu softlock: US-12 ve demo alternatif rota → su/gece/ana ilerleme erişilebilir. Belge karşılığı var; playtest yok.
- Voxel/tam yıkım/multiplayer sızması: ADR OUT ve SC-13 → sonraki değişiklik kararı olmadan uygulanamaz.

## G0 owner kapanış kaydı — 2026-10-18

- **Karar yetkisi:** Proje sahibi G0 kapanışını bu sohbet üzerinden açıkça talep etti. Bu kayıt sohbet temelli ürün onayıdır; kriptografik imza veya bağımsız elle yazılmış test raporu değildir.
- **Branch ve kaynak baz:** `s012-integrated-graybox-slice`; P00 araştırma/snapshot bazında HEAD `fe0f4c70f839b36db25f6ccefc6b3e4f8e9519dd`; belgelerin GitHub'a gönderilmiş commit'i `76fe1cb66a3309f7fd9c42782bbf2a0549be9d03`. `SOURCE_PRESERVATION_MANIFEST.md` önceki kayıt olarak korunur ve yeni HEAD'e göre geriye dönük yeniden yazılmaz.
- **MS-01:** Kullanıcının gönderdiği terminal çıktısı incelendi. `git diff --check` temiz; görülen yedi tracked değişiklik ayrı FirstTownVisualSlice işine ait. Kapsam dışı değişiklikler bu P00 gate ile onaylanmaz. Ayrı log dosyası sağlanmadı.
- **MS-02:** Kullanıcı `P00 STATIC all: 0 issue(s)` sonucunu ardışık üç çalıştırmada bildirdi; **STATIC_PASS**. Bu yalnız kontrollü doküman/source/koruma doğrulamasıdır, runtime PASS değildir.
- **MS-03:** G0 receipt, ADR, oyuncu hikâyeleri, kapsam matrisi, ilk demo, S001/S002 handoff, transaction ve persistence sözleşmeleri bu değerlendirmede çapraz incelendi. Ayrı owner MS-03 terminal/inceleme dosyası bulunmuyor; ürün sahibinin G0 kapanış talebi belgelenmiş karar alternatiflerinin kabulü olarak yorumlandı. Yanlış veya istenmeyen kapsam kararı saptanırsa yeni ADR ile düzeltilmelidir.
- **OD-01 — ACCEPTED:** Claim edilmiş sığınağın yakınındaki authored build zone içinde sınırlı modüler inşa. Voxel, dünya çapında sınırsız yapı/yıkım, co-op **OUT**.
- **OD-02 — ACCEPTED:** SC-01–09 ürün kapsamı **MUST**, teslimler milestone'a bölünmüş. İlk playable'da tam ev gerekmez; güvenlik, tek sahiplik ve kayıt korunur. Zaman ve COULD kesintileri ölçümle yeniden ele alınır.
- **OD-03 — ACCEPTED:** İnşa isteğe bağlı, ana survival/quest yolu inşasız da mümkün. 15–30 dakika ilk playable **hedef**, ölçüm değil; tek-cell/tek çit pilot. 16/cell ve 32/world sınırları hipotezdir; BLD-S007/G4 kararı bekler.
- **OD-04 — ACCEPTED AS PROVISIONAL ARCHITECTURE:** Candidate full-generation, mutation freeze ve durable receipt tasarım yönü. Disk gecikmesi/perf ve fault injection doğrulanmadan runtime güvenilirliği **PASS** sayılmaz; G3'te ölçülüp gerekirse yeni ADR açılır.
- **OD-05 — ACCEPTED:** Mevcut S010 recipe/shelter korunur; sürümlü yeni veri bölümleri ve geriye uyumlu facade/adapter kullanılır. Node respawn başlangıçta kapalıdır; two-cell lease ve ileri streaming sonraki gate'lere ertelenir.
- **Canonical GDD kaynaklarını güncelleme:** **AUTHORIZED / NOT_YET_APPLIED**. `BLD_S001_PRODUCT_SCOPE_CHANGE.md` içindeki önce/sonra farkları, tarihî kaynakları silmeden ayrı kontrollü dokümantasyon değişikliği olarak uygulanacaktır. G0 onayı kaynak dosyaların halihazırda değiştirildiği anlamına gelmez.
- **G0 kararı:** **ACCEPTED — P00 DESIGN/DOCUMENTATION ONLY**. Runtime tests `N/A—NO_RUNTIME_CHANGE`; P01 harvesting/crafting/building `NOT_IMPLEMENTED`; save/load/perf ve G4 gameplay sonuçları `NOT_RUN`.
- **P01 yetkisi:** **ASSET RESEARCH AND PLANNING READY**; yeni bir P01 uygulama promptu ve owner test komutları hazırlanabilir. Mevcut G0 talebi P01 kod, sahne, terrain, prefab veya test yürütme izni değildir.

G0'da kalan açık parametreler: build zone ölçüleri/koordinatları, kaynak maliyetleri, node ID authoring, NavMesh doğrulama stratejisi, commit gecikmesi ve receipt retention. Bunlar ilgili sprintlerde owner-run kanıtlarla karara bağlanacaktır. `SOURCE_PRESERVATION_MANIFEST.md` veya baseline SHA'larını yeni git commit'ine uyarlayıp önceki kanıtı bozmayın. Eski baseline'a göre çalışan `MANUAL_VERIFY_COMMANDS.md all` komutu push sonrası HEAD/status değişimi nedeniyle beklenen şekilde drift bildirebilir; yeni source/owner değişiklikleri için ayrı kanıt ve güncel baseline kullanılmalıdır.
