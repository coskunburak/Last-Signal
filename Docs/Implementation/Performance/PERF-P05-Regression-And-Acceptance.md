# PERF-P05 — Sahibinin çalıştıracağı ölçüm ve regresyon

**Bu aşamada Unity derleme/test/Play Mode/build/benchmark çalıştırılmadı (`NOT_RUN`).** Aşağıdaki komutlar hazırlanmıştır; bir test PASS sonucu değildir. Önce yalnız “ilk ölçüm paketi”ni tamamlamak yeterli. Performans gate'i sahibinin profiler çıktısı olmadan geçilmeyecek.

## 1. Derleme ve read-only inventory

1. Çalışmanızı kaydedin. Projeyi Unity **6000.5.0f1** ile açın; import/shader compile tamamlanana kadar bekleyin. Yeni CS veya shader hatası varsa durup Console'un ilk hatasını gönderin.
2. `Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity` sahnesini kendiniz açın. Diğer sahneleri additive yüklü bırakmayın. Regenerate/generate menüsünü kullanmayın.
3. Play Mode dışında **Last Signal → Forest 120 → Performance → Export read-only scene audit**.
4. Console'da tam çıktı yolu görünmeli: `Docs/Implementation/Performance/Evidence/native-<UTC>-<id>.json`. `sceneDirtyBefore` ve `sceneDirtyAfter` aynı olmalı; sahne kaydedilmez. `meshes` listesi potansiyel tüm-LOD triangle maliyetine göre sıralıdır; gerçek frame triangle sayısı değildir. `lods` per-object seviyeleri, materyal slotları ve thresholds içerir.
5. Fir mesh GUID `16cb278d4a5214f379bd7a4402f5d8af`, localId `6774275268720665551` ve `1092776003849301120`: mesh adları kaynak LOD1/LOD2'ye karşılık gelmeli; eski beklenen triangle 177.444/70.661. Fark veya million-triangle warning varsa herhangi bir geometri değişikliği yapmadan JSON'u gönderin. Eski sayılar testi geçti kabul edilmez.

## 2. İlk ölçüm paketi — iki makinede aynı sıra

1. Game view'u **1920×1080 sabit çözünürlük** yapın; Free Aspect/Retina ölçek varsayımına güvenmeyin. Native JSON edit-mode boyutu runtime viewport olmayabilir; F9 JSON boyutunu kontrol edin. Kalite PC / pipeline High. VSync/cap değiştirmeyin, rapordaki değerleri karşılaştırın.
2. Scene view render'ını kapatın veya Game view'u Maximize On Play yapın. Gizmos kapalı; Console spam yok; aynı Profiler bağlantı koşulu kullanılsın. Deep Profile kapalı.
3. Play Mode'a **siz** girin. Game view'a odaklanın. **F3** yoğun orman / Morning pozunu seçer. F3 başlangıcı x=24, z=82, pitch=0°, yaw=65°; collision nedeniyle authored marker kaymış olabilir, JSON'daki gerçek pose esas alınır. **F9**: >=180 frame ve >=10 saniye ısınma ardından 30 saniye kayıt. Sonuç log'u gelmeden ikinci koşula geçmeyin. Mac'te gerekirse Fn+F3/F9.
4. A'yı üç kez kaydedin. Run bitince **Performance → B - Hide vegetation renderers (Play Mode only)**, Game view'a tekrar odaklanın; F9 üç kez. B sadece renderer'ları kapatır; grass/terrain/physics korunur.
5. **A - Restore diagnostic overrides**. Önce B'nin görsel olarak geri geldiğini doğrulayın. **C - Disable tree shadows (Play Mode only)** → Game view → F9 üç kez. Ardından A Restore. Aynı anda B ve C biriktirilmez; yeni menü eski denemeyi restore ederek başlar.
6. Bir baseline dönüş kaydı: A Restore, F3, F9; warmup sonrası yaklaşık 30 saniye aynı yerde yavaş ve sürekli yatay dönüş. Kamera rotasını/temposunu not edin. Ayrı bir CPU Timeline kaydında aynı hareketi ve en ağır spike'ı yakalayın.
7. **Window → Analysis → Profiler**: CPU Usage Timeline, Rendering ve Memory modülleri; GPU Usage destekleniyorsa ekleyin. İlk import yerine kararlı F3 ve dönüşte 5–10 saniyelik faydalı aralığı Save ile `.data` olarak kaydedin. Rendering'de batches/SetPass/triangles/vertices/visible renderers görünüyorsa ekranı alın; bu son metrik araçta yoksa `UNAVAILABLE` yazın.
8. Benchmark bittikten sonra **Window → Analysis → Frame Debugger**, F3'te tek kare: gölge, depth, GBuffer/forward, foliage, volumetrics ve post pass'leri; batching break sebepleri ve ilgili draw screenshot'ları. Frame Debugger'ı kapatın. GPU süresi macOS Metal'de görünmüyorsa boş olduğu gibi bırakın; gerekiyorsa sonraki aşamada Xcode Metal GPU capture istenir. Windows'ta da GPU modülünün desteklendiği varsayılmaz.
9. Play Mode'dan çıkınca override'lar restore edilmeli. Tekrar girip F3 görüntüsünün A olduğunu kontrol edin. Sahneyi kaydetme zorunluluğu yok. Bu menüler edit-mode'da B/C uygulamayı reddeder; derleme/reload/PlayMode çıkışında restore eder. Capture devam ederken feature switch reddedilir.

**Gönderilecek minimum paket:** native JSON; her makineden 3 A + 3 B + 3 C run'ın `frames.csv`, `summary.json`, `summary.txt`; baseline rotation CPU `.data`; Rendering/Memory/Frame Debugger görüntüleri; aşağıdaki donanım bilgileri. Denemeler çok ağırsa ilk A ve native JSON ile dönün; örnekleme süresini/başarısız koşulu belirtin.

Çıktılar `Application.persistentDataPath/ForestPrototype120/<UTC>-<id>/`; Console'un yazdığı tam yol esastır. Genellikle macOS `~/Library/Application Support/DefaultCompany/Last Signal/ForestPrototype120/`, Windows `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Last Signal\ForestPrototype120\`. Kesilen kayıt tam benchmark diye dışarı aktarılmaz. Export sırasında oluşan kısa stall ölçümün dışındadır. Dosya izni/disk hatası Console'da raporlanır.

F9 lifecycle kontrolü: yeniden F9 sırasında ikinci kayıt başlamamalı; ısınmada Play Mode'dan çıkış cancellation uyarısı vermeli; tekrar girip tam kayıt üretilebilmeli. Profiler'da `ForestPrototypeCapture.Update` kaynaklı düzenli `GC.Alloc` oluşmamalı; bu kaynak kod hedefidir, ölçülmüş garanti değildir. Metadata/getter/backend maliyeti ayrıca incelenmeli.

## 3. Donanım ve güç — salt okuma

macOS Terminal:

```bash
system_profiler SPHardwareDataType SPDisplaysDataType
pmset -g batt
pmset -g custom
pmset -g therm
```

Paylaşırken seri numarası/UUID'yi çıkarın. Low Power Mode, adaptör ve ısınma durumunu not edin; GPU zamanlaması veya sıcaklık tool tarafından verilmezse uydurmayın.

Windows PowerShell:

```powershell
Get-CimInstance Win32_Processor | Select-Object Name,NumberOfCores,NumberOfLogicalProcessors
Get-CimInstance Win32_VideoController | Select-Object Name,DriverVersion
Get-CimInstance Win32_ComputerSystem | Select-Object TotalPhysicalMemory
powercfg /getactivescheme
nvidia-smi --query-gpu=name,driver_version,memory.total,power.draw,temperature.gpu --format=csv
```

`nvidia-smi` bulunamazsa NVIDIA System Information/Task Manager → Performance → GPU ekranından dedicated GPU memory ve driver alın. WMI AdapterRAM alanı yüksek VRAM'i doğru göstermeyebilir; onunla karar vermeyin. Task Manager'da Unity GPU engine/gerçek RTX kullanımı, Graphics API için F9 SystemInfo sonucu esas alınır. Windows Settings güç modu, AC/battery, CPU/driver sürümü ayrıca kaydedilir. Driver/OS/güç ayarını otomatik değiştiren komut yok.

## 4. Ayrı manuel komutlar

Python statik audit Unity açmaz. Output verilmezse stdout; `--output` yeni dosya ister, üzerine yazmaz. Batch Unity komutlarından önce çalışmayı kaydedip Editor'ü normal kapatın; runner açık Editor/lock tespitinde durur. Test sırasında `-nographics` kullanılmaz. Her komutu ayrı çalıştırın, ilk failure'da log/XML'i inceleyin.

macOS:

```bash
cd '/Users/burakcoskun/Last Signal'
# Salt okunur statik envanter:
python3 Tools/ForestPrototype120/performance_static_audit.py
# Sadece import/derleme gate'i:
bash Tools/ForestPrototype120/manual-checks.sh compile
# Kaydedilmiş forest referansları (generate DEĞİL):
bash Tools/ForestPrototype120/manual-checks.sh references
# Focused EditMode:
bash Tools/ForestPrototype120/manual-checks.sh edit
# Focused PlayMode:
bash Tools/ForestPrototype120/manual-checks.sh play
# Mevcut ağaç/persistence ve hareket regresyonları, ayrı:
bash Tools/ForestPrototype120/manual-checks.sh regression-edit
bash Tools/ForestPrototype120/manual-checks.sh regression-play
```

Windows: PowerShell'i projenin `Assets`, `Packages`, `Tools` klasörlerini içeren kökünde açın. `py -3` yoksa aynı komutlarda kurulu `python` kullanın:

```powershell
py -3 Tools/ForestPrototype120/performance_static_audit.py
py -3 Tools/HDRPProductionOptimization/manual_checks.py compile --output Docs/Implementation/Performance/Evidence
py -3 Tools/HDRPProductionOptimization/manual_checks.py references --output Docs/Implementation/Performance/Evidence
py -3 Tools/HDRPProductionOptimization/manual_checks.py edit --output Docs/Implementation/Performance/Evidence
py -3 Tools/HDRPProductionOptimization/manual_checks.py play --output Docs/Implementation/Performance/Evidence
py -3 Tools/HDRPProductionOptimization/manual_checks.py regression-edit --output Docs/Implementation/Performance/Evidence
py -3 Tools/HDRPProductionOptimization/manual_checks.py regression-play --output Docs/Implementation/Performance/Evidence
```

Runner Unity 6000.5.0f1'i Hub kurulumundan arar; farklı konum için `--unity '<gerçek Unity executable yolu>'`. `--dry-run` yalnız komutu gösterir. macOS wrapper log/XML'leri `Docs/Implementation/ForestPrototype120/Evidence/<UTC>-<action>/` altına; yukarıdaki Windows komutları Performance/Evidence altına yazar.

Filtreler doğrulandı: `edit` ForestPrototypeTests; `play` ForestPrototypePlayTests; `regression-edit` P01TreeStructureTests/P01TreePersistenceTests/FirstTownPersistenceTests; `regression-play` MovementAcceptanceTests/P01TreePhysicsTests. Daha geniş `full-edit/full-play` komutları runner'da var ancak tanılama değişikliğinde ilk gate olarak gerekli değil; unrelated gameplay değişmedi.

## 5. Görsel / işlevsel kabul ve daha sonra Player

F1 trail, F2 clearing, F3 dense, F4 ground, F5 fir yakın/uzak, F6 overcast; F7/F8 preset. Durağan, yürüyüş, hızlı dönüş, canopy silhouette ve fog koşullarında LOD pop/crossfade, alpha shimmer, bark, materyal/shadow continuity kontrolü. WASD/sprint/crouch/jump, trunk collision, sınır ve terrain teması aynı kalmalı. A/B görsel bozulması yalnız tanılama sırasında kabul edilir.

Forest dekoratif: burada ağaç kesme testi geçmesi beklenemez. Mevcut tree physics validation sahnesinde kesme/düşme/gathering ve persistence'i ayrıca test edin; FirstTown veya interactive prefab değişmediği için bu aşama mekanikleri değiştirdiğini iddia etmez.

İlk sonuçlar sonrası standalone karşılaştırma: **File → Build Profiles** altında ayrı, açıkça `ForestPerf` isimli macOS/Windows Development Build profili oluşturun; yalnız ForestPrototype120 sahnesini profile-specific scene list'e ekleyin, production/global scene list'i değiştirmeyin. 1920×1080, eşit VSync/cap, aynı generation/shot. Script Debugging ve Deep Profiling kapalı. F9 yalnız Editor veya Development Build'de etkin; release Player'da inert. Build/run işlemini yalnız sahip başlatır. B/C menüleri Editor assembly'sindedir, Player'da yoktur; ilk Player gate A'dır. B/C Player karşılaştırması gerekirse ayrı opt-in harness hazırlanır.

Kabul: dev usability 30 FPS hedefi, tekrarlayan yüzlerce ms stall olmaması ve süre uzadıkça bozulmama; runtime 1080p Medium 60 FPS bir sonraki hedef. Hiçbiri bu aşamada sağlandı ilan edilmemiştir.
