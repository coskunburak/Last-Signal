---
doc_id: LS-DOC-02
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Ürün vizyonu, oynanış döngüleri ve dünya tasarımı

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §1-6, §15-17. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Oyun vaadi ve oyuncu rolü

Oyuncu terk edilmiş kırsal bir ilçede kaynak toplayan, tehlikeyi okuyarak seferler planlayan ve yaşam alanını genişleten bir hayatta kalandır. Hazırlık ve bilgi savaş gücü kadar önemlidir. İlk hedef temiz su ve gece barınağı; orta hedef güvenilir ikmal rotaları; uzun hedef radyo altyapısını geri kazanıp bölgenin kaderi hakkında karar verebilmektir.

World Pressure ve sığınak ağı, GDD'nin önerdiği ayırt edici sistemlerdir. Bunlar satış başarısı kanıtı değildir. Oyuncu rutinleri değişen tehdit ve tükenen kaynaklarla karşılaşır; aynı zamanda büyüyen bilgi ve lojistik olanakları sayesinde güçlenir. Dünya oyuncuya karşı hile yapıyormuş hissi üretirse signature sistem sadeleştirilir.

**REQ-VIS-001:** Her temel özellik hazırlık, risk değerlendirme, keşif, toparlanma veya ilerleme kararlarından en az birine hizmet eder. Yalnız menü sayısını artıran sistem ilk slice'a alınmaz.

## 2. Tasarım ilkelerinin sınanması

| İlke | Somut davranış | Başarısız örnek |
|---|---|---|
| Hazırlık kazandırır | Hava uyarısı sonrası yağmurluk seçmek avantaj verir | Fırtına uyarısız anlık hasar verir |
| Dünya hatırlar | Açılan dolap boş kalır; kırılan cam kalıcıdır | Reload ile market dolar |
| Çatışmanın bedeli vardır | Mermi ve ses sonraki rotayı etkiler | Her zombiyi öldürmek daima en ucuz yol |
| Yoğun dünya | Yakın POI'ler alternatif yol sunar | On dakika boş yürüme |
| Yetkinlik ilerlemedir | Yeni alet, kapalı depoya eriştirir | Yalnız +% hasar artar |

## 3. Seferin tam sözleşmesi

Hazırlıkta oyuncu hedefini, tahmini süreyi, hava/ışık koşulunu, çıkış ve dönüş rotasını seçer. Oyuncuya sistem tarafından zorunlu bir kontrol listesi dayatılmaz; UI karar vermeyi kolaylaştırır. Envanterde görev malzemesi için boş alan bırakmak gerçek bir hazırlık seçeneğidir.

Yolculukta orman daha az düşman ama düşük görüş; ana yol daha hızlı ama açıkta kalma riski sunar. Her güzergâhta avantaj bulunur. İleride olmayan araç sisteminin varlığına göre harita tasarlanmaz.

POI içinde hedef item, destek loot ve risk katmanları ayrılır. Temel ihtiyacı karşılayan bir dış oda ile yüksek değerli kilitli oda farklı sefer uzunlukları üretir. Oyuncu erken dönerek yine bir başarı elde edebilmelidir.

Kriz; yetersiz stamina, yeni bir ses, kırılan kapı, fazla yük veya hava değişimi ile oluşabilir. Tek seferde tüm kriz sistemleri tetiklenmez. Tehdit cooldown'ı ve alternatif kaçış, yönetilebilir gerilim sağlar.

Dönüşte oyuncu kaynaklarını depolar, yarasını tedavi eder, aletini onarır ve bir sonraki hedefini oluşturur. Toplanan malzemenin sığınakta görünür bir faydaya dönüşmesi seferin ödülüdür. Envanter düzenlemek oturumun çoğunu almamalıdır.

**REQ-VIS-002:** Slice'ta aynı hedefe en az iki geçerli erişim yaklaşımı bulunur. Her iki yaklaşım da test edilir; yalnız tasarım çiziminde gösterilmesi yeterli değildir.

## 4. Tempo

Micro kararlar 2-10 saniye, encounter 30 saniye-4 dakika, tipik sefer 12-25 dakika, oturum 45-90 dakika tasarım hedefidir. Bunlar kronometreyle zorlanan bölüm uzunlukları değildir. Harita, 15 dakikalık oyuncuya doğal bir geri dönüş olanağı da verir. Güvenli alan rahatlığı tehdit kontrastını kurar.

Kontrollü 35 dakikalık slice rotası eğitim amaçlıdır; veteran oyuncu daha hızlı, keşifçi daha yavaş tamamlayabilir. Telemetry medyan ve uç değerleri ayrı raporlar. Birkaç geliştirici koşusu oyuncu davranışını temsil etmez.

## 5. Dünya kuralları ve kurgu

GDD'nin Pine Ridge, Ash Creek, Red Quarry, North Relay, Blackwater adları öneri toponimlerdir. İlçe, gerçek bir afet veya yaşayan topluluğun bire bir kopyası değildir. Zombi türleri okunabilir davranış rolleri taşır; salgın açıklaması mekanik tutarlılık bozulmadan değiştirilebilir.

Oyuncu geçmişi hafif tanımlıdır: yarıda kalan tahliye sonrası hayatta kalma. Anlatı notları konum bilgisi, kod, tarif veya karakter hikayesi sağlar. Geliştirici, kritik survival kuralını yalnız opsiyonel bir günlükte saklamaz. Lore okunmadan da ana mekanikler öğrenilebilir.

## 6. Bölgesel progression

Pine Ridge güvenli başlangıç ile shelter öğretir. Ash Creek yoğun iç mekân ve medical loot sunar. Red Quarry parçalar karşılığında gürültülü erişim problemi üretir. North Relay hava ve uzun görüş problemidir. Blackwater endgame için ilerleme ve hazırlığı sınar. Bu beş bölgenin hepsi slice'a yapılmaz; slice bir bölgede temsili işlevler sunar.

Erişim kapıları yalnız invisible wall değildir. Kırık köprü, eksik alet, hava koşulu veya bilgi gereksinimi okunur. Oyuncu erken keşifle bazı kapıları aşabiliyorsa objective sistemi bunu kabul eder. Zor alanlara giren düşük ekipmanlı oyuncu otomatik stat cezasıyla cezalandırılmaz; gerçek tehlikeler yeterlidir.

## 7. Ürün sınırı

Single-player, Windows ticari hedef ve Mac geliştirme akışı GDD çalışma temelidir. Mac'te çalışan build Windows sürümünün test edildiği anlamına gelmez. 1.0'da araçlar, NPC kolonisi, PvP ve sınırsız serbest inşa başlangıç kapsamına dahil değildir. Gelecekteki genişlemeler bugünkü veri tasarımını gereksizce ağırlaştırmamalıdır.

**REQ-VIS-003:** Ana ilerleme bir loot RNG sonucuyla kalıcı kilitlenemez. Kritik araç/parça için garantili kaynak veya açık recovery yolu gerekir.

## 8. Kabul ve bağlantılar

Oyuncu ilk sefer sonunda kazandığını ve sıradaki hedefini anlatabilmeli. Ölüm sebebini kaynak, konum veya uygulama hatası olarak açıklayabilmeli. Pilot playtestte oyuncu ne olduğunu anlamıyorsa önce geri bildirim, sonra denge değiştirilir. Sistemler: [pressure](12_World_Pressure.md), [sığınak](14_Siginak_Crafting_Progression.md), [slice senaryosu](21_Vertical_Slice_ve_Playtest.md).
