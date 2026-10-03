# S012 doğrulama ve Burak kabulü

## Tekrar üretim

Unity 6000.5.0f1 ile bu proje açık olmalı, başka test/build çalışmamalı. Kayıtsız Editor değişikliklerini önce koru. `python3 Tools/s012-verify.py edit --filter LastSignal.Tests.S012SceneTests` ve `python3 Tools/s012-verify.py play --filter LastSignal.Tests.S012IntegrationTests` mevcut Editor doğrulayıcısına istek gönderir. Filtreyi kaldırmak ilgili tam assembly'yi çalıştırır. Her çağrı değişmez run dizini, source hash manifesti, XML ve sonuç JSON'u üretir. Tarihsel testlerin sabit yola yazdığı çıktılar yeni run altına taşınır ve önceki baytlar geri konur. İkinci Unity batch süreci açılmaz. Zaman aşımında canlı testi incele; script yedek yolunu bildirir ve çalışan testin dosyalarına dokunmaz.

Build ve testlerin tam Terminal komutları `S012_RUN_COMMANDS.md` içindedir. Batch build için açık Editor önce kapatılır. Sahne zaten oluşturuldu; `Author()` mevcut adayı ezmeyi reddeder. Standalone teknik kabul argümanı `-s012Acceptance <yeni evidence dizini>`, performans argümanı `-s012Performance <ayrı evidence dizini>`. İkisi aynı süreçte kullanılmaz. Teknik kabul fixture konumlandırması kullanır; performans çevre snapshotı ve başlangıç konumu fixture'dır. Hiçbiri insan testi değildir.

Güncel teknik aday `Builds/S012/20261003T102952Z/LastSignal.app`: build PASS, önceki gameplay kaynağının standalone teknik kabulü PASS, D118 düzeltmesi sonrası üç koşullu performans yakalaması `Evidence/20261003T103218Z-performance-retry` PASS. Gerçek 30–45 dakikalık oynanış ve ürün kararı hâlâ NOT_RUN. 1280×800 performans ekran görüntülerinde üst HUD ve alt kontrol ipuçları kısmen çakışıyor; okunurluğu gerçek koşuda özellikle kaydet.

## Burak için zorunlu geliştirici oynanış kabulü — NOT_RUN

Test edilen build kimliğini, tarih/başlangıç-bitiş saatini ve kontrol cihazını kaydet. Teknik otomasyon argümanı olmadan build'i aç. Mevcut kişisel checkpoint dosyasını koru; yeni worldId eski dünyayı güvenli şekilde reddeder, önceki checkpoint'in üzerine manuel save yazmadan önce yedekle.

1. Açılışta başlayan oturumu menüye dönüp **New Game** ile yeniden başlat. Oyuncu cabin içinde başlamalı; radyo veya duvar içinde olmamalı.
2. WASD, fare, SHIFT, C, E kontrollerini dene. J radyonun görev günlüğünü açıp kapatır; ESC menüyü açar. UI kapanışında istemsiz ateş olmamalı.
3. Cabin içindeki radyoyu E ile oku; E çıkış terminalinden dışarı çık. Başlangıç loadout'u mevcut üretim prefabıdır; debug item komutu kullanma.
4. Doğrudan rotada security ve markete git. Tehdidi gör/duy, geri çekilme alanını dene. 3 ile levye, 1 ile tüfek seç; R ile mevcut mühimmat üzerinden reload dene. Hasar almak zorunlu değil.
5. Aynı adayın ayrı oturumunda batı/clinic yaklaşımını kullan. Görüş bariyerlerinin tehdit yönetimini değiştirip değiştirmediğini kaydet. Kapı, giriş ve dönüşte takılmaları konumuyla yaz.
6. Maintenance'dan tek fuse, wrench, scrap ve fuel al. Inventory yönetimi ve world pickup gerçek arayüzle yapılmalı. Sigortayı erken aldığın ayrı koşuda radyoyu sonradan okuyarak ilerlemeyi doğrula.
7. Güneydoğu relay'e git. E ile üç saniyelik onarımı başlat. Bir denemede uzaklaşarak iptal et; fuse kaybolmamalı. Sonra tamamla.
8. Cabin'e yürüyerek dön. Return terminaliyle gir, radyoyu dinle. Reward tekrar verilmemeli.
9. Sığınak socketlerinde Claim ve Install işlemlerini uygula; üç modül toplam 6 scrap kullanır. Storage'a malzeme, fuel ve wrench koy. Workbench'ten craft başlat, fuel ekle, generator aç. Aktif iş sırasında save → menu → load dene.
10. Başlangıç storage 4 slot. Dördüncü slotu farklı item ile doldurup çıktı alma reddini gözle; çıktı korunmalı. Bir slot açıp tam 10 rifle ammo al; tekrar alma reddedilmeli. Upgrade 6 scrap tüketir. Gerçek output/stash miktarlarını kaydet.
11. Yaralandıysan kurulmuş yatakta E ile dinlen; yalnız desteklenen sağlık/ıslanma etkilerini raporla. Tıbbi malzeme veya su tüketimi uygulanmış varsayılmamalı.
12. Save → menu → uygulamayı kapat → yeniden aç → menu → Load checkpoint. Inventory, sağlık, world loot, ölü düşman, craft ve görev aynı duruma dönmeli.
13. Ayrı checkpoint varyantlarında ilk sefer öncesi, sığınak sonrası, görev sırasında, fuse taşırken ve inventory transferinden sonra öl. Menu → Load veya New Game yolunu kullan. Tekrar ölümde checkpoint değişmemeli, ekipman/fuse çoğalmamalı.
14. İsteğe bağlı maintenance portalından iki cell'e geç; yüklenirken boş alana erişim olmamalı. Bu portal geçişini yürüyüş süresinden ayrı kaydet.

Toplam hedef 30–45 gerçek dakika. Duraklama, yardım, her fixture/debug müdahalesi ve blocker süresi ayrı yazılmalı; hedefi tutturmak için boş bekleme eklenmemeli. Video/screenshot yanında ilgili save ve log da bağlanmalı. Ajan otomasyonu bu bölümün NOT_RUN durumunu değiştirmez. Harici insan playtesti S018 kapsamında ayrı faaliyettir.

## Kayıt şablonu

Build/hash, tester, cihaz, rota A/B, başlangıç/bitiş, aktif oyun süresi, pause, yardım, ilk tehdit/ilk kaynak, combat, inventory, relay, shelter/craft, quit/load, ölüm varyantları, hata şiddeti/tekrar üretim, kanıt yolları, genel PASS/FAIL. Eğlence ve yatırım kararı Burak'a aittir.
