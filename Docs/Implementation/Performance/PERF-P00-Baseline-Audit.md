# PERF-P00 — Forest performans başlangıç denetimi

**Güncelleme:** Sahibi tarafından alınan native raporlar incelendi; fir LOD1/2 doğrulandı. Aşağıdaki ilk-aşama bilinmeyenleri için `NATIVE-AUDIT-REVIEW-20261023.md` en güncel kayıttır.

Tarih: 23 Ekim 2026. Aşama: statik denetim + sahibinin başlatacağı tanılama araçları.
Durum: **KRİTİK SORUNUN GİDERİLDİĞİ DOĞRULANMADI**. CPU/GPU ölçümü `NOT_MEASURED`; bu aşamanın Unity derleme, EditMode, PlayMode ve görsel doğrulaması `NOT_RUN`.

## Koruma ve kapsam

- Branch `s012-integrated-graybox-slice`, HEAD `3fa3161f0d3d22f40d422fd5b404605f25088ce0`.
- Başlangıç `git status --short`: 553 değiştirilmiş, 14 önceden silinmiş izlenen girdi, 100 izlenmeyen dosya/dizin girdisi. Bunlar önceki kullanıcı çalışmalarıdır; silinenler geri getirilmedi, diğer değişiklikler geri alınmadı. Tam `-uall` kayıt checkpoint içindedir.
- Önceden bulunan checkpoint: `/Users/burakcoskun/LastSignal-Recovery/HDRPProduction-20261021-181319`; `VERIFIED.json` okundu. 108.402 dosya doğrulandığı önceki kaydın iddiasıdır; bu aşamada tüm yedek yeniden hashlenmedi. MaskAssignment, WeaponNormals ve UnusedPackages checkpoint'leri de mevcut, değiştirilmedi.
- Yeni küçük checkpoint: `Docs/Implementation/Performance/Checkpoints/20261023T073625072827Z/`. Değiştirilecek mevcut `ForestPrototypeCapture.cs` birebir kopyası, Git durumu ve ilgili 268 dosyanın SHA-256 manifesti. Yeni tam proje kopyası yok.
- Sahne/prefab/TerrainData/materyal/shader/paket/kalite ayarı değiştirilmedi. İlkTown, S004, ağaç kesme veya diğer oyun sistemlerine kod değişikliği yapılmadı.

## Doğru sahne ve referanslar

Hedef: `Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity`.
GUID: `d84cde2c502e341cd955c86fcceab00f`.
Authoring sabiti, sahne `.meta`, sahnedeki `ForestPrototypeState`, prefab GUID'leri ve üretim klasörü tutarlı.
Üretim: `Assets/LastSignal/Art/ForestPrototype120/Generations/G20261021101239485`.
Placement digest: `88F780133F02F4DF4F076DC56221B3A4968995DA00DB244459AC9FAAC9B3C2F3`.

`Library/LastSceneManagerSetup.txt` bu incelemede boş sahne listesi içeriyor. Bu bir canlı Editor sorgusu değildir; kullanıcının şu anda hangi sahneyi görüntülediği bilinmiyor. Repository hedefi doğrulanmıştır. `EditorBuildSettings.asset` yalnız S001Acceptance ve CombatAcceptance içerir; mevcut build'in Forest açtığı varsayılamaz. Build profilinde ayrıca override olabilir; sahibinin kontrolü gerekir.

## Statik envanter

Kaynak: `Evidence/static-baseline-20261023.json`. Python aracı yalnız bu üretimin prefablarını açar, prefab removal override'larını düşer ve stripped referansları ikinci kez saymaz. Binary mesh/Terrain verisini açmaz. Sahnedeki aktiflik override'ları/karmaşık prefab yapısı değişirse sessizce tahmin etmek yerine durur.

| Öğe | Kaydedilmiş sahnede bulunan |
|---|---:|
| GameObject / activeSelf | 10.353 / 10.353 |
| MeshRenderer / MeshFilter | 5.792 / 5.792 |
| LODGroup | 2.300 |
| Prefab örneği | 2.300 |
| Ana canopy ağacı | 290 |
| Uzak dekoratif ağaç | 150 |
| Fir hero | 1 |
| Fern / shrub / rock / branch | 1.500 / 220 / 85 / 54 |
| Terrain | 1 |
| Terrain detail adedi | 9.000 **authoring sayacı**; native TerrainData ile doğrulanmalı |
| Terrain treeInstanceCount | Native audit bekleniyor; canopy GameObject prefablarıdır |
| Renderer materyal referansı (benzersiz) | 15; terrain/detail materyalleri bu toplamın dışında |
| Mesh GUID+fileID referansı (benzersiz) | 82 |
| Gölge açık MeshRenderer slotu | 4.072; bütün LOD seviyeleri dahil |
| Collider toplamı | 382 |
| Capsule / Box / Terrain / CharacterController | 291 / 89 / 1 / 1 |
| MeshCollider / Rigidbody | 0 / 0 |
| SkinnedMeshRenderer / ParticleSystem / ReflectionProbe | 0 / 0 / 0 |
| Camera / etkin Light | 1 / 1 (directional) |
| MonoBehaviour | 12; bunların biri Volume, biri HDAdditionalLightData, biri HDAdditionalCameraData |
| DecalProjector | 0; sahnedeki script sınıfları incelendi |
| Tam aynı prefab + parent + local position adayı | 0 |

**5.792 renderer, aynı karede çizilen renderer sayısı değildir.** LOD seçimi, frustum ve shadow pass'leri ayrıca ölçülmelidir. Aynı şekilde gölge slotları gerçek shadow draw sayısı değildir. Duplicate testi yalnız aynı prefab/ebeveyn/konum tekrarını bulur; yakın çakışmaları veya farklı prefablarla tekrarları dışlamaz.

| Ağaç ailesi | Ana orman | Uzak dekor |
|---|---:|---:|
| Spruce 1 | 50 | 31 |
| Spruce 4 | 67 | 34 |
| Spruce 6 | 53 | 29 |
| Spruce 8 | 54 | 26 |
| Spruce 9 | 48 | 30 |
| Birch 2 | 18 | 0 |
| Fir hero | 1 | 0 |

Aktif mesh kaynakları: European Forests `Source/Models/Spruce/Spruce_{1,4,6,8,9}.fbx`, `Birch/Birch_2.fbx`; fir ve fern `.blend`; doğrulanmış FantasyEnvironments `Environments/FBX/Bush1.fbx` ve `Rock1.fbx`; bu üretimin BranchMesh ve DistantGround meshleri. Tüm ThirdParty içerikleri yüklenmedi.

Authoring `Generate` mevcut state sayısını kontrol ediyor, staging sahnesinde üretip yalnız mevcut `Generated` alt ağacını değiştiriyor; her çalıştırmada eski ağacın yanına yenisini eklemiyor. 150 uzak ağacın LOD0–2 renderer/mesh bileşenleri ve collider'ları scene override ile kaldırılmış. Dört LOD'lu kaynak prefabların ham sayılarını 150 ile çarpmak sahne maliyetini yanlış büyütürdü.

## Fir yüksek poligon kontrolü ve LOD

Fir kaynağı GUID `16cb278d4a5214f379bd7a4402f5d8af`. Kaydedilmiş sahne yalnız **bir** `FirHero_LimitedLOD1LOD2.prefab` örneğine bağlı; içinde iki mesh referansı var:

- Hero_LOD0: local fileID `6774275268720665551`.
- Hero_LOD1: local fileID `1092776003849301120`.

Authoring bunları isimle `fir_tree_01_a_LOD1` ve `fir_tree_01_a_LOD2` üzerinden seçiyor ve mesh başına 180.000 triangle sınırı koyuyor. Kaynak grid veya ek fir prefab örneği yok. **Binary importer'ın bugünkü mesh adı/topolojisi native olarak yeniden okunmadığı için 4,18 milyon triangle mesh'in kesinlikle kullanılmadığına dair son onay bekliyor.** Yeni native raporda bu iki fileID'nin ad/triangle/vertex/submesh değerleri kontrol edilecek; milyon üçgenli bağlı mesh ayrıca uyarı üretir. Kaynak silinmedi veya yeniden import edilmedi.

Önceki Forest HANDOFF'taki sayılar güncel ölçüm yerine geçirilmemiştir:

| Aile | Önceki kayıt (triangle) | Şimdi doğrulanan yapı | Sonraki veri |
|---|---|---|---|
| Spruce 1 | 15.096 / 7.548 / 3.773 / 1.886 | Ana ormanda 4 LOD, 3 renderer/LOD | Native vertex/triangle/submesh tablosu |
| Spruce 4 / 8 | LOD0 16.085 / 17.138 | 4 LOD | Tüm seviyeler |
| Spruce 6 / 9, Birch 2 | Güncel topoloji yok | 4 LOD | Tüm seviyeler |
| Fir | Kaynak LOD0 4.176.819; seçilen LOD1/2 177.444 / 70.661 | 2 LOD, 4 materyal/renderer, capsule | İki fileID'nin native mesh kimliği |
| Fern a/b/c/d | 784 / 2.384 / 2.248 / 816 | 374 / 404 / 353 / 369 örnek; tek geometri seviyesi | Native sayım + overdraw |

Canopy threshold'ları `.52/.27/.12/.012`, fir `.65/.018`, fern `.018`; hepsi ekran yüksekliği oranı, metre değil. Uzak ağaçlar yalnız son LOD ve `.009` cull kullanıyor. Fern shader'ı opaque alpha-clip + double-sided; fern gölgeleri kapalı. Fernlerde ucuz ara mesh yok, yalnız cull geçişi var. CrossFade/animated açık; High `supportDitheringCrossFade: 0` ile uyum görsel/Frame Debugger doğrulaması gerektiriyor. Bu uyumsuzluk bir FPS kök nedeni ilan edilmedi.

## HDRP / platform konfigürasyonu

| Başlık | Kaydedilmiş değer / sınır |
|---|---|
| Unity / HDRP | ProjectVersion 6000.5.0f1; manifest ve lock HDRP 17.5.0; yerel paket kaynağı mevcut |
| Renk uzayı | Linear (`m_ActiveColorSpace: 1`) |
| Default Graphics pipeline | High, GUID `15722f09b35214c7cac8dee7a2a4c2e3` |
| Quality PC / Standalone default | index 1 → High; hem macOS hem Windows için aynı Standalone varsayılanı |
| Quality Mobile | Medium GUID `6c097e46d54844257ad24baa94055d84`; Standalone için excluded |
| Development / Benchmark | Assetler var, Quality listesine atanmış değiller; otomatik seçilmezler |
| Render path | High Both; güncel global Camera Frame Settings LitShaderMode biti 1 → Deferred; kamera customRenderingSettings=0. Canlı debugger overrides bilinmiyor |
| SRP Batcher / GPU Resident Drawer | Asset SRP Batcher açık; GRD mode 0 (kapalı) |
| Dynamic resolution | Asset enabled=0; kamera allowDynamicResolution=0; native ekran çözünürlüğü ölçülmedi |
| AA / LOD | Kamera TAA; quality lodBias=1, maximumLODLevel=0 |
| VSync / target cap | PC VSync=0; runtime Application.targetFrameRate bilinmiyor, kayıt alacak |
| Desktop Graphics API | ProjectSettings'te explicit desktop API listesi yok. Metal/DX11/DX12'nin gerçek seçimi SystemInfo kaydıyla doğrulanmalı |
| Frame Timing Stats | ProjectSettings enableFrameTimingStats=0. Bu, her Development Player'da timing yok demek değildir; API runtime kullanılabilirliği kontrol edilir |
| Shadows | High directional max 4096; sahne güneşi override 2048; Morning/Overcast shadow distance 80 m |
| Fog / AO / contact | Volumetric fog açık; Morning meanFreePath 240 m, depthExtent 96 m; SSAO .55; contact shadow enable=true, 20 m |
| SSR / clouds | High destekliyor; scene profillerinde özel SSR/VolumetricClouds override yok. Destek bayrağı çalışma maliyeti kanıtı değildir; default volume/effective stack incelenmeli |
| SSGI / ray tracing | High supportSSGI=0, supportRayTracing=0; global frame bitinin açık olması SSGI çalışıyor demek değildir |
| Texture streaming | Üretilmiş texture importlarında streaming mipmaps açık; PC quality streamingMipmapsActive=0. Bellek baskısı hipotezi; henüz değişiklik yok |
| Varsayılan Player boyutu | 1024×768; karşılaştırma için ayrıca gerçek 1920×1080 ayarlanmalı |

Render materyalleri üç shader kimliği kullanıyor: HDRP/Lit, vendor Bark_HDRP ve Leaves_HDRP. 15'inde instancing bayrağı açık; gerçek draw birleştirme/SRP uyumluluğu Frame Debugger gerektirir. Terrain/detail yolları ayrı değerlendirilir. Derlenen veya çalışan shader variant sayısı statik dosyadan belirlenmedi; native araç yalnız materyal keyword imzalarını raporlar.

macOS read-only sistem sorgusu: Mac15,7 MacBook Pro, Apple M3 Pro, 12 CPU çekirdeği (6+6), 18 GPU çekirdeği, 18 GB unified RAM; Metal destekli. Unity'nin canlı API'si, güç/termal durum ölçülmedi. Windows CPU, gerçek GPU seçimi, VRAM, driver/API, güç/termal durum **UNKNOWN**. Laptop RTX 3060 adına bakarak VRAM tahmin edilmedi.

## CPU / Editor incelemesi

Sahne-local scatter/mesh üretimi Editor authoring'de; ağaç başına Update yok. Explorer yalnız input/shot değişimleri ve F9'u yönetiyor; Lighting.Apply yalnız preset değişiminde çalışıyor. Motor/stance nonalloc physics sorguları kullanıyor; scene-wide arama veya materyal instance üretimi bu sahnenin her-kare yolunda görülmedi. Kaydedicide her-kare `new double[]` vardı; yalnız F9 sırasında etkin olduğundan sürekli düşük FPS'nin kanıtlanmış açıklaması değildir.

Scene view çift render, gizmos, Editor callbacks, shader compilation/import, overdraw, shadow maliyeti ve camera rotation spike'ları ölçülmemiştir. Native araç Editor'ün yalnız bu açık sahnesini inceler, sahne açmaz/kaydetmez; ExecuteAlways veya OnValidate tetikleyen authoring çağrısı yapmaz. Editor yüklenmesi/import'un kendi maliyeti ayrıca gözlemlenmeli.

## Sonraki karar için gerekli kanıt

`PERF-P05-Regression-And-Acceptance.md` içindeki ilk paket yeterli: native audit JSON, her makinede F3 sabit A/B/C üçer tekrar, bir baseline kamera dönüşü CPU Timeline capture ve Rendering/Memory ekranı; GPU desteklenirse GPU capture, desteklenmezse açık `UNAVAILABLE`. Ölçümler gelene kadar yoğunluk/LOD/HDRP/texture/physics optimizasyonu uygulanmayacak.

API davranışı için: [Unity FrameTimingManager](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTimingManager.html), [FrameTiming alanları](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTiming.html). GRD değerlendirmesi yerel HDRP 17.5 `Documentation~/gpu-resident-drawer.md` ve `make-object-compatible-gpu-rendering.md` ile kontrol edildi; [Unity GRD dokümanı](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/gpu-resident-drawer.html) genel referanstır. GRD açmak bu aşamanın değişikliği değildir.
