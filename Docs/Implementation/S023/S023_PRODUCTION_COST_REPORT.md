# S023 gerçek üretim maliyeti

Durum **NOT_MEASURED**. Kullanıcı zaman kaydı henüz sağlanmadı. Kod/dosya sayısı, agent çalışma süresi veya author komutunun wall-clock süresi gerçek geliştirici saati olarak kullanılmaz. S023 planındaki 40–60 saat yalnız bütçedir.

`cost-log.csv` kayıt şeması: tarih, kişi, iş, maliyet sınıfı, yöntem, odak dakika, bekleme dakika, rework dakika, kabul edilen çıktı, evidence, durum. Her çalışma aralığı tek kez sayılır; bekleme ile odak süreleri ve rework ayrı tutulur. Sayısal alanlar ölçülmeden boş bırakılır.

| İş | Maliyet sınıfı | Ölçülecek kanıt | Actual |
|---|---|---|---|
| Graybox / rota | Bölgeye özel | Başlangıç-bitiş kaydı, aynı acceptance kapsamı | NOT_MEASURED |
| Ana POI authoring | Bölgeye özel | Manuel düzenleme ve S019 template işlemleri ayrı | NOT_MEASURED |
| Küçük POI + yard | Bölgeye özel | Giriş/retreat/loot/nav dahil kabul edilen alan | NOT_MEASURED |
| Environment art | Bölgeye özel | Palette/geometry/görsel rework | NOT_MEASURED |
| Asset uyarlaması | Tekrar kullanılabilir | Shared ref/override/dependency işlemleri | NOT_MEASURED |
| Collision / navigation | Doğrulama ve düzeltme | Authoring süresi + native bake bekleme ayrı | NOT_MEASURED |
| Loot / encounter | Bölgeye özel | Profile/placement/identity ve acceptance | NOT_MEASURED |
| Streaming / persistence debugging | Doğrulama ve düzeltme | Hata sınıfı, fix, dar retest, regression | NOT_MEASURED |
| Authoring/capture/runner araçları | Araç geliştirme | Yeni araç işi, bölge içeriğiyle çift sayım yok | NOT_MEASURED |
| QA / rework | Doğrulama ve düzeltme | İlk kabulde bulunan kusur ve tekrar iş | NOT_MEASURED |
| Performance optimization | Doğrulama ve düzeltme | Önce/sonra matched capture + gerçek süre | NOT_MEASURED |

S019 reuse gerçekleri: identity rekey, PoiAuthoringLayout, content validator, mevcut kit ve native navigation authoring sözleşmesi kullanıldı. Bunun ölçülmüş saat kazancı **bilinmiyor**. Yeni depo geometri üretimi bölgeye özgüdür; S019 template'in aynısı kullanılmış gibi maliyet indirimi uygulanmaz.

S019 verim karşılaştırması: aynı küçük POI kapsamını iki disposable authoring sahnesinde karşılaştırın; birinde manuel kurulum, diğerinde template/araç. Aynı operatör, aynı kapı/loot/encounter/nav/identity kabul listesi. Gerçek aktif dakika, eylem sayısı, yakalanan hata, rework dakika ve kabul sonucu kaydedilir. Öğrenme/sıra etkisini not edin; mümkünse sıralamayı değiştirerek ikinci örnek alın. Araç/native bekleme süreleri ayrı. Eski S019 gerçek süreleri yoksa geriye dönük tahmin yazmayın.

Sonraki bölge tahmini yalnız kayıtlar gelince: ölçülmüş bölgeye özel işlerin düşük/orta/yüksek aralığı + gereken yeni araç işi + gözlenen QA/rework oranı. Bir kez yapılan shared iş ikinci kez tam faturalandırılmaz; yeni kit/biome gerekirse ayrıca eklenir. En az iki eş kapsamlı örnek yoksa aralığın güveni LOW olarak raporlanır. Alan büyüklüğü veya 4 km² hedefi tek başına üretim saati hesabı değildir.

| Toplam | Saat |
|---|---|
| Tekrar kullanılabilir | NOT_MEASURED |
| Bölgeye özel | NOT_MEASURED |
| Araç geliştirme | NOT_MEASURED |
| Doğrulama/düzeltme | NOT_MEASURED |
| Sonraki bölge tahmini | NOT_ESTIMATED — gerçek zaman kaydı bekleniyor |
