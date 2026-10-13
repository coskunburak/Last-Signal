# Weapon stance — kullanıcı QA turu 01 / 2026-10-11

Durum: **Düzeltmeler uygulandı — tekrar manuel QA bekleniyor.** Bu revizyonda ajan Unity testlerini, build veya otomatik görsel kabul çalıştırmadı.

## Okunan gerçek kanıt

`Docs/Implementation/CharacterIntegration/Evidence/` altındaki kullanıcı çalıştırmalarının `results.xml` ve ilgili `Editor.log` içerikleri incelendi:

| Gate / evidence klasörü | Kullanıcının sonucu |
|---|---|
| `20261010-233502-character-edit-15d7d841` | 21 test, 0 hata, PASS |
| `20261010-233541-character-play-06eab58d` | 26 test, 2 hata |
| `20261010-233656-player-combat-edit-6067ff7e` | 92 test, 0 hata, PASS |
| `20261010-233713-player-combat-play-b76c1c31` | 51 test, 0 hata, PASS |
| `20261010-233918-locomotion-play-820289be` | 18 test, 1 hata |
| `20261010-234024-full-edit-0de2cb5e` | 683 test, 1 hata |
| `20261010-234045-full-play-1668f136` | 363 test, 8 hata |

Bu sonuçlar **önceki kod revizyonuna** aittir. Yeni düzeltmeler NOT_RUN; önceki PASS'ler yeni revizyonun doğrulaması olarak sunulmaz. Önceki revizyon test derlemesinde compiler hatası raporlanmamıştır.

## Bulgular ve düzeltmeler

### 1. Slide sonrası sol el teması

`RifleCrouchWalkSprintAndSlideKeepStockAndBoundedArms` son temas kontrolünde 92,9–94,8 mm sol palm hatası verdi; 40 mm sınırı aşıldı. Aynı sorun targeted, locomotion ve full suite'te tekrarlandı, dolayısıyla yalnızca test sırası sorunu değil.

Kaynakta `TorsoYaw`, animasyonun mevcut torso yaw değerinin üstüne her pozda ekleniyordu. Böylece profile 30° yazmak gerçek anatomik heading'in 30° olmasını sağlamıyordu; idle/slide/movement clip dönüşleri omuz açıklığının aim yönüne göre izdüşümünü ve support wrist erişimini değiştiriyordu.

Düzeltme: imported-rest axes ile kalibre edilen animasyon heading'i ölçülüp profile hedef heading'ine doğru fark uygulanır. Düzeltme ±60° ile sınırlı, pose weight ile blend edilir ve Chest/UpperChest paylaştırması korunur. Stock ve aim önceliği, bone lengths ve reach clamp değişmedi. Temas toleransları yükseltilmedi, silah geri çekilmedi.

**Belirsizlik:** Eski failure mesajı ham kemik pozlarını, aktif Animator state'ini veya reach residual'ı içermiyordu. Dolayısıyla 9,4 cm hatanın tamamının clip yaw kaynaklı olduğunu henüz kanıtlayamıyoruz. Kaynaktaki additive-heading kusuru düzeltildi; kullanıcı tekrar çalıştırması bunun failure üzerindeki etkisini gösterecek. Yeni probe mesajı scaled time/delta, pose weight, Animator state/transition/normalized time, shoulder heading, kol uzunluğu, target uzaklığı, reach residual ve wrist error içerir.

Yeni EditMode vakaları farklı clip/actor yaw değerlerinde aynı hedef heading'ini, weight sınırlarını ve düzeltme sınırını kontrol eder. Hareket testi slide sonrası idle'ı iki saniye boyunca tekrar örnekler.

### 2. Blend tamamlanması için yanlış kare sınırı

`RifleEquipWeightStaysBoundedAndAdsModalPauseKeepsContactsFrozen` 120 örnek sonunda weight=0,359 ve full suite'te 0,222 buldu. Rig'in blend süresi saniye cinsinden, testin limiti kare cinsindendi. Uncapped batch player'da 120 kare, 0,15 saniyelik blend'i tamamlamak zorunda değildir.

Test artık aktif profilin blend süresinden türetilen scaled-time deadline kullanır; donmuş saat için ayrı realtime watchdog vardır. Her örnekte monotonic/0…1 weight ve sonunda temas kontrolü korunur. Runtime blend hızı değiştirilmedi.

### 3. Full suite Input System statePtr hataları

Altı CharacterIntegrationPlayTests vakası yalnızca full-play'de `InputActionState.OnBeforeInitialUpdate → CheckStateIsAtDefault → ArgumentNullException(statePtr)` ile düştü. Stack trace, InputTestFixture'ın native player loop'u kendi değiştirdiği input manager'a yönlendirdiğini gösteriyor. Paylaşılan/önceki sahneden yaşayan input action durumları bu manager swap ile uyumsuz kalabiliyor.

Karakter fixture'ı InputTestFixture kalıtımından çıkarıldı; projede zaten kullanılan `InputFixtureIsolation.NativeInputScope` ile gerçek manager/clock korunuyor. Fixture'a özel sanal keyboard/mouse kullanılıyor, fiziksel cihazlar geçici askıya alınıyor ve teardown'da geri yükleniyor. Button olayları mevcut QueueButton helper'ıyla normal player loop'a gönderiliyor. Gameplay input koduna dokunulmadı; exception bekleme/susturma eklenmedi. Suite sırası etkisi için full-play yeniden çalıştırılmalıdır.

### 4. Optional URP referansının zorunlu sayılması

`SerializedContractTests.PlayerPrefabHasSafeCapsuleAndCompleteReferences`, `UniversalAdditionalCameraData.m_VolumeTrigger=null` üzerinde başarısızdı. Kurulu URP kaynak kodunda null trigger geçerlidir; `GetVolumeLayerMaskAndTrigger` bu durumda camera.transform kullanır. Eski test, tüm MonoBehaviour object-reference alanlarını ayırt etmeden zorunlu sayıyordu.

Test şimdi PlayerInputReader, Look, Motor, Stance, Interaction, Combat, inspection ve FPS action sunumunun zorunlu serialized alanlarını açıkça kontrol ediyor. Tüm hierarchy'de eksik script ve gerçekten bozuk serialized reference kontrolü devam ediyor. Geçerli optional null alanlar kabul edilir; Player prefabına test uğruna sahte referans eklenmedi. Bu kontrat testi artık character-edit gate'ine de dahil.

### Terminalde `#` hatası

Interactive zsh `#` satırlarını yorum olarak kabul etmeyebilir. Yeni komutlar yorum satırı içermez. Shell ayarı değiştirilmedi.

## Değişen dosyalar

- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponRig.cs`
- `Assets/LastSignal/Scripts/Runtime/Character/CharacterWeaponPoseProfile.cs` (TorsoYaw tooltip)
- `Assets/LastSignal/Scripts/Tests/EditMode/CharacterWeaponPoseTests.cs`
- `Assets/LastSignal/Scripts/Tests/EditMode/SerializedContractTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterIntegrationPlayTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterWeaponPosePlayTests.cs`
- `Assets/LastSignal/Scripts/Tests/PlayMode/CharacterPoseProbe.cs`
- `Tools/character-verify.py`

Bu turda prefab, controller, pose asset, vendor, combat veya motor değiştirilmedi. Önceki kaynak kopyaları `/tmp/last-signal-stance-feedback-01/` altında. Statik delimiter/diff kontrolü ve Python AST parse yapıldı; derleme/test yerine geçmez.

## Yeniden çalıştırma

### 2026-10-11 erişim düzeltmesi sonrası sonuçlar

Kullanıcının `20261011-001115-character-edit-5c768681/results.xml` kaydı 29/29 PASS; `20261011-001138-character-play-c155f323/results.xml` kaydı 26/26 PASS doğruluyor. Önceki `RifleCrouchWalkSprintAndSlideKeepStockAndBoundedArms` hatası bu Character PlayMode çalıştırmasında geçti.

`20261011-001329-locomotion-play-95285186` test XML'i üretmeden native süreç çökmesiyle sonlandı. Python subprocess dönüşü -10 (macOS SIGBUS); Editor.log satır 392'de Native Crash Reporting, son stack'te `Burst.Compiler.IL.Server.LibraryCompiler:CompileMethodGroupImpl` ve Burst Child Domain görülüyor. Bu kayıt bir NUnit assertion başarısızlığı göstermiyor; locomotion gate'i doğrulanmış sayılmaz. Burst derleyici iş parçacığında çökme görülmesi temel tetikleyiciyi tek başına açıklamaz. Oyun kodu veya test toleransı bu olay için değiştirilmedi. Sonraki adım aynı locomotion gate'ini tek başına tekrar çalıştırmak; tekrar native çökme olursa Burst tarafını ayrı teşhis etmek. Tam regresyon ve insan görsel kabulü hâlâ bekleniyor. Agent Unity testi/derlemesi çalıştırmadı.

### 2026-10-11 ikinci PlayMode geri bildirimi

`20261011-000540-character-edit-18f70d5b` ve `20261011-000608-character-edit-7ea30763`: 26/26 PASS, compiler diagnostic yok. API düzeltmesi kullanıcı çalıştırmalarıyla doğrulandı.

`20261011-000626-character-play-b74b12f4`: 26 test / 1 hata; `20261011-000746-locomotion-play-3226a7dc`: 18 test / 1 hata. İkisinde de `RifleCrouchWalkSprintAndSlideKeepStockAndBoundedArms`, slide sonrası idle örneklemesinde aynı erişim hatası verdi. Sol hedef 0.4451–0.4453 m, kol toplamı 0.4154 m; MaximumReach=0.97 ile kullanılabilir mesafe yaklaşık 0.403 m. IK residual ve gerçek bilek/palm hatası 0.0421–0.0423 m olarak örtüşüyor. Bu kanıt, kalan hatanın hedefin erişim dışında kalmasından kaynaklandığını gösteriyor.

Clavicle yardımı sonrası kalan erişim için `StockReachYaw` geometrisi eklendi: omuz–dipçik vektörü gövdeyle dönerken dipçik–bilek vektörü silah yönünü koruyor. Yeni profil alanı `TorsoReachYawDegrees` en fazla 12° yardım tanımlar; yeterli erişimde ek dönüş sıfır, erişim sağlanabiliyorsa en küçük yeterli dönüş seçilir. Sağ elin minimum/maksimum erişimi kötüleşecekse yardım kısıtlanır. Kemik uzunlukları ve silah yönü değiştirilmeden gövde döndürülür, dipçik/hedefler birlikte taşınır. Karşılanamayan hedef için residual raporlanmaya devam eder. Varsayılan ve RiflePose asset aynı değeri kullanır; mevcut profili koruyan authoring akışı geçerlidir.

Üç ölçek için geometrik EditMode vakaları, profil serialization kontrolü ve PlayMode yardım açısı sınırı eklendi. Temas toleransları değişmedi. Tanıya uygulanan yardım açısı eklendi. Bu değişikliklerin Unity derleme/test durumu **NOT_RUN**; hedef gate'ler ve sonrasında tam regresyon kullanıcı tarafından çalıştırılmalı. Üst gövde silüeti ve geçişleri ayrıca görsel kabul bekliyor.

### 2026-10-11 derleme geri bildirimi

`20261011-000311-character-edit-a3fecd12`, `20261011-000325-character-play-bb1d9703` ve `20261011-000337-locomotion-play-8d78bcbb` çalıştırmaları aynı CS0619 hatasında durdu; testler başlamadı, `results.xml` oluşmadı. Eklenen serialized-reference kontrolündeki `objectReferenceInstanceIDValue`, Unity 6000.5.0f1'de kullanımı yasaklanmış eski API idi. Kontrol `objectReferenceEntityIdValue` ve yeni kimlik türünün boş değerini temsil eden `EntityId.None` karşılaştırmasına geçirildi. Yerel Unity API belgeleri incelendi; eksik referans denetimi korunuyor. Bu düzeltmeden sonra Unity derlemesi veya test çalıştırılmadı; aşağıdaki gate'ler kullanıcı tarafından sırayla yeniden doğrulanmalı.

Unity'yi kaydedip normal kapatın. Her bloğu ayrı çalıştırın; bir hata çıkarsa o gate'in XML/log sonucunu paylaşın.

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

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/character-verify.py locomotion-play
printf 'Exit code: %s\n' "$?"
```

Bu üçü geçtikten sonra, optional-reference kontrolü ve fixture sırası etkisini doğrulamak için:

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

Runner her defasında yeni evidence klasöründe `Editor.log`, `results.xml`, `gate-result.json` bırakır. Bu turda build gerekmiyor. Testler geçtikten sonra ilk handoff'taki S013Cabin sekiz açılı görsel kabul tekrarlanmalı; özellikle slide→idle ve sol destek eli kontrol edilmeli. Yeni sonuçlar ve ekran görüntüleri bekleniyor.

### 2026-10-11 kullanıcı gate sonuçları

Aşağıdaki kullanıcı çalıştırmalarının `gate-result.json` ve `results.xml` dosyaları incelendi. Her gate `PASS`, Unity çıkış kodu 0, başarısız ve atlanan test sayısı 0; derleyici tanısı yok.

| Gate | Evidence klasörü | Geçen test |
| --- | --- | ---: |
| `character-edit` | `20261011-122339-character-edit-0da74342` | 29 |
| `character-play` | `20261011-122420-character-play-988b685d` | 26 |
| `player-combat-edit` | `20261011-122541-player-combat-edit-ed2cec06` | 92 |
| `player-combat-play` | `20261011-122620-player-combat-play-b8ae2a3a` | 51 |
| `locomotion-play` | `20261011-122848-locomotion-play-0845aeae` | 18 |
| `full-edit` | `20261011-123003-full-edit-0c5ab197` | 715 |

`full-play` bu sonuçlarla doğrulanmış sayılmaz. Önceki `full-play` fixture/sıra hatasının düzeldiğini sınamak için sıradaki gate ayrı çalıştırılmalı; ardından S013Cabin görsel ve Inspector kabulü yapılmalı.

### 2026-10-11 full-play geri bildirimi

`20261011-125202-full-play-1d43c0a9`: 394 test, bir hata, sıfır atlama; derleyici tanısı yok. Tek hata `RuntimeSmokeTests.SmokeTest_ThreeSessionCycles` içinde ateşten sonra şarjörün 30 kalmasıydı (29 bekleniyordu). Bu test eski `InputTestFixture` yöneticisini değiştiriyor ve oyuncunun action asset'ini reflection ile yeniden bağlıyordu. Test, diğer geçen tam-suite savaş testlerinde kullanılan `NativeInputScope` ve gerçek player loop olay kuyruğuna geçirildi. Her döngüde silahın `Ready`, oyuncu girişinin aktif olduğu açıkça doğrulanıyor; başarısızlıkta oturum ve sanal cihazlar temizleniyor. Oynanış kodu değişmedi. Bu düzeltmeden sonraki `full-play` sonucu **NOT_RUN**; kullanıcı aynı gate'i yeniden çalıştırmalı.
