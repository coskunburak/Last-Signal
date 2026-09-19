---
doc_id: LS-DOC-10
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Combat, silah state machine ve hasar hesabı

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §12, §9; Ek B. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Temel sözleşme

Oyuncu tek bir yavaş zombiye karşı yeterli hazırlıkla güçlü; kalabalık, bitkinlik ve yanlış pozisyonda kırılgandır. Combat sonucu hit detection, weapon data ve hedef state ile belirlenir. Animation/SFX sonucu bildirir.

**REQ-CMB-001:** Bir ateş komutu ancak Ready state, geçerli ammo ve eylem izni varsa kabul edilir. Kabul ammo tüketimini ve shotId üretimini aynı committe yapar; reddedilen ateş NoiseEvent üretmez.

## 2. Silah yaşam döngüsü

Unequipped → Equipping → Ready. Ready'den Firing, Reloading, ClearingJam veya Unequipping'e gidilir. Dead/Stunned ilgili cancel politikasını uygular. Her async animation callback weapon instance ve action generation numarasını taşır; eski silahın reload callback'i yeni silahı dolduramaz.

Magazine, chamber ve loose ammo üç ayrı miktardır. GDD'nin “tactical reload mermi kaybettirmez” hedefi inventory kapasitesiyle birlikte ele alınır. Dolu eski magazine inventory'ye sığmıyorsa reload başlamaz veya açık drop-old-mag seçeneği sunulur; ammo sessizce yok olmaz.

## 3. Reload commit noktaları

| Aşama | State değişimi | İptalde kalan |
|---|---|---|
| Reserve | Yeni magazine/rounds kullanım için ayrılır | Rezerve item serbest bırakılır |
| Remove | Eski magazine çıkar, belirlenmiş owner'a geçer | Silah magazinesiz kalabilir |
| Insert | Yeni magazine silaha geçer | Takılmış magazine kalır |
| Chamber | Magazine'den bir round chamber'a geçer | Hazır chamber korunur |
| Ready | Action lock kalkar | Son durum kullanılabilir |

Slice ilk implementation basit round pool olabilir; bu durumda staged magazine davranışı varmış gibi gösterilmez. Tam magazine modeline geçiş migration ve inventory testleriyle ayrı görevdir. Prototype ve production modelinin karışması ammo duplication'a yol açar.

**REQ-CMB-002:** Reload iptal/save/load/equip change sırasında toplam ammo yalnız gerçek ateş veya açık discard ile azalabilir; kendiliğinden artamaz.

## 4. Hit detection

Camera ray nişan hedefini seçer. Muzzle/weapon origin'den hedefe ikinci ray/sweep duvar engelini kontrol eder. Duvar dibindeki oyuncu camera açık görüşü var diye namlusu duvar içinden ateş edemez. Shotgun pelletleri aynı shotId altında alt hitId alır; pellet grouping damage raporunda tek shot olarak sayılır.

Melee sweep yalnız active hit window'da çalışır. Her swingId hedef başına en fazla tanımlı hit sayısı üretir; frame sayısına bağlı çoklu hasar yoktur. Collider-child sayısı daha yüksek zombiye katlanan damage uygulanmaz. Friendly/self layers açık filtrelenir.

## 5. Hasar sırası

Önerilen sıra: baseDamage × hitZoneMultiplier × conditionEffect → armor mitigation → finalDamage clamp → health mutation → injury severity → stagger. Armor penetration 0..1 katsayıdır, gerçek balistik fizik iddiası değildir. Basit model: effectiveArmor = armor × (1-penetration); mitigation = clamp(effectiveArmor/(effectiveArmor+K),0,maxMitigation). K balance parametresi. Head multiplier GDD 2.5-4.0 aralığındadır; slice Shambler için 3.0 başlangıçtır.

Overflow veya negatif damage healing'e dönüşmez. Healing ayrı command'dır. Immunity, environment damage ve fire tick aynı DamageInfo şemasından geçer. Kill attribution son vuruş/assist gelecekte genişletilebilir; şimdi sadece doğru damage source kaydı gerekir.

## 6. Melee rolleri

Knife düşük reach, düşük stamina, hızlı recovery; crowbar orta reach, yüksek stagger, door utility; axe yüksek damage, uzun commitment. Shove hasardan çok alan açar; diminishing stagger ve cooldown sonsuz stunlock'u önler. Bitkin oyuncu normal light attack yapabilir ama heavy reddedilir. Animation telegraph hedefin oyuncuya hangi anda vuracağını okunur kılar.

## 7. Firearm hissi

Aim spread hareket, stance, restedness, pain ve aim settle ile değişir. Recoil görsel kick ile simulation aim offset ayrılır; accessibility shake kapatma silah spread'ini değiştirmez. Suppressor noise radius'ünü düşürür ama sıfırlamaz. Misfire tam slice'a şart değildir; condition sistemi yeterince anlatılmadan random jam açılmaz.

**REQ-CMB-003:** Kamera sarsıntısı ve gore kapatıldığında gameplay hasarı, hit timing ve AI hearing değişmez. Görsel seçenekler oyunun authority katmanına yazamaz.

## 8. Test matrisi

Duvar dibinde fire, 0 ammo, chamber=1 magazine=0, reload her aşamada iptal, save after magazine remove, weapon drop during reload, 30/120 FPS melee, aynı hedefte beş collider, headshot helmet, corpse'a attack ve stun sırasında buffered fire. Kabul raporu ammo conservation ve hit dedupe invariantlarını açık sonuçla gösterir. Combat hissi için gerçek build video ve oyuncu görüşü gerekir; unit test tek başına “iyi combat” kanıtı değildir.
