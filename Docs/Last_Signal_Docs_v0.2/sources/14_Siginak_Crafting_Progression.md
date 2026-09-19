---
doc_id: LS-DOC-14
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Sığınak, crafting, elektrik ve ilerleme sözleşmeleri

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §16, §18. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Sığınak kimliği

Mevcut binalar claim edilir; serbest voxel/yapı inşası ilk ürün kapsamında değildir. Sığınak bir utility düğümüdür: storage, bed, water, workbench, power ve radio. Aynı mekân her yeteneği ücretsiz sağlamaz; yer, risk ve kaynak nedeniyle uzmanlaşır. Linked state otomatik fast travel demek değildir. İlk kapsamda hızlı seyahat yoktur; ağ bilgi ve lojistik faydası sağlar.

**REQ-SHL-001:** Claim, dünyadaki zombileri silmez veya görünmez dokunulmazlık küresi yaratmaz. Güvenlik uygun kapılar, yakın threat kontrolü ve koruma modüllerinden hesaplanır.

## 2. State ve modüller

Claimed, Secured, Supplied, Powered, Linked GDD basamaklarıdır. Teknik model tek geri dönmeyen enum olmamalı: elektrik kesilince sığınak Powered faydasını kaybeder ama sahiplik ve storage kalır. unlockedCapabilities + currentOperationalFlags ayrılır.

ModuleInstance; stable ID, definition, installed socket, condition, enabled, fuel/charge ve lastProcessedWorldTime taşır. Placement authored socket/alanla sınırlandırılır. Collider overlap, doorway/nav blockage ve required clearance kontrol edilir. Geçersiz placement malzeme tüketmez.

## 3. Craft transaction

Crafting iki ölçeklidir. Bandage gibi kısa elde üretim simulation time; uzun workbench işi world time ilerletme kullanabilir. Her recipe timeBasis alanını belirtir. Malzeme seçimi explicit veya deterministik “en düşük uygun quality önce” politikasına bağlıdır; kullanıcının favorite/quest item'i otomatik sökülmez.

Akış: recipe/knowledge/station validate → source revisions kontrol → malzemeyi transaction escrow'a taşı → job oluştur → timer → output üret + escrow tüket → complete receipt. Pending job save'de saklanır. İptalde yalnız tüketilmemiş escrow döner; progress kaybı UI'da görünür. Slice için aşamalı resource consumption yerine tek completion commit tercih edilir.

**REQ-CRF-001:** İptal, save/load ve output capacity failure malzeme çoğaltamaz veya yok edemez. Output için ayrılan yer dolmuşsa CompletedWaitingOutput state veya workstation output container kullanılır; sonuç item'i silinmez.

## 4. Tarif örnekleri

| Recipe | Girdi | İstasyon | Süre tabanı | Sonuç |
|---|---|---|---|---|
| Clean bandage | 2 cloth + antiseptic charge | El | 5 sim s | 1 bandage |
| Boiled water | 500 ml dirty water + fuel | Ateş + pot | 10 world min | 500 ml clean water |
| Door brace | 2 plank + nails + tool | Shelter | 15 world min | Door module |
| Tool repair | Scrap + tape + repairable tool | Workbench | 20 world min | Condition restore |
| Relay repair | Fuse + cable + hand tool | Relay | 10 world min | Objective receipt |

Bunlar tasarım örnekleridir; maliyetlerin kanonik sürümü balance dosyasındadır. Sıvıyı kaynatırken container kapasitesi ve contamination state aynı transactionda değişir. Heat source erken kapanırsa recipe askıya alınabilir, tamamlandı sayılmaz.

## 5. Power ve fuel

Generator capacityW ve consumer demandW karşılaştırılır. İlk model priority list ile deterministic load shedding yapar; fiziksel şebeke simülatörü değildir. Fuel burn world hour'a göre hesaplanır. Consumer etkinlikleri radio, light ve workshop olarak başlar; buzdolabı spoilage çarpanı alpha hedefidir.

Generator noise sürekli source olarak pressure'a katkı verir. Elektrik açmak her kullanıcının tercih ettiği tek doğru yol olmamalı: sessiz battery kısa süreli alternatif, ateş ısı avantajı verir. Fuel bittiğinde enabled flag ile operational state ayrılır. Oyuncu tekrar yakıt eklediğinde sistem definition davranışına göre açılır; otomatik start kararı açıkça gösterilir.

**REQ-SHL-002:** Unloaded shelter'daki yakıt, hava ve jobs world time boyunca doğru ilerler. Yeniden load “full fuel” veya tamamlanmamış sonsuz job üretmez.

## 6. İlerleme

Gear daha fazla kaynak taşıma, knowledge yeni tarif/rota, shelter daha iyi recovery, world access yeni POI sağlar. Skill familiarity küçük verim bonusudur; ana bölgeyi açmak için anlamsız tekrar grind gerekmez. İlk ürün en az bir “daha çok damage” dışı erişim ödülü sunmalıdır: pry tool, radio intel veya yağmur koruması.

Üs saldırıları ilk slice zorunlu değildir. Pressure yüzünden her gece otomatik baskın sığınak rahatlığını yok eder. Sonradan eklenecek raid için warning, karşı oyun, storage kayıp kuralı ve offline simülasyon ayrıca tasarlanır.

## 7. Kabul

Craft aynı itemi iki job'a reserve edemez. İptal sonrası escrow doğru döner. Full output container'da sonuç kaybolmaz. Fuel bittiğinde sleep kesilir veya sıcaklık sonucu doğru hesaplanır. Generator iki kere register olmaz. Sığınak unclaim storage'ı yok etmez; sahiplik değiştirme gelecekteki co-op konusudur. [Save](17_Save_Load_Migration.md) ve [zaman](15_Time_Weather_Environment.md) ile çapraz test zorunludur.
