> 2026-10-11 kullanıcı test sonuçları ve ilk düzeltme turu: [WEAPON_STANCE_QA_01.md](WEAPON_STANCE_QA_01.md). Aşağıdaki NOT_RUN bilgileri ilk teslim anını anlatır. Kullanıcı ilk revizyonu çalıştırdı; yeni düzeltmelerin tekrar test edilmesi gerekiyor.

# Weapon stance overhaul — 2026-10-10

**IMPLEMENTATION COMPLETE — MANUAL QA PENDING**

Unity testleri, Unity Test Runner, build ve otomatik görsel kabul çalıştırılmadı. Yeni C# derlemesi doğrulanmadı. Bu belge PASS kanıtı değildir. Statik diff/runner sözdizimi kontrolleri ve değişiklik öncesi salt okunur Unity asset ölçümleri yapıldı.

## A. Başlangıç ve kök nedenler

- Proje: `/Users/burakcoskun/Last Signal`.
- Dal: `s012-integrated-graybox-slice`; başlangıç HEAD: `c41dcf1a0e0942377556227e938ef847391623bb`.
- Başlangıç `git status --short`: 9.624 kayıt. Tam başlangıç durumu ve düzenlenen mevcut dosyaların kopyaları `/tmp/last-signal-stance-baseline/` altında kaydedildi. Mevcut karakter rig abonelik/şarjör düzeltmeleri, inspection kamera sahipliği düzeltmeleri ve mevcut PlayMode testleri korundu. Player, scene, combat/FPS ve vendor dosyaları bu çalışmada düzenlenmedi. Commit/push yapılmadı.
- Gerçek kurulu/bağlı Unity: **6000.5.0f1**, `88b47c5e7076`; executable macOS **arm64**. Sahne: `Assets/LastSignal/Scenes/Production/S013Cabin.unity`.
- Diesel avatarı asset incelemesinde `isHuman=true`, `isValid=true`; iki shoulder, upper/lower arm, hand ve finger landmark mevcut. Importer `globalScale=1`, `useFileScale=1`. İskeletin 100× iç ölçeği dünya boyunun 100× olduğu anlamına gelmiyor: upper arm yaklaşık 0,188 m, forearm yaklaşık 0,227 m; shoulder açıklığı yaklaşık 0,471 m. Vendor FBX/importer değiştirilmedi.

**Doğrulanan kaynak/asset sorunları:**

1. MRPoly sağ kabza mesh üçgenleri y=0,092 / z=-0,140 kesitinde x=0,034098…0,064293 m; eski sağ palm x=0,079 m dış yüzeyden yaklaşık 14,7 mm uzaktaydı. Sol foregrip y=0,105 / z=0,100 kesitinde x=0,039683…0,058709 m; eski x=0,032 yaklaşık 7,7 mm dışarıdaydı. Bunlar ölçek öncesi değerlerdir; world rifle ölçeği 0,85.
2. Stock hedefinin başlangıcı hareketli upper-arm origin, offset eksenleri ise oyuncu root rotasyonuydu; göğüs/omuz dönerken offset aynı anatomik çerçeveyi izlemiyordu. 35° yaw tek Chest kemiğinde uygulanıyordu.
3. Eski arm solve geçmiş bend plane tutmuyor, çok küçük hedef vektöründe çözümü bırakıyor ve erişilemeyen hedef mesafesini ayrı raporlamıyordu. Elbow hint, animasyonun anlık dirseğiyle karıştırılıyordu.
4. Sprint/slide açıları boolean durumla tek karede ekleniyordu. Reload sırasında şarjör aşağı inerken destek hedefi sabit reload referansında kalıyordu.
5. Input-modal pause sırasında combat `AimAmount` değerini azaltabiliyor; rig bu değişimi doğrudan okuyordu. Silahın kozmetik ADS pozunu ayrıca dondurmak gerekiyordu.
6. Authoring her çalıştığında RiflePose için yeni defaults oluşturup mevcut profile CopySerialized uyguluyordu; elle yapılan kalibrasyon kayboluyordu.

**Hipotez / insan incelemesi gerekli:** Tek kemikteki 35° yaw ve eski geniş elbow hint'lerin ceketteki sıkışık siluete katkısı makul, fakat bu oturumda referans ekran görüntüsü sunulmadı. 30° dağıtılmış yaw, daha yakın anatomik elbow hint'ler ve yeni stock offset'lerinin görsel olarak daha iyi olduğu henüz kabul edilmedi. Sayısal temas tek başına anatomik/görsel kabul değildir.

## B. Uygulanan çözüm

- Tek CharacterWeaponRig korundu. CharacterArmIK onun kullandığı saf geometri yardımcısıdır; ikinci MonoBehaviour/rig/animation framework değildir.
- Göğüs yaw/pitch katkısı Chest %35, UpperChest %65 paylaştırılır (UpperChest yoksa Chest %100). Head karşı dönüşü korunur. Animator'ın nefes/gait hareketi taban poz olarak kalır.
- Shoulder pocket eksenleri gerçek imported upper-chest/rest frame ile kalibre edilir. Offset actor metresidir, kemiklerin 100× local ölçeğiyle çarpılmaz. Uniform actor scale hesaba katılır.
- Clavicle yalnızca reach son %10'unda, profildeki en fazla 8° sınırına kadar katkı verir. Stock noktası bu yardım öncesinde yakalanır; omuz yardımı silahı sürükleyen bir geri besleme oluşturmaz. Omuz, chest, arms, hands ve fingers her Update öncesi/on-disable restore edilir.
- Stock ve aim önceliklidir. Eller gerçek palm-to-wrist dönüşümüyle çözülür. Kol uzatılmaz; üst/alt segment uzunlukları korunur. Hedef erişilemiyorsa wrist clamp edilir ve reach residual milimetre olarak gösterilir.
- Elbow düzlemi önceki kareden actor axes içinde saklanır, tekillik çevresinde korunur ve saniyede 360° ile sınırlandırılır. Weight girişinde animasyon dirseği korunarak hedefe geçilir. Minimum reach/hiperextension sınırları ve sıfır/sonlu olmayan girdiler ele alınır.
- Önkol eksenindeki pronation/supination bileşeni swing/twist ayrımıyla hesaplanır; profildeki 60° sınırına kadar önkola dağıtılır. Wrist hedef yönünü korur; dönüş forearm boyunca olduğu için kol uzunluğu/hedef konumu değişmez.
- Sağ/sol palm soketleri ölçülen mesh yüzeylerine taşındı; StockContact X'i stock yüzeyinin merkezine alındı. Mevcut avuç normal/finger yönü, imported wrist kalibrasyonu ve trigger-index düzeni korundu. Parmak kalınlığı actor ölçeğine uyarlandı.
- Sprint/slide quaternion blend, equip/unequip weight ve camera pitch geçişleri scaled delta time ile ilerler. Modal pause ve timeScale=0 geçişleri dondurur. ADS/hip ayrımı profil verisinden gelir; FPS viewmodel offset'i kullanılmaz.
- Kabul edilmiş ShotFired olayından bounded cosmetic recoil üretilir: göğüs, tüfek ve eller birlikte tepki verir. Recovery üstel olup kare hızından bağımsızdır. Gameplay recoil/hit çözümü değiştirilmedi.
- Reload normalize zamanı gerçek tactical/empty süresinden alınır; sin² eğrisi uçlarda sıfır hıza sahiptir. Destek palm hedefi şarjörün gerçek yer değiştirmesini izler. Kesinti/holster/action release şarjörü eski yerine getirir. **Tam authored reload klibi yapılmadı.**
- Action sahipliği Animator değerlendirmesinden sonra tekrar kontrol edilir. Treatment, diğer kol eylemleri, ölüm ve seated durumda firearm pose aynı sunum karesinde bırakılır. Seated Humanoid IK yalnızca CharacterSeatPresentation'a aittir.
- Inspector/Scene tanısı editör assembly'sindedir; varsayılan çizim kapalıdır. Üretim runtime'ında debug draw, per-frame LINQ, material instance, hierarchy search veya asset load eklenmedi. Magazine parent/ancestry equip sırasında cache edilir.

Kalibrasyon sırası: önce mesh temas yüzeyleri, sonra rest frame ve arm boyları, ardından nominal reach için stock pocket/hip eğimi, en son elbow yönü ve geçişler. Rifle WorldScale 0,85 korunmuştur. Yeni hip pocket=(-0,060,-0,015,0,045), ADS=(-0,065,0,005,0,045); hip pitch=5°, ADS=0°, yaw=30°, torso pitch ağırlığı=0,45. Varsayılanlar ile RiflePose asset aynı değerleri kullanır.

## C. Değişen dosyalar

Runtime:
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponRig.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterArmIK.cs` + yeni meta
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponPoseProfile.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterHandPose.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterInspectionCamera.cs`

Authoring/asset:
- `Assets/LastSignal/Scripts/Editor/Character/DieselCharacterAuthoring.cs`
- `Assets/LastSignal/Scripts/Editor/Character/CharacterWeaponRigEditor.cs` + yeni meta
- `Assets/LastSignal/Art/CharacterIntegration/RiflePose.asset`
- `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldRifle.prefab`

Testler/handoff:
- `Assets/LastSignal/Scripts/Tests/EditMode/CharacterIntegrationAssetTests.cs`
- `Assets/LastSignal/Scripts/Tests/EditMode/CharacterWeaponPoseTests.cs` + yeni meta
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterIntegrationPlayTests.cs` (mevcut sınıf partial yapıldı; önceki değişiklikler korundu)
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterWeaponPosePlayTests.cs` + yeni meta
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterPoseProbe.cs` + yeni meta
- `Assets/LastSignal/Scripts/Tests/PlayMode/S018SurvivalPlayTests.cs`
- `Tools/character-verify.py`
- Bu handoff ile `SHOULDER_IK_INTEGRATION.md` güncel revizyon notu.

Mevcut asset/prefab GUID'leri korundu. Authoring socket konfigürasyonu tekrar uygulanabilir, mevcut socket Transform'larını yeniden kullanır. Yeniden üretim mevcut RiflePose kalibrasyonunu korur; yalnızca yoksa defaults yaratır. **Tam authoring çalıştırılmadı:** mevcut pipeline controller, clips, body/player prefab ve FPS palette üzerinde daha geniş yazımlar yapar. Bu düzeltmeyi kullanmak için Author Diesel integration komutunu çalıştırmak gerekmez.

## D. FPS ayrımı ve işlem sırası

PlayerCombatController/WeaponController state, ammo, fire/reload ve gameplay aim otoritesidir. VAL/MRPoly viewmodel, mevcut FPS animasyonu/sight/muzzle ve camera recoil kendi katmanında kaldı. World rifle hasarsız/collider'sız cosmetic prefab olarak WorldBody altında yaşar; FPS skeleton'a bağlanmaz.

MRPoly/VAL'den mesh/muzzle/sight yönü, gerçek kabza ilişkileri, accepted-shot ve reload timer/event sözleşmeleri kullanıldı. FPS el Transform'ları Diesel'e kopyalanmadı. Kaynakta FPS referansları ayrı animasyon skeleton'ına bağlıdır; world wrist hedefi değildir.

Sıra: mevcut combat/motor → PlayerLocomotionPresenter Update (100) → rig restore/update (300) → Animator/muscle değerlendirmesi + Base Layer IK (seated ownership) → CharacterSeatPresentation LateUpdate (250) → CharacterWeaponRig LateUpdate (300) → inspection camera (500). Bone caches her kare taban animasyonu restore eder; transient Action layer ve eski generic Grip layer'ın mevcut sahipliği korunur.

## E–F. Beklenen görünüm ve yazılmış testler — NOT_RUN

Beklenti: sağ palm kabzaya yaklaşır, destek eli foregrip yüzeyine oturur; dönüş tek göğüs ekleminde toplanmaz; stock göğüs/omuz çerçevesini izler; dirsek tekillik çevresinde ters dönmez; sprint/slide daha yumuşak başlar; reload eli şarjörle birlikte hareket eder. Bunlar uygulanmış davranış hedefleridir, görsel PASS değildir.

EditMode (LastSignal.EditModeTests): mevcut referans/avatar/controller/prefab kontrolleri; yeni iki segment uzunluğu, minimum/maksimum erişim ve residual, sıfır hedef, geçersiz girdi, collinear/antipodal elbow plane, zaman adımı bölünmesi, bounded shoulder/forearm twist, gerçek Diesel wrist/palm roundtrip (0,5×/1×/2×), finger restore drift, mesh triangle contact, tekrar socket authoring, profile JSON/reference/legacy compatibility. Mevcut hip/ADS reach testi yeni chest frame ve bounded clavicle yardımıyla güncellendi.

PlayMode (LastSignal.PlayModeTests): gerçek Player prefabı, combat input/state ve Animator üzerinden idle, ADS↔hip, crouch/walk/sprint/slide, ±55° pitch/kol uzunluğu, accepted-shot/ammo/recoil recovery, reload tamamlanma/kesinti, tekrar slot ve gerçek equip/unequip, disable/re-enable/stale reference, pose weight sınırları, ADS modal pause, timeScale=0 ve slow/resume, seated pause/exit, ölüm/recovery. CharacterPoseProbe gerçek LateUpdate sonunu gözler; IK bypass veya mock solver kullanmaz. Mevcut gerçek VehicleOccupancy testleri ve S013Cabin S018 treatment/consume testi de hedefli gate'e dahildir; treatment/consume sırasında firearm ownership bırakılması için assertion eklendi.

Tüm yeni/değişen testler **NOT_RUN**. Yeni C# compilation **NOT_VERIFIED**. Referans screenshot uydurulmadı.

## G. macOS manuel komutlar

Önce açık Unity Editor çalışmalarını kaydedip Unity'yi normal kapatın. Aynı projede batchmode ile açık Editor çakışır; runner açık Unity görürse durur. Lock dosyalarını silmeyin. Aşağıdaki komutları **tek tek**, önceki sonucu kontrol ederek çalıştırın. Bu ajan hiçbirini çalıştırmadı.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py character-edit
printf 'Exit code: %s\n' "$?"
```

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py character-play
printf 'Exit code: %s\n' "$?"
```

Combat regressions (mevcut player/health/ammo/weapon/vehicle/survival kontrolleri de dahildir):

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py player-combat-edit
printf 'Exit code: %s\n' "$?"
```

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py player-combat-play
printf 'Exit code: %s\n' "$?"
```

Locomotion:

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py locomotion-play
printf 'Exit code: %s\n' "$?"
```

Full regression (ayrı çalıştırın):

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py full-edit
printf 'Exit code: %s\n' "$?"
```

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py full-play
printf 'Exit code: %s\n' "$?"
```

Testler ve Editor görsel incelemesi sonrasında development-player kontrolü istenirse mevcut gerçek build metodu:

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py development-build
printf 'Exit code: %s\n' "$?"
```

Runner gerçek `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity` executable'ını kullanır. Assembly'ler `LastSignal.EditModeTests`, `LastSignal.PlayModeTests`; full-edit ayrıca `LastSignal.Art.EditorTests`. Build metodu `LastSignal.Editor.CharacterVerification.DevelopmentMac` olup S013Cabin'i build eder. Render callback kullanan mevcut testler için `-nographics` eklemeyin. Test komutlarında erken kapatan `-quit` yoktur.

Her komut benzersiz `Docs/Implementation/CharacterIntegration/Evidence/<timestamp>-<gate>-<id>/` altında `invocation.json`, `Editor.log`, testlerde `results.xml`, `gate-result.json` bırakır. Unity exit code ve wrapper sonucu gösterilir; sıfır test başarı sayılmaz. Full regression önceki sabit evidence dosyalarını mevcut koruma helper'ıyla korur. Build isteğe bağlıdır ve ayrıca `build-summary.txt`/`LastSignal.app` üretir.

## H. S013Cabin manuel görsel kabul

1. Testler geçtikten sonra projeyi Unity 6000.5.0f1 ile açın. `Assets/LastSignal/Scenes/Production/S013Cabin.unity` sahnesini açın. Author Diesel integration çalıştırmayın.
2. Play'e girin; oyun menüsü görünüyorsa mevcut Yeni Oyun/Devam akışıyla oyuncuyu oluşturun. Tüfeği **1** ile seçin. Game görünümüne odaklanın. **6** FPS/inspection, **7** açı değiştirir. Inspector üzerinden player/capsule/camera offset değiştirmeyin.
3. Sekiz açı sırası: **ön, arka, sol, sağ, ön-sol üç çeyrek, ön-sağ üç çeyrek, yüksek, alçak**. Her açıdan screenshot alın. Inspection yönü hareket/aim otoritesini değiştirmez; mouse hâlâ FPS aim'i yönetir. Dar kabin kamerayı collision ile yaklaştırırsa açık alana çıkın.
4. Her açıda omuz açıklığı, stock pocket teması ve sırtı delmeme, yan görünüşte namlu doğrultusu, iki avuç/parmak teması, wrist roll, elbow flare/torso teması, göğüs/jacket deformasyonu ve geçişlerin akıcılığını kontrol edin. StockError=0 olması bu maddelerin geçtiği anlamına gelmez.
5. Aynı matrisi idle; **WASD** yürüme; **Left Shift+W** sprint; **C/Left Ctrl** crouch; sprint sırasında crouch ile slide; hip-fire; **sağ mouse** ADS; **sol mouse** ateş; **R** reload; **3** melee ve **1** rifle switch; **E** interaction; mevcut envanterden bandaj/tedavi; **Escape** pause/resume durumlarında tekrarlayın. Bindings kullanıcı ayarlarında değiştirilmişse mevcut binding'leri kullanın.
6. ADS'de yukarı/aşağı bakın; ±55° world pose sınırına özellikle bakın. Ateş sırasında stock/ellerin ayrılmasını ve recoil dönüşünü izleyin. Yarım reload'da slot değiştirin; şarjör/elde eski state kalmamalı. Pause'u hem hip hem ADS'de deneyin.
7. Sahnedeki mevcut araç kullanılabilir olduğunda mevcut **E** giriş/çıkış akışını kullanın. Rifle visual/IK seated durumda kapanmalı; steering elleri CharacterSeatPresentation'a ait kalmalı. Çıkışta yeni poz blend ile dönmeli.
8. Tanı için Hierarchy'de çalışan Player'ın **WorldBody** nesnesini seçin. CharacterWeaponRig Inspector'da **Draw selected pose contacts** açın, Scene Gizmos'u etkinleştirin. Pocket/Stock, palm/wrist target, actual wrist, elbow hint ve reach disc'leri görünür. Inspector palm error ve unreachable distance'ı mm gösterir. Görsel kabul screenshot'larında çizimi kapatın.
9. Başlangıç referans screenshot'larını aynı açı/aim/state ile karşılaştırın. Bu oturumda yalnızca metin talebi vardı; ajan önce/sonra görsel karşılaştırması yapmadı. Son görsel kabul kullanıcıya aittir.

## I–J. Kalan riskler ve sonraki adım

- Derleme, runtime davranışı, performans ve test sonucu henüz kullanıcı kanıtı bekliyor. Tüm testler NOT_RUN.
- Extreme pitch, animasyon kaynaklı büyük shoulder hareketi, sprint/slide ve reload sırasında reach clamp'in oluşturabileceği el boşluğu insan incelemesi gerektirir. Residual saklanmaz; Inspector'da görünür. Kamera/capsule oynatılarak gizlenmedi.
- Bu çözüm pozitif uniform actor scale ve mevcut Humanoid mapping için tasarlanmıştır; nonuniform/mirrored skeleton scale destek iddiası yoktur.
- Parmak/jacket temasları ve bilek twist'i rig/mesh skinning'e bağlıdır. Mesh yüzey temasını düzeltmek parmak penetrasyonunu tek başına kanıtlamaz.
- Dedicated full-body rifle reload, treatment ve vehicle entry/exit klipleri eklenmedi; yeni asset satın alınmadı/indirilmedi.
- Mevcut repo geniş ve kirli; tam regression ilgisiz önceden var olan arızalar gösterebilir. Sonuç geldiğinde failure/stack trace kapsamına göre ayırın.

Burada duruldu. Kullanıcıdan `gate-result.json`, `results.xml`, `Editor.log` ve sekiz açıdaki güncel karakter screenshot'ları bekleniyor. Sonuçlara göre hedefli düzeltme yapılacak ve yeni manuel komutlar verilecek; ajan testleri kendisi çalıştırmayacak.
