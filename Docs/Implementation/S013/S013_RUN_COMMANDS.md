# S013 tekrar üretim komutları

Proje kökü `/Users/burakcoskun/Last Signal`. Aynı proje üzerinde ikinci Unity Editor açılmaz. Mevcut Editor açık, Play kapalı, derleme bitmiş ve sahne kaydedilmiş olmalı. `Tools/s013-build.py` mevcut Editor'a dosya isteği gönderir; ikinci Editor açmaz. Her build yeni dizin oluşturur ve terminale `BUILD=...` yazar. `production` S013Cabin, `baseline` korunmuş IntegratedGraybox kullanır. Yöntem mevcut dizini ezmeyi reddeder. `Author()` mevcut üretim varlıklarını yeniden oluşturmak için çağrılmaz.

```sh
cd "/Users/burakcoskun/Last Signal"
python3 Tools/s013-build.py production --timeout 1800
python3 Tools/s013-build.py baseline --timeout 1800
```

İki build'i aynı anda başlatmayın. İlk komut `PASS` bitince ikincisini çalıştırın. Eski `20261004T203200Z-production` build'i son HUD/performance değişikliklerini içermez.

Başarılı build.txt olmadan koşu başlamaz:

```sh
python3 Tools/s013-run.py acceptance "<BUILD-production>"
python3 Tools/s013-run.py performance "<BUILD-production>"
python3 Tools/s013-run.py performance "<BUILD-baseline>"
python3 Tools/s013-run.py performance "<BUILD-production>" --tier Low --quick
python3 Tools/s013-run.py performance "<BUILD-production>" --tier Medium --quick
```

Her koşu yeni evidence dizini açar, gerçek executable adını Info.plist'ten okur;1920×1080 windowed komutunu, build raporunu, process exit code ve logları kaydeder. `environment.txt` gerçek render çözünürlüğünü doğrulamak için ayrıca incelenir. Kabul otomasyonu kendi explicit save yollarını kullanır. Manuel oyunda kişisel save dosyasını koruyun. Performans sırasında Editor test/build ve başka ağır işler eşzamanlı çalıştırılmaz; arka plan yükü kaydedilir.

Tam regresyon mevcut Editor köprüsüyle:

```sh
python3 Tools/s012-verify.py edit
python3 Tools/s012-verify.py play
```

Wrapper tarihsel artifactleri yedekler ve geri koyar. Koşu sürerken Docs altında yeni belge/kanıt üretmeyin; bunlar test çıktısı sanılıp taşınabilir. Yeni Art.EditMode assembly ayrıca TestRunnerApi assemblyNames `LastSignal.Art.EditorTests` ile çalıştırılır; sonuç için `LastSignal.RegressionPath` yeni XML yolu kullanılır. S013IntegrationTests mevcut PlayMode assembly içindedir.
