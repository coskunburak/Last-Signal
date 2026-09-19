---
doc_id: LS-DOC-23
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Denge parametreleri, ilk item kataloğu ve recipe matrisi

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §8, §10-14, §25; Ek C. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Tuning otoritesi

Bu dosya v0.2 paketinin başlangıç sayıları için merkezdir. Değerler gameplay ve performans testleri yapılmadan final kabul edilmez. Projede uygulanınca balance asset/JSON kaynağı ile bu tablo senkron tutulmalıdır; iki ayrı elle düzenlenen doğruluk merkezi kurulmaz. Doc update yeni test gerektiriyorsa kapsam etkisi yazılır.

**REQ-BAL-001:** Parametre adı birim ve zaman tabanı taşır. Bir değerin değişmesi save schema değişikliği mi yalnız content revision mı gerektirdiği belirtilir. Experimentler versioned profile ile yapılır.

## 2. Temel parametreler

| Key | Değer | Birim / politika |
|---|---:|---|
| world.seconds_per_sim_second | 12 | oran; 24 world hour=120 gerçek dk |
| player.walk_speed_mps | 3.2 | m/s |
| player.sprint_speed_mps | 5.5 | m/s |
| player.crouch_speed_mps | 1.6 | m/s |
| player.accel_mps2 | 18 | m/s² |
| player.step_height_m | 0.3 | m |
| player.slope_limit_deg | 45 | derece |
| player.capsule_height_m | 1.8 | m |
| player.crouch_height_m | 1.2 | m |
| player.capsule_radius_m | 0.3 | m |
| interaction.range_m | 2.2 | m |
| stamina.maximum | 100 | puan |
| stamina.sprint_drain_per_sim_s | 12 | puan/s |
| stamina.regen_per_sim_s | 20 | temel puan/s |
| stamina.regen_delay_sim_s | 1.0 | s |
| survival.hydration_zero_world_h | 12 | baseline istirahat |
| survival.nutrition_zero_world_h | 30 | baseline |
| survival.restedness_zero_world_h | 18 | baseline |
| inventory.base_mass_g | 18000 | 18 kg base |
| inventory.base_slots | 12 | stack slot |
| inventory.daypack_bonus_g | 8000 | +8 kg |
| inventory.daypack_bonus_slots | 8 | +8 slot |
| inventory.overload_cap_ratio | 1.2 | üst pickup sınırı |
| pressure.disturbance_half_life_world_h | 3 | saat |
| pressure.danger_disturbance_weight | 0.7 | normalize weight |
| pressure.danger_presence_weight | 0.3 | normalize weight |
| director.recovery_sim_s | 90 | yeni event cooldown, hearing kapanmaz |
| world.initial_cell_size_m | 100 | ölçüm sonrası değişebilir |
| streaming.max_concurrent_load | 2 | geçici bütçe |

Bu değerlerle tam stamina sprint yaklaşık 8.3 s, gecikme sonrası boş bardan recovery yaklaşık 5 s; survival modifiers hariç. Sefer yolunun tamamını koşmak beklenmez. Yük arttıkça bu süre düşer. “Ağır yük eğlence katıyor mu?” testinde yürüyüşü aşırı yavaşlatmak yerine sprint/noise trade-off önce denenir.

## 3. Gürültü profilleri

| Kaynak | Çalışma yarıçapı | Event/TTL |
|---|---:|---|
| Crouch step | 1.5 m | Tek adım / 0.5 sim s |
| Walk | 4 m | Tek adım / 0.5 sim s |
| Sprint | 10 m | Tek adım / 0.7 sim s |
| Door slam | 16 m | Tek commit / 1 sim s |
| Glass break | 28 m | Tek commit / 2 sim s |
| Crowbar impact | 12 m | Tek hit / 1 sim s |
| Suppressed pistol | 35 m | Tek shot / 2 sim s |
| Pistol | 110 m | Tek shot / 4 sim s |
| Shotgun | 180 m | Tek shot / 5 sim s |
| Generator | 40 m | Sürekli source |

Radius, gerçek sesin metre olarak doğruluğu iddiası değildir; görünür harita ölçeği ve travel time ile ayarlanır. Gunshot 400 m slice'ın önemli bölümünü etkileyebilir; test çok baskılıysa radius ve migration ayrı azaltılır. Ses TTL'si migration travel time değildir.

## 4. Combat baseline

| Silah | Damage | Active/recovery | Stamina | Noise |
|---|---:|---|---:|---:|
| Knife | 18 | 0.15 / 0.4 sim s | 8 | 5 m |
| Crowbar | 28 | 0.22 / 0.7 sim s | 15 | 12 m |
| Axe | 42 | 0.3 / 1.0 sim s | 24 | 16 m |
| Pistol | 32 | 0.25 sim s fire interval | 0 | 110 m |
| Shotgun | 8×9 pellet | 1.0 sim s interval | 0 | 180 m |

Shambler 90 HP, head multiplier 3.0 başlangıç; torso crowbar yaklaşık 4 hit, pistol yaklaşık 3 hit, tek doğru headshot potansiyeli. Armor ve condition bu örneği değiştirebilir. Runner 60 HP fakat hız avantajı; Lurker 75 HP. Bunlar denge hipotezi, UI'da sayısal damage gösterme zorunluluğu değil.

## 5. İlk 72 item definition adayı

Kütle her bir item için gramdır. Kaynak profili guaranteed spawn anlamına gelmez. P=ilk dar prototype, V=polished slice adayı. Bir itemin burada bulunması modeli, icon'u, sesi veya kodu yapıldığı anlamına gelmez. Unique equipment maxStack=1; sıvı değerleri kabın boş kütlesi, içerik kütlesi ayrıca hesaplanır.

| No | Definition ID | Kütle g | Stack | Kaynak | Aşama |
|---|---|---:|---:|---|---|
| 1 | item.food.canned_beans | 450 | 3 | kitchen | P |
| 2 | item.food.crackers | 180 | 4 | kitchen | V |
| 3 | item.food.energy_bar | 60 | 6 | store | P |
| 4 | item.food.canned_soup | 450 | 3 | kitchen | V |
| 5 | item.food.jerky | 100 | 4 | camp | V |
| 6 | item.food.spoiled_meal | 300 | 2 | kitchen | V |
| 7 | item.food.canned_fish | 200 | 3 | store | V |
| 8 | item.food.rice | 500 | 3 | store | V |
| 9 | item.food.pasta | 500 | 3 | kitchen | V |
| 10 | item.food.cooked_meal | 350 | 2 | recipe | V |
| 11 | item.food.dried_fruit | 150 | 4 | store | V |
| 12 | item.food.biscuit | 100 | 4 | kitchen | V |
| 13 | item.drink.water_bottle | 30 | 1 | kitchen | P |
| 14 | item.drink.soda_can | 350 | 3 | store | V |
| 15 | item.container.canteen | 250 | 1 | camp | V |
| 16 | item.container.water_jug | 200 | 1 | utility | V |
| 17 | item.med.clean_bandage | 50 | 5 | clinic/recipe | P |
| 18 | item.med.rag | 60 | 5 | house | V |
| 19 | item.med.antiseptic | 150 | 1 | clinic | V |
| 20 | item.med.painkiller | 30 | 1 | clinic | V |
| 21 | item.med.antibiotic | 30 | 1 | clinic | V |
| 22 | item.med.splint | 300 | 2 | clinic/recipe | V |
| 23 | item.med.pressure_dressing | 90 | 3 | clinic | V |
| 24 | item.med.purification_tablet | 5 | 8 | camp | V |
| 25 | item.med.first_aid_kit | 500 | 1 | clinic | V |
| 26 | item.tool.can_opener | 120 | 1 | kitchen | V |
| 27 | item.tool.screwdriver | 180 | 1 | workshop | V |
| 28 | item.tool.wrench | 450 | 1 | workshop | V |
| 29 | item.tool.lockpick | 40 | 1 | stash | V |
| 30 | item.tool.flashlight | 250 | 1 | house | P |
| 31 | item.tool.lighter | 30 | 1 | house | V |
| 32 | item.tool.matches | 20 | 1 | camp | V |
| 33 | item.tool.pot | 700 | 1 | kitchen | V |
| 34 | item.tool.hammer | 600 | 1 | workshop | V |
| 35 | item.tool.bolt_cutter | 2400 | 1 | workshop | V |
| 36 | item.tool.sewing_kit | 100 | 1 | house | V |
| 37 | item.weapon.knife | 250 | 1 | kitchen | V |
| 38 | item.weapon.crowbar | 1800 | 1 | workshop | P |
| 39 | item.weapon.axe | 2200 | 1 | shed | V |
| 40 | item.weapon.pistol | 850 | 1 | secured | P |
| 41 | item.weapon.shotgun | 3400 | 1 | secured | V |
| 42 | item.ammo.9mm | 12 | 30 | secured | P |
| 43 | item.ammo.shell | 40 | 12 | secured | V |
| 44 | item.magazine.pistol | 90 | 1 | secured | V |
| 45 | item.attachment.suppressor | 300 | 1 | rare stash | V |
| 46 | item.clothing.jacket | 900 | 1 | house | V |
| 47 | item.clothing.raincoat | 650 | 1 | house | V |
| 48 | item.clothing.boots | 1200 | 1 | house | V |
| 49 | item.clothing.gloves | 150 | 1 | workshop | V |
| 50 | item.clothing.cap | 100 | 1 | house | V |
| 51 | item.clothing.vest | 2400 | 1 | secured | V |
| 52 | item.bag.daypack | 700 | 1 | roadside | P |
| 53 | item.resource.cloth | 50 | 10 | dismantle | P |
| 54 | item.resource.scrap | 250 | 8 | workshop | V |
| 55 | item.resource.tape | 100 | 3 | workshop | V |
| 56 | item.resource.wood | 1000 | 5 | shed | V |
| 57 | item.resource.plank | 1500 | 4 | shed | V |
| 58 | item.resource.nails | 100 | 5 | workshop | V |
| 59 | item.resource.battery | 50 | 4 | house | V |
| 60 | item.container.fuel_can | 600 | 1 | utility | V |
| 61 | item.resource.cable | 200 | 5 | utility | V |
| 62 | item.resource.electronic_parts | 300 | 4 | utility | V |
| 63 | item.resource.charcoal | 250 | 4 | shed | V |
| 64 | item.resource.filter | 80 | 2 | utility | V |
| 65 | item.resource.rope | 400 | 3 | shed | V |
| 66 | item.resource.tarp | 900 | 1 | camp | V |
| 67 | item.key.cabin | 20 | 1 | roadside | P |
| 68 | item.key.gas_station | 20 | 1 | house | V |
| 69 | item.quest.relay_fuse | 150 | 1 | guaranteed secured | P |
| 70 | item.note.relay_route | 10 | 1 | cabin | V |
| 71 | item.device.radio | 500 | 1 | cabin | V |
| 72 | item.resource.generator_part | 8000 | 1 | workshop | V |

ItemDefinition ile WeaponDefinition ayrı catalog olabilir; örneğin item.weapon.pistol, weapon.pistol.service profiline referans verir. ID adlandırması tüm dosyalarda aynı namespace politikasıyla uygulanır. Bir boş su şişesi ayrı item yaratmak zorunda değildir: aynı instance volume=0 olabilir. First-aid kit nested inventory değildir; tanımlı charges/treatment capability sağlar.

## 6. 16 recipe / upgrade adayı

| ID | Girdi / koşul | Çıktı | Süre |
|---|---|---|---|
| recipe.hand.bandage | 2 cloth + 1 antiseptic charge | 1 clean_bandage | 5 sim s |
| recipe.fire.water | 500 ml dirty water + pot + heat | 500 ml clean water | 10 world min |
| recipe.hand.purify | 500 ml questionable water + tablet | 500 ml clean water | 5 world min |
| recipe.hand.splint | 1 wood + 2 cloth | 1 splint | 8 sim s |
| recipe.bench.tool_repair | 1 scrap + 1 tape + tool | +200 condition, cap'a kadar | 20 world min |
| recipe.bench.coat_repair | 2 cloth + sewing kit | +200 condition | 15 world min |
| recipe.shelter.door_brace | 2 plank + 1 nails + hammer | Door brace module | 15 world min |
| recipe.shelter.storage | 3 plank + 2 nails + hammer | Storage module | 30 world min |
| recipe.shelter.bed | 2 cloth + tarp + shelter socket | Bed module | 30 world min |
| recipe.shelter.workbench | 4 plank + 2 scrap + wrench | Workbench | 60 world min |
| recipe.shelter.collector | Tarp + rope + jug | Rain collector | 30 world min |
| recipe.shelter.lamp | Battery + cable + electronics | Lamp module | 20 world min |
| recipe.relay.repair | Fuse + cable + screwdriver | Relay repaired receipt | 10 world min |
| recipe.fire.meal | Rice + clean water + pot + heat | Cooked meal | 20 world min |
| recipe.hand.cloth_salvage | Unequipped ruined clothing | Definition'a göre cloth | 5 sim s |
| recipe.bench.parts_salvage | Unprotected broken radio + screwdriver | Electronic parts | 15 world min |

Bu tablo unique quest radio'nun sökülebileceği anlamına gelmez. protected tag, recipe input filter'da kontrol edilir. Repair cost miktarından bir tool'un son condition'ını tam çıkarmadan malzeme tüketilmez. Rain collector, craft edilmiş şişe değil module; yağmur exposure ve container capacity kullanır.

## 7. Denge deneyi ve kabul

**REQ-BAL-002:** Aynı testte loot, düşman hasarı ve survival tüketimi aynı anda değiştirilmez; hangi değişkenin sonucu etkilediği izlenir. A/B playtest küçük ekipte sırayla yapılabilir; istatistiksel kesinlik iddiası gerekmez.

**REQ-BAL-003:** Gıda/su/ammo kritik gereksinimleri economy simulation + insan koşusuyla kontrol edilir. 72 adayın hepsi aynı sıklıkta dağıtılmaz. İlk prototype 12 P item ile sınırlanabilir; koşu eğlenceli olmadan katalog büyütülmez.
