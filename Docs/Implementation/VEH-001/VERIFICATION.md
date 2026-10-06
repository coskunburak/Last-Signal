# Kullanıcıyla doğrulama döngüsü

Unity'de Last Signal projesi açık, Play kapalı, derleme bitmiş olmalı. Aynı anda ikinci Unity veya başka test/build koşusu başlatılmamalı. Test sırasında Docs/Implementation altında düzenleme yapılmamalı: mevcut S012 wrapper tarihsel kanıtları yedekleyip geri koyar.

İlk kapı:

```sh
cd "/Users/burakcoskun/Last Signal"
python3 Tools/veh001-verify.py focused
```

Çıktının tamamını paylaş. Betik ilk hatada durur. EVIDENCE satırı kayıt yerini gösterir. Beklenen alt sınıflar: Foundation, Noise, Exit, Save, Integration, Occupancy. Her XML'de pozitif test sayısı aranır.

Focused sonuçları incelendikten sonra ayrı sırayla:

```sh
python3 Tools/veh001-verify.py edit
python3 Tools/veh001-verify.py play
python3 Tools/veh001-verify.py build
```

Bunlar peş peşe körlemesine çalıştırılmamalı; her kapı sonucu incelenip sonraki komut verilir. Full PlayMode baseline süresi yaklaşık650s idi; defaulttimeout1800s. Timeout olursa Unity hâlâ çalışıyor olabilir; yeni koşu başlatma, çıktıyı paylaş.

Uyarı farkını sınıflandırmak için temiz Development build (yalnız ayrı komut verildiğinde):

```sh
python3 Tools/veh001-verify.py build --clean-cache
```

Bu yeni immutable output'a ve `warnings.txt` kanıtına yazar. Unity açık, Play kapalı olmalı. Sırf 0 hata ile uyarı regresyonu kapatılmaz.

Production manuel rota: S013Cabin aç → Play → kabinden(-340,-125) pickup'a yaklaş → E bin → I motor → W/S gaz/servis freni → R açık geri niyeti → Space el freni → H korna → L farlar → dur → E çık. Yakıt noktası sol arkada, bagaj arkada; ikisi aynı etkileşim ışınını kullanır. Gamepad eşlemeleri cockpit HUD'da görünür. Ayakta ve araç içinde save/load, pause/focus, dolu depo, engelli çıkış ve bagaj transferi ayrıca gözlenmeli.

Yeni performans capture kaynakları derlenip yeni production Development build geçtikten sonra, **o build'in** `BUILD=` klasörüyle ayrı komut:

```sh
python3 Tools/veh001-performance.py "Builds/S013/<yeni-BUILD-klasörü>"
```

Capture üretim sahnesinde 50 bin/in, 50 oturum spawn/end, 5 saniye ısınma ve 20 saniye sürüş yapar. `result.txt`, `drive.txt`, `lifecycle.csv`, `sessions.csv` ve varsa `drive.png` yeni VEH evidence klasörüne yazılır. Bu otomatik sürüş/oturum koşusu gamepad, görsel kalite, sürüş hissi, eğim/gravel ve cell portal geçişinin insan kabulü yerine geçmez. Performans `PASS capture completed` ifadesi yalnız ölçümün tamamlandığını gösterir; bütçe kabulü ayrıca veriler incelendikten sonra kararlaştırılır.

Manuel kabul NOT_RUN: görsel bütünlük, gece/rain, ses kalitesi, handling, eğim/gravel, gamepad UI konforu. Performance NOT_RUN: fizik/script CPU, GC, bellek ve soak; ölçüm olmadan hedef PASS verilmez.
