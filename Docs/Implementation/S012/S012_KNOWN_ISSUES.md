# S012 sorun ve risk kaydı

## S012-001 — Başlangıç checkpoint'i yüklenemiyor (S1, düzeltildi; hedefli tekrar PASS)

Tekrar: yeni IntegratedGraybox oturumu → hemen save → ölüm → menu/load. Başlangıç (-359.8,-140) radyo collider'ıyla çakışıyordu. Save capture kabul ediyor, hydrate kapsül güvenliği reddediyordu. Kaynak S011 spawn/radio yakınlığının ölçekten bağımsız çakışması. Düzeltme yalnız yeni aday spawn'ını mevcut `ShelterLoop.InsideAnchor` (-358.2,-139) noktasına alır; SaveValidation gevşetilmedi. Önce: `20261002T234046Z/focused-play-r1.xml` 5/6. Sonra: `focused-play-r2.xml` 6/6. Eski S011 sahnesinin koordinatları değiştirilmedi; oradaki başlangıç spawn/radio yakınlığı ayrı tarihsel içerik riski olarak kalır.

## S012-002 — Kanonik alan/rota yok (S1 kabul açığı, uygulandı; insan kabulü bekliyor)

Başlangıç RelayExpedition ana zemini 24×26 m idi. Mevcut S011 integration PASS bunun 400×400 m veya 30–45 dakika olduğunu kanıtlamıyordu. Yeni aday 400×400 m, beş resident yapı ve iki farklı yaklaşım içerir. Kaynak sahne korunur. Önce: giriş denetimi; sonra: S012SceneTests ve motor traversal çıktıları. Yeni alanın yoğunluğu/eğlencesi insan koşusu olmadan doğrulanmış sayılamaz.

## S012-003 — Eski not yeni rotayı tarif etmiyor (S2, düzeltildi)

S011 sabit metni “yard south edge” diyordu. Yeni haritada bakım kuzeydoğu, röle güneydoğuda. RelayMission'a serialized sunum metni eklendi; boşsa eski Clue fallback korunur. Yeni sahne kendi notunu taşır. Görev otoritesi/receipt değişmedi. Clue-first/fuse-first testleri ve eski S011 regresyonu birlikte doğrulanmalı.

## Test altyapısı bulguları

- 2026-10-03 ilk D118 standalone performans koşusu FAIL: snapshot saati değiştirilirken ShelterProduction son işlenen saati taşınmamış. 307.020 `Noncontiguous production clock` istisnası, 317 MB log ve geçersiz kare/GC ölçümleri oluştu. Fixture dünya katılımcılarını hedef saate ilerletecek şekilde düzeltildi; `20261003T103218Z-performance-retry` yeni build ile Errors=0 ve PASS capture completed verdi; eski ölçümler geçersiz kalır. Başarısız dosyalar tarihsel kanıt olarak korunur.
- 2026-10-03 ilk kullanıcı tam PlayMode tekrarı 200/204 bitti; bu başarısız XML korunur. `AmmunitionIntegrationTests.ActualShotsMissWallEmptyAndRejectedRequests` 180 s timeout ve Unity Account API/odak uyarısı verdi. Üç `ZombieDamagePlayTests` hatası uzun profiling sırasında oyuncunun hasar alması, hareketli hedefe cooldown ışını ve kısa attack fazlarının kare sınırında atlanması olasılıklarına karşı fixture düzeyinde ayrıştırıldı. Düzeltme sonrası mühimmat 1/1 ve ZombieDamage 15/15 hedefli tekrar PASS; ardından `20261003T100356-546593Z/play.xml` tam PlayMode 204/204 PASS verdi. Dört test bu tam koşuda da geçti; regresyon bulgusu yeniden doğrulandı. Aralıklı hataların tekil kök nedenleri kesin kanıtlanmış sayılmaz.
- Levye testi camera transform'unu bir kez çeviriyor, gerçek FirstPersonLook sonraki frame'de bunu geri alıyordu. Düzeltme fixture'da oyuncu yaw ve public look API'sini kullanır; oyun hit/range/stamina değişmedi.
- Dönüş testi röle collider'ının merkezine yürümeyi istiyordu. Düzeltme waypoint'i etkileşim mesafesindeki 1.5 m yan noktaya taşır; collider kaldırılmadı.
- Bunlar oyuncu hatası veya “üç gameplay blocker” olarak sayılmaz. Başarısız XML'ler korunur.

## Açık değerlendirmeler

- **NOT_RUN, zorunlu kabul:** Yardımsız gerçek build'de 30–45 dakika; tek oturum hazırlık/keşif/combat/dönüş/craft/uygulamayı kapat-aç/load. Burak checklist'i gerekir.
- **S2 tasarım riski:** Geniş resident alanda tek authored zombie var. Regional pressure/materialization mevcut iki portal cell'de işler. Bütün resident bölgede nüfus yayılımı uygulanmış değildir. Dönüş baskısının yeterliliği gerçek koşuda değerlendirilmelidir; yeni paralel spawn authority eklenmedi.
- **S2 içerik riski:** Tekrarlanan sade duvarlar ve text işaretleri yön bulmaya yetmeyebilir. Gece landmark okunurluğu ve rotanın tempo değerlendirmesi bekler.
- **S2 HUD okunurluğu:** D118 `day.png`/`night.png`/`rain.png` görüntülerinde sabit üst başlık ile stamina satırı ve alt kontrol ipuçları ile görev kutusu kısmen çakışıyor (1280×800). Teknik akış etkilenmedi; yardımsız koşuda etkileşim ve görev okunurluğu özellikle değerlendirilmeli. S013 görsel cilası varsayılmadan S012 ürün kararında açık risk olarak tutulur.
- **S2 uyumluluk sınırı:** Yeni aday `world.s012.graybox / s012-graybox-v1`; eski world checkpoint'leri yanlış koordinatlarda açılmak yerine reddedilir. Save şeması aynı. Eski world içeriği için migration iddiası yok.
- **S2 mevcut sistem sınırı:** Water/food/medical loot, tüketim/hunger/treatment sistemi anlamına gelmez. Fiili sağlık toparlanması yatakta dinlenmedir. Bu içeriklerin oyuncuya vaadi sonraki ürün değerlendirmesinde netleştirilmeli.
- **S3 graybox:** Primitive bina/işaretler; görsel kalite S013, animasyon S014, atmosfer S015 alanıdır.

S0/S1 açık sayısı son regresyon ve insan koşusu olmadan sıfır ilan edilmez. Tamamlanmayan canonical iş S013'e taşınmış sayılmaz.
