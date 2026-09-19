---
doc_id: LS-DOC-06
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Etkileşim, kapılar ve dünya eşyaları

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §7, §10-11. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Resolver

Oyuncunun view origin'inden uygun layer'lara bounded ray/sphere query yapılır. Sıralama önceliği görünürlük, mesafe, bakış açısı, explicit interaction priority ve stable tie-breaker'dır. Standart menzil 2.2 m. Büyük objede pivot yerine interaction anchor veya collider closest point kullanılır. Bir dolabın pivotu duvar arkasında diye erişilebilir kolu reddedilmemelidir.

**REQ-INT-001:** HUD prompt ile commit aynı eligibility politikasını kullanır. Commit anında world revision/mesafe/occlusion yeniden doğrulanır; eskimiş preview hak vermez.

Hedef değişiminde kısa hysteresis titremeyi azaltabilir. Hysteresis menzil dışındaki veya duvar arkasındaki hedefi tutamaz. UI “Aç” gösterirken başka bir itemin pickup'ına dönmek kabul edilmez. Operasyon busy ise prompt sebebiyle disabled olur.

## 2. Interaction teklif verisi

Offer alanları: targetId, actionId, labelKey, iconKey, durationSeconds, holdPolicy, requiredToolTags, disabledReason, expectedRevision. Bu bir immutable preview'dur. Başarılı interaction ayrı Result üretir. IInteractable her kapıya komple inventory/crafting davranışı koymaz; hedef yalnız yapabildiği teklifleri bildirir, servis eylemi yürütür.

| Aksiyon | Süre/commit | İptal davranışı |
|---|---|---|
| Pickup | Aynı simulation adımında | Commit öncesi atomik red |
| Open door | Başlatma/engel kontrolü | Engel varsa state açıldı sayılmaz |
| Search | Kısa progress; sonuç materialize | İlk search seed sabit, reroll yok |
| Force open | Tool + progress | Commit öncesi item tüketimi yok; efor yine harcanabilir |
| Dismantle | Açık hold onayı | Yanlış input geri alınabilir |
| Treatment | İlgili wound transaction | Tedavi dosyasındaki commit aşaması |

## 3. Pickup ve drop transaction

Pickup: entity still exists → capacity check → instance ownership değiştir → world representation consumed/tombstone → events. World GameObject Destroy çağrısı gerçek ownership transferinin yerine geçmez. Save araya girse tutarlı bir snapshot görür. Presentation silinemediyse world item tekrar pickup edilemez; ID zaten başka owner'dadır.

Drop: güvenli yakın yer seç → collision/ground doğrula → yeni world owner yarat → inventory çıkar → state commit → prefab göster. Prefab instantiate commit öncesi başarısızsa drop reddedilir. Commit sonrası presentation başarısızsa world owner korunur, pending representation/proxy ve retry kullanılır; başarı eventleri yayımlandıktan sonra sessiz rollback yapılmaz. Eşya kaybolamaz. Dünya sınırı veya kapalı duvar içinde bırakma reddedilir.

**REQ-INT-002:** Aynı world item iki komutla istenirse en fazla biri başarılı olur. Başarısız drop/pickup toplam item miktarını değiştirmez.

## 4. Kapı state modeli

OpenFraction (0..1), lockState, integrity, barricadeModules, hinge configuration ayrı alanlardır. Açık/kapalı animasyonu ile locked durumu aynı enum'a sıkıştırılmaz: yarı açık kırık kapı ifade edilebilir. Slice'ta sabit açık/kapalı uç değerler ve broken mode yeterlidir; ara animasyon transient olabilir.

Kapı açılırken capsule sıkıştırma riskine karşı sweep/overlap yapılır. Locked door bir key tag, pick veya pry çözümü sunar. Başarı sonrası anahtar tüketilip tüketilmeyeceği definition'da açık; varsayılan anahtar kalıcıdır. Force open noise bir kez committe ve gerekirse çalışma ritminde üretir; her render frame'de 20 m olay yayımlanmaz.

Broken state nav engelini günceller. NavMesh bake'i her kapı açılışında çalıştırılmaz; link/obstacle politika değişir. Zombi kapıyı kırabilir fakat oyuncunun görüşü dışında kapı kendiliğinden resetlenmez.

## 5. Pencere ve kırık yüzey

Intact → broken → cleared-glass geçişleri önerilir. Kırma ses + risk üretir; clearing için eldiven/alet avantajı olabilir. Broken state görsel, traversal ve save açısından aynı karardır. Cam particle'ları kalıcı binlerce rigidbody olmaz; kısa fizik/pooled debris ve kalıcı decal yeterlidir.

## 6. Yükleme ve hedef ömrü

Interaction süresince hedef cell için kısa lease alınır. Cell unload isteği etkileşimi güvenli iptal eder veya lease bitene kadar bekler; pointer boşa düşmez. Scene callback'i geç döndüğünde eski session target'ına işlem uygulanmaz. TargetUnavailable geri bildirimi modalı kapatır ve odağı oyuncuya döndürür.

## 7. Kabul

**REQ-INT-003:** Duvar arkasından loot, açık kapı üzerinden yanlış container seçimi ve kapının içine item bırakma testte reddedilmelidir. İki komutta tek pickup, full bag red, blocked drop, held interaction sırasında hasar, pause, target destroy ve unload senaryoları ölçülür. Hot query allocation testi yalnız warmed resolver üzerinde yapılır; yükleme sırasındaki allocationla karıştırılmaz.

İlişkiler: [veri sözleşmeleri](04_Veri_Kimlik_Command_Event.md), [inventory](07_Item_Inventory_Equipment.md), [streaming](13_World_Streaming_POI.md).
