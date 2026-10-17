# S023 performans hazırlığı

Tüm sonuçlar **NOT_MEASURED**. Capture altyapısı IMPLEMENTED, compilation/runtime NOT_RUN. 1080p60 / 16,67 ms hedef bir hipotezdir; donanım garantisi veya ölçülmüş iyileşme değildir.

## Kullanıcı capture protokolü

S023 pilot SessionFlow'da S023Capture bulunur. Editor veya Development build'de, yalnız `s023-pilot` save identity'siyle etkinleşir. Capture kendi başına PlayMode, scene transition, combat veya save başlatmaz.

1. Handoff focused/full/build kontrolleri tamamlandıktan sonra aynı generated sahne ve aynı build hashleriyle karşılaştırın. Runner HEAD/status/source hashlerini capture klasörüne referanslayın. `sourceRevision/sourceDirty` alanları **authoring anını** kaydeder; sonradan değişiklik olduysa runner'ın güncel build identity'si asıl kanıttır.
2. Seed 23023, compositionSeed 23023; resolution 1920×1080, aynı quality, FOV, VSync/frame cap, fullscreen ve cihaz. OS/CPU/GPU/RAM/reported VRAM otomatik kaydedilir; GPU core/SSD/thermal/power state ve macOS unified-memory yorumunu operatör notuna ekleyin. Reported VRAM gerçek ayrılmış residency ölçümü değildir.
3. Her senaryo yeni disposable session ile başlar. `Alt+F12` gündüz açık, `Ctrl+F12` gece açık, `Shift+F12` gündüz yağmur koşulunu mevcut WorldClock üzerinden kurar. Time advance fail olursa hatayı inceleyin; fallback yok. Capture sırasında koşul değiştirme kapalıdır. Aktif threat, hızlandırılmış time advance'i durdurabilir; önce güvenli resident'te koşulu ayarlayın.
4. Inspector'daki `scenario` alanını ve `profilerOverhead` notunu doldurun. Standalone'da `-s023Scenario depot-day` etiketi launch argümanı kullanılabilir. Etiket senaryoyu otomatik kurmaz. Profiler attached/deep profiling/frame debugger/capture overlay bilgisi zorunlu nottur.
5. Sabit başlangıç/yaw ve tarifli rota ile **F12**: 5 saniye ısınma + 60 saniye örnekleme. Erken bitirmek için F12. Ölçüm sırasında screenshot almayın. En az beş tekrar; warm ve cold loading ayrı etiketlenir.
6. Çıktı Unity Console'da `S023 capture written: <absolute path>` olarak görünür. `Application.persistentDataPath/S023Captures/<UTC>-<GUID>/capture.json`, frames.csv ve sayaç ham CSV'leri. Yalnız capture dosyaları oluşturulur; save slotu değişmez. İlgili klasörü `Docs/Implementation/S023/Evidence/<run>/captures/` altına kullanıcı kopyalar.

## Senaryolar ve matched karşılaştırma

| Senaryo | Operatör rotası / koşul | Durum |
|---|---|---|
| Empty baseline | Aynı renderer/settings ile ayrı boş benchmark; Unity Profiler ile kayıt, gameplay kıyasına karıştırma | NOT_MEASURED |
| Resident baseline | S013 sığınak/approach: sabit nokta ardından 30 m gidiş/dönüş; seed 23023 | NOT_MEASURED |
| Main POI | Depot entry→front door→scrap→west opening→retreat, aynı hız | NOT_MEASURED |
| Dense encounter | Yard doğal authored Runner + en fazla 3 local population actor; noise/death durumu not edilir | NOT_MEASURED |
| Night | Resident/depot aynı koşul; target time 82800s time-of-day | NOT_MEASURED |
| Rain | Resident/depot aynı koşul; 43200s ve mevcut yağmur scalar'ı | NOT_MEASURED |
| Flashlight | Gece eş rota, mevcut flashlight açık; ışık ayarı not edilir | NOT_MEASURED |
| Region transition | Resident→3→4→resident; cold/warm ayrı, gerçek CellReady latency | NOT_MEASURED |
| Save/load | Manuel normal Save/Load; capture/write/hydrate süreleri son save operasyonuna aittir | NOT_MEASURED |
| Repeated load/unload | 50 tur 3↔4; 0/10/25/50 turda aynı resident referansına dönüş | NOT_MEASURED |

Steady-state resident ve depot capture dosyaları geldikten sonra kullanıcı offline karşılaştırmayı çalıştırabilir:

```bash
python3 Tools/s023-compare.py --baseline '/absolute/resident/capture.json' --region '/absolute/depot/capture.json' --output 'Docs/Implementation/S023/Evidence/<new-run>/comparison.json'
```

Bu bir Unity/test/profiling komutu değildir; mevcut JSON'ları karşılaştırır. Var olan sonucu ezmez. Seed/platform/CPU/GPU/settings/condition/build-authoring identity uyuşmazsa, pause/error varsa veya örnek yetersizse NOT_COMPARABLE. Başarılı karşılaştırma COMPARED_NOT_ACCEPTED; performans PASS'i değildir. Source dirty true iki run'ın gerçekten aynı olduğunu tek başına kanıtlamaz: runner source hashes ve route notes mutlaka karşılaştırılır.

## Metrikler / sınırlar

- Frame median/p95/p99, >50ms spike sayısı; ham frame sırası korunur.
- Mevcutse Main Thread, GPU Frame Time, draw calls/batches, GC Allocated In Frame, GC Used Memory, Total Used Memory, Texture Memory, Mesh Memory. Her counter'ın native birimi raporda bulunur (örn. nanosecond → ms için 1e6'ya bölünür). Desteklenmiyorsa UNAVAILABLE; sıfırla taklit edilmez.
- CellReady load latency / yükleme sayısı, gözlenen Unloaded geçiş sayısı. Unload sayacı frame örneklemesine dayanır; aynı frame'deki birden fazla durum değişimi gözden kaçabilir. Profiler/timeline ile doğrulayın. Save capture/write/hydrate değerleri son operasyonun ms değeridir, dağılım istatistiği değildir.
- Saf native memory, texture residency ve mesh/texture sahipliği için kullanıcı Memory Profiler snapshot'ı gerekir. Total Used Memory managed/native birlikte içerir. Capture alanları native-only sonuç gibi yorumlanmaz.
- Resident benchmark sahnesi retained referanslar içerir; sadece görsel unload yapıldığında tüm asset memory sıfırlanması beklenmez. 10/25/50 tur sonunda aynı player/cell/encounter koşulu ve warm cache karşılaştırılır. Sürekli yükseliş ile plateau ayrılır.
- Recorder ve sampling dizileri bellek kullanır; başlangıç allocation'ları ısınmada yapılır. Observer overhead **NOT_MEASURED**; eş ayarla recorder kapalı bir capture alın ve farkı not edin. Editor frame süresi retail/Windows performansı değildir.
- Capture sırasında error/exception varsa INVALID_ERRORS. `pausedSamples` kaydedilir; steady-state karşılaştırıcı pause'lı örnekleri reddeder. Save/load senaryolarında pause ayrı olay notudur. Manuel rota, sample duration ve kritik olay zamanlarını kullanıcı kaydetmelidir.

| Kabul hipotezi | Resident | Yeni bölge | Sonuç |
|---|---|---|---|
| 1080p p95≤16,67 ms / p99≤25 ms | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED |
| Ek bellek ≤128 MiB araştırma eşiği | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED |
| 50 tur sonrası plateau / duplication yok | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED |
| Streaming main thread normal frame ≤1,5 ms hipotezi | NOT_MEASURED | NOT_MEASURED | NOT_MEASURED |

Darboğaz kanıtı olmadan mesh merge, aggressive culling, texture quality düşürme veya AI yoğunluğu azaltma optimizasyonu yapılmadı.
