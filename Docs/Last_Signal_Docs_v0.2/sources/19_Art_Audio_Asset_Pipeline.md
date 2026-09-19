---
doc_id: LS-DOC-19
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Sanat yönetimi, asset üretimi, animasyon, VFX ve ses

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §20. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Stil hedefi

Stilize gerçekçilik: büyük okunur şekiller, orta frekansta kontrollü doku, doğal düşük doygunluk ve sıcak shelter kontrastı. The Long Dark atmosfer referansı, bire bir shader/asset tarifi değildir. URP seçimi tek başına low-poly veya painted görünüm vermez. Ortak palette, materyal standardı, silhouette ve lighting gerekir.

**REQ-ART-001:** Asset kabulü tek başına güzel görünmesine göre değil benchmark sahnesinde diğer assetlerle uyumuna göre yapılır. Aynı ölçek/ışıkta karşılaştırılmadan marketplace paketi üretim standardı sayılmaz.

## 2. Benchmark sahnesi

Bir ev dışı+oda, yol, üç ağaç, rocks, player hands, zombie, flashlight, bir kapı ve loot prop. Gün, gece, yağmur profilleri. Renk paleti, exposure ve material roughness burada kalibre edilir. Golden screenshot'lar sürümlü tutulur. Rastgele her scene'de farklı postprocess ile sorun kapatılmaz.

Çalışma palette önerisi: forest #40564A, cold fog #899B9D, wood #75604B, metal #606765, hazard rust #A84B2A, shelter amber #D29B3D. Hex kodlar final art bible kararı değildir; referans başlangıcıdır. Tehlike rengi yalnız HUD için değil çevresel signifier olarak ölçülü kullanılır.

## 3. Import ve ölçüler

Birim metre; pivot floor-contact veya hinge amacına göre. Transform scale tercihen (1,1,1), unapplied negative scale hataları düzeltilir. Material slot sayısı en aza indirilir. Collider oyun geometrisiyle uyumludur; detailed render mesh otomatik her prop için collider yapılmaz. Static small props merged/instanced olabilir; interactable persistent entity ayrı ID taşır.

| Asset sınıfı | Başlangıç bütçe hipotezi | Not |
|---|---|---|
| Küçük loot prop | LOD0 300-2.000 triangle | Silhouette önemli |
| Furniture | 1.000-6.000 triangle | Screen size ile değerlendir |
| Zombie | 15.000-30.000 triangle | Skinning/animator maliyeti ayrıca |
| FP weapon | 15.000-40.000 triangle | Yakın ekran, material sayısı kontrol |
| Foliage | LOD+billboard gerekir | Overdraw triangle sayısından daha kritik olabilir |

Bunlar kabulün tek ölçütü değildir. Texel density environment yaklaşık 256 px/m, hero yakın prop 512 px/m başlangıç hedefi; tekrar eden yüzeyde tiling/trim sheet tercih edilir. VRAM ve görüntü kanıtıyla yükseltilir. Her asset'e 4K texture vermek yasak bir slogan değil ölçülmesi gereken maliyettir.

## 4. Marketplace normalizasyonu

Kaynak paketler ThirdParty altında tutulur. Projeye özgü prefab/material varyantları _Game içinde oluşturulur; vendor update üzerine doğrudan değişiklik yazılmaz. Normalize sırası: ölçek/pivot → shader compatibility → palette/roughness → gereksiz detay → LOD/collider → lighting comparison → gameplay anchor → persistent validation.

**REQ-ART-002:** Lisans, kaynak URL, satın alma/erişim kaydı ve değişiklik geçmişi asset envanterinde tutulur. AI üretimi dahil yeniden dağıtım hakkı varsayılmaz. Bu belge lisans uygunluğu incelemesi yapılmış demek değildir; paket bazında doğrulanır.

Meshy veya başka üreticiden çıkan model doğrudan game-ready kabul edilmez: topology, manifold/collision, UV, texture, rig weights, LOD ve import test gerekir. Karakterleri sırf görsel benzerlikle ortak rig kabul etmek animasyon hatası yaratır.

## 5. Animasyon üretimi

Shared skeleton ve pose standards player FP ve zombie için ayrı. Root motion/world navigation sahipliği açık; AI nav ilerlerken animasyon ikinci kez hareket ettiremez. Attack timing simülasyonun active window verisiyle eşlenir. Animation events ses/efekt için kullanılabilir; damage authority olamaz.

Minimum zombie set: idle 2, walk, run archetype, turn, investigate, attack 2, stagger, death 2. FP set: idle, equip/unequip, light/heavy/shove, fire, reload stages, consume, bandage. Farklı silahlar için bire bir unique set üretmek yerine archetype ve offset kullanılır. Eksik animasyon placeholder olarak manifestte görünür.

## 6. Ses ve VFX

Ses kategorileri: informative gameplay, player foley, environment, radio/voice, music. Pool ve concurrency groups kullanılır; yakın attack cue, uzak ambience tarafından voice-limit yüzünden susturulmaz. Loop source session/cell unload'da temizlenir. Indoor/outdoor reverb volumes authored olur; occlusion ray sayısı budget'ta sınırlanır.

VFX impact materyal seti wood/metal/stone/flesh/glass. Blood decal sayısı ve ömrü bounded. Dismemberment, fluid physics, volumetric smoke simulation ilk scope'ta yoktur. Yağmur/wind görselleri weather scalar'ını takip eder, kendi random hava sistemini yaratmaz.

**REQ-ART-003:** Low quality ve reduced gore modunda hit, wound, muzzle ve threat geri bildirimi hâlâ anlaşılırdır. Ses ayarı gameplay noise radius'ünü değiştiremez.

## 7. Asset done ve kabul

Her production prefab: scale, pivot, shader, LOD, collider, icon, interaction anchor, naming, address key, license entry, memory estimate, benchmark screenshot. Hatalı referans CI validation error; düşük öncelikli cosmetic variation backlog olabilir. Sprite/texture atlas padding bleeding, skeletal bounds clipping, mesh collider maliyeti ve flashlight altında materyal kontrastı sahnede test edilir.
