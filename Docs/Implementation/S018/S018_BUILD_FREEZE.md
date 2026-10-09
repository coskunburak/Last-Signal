# S018 D172 — aday dondurma sözleşmesi

> **2026-10-09 güncellemesi:** Canonical consume/treatment/çanta ekipmanı ve survival save extension eklendi; kaynak artık QA1 snapshot'ı değildir. QA1 immutable tarihsel artifact olarak korunur. Yeni aday QA2 RESERVED / NOT_BUILT / NOT_VERIFIED. Yeni regression, build, manifest/hash ve Windows kabulünden önce PT1 freeze açılamaz. Aşağıdaki QA1 manifest/digest yalnız geçmiş build'e aittir.


**NOT_FROZEN — dış oyuncu PT1 ayrılmıştır. S018-QA1-Win64 teknik build ve hashli paket hazır; gerçek Windows/human kabulü yok.** Bu dosyanın varlığı freeze, build PASS veya dış dağıtım izni değildir. Dış pilot girişi BLOCKED. QA1 hazırlanırken gameplay değiştirilmemişti; sonraki canonical uygulama değişikliği yukarıda kayıtlı.

## Bilinen ve tamamlanması gereken kimlik

| Alan | Şimdi bilinen / freeze öncesi gereken |
|---|---|
| Candidate | `S018-PT1` (RESERVED); platform artifact `S018-PT1-Win64` veya `S018-PT1-macOS` |
| Version | Round 6 build.txt: Version=`0.1.0`; kaynak bundleVersion ile aynı |
| Source | `7f5ffb783f396f9675c28da18be7021d90a02eb7`, branch `s012-integrated-graybox-slice`, dirty=true |
| Source identity | QA1 full source digest `22dab93e5cacabfe3e0ac0b791453e7e554767b261ca0172d42b83e5cb04e578`; 18.614 dosya before/after aynı. Entry audit digest ile karıştırılmaz. |
| Preservation | `Evidence/round6-vQGRlm/source-snapshot/` yerel clone; tam hash manifestleri `Evidence/20261008T104414-917756Z-round6-review/`. Kalıcı dış yedek henüz doğrulanmadı. |
| Unity | `6000.5.0f1 (88b47c5e7076)` |
| Packages | Input `1.19.0`, URP `17.5.0`, Test Framework `1.7.0`, Navigation `2.0.13`, uGUI `2.5.0`; manifest + lock SHA-256 [QA1 içerik kimliğinde](Evidence/20261008T104812-230928Z-production-readiness/content-identity.json) mevcut |
| Content | Scene SaveSession: `s012-graybox-v1`; world `world.s012.graybox`; ItemCatalog ve item/recipe/glyph/audio referansları tam source manifestine dahil |
| Save | Cell+population bileşiminde beklenen schema **4**, progressionVersion **1**; adayın gerçek save header'ı ile teyit NOT_RUN |
| Seed | Scene inspection `overrideSeed=true`, `explicitSeed=12345`. Gerçek oyuncu logu/save header eşitliği NOT_RUN; eski profilden load yeni oyun yerine geçmez. |
| Scene | `Assets/LastSignal/Scenes/Production/S013Cabin.unity`; genel EditorBuildSettings eski validation sahneleri içerir |
| Build options | Mevcut Mac/Windows builder **Development**; profiler bağlı mı ayrıca kaydedilir. Retail performansı diye sunulmaz. |
| Quality / resolution | İncelenen Editor PC; pilot başlangıcı PC, 1920×1080 windowed önerisi. Gerçek player değerleri dry-run'da teyit ve freeze edilir; final ayar onayı PENDING. |
| Input | Windows KBM zorunlu; fiziksel kabulden sonra belirli gamepad model/bağlantısı eklenebilir. Actions asset + profil/binding snapshot; tutorial reset; session settings değişiklikleri loglanır. |
| Target platform | Windows64 zorunlu P05 kabulü; Mac geliştirme/isteğe bağlı pilot. Aynı gameplay kaynağından platform artifactleri ayrı checksum taşır. |
| Artifact / checksum | S018-QA1-Win64; SHA-256 `62a69a653e52098201cd9d045104d7bc48b32fcb2b2fe77463cafcb31c26c448`; tam paket yolu güncel handoff içinde. Dış pilot onayı değil. |
| Known issues | Yeni consume/çanta-equip/treatment uygulamasının kabulü NOT_RUN; 306/306 PlayMode ve 10/10 Art Editor PASS; fiziksel input, Windows, quit/load ve insan ses/görsel/performance kabulü yok; S0/S1 güncel sayısı bilinmiyor |

ItemCatalog dosyası `Assets/LastSignal/Data/Items/Definitions/ItemCatalog.asset`; save yolu runtime `Application.persistentDataPath/saves/current.json`. Actual save header, seed, world/content/generation ve snapshot kimliği kaydedilir. Eski dünyadan gelen aynı ad nedeniyle yanlış slot yüklenmediği doğrulanır; kişisel save silinmez.

## Freeze sırası ve izin verilen değişiklikler

1. [Handoff](S018_VERIFICATION_HANDOFF.md) tek kapı sonucunu inceleyin; FAIL varsa yalnız ilgili düzeltme/tekrar. Yeni bug kaydı olmadan gameplay fix yok.
2. Canonical domain ve S012–S017 kabul açıklarını çözün veya owner sınırlı araştırma kapsamını açıkça kaydetsin. Dar pilot G5'te eksik sistemi PASS yapmaz. Yeni asset gerekmiyor.
3. Ön kaynak manifesti ve salt-okunur kaynak snapshot'ı oluşturun; mevcut production builder ile kullanıcı build alsın. Build sırasında kaynak değiştirmeyin. Builder dirty scene'i reddeder; kaydedilmemiş çalışmayı zorla atmayın.
4. Build raporu Succeeded, Errors=0; warnings sınıflansın. Son kaynak manifesti eşleşsin. S017 helper yalnız Assets/LastSignal+Packages+ProjectSettings ve kendisini hashler; tam vendor/tool kapsamı ayrıca gereklidir. Mac helper kendi başına kapsamlı source snapshot üretmez.
5. Kullanıcı gerçek standalone golden path, save/quit/restart/load ve Windows/input dry-run yapar. S0/S1 listesi değerlendirilir. Profil ve başlangıç save koşulları sabitlenir. Development ölçümü ile sonraki non-development ölçümü ayrılır.
6. Paketi benzersiz dizine arşivleyin; SHA-256 hesaplayın; artifact/build raporu/manifests/manual kayıtlar `Evidence/<run-id>/` altında bağlansın. Paket kimliği ve player ekranındaki kimlik yoksa yanındaki salt-okunur manifest birlikte dağıtılır.
7. Owner pilot için aday ID/platform/hash/known issues/kapsam/tarih imzasını kaydeder. Ancak bundan sonra durum FROZEN / PILOT_READY olabilir. Bu onay dış yayın yetkisi değildir; restricted ZIP/itch.io dağıtımı owner eylemidir.

T01–T08 arasında loot, balance, UI, tutorial, navigation, seed, enemy count ve item availability sabit. Kritik emergency rebuild **S018-PT2** veya sonraki ID olur; neden, değişen dosyalar, etkilenen testler ve eski/yeni hashler kaydedilir. Eski artifact silinmez. Platform farklılığı ile gameplay değişikliği ayrı tutulur; source değişirse platform sonekiyle gizlenmez. Aynı oyunun farklı ayarları kullanıcı rahatlığı için değişebilir; davranışı etkileyen confounder olarak işaretlenir.

## Daha sonraki kullanıcı build/standalone komutları — şu an çalıştırmayın

Kullanıcının son talebiyle tüm test yürütmesi ertelendi. [Handoff](S018_VERIFICATION_HANDOFF.md) son doğrulama aşamasını tanımlar; aşağıdaki komutlar şimdi çalıştırılmayacak. Aşağıdakiler bulunan gerçek altyapıya dayanan sonraki kapı tarifidir; bir öncekinin sonucu incelenmeden başlatılmaz.

Windows build: Editor kapalı; `python3 Tools/s017-verify.py windows-build`. Çıktı `Builds/S017/<run-id>/LastSignal.exe`, `build.txt`, `warnings.txt`, `s017-build-identity.json`, source manifests; kanıt `Docs/Implementation/S017/Evidence/<run-id>/`. Mevcut yöntemi koruyun. Bu Windows üzerinde çalıştırılmış olmak değildir.

Mac build: Editor açık, responsive, Play kapalı, sahne kaydedilmiş; `python3 Tools/s013-build.py production --timeout 1800`. Çıktı `Builds/S013/<run-id>-production/LastSignal.app`, `build.txt`, `warnings.txt`. Build pending/timeout olursa ikinci Editor veya ikinci build başlatmayın.

Mac teknik acceptance, build sonucu incelendikten sonra aşağıdaki gerçek komutla hazırlanır. Terminale önce başarılı build'in yazdırdığı **tam BUILD dizinini** girin; tarihsel veya en son dizini otomatik seçmeyin. Bu teknik fixture doğal insan oturumundan ayrıdır.

```sh
(
  cd '/Users/burakcoskun/Last Signal' || exit 1
  printf 'Onaylanan BUILD dizininin tam yolu: '
  IFS= read -r S018_BUILD
  S018_PARENT="$(mktemp -d "$PWD/Docs/Implementation/S018/Evidence/acceptance-XXXXXX")" || exit 1
  python3 Tools/s013-run.py acceptance "$S018_BUILD" --output "$S018_PARENT/standalone"
)
```

Çıktı `standalone/{command.json,launch.json,player.log,standalone.txt,process-result.json}`; wrapper başarılı process ve PASS sonucu, FAIL yokluğu ister. Log hata taraması ve state kabulü ayrıca değerlendirilir. Fixture yaklaşık saniyeler sürer; 30–45 dakikalık doğal rota veya gerçek insan comprehension değildir.

QA1 ZIP ve checksum hazır; yeni aday paketlendiğinde kullanılacak checksum komutu:

```sh
(
  printf 'Frozen ZIP dosyasının tam yolu: '
  IFS= read -r S018_ZIP
  test -f "$S018_ZIP" || exit 1
  shasum -a 256 "$S018_ZIP"
)
```

Çıkan hash/path freeze tablosuna ve her session'a yazılır. Upload/publish komutu yok.

## Gerçek Windows ve quit/load kabul formu

Windows sürüm/build, CPU/GPU/RAM, çözünürlük, quality/FOV, input model/bağlantı, ses çıkışı, candidate/hash, kaynak kimliği, profiler durumu, başlangıç/bitiş ve player.log kaydedilir. Menü açılışı → yeni oyun → movement/look → inventory/split/transfer → combat → relay/return/upgrade → save → uygulamayı tamamen kapat → aynı executable'ı yeniden aç → load → core progression devamı. Beklenen/gerçek durum ve kanıt her adımda ayrı yazılır.

Kaydetmeden önce mission aşaması/reward receipt, item/ammo sayıları, shelter/storage/craft, door/loot ve player konumu not edilir. Restart sonrası state kaybı, duplication, kilitlenme olmadan devam aranır; yalnız pause/menu dönüşü quit/load değildir. Save path yazılabilirlik, kullanıcı adında farklı karakterler, shader/pink materyal, font/render, native plugin ve input farkları ayrıca kaydedilir. Kişisel save üzerinde fault denemesi yapılmaz.

KBM↔gamepad, disconnect/reconnect, held input, alt-tab/focus ve menu return durumlarında yanlış ateş/hareket, stuck pause veya focus kaybı ayrı bulgulardır. Cihaz desteği yalnız denenmiş model/OS için kabul edilir. Windows makinesi yoksa runtime BLOCKED, henüz koşulmadıysa NOT_RUN. Mevcut performance workflow `Tools/s013-run.py performance` ve S014 population araçlarıdır; yalnız ölçülmüş ihtiyaç sonrası ayrı kullanıcı turu olarak seçilir.


## 2026-10-08 — teknik testler sonrası

Kullanıcı Round 3 306/306 PlayMode ve Round 4 10/10 Art Editor PASS; exit=0 ve kaynak korunmuş. Önceki 541/541 EditMode ilgili kaynak kapsamı aynı. Bu tarihsel aşamanın ardından Round 5/6 ile Windows build alındı; kaynak clone snapshot'ı korundu. Tam manifest/snapshot karşılaştırması, paket checksum'u, gerçek Windows/quit-load ve human kapsam açıkları kapanmadan bu belge NOT_FROZEN kalır.


Round 5: Windows64 Development artifact üretildi (Succeeded, Errors=0, 538 warning) fakat build sırasında ProjectSettings analytics define değişti. `Builds/S017/20261008T102605-710162Z-windows-build` korunur, NOT_FROZEN. Round 6 build-sonrası mevcut kaynakla immutable tekrar; hiçbir mutasyon istisnası verilmedi.


Round 6 build PASS: Succeeded, Errors=0, 495 warning, exit=0, changed=[]. Tam kaynak snapshot manifesti 18.614 dosyada aynı. S018-QA1-Win64 owner QA ZIP oluşturuldu, dışarı yüklenmedi. Gerçek Windows erişimi owner tarafından bildirildi. Round 7 smoke/process quit-load NOT_RUN; kapsam kapanmadan PT1 frozen ilan edilmez.


## Windows beklerken tamamlanan kimlik incelemesi

[QA1 içerik kimliği](Evidence/20261008T104812-230928Z-production-readiness/content-identity.json) doğrudan build öncesi korunmuş snapshot'tan çıkarıldı. Scene, katalog, input actions, glyph/audio catalog, manifest/lock ve ProjectSettings hashleri kaydedildi. ItemCatalog içindeki **9 referansın 9'u** yerel meta GUID → asset yoluyla çözümlendi; bu, oyunun tüm item sayısı veya 60–80 polished-item hedefinin karşılandığı iddiası değildir.

S013Cabin üzerinde WorldCellManager, WorldPopulationManager ve RelayMission bulundu. SaveSession seçim mantığına göre schema=4/progressionVersion=1 bekleniyor; header, gerçek kayıt alındığında ayrıca okunacak. Authored overrideSeed=true/12345; quality/resolution gerçek player'da teyit edilecek. Paket ve runtime değiştirilmedi; SHA-256 aynı artifact'i tanımlar.

Owner en son tüm uygulanabilir hazırlık bitene kadar test yürütmesini erteledi. Erişim mevcut; durum NOT_RUN, erişim BLOCKED değil. Kendiliğinden test veya zamanlanmış görev başlatılmaz. Sonraki owner onaylı kaynak değişikliği nedeniyle QA2 build gereklidir; güncel kuyruk handoff başındadır.
