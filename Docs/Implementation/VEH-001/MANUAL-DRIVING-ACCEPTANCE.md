# Development build sürüş kabulü

Durum: NOT_RUN. Focused 68/68, EditMode 495/495, PlayMode 246/246 ve temiz Development build geçti. Kullanılacak build: `Builds/S013/20261005T232232-003098Z-production/LastSignal.app`. Otomatik performans capture hatasız tamamlandı; performans bütçesi ve manuel kabul ayrı değerlendirilir.

Build görünür teker kontrolü için `-veh001Inspect` argümanıyla başlatılabilir. Yalnız bu opt-in Development incelemesinde 8 cockpit/dış görünüş, 9 dört dış açı arasında geçiş yapar; sürüş input'u aynı araç otoritesinde kalır. Normal çalıştırmada bu kamera kurulmaz. Yeni build yolu belli olduktan sonra tam başlatma komutu verilecek.

1. Build'i aç, oturumu başlat. Kabin yakınındaki pickup'a yaklaş, E ile bin; I ile motoru çalıştır. Yol, kaput ve direksiyon görünmeli. Sağa/sola/aşağı bakarken tavan, koltuk veya dashboard içinden bakış oluşmamalı.
2. Araç dururken A/D ile sol ve sağ kilidi kontrol et; bırakınca direksiyon merkeze dönmeli. Cockpit direksiyonu ön tekerlerle aynı yönde olmalı. 8/9 inceleme açılarıyla dış tekerleri gözle; cockpitten görülemeyen dört-teker davranışını PASS sayma.
3. Gri yol boyunca W ile hızlan. Ilımlı sol/sağ dönüş yap. Ön tekerler dönmeli, arka tekerler yönlenmemeli; bütün lastikler fizik hareketiyle uyumlu dönmeli. Dönüş animasyonu için kısa video veya gerçek gözlem gerekir; tek ekran görüntüsü yeterli değil.
4. S ile sert fren yap. Araç durduğunda S basılı kalsa bile geriye kaçmamalı. Fren lambalarını gözle. R ile geri git: hareket ve lastik dönüşü ters yönde, geri lambaları açık olmalı. Düşük hızda dar manevra ve Space el freni dene.
5. Yolun kahverengi gravel bölümünden geç; hafif tutunma farkını kontrol et. Yolun ilerisindeki alçak kaldırımdan düşük hızda geç. Tekerler gövdeye göre hareket etmeli; araç sürekli zıplamamalı, lastikler çamurluktan kopmamalı.
6. Park alanının kenarındaki kısa direğe çok düşük hızda hafifçe temas et. Hasar/impact davranışını gözle; ardından dur ve güvenli çıkışı kontrol et.
7. H korna ve L farları dene. Motor sesinin gaz/coast/stop durumunu izle. W basılıyken pause yap, resume et: gazı bırakıp tekrar basana kadar itiş başlamamalı. Pencere odağını kaybedip geri gelince de kontrol et.
8. Park et; araç içinde save → menu → load yap. Araç pozu, yakıt, kondisyon ve bagaj korunmalı; motor kapalı yüklenmeli. Motoru açıp tekrar sür, park et, E ile çık. Ayakta kamera ve hareket normal olmalı.
9. Aynı kısa sürüşü gamepad ile yap; ileri/geri, fren, direksiyon ve çıkış yönlerini kontrol et. Gece/yağmurda ön görüş, farlar ve kabin korumasını ayrıca gözle.

Yalnız gözlenen kusurları kısa biçimde bildir: “ön teker dönmüyor”, “teker ters dönüyor”, “direksiyon ters”, “araç sağa çekiyor”, “süspansiyon zıplıyor”, “kamera kırpıyor” gibi. Tam rotayı bitirip kusur görmediysen “rotayı tamamladım, kusur görmedim” demen yeterli. Gözlenmeyen dış teker/gamepad/gece adımları ayrıca NOT_RUN kalır.
