# PERF-P04 — Platform benchmark kayıt formu

23 Ekim 2026. Hiçbir FPS/kare süresi veya performans kazancı ölçülmedi.

| Senaryo | macOS baseline | macOS optimized | Windows baseline | Windows optimized | Değişim |
|---|---|---|---|---|---|
| F1 forest idle | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | Pending |
| F3 dense forest | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | Pending |
| Canopy / camera rotation | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | Pending |
| Trail movement | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | Pending |
| Tree physics / harvesting (ayrı validation sahnesi) | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | Pending |
| Standalone Player | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED | Pending |

## Her run için

F9 `summary.json`: platform, CPU/GPU/API, Unity, RAM ve driver'ın bildirdiği graphics memory, quality **ve** pipeline asset adı, VSync/cap, çözünürlük/scaled dimensions, kamera pozları/FOV/AA, generation/digest/preset, scenario, P50/P95/P99, ortalama/median FPS, kullanılabilirlik ve eksik GPU sample sayısı. Apple unified memory değerini dedicated VRAM diye yorumlamayın.

`frames.csv`: wall-clock frame_ms, Main/Render Thread marker süreleri, mevcutsa gecikmeli FTM CPU/GPU ve timestamp, draws/batches/SetPass/triangle/vertex/used memory/GC allocated bytes, GC collection-count delta, scaled dimensions, FTM scale, focus. `gc0/1/2_delta` toplanıp tek toplam GC sayısı yapılmaz; nesillerin collection sayıları üst üste binebilir. GC pause sürelerini Timeline'dan alın.

Manuel ekleyin: run ID ve tekrar numarası; macOS/Windows ve OS sürümü; AC/battery, güç modu, termal durum; Windows gerçek VRAM/driver, Unity'nin RTX üzerinde çalıştığının kanıtı; Profiler bağlantısı/Deep Profile/Editor profiling durumu; Game view pixel dimensions; shader/import bitiş durumu; screenshot/Profiler dosya adları; render feature değişikliği; hareket rotası.

`frames.csv` formatı v2'de değişti. `summary.txt` P50/P95/P99/averageFPS kısa özeti korunur, tam metadata `summary.json` dosyasındadır. Eski CSV tüketicileri başlıkla okumalı; konumsal eski 7 kolon varsayımı kullanmamalı.

## Geçerli karşılaştırma

1. Aynı generation/digest, shot/FOV, preset, çözünürlük, API, quality/pipeline, VSync/cap ve Profiler bağlantı durumu.
2. Aynı kaydedici sürümüyle A ve B/C; eski kaydedici çıktısıyla yeni çıktıyı saf render optimizasyonu olarak karşılaştırmayın.
3. Shader/import tamamlandıktan sonra >=180 kare **ve** >=10 saniye warmup, ardından 30 saniye; koşul başına 3 tekrar.
4. `status=COMPLETED`, `configurationChanged=false`, focus korunmuş; buffer-limit/yarım/cancelled run kabul edilmez. `configurationChanged` yalnız quality index, pipeline referansı, cap/VSync ve output size değişimini yakalar; diğer manuel değişiklikleri sabit tutmak sahibinin sorumluluğudur.
5. FrameTimingManager aynı kareyi gecikmeli verir. CSV'de FTM ile Update frame numarasını aynı frame gibi eşleştirmeyin. Tekrar eden timestamp ve <=0 timing boş bırakılır. GPU sample yoksa `UNAVAILABLE`; sıfır maliyet veya CPU bottleneck sonucu çıkarılmaz.
6. Editor render counters başka Editor pencerelerinin maliyetini içerebilir. Envanter gerçek görünür renderer/çalışan shader variant sayısını ölçmez. Rendering Profiler ve Frame Debugger ayrı kanıttır.
7. Hafıza snapshot/Frame Debugger açıkken F9 benchmark koşmayın; inceleme kareleri ayrı alınır.

Frame budget hedefleri 30 FPS için 33,33 ms, 60 FPS için 16,67 ms. Kabul yalnız ortalama değil P95/P99, tekrarlanan spike, görsel kalite ve gameplay birlikte incelenerek verilir. Fark yüzdesi gerçek tekrarlar geldikten sonra hesaplanır.
