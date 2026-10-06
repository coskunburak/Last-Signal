# Last Signal Art Bible — uygulama standardı v0.2

Burak’ın “onaylıyorum. Production level uygulamaya geçelim” kararıyla B/Fantasy Landscape doğa ailesi + proje-owned kırsal cabin kiti uygulamaya alındı. Bu karar, bitmiş POI için son görsel kabul yerine geçmez.

Kimlik: soğuk, terk edilmiş bir kırsal ilçede insanın düzeni sürdürme çabası. Dışarıda kırık ama inandırıcı organik kütleler; sığınakta yatay, sağlam ve elle onarılmış yüzeyler. Stilize gerçekçilik; başka oyunun tasarımı veya shader'ı kopyalanmaz.

| Alan | Üretim kuralı | Kabul yöntemi |
|---|---|---|
| Siluet | Büyük kütle → yapısal orta detay → sınırlı küçük aşınma. Yakından dev üçgen/levha hissi veren yaprak kümeleri kullanılmaz | Siluet ve üç mesafe, 1080p |
| Mimari | Basit kırsal ahşap gövde, inandırıcı çatı yükü, onarım ekleri; fantasy ornament yok | İç/dış aynı grid, kapı/çatı birleşimi |
| Arazi | Yol kenarında malzeme/bitki geçişi, zemine oturan temel; rastgele kaya yığınıyla rota kapatma yok | Gerçek direct/covered route yürüyüşü |
| Bitki | Tek ana aile; farklı yaş ve yükseklik, kontrollü boşluk. Threat koridorunda yüz hizası yoğun çalı yok | Zombie karşılaştırma ve NavMesh |
| Texture | Orta frekansta fırça hissi; foto noise ve güçlü baked specular reddedilir | Gün + flashlight yakın çekim |
| Materyal | Ahşap mat, metal sınırlı yansımalı, cam ayrı tepki; hepsine aynı roughness verilmez | Yan yana swatch + gece |
| Palette | Forest #40564A, Fog #899B9D, Wood #75604B, Metal #606765, Rust #A84B2A, Amber #D29B3D başlangıç referansı | Gerçek ışıkta kalibrasyon; bunlar final albedo değildir |
| Aşınma | Kapı kolu, su akışı, taban teması ve onarım mantığını izler; her yüzeye eşit kir yok | Hikâye ve okunurluk incelemesi |
| Işık | Soğuk doğal dış dolgu; sıcak ama parlamayan shelter; yüzeyler tamamen siyaha düşmez | Aynı kamera gün/gece/yağmur |
| Hava | Mevcut weather/time otoritesi, içeride roof koruması; yeni bağımsız rüzgâr saati yok | RainRoof ve geçiş testi |
| Navigasyon | Amber güvenli kullanım alanını, ölçülü pas rengi bakım/tehlikeyi işaretler; zorunlu item dekor içinde kaybolmaz | İlk bakış + etkileşim raycast |
| FP ölçek | 1u=1m; gerçek CharacterController 1.8m, radius .3m. Crouch, skin width ve AI radius ayrıca ölçülmeden kapı standardı kesinleşmez | Gerçek motor ve canlı AI geçişi |

## Üç kabul adayı — final üretim onayı değil

1. B Rock1: kırık büyük düzlemler ve orta frekans boyalı doku doğal kütleyi okutuyor; 658 üçgen. B-detail kanıtı. Ölçek/LOD ve ortak ışıkta ADAPT adayı.
2. B Pine_tree1: asimetrik, dolgun ve okunur taç; 1554 üçgen. Ana aile için ADAPT adayı; doygunluk ve LOD şartı var.
3. A Fern: 16 üçgen LOD0, küçük alt örtüde basit siluet; A-detail kanıtı. Yalnız B ile aynı benchmark'ta uyum kanıtlanırsa sınırlı ADAPT; karışım onayı verilmedi.

## Üç red örneği

1. A Cedar_Tree_03 yakın planda katmanlı düz yaprak yüzeyleri: boyamak silueti düzeltemez; hero cabin ağacı olarak mevcut hali reddedilir.
2. D barn_1'in yıkık açık çatısı: güvenli ve kuru shelter vaadiyle çelişir; bütün bina modüler cabin kiti yerine kullanılamaz.
3. E Cube.003: 59.562 üçgen, texture bağlantısı yok; standart mobilya adayı olarak pahalı ve görsel olarak tamamlanmamış. Otomatik decimate veya beyaz placeholder ile PASS olmaz.

Kanıtlar ve adayların kesin yolları S013_ASSET_COMPARISON.md / unity-asset-audit-r2.json. Ana aile kararı onaylıdır. Uygulanan palette ve yüzeyler `Assets/LastSignal/Art/S013/Production` altında. Bitmiş POI için gündüz/gece/yağmur incelemesi ve Burak son görsel kabulü sonrası v1.0 yayımlanır.
