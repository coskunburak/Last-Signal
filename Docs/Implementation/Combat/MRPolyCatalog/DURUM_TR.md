# MR POLY silah kataloğu

## Üretilen varlıklar

- `Assets/LastSignal/Prefabs/Combat/MRPoly/World`: 7 Assault Rifle, 5 Tactical Pistol, 4 Pump Shotgun; toplam 16 sunum prefabı.
- `Assets/LastSignal/Prefabs/Combat/MRPoly/FirstPerson`: 7 Assault Rifle renk varyantı. Mevcut VAL animasyonları, silah tanımı, ADS, optik presenter ve lens varlıkları korunur.
- `Assets/LastSignal/Materials/MRPoly`: kaynak materyallerin renk, metaliklik ve pürüzsüzlük değerlerini kullanan URP materyalleri.

Kaynak varyant prefablarında eski FBX/materyal GUID bağlantıları vardır. Proje kopyaları geçerli FBX modellerinden yeniden oluşturulur. Renk yerleşimleri varyant adlarına göre yeniden kurulmuştur; kaynak varyantların birebir görsel eşdeğeri oldukları henüz doğrulanmadı.

`Last Signal/Weapons/Build MR POLY Visual Catalog` menüsü kataloğu tekrar üretir. Üretilen prefablara yapılan manuel görsel düzenlemeler yeniden üretimde üzerine yazılabilir. Mevcut `Assets/Resources/Weapon_AssaultRifle.prefab` değiştirilmez.

## Doğrulama

- 23 prefabın Unity tarafından yüklenmesi ve MeshFilter referansları ön kontrolde doğrulandı.
- Unity betik derlemesi hatasız.
- Kaynak paketteki 93 dosya, üretim öncesi `source-sha256.json` kaydıyla karşılaştırıldı; değişiklik yok.
- `MRPolyCatalogTests` eklendi. NUnit testi henüz çalıştırılmadı.

## Açık işler

- Tactical Pistol ve Pump Shotgun şu aşamada yalnız dünya/sunum prefablarıdır; envantere alma veya ateşleme bileşeni eklenmedi.
- Bu iki silahın oynanabilir birinci şahıs sürümleri için uygun tutuş, reload, tabanca sürgüsü ve pompa animasyonu gerekir. FBX modellerinde yalnız tabanca şarjörü/tetik ve pompalı tetik ayrı nesnelerdir; sürgü/pompa hareketli parçaları ayrıca hazırlanmalıdır.
- Görsel incelemede tabanca ve pompalıda mekanik nişangâh görülmüştür. Bu silahlara büyütmeli optik veya kırmızı nokta eklenmedi. Büyütmeli optik mevcut tüfek varyantlarında korunmuştur.
- Tüm silahlar için üretim kabulü henüz tamamlanmadı. El pozları, yeni silah mekanikleri, kaynak palet eşdeğerliği ve oyun içi görsel/performance kabulü tamamlanmadan bu katalog bütünüyle oynanabilir üretim paketi sayılmaz.
