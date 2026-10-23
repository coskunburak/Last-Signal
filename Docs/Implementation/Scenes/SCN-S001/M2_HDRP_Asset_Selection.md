# SCN-S001 M2 — ön seçim, üretim kabulü verilmedi

**UPDATED: PROTOTYPE ASSET INTEGRATION IMPLEMENTED, LICENSE/OWNER ACCEPTANCE PENDING.** M0/M1 sırasında gerekli adaylar ve mevcut kanıtlar okundu. Tam M2/visual/license kabulü yapılmadı. Yeni paket yüklenmedi, vendor taşınmadı, global conversion uygulanmadı. Aşağıdaki adayların hiçbiri bu sprintte accepted shipping içerik ilan edilmedi; M1 sonrasında kullanıcının düzeltmesiyle ForestPrototype120 G20261021101239485 türevleri adaya taşındı.

AST-S001 Catalog/MovePlan/Migration-Log/Manual-Acceptance kullanıldı. 72 kayıt sınıfları toplanmadı; envanter veya vendor hash taraması tekrarlanmadı. Catalog'daki proposed destination değil gerçek original_path kullanıldı. `Assets/Realistic Tree` ThirdParty catalog kapsamının dışında, ayrıca incelenmesi gereken vendor kaynağıdır.

| Aday / gerçek kaynak | Karar | Kanıt ve kullanım şartı |
|---|---|---|
| `Assets/Realistic Tree/Prefabs/HDRP/Spruce/Spruce {1,4,6,8,9}.prefab`, `Birch/Birch 2.prefab` | LICENSE_PENDING | ForestPrototypeAssets bu yolları kullanıyor. European Forests adıyla beklenen ailenin bu importla ticari/provenance eşlemesi henüz belgelenmedi. HDRP Bark/Leaves shader aileleri ve generated LOD'lar var; satın alma/indirme lisans kaydı gerekli. |
| `Assets/ThirdParty/fir_tree_01_4k.blend (1)` (AST-067) | LICENSE_PENDING | Source pack ~956.6 MB; texture 18×4K. Native eski raporda 4,176,819 triangle LOD0 kullanılmıyor; generated hero LOD1 177,444 / LOD2 70,661, 4 submesh. Yoğun canopy için uygun değil; sınırlı hero bile ölçüm bekliyor. |
| `Assets/ThirdParty/fern_02_4k.blend` (AST-066) | LICENSE_PENDING | Generated a/d 784/816 triangle, b/c 2384/2248. 4K diffuse, normal/roughness/opacity kaynakları authoring pipeline'da kullanılıyor; 1500 örneği kopyalamak yerine kümeli düşük bütçeli M4 gerekir. |
| `Assets/ThirdParty/forrest_ground_03_4k.blend/textures/forrest_ground_03_` (AST-068) | LICENSE_PENDING | diff/rough/nor_gl/disp kaynakları mevcut; path önceki AST sprintinde düzeltilmiş. Normal importu, roughness ters çevirme, linear mask ve terrain B-height semantiği M3'te doğrulanmalı. Lit detail-mask ile terrain height karıştırılmamalı. |
| `Assets/ThirdParty/Environments/Silver_Cats/Hand_Painted_Nature_Kit_LITE` (AST-022), generated Rock/Bush/Grass | LICENSE_PENDING | Görsel yön hand-painted; realistic canopy yerine kabul edilmedi. Native generated Rock 658, Bush 54 triangle. Readme lisans kanıtı yerine geçmiyor. Uygun gerçekçi alternatif bulunana kadar kaynak/işlev korunur. |
| `Assets/ThirdParty/Modular European House` (AST-033), `Assets/ThirdParty/Abandoned buildings` (AST-001) | LICENSE_PENDING | Mevcut kasaba bağımlılıkları. İlkinde catalog 20 HDRP + 1 UNKNOWN material; ikincide 9 HDRP referansı. Bu oran model, kapı/collision veya ticari onay değildir. Yapı replacement yapılmadı. |
| `Assets/LastSignal/Data/FirstTown/Road_*.asset` ve VisualSlice generated yol meshleri | EXPERIMENTAL | Proje meshleri ve bağlantılar korunur; asfalt materyali yalnız ayrıldı. Terrain hizası ve yol/toprak geçişi owner görünümü olmadan kabul edilemez. |
| `Assets/ThirdParty/RainMaker` (AST-034), `RainSnowCloudEffect` (AST-035) | LICENSE_PENDING | Yeni weather manager kurulmayacak. Mevcut TownAtmosphere/WorldClock/TownRain korunur. Pipeline migration/UNKNOWN riskleri nedeniyle bu aşamada yeni entegrasyon yok. |

LOD kanıt kaynağı `Docs/Implementation/Performance/NATIVE-AUDIT-REVIEW-20261023.md` (önceden owner tarafından çalıştırılan native rapor). Sayılar mesh üçgenleridir; frame'de çizilen toplam veya GPU süresi değildir. Spruce generated LOD0 15,096–18,581; LOD3 1,886–2,321. Birch2 13,526 / 6,763 / 3,381 / 1,689. Fern/rock tek seviyeli cull grupları kusursuz çok seviyeli LOD kabul edilmez.

Tüm adaylar için henüz açık alanlar: canlı visual realism/texture ölçeği; kaynak ve generated material slot eşleşmesi; uygun alpha clip/double-sided/normals; gerçek runtime texture/mesh bellek maliyeti; gölge ve overdraw maliyeti; collider clearance. Decorative LOD prefabını harvestable köke doğrudan takmak güvenli değil: notch meshleri, fizik profil boyutları, collision proxy ve resource definition birlikte eşleşmeli. Bunların tamamı **NOT_RUN / OWNER_ACCEPTANCE_PENDING**.

Mevcut performans kanıtı: Mac baseline 1649×860, M3 Pro/Metal/Editor, High, 30 s, 164 frame; ortalama 5.466 FPS, P50 182.257 ms, P95 367.610 ms. Bu yeni adayın ölçümü değildir. Vegetation-off B farklı kamera açısına sahip; doğrudan hızlanma yüzdesi çıkarılamaz. Tree-shadow owner gözlemi önemli ipucu, ancak kontrollü C ölçümünün yerine geçmez. Yeni Main için median/P95/P99/GPU/draw/memory ve Windows ölçümleri **NOT_RUN**.

Öncelik: lisans kanıtı → sınırlı Spruce/Birch/fern/ground değerlendirmesi → isolated HDRP materyalleri → tek Pine harvestable visual eşlemesi → kontrollü shadow/LOD ve owner görüntüleri. 60 FPS veya ticari kullanılabilirlik taahhüdü yok.


## Uygulanan seçim
Spruce1/4/6/8/9 ve Birch2, fern_02_a/d, Woodland/grass/soil/rock katmanları, GrassDetail, Rock ve FallenBranch doğrudan aynı prototype generation'dan yeni GUID'lerle kopyalandı. Poly Haven yüksek poligonlu Fir eklenmedi. Kaynak listesi PrototypeVisualAssets.json; değişmeyen 171 kaynak dosyanın hashleri PrototypeSourcePreservation.json.

Bu seçim prototype ile eşleşmeyi sağlar; prototype içindeki Rock/GrassDetail ve bazı diffuse-only zeminler hâlâ stilize kökenlidir. Bunları gerçekçi photogrammetry veya tümüyle kabul edilmiş PBR diye sınıflandırmıyoruz. İlk sürümde korunmuş binalar ve büyük sınır çiti de eski sanat kalitesindedir. İleri gerçekçilik/üretim kabulü bu sınırlamalar giderilmeden verilemez.
