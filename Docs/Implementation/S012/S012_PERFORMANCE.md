# D118 kontrollü ölçüm kapsamı

LS-DOC-20 okundu. Belgede 1080p için p95 ≤16.67 ms, p99 ≤25 ms, 50 ms üzeri frame sayısı ve 4 GB küçük slice process hedefi **donanım onayına kadar geçici hipotez** olarak tanımlanıyor. Onaylı retail bütçesi olarak kullanılamaz. Bu görevin makinesi Mac15,7 / M3 Pro / 18 GB; Windows sonucu yok.

S012Performance opt-in development aracı üç ayrı taze oturumda aynı güneybatı açık şeritte gerçek FirstPersonMotor ile 60 saniye gerçek zamanlı ileri/geri hareket eder. Her varyant öncesi 5 saniye ısınma dışlanır. Clock snapshot fixture'ı gerçek WorldClock üzerinden uygulanır; daytime=12:00, night=23:00, rain=12:00+rain. Sabit weather aralığı örnek boyunca aynı koşulu tutar. Oyuncu konumu fixture; objective/inventory grant yok. Bu segment combat veya streaming içermez; o koşulları ölçmüş gibi gösterilmez.

Frame ortalama/p50/p95/p99, ham frame dizisi, mevcut ProfilerRecorder sayaçları, GC collection farkı, managed/Unity allocated/reserved bellek ve physical/logical nüfus kaydedilir. Desteklenmeyen counter UNAVAILABLE; 0 GPU süresi gerçek GPU ölçümü sayılmaz. Unity total allocation native-only/process RSS değildir.

Ayrı lifecycle bölümü 10 New Game ve her oturumda iki cell için beş request/ready/unload çifti (toplam 100 cell load/unload) uygular. Karşılaştırılabilir noktada UnloadUnusedAssets+GC sonrası bellek ve listener sayıları kaydedilir. Bu kısa stres testi iki saatlik soak veya 30–45 dakikalık yardımsız oynanış değildir. Sürekli uzun oturum bellek doğrulaması ayrıca gereklidir.

Ölçüm PASS yalnız veri yakalamanın tamamlandığını ifade eder; bütçe veya tüm D118 kabulü değildir. Beş tekrar ve uzun oturum hedefleri tamamlanmadan kapsam daraltılmaz. Gözlenen fark olmadan optimizasyon yapılmaz.

2026-10-03 ilk standalone ölçüm (`Evidence/20261003T102140Z-performance`) FAIL verdi. Snapshot zamanı doğrudan 12:00/23:00'a taşınırken bağlı ShelterProduction saati başlangıç zamanında kaldı; `Noncontiguous production clock` istisnası 307.020 kez loglandı. Bu nedenle day/night/rain kare ve GC sayıları geçersizdir; lifecycle çıktısı üretilmiş olsa da aynı hatalı süreçte toplandığından D118 kabulü değildir. Fixture artık bağlı katılımcıları `WorldSimulation.AdvanceUntil` ile hedef saate ilerletip snapshot'ı o anda alır ve örnek sırasında ilk hatada durur. Düzeltme sonrası ölçüm aşağıda ayrı kayıtlıdır.

2026-10-03 düzeltme sonrası `Evidence/20261003T103218Z-performance-retry` PASS capture completed, Errors=0. Mac15,7 / M3 Pro, macOS 15.5, Unity 6000.5.0f1 development build `20261003T102952Z`, 1280×800, VSync=0, uncapped frame rate. Aynı güneybatı açık şeritte 5 s ısınma hariç koşul başına 60 s gerçek zamanlı örnek:

| Koşul | Kare | Ortalama | p95 | p99 | 50 ms üzeri | GC koleksiyonu |
|---|---:|---:|---:|---:|---:|---:|
| Gün | 130301 | 0.461 ms | 0.516 ms | 0.570 ms | 0 | 178 |
| Gece | 129791 | 0.462 ms | 0.520 ms | 0.575 ms | 0 | 167 |
| Yağmur | 124931 | 0.480 ms | 0.536 ms | 0.591 ms | 0 | 162 |

Ekran görüntüleri üç görsel koşulu doğrular. GPU Frame Time kaydedildi; Render Thread, draw-call ve batch sayaçları UNAVAILABLE. Bu düşük kare süreleri yalnız 1280×800 açık şerit, 0 physical population ve streaming transition olmayan ölçüme aittir; 1080p combat/streaming veya retail bütçesi sonucu değildir. Her koşulda GC koleksiyonu devam ediyor; `GC Allocated In Frame` ortalaması yaklaşık 1.1 KB/kare. `lifecycle.csv` 10 oturum ve 100 cell request/ready/unload çifti tamamladı: managed used +110592 B, Unity allocated +114020 B, reserved 260931584 B sabit, ready/noise listener 1/1 sabit. Bu kısa koşu sızıntı yokluğunu veya iki saatlik soak'ı kanıtlamaz. Uzun oturum ve beş tekrar halen açık.
