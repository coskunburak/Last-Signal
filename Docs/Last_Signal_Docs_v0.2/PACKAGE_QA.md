# Dokümantasyon kalite kontrolü — v0.2.0

Tarih: 17 Eylül 2026. Kontrol kapsamı yalnız bu Markdown paketidir.

## Kontroller

- 29 kaynak dosyada UTF-8, benzersiz belge ID ve sürüm metadata'sı: PASS.
- 82 açık gereksinimde benzersiz ID ve matriste kapsama: PASS.
- 72 item ID benzersiz; 12 dar prototype adayı: PASS.
- 16 recipe adayı ve 24 sıralı W iş paketinin dependency referansları: PASS.
- Kaynak GDD gövde paragrafları ve tablo hücrelerinin Markdown arşivde bulunması: PASS.
- Göreli dosya bağlantılarının paket içinde çözülmesi: PASS.
- Markdown fenced code bloklarının kapanması ve tablo sütun sayıları: PASS.
- Kaynak dosyalarından master birleşik kopya üretimi: PASS.
- Denge örneklerinde dünya zamanı oranı, stamina süreleri ve item sayıları: kontrol edildi.
- Tarihî GDD ile v0.2 önerileri arasındaki farklılıklar: karar kaydında açıklandı.
- Dosya SHA-256 manifesti ve ZIP CRC kontrolü: PASS.

## Oyun ve bağlantı durumu

Unity compile/EditMode/PlayMode: NOT_RUN. Windows/macOS oyun buildi: NOT_RUN. Gerçek oynanış ve performans: NOT_RUN. NotebookLM yüklemesi: NOT_DONE. Unity MCP / NotebookLM MCP: NOT_CONFIGURED. Bu paketin test sonuçları çalışan oyun veya kurulmuş entegrasyon kanıtı değildir.

## Bilinen sınırlar

Sayısal gameplay değerleri başlangıç hipotezi. Hedef cihaz RAM'i ve repo manifesti incelenmedi. MCP sağlayıcısı seçilmediği için bağlantı komutlarında açık placeholder biçimi var; bunlar çalıştırılacak hazır endpoint değildir. Şablonların “doldurulacak” alanları bilinçlidir. GDD arşivi tarihseldir; NotebookLM'ye güncel kaynaklarla birlikte filtresiz yüklenmemelidir.
