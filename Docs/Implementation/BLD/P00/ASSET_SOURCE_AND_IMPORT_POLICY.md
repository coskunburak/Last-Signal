# D014 — Asset kaynak, import ve kullanım kararı

BLD-P00-DEC-014 · PROPOSED teknik politika; P00 asset gereksinimi kullanıcı kapsamına göre **0**. Belge IMPLEMENTED / TEST_NOT_RUN.
Girdi D014, asset governance ve mevcut [S013 registry](../../S013/S013_ASSET_REGISTRY.md), [S023 audit](../../S023/S023_ASSET_AUDIT.md), [FirstTownCoreForest kaynak kaydı](../../FirstTownCoreForest/DESIGN_AND_ASSETS.md). [Kart izi](BLD_S002_HANDOFF.md). Bu referanslar yeni envanter/lisans doğrulaması değildir; ThirdParty ağacı context'e yüklenmedi.

## Kaynak kullanımı

Mevcut orman belgesi vendor FantasyEnvironments ailesi ile project-owned S013 Production Prefabs bağlantısını kayıtlıyor. S023 belgesi Wall_2m/Window_2m, mevcut materyaller ve Door prefab kullanımını gösteriyor; entitlement/public redistribution konusunda kapanmamış notlar taşıyor. Bunlar sonraki seçim için aday kaynaklardır; hasat ağacı, çit veya preview'a hazır oldukları iddia edilmez. Kullanıcının mevcut `Docs/MeshyAI LAST SIGNAL/` işi bu görevde okunmadı/değiştirilmedi, P00 dependency'si değildir.

Yeni indirme, marketplace satın alımı, asset import, vendor dosya taşıma veya Unity native inspection yapılmadı. P00 metadata veya runtime asset ihtiyacı doğurmaz. Next phase seçim sahibi Burak; teknik/art hazırlık BLD-S003/004/008/011–013, production polish BLD-S023/024.

## Sonraki asset selection gate

| Kontrol | Gerekli kanıt / ret koşulu |
|---|---|
| Provenance | Kesin ürün/kaynak URL'si, yazar, sürüm, edinim tarihi, orijinal archive SHA256, dosya/GUID mapping; belirsiz kaynağı “free” diye kabul etme |
| Lisans | Ticari oyun kullanım hakkı, seat/redistribution sınırı, attribution metni/yeri, receipt/license copy; ücretsiz fiyat lisans değildir |
| Unity/URP | Projenin hedef Unity 6000.5.0f1 / URP ortamında owner native incelemesi; shader/pipeline materyal dönüşümü ve editor version kanıtı; burada uyumluluk doğrulanmadı |
| Pivot/ölçü | Metre birimi, +Y up, +Z forward, unit scale; project-owned wrapper ile pivot normalize; vendor geometry/meta korunur |
| Çit snap standardı | Öneri 2 m nominal segment; pivot alt-orta, uç anchor x=±1 m, y=0; rotation/endpoint tolerance versionlı kit manifest'inde. Bu tasarım standardıdır, mevcut model ölçüsü değildir. Gerçek 2 m kit seçilmezse manifest/definition birlikte revize edilir. |
| Collider/joint | Basit clearance/collision volumes; render mesh ölçüsüne güvenme; gate için hinge ve tam sweep, snap orientation/occupancy; spawn/nav yolları açık |
| LOD/render | LOD geçişleri, renderer/material slot sayıları, bounds/culling, triangles ve texture memory; ölçüm raporu olmadan “ucuz” kabul yok |
| Texel/PBR | Aynı kitte tutarlı texel density, roughness/normal/albedo scale; gerçek gün/dusk/gece/yağmur FPS görüntüsü; rastgele texture varyantı gameplay identity yaratmaz |
| Ghost | Render-only ayrı projection; aynı silhouette/footprint; transparent/tint materyal politikası, renk+ikon+metin. Yeni shader gerekip gerekmediği seçimde kanıtlanır; P00 shader üretmez. |
| Performans | Hedef cihaz ve aynı kamera/ışık/instance sayısında GPU/CPU/draw calls/GC/VRAM baseline ve candidate; art değişimi nav/perf bedelini gizleyemez |
| Koruma / geri alma | Orijinal vendor dosyası immutable, proje-owned variant, dependency GUID listesi ve source hash; staged scene, promote ayrı owner kararı |

## Risk matrisi

| ID | Risk / seviye | Tetik | Azaltım / owner / kapanış |
|---|---|---|---|
| A01 | Belirsiz lisans / yüksek | Exact URL, receipt veya ticari izin yok | Owner kaynak kaydı ister; ilgili asset release kullanımı BLOCKED; P00 engellenmez |
| A02 | URP/material uyumsuzluğu / orta | Native incelemede shader hata/pink veya PBR tutarsız | Art project-owned uyarlama; owner görsel receipt |
| A03 | Yanlış pivot/collider/snap / yüksek | Gerçek footprint preview ile farklı | Authoring ölçümü ve QA-Q04/Q11; model varyantı değiştir veya ret |
| A04 | Çok renderer/texture/LOD maliyeti / yüksek | Ölçüm bütçeyi aşar | Tek kit/az varyant, ölçümlü optimizasyon; QA-Q14 |
| A05 | Vendor/VisualSlice overwrite / yüksek | Source hash drift veya yeni GUID | İşlem durur; source-preservation reconciliation, ayrı staged variant |
| A06 | Hasat state'ini dekoratif ağaç indeksine bağlama / yüksek | Scatter regenerate ID'leri değiştirir | Stable authoring map ve migration; Gathering/Persistence owner |

Seçim: mevcut kaynakları önce değerlendirme; alternatif yeni paket arama P00'da gereksiz ve reddedildi. Geri dönüş seçilmemiş adayları import etmeme, sonraki phase'de yalnız owned variant geri alma. Kabul: owner kaynak/lisans risklerinin açık olduğunu ve yeni paket sayısının 0 olduğunu statik incelemeyle doğrular. Lisans veya runtime görsel PASS yoktur.
