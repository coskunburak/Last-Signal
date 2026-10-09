# S015 — İlk manuel doğrulama devri

## Güncel durum

**S015 = PARTIAL.** Focused EditMode Burak koşusuyla **14/14 PASS**, focused PlayMode **6/6 PASS**. Son tam EditMode **524/524 PASS**, Art Editor **10/10 PASS**. Son iki tam PlayMode koşusu **279/280 FAIL**; 17 testlik fixture tekrarları da **16/17 FAIL**. Son fixture koşusunda C bırakma olayı `InputSystem.Update()` çağrısından hemen sonra da işlenmedi. Güncellemenin çalışıp çalışmadığını ve olay zamanını ayrıştıran tanı tekrarı bekleniyor. Build ve işitsel kabul **NOT_RUN**. Test/build/gameplay kabulü Burak'a aittir.

## Focused EditMode — PASS

İlk komut açık Unity Editor kilidinde durmuş; test çalışmamış. Burak Editor'ı kapatıp ikinci komutu çalıştırdı. `Evidence/20261007T141921-948030Z-focused-edit/`: XML `total=14`, `passed=14`, `failed=0`, `skipped=0`, `result=Passed`; process exitCode=0; `source-preservation.json` `changed=[]`. İlk koruma mesajı test FAIL değildir.

## Focused PlayMode — PASS

Burak'ın `Evidence/20261007T142322-844665Z-focused-play/` koşusu: XML `total=6`, `passed=6`, `failed=0`, `skipped=0`, `result=Passed`; process exitCode=0; `source-preservation.json` `changed=[]`.

## İlk tam regresyonun sonucu

`Evidence/20261007T142820-974603Z-full-edit/`: ana EditMode 524/524 PASS; Art Editor 3/10, yedi `NON_URP_SHADER` hatası. `Evidence/20261007T142854-642143Z-full-play/`: PlayMode 277/280; bir eğilme testi ve iki araç testi hata verdi. İki koşuda kaynak değişimi yok; tarihsel kanıtlar geri kondu. `Evidence/20261007T144912-115720Z-art-editor/`: düzeltme sonrası Art Editor 10/10 PASS. `Evidence/20261007T144933-690211Z-full-play/`: ilk eğilme dar tekrarı FAIL; ikinci C isteği girdi okuyucusuna ulaşmadı. C bırakma doğrulaması ve bir frame bekleme eklenince `Evidence/20261007T145209-287837Z-full-play/` 1/1 PASS. `Evidence/20261007T145238-448041Z-full-play/`: tekerlek sunumu 1/1 PASS; cockpit kamera ofseti 0 ölçüldüğü için ikinci test FAIL. Kamera `Update` içinde ofseti çıkarıp `LateUpdate` içinde uygular; ölçüm `LateUpdate` sonrası fixed step'e alınca `Evidence/20261007T145533-608169Z-full-play/` 1/1 PASS. Dar koşular tüm regresyonun aynı kaynak durumunda birlikte geçtiğini kanıtlamaz.

`Evidence/20261007T145716-990329Z-full-edit/`: ana EditMode 524/524, Art Editor 10/10 PASS. `Evidence/20261007T145751-440975Z-full-play/`: PlayMode 279/280; tek hata `MouseAndCrouchActionsReachCameraAndCapsule`, `Release(keyboard.cKey)` sonrasında C tuşu hâlâ basılı. Test fixture'ı delta key olayları yerine tam `KeyboardState` olayları kullanacak şekilde değiştirildi. İki tam koşuda `changedSources=[]` ve tarihsel kanıtlar korundu.

`Evidence/20261007T151448-490448Z-full-play/`: PlayMode yine 279/280; aynı testte tam `KeyboardState()` bırakma olayına rağmen C tuşu basılı kaldı. Bu sonuç sorunun yalnız delta tuş olayına özgü olmadığını gösterir. Test, her sentetik klavye olayı sonrası `InputSystem.Update()` çağıracak biçimde güncellendi. Kullanıcının tercihine göre S15 ses paketleri Git LFS ile izlenir. `.gitignore` yalnız S015 koşularındaki çoğaltılmış `generated-artifacts` dizinlerini dışlar.

`Evidence/20261007T153635-069207Z-full-play/`: 17 testlik fixture 16/17; C tuşu bırakma assertion'ı yine FAIL. `InputSystem.Update()` çağrısı tek başına çözmedi. Assertion artık hemen güncelleme sonrası ve sonraki frame için ayrı ayrı; sonraki dar koşu hangisinde durumun bozulduğunu gösterecek.

`Evidence/20261007T154017-782779Z-full-play/`: fixture yine 16/17; `C immediately after release update` FAIL, `deviceAdded=True`, `updateMode=ProcessEventsInDynamicUpdate`. Bırakma olayı doğrudan input güncellemesinde işlenmemiş. Unity Input System kaynak kodu, odak dışı Editor durumunda olay kuyruğunu boşaltabilir. Fixture `Application.runInBackground=true` ve `BackgroundBehavior.IgnoreFocus` kullanacak şekilde ayarlandı, önceki Application değeri TearDown'da geri yükleniyor. Bu değişiklik henüz koşulmadı.

`Evidence/20261007T154400-648607Z-full-play/`: fixture yine 16/17; aynı assertion, `background=IgnoreFocus`, `runInBackground=True`. Bu ayarlar tek başına çözüm değil. Unity Input System paketindeki `InputManager.defaultUpdateType` odak/Play durumuna göre Editor güncellemesini seçebilir; `InputTestFixture` UnityTest'te Editor güncellemelerini devre dışı bırakır. Ayrıca cihazdan eski zaman damgalı olaylar atılır. Test assertion'ı şimdi güncelleme/olay sayaçlarını ve kuyruk/cihaz zamanlarını yazıyor; bir sonraki koşu bu olasılıkları ayıracak.

## Şimdi 17 testlik fixture tekrarı

Unity Editor kapalı olsun. Son kod değişikliği yalnız PlayMode test fixture'ındaki tanı mesajıdır. `MovementAcceptanceTests` sınıfının 17 testini aynı Unity oturumunda çalıştır; önceki tek-test koşusu tam pakette görülen sıralama etkisini yakalamadı. Araç eski kanıt yollarını korur ve 17 testin tamamını şart koşar.

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/s015-regression.py full-play --filter LastSignal.Tests.MovementAcceptanceTests
```

Bu fixture PASS olursa aynı komutun filtresiz `full-play` sürümü gerekir. Fixture tek başına tam regresyon PASS anlamına gelmez.

Gerçek executable: `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`. Tam koşuda filtre yok. Ana EditMode `LastSignal.EditModeTests`, Art Editor `LastSignal.Art.EditorTests`, PlayMode `LastSignal.PlayModeTests`. İlk tam koşudaki keşif sayıları **524 / 10 / 280**; tekrar koşusunda düşüş araştırılır.

Yalnız terminal özeti ve `RUN=...` yolunu gönder. Hata varsa failing testcase, failure message ve stack trace yeterli; tam Unity log'u gerekmiyor. XML yoksa `unity.log` içindeki compile/exception/error çevresi istenir. Wrapper komutunu yalnız Burak çalıştırır.

## Kapı sırası

1. Focused EditMode: PASS, 14/14.
2. Focused PlayMode: PASS, 6/6.
3. Son tam EditMode ve Art Editor PASS; PlayMode iki kez 279/280. 17 testlik fixture 16/17; güncelleme/olay zamanı tanısı ve ardından tam PlayMode bekleniyor.
4. Full regression PASS ise mevcut S013 production build komutu verilecek.
5. Build Succeeded + C# Errors 0 ve warning sınıflandırması sonrası aynı S013Cabin rotasında kulaklık, hoparlör, muted ve ses ayarları kabul rotası verilecek.

Bu belge sonraki kapıları çalıştırma isteği değildir. Uygulama testlerinden önce production build veya manuel rota istenmiyor.

## Kanıt

Her kullanıcı regresyon koşusu `Docs/Implementation/S015/Evidence/<unique UTC>-full-edit/` veya `-full-play/` içinde stage bazlı command/XML/log/result, kaynak hash ve historical preservation dosyaları üretir. Mevcut implementation-only kayıt `S015_EVIDENCE_POINTER.txt` içindedir.

## Sonuç kaydı

- Focused EditMode: PASS — 14/14; kullanıcı XML ve exitCode kanıtı yukarıda.
- Focused PlayMode: PASS — 6/6; kullanıcı XML ve exitCode kanıtı yukarıda.
- Son full EditMode: PASS 524/524; Art Editor: PASS 10/10. Son iki full PlayMode: FAIL 279/280; son 17 testlik fixture FAIL 16/17. Güncelleme/olay zamanı tanısı NOT_RUN.
- Production build: NOT_RUN.
- Headphone / speaker / muted / settings / performance: NOT_RUN.
- S014 durumu: PARTIAL; insan görsel kabul ve diğer açıkları değiştirilmedi.
- Üç vendor paketinde LICENSE_EVIDENCE_REQUIRED; Temporary cue'ların işitsel uygunluğu açık.
