# WLD-G4 — navigasyon temeli, yol entegrasyonu henüz yok

**IMPLEMENTED / OWNER_BAKE_PENDING.** Owner G3 terrain adayında Play başlattığında13 kez `Zombie spawn has no matching NavMesh` hatası döndü. Bu bir terrain üretim hatası değildir: G3 yeni topografyaya uymayan eski bake'i kaldırmıştı. Eksik adım yeniden bake ve spawn doğrulamasıdır. Runtime hata kontrolü, spawn koordinatları veya düzeltme yarıçapı gevşetilmez.

Salt okunur bulgular: aktif saved TerrainStage, Play=false, dirty=false; tek NavMeshSurface, agent `LastSignal Shambler` (-1372625422), `navMeshData=null`,13 encounter. Kaynak collect=All, geometry=PhysicsColliders, mask=-261, voxel override=.1m, tile override128. Örnek prefab SpawnSampleRadius=2m. Her encounter'ın kendi tanımı ve agent maskesi bake sonrası ayrıca kullanılır.

## Uygulama

Yeni `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldNavigationAuthoring.cs` iki explicit owner menüsü ekler:

- **7 Create navigation candidate and bake**: önce saved G3 kontrolü, sonra ayrı `Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_NavigationStage.unity` kopyası. Mevcut tek surface aynı agent/physics/mask/voxel/tile ayarlarıyla kullanılır; collection approved resident volume (center−250,20,0; size1000,120,500) olur. İki terrain'i kapsar. Bake sonucunu yalnız yeni `Generations/G4Nav-.../ResidentNavMesh.asset` içine kaydeder. G3/G2/Main ve mevcut602 asset değişmez. Yeni save slot `wld_s001_naturalworld_navigation_stage`.
- **8 Validate saved navigation**: kaydedilmiş scene/nav asset hashleri, kaynakların değişmemesi, doğru surface/terrain referansları ve13 spawn coverage kontrolü. `NavMesh.SamplePosition` runtime ile aynı agentType/areaMask/SpawnSampleRadius değerlerini kullanır. Başlangıç düzeltme mesafesi raporlanır; hiçbir spawn taşınmaz. Bir spawn eşleşmezse oluşturma başarısız sayılır; kimliği journal'a yazılır.

Tüm nesnelerin transform, parent, component ve stable ID snapshot'ı bake öncesi/sonrası ve scene yeniden açıldıktan sonra karşılaştırılır. G3 terrain assetleri salt okunur paylaşılır. Source hashleri tekrar ölçülür. Mevcut aday veya receipt üzerine yazılmaz. Hata durumunda yalnız bu çalıştırmanın scene/nav asset/boş klasörleri geri alınır; G3 yeniden açılır. Kayıt `G4_Navigation_Latest.json`, per-run journal `Evidence/G4Nav-...json`.

Bu tamamlanmış G4 yol/traversal kabulü değildir. Yeni yol meshleri, batıdaki çit geçişi, POI rotaları ve tam kovalamaca testleri sonraki çalışmadır.13 spawn'ın örneklenmesi dünya genelinde yol bağlantısı veya AI performansı garantisi değildir. Ekolojik dağılım G5 bekler.

## Owner sırası

1. Play kapalı, kaydedilmiş **WLD_S001_NaturalWorld_TerrainStage** açık olsun. G3'ü yeniden üretmeyin. Inspector'dan bu kaynak sahneye bake kaydetmek yerine aşağıdaki izole menüyü kullanın.
2. **Last Signal → World Reconstruction → WLD-S001 → 7 Create navigation candidate and bake**. Bake büyük arazi nedeniyle zaman alabilir. Başarıda NavigationStage açık kalır ve `WLD G4 NAVIGATION_CREATED. Spawn coverage=13/13` beklenir.
3. Yalnız başarılı olursa **8 Validate saved navigation**. Beklenen `WLD G4 SAVED_NAVIGATION_VERIFIED`.
4. Yalnız bu iki adım başarılıysa **NavigationStage** üzerinde Play açın. Aynı13 NavMesh spawn hatasının çıkmaması, zombilerin doğması ve yakın oyuncuya hareketi gözlemlensin. Kaynak TerrainStage hâlâ bake içermeyen geri dönüş noktasıdır; Play testi orada yapılmamalı.
5. `G4_Navigation_Latest.json`, iki Console sonucu ve Play gözlemini gönderin. Bake hata verirse ilk exception ve yeni journal yeterli; tekrar tekrar bake/Validate çalıştırmayın.

Agent bake, Play, route test veya benchmark çalıştırmadı. Statik/compile kontrolleri runtime kabulünün yerine geçmez. WMV format ve Account API uyarıları bu spawn hatasının nedeni değildir; vendor dosyaları değiştirilmedi.

Teslim kontrolü: WorldNavigationAuthoring tipi ve CreateAndBake metodu Unity assembly'sinde görüldü, compiling=false. Statik verifier tüm mevcut kaynak/G2/G3 receipt hashlerini doğruladı; navigationCandidate=NOT_RUN. Son compile gözleminde açık TerrainStage dirty=true idi; otomatik save/discard yapılmadı. Önce kaydedilmemiş durumu inceleyin. Kaydetmek G3 hashini değiştirirse receipt'i körlemesine yenilemeyin; değişikliği birlikte değerlendirmek gerekir.
