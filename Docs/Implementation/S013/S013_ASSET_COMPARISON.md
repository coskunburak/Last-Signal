# S013 beş paket karşılaştırması — sanat kararı bekleniyor

**Öneri: B / Fantasy Landscape ana doğa ailesi, proje sahipli özgün kırsal cabin kiti.** Bu öneri Burak onayı değildir. Doğa kitlerinin birlikte karıştırılması önerilmiyor. Gösterilen ortam bir asset laboratuvarıdır; bitmiş POI değildir.

Sahne: `Assets/LastSignal/Scenes/Validation/S013AssetComparison.unity`. En yeni kanıt `Evidence/20261003T224451-879557Z/comparison-r4/`. 1920×1080, FOV50, ACES, exposure0, sun1.3, ortak ambient SH. Aynı kamera iki kadraj; x-offset dışında transform sabit. Tüm modeller kaynak ölçeğinde; zemin teması LOD0 renderer bounds ile hizalandı. Diğer bayler capture sırasında kapatılır. Gün/gece/yağmur kabulü değildir.

| Paket / aday | Siluet ve geometri | Texture / materyal | Mimari uygunluk | URP / maliyet / performans | Karar ve lisans |
|---|---|---|---|---|---|
| A Pine_Tree, Cedar_Tree_03, Fern, Stump, Forest_Grass_02 | Pine 857, Cedar 375 LOD0 üçgen; dal kartları ve Cedar yatay katmanları yakında belirgin | Boyalı bark çok iri; zemin fazla canlı; fern küçük ve okunabilir | Bina sağlamaz | Mevcut URP + LOD avantajı; yakın dal uyarlaması orta-yüksek, alpha overdraw ölçülmedi | Ana aile olarak DEFER; Fern sınırlı ADAPT adayı; EULA edinim teyidi bekler |
| B Pine_tree1, Birch_tree1, Bush1, Rock1, ground | Pine1554, Bush54, Rock658 üçgen; dolgun organik taç ve kırık kaya düzlemleri | Orta frekans boyalı yüzey; sarı/yeşil doygunluk fazla; yaprak detayı yakında gürültülü | Town mimarisi otomatik kabul değil; modern kırsal kit ayrı | Built-in→URP kopya render edildi; seçili prefabda LOD yok, maliyet orta; overdraw/perf bekler | Ana aile ADAPT önerisi; Burak onayı yok; EULA edinim teyidi bekler |
| C Tree01A, Bush01, Rock01, Bridge01 | Küçük konik ağaç düzenli; kaya yuvarlak ve B'den farklı | Yeşil aşırı canlı; kaya normal frekansı yüksek | Köprü cabin gereksinimi değil | URP Graph render edildi; LOD var; environment SH gerekir; maliyet orta | Bitki/köprü DEFER; kaya ADAPT araştırması, bu POI için zorunlu değil; lisans belirsiz |
| D barn_1 | 5558 üçgen, 4 submesh; 14.62×7.25×7.71 m world bounds; yıkık bütün bina | Foto benzeri aşınma ve yoğun plank detayı boyalı doğadan ayrışıyor | Kırsal dil uygun; açık çatı güvenli shelter'a uygun değil; modüler kit değil | URP embedded material render; collider/LOD yok; bölme/çatı/texture uyarlaması yüksek | Temsili cabin için REJECT; gelecek harabe DEFER; lisans belirsiz |
| E cupboard_pack 4 mesh | 6054/3284/3964/59562 üçgen; birleşik sergileme; child scale4.98–33.52 | Beyaz, bağlı texture yok; modern temiz tasarım değerlendirmesi eksik | Yaşlandırma ve parça ayrımı gerekir | URP shader var ama bitmiş görünüm yok; collider/LOD yok; maliyet yüksek | DEFER; pahalı parça mevcut halde REJECT; kaynak ve lisans teyidi bekler |

A'nın LITE içeriğinde gerçek bush/rock bulunmadığından Fern alt örtü örneği ve Stump gösterildi; bunlar çalı/kaya yerine geçmiş sayılmadı. B küçük ağaç örneği Pine yanında Birch'tür; native boyutları değiştirilmedi. D/E yakın çekimde mimari/mobilya durumları görünür. Model kanalı varlığı UV kalite kabulü değildir.

## Görüntüler

### A — Silver Cats

![A tam siluet](Evidence/20261003T224451-879557Z/comparison-r4/A-full.png)
![A yakın](Evidence/20261003T224451-879557Z/comparison-r4/A-detail.png)

### B — önerilen Fantasy Landscape

![B tam siluet](Evidence/20261003T224451-879557Z/comparison-r4/B-full.png)
![B yakın](Evidence/20261003T224451-879557Z/comparison-r4/B-detail.png)

### C / D / E

![C](Evidence/20261003T224451-879557Z/comparison-r4/C-detail.png)
![D](Evidence/20261003T224451-879557Z/comparison-r4/D-full.png)
![E](Evidence/20261003T224451-879557Z/comparison-r4/E-detail.png)

## Tutarlılık değerlendirmesi

B'nin kaya ve bark yüzeyleri, mevcut zombi ile A'nın büyük pul bark dokusundan daha yakın bir detay frekansı kuruyor. Ancak mevcut FPS elinin geniş düz renkleri, koyu küçük silah yüzeyleri ve zombinin daha yoğun ten dokusu hâlâ farklı kalite düzeyinde. Bu farklılık kamera ile saklanmadı; mevcut projeden sergilenen el/silah statik referanstır, gerçek first-person duruş/FOV kabulü değildir. S014 animasyon veya karakter yeniden üretimi bu çalışmada yapılmadı.

B seçilirse ilk adım üç sınırlı doğa prefabını normalize edip kendi ahşap duvar/çatı malzemesiyle tek cabin benchmark'ında tekrar karşılaştırmak. Yalnız tint uygulayıp PASS denmeyecek; LOD, texture yoğunluğu, yakın yüzey ve gece threat okunurluğu ayrıca sınanacak.
