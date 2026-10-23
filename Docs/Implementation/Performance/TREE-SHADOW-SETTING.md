# Oyuncu seçimi: ağaç gölgeleri

Kullanıcının yeni benchmark istemeden açık/kapalı seçenek talebi uygulandı. Bu ayar ForestPrototype120 exploration sahnesi içindir; FirstTown production ayar menüsüne veya interactive tree authority sistemine müdahale edilmedi.

## Kullanım

Play Mode kapalıyken derleme tamamlandıktan sonra ForestPrototype120 içinde yeniden Play. Escape → Grafik ayarları → Ağaç gölgeleri kutusu. Escape ile oyuna dönülür. İlk kullanımda açık; seçim PlayerPrefs `LastSignal.Forest.TreeShadows` ile cihazda kalır. Otomatik donanım sınıflandırması yok. Release Player da bu runtime seçeneği içerir.

Canopy, hero ve distant tree renderer shadowCastingMode kapalıda Off; açıkta başlangıçtaki tek/çift taraflı moduna geri döner. Geometri, renderer.enabled, LOD, colliders, terrain, ışıklar, materyaller ve gameplay değişmez. Terrain/shrub/rock gölgeleri ve HDRP contact/SSAO etkileri ayrı kalır; bu seçenek tüm sahne gölgelerini kapatma değildir. Pause paneli mevcut Explorer Escape input toggle'ını kullanır; oyun zamanını durdurmaz.

## Değişen dosyalar

- `Assets/LastSignal/Scripts/Runtime/Forest/ForestTreeShadowSettings.cs` ve `.meta`: yeni runtime panel, renderer başlangıç durumları, tercih kalıcılığı. Renderer taraması yalnız Initialize sırasında, her karede yok. Hot reload için durum serialized tutulur. Tercih yazımı yalnız seçim değiştiğinde.
- `Assets/LastSignal/Scripts/Runtime/Forest/ForestPrototypeExplorer.cs`: Start içinde ayar bileşeni kurulumu; mevcut scene/prefab serialization değiştirilmedi.
- `Assets/LastSignal/Scripts/Runtime/Forest/ForestPrototypeCapture.cs`: summary.json içine `treeShadowSetting` eklenir. Capture sırasında kullanıcı ayarı kilitlenir.
- `Assets/LastSignal/Scripts/Editor/FirstTownForest/ForestPerformanceIsolation.cs`: B/C aktifken oyuncu seçeneği kilitli; restore ile serbest. Böylece tanılama snapshot'ı ile kullanıcı tercihi çatışmaz.

## Sınırlar / durum

FPS artışı NOT_MEASURED; Unity compile/visual/regression NOT_RUN. Kullanıcı isteği üzerine yeni benchmark talep edilmedi/başlatılmadı. Önceki B sonucunda bütün bitki renderer'ları kapandı; 122 FPS yalnız gölge kapatma sonucu değildir. Gölgeler kapalı olduğunda orman geometrisi maliyeti devam eder. Tercih değişiminde birkaç bin renderer flag güncellemesinin tek seferlik maliyeti olabilir; kare başına tekrar edilmez. GUI açıkken IMGUI maliyeti vardır.

Koruma: `Checkpoints/20261023T082254823757Z` üç değiştirilmiş mevcut kaynağın önceki kopyalarını içerir. Geri almak gerekirse daha yeni kullanıcı düzenlemelerini koruyarak bu üç değişikliği geri alın; yeni .cs/.meta kullanılmadan kaldırılabilir. Tercihi sıfırlamak için panelde tekrar açık seçin; PlayerPrefs.DeleteAll kullanılmaz. Sahne/material/prefab/HDRP asset değişikliği yok. Önceki preservation receipt'ler önceki aşamalara aittir.
