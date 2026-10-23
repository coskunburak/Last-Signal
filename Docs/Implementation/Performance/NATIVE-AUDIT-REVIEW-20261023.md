# Native rapor değerlendirmesi — 23 Ekim 2026

Sahibi tarafından çalıştırılan iki audit okundu: `native-20261023T0802449785200Z-0910cbdc3280481085163723847f2295.json`, `native-20261023T0804076160920Z-a2accec810e0477b88f8ecca9490701e.json`.
İkisi de aynı generation/digest/envanteri gösteriyor. Bu bir benchmark sonucu değildir.

## Kesinleşenler

- Metal / Apple M3 Pro / PC quality / LastSignal_HDRP_High; SRP Batcher açık.
- 10.353 aktif nesne, 382 collider, 0 MeshCollider, 0 missing script; duplicate candidate yok.
- TerrainData: 0 tree instance, 9.000 detail hücre toplamı, 1 detail prototype, instanced terrain.
- sceneDirtyBefore=false ve sceneDirtyAfter=false; audit sahneyi kirletmedi.
- Fir yalnız `fir_tree_01_a_LOD1` (177.444 triangle, 138.058 vertex) ve `fir_tree_01_a_LOD2` (70.661 triangle, 73.227 vertex) kullanıyor. İkisinde 4 submesh var. Denetlenen sahnenin renderer referanslarında 4.176.819 triangle kaynak LOD0 kullanılmıyor. Önceki P00/P01'de bekleyen bu kontrol kapandı.
- 2.009 uyarının tamamı tek seviyeli cull grubu bilgisidir; 2.009 bozuk LOD anlamına gelmez. Fern/shrub/rock/branch/distant grupları bu kapsamdadır.
- Fern b/c toplam potansiyeli 963.136 + 793.544 triangle. Bu bütün örneklerin toplamıdır, frame'de çizilen sayı veya GPU darboğazı kanıtı değildir.

| Aile | Native LOD triangle sayıları (ayrı tek sayı distant çeşittir) |
|---|---|
| Birch2 | 13526 / 6763 / 3381 / 1689 |
| Bush | 54 |
| FallenBranch | 822 |
| FirHero_LimitedLOD1LOD2 | 177444 / 70661 |
| Rock | 658 |
| Spruce1 | 15096 / 7548 / 3773 / 1886; 1886 |
| Spruce4 | 16085 / 8042 / 4020 / 2010; 2010 |
| Spruce6 | 18581 / 9290 / 4644 / 2321; 2321 |
| Spruce8 | 17138 / 8569 / 4283 / 2141; 2141 |
| Spruce9 | 17138 / 8569 / 4284 / 2142; 2142 |
| fern_02_a | 784 |
| fern_02_b | 2384 |
| fern_02_c | 2248 |
| fern_02_d | 816 |

## B/C çağrılarının reddi

Eski guard Play Mode, aktif sahne veya sceneCount koşullarından herhangi biri sağlanmadığında aynı exception'ı veriyordu. Auditler doğru sahnede bir sahne yüklü olduğunu gösterse de B/C tıklama anının durumunu kaydetmediği için hangi koşulun yanlış olduğu kesin değil. Edit Mode'da B/C seçilmiş olması olasıdır. Renderer değişikliği guard sonrasında olduğundan bu hatalı çağrılar B/C uygulamadı.

`Assets/LastSignal/Scripts/Editor/FirstTownForest/ForestPerformanceIsolation.cs` beklenen mod/scene/capture koşullarında exception yerine ayrıntılı warning ve erken dönüş verecek şekilde değiştirildi. PlayMode, pause, aktif sahne ve yüklenen sahne adedi gösterilir. Pause kontrolü de eklendi. Yapısal hatalar halen durdurulur; güvenlik koşulu gevşetilmedi. Önceki kaynak `Checkpoints/20261023T080455450861Z/ForestPerformanceIsolation.cs.before` içinde. Yeni değişiklik Unity'de NOT_RUN; sahibinin audit başarısı önceki sürümün derlenip Export çalıştığına kanıttır, bütün testlerin geçtiğine değil.

Account API uyarısının stack'i Unity.AI.Toolkit.Accounts paketine aittir; audit stack'inden ayrıdır. Mesaj ağ veya Editor focus olasılığını bildiriyor. Bunun frame stutter sebebi olduğu ölçülmedi; paket/hesap ayarı değiştirilmedi.

## İlk sonraki adım

Önce yalnız A kaydı: compile bittikten sonra Forest tek açık sahne → 1920×1080 Game view → Play → pause kapalı → Game view odak → F3 → F9. >=180 kare ve >=10 saniye warmup, sonra 30 saniye bekleyin. `Forest performance capture written:` log'undaki klasörü paylaşın. B/C'yi ilk A tamamlandıktan sonra Play Mode'dan çıkmadan seçin. GPU/CPU/FPS hâlâ NOT_MEASURED; görsel veya render ayarı değişmedi.
