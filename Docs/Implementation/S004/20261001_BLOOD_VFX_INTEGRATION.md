# S004 — Kan VFX entegrasyonu

Tarih: 2026-10-01. Unity 6000.5.0f1; URP 17.5.0.

**IMPLEMENTATION COMPLETE — MANUAL / FULL REGRESSION VALIDATION PENDING**

Bu kayıt uygulama ve hafif otomatik doğrulama kanıtıdır. Nihai sanat, tam regresyon, build ve performans kabulü değildir.

## Başlangıç ağacı ve korunmuş çalışma

Yerel çalışma ağacı otorite kabul edildi. Başlangıç: 594 silinmiş, 143 değiştirilmiş, 151 izlenmeyen yol girdisi; izlenmeyen klasör girdileri dosya sayısı değildir. Tam listeler `Evidence/20261001-BloodVFX/baseline-*.txt` içinde. LFS clean filtresi sandbox içinde `.git/lfs/tmp` yazımı istediğinden yalnız okuma amaçlı diff komutlarında geçici `-c filter.lfs.process= -c filter.lfs.required=false` kullanıldı; Git yapılandırması değiştirilmedi.

- A — Önceki özellik çalışması: NewPunch görseli, dismemberment/detached pool, controller, combat, kayıt/anatomi ve ilgili testler. Değişikliklerin geçmiş kökeni Git üzerinden yeniden varsayılmadı.
- B — Mevcut regresyon/triage alanları: movement, session/world geçişleri, save ve nüfus testleri. Beklentileri değiştirilmedi. Nüfus yöneticisine yalnız aşağıda açıklanan ölüm sunumu bağlantısı eklendi; görev başlangıcına göre tam küçük fark `world-presentation-only.patch` içinde.
- C — İçe aktarılmış vendor: `Assets/LastSignal/Blood VFX/Vefects/Free Blood VFX/`. Giriş SHA-256 listesiyle 398 dosya karşılaştırıldı.
- D — Bu görev: üç yeni runtime VFX sınıfı, editor authoring/validator, iki odaklı test sınıfı, proje sahipli shader/materyal/prefab/profil, production prefab anchorları ve iki mevcut runtime dosyasındaki dar bağlantı.

Reset, checkout, clean, stash, commit veya push yapılmadı. Görev başlangıcında değişmiş/izlenmeyen dosyalar arasında kod/asset tarafında yalnız `ZombieDismemberment.cs`, `WorldPopulationManager.cs` ve `LS_Zombie_Runtime.prefab` bu görev nedeniyle değişti; ayrıca mevcut S004 asset envanterine bu kayıt eklendi.

## Paket ve kaynak seçimi

Yerel meta AssetOrigin: **Free Blood VFX - URP 1.0.1**, productId **375130**, uploadId **906040**; yerel marka/yayıncı adı **Vefects**. Paket içinde lisans metni/satın alma belgesi bulunmadı; lisans hakkı doğrulanmış sayılmadı. Yeni dependency eklenmedi. BloodFX kullanılmadı.

Paket envanteri: 65 prefab, 36 materyal, 62 TGA, 6 shader, 7 FBX. `.cs`, ses dosyası veya VFX prefablarında AudioSource/MonoBehaviour yok. Ana shader URP lit, Amplify çıktısıdır; Amplify/VFX Graph runtime bağımlılığı eklenmedi.

| Amaç | Vendor kaynağı — VFX klasörüne göre | Kullanım |
| --- | --- | --- |
| Kafa/radyal burst ve damla görseli | `Particles/Once/VFX_Splat_01_Floor_Once.prefab` | Proje sahipli kopyada hareket, boyut, emisyon ve simülasyon yeniden yazıldı |
| Arter fışkırması | `Particles/Once/VFX_Splat_Directional_01_Floor_Once.prefab` | Aynı şekilde proje sahipli ParticleSystem türevi |
| Yüzey/pool şekli | `Materials/M_VFX_Fake_Blood_01_Lit.mat` içindeki `_ErosionTexture` | Yeşil kanal, yeni paylaşılan yüzey shader'ında alpha maskesi |
| SFX | Yok | Uydurulmadı; AudioSource veya AI hearing bağlantısı eklenmedi |

Kaynak efektler uçan biyolojik sıvı sistemi değildir: child Splat tek sabit kart yayar, hız sıfırdır. Vendor custom vertex stream/CustomData ayarları particle türevlerinde korunmuştur. Kopyalarda playOnAwake, loop, burst, cone, rotation3D, size3D, gravity, world simulation ve kapasite açıkça ayarlanmıştır. Vendor `_OpacityMask` boştur; RGB TGA'nın alpha kanalı yoktur. Yüzey shader'ının `.g` okuması bu nedenle gereklidir. Vendor materyalinde shader tarafından kullanılmayan eski `_MainTexture` GUID'i çözülemiyor; kullanılan LUT/erosion kaynakları yerelde mevcut. Vendor üzerinde düzeltme yapılmadı.

## Otorite sınırı ve ömür

`ZombieDismemberment.ResolveCommittedHit` başarılı maskeyi/gizlemeyi tamamlayınca bir `ZombieSeverPresentation` bildirir. Mevcut duplicate guard korunur. Kolun bağımlı eli ikinci istek üretmez. Callback hatası health transaction'a taşınmaz. RestoreState ve ResetState kopma efektini tekrar oynatmaz; ResetState aktif sunumu temizler.

`ZombieBloodVfxPresenter` explicit serialized anchor eşlemesi ve profil üzerinden tek scene-local `ZombieBloodVfxPool` kullanır. Runtime isim araması yoktur. Anchorlar editor'da Humanoid kemiklerden üretilir: head ekleminde Neck parent; upper-arm ekleminde Shoulder parent; hand ekleminde LowerArm parent. Yerel +Z dış püskürme yönüdür. Mevcut kopan parça havuzunun bağımsız fiziği değiştirilmedi.

**Gerekli dar nüfus değişikliği:** Eski `WorldPopulationManager.OnZombieDied` aynı karede nesneyi kapatıp yok ediyordu. Yeni akış önce mevcut unsubscribe, physical remove ve Dead ledger işlemlerini aynen tamamlar. Yalnız terminal ve halen kanayan aktör kısa sunum süresince tutulabilir. Canlı nüfus/save listesine dönmez; controller'ın mevcut death animasyonu ilerler. Varsayılan en uzun lease kafa için 5,1 saniye; 16 ceset slotu. Kapasite dolunca en eski lease bırakılır. Süre, gore OFF, hücre çıkışı, session reset ve scene değişimi cleanup yapar. Sunum başarısızlığında eski Shutdown/Destroy yolu kullanılır. Bunun tam streamed-world/save geçişinde uçtan uca regresyonu kullanıcı tarafından çalıştırılmalıdır.

## Profil ve bütçeler

| Bölge | Burst parçacık / cone | Ölçek | Spurt süresi / tepe oranı | Drip süresi / oranı | En fazla yüzey izi |
| --- | --- | --- | --- | --- | --- |
| Kafa | 18 / 65° | 1 | 1,6 sn / 25 sn⁻¹ | 2 sn / 5 sn⁻¹ | 3 |
| Kol | 12 / 32° | 0,7 | 1,1 sn / 18 sn⁻¹ | 1,5 sn / 5 sn⁻¹ | 2 |
| El | 7 / 20° | 0,42 | 0,65 sn / 10 sn⁻¹ | 1 sn / 3 sn⁻¹ | 1 |

Spurt basıncı profil eğrisi ve yaklaşık 0,286 saniyelik pulse ile azalır. Emitter anchor'ı takip eder; yayılan parçacıklar world-space hareket eder. Spurt sonrası düşük oranlı damlama başlar, sonrasında 1,5 saniyelik temizlenme payı vardır. Önce el, sonra aynı kol koparsa eski el kanaması iptal edilir.

Havuz ön ısıtma ilk uygun presenter etkinleşmesinde yapılır: 16 efekt seti; her sette burst/spurt/drip prefabı; child maxParticles 32/48/24. Disabled vendor root yayıcıları parçacık üretmez. Toplam görünür parçacık teorik üst sınırı 1664; bu ölçülmüş performans sonucu değildir. Warmup instantiate yapar; normal sever yolunda Instantiate/Destroy veya materyal kopyası yoktur. Ceset lease sonunda mevcut aktörün destroy edilmesi ayrı lifecycle işlemidir. Kapasite baskısında eski efekt kesilir, yeni nesne üretilmez.

## Yüzey kanı, pool ve ıslaklık

Aktif PC renderer yalnız SSAO içeriyordu; Mobile renderer da decalsizdi. URP Decal Renderer Feature eklenmedi. **Buradaki yüzey izleri DecalProjector değildir:** pooled, ışık alan, maske kullanan küçük quad'lardır. Kavisli yüzeye yapışan projeksiyon/conformal decal sağlamazlar.

Bir global 32 işaret havuzu hem sıçramayı hem büyüyen pool'u kapsar. Süre 25 sn, pool büyümesi 4 sn; son 3 sn fade. Kafa/kol/el için ilk aşağı ray pool adayı, kalanlar küçük ileri/aşağı fan. En fazla 3 aday × (1 ana + 4 köşe doğrulama rayı) = varsayılan kafa kopmasında 15 ray; tümü yalnız olay anında. Particle collision callback yok. Default layer beyaz listesi; trigger, Rigidbody, zombie/player ve transparent yüzeyler reddedilir. Renderer'ı açıkça collider üzerinde olmayan nesneler konservatif olarak reddedilir; TerrainCollider istisnadır. Çok materyalli renderer'da tüm materyaller opak olmalıdır. Yakın aynı yerler elenir. Dört köşe aynı collider ve yakın normal üzerinde değilse kart oluşturulmaz. Destek collider'ı kaybolur/pasifleşirse işaret silinir.

Normal hizalaması, rastgele rotasyon, sınırlı ölçek farkı ve 8 mm offset kullanılır. Shader ana/ek URP ışıklarını ve ortam SH'sini okur; emission yoktur. Paylaşılan materyal + slot başına tekrar kullanılan MaterialPropertyBlock taze kırmızıyı koyulaştırır, smoothness'i .48→.2 azaltır.

**Stump sanat sınırı:** Mevcut body-parts görselinde ayrı açıkça bağlanmış stump/cap renderer/material bulunmuyor. Bütün deri materyalini kırmızıya boyamak veya kemik kesimini tahmin ederek yeni cap mesh uydurmak yapılmadı. Yeni ıslaklık geçişi yüzey kanına uygulanır; mevcut kesik yüzeylerinin özel ıslak stump materyali bu uygulamada eklenmedi ve sanat kabulü olarak açık kalır.

## Gore, kayıt ve ses

Gore OFF yeni bildirimleri bastırır, aktif parçacık/işaret/ceset lease'lerini temizler. ON eski kopmaları yeniden oynatmaz. Sağlık, ölüm, anatomi maskesi, sağ kol saldırı yetkisi ve hearing sunum ayarına bağlanmadı. Kan izleri ve geçici kanama kaydedilmez. Mevcut torso wound korunur. Ek torso burst, detached first-impact efekti ve SFX eklenmedi; ilk ikisi istekte opsiyoneldi, ses kaynağı pakette yoktur.

## Doğrulama

- İlk yalnız kan EditMode: 5/5 PASS.
- Genişletilmiş hafif EditMode: 25/25 PASS (7 yeni kan testi + 18 mevcut WorldPopulationManager testi).
- İlk kısa PlayMode: 6/6 PASS.
- Son kısa PlayMode: 8/8 PASS; 3,46 saniye test süresi. Tek aktörle 80 doğrudan havuz isteği, kapasite ve materyal artışı kontrolüdür; 10/20/30 zombi stress testi değildir.
- Son cleanup/exception değişiklikleri sonrası aynı 25 EditMode testini tekrar başlatma denemesi Unity başlangıcında sonuç XML üretmeden uzadı; süre bütçesi gereği durduruldu. Bu deneme PASS değildir. Yukarıdaki 25/25, bu son küçük değişikliklerden önceki başarılı koşudur. Son dosyalarda 8/8 PlayMode ve editor validator tamamlandı.
- Editor validator: `LS_BLOOD_VALIDATION_PASS`, exit 0.
- Shell kullanıcı komut dosyası: `bash -n` PASS.
- Vendor SHA-256: 398 dosya, 0 değişiklik.

PlayMode kapsamı: temiz spawn, head tek request/sağlık ölümü, el→kol temizliği, gore toggle, disable/reuse, restore, süre sonu, yüzey hizalaması/bütçe/materyal sayısı, destek yüzeyi kaldırma ve terminal corpse lease/cell release sözleşmesi. Corpse test fixture'ı nav/AI başlatmaz; health death sonrası controller terminal state'ini kurar. Gerçek population materialization→death→save geçişinin tamamı test edilmiş sayılmaz.

İlk authoring validator denemesi, emission-disabled vendor root renderer'ın boş materyalini hata saydı. Validator yalnız çizim yapan particle renderer'larını kontrol edecek şekilde düzeltildi; authoring tekrar çalıştırılmadı, üretilmiş assetler üzerinden son validator geçti. İlk sandbox Unity başlangıcı lisans servisine erişemedi; durdurulup yetkili dış sandbox Unity yürütümüyle devam edildi. Native crash/Camera.Render testi çalıştırılmadı.

Tam EditMode, tam grafik PlayMode, build, render screenshot kabulü, 10/20/30 stress ve uzun profil çalıştırılmadı. Geçmiş 418/418 veya 171/176 sayıları bu görevin güncel test sonucu değildir.

## A01–A30 kabul izlenebilirliği

| Kabul | Kanıt / durum |
| --- | --- |
| A01 | Temiz spawn testi PASS |
| A02–A03 | Tek head isteği/aktif slot ve explicit neck binding PASS; ilk kare görseli MANUEL |
| A04–A05 | Bounded expiry PASS; pulse/drip görsel ayrımı MANUEL |
| A06 | Duplicate request bastırma PASS |
| A07–A09 | Beş serialized anchor yapısı PASS; pozlu kesim hizası MANUEL |
| A10–A11 | Profil ölçek/sayı sıralaması doğrulandı; algısal fark MANUEL |
| A12–A13 | Surviving-parent ve world simulation kodu incelendi; ölüm/ragdoll görseli MANUEL |
| A14–A15 | Disable, reuse, restore cleanup PASS |
| A16 | Gore OFF request ve aktif efekt temizliği PASS |
| A17 | Sunum istisnasında fatal health commit PASS; gore-off head görsel/gameplay karşılaştırması MANUEL |
| A18–A19 | Reduced gore logical sever/sağ kol yetkisi testi PASS |
| A20 | Toggle cleanup PASS |
| A21–A22 | Sabit mark kapasitesi ve normal hizası PASS |
| A23 | Layer/trigger/dynamic/transparent/köşe filtreleri kodda; uygunsuz yüzey çeşitleri MANUEL |
| A24 | Olay başına raycast; particle collision kapalı, kod/prefab incelemesi |
| A25 | Pool ve marks aynı 32 slot bütçesinde; havuz testi PASS |
| A26 | Tek aktör tekrar sever request'lerinde materyal sayısı artmıyor PASS |
| A27 | Warmup sonrası event yolunda Instantiate/Destroy yok; profiler GC kanıtı MANUEL |
| A28 | Odaklı testlerde beklenmeyen NullReference/MissingReference yok |
| A29 | Detached pool kodu değiştirilmedi; kapsamlı mevcut parça regresyonu ÇALIŞTIRILMADI |
| A30 | 398 vendor dosya hash eşitliği PASS |

## Kullanıcının çalıştıracağı komutlar

Önce bu projenin açık Unity editörünü kapatın. Her komut yeni zaman damgalı sonuç klasörü açar. Tam PlayMode grafik destekli çalışır; **-nographics eklemeyin**. Build mevcut `LastSignal.Editor.StudioNewPunchBuild.BuildDevelopmentMac` metodunu kullanır; `Builds/S004-NewPunch/LastSignal.app` çıktısını yeniden oluşturur, build özeti mevcut NewPunch evidence yoluna gider.

```sh
CHECK='/Users/burakcoskun/Last Signal/Docs/Implementation/S004/Evidence/20261001-BloodVFX/run-validation.sh'
bash "$CHECK" focused-edit
bash "$CHECK" focused-play
bash "$CHECK" regression-edit
bash "$CHECK" regression-play
bash "$CHECK" full-edit
bash "$CHECK" full-play
bash "$CHECK" build
```

İsteğe bağlı mevcut AI profil testleri:

```sh
bash "$CHECK" production-ai-profile  # mevcut 1/10/25 actor testi
bash "$CHECK" thirty-ai-profile      # mevcut 30 actor testi
```

Bunlar blood VFX'yi özellikle tetikleyen 10/20/30 kabulünün yerine geçmez. Gerçekte bulunmayan bir stress test adı verilmedi.

## Sıralı manuel görsel kabul

1. `Assets/LastSignal/Scenes/ZombieAcceptance.unity` veya production sahnesinde **1 zombi**, gore ON. Pistol/silah ile başı koparın. İlk karede tek okunur burst, boyunda darbeli spray, basınç azalması, azalan damla ve sonunda tam durma bekleyin. Kopmuş baş bağımsız hareket etmeli. Kesik yeri/deri dikişi, kart kenarı ve yanlış yön kontrolü yapın.
2. Sol kol, sağ kol, sol el, sağ el için ayrı temiz zombiler kullanın. Her birini önden, yandan ve arkadan izleyin. El efekti koldan, kol efekti kafadan küçük olmalı. Sağ kol/el sonrası saldırı teması olmamalı. Önce el sonra aynı kol senaryosunda bilekte hayalet kanama kalmamalı.
3. Zemin ve yakındaki duvar izlerini inceleyin. Kartlar duvar/zemin normaline yatmalı, kenardan taşmamalı, cam/oyuncu/zombi üstünde görünmemeli. İlk küçük zemin izi yaklaşık 4 saniyede büyümeli, 25 saniye sınırında kaybolmalı. Eğimli/kavisli yüzeyde quad sınırlarını özellikle kontrol edin.
4. Ölüm animasyonu, nüfusla spawn edilen zombi, hücre çıkışı ve save/load deneyin. Ledger'da ölü actor yeniden canlı sayılmamalı. Yalnız kanayan corpse kısa süre kalmalı; varsayılan head yaklaşık 5,1 sn sonunda kaldırılmalı. Hücre boşaltılınca corpse/boşlukta iz kalmamalı.
5. Gündüz, gece, el feneri; sahnede varsa yağmur koşullarında tekrarlayın. Neon/emission, bloom, siyahlaşma, aşırı opak kart ve plastik highlight olmamalı. VFX yağmur scalar'ına özel bağlı değildir.
6. Gore OFF ile aynı hasar/sever/ölüm/saldırı yetkisini karşılaştırın. Kanama sürerken OFF yapın: parçacıklar, marks ve retained corpse hemen temizlenmeli. ON yaptığınızda eski kanama başlamamalı.
7. Ardından 10, 20 ve 30 zombiyle aynı kopmaları tetikleyin; **bunlar manuel kullanıcı stress koşullarıdır**. Unity Profiler'da ilk warmup ile sonraki sever'ları ayırın: GC.Alloc, ParticleSystem, transparent overdraw, physics raycast, materyal sayısı, 16 efekt/32 mark/16 corpse sınırı ve ani FIFO kesilmeleri. Düşük/yüksek FPS'de görsel süreleri kontrol edin. Ölçülmemiş hedef FPS veya GC=0 iddiası yoktur.

## Dosyalar ve kalan riskler

Runtime: `Scripts/Runtime/VFX/ZombieBloodVfxProfile.cs`, `ZombieBloodVfxPresenter.cs`, `ZombieBloodVfxPool.cs`; bağlantılar `Scripts/Runtime/AI/ZombieDismemberment.cs`, `WorldPopulationManager.cs`. Editor: `Scripts/Editor/ZombieBloodVfxAuthoring.cs`. Testler: EditMode `ZombieBloodVfxTests.cs`, PlayMode `ZombieBloodVfxPlayTests.cs`. Bunların kökü `Assets/LastSignal/`; production binding `Assets/Resources/LS_Zombie_Runtime.prefab`.

Proje sahipli assetler `Assets/LastSignal/Blood VFX/ProjectOwned/`: üç `PS_LS_Blood_*` prefabı ve materyalleri, `LS_BloodSurface.prefab`, `LS_BloodSurface.shader`, `M_LS_BloodSurface.mat`, `ZombieBlood.asset` ve metaları. Authoring mevcut profili bulursa tuning'i ezmez; tekrar üretim yerine assetler düzenlenmelidir. Validator elle menü/executeMethod ile çalışır; her domain reload'da tarama yoktur.

Görsel tuning, özel stump wetness/cap işi, day/night/flashlight, planar yüzey kartı sınırları, tam world/save geçişi ve ölçülmüş performans hâlâ kabul kapısıdır. Paket sahipliği/lisans belgesi ayrıca envantere eklenmelidir. Global havuz tek production profilini destekler; ikinci farklı profil için ayrı havuz yaratmaz.

Son `git diff --check` kalan uyarılar önceki `Crowbar_Viewmodel.prefab`, `Player.prefab`, `FirstPersonMotor.cs` boşluklarıdır; bu görevin değişikliklerinde whitespace uyarısı yoktur. Bunlar düzeltilerek ilgisiz çalışma değiştirilmedi.

## 2026-10-02 sunum güncellemesi

Torso yüzeye oturan skinned yara ve güçlendirilmiş kafa kanaması: [uygulama ve kullanıcı doğrulama devri](20261002_WOUND_HEAD_PRESENTATION.md). Yukarıdaki eski test sonuçları bu güncellemenin kabul kanıtı değildir. Yeni kafa toplam efekt/ceset sunum penceresi 5,8 saniyedir.
