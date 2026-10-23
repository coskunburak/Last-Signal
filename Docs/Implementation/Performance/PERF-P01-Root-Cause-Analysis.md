# PERF-P01 — Kanıt ve öncelik sırası

23 Ekim 2026. **Ölçülmüş CPU/GPU darboğazı yok.** Aşağıdaki sıralama araştırma önceliğidir; ms kazanç tahmini veya FPS iyileşme iddiası değildir.

| Öncelik | Kanıt / durum | Olası etki | En küçük sonraki adım | Risk / kabul yöntemi |
|---|---|---|---|---|
| P0 | 5.792 tüm-LOD renderer slotu, 441 ağaç; 15 shared renderer materyali | Render submission/culling, shader/pass/alpha maliyeti; **hipotez** | Aynı F3 pozunda A/B vegetation çizimi karşılaştırması + CPU Main/Render + Frame Debugger | B yalnız tanılama; renderer flag'leri restore edilir, collider değişmez. B terrain grass'ı içermez |
| P0 | Fir yalnız bir prefab, iki local mesh fileID; binary mesh kimliği yeniden doğrulanmadı | Yanlış ithal mesh varsa büyük geometry yükü; **henüz doğrulanmadı** | Native audit'te bu iki fileID'nin mesh adını, triangle ve vertex sayılarını kontrol et | LOD1/2 doğrulanmadan kaynak/model değişikliği yok; gerekirse yalnız ayrı derivative |
| P1 | Fern 1.500 örnek, tek geometri LOD, alpha clip/double-sided; 404 b ve 353 c | Overdraw ve vertex yükü; **hipotez** | B sonucu, GPU foliage/depth/GBuffer/shadow pass ve overdraw görünümü | Ölçüm gerekçelendirirse tür bazlı LOD derivative; yoğunluk azaltılmadı |
| P1 | 4.072 tüm-LOD shadow caster slotu, 80 m shadow distance, tek güneş 2048 | Shadow map ve alpha-clipped caster maliyeti; **hipotez** | C yalnız ağaç gölgeleri kapalı A/B; shadow pass süresi | Runtime snapshot restore. Kalıcı gölge kaybı yok |
| P1 | Morning fog/SSAO/contact/TAA aktif yapılandırılmış; gerçek GPU süresi yok | Piksel/post/fog maliyeti; **hipotez** | Debugger'da D ve E tek özellik alt denemeleri; önce temel capture | Paylaşılan Volume assetlerini düzenleme; runtime debugger override restore |
| P1 | Eski kaydedici her karede `new double[]` ve List büyümesi; yalnız F9'dan sonra çalışır | Ölçümün GC ile kirlenmesi **kodda doğrulandı**, stutter sebebi olduğu ölçülmedi | Sabit struct buffer + warmup + GC/batch/SetPass sayaçları uygulandı | Unity compile ve F9 lifecycle manuel; Profiler'da capture Update GC.Alloc kontrolü |
| P2 | PC texture streaming kapalı, importlarda mip streaming açık | Bellek/bandwidth/stutter; **hipotez** | Memory Profiler ve RAM/VRAM basıncı al | Texture küçültme/streaming enable henüz yapılmadı |
| P2 | GRD kapalı, SRP Batcher açık, instancing flags açık | CPU bound ise draw submission fırsatı; **hipotez** | CPU Timeline + batching sebebi; daha sonra bağımsız GRD deney profili | Shader Graph/wind/crossfade/MPB ve Metal/DX uyumu ölçülmeli; build variant maliyeti var |
| P2 | CrossFade grupları mevcut, High dithering support kapalı | LOD geçişi/görsel tutarsızlık olasılığı | Yakın/orta/uzak kamera hareketi + Frame Debugger | Görsel risk; şu aşamada ayar değişmedi |
| P3 | 382 collider, 0 Rigidbody, 0 MeshCollider; nonalloc oyuncu sorguları | Dekoratif ormanda physics baskınlığı kanıtlanmadı | Timeline Physics ölçülürse G | Ağaç kesme sistemi başka sahnededir; collider topluca silinmez |

## Deney matrisi

Her alt deney ayrı baseline A ile kıyaslanır. Profil değiştirmek birden fazla etkiyi beraber değiştirir; ilk teşhis yerine High/Development karşılaştırması kullanılmaz. Her geçişten sonra shader/temporal history kararlı hale gelsin; F9 ayrıca >=180 kare ve >=10 saniye warmup yapar.

| Kod | Tek değişken / koşul | Geri dönüş / sınır |
|---|---|---|
| A | Tam orman, High, Morning, F3, 1920×1080 | Referans |
| B | Yeni menü ile vegetation renderer.enabled=false | Terrain grass ve environmental details korunur; bu **tüm bitki örtüsü sıfır** testi değildir. A Restore |
| C | Yeni menü ile canopy/hero/distant tree shadowCastingMode=Off | Fern/shrub zaten gölgesiz; A Restore |
| D1 | Rendering Debugger Camera Frame Settings: Volumetrics off | Yalnız runtime debug override, sonra reset. D2 clouds ancak effective aktifse |
| E1/E2/E3 | Ayrı ayrı SSAO, contact shadows, SSR off | Her biri öncesinde A; SSR'nin effective aktif olması kontrol edilir |
| F | A ile aynı görüntü, render-resolution maliyeti deneyi | İlk turda Game view 1280×720 yardımcı karşılaştırması yapılabilir: bu **render scale değil output resolution değişimi** olarak etiketlenmeli. Gerçek HDRP scale A/B sonraki ayrı opt-in profil işi |
| G | Physics Timeline attribution | Collider veya gravity kapatmak hareketi değiştirir; bu aşamada ölçmeden otomatik G override uygulanmadı |
| H | Ek ışık maliyeti | Bu sahnede yalnız 1 directional var; çoklu ışık kapatma deneyi uygulanabilir değil |
| I | Gameplay etkisiz sabit render | Sahne AI/hasat çalıştırmıyor; F3 idle başlangıçtır. Gerekirse sonraki aşamada hareket harness'i ayrıca izole edilir |
| J | Hareket açık, B vegetation render kapalı | Aynı rota/tempo ve collision; üretim combat/AI yükünü temsil etmez |

D/E için Window → Analysis → Rendering Debugger → Frame Settings → GameplayCamera seçilir; runtime override sütununda tek özellik değiştirilir, diğerleri korunur. Arayüzde özellik görünmüyorsa yanlış kamerada/Editor modunda devam edilmez; ekran görüntüsüyle geri bildirim istenir. Paylaşılan `.asset` Volume Inspector değerlerini değiştirmek Play Mode'dan çıkınca otomatik geri alınmaz; bu yüzden burada asset düzenlemesi yok.

## Spike ayrımı

- Import/shader compile/ilk yükleme: ayrı kayıt, steady-state tablosuna karıştırılmaz.
- Kamera dönüşü: F3 aynı noktada 30 saniye sabit hızla dönüş; CPU Timeline'da culling/render submission, GC.Alloc/GC.Collect, shader creation ve physics spike marker'ları eşleştirilir.
- GPU değerleri yoksa FPS'den GPU süresi türetilmez. Thread marker'larında wait dahil olabileceği için büyük Main Thread sayısı tek başına CPU bound kanıtı değildir.
- Runtime generation, materyal instance veya sürekli scene-wide arama bu sahneye bağlı incelenen kodda bulunmadı. Diğer production gameplay sistemleri bu sonuçla aklanmış sayılmaz.

P02/P03 kalıcı uygulama: ölçümden sonra **tek** hedef değişiklik → aynı A/B workload → yakın/orta/uzak görsel kabul → retain/revert. Genel HDRP ayar paketi uygulanmadı.
