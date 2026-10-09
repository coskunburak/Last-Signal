# S014 güncel giriş fark matrisi

UTC 2026-10-06; yerel tarih 2026-10-07. HEAD 97516c8b20793f4ca834e214318bdac45f041a4d. Kaynak değişikliklerinden önce oluşturuldu.

## D131 — PARTIAL

- Uygulama: Prefab/importer/clip rigleri
- Yetkili sahip: Animator/importer; hareket otoritesi motor/nav
- Asset: Player.prefab; VAL; Crowbar_Viewmodel; LS_Zombie_Runtime
- En yeni ilgili test/kanıt: ZombiePresentationTests; tarihsel rig-bindings
- Eksik kabul: Canlı uç pozlar, reimport GUID, bounds/culling
- Gerekli iş: Önce canlı örnekleme; kanıtsız rig değişimi yok
- Persistence etkisi: Yok
- Performans etkisi: Skinning/bounds ölçümü gerekli
- Lisans etkisi: VAL/DJMaesen/New Punch açık
- Rollback riski: Rig değişirse yüksek

## D132 — PARTIAL

- Uygulama: WeaponViewPresenter; MeleeStanceViewPresenter StancePivot
- Yetkili sahip: PlayerCombatController / görsel presenter
- Asset: Weapon_AssaultRifle; Crowbar_Viewmodel; VAL_MRPoly
- En yeni ilgili test/kanıt: 6 Ekim full play içindeki S014WeaponPresentationTests 9 PASS; FirstPersonLook daha sonra değişti
- Eksik kabul: FOV60/75/100 × 16:9/16:10/ekran; ortak skin/sleeve; tüm hareketler
- Gerekli iş: Yerel materyal/mesh kimliği incelemesi; sunum türevi
- Persistence etkisi: Yok
- Performans etkisi: Materyal/renderer maliyeti
- Lisans etkisi: VAL/MR POLY/DJMaesen açık
- Rollback riski: Materyal düşük; skeleton yüksek

## D133 — IMPLEMENTED

- Uygulama: ZombieAnimationPresenter actual velocity; root motion kapalı
- Yetkili sahip: ZombieNavigation
- Asset: LS_Zombie_Runtime; AC_Zombie_Shambler
- En yeni ilgili test/kanıt: 6 Ekim full play 10 ZombiePresentationTests; presenter sonra değişti
- Eksik kabul: Foot sliding/turn/stop videosu
- Gerekli iş: Ölçülen kusura göre presentation-only
- Persistence etkisi: Yok
- Performans etkisi: Animator/skinning/nav ayrımı
- Lisans etkisi: New Punch açık
- Rollback riski: Kullanıcı presenter değişiklikleri korunmalı

## D134 — IMPLEMENTED

- Uygulama: MeleeWeaponController/Resolver; iptal ve stance
- Yetkili sahip: MeleeAttackState/Resolver
- Asset: Crowbar_Viewmodel; authored swing
- En yeni ilgili test/kanıt: 6 Ekim full play 9 sunum testi; melee kaynakları eşleşiyor
- Eksik kabul: Canlı hit/miss/escape/multihitbox/cancel zaman çizgisi
- Gerekli iş: Kanıt toplama; timing kanıtsız değişmez
- Persistence etkisi: Yok
- Performans etkisi: Sweep/animator ölçümü
- Lisans etkisi: DJMaesen attribution açık
- Rollback riski: Origin değişmez

## D135 — IMPLEMENTED

- Uygulama: WeaponController/RuntimeState ve presentation gate
- Yetkili sahip: WeaponRuntimeState + PlayerInventory
- Asset: Weapon_AssaultRifle; VAL_MRPoly
- En yeni ilgili test/kanıt: 6 Ekim full play 9 sunum ve 9 persistence grubu PASS; 5 Ekim raw reload ölçümü
- Eksik kabul: HUD+magazine sürekli görüntü, güncel final regression
- Gerekli iş: Commit 1.666667/1.833333 korunacak
- Persistence etkisi: Mevcut save korunur
- Performans etkisi: Switch lifecycle
- Lisans etkisi: VAL/MR POLY açık
- Rollback riski: Reload rewrite yapılmaz

## D136 — IMPLEMENTED

- Uygulama: İki varyant ve gameplay contact/range/LOS
- Yetkili sahip: ZombieController
- Asset: Shambler.asset; AC_Zombie_Shambler
- En yeni ilgili test/kanıt: 6 Ekim full play 10 sunum; controller/presenter sonra değişti
- Eksik kabul: Her iki saldırıda canlı dodge/LOS/pause/cull
- Gerekli iş: Güncel odaklı regresyon ve encounter
- Persistence etkisi: Yok
- Performans etkisi: AI/contact ölçümü
- Lisans etkisi: New Punch açık
- Rollback riski: Mevcut vehicle knockdown korunur

## D137 — IMPLEMENTED

- Uygulama: ZombieHealth/dismemberment/corpse persistence
- Yetkili sahip: ZombieHealth/SaveSession/population
- Asset: LS_Zombie_Runtime; Shirtless visual
- En yeni ilgili test/kanıt: 6 Ekim full play; 21:28 sonraki vehicle route PASS manifest eşleşiyor
- Eksik kabul: Birleşik death/loot/save/load/reduced-gore rotası
- Gerekli iş: Mevcut sistemi doğrula; ragdoll ekleme
- Persistence etkisi: Tombstone/identity korunmalı
- Performans etkisi: Dismemberment pool/lifecycle
- Lisans etkisi: New Punch açık
- Rollback riski: Kullanıcı death/impact işi korunmalı

## D138 — MISSING / PARTIAL

- Uygulama: Kapı mevcut; treatment/consume sahibi yok
- Yetkili sahip: PlayerHealth + PlayerInventory + OwnershipTransaction
- Asset: DoorInteractable; medical item definitions
- En yeni ilgili test/kanıt: Interaction/persistence testleri; treatment testi yok
- Eksik kabul: Wound/item revision, tedavi state, input/UI, save, gerçek el sunumu
- Gerekli iş: Mevcut domain üzerinde minimum bandaj; ayrı survival framework yok
- Persistence etkisi: Additive DTO ve eski save doğrulaması gerektirir
- Performans etkisi: Commit dışı allocation yok hedefi
- Lisans etkisi: Yeni özgün sunum; vendor bytes korunur
- Rollback riski: Inventory/health/save nedeniyle yüksek; odaklı negatif test zorunlu

## D139 — PARTIAL

- Uygulama: S013Performance ve vehicle capture var
- Yetkili sahip: Development ölçüm; gameplay aynı
- Asset: S013Cabin production
- En yeni ilgili test/kanıt: Mevcut capture kodu kanıt değildir
- Eksik kabul: 1/10/20 zombie, ≥5 tekrar, CPU/GC/memory/quality
- Gerekli iş: Development-only ölçüm genişletmesi
- Persistence etkisi: Test save kullanıcı kaydından ayrı
- Performans etkisi: LS-DOC-20 bütçeleri; GPU unavailable olabilir
- Lisans etkisi: Yeni vendor yok
- Rollback riski: Validation-only; normal başlangıç etkilenmez

## D140 — NOT_RUN

- Uygulama: S013 build/acceptance pipeline var
- Yetkili sahip: SessionFlow + mevcut production sahipleri
- Asset: S013Cabin
- En yeni ilgili test/kanıt: Build 20261006T204143 başarılı ama güncel değil
- Eksik kabul: D131–139 final durumu + fresh full tests/build/continuous route
- Gerekli iş: En son; final görsel karar Burak
- Persistence etkisi: Stable test session
- Performans etkisi: D139 kanıtıyla birlikte
- Lisans etkisi: Release entitlement engeli
- Rollback riski: Immutable yeni build

## Ortak sınırlar

S013 eski build engeli güncel değildir; final POI görsel onayı, entitlement ve karşılaştırmalı benchmark kapanış kanıtı bulunmadı. S015_ENTRY = BLOCKED. Lisans/insan kabulü bağımsız teknik işin durdurulma gerekçesi değildir. 510/266 tarihsel tam sonuç güncel final PASS sayılmadı. Unity açıkken batch S014 aracı çalıştırılmaz; mevcut s012 açık Editor köprüsü kullanılır. Önce odaklı, sonra tam Edit/Play, sonra immutable build.
