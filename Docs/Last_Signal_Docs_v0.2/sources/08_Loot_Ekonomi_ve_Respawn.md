---
doc_id: LS-DOC-08
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Loot üretimi, kaynak ekonomisi ve tükenme

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §11, §24. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Tematik dağılım

Loot kategori beklentisi oluşturur: klinik ilaç, atölye alet, mutfak gıda, karakol ammo. Her yerden aynı random rarity tablosu çekilmez. Bir POI loot profili anchor itemler, optional valuables, destek ihtiyaçları ve düşük değerli clutter içerir. Clutter her zaman pickup değildir; anlamsız 300 nesne envanter derinliği sayılmaz.

**REQ-LOOT-001:** Kampanya görevini açan kritik parça yalnız rastgele çekilişe bağlı olamaz. Garantili yerleşim veya yeniden edinme fallback'i kayıtlıdır.

## 2. Deterministik üretim

Container ilk keşfedilirken world seed + stable container ID + loot table version + purpose salt üzerinden RNG stream oluşturulur. Platform bağımlı GetHashCode kullanılmaz; tanımlı hash/RNG algoritması seçilir ve fixture ile sabitlenir. Combat, weather ve loot ayrı stream kullanır; oyuncunun ateş etmesi dolabın loot'unu değiştirmez.

Üretim durumu: NotGenerated → Generated → Modified. İlk generation sonuçları save state olur. Generated boş container, NotGenerated ile aynı değildir. Save/load veya UI kapat-aç reroll yaratamaz. Loot table güncellemesi eski açılmış dolapları yeniden üretmez. Açılmamış dolaplar için world'ın catalog/loot version pin politikası açıkça seçilir; varsayılan world yaratıldığı tablo sürümünü korur veya migrationla taşır.

**REQ-LOOT-002:** First search retry aynı sonucu vermelidir. Generation başarıyla state'e girdikten sonra presentation/load hatası yeni roll tetikleyemez.

## 3. Çekiliş sırası

1. POI/context tag uygunluğu.
2. Guaranteed entries ve unique exclusions.
3. Slot draw count aralığı.
4. Weighted category, ardından item.
5. Quantity/condition/contamination roll.
6. Stack ve container capacity normalize.
7. Generated state commit, discovery event.

Negatif weight validation error; toplam weight sıfırsa explicit empty result veya error, division by zero değil. Replacement-without-repeat gerekiyorsa candidate havuzu bounded olur. Recursive nested loot table cycle buildde bloklanır. Sıfır şanslı anchor item yanlış definition sayılır.

## 4. Kaynak dengesi

Gıda ve su rutin seferi; ilaç hata sonrası toparlanmayı; ammo çatışma bütçesini; fuel gece/güç seçeneklerini; parts erişim yükseltmelerini taşır. Bir malzemenin yalnız tek tarifte kullanılması her zaman sorun değildir, ancak inventory yükü karşılığında işlevi anlaşılmalıdır.

Başlangıç seferi minimum bir su kaynağı ve basit tedavi sunar. Bunlar oyuncu ölüm ihtimalini sıfırlamaz; öğrenmeye fırsat verir. Net sefer değeri yalnız para cinsinden hesaplanmaz: yeni bilgi, güvenli rota ve nadir alet de kazançtır. Item fiyatı varsa internal economy score; NPC shop eklenmiş sayılmaz.

Tükenme ile uzun sandbox arasında gerilim vardır. Kampanyada hızlı loot respawn yok. Finite kritik malzemeler ana sona yetecek safety margin ile tasarlanır. Aftermath sürdürülebilir kaynak/event restock ayrı kapsamdır; sonsuz oynanabilirlik pazarlama sözü verilmeden test edilir.

## 5. Depletion ilişkisi

Depletion, bir POI'nin initial loot opportunities tabanına göre tüketilen payıdır. Taşınan her item için sınırsız additive pressure yapılmaz. Aynı itemi geri koy-al döngüsü yeni tükenme yazmaz. Container first depletion receipt ve remaining supply metric kullanılır.

**REQ-LOOT-003:** Item drop/pickup, split/merge veya aynı container'a geri koyma reward/pressure farming üretemez. Loot acquired ile newly depleted world source ayrı eventlerdir.

## 6. Respawn ve corpse

Corpse loot sadece zombie ölümünde bir kez materialize olur. Actor respawn ve loot respawn farklı kavramlar. Yeni göç etmiş zombi kendi persistent ID'siyle gelir; temizlenmiş oda eski cesedi canlandırmaz. Corpses görsel cleanup sonrası loot varsa container proxy'ye dönüşebilir. Önemli oyuncu çantası genel corpse cleanup'a dahil edilmez.

## 7. Tuning ve kabul

Simülasyon raporu: 10.000 seed üzerinden kategori dağılımı, boş container oranı, kritik gate reachability ve sefer kaynak dengesini inceler. Bu, eğlencenin otomatik doğrulaması değildir. İnsan testinde oyuncunun neyi bıraktığı, hangi itemi hiç kullanmadığı ve neden geri döndüğü sorulur.

BDD: Given generated-empty fridge, When save/load, Then hâlâ boş. Given unique relay fuse collected, When yeniden arama, Then ikinci fuse oluşmaz. Given table version changes, When old save opens, Then belgelenmiş migration dışında dünya yeniden roll olmaz. Bağlantılar: [balance](23_Balance_Katalog_ve_Ekonomi.md), [save](17_Save_Load_Migration.md).
