# S015 — Uygulama ve manuel doğrulama devri

## Güncel durum — 2026-10-07

**S015 = PARTIAL.** Burak'ın odaklı EditMode koşusu **14/14 PASS**, odaklı PlayMode koşusu **6/6 PASS**. Son tam EditMode koşusu **524/524 PASS**, Art Editor **10/10 PASS**. Son iki tam PlayMode koşusu **279/280 FAIL**; 17 testlik fixture tekrarları **16/17 FAIL**. C bırakma olayı açık `InputSystem.Update()` çağrısından sonra da işlenmedi; güncelleme/olay zamanı tanısı bekleniyor. Build ve insan işitsel kabulü **NOT_RUN**.

## Başlangıç ve kapsam

- Dal: `s012-integrated-graybox-slice`; HEAD: `97516c8b20793f4ca834e214318bdac45f041a4d`.
- Başlangıç çalışma ağacı dirty: 17 modified, 3411 deleted, 44 untracked kayıt. Ham giriş `S015_ENTRY_GIT_STATUS.txt`. Önceden mevcut AI/vehicle/camera/VFX değişiklikleri geri alınmadı. Commit/push yapılmadı.
- Açık Unity: 6000.5.0f1, `S013Cabin.unity`, Play kapalı. İkinci Editor açılmadı. MCP yalnız authoring, importer, prefab ve derleme incelemesinde kullanıldı.
- Canonical S015 tam okundu; P04 G4 ve LS-DOC-11/18/19/21 ses/erişilebilirlik bölümleri hedefli incelendi.
- S014 ledger/handoff ve daha yeni final summary hâlâ PARTIAL/insan kabul açığı içeriyor. D138 tedavi otoritesi eksikliği, görsel onay ve edinim kanıtı S015 ses sunumu kodunu teknik olarak engellemiyor; S014 durumu değiştirilmedi.
- Dört paket gerçek dosya sayımı: 1745 WAV, 3 PDF, 14 görsel, 1 URL dosyası. Vendor runtime/demo architecture yok. 59 farklı klip, 32 katalog cue girdisine bağlandı.

## Mimari

- `AudioCatalog`: project-owned mixer, beş bus ve gerçek clip referansları. `ProductionAudio` SessionFlow'un sahneye ait sunum bileşeni. Static singleton / DontDestroyOnLoad yok.
- Beş kullanıcı seviyesi: `20*log10(value)`, güvenli `-80 dB` mute floor. Mevcut `ZombieGorePreference` gibi PlayerPrefs cihaz tercihi; ayrı gameplay save şeması yok. Ses ayarı UI'sında debounce + pause/quit flush ile kayıt.
- Player adımları motorun gerçek substep displacement olayından; gameplay `FootstepNoiseProducer` aynı yerde ve değiştirilmeden kaldı. Walk/sprint/crouch stride/gain ayrı. Zombi adımları bounded gerçek hareket mesafesinden; telegraph sırasında adım yok.
- Yüzey metadata'sı: cabin floor wood, entry path/gravel zone gravel, genel zemin grass. Görsel soil mesh'in gerçek elipsi (22 × 19.5 yarıçap), 3.25 m foundation cutout ile physics collider eklemeden audio area. Aynı düzlemde cabin wood tie-break. Default dirt fallback.
- `WeaponAudioPresenter` mevcut sınıfı genişletildi ve gerçekten eksik olan rifle prefab component'i eklendi. Shot/reload/empty/equip olayları aynı controller'dan gelir. Reload %16 detach; gerçek ammo commit insert; empty reload %86 bolt. VAL animation event listeleri boş olduğundan authoritative timer okunur. Receiver'lar duplicate/interrupt korumalı; damage/ammo/fire-rate/timer yazılmaz.
- `ZombieRuntimeState.Changed` semantic event'i; idle, alert, approach, windup, contact, hurt, death cue'ları. Yakın warning idle/approach sesini keser; ilk approach alert'ten en az 4 sn sonra. Death/despawn kaynakları temizlenir.
- `WorldClock` mevcut yağmur/gece otoritesi. Cabin spatial blend ve restrained reverb; occlusion sunuma ait. Duvar arkası gain 0.65, low-pass 3200 Hz; yön tamamen kaybolmaz. En fazla 4 ray/100 ms; hot path LINQ veya event başına dizi üretimi yok.
- Müzik gerçek zombi durum değişimi ve görünür chase/attack için 1 sn presentation heartbeat ile beslenir; 4 sn threat TTL, 8 sn relief, yumuşak gain geçişi. Load'da WorldSimulation nesnesi değişince, session stop/restart ve component disable'da temizlenir.
- Yakın attack warning ambience'i 0.9 sn boyunca 0.55, fire tail'lerini 0.7 oranında kısar. Audio bus'larından AI hearing'e geri bağlantı yok. Sunum rastgeleliği kendi `System.Random` örneğinde; Unity gameplay RNG'sini tüketmez.
- Captions gerçek cue menzilini paylaşır, Master/Voice mute'dan bağımsızdır; sadece Ön/Sol/Sağ/Arka + semantik metin. Koordinat/mesafe/duvar arkası kesin kimlik verilmez. Mevcut objective/journal/interaction metinleri korunur.
- Gerçek `TryInteract` başarısı düşük click üretir; başarısız işlem üretmez. Melee mevcut commit olaylarını kullanır.

## Önemli değişen dosyalar

- `Assets/LastSignal/Scripts/Runtime/Audio/`: katalog, ayarlar/UI, pool/atmosfer/müzik, surface/footstep/zombie presenter.
- `Assets/LastSignal/Scripts/Runtime/Combat/{WeaponAudioPresenter,WeaponAnimationPresenter,MeleeWeaponPresenter}.cs`.
- Küçük sunum seam'leri: `FirstPersonMotor.cs`, `ZombieRuntimeState.cs`, `InteractionController.cs`, `SessionFlow.cs`.
- `Assets/LastSignal/Scripts/Editor/S015AudioAuthoring.cs` idempotent üretim authoring aracı.
- `Assets/LastSignal/Audio/Mixers/LastSignal.mixer`, `Audio/Config/S015AudioCatalog.asset`.
- `Assets/LastSignal/Scenes/Production/S013Cabin.unity`, Player/LS_Zombie_Runtime/Weapon_AssaultRifle prefab'ları.
- `Assets/LastSignal/Scripts/Integrations/MotionCore/Editor/VehicleDrivingAuthoring.cs`: gerçek kırık NOX yolunun `S15 Sound Pack` karşılığı. Vehicle runtime dirty dosyaları düzenlenmedi. Mevcut dynamic vehicle AudioSource'ları BeginSession'da Effects'e yönlendirilir; clip/gain/motor/horn/impact otoritesi korunur.
- Yalnız 59 seçili vendor klibinin import `.meta` ayarları; WAV dosyaları değiştirilmedi.
- `S015AudioTests.cs` (14 EditMode), `S015AudioPlayTests.cs` (6 PlayMode), `Tools/s015-verify.py`.

## Doğrulama sahipliği

Burak'ın `Docs/Implementation/S015/Evidence/20261007T141921-948030Z-focused-edit/` koşusu: `LastSignal.Tests.S015AudioTests`, Unity EditMode, `total=14`, `passed=14`, `failed=0`, `skipped=0`, `result=Passed`, process exitCode=0; `source-preservation.json` değişen kaynak göstermiyor. Açık Editor nedeniyle ilk komut kilit korumasında durmuş; test veya başarısız test koşusu değildir.

Burak'ın `Docs/Implementation/S015/Evidence/20261007T142322-844665Z-focused-play/` koşusu: `LastSignal.Tests.S015AudioPlayTests`, Unity PlayMode, `total=6`, `passed=6`, `failed=0`, `skipped=0`, `result=Passed`, process exitCode=0; `source-preservation.json` değişen kaynak göstermiyor.

Burak'ın ilk tam regresyonu: `Evidence/20261007T142820-974603Z-full-edit/` ana EditMode 524/524 PASS ve Art Editor 3/10 (7 hata); `Evidence/20261007T142854-642143Z-full-play/` PlayMode 277/280 (3 hata). Her iki koşuda `changedSources=[]`, tarihsel kanıt yolları geri konmuş. Art Editor hatalarının tamamı URP Lit materyallerinin batchmode'da `GetTag` sonucuyla yanlış sınıflanması. İki PlayMode araç testi batchmode'da yürütülmeyen `WaitForEndOfFrame` nedeniyle durdu. Shader doğrulaması gerçek URP Lit shader'ını tanıyor; araç testleri bir sonraki frame sınırını bekliyor. `Tools/s015-regression.py` kullanıcı çağırdığında tarihsel evidence yollarını koruyarak S015 altında yeni XML/log üretir. Tam regresyon PASS sonrası mevcut S013 build workflow ve aynı rotada headphone/speaker/mute/ayar kabulü istenecek.

Dar tekrar: `Evidence/20261007T144912-115720Z-art-editor/` 10/10 PASS, kaynak değişimi yok. `Evidence/20261007T144933-690211Z-full-play/` yalnız ilk filtreli eğilme testi çalıştı ve FAIL: `StandBlocked=False`, `CanStand=True`, `GameplayActive=True`, `CrouchHeld=True`, `requests=1`, `sliding=False`. Yani ikinci C basışında ikinci input request gelmedi; tavan veya kayma engeli yok. Test, bırakılan C tuşunu input reader'ın gözlediğini ayrıca doğrulayacak ve bir frame daha bekleyecek şekilde daraltıldı.

İkinci dar tekrar: `Evidence/20261007T145209-287837Z-full-play/` eğilme testi 1/1 PASS. `Evidence/20261007T145238-448041Z-full-play/` tekerlek sunumu 1/1 PASS; cockpit body-feel testi ofseti 0 ölçerek FAIL. `FirstPersonLook.Update` ofseti çıkarır, `LateUpdate` geri uygular; `yield return null` coroutine'i bu ikisinin arasında sürdürdü. Test ölçümü bir sonraki fixed step'e alındı. `Evidence/20261007T145533-608169Z-full-play/` son cockpit testi 1/1 PASS; exitCode=0, `changedSources=[]`. Bu dar sonuçlar tüm testlerin aynı koşuda geçtiğini kanıtlamaz.

Tam regresyon tekrarı: `Evidence/20261007T145716-990329Z-full-edit/` ana EditMode 524/524 ve Art Editor 10/10 PASS, exitCode=0. `Evidence/20261007T145751-440975Z-full-play/` PlayMode 279/280; yalnız `MouseAndCrouchActionsReachCameraAndCapsule` FAIL, `Release(keyboard.cKey)` sonrası `keyboard.cKey.isPressed=True`. Odaklı test daha önce PASS iken tam pakette bırakma olayı işlenmedi. Testte dört C press/release geçişi `InputSystem.QueueStateEvent(keyboard, new KeyboardState(...))` tam klavye durumlarıyla yazıldı. İki tam koşuda `changedSources=[]`; tarihsel kanıtlar korundu.

Son PlayMode tam tekrarı: `Evidence/20261007T151448-490448Z-full-play/` 279/280; aynı testte `KeyboardState()` bırakma olayı sonrası C tuşu hâlâ basılı. Bu, delta olayına özgü açıklamayı dışladı. Test fixture'ı sentetik tam klavye olaylarını kuyruğa aldıktan sonra `InputSystem.Update()` ile açıkça işliyor. Hızlı tekrar için `Tools/s015-regression.py --filter LastSignal.Tests.MovementAcceptanceTests` 17 test keşfini doğrular; fixture PASS olursa tam 280 PlayMode testi yine koşulmalıdır. Kullanıcının tercihine göre dört S15 vendor paketi Git LFS ile izlenecek; `.gitignore` yalnız S015 koşularının çoğaltılmış `generated-artifacts` dizinlerini dışlar, katalog kliplerini dışlamaz.

`Evidence/20261007T153635-069207Z-full-play/`: 17 testlik fixture 16/17, C bırakma assertion'ı aynı şekilde FAIL. Açık `InputSystem.Update()` tek başına çözmedi. Assertion artık olay güncellemesinin hemen ardından ve sonraki frame'de ayrı ayrı; yeni Unity koşusu bekleniyor.

`Evidence/20261007T154017-782779Z-full-play/`: 17 testlik fixture yine 16/17. `C immediately after release update` FAIL, `deviceAdded=True`, `updateMode=ProcessEventsInDynamicUpdate`; olay hemen işlenmedi. Unity Input System yerel kaynak kodundaki odak dışı Editor kuyruğu boşaltma yolu olası açıklama. Hareket test fixture'ı `Application.runInBackground=true` ve `BackgroundBehavior.IgnoreFocus` ile ayarlandı, Application değeri TearDown'da geri yüklenir. Son değişiklik Unity'de henüz koşulmadı.

`Evidence/20261007T154400-648607Z-full-play/`: 17 testlik fixture yine 16/17; aynı assertion'da `background=IgnoreFocus` ve `runInBackground=True`. Arka plan ayarı tek başına yeterli değil. Yerel Input System kaynak incelemesinde `InputManager.defaultUpdateType` odak/Play durumuna göre Editor update seçebiliyor, `InputTestFixture` ise UnityTest sırasında Editor update maskesini kapatıyor. Ayrı bir olasılık da eski zaman damgalı state olaylarının reddedilmesi. Test hata mesajı artık önce/sonra update ve event sayaçlarını, kuyruk ve cihaz zamanını içeriyor; sonraki fixture tekrarında doğru dal ayrıştırılacak.

S014 özetindeki 510 EditMode + 10 ArtEditor / 274 PlayMode karşısında mevcut keşif 524 / 10 / 280. Vehicle audio/noise sınırları tam PlayMode koşusunun 277 geçen testi içinde kapsandı; üç hata yukarıda kaydedildi.

## Açık işler

- Son full EditMode/Art Editor PASS 524/524 + 10/10; son iki full PlayMode koşusu FAIL 279/280. C bırakma olayına yönelik güncelleme/olay zamanı tanısı ve ardından full PlayMode tekrarı bekleniyor. Production build ve tüm insan kabulü NOT_RUN. Focused EditMode 14/14 ve PlayMode 6/6 PASS yalnız kendi kapsamları için geçerlidir.
- Temporary cue seçimleri, reload ses/görüntü hizası, yağmur masking, loop dikişleri, stereo/mono denge, tekrarlama ve clipping yalnız kullanıcı dinlemesiyle değerlendirilecek. Uygulama bu kaliteyi kanıtlamaz.
- Üç pakette LICENSE_EVIDENCE_REQUIRED. NOX yerel README CC0 beyanı var; edinim/yayın kanıtı ayrı.
- Measured CPU/GC/streaming memory ve maksimum crowd mix headroom henüz ölçülmedi. Kaynak sınırları performans PASS anlamına gelmez.

Kart matrisi `S015_ACCEPTANCE_MATRIX.md`, seçili klipler `S015_AUDIO_INVENTORY.md`, lisans `S015_ASSET_INVENTORY.md`, sonraki tek komut `S015_VERIFICATION_HANDOFF.md`. Ham source-only kanıt dizini `S015_EVIDENCE_POINTER.txt` içindedir.
