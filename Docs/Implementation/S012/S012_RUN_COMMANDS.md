# S012 test ve build komutları — kullanıcı çalıştırması

Aşağıdaki komutları Terminal'de proje kökünde çalıştırın. İlk iki komut **mevcut açık Unity Editor** ve PlayMode kapalıyken çalışır. Her test yeni kanıt klasörü üretir ve eski sabit yollu kanıtları korur. Aynı anda iki komut başlatmayın. Sonuçta yazan `RUN=...` klasörünün `result.json`, `edit.xml` veya `play.xml` dosyasını ve Terminal çıktısını iletin. FAIL varsa `generated-artifacts/` içeriğini de koruyun.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/s012-verify.py edit
python3 Tools/s012-verify.py play
```

Daha kısa S012 kontrolü gerekirse tam suite yerine:

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/s012-verify.py edit --filter LastSignal.Tests.S012SceneTests
python3 Tools/s012-verify.py play --filter LastSignal.Tests.S012IntegrationTests
```

2026-10-03 PlayMode tekrarındaki dört hata için kullanılan hedefli komutlar aşağıdadır. Bu koşular 1/1 ve 15/15 PASS, ardından `20261003T100356-546593Z` tam PlayMode koşusu 204/204 PASS verdi. Yeni bir değişiklik veya hata olmadıkça tekrar gerekmez; sıradaki adım standalone build'dir.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/s012-verify.py play --filter LastSignal.Tests.AmmunitionIntegrationTests.ActualShotsMissWallEmptyAndRejectedRequests
python3 Tools/s012-verify.py play --filter LastSignal.Tests.ZombieDamagePlayTests
```

**Önce Editor'ü kapatın**; ardından macOS development build için yeni ve tekil log diziniyle aşağıdaki komutu kullanın. Aynı projeye ikinci Unity süreci açmayın. `Builds/S012/<UTC>/LastSignal.app` ve aynı klasörde `build.txt` oluşur. Komutun çıkış kodu, `build.txt` ve `editor-build.log` birlikte değerlendirilmelidir.

```bash
cd '/Users/burakcoskun/Last Signal'
s012_build_run="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "Docs/Implementation/S012/Evidence/$s012_build_run"
'/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode -projectPath "$PWD" -quit \
  -executeMethod LastSignal.Editor.S012Authoring.BuildCurrent \
  -logFile "$PWD/Docs/Implementation/S012/Evidence/$s012_build_run/editor-build.log"
```

Build PASS olursa uygulamanın Terminal'den açılmasıyla **fixture kullanan teknik standalone kabul** yapılabilir. `<build-run>` yerine build'in gerçek klasör adını, `<accept-run>` yerine yeni bir klasör adı koyun. `standalone.txt`, `player.log` ve tüm JSON/TXT dosyalarını iletin. Bu otomasyon yardımsız oyun süresi kanıtı değildir.

```bash
cd '/Users/burakcoskun/Last Signal'
s012_build='Builds/S012/<build-run>/LastSignal.app'
s012_accept='Docs/Implementation/S012/Evidence/<accept-run>'
mkdir -p "$s012_accept"
"$PWD/$s012_build/Contents/MacOS/Last Signal" \
  -s012Acceptance "$PWD/$s012_accept" -logFile "$PWD/$s012_accept/player.log"
```

Performans koşusu ayrı taze süreçte başlatılır; yaklaşık üç adet 60 s örnek ve 100 cell load/unload vardır. `result.txt`, üç koşulun `.txt`/`.csv` dosyaları, `lifecycle.csv`, `environment.txt` ve `player.log` gönderilir. Mac development ölçümleri Windows retail sonucu değildir. `20261003T102140Z-performance` saat fixture hatasıyla FAIL verdi; `20261003T101609Z` build'i düzeltmeden önce üretildi. `20261003T102952Z` yeni build ve `20261003T103218Z-performance-retry` ölçümü PASS capture completed verdi; yeni değişiklik olmadıkça tekrar gerekmez.

```bash
cd '/Users/burakcoskun/Last Signal'
s012_build='Builds/S012/<build-run>/LastSignal.app'
s012_perf='Docs/Implementation/S012/Evidence/<perf-run>'
mkdir -p "$s012_perf"
"$PWD/$s012_build/Contents/MacOS/Last Signal" \
  -s012Performance "$PWD/$s012_perf" -logFile "$PWD/$s012_perf/player.log"
```

Manuel kesintisiz oynanış için aynı uygulamayı otomasyon argümanı olmadan `open "Builds/S012/<build-run>/LastSignal.app"` ile başlatın; `S012_VERIFICATION_HANDOFF.md` checklist'ini doldurun. Mevcut kişisel checkpoint'i koruyun. Build sonucunu görmeden standalone/performance çalıştırmayın. Başarısızlıkta kaynak veya test assertion'larını değiştirmeyin; XML/log ve tekrar üretim adımlarını gönderin, düzeltmeyi bunlara göre yapacağım.
