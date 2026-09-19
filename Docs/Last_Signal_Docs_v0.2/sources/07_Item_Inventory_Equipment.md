---
doc_id: LS-DOC-07
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Item, envanter, ekipman ve taşıma kapasitesi

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §10; Ek B. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Container modeli

ContainerState; ID, owner, slotCount, maxMassGrams, stack listesi, revision ve erişim politikası taşır. Player, dolap, sandık ve corpse aynı domain işlemlerini kullanır. Equipment slotları restriction taşır; silahı food slotuna koyan UI hatası domain tarafından da reddedilir. Dünya eşyası da ownership hesabına dahildir.

**REQ-INV-001:** Her ItemInstanceId aynı anda yalnız bir owner'a aittir. Transfer kaynak ve hedefi tek transactionda değiştirir. UI'da listeden silmek ownership değişikliği değildir.

İlk kapsam nested backpack değildir. Çanta kapasite sağlayan equipped container'dır; içinde başka dolu backpack taşımaya izin verilmez. Böylece recursive weight, cyclic container ve yük azaltma exploit'i baştan sınırlanır. Gelecekte nested support ayrı migration gerektirir.

## 2. Operasyon semantiği

| İşlem | Ön koşul | Başarı sonrası |
|---|---|---|
| Add | Yeterli slot/kütle | Stack oluştur/birleştir, revision artır |
| Split | 0 < amount < quantity | Yeni instance; toplam miktar sabit |
| Merge | Uyumlu stack state | MaxStack aşılmaz |
| Transfer | İki revision güncel | Tek owner değişimi |
| Equip | Uygun slot ve eski eşya için yer | Swap atomik |
| Consume | Geçerli hedef stat/effect | Charge/quantity azalır ve sonuç aynı committe |
| Drop | Güvenli world konumu | World owner'a aktar |

Transfer varsayılanı all-or-nothing. “Sığanı al” ayrı explicit action'dır ve taşınacak miktarı gösterir. Take All batch'i her item için deterministik sıra kullanır; alınamayanları sebebiyle listeler. Bütün batch'in tek transaction olması zorunlu değildir fakat sonuç item bazında doğru olmalıdır.

## 3. Ağırlık ve hız

Unit mass integer gramdır; quantity × mass overflow için güvenli aralık kontrolü bulunur. Su volümü, ammo ve attachment kütlesi eşyanın effective mass'ına dahildir. Equipped itemlerin ağırlığı yok sayılmaz. Backpack bonusu max mass ve slot sayısını ayrı artırabilir.

GDD bantları: Light <50%, Loaded 50-80%, Heavy 80-100%, Overloaded 100-120%, Blocked >120%. Bu paketin sınır politikası: %50 Loaded, %80 Heavy, %100 Overloaded, %120 tam sınır kabul; %120 üstü yeni pickup yasak. Band değişiminde UI ve movement aynı kaynak değeri okur.

**REQ-INV-002:** Bag küçültme veya çıkarma eşya silemez. Yeni kapasite yeterli değilse işlem reddedilir ve oyuncuya önce boşaltma/drop seçeneği gösterilir. Otomatik dağılma slice dışında kalır.

## 4. Quick slot ve equipment

Quick slot item kopyası değil instance reference veya açık category binding'dir. Consumed instance yok olduğunda binding temizlenir. Aynı türden otomatik yenileme seçenekse UI işaretler. Equip state: Unequipped → Equipping → Ready → Unequipping. Fire yalnız Ready'de geçerli. Slot değiştirirken reload/consume commit kuralı ilgili domain'e danışır.

Armor/head/coat/gloves/boots/backpack ayrı slotlar olabilir. İç-dış torso katmanı önce görsel/oynanış faydası kanıtlanınca genişletilir. Sayısız slot UI maliyeti yaratır. Slice önce coat, boots, backpack ve weapon slots ile başlayabilir.

## 5. Condition ve repair

Condition 0..1000. Silah kırılması generic “item yok et” değildir; Broken tag ile onarılamaz/onarılır politika belli olur. Tool condition craft için gerekense işlem başında uygunluk kontrol edilir. Repair restoredCondition ve materialCost aynı committe uygulanır. MaxCondition düşüşü bazı değerli silahlara özgü opsiyondur, bütün itemlerde zorunlu değildir.

Mühimmat magazine/chamber farklı payload'dır. Weapon değiştirince reload kısmi durumu kaybolmaz. Detay [combat](10_Combat_Weapons_Damage.md).

## 6. UI sözleşmesi

Drag-drop yalnız request üretir. Başarıya kadar ghost preview vardır; authoritative sonucu gelince liste yenilenir. Başarısız işlem kaynak itemi eski yerine döndürür. Keyboard/gamepad ile drag olmadan bütün işlevlere erişilir. Hover karşılaştırma donmuş snapshot okur; frame başına tüm inventory'yi sıralamaz.

**REQ-INV-003:** Capacity red, stale revision, missing target ve slot restriction farklı hata kodu olmalıdır. Hepsini “işlem başarısız” göstermek debug ve oyuncu öğrenmesini bozar.

## 7. Save ve test

Container contents, equipped bindings, charges, contamination ve revisions kaydedilir. Load sırasında hesaplanabilir weight türetilir; kaydedilmiş cache değerine kör güvenilmez. Duplicate instance save'de bulunursa sessizce birini silmek yerine doğrulama/recovery hatası verilir.

Testler: split+merge roundtrip; 1 gram fazla yük; tam stack sınırı; iki revision conflict; consume son charge; equip swap yer yok; dolu bag çıkarma; quick slot deleted item; interrupted reload weapon transfer; repeated save/load. Property testi toplam miktar ve ownership invariantlarını rastgele geçerli operasyon dizilerinde doğrular. Başarı sayısı değil bu risklerin kapsanması önemlidir.
