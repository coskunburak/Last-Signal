# S014 D139 — 2026-10-07 üretim prefabı ölçümü

Durum: **PARTIAL**. İstenen PC/Low 1/10/20 × 5 matrisinin 30 ölçümü tamamlandı ve geçici frame budget her tekrarda karşılandı. Tam D139 kapanışı için standalone teardown/residue telemetrisi ile kritik tick allocation/Animator-skinning ayrıştırması açık. Dedicated lifecycle testleri 3/3 PASS; bunlar her standalone tekrarın ayrı residue kaydı yerine geçmez.

## Kaynak ve yöntem

Dal `s012-integrated-graybox-slice`, HEAD `97516c8b20793f4ca834e214318bdac45f041a4d`, dirty çalışma ağacı. Unity 6000.5.0f1. Ölçüm build: `Builds/S013/20261007T090101-351030Z-production`; build.txt Succeeded, 0 error, 55 warning. Kaynak manifesti focused 3/3 koşusuyla eşleşiyor. Bu final regresyon sonrası final build değildir.

Apple M3 Pro, 12 CPU, 18432 MB RAM, Metal; macOS 15.5; 1920×1080, FOV75, VSync0, targetFrameRate=-1. Üretim S013Cabin resident alanında sabit hedef; production LS_Zombie_Runtime. Her tekrar: disposable session, 2s preroll, 5s warmup, 20s measurement. İlk tekrarın 2s preroll bölümünde binary CPU profile; ölçüm penceresinde binary profiling kapalı. PlayerHealth aktif; yalnız fixture HealthChanged olayı üzerinden mevcut RecoverHealth ile hedef canlı tutulur. Bu tıbbi gameplay özelliği değildir.

PC mevcut renderer; Low mevcut S013 aday ayarı (shadowDistance25, cascade1, MSAA1). Yalnız URP kopyası değişir; sağlık/hasar/range/timing/navigation/hitbox/loot/target alanlarına kalite dalı yazmaz. Gameplay tanımları aynı prefab ve buildden gelir; temas sayılarının eşitliği deterministik gameplay kanıtı olarak sunulmaz.

LS-DOC-20: p95 ≤16,67 ms, p99 ≤25 ms; >50 ms ayrıca. Mac Development fixture sonucu Windows/retail veya tüm POI minimum donanım kabulü değildir.

## Ölçümler

Avg beş run ortalamasının ortalaması; p95/p99 beş tekrarın en kötüsü. Temas sayıları warmup dahil fixture ömrü toplamıdır; yalnız ölçüm penceresi farkı değildir. GC0 Mono sayacı; GC1/2 aynı olayları yansıtabileceğinden toplanmaz.

| Kalite | Zombi | Tamamlanan | Avg ms | En kötü p95 | En kötü p99 | >50ms | GC0 toplam | Temas | Frame hedefi |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| PC | 1 | 5/5 | 1.4200 | 1.9330 | 2.2372 | 0 | 222 | 50 | PASS |
| PC | 10 | 5/5 | 1.7983 | 2.3170 | 2.7355 | 0 | 173 | 452 | PASS |
| PC | 20 | 5/5 | 1.8172 | 2.1040 | 2.3169 | 0 | 166 | 757 | PASS |
| Low | 1 | 5/5 | 1.3965 | 1.9400 | 2.0876 | 0 | 221 | 50 | PASS |
| Low | 10 | 5/5 | 1.7965 | 2.3407 | 2.5259 | 0 | 171 | 452 | PASS |
| Low | 20 | 5/5 | 1.8480 | 2.3823 | 2.8264 | 0 | 161 | 734 | PASS |

## Alt sistem ve bellek

GPU Frame Time Metal standalone sayacı pozitif veri sundu; CPU frame süresinden türetilmedi. Ham nanosecond ortalamaları counters.txt içinde. GPU sonuçları bu tool yolunun raporladığı değerlerdir; bağımsız GPU timeline incelemesi yok. Render Thread, Draw Calls Count, Batches Count, Animator.Update ve Skinning.Update bu isimlerle UNAVAILABLE. CPU raw dosyaları saklı.

AI marker ortalamaları PC için yaklaşık 0,041 / 0,305 / 0,536 ms; 2ms AI+perception hipotezinin altında. Whole-frame GC allocation yaklaşık 2,4–2,6 KB/kare ve çok sayıda GC mevcut; bunun tümünü AI ticklerine atfetmek doğru değildir. Kritik tick 0B hedefi için root-cause ayrıştırması açık.

Unity allocated memory yaklaşık 191–201 MB, managed yaklaşık 4,8–5,8 MB aralığında. Aynı sayının tekrarlarında monoton büyüme görülmüyor; kısa pencerede sınırlı dalgalanma. Tekrarlanan oturumlarda gerçek leak yok iddiası verilmez. Player kapanış loglarında Unity native MemoryLeaks/erken thread finalize kayıtları bulunuyor; nedeni lifecycle olarak kanıtlanmadığından gözlem olarak korunur, sessizce yok sayılmaz.

## Geçerlilik sınırları

Her tekrar istenen aktif aktör sayısını, canlı/hearing durumunu, hatasız warmup/sample ve pozitif gerçek temas sayısını doğruladı. Start öncesi collider/NavMesh doğrulanır. ClearActors her geçişte Shutdown/disable/Destroy çağırır; dedicated 1/10/20 testleri actor/listener residue ve idempotent teardown doğrular. Standalone CSV teardown sonrasını ayrıca ölçmez: bu nedenle 30 completed measurement, 30 tam lifecycle-certified acceptance olarak adlandırılmaz.

Sabit hedef çevresindeki crowd ve üretim culling korunur; 20 active actor, 20 tam görünür mesh demek değildir. Avatar-only dummy yok. Aynı seed garanti edilmez; session population ve scheduler identity varyansı kayıtlardadır. NavMesh paths/contact varyansı ham CSVde gösterilir. Son Low tekrarı sırasında kısa Editor durum sorgusu yapılmıştır; büyük spike yok, bu dış etkinlik gizlenmez.

## Kanıt

- PC: `Evidence/20261007T090154-796032Z-population-pc/`
- Low: `Evidence/20261007T091601-579186Z-population-low/`
- Özet: `Evidence/20261007T092418-434573Z-population-summary/aggregate-run-means.json`
- Invalid fixture: `Evidence/20261007T085519-615708Z-population-pc/INVALIDATED.txt` (sağlık disabled → temas0; kabul değil)
- Düzeltilmiş focused: `../S012/Evidence/20261007T085925-363070Z/play.xml` (3/3 PASS)
- Analiz: `Tools/s014-population-summary.py` ham frame sayısını, 15 satırı, aktif sayıyı ve temas koşulunu kontrol eder.

Her koşuda command.json, build identity/report, source-status, environment, summary, 15 frame CSV/counter/screenshot, üç cpu.raw, Player.log ve result.txt korunur. Tarihsel kanıtlar değiştirilmedi.
