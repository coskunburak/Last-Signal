---
doc_id: LS-PLAN-QA
version: 1.0.0
created: 2026-09-17
language: tr
status: DOCUMENT_VALIDATION
---

# Belge paketi kalite kontrolü

Bu rapor **yalnız Markdown planının yapısal doğrulamasıdır**. Unity derlemesi, oyun testi, oyuncu testi, Windows/macOS buildi veya yayın yapılmış demek değildir. Oyun uygulama durumu NOT_VERIFIED, oyun doğrulaması NOT_RUN'dır.

| Kontrol | Sonuç | Ölçüm |
|---|---|---|
| Faz sayısı | PASS | 20; P00–P19 |
| Sprint sayısı | PASS | 60; S001–S060 |
| Günlük kart sayısı | PASS | 600; D001–D600 |
| Sprint başına gün | PASS | Her birinde tam 10 |
| Günlük başlık benzersizliği | PASS | 600 benzersiz başlık |
| Günlük işlem benzersizliği | PASS | 600 benzersiz işlem metni |
| SP / co-op ayrımı | PASS | 480 SP, 120 DEFERRED co-op kartı |
| Co-op başlangıç koruması | PASS | S049–S060 G15+GO koşulu taşıyor |
| Bağımlılık grafiği | PASS | Bütün hedefler mevcut; her kenar daha önceki sprinte; döngü yok |
| Belge kimlikleri | PASS | Tekil doc_id |
| İç bağlantılar | PASS | 1547 içerik bağlantısı mevcut dosyaya çözülüyor |
| Kod/diagram fence dengesi | PASS | Açık kalan fence yok |
| Gereksinim eşlemesi | PASS | 82 v0.2 REQ referansı |
| Kapasite aritmetiği | PASS | 48×10=480; 12×10=120; SP 96+8=104 hafta |
| Koşullu toplam takvim | PASS | 120 üretim + 12 rezerv = 132 hafta |

## İçerik incelemesinde korunan kurallar

İlk save S002; ilk mikro loop S004; single-player tamamlanmadan co-op yok. GDD'deki üst içerik hedefleri otomatik kullanıcı kararı sayılmıyor. Yayın kartları somut aday ve yetkiye bağlı. Gerçek hedef platform testi ile Editor başarısı ayrılıyor. Reserve günleri 600 feature kartına eklenmiyor. Uzak dönem saatleri garanti değil, yeniden tahmin gerektiriyor.

Bu kontrol 600 işin gerçekten birer günde bitirilebileceğini, oyunun eğlenceli olacağını veya satış başarısını doğrulamaz. Büyük kartlar gerçek üretim hızına göre alt işlere ayrılmalıdır. Süre tahmininin doğrulanması S001 sonrası gerçek çalışma verisiyle başlar.
