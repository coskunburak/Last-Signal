# S004 — MCP ile doğrulanan durum · 19 Ekim 2026

Bu rapor, aynı klasördeki önceki `TEST_NOT_RUN` durumlarının güncel karşılığıdır. Kullanıcının sonraki açık talimatıyla Unity testleri, staging hazırlığı ve görüntü kayıtları MCP üzerinden yürütüldü. Üretim sahnesi yayınlanmadı; commit/push/build yapılmadı. Görsel ve ses beğenisi kullanıcı adına onaylanmadı.

## A. Kök neden

Eski S003 akışında sağlık sıfıra indiğinde `ResourceNodeView.Present` ayakta duran görseli kapatıyordu. S004 fizik gövdesi bağlanmamış sahneler bu eski kaynak tüketim akışına girebiliyordu. İlk başarısız `FirstTownTreePhysics_20261019102748-99bfd179` taslağında fizik gövdesi bulunmadığı doğrulandı. Güncel `GatheringWorld.TryHit`, fizik referansı eksikse bu yolu `PhysicsNotReady` ile reddeder; kaynak tüketimi artık fiziksel ağacı kaldırmaz.

Çamla yapılan gerçek test ayrıca ikinci bir sorun buldu: geniş, tam boy kütük kutusu, görünürde kesilmiş boşluğu fiziksel olarak doldurup kırılmış gövdeyi dik tutuyordu. Alt kütük korunurken üst temas bölgesi artık kesik yönüne ve kalan desteğe göre daralıyor. Yeni test dik duran gövdenin yalnızca uyumasını başarı saymaz; en az 60° devrilmeyi ister.

## B. Doğrulanan davranış

Gerçek melee denetleyicisinin normal Update zamanlaması kullanıldı. Huşta ilk üç çentiğin ilerlemesi, gerçek joint kırılması, görünür gövde/kütük, yerçekimiyle devrilme, çevreyle temas, doğal oturma ve tek yield kimliği kaydedildi. Kalın çamın son doğal koşusunda 8 kabul edilmiş darbe gerekti; önceki huş koşusunda 4. Bu bir sabit dört-vuruş üretim kuralı değildir.

## C. Fizik

Tam kısıtlı FixedJoint, yerçekimi, ölçekten türetilen kütle/COM, compound capsule gövde, speculative CCD ve sınırlandırılmış balta darbesi kullanılır. Kuvvet/tork sınırları kalan kesitle azalır. `OnJointBreak` yalnızca mevcut Gathering yetkisine kalıcı kırılma isteği gönderir. Düşüşte Transform animasyonu yoktur. Kütük alt tabanı ve daralan üst destek iki sabit kutuyla yaklaşık temsil edilir; tekrarlanan projeksiyon yeni çarpıştırıcı biriktirmez. Eğim, engel, durma, görünürlük ve başarısız fizik kurtarma testleri çalıştırıldı.

## D. Geometri ve sahneler

OpenFracture ile editörde hazırlanmış 16 derinlik × 8 yön, kapalı üst/alt gövde çiftleri kullanılır. Kabuk ve yaprak kimliği korunur; iç yüzey mevcut URP Wood malzemesidir. Vuruş sırasında tüm ağacın mesh'i dilimlenmez.

- Huş: `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019103726-320e36cd.unity`
- Çam: `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019163434-8e0f05b7.unity`
- Kaynak: `Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity`

Kaynak sahnenin SHA256 değeri korunmuştur: `a80fd7fb8021c8b115a424edab8cbdd3ca9575a683b23338b60f3c0f69b77154`. Çam türevleri yaklaşık 19 MB, mevcut huş türevleri yaklaşık 18 MB. İki sahnenin düşüş çevresi incelendi; clearance yalnızca bu doğrulama sahnelerinde etkinleştirildi. Bu inceleme tüm üretim ormanının navigasyon kabulü değildir.

## E. Ses

Dokuz mevcut kayıttan türetilmiş olay kesiti `Assets/LastSignal/Audio/S004/ValidationPreview/` altında: WoodHit, BarkFracture, StructuralCrack, Creak, JointFailure, FallingMotion, GroundImpact, BranchContact, Settling. Kaynaklar, lisans sayfaları, zaman aralıkları ve hash'ler `AUDIO-SOURCES.md` ve `AUDIO-PREVIEW.json` içindedir.

S004 sesleri mevcut 16 seslik ProductionAudio havuzuna bağlandı. Kabul edilmiş vuruşların ses havuzuna gönderimi ve kırılma/temas/oturma olayları gerçek sahnede test edildi. Kütüğün kendi teması iniş sayılmaz; süren dış temaslar da tek impact receipt denetiminden geçer. GameplayNoise sesi duyma sisteminden bağımsız kalır. `mappingApproved=false`; önizleme yalnızca editördeki isimlendirilmiş doğrulama sahnelerine izin verir. İnsan dinleme/miks kabulü henüz yoktur.

## F. Kayıt ve hücreler

Kısmi kesik, düşüş ortasında hız/poz, oturmuş gövde, tek yield kimliği ve tekrar yükleme test edildi. Gerçek WorldCellManager Request → Ready → Enter → Unload → Request akışına, geçici test cell prefab'ına fiziksel ağaç eklenerek gidildi. Kısmi kesik ve kırılmış ağaç yeniden oluşturulurken kaynak kimliği korundu. Bu test sahnedeki objeyi kapatıp açmakla sınırlı değildir; fakat tüm ormanın streaming sistemine dönüştürüldüğü anlamına da gelmez.

Unity'nin null gathering alanını varsayılan boş nesne olarak serileştirmesi, eski kayıtların decode doğrulamasını bozuyordu. Yalnızca işaretsiz ve tam olarak boş eski temsil normalize edilir; ilerleme içeren işaretsiz durum reddedilir. 33 hata veren regresyon grubu düzeltmeden sonra 181/181 geçti.

## G. Dosyalar

`MCP-FILES.json` ilgili dosyaların yol/hash/boyut envanteridir. Devam çalışmasının ana değişiklikleri:

- GatheringState: eski boş kayıt normalizasyonu ve olumsuz durum testleri.
- TreePhysicsBody: doğrulama ses bankası, kütüğün daralan temas desteği, süreğen temas deduplikasyonu.
- TreeAudioBank: yalnızca editör doğrulama sahnelerine sınırlı ses önizlemesi.
- TreePhysicsAuthoring: seçili kaynak için açık otomasyon girişi, yeni sahnelere ses kesitlerini bağlama.
- TreeVerificationRunner: gerçek Unity Test Runner sonuçlarını domain reload boyunca dosyalama.
- P01 testleri: zemin üzerinde nişan, gerçek zamanlı saldırı/görüntü/Profiler kaydı, gerçek hücre boşaltma-yükleme, eğim ve kütük desteği regresyonu.
- WoodenAxe karakter profili: dar FOV'da elin kadrajdan taşmasını gidermek için hazır tutuşu 2,5 cm yükseltme.
- MeleeGrip/CharacterIntegration testleri: mevcut iki elli tabanca profili ve karaktere özel görsel türevleriyle uyum; poz doğrulamasını gerçek LateUpdate sonrası örnekleme.
- `Tools/p01-tree-verify.py`: gerçek CharacterIntegration fixture adı, yeni sahne testleri ve evidence dizini aktarımı.
- Çam staging sahnesi/mesh/profil/prefab türevleri ve dokuz ses kesiti. Vendor kaynaklarına ve üretim sahnelerine dokunulmadı.

## H. Kanıt ve tekrar çalıştırma

`MCP-CASE-INDEX.json`, her testin son sonucunu ve kanıt klasörünü listeler. `MCP-TEST-MATRIX.md`, koşuları ve düzeltme sonrası sonuçlarını ayırır. Önceki başarısız koşular silinmedi. MCP koşularında `request.json`, `tests.xml`, `tests.log`, `result.json` bulunur. S004 günlük satırları `unity-s004-excerpt.log` dosyasındadır; tam ortak günlük `Logs/Editor.log` konumundadır.

Unity editörü kapalıyken terminal alternatifi:

```bash
cd '/Users/burakcoskun/Last Signal' && \
S004_STAGE='Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019103726-320e36cd.unity' && \
python3 Tools/p01-tree-verify.py edit && \
python3 Tools/p01-tree-verify.py play && \
python3 Tools/p01-tree-verify.py physics --stage "$S004_STAGE" && \
python3 Tools/p01-tree-verify.py persistence && \
python3 Tools/p01-tree-verify.py persistence-stage --stage "$S004_STAGE" && \
python3 Tools/p01-tree-verify.py regression-edit && \
python3 Tools/p01-tree-verify.py regression-play
```

Beklenti: sıfır başarısız/atlanan test; her kapı ayrı OWNER kanıt dizini üretir. Batch koşusu canlı kamera görüntüsü üretmez; görsel kanıt MCP koşularındadır. Yeni sahne hazırlama: kaynak FirstTownHarvest açıkken **Last Signal → BLD → P01 → S004 → Create Single Tree Physics Stage**. Geri dönüş: kaynak sahneyi aç; test türevlerini inceleme için ayrı tut. Kaynak veya üretim sahnesini türevle değiştirme.

## I. Görsel ve performans incelemesi

`REVIEW.html` gerçek Unity kamera görüntülerini, ses kesitlerini ve kanıt bağlantılarını içerir. Çentik, balta tutuşu, düşüş ve kütük görüntüleri doğrudan oyun kamerasından/ayrı inceleme kamerasından alınmıştır. Kullanıcı kabulü yerine geçmez. Yağmur/gece, bütün görüş açıları ve farklı orman yoğunlukları henüz kabul edilmedi.

Apple M3 Pro, Unity 6000.5.0f1 üzerinde 60 FPS hedefli editör ölçümü alındı. Native `GC Allocated In Frame` ve `Physics.Simulate` sayaçları geçerliydi. Çam koşusunda düşüş sırasında medyan kare 16,695 ms, P95 17,309 ms; bütün sahnenin ortalama Physics.Simulate süresi 0,144 ms idi. Görüntü kaydı dahil en kötü kare 86,405 ms. Bunlar tüm editörü, testi, dosya kaydını ve diğer sistemleri kapsar; ağaç sisteminin ayrıştırılmış maliyeti değildir. `editor-summary.json` ve CSV ham veriyi içerir. Önceki managed-allocation sıfır örnekleri geçerli GC kanıtı olarak kullanılmaz. Standalone oyuncu, 30/120 FPS karşılaştırması ve eşzamanlı çoklu ağaç bütçesi ölçülmeden üretim performansı onaylanmış sayılmaz.

## J. Durum

**İşlevsel doğrulama: MCP ile yürütüldü; güncel test matrisi esas alınır.** Gerçek huş/çam çentik ve devrilme, kaynak koruma, ses gönderimi ve hücre/kayıt yaşam döngüsü için kanıt var.

**Üretim kabulü: PARTIAL / VISUAL_NOT_APPROVED / AUDIO_NOT_APPROVED / PERFORMANCE_ACCEPTANCE_PENDING.** İnsan görsel/ses kabulü ve yukarıdaki kapsamlı performans ölçümleri yapılmadan “production-complete” denmez. Üretim sahnesi terfisi yapılmadı.
