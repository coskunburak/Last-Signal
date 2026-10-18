# G0 — P00 kabul ve owner karar receipt'i

**Durum: PENDING_OWNER.** Tarih: 2026-10-18, Europe/Istanbul. Branch `s012-integrated-graybox-slice`, HEAD `fe0f4c70f839b36db25f6ccefc6b3e4f8e9519dd`.

P00 belge/architecture üretimi yetkilendirilmiştir ve D001–D016 için somut adaylar yazılmıştır. IMPLEMENTED yalnız belge üretimi demektir. Owner static sonuçları, ürün/ekonomi kararı ve G0 imzası alınmadı. **G0 APPROVED değildir; P01 başlatma yetkisi yoktur.**

Kaynak: [özgün G0 kabulü](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/GATES/G0_ACCEPTANCE.md), [P00 phase](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/PHASE.md). Kaynak şablondaki runtime gate'leri, kullanıcının açık docs-only/manual-verification talimatına göre aşağıda gerekçeli N/A olarak ele alınır; şablonun kendisi değiştirilmedi.

## Kabul receipt'i

| Alan | Teslim / durum | Owner kanıtı |
|---|---|---|
| Kapsam farkı ve ADR | [ADR-001](BLD_S001_PRODUCT_SCOPE_CHANGE.md), tarihî GDD korunmuş; PENDING_OWNER | MS-03 + OD-01 |
| Oyuncu vaadi/alternatif survival | [US-01–12](PLAYER_PROMISE_AND_STORIES.md), [değer](SURVIVAL_VALUE_MAP.md), [demo](FIRST_PLAYABLE_SCENARIO.md) | MS-03 + OD-03 |
| MUST/OUT ve takvim | [SC-01–13/bütçe](SCOPE_MATRIX_AND_BUDGET.md), düşük güven aralıkları | MS-03 + OD-02 |
| Teknik tasarım/schema/dependency | [S002](BLD_S002_HANDOFF.md), DEC-009–016 | MS-03 + OD-04/05 |
| Günlük kartlar | D001–D016 belge IMPLEMENTED; [S001](BLD_S001_HANDOFF.md)/[S002](BLD_S002_HANDOFF.md) acceptance matrisi | MS-02/03 NOT_RUN_BY_OWNER |
| Branch/source preservation | [Entry](ENTRY_AUDIT.md), [manifest](SOURCE_PRESERVATION_MANIFEST.md), [son rapor](PRESERVATION_REPORT.md) | MS-01/02 NOT_RUN_BY_OWNER |
| Compilation/EditMode/PlayMode/build | N/A—NO_RUNTIME_CHANGE; sadece Markdown, test veya assembly/runtime değişmedi | P00 için Unity komutu yok |
| S010/S023 regression/gameplay/visual | N/A—NO_RUNTIME_CHANGE; legacy davranış/scene değişmedi; mevcut dirty işler ayrıca owner kapsamı | Geçmiş PASS taşınmadı |
| Save/load/cancel/duplicate/unloaded | P00 yürütmesi N/A—NO_RUNTIME_CHANGE; future QA-Q01–13/16–20 TEST_NOT_RUN | Runtime phase owner kanıtı henüz yok |
| Performance | NOT_MEASURED; P00 benchmark N/A—NO_RUNTIME_CHANGE | Gelecekte QA-Q14 |
| Yeni asset/GUID | Yeni harici asset 0; GUID değişimi 0; [asset policy](ASSET_SOURCE_AND_IMPORT_POLICY.md) | MS-01 |
| Migration | Strateji/fixture rehberi hazır; schema/fixture implementasyonu yok | FX-* future; source dosyalar korunacak |
| Sonraki phase | BLOCKED_GATE / PENDING_OWNER | G0 kabulü bile otomatik execution izni değildir |

## Açık owner kararları

| ID / durum | Somut karar adayı | Alternatif / trade-off | Son karar noktası |
|---|---|---|---|
| OD-01 PENDING_OWNER | ADR-001 kontrollü, claimed shelter yakınındaki authored zone istisnası; voxel/co-op/sınırsız inşa OUT | Yalnız legacy socket kapsamını sürdürmek yeni oyuncu vaadini daraltır | G0 |
| OD-02 PENDING_OWNER | SC-01–09 MUST, modüler barınak sonraki hedef; ek takvim ve COULD kesme sırası | Hangi MUST ertelenecekse ID ve downstream etkisi açıkça seçilir; veri güvenliği kesilmez | G0, yeniden G4/G5 |
| OD-03 PENDING_OWNER | İnşa isteğe bağlı; 15–30 dk demo hedefi, gerçek koordinatlar BLD-S007'de; ilk playable tek-cell/tek çit | Daha büyük ilk demo art/QA maliyetini yükseltir; 16/cell,32/world limitleri ölçülene kadar yalnız başlangıç hipotezi | G0 yaklaşım; sayılar BLD-S007/G4 |
| OD-04 PENDING_OWNER | Candidate full generation + mutation freeze + durable receipt; kısa bekleme UX ve disk maliyeti | WAL/journal daha karmaşık; latency uygunsuzsa ayrı ADR, güvenliği gevşetme yok | G0 yaklaşım; ölçüm G3 |
| OD-05 PENDING_OWNER | Legacy recipe adapter/facade, yeni versionlı sections, node respawn başlangıçta kapalı; future two-cell leases | Legacy rewrite yüksek risk; respawn/graph genişlemesi sonraki gate kararı | G0; BLD-S005/020/021 öncesi detay |

OPEN implementation parametreleri: gerçek BuildZone koordinat/ölçek/slope/clearance, catalog maliyet/ID/source revision değerleri, nav algoritması, receipt retention/latency ve perf eşikleri, resource authoring ID map, license entitlement ve gerçek legacy fixtures. Her birinin sahibi Burak + ilgili future domain/sprint; [QA risk matrisi](QA_AND_FAULT_MATRIX.md) kapanış gate'lerini verir. Bunlar onaylanmış oyun değerleri veya P00 içinde yapılmış runtime çalışma değildir.

## G0 üç ürün riski

- Sıfır materyalle upgrade: SC-02/04, US-06, QA-Q01/18 → ret ve delta 0. Belge karşılığı var; oyun sonucu yok.
- İnşa istemeyen oyuncu softlock: US-12 ve demo alternatif rota → su/gece/ana ilerleme erişilebilir. Belge karşılığı var; playtest yok.
- Voxel/tam yıkım/multiplayer sızması: ADR OUT ve SC-13 → sonraki değişiklik kararı olmadan uygulanamaz.

## Owner dolduracak — henüz imzalanmadı

- Owner / tarih / timezone: **PENDING**
- İncelenen branch/HEAD ve P00 docs hash listesi: **PENDING**
- MS-01 sonucu + log yolu: **NOT_RUN_BY_OWNER**
- MS-02 sonucu + log yolu: **NOT_RUN_BY_OWNER**
- MS-03 içerik inceleme notu / düzeltilmesi gereken paragraf: **PENDING**
- OD-01 / OD-02 / OD-03 / OD-04 / OD-05: **PENDING**
- Scope değişikliğini GDD'ye uygulama kararı: **PENDING** (kaynak GDD bu teslimde değiştirilmedi)
- G0 kararı: **PENDING_OWNER**; seçenekler ACCEPTED / PARTIAL / REWORK / REJECTED
- P01 için ayrı execution talimatı: **VERİLMEDİ**

Owner başarısız kontrol gönderirse yalnız o kanıt incelenir; root cause ve en küçük düzeltme, ardından exact focused retest verilir ve sonuç beklenir. Bu receipt hiçbir kutuyu kanıtsız PASS yapmaz.
