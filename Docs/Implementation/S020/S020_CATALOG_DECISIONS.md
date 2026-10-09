# S020 katalog karar manifesti — rc1

72 tasarım adayı tek tek değerlendirilmiştir. Eşlemeler runtime ID migration/alias değildir; eski ID değiştirilmez. `EXISTING` eşdeğer bir oyun rolünü belirtir, tasarımın bütün payload özelliklerinin var olduğunu iddia etmez. Yeni itemler doğrudan runtime kataloğuna kayıtlıdır.

| No | Aday | Karar | Runtime ID / gerekçe |
|---|---|---|---|
| 1 | `item.food.canned_beans` | EXISTING | food.canned |
| 2 | `item.food.crackers` | NEW | food.crackers |
| 3 | `item.food.energy_bar` | DEFERRED | Seçilen konserve/kraker aileleri yeterli; ayrı tüketim/taşıma rolü ve sanat maliyeti bu RC için gerekçelenmedi. |
| 4 | `item.food.canned_soup` | DEFERRED | Seçilen konserve/kraker aileleri yeterli; ayrı tüketim/taşıma rolü ve sanat maliyeti bu RC için gerekçelenmedi. |
| 5 | `item.food.jerky` | DEFERRED | Seçilen konserve/kraker aileleri yeterli; ayrı tüketim/taşıma rolü ve sanat maliyeti bu RC için gerekçelenmedi. |
| 6 | `item.food.spoiled_meal` | DEFERRED | Kontaminasyon, enfeksiyon, ağrı, kırık veya charge/treatment payload sistemi yok; yalnız kanama mevcut. |
| 7 | `item.food.canned_fish` | REJECTED | Konserve ile örtüşen rol; yeni karar/ekonomi faydası yok. |
| 8 | `item.food.rice` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 9 | `item.food.pasta` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 10 | `item.food.cooked_meal` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 11 | `item.food.dried_fruit` | REJECTED | Kraker ile örtüşen porsiyon rolü; katalog bütçesi. |
| 12 | `item.food.biscuit` | REJECTED | Kraker ile örtüşen rol; aynı ailede gereksiz varyant. |
| 13 | `item.drink.water_bottle` | EXISTING | drink.water |
| 14 | `item.drink.soda_can` | NEW | drink.soda |
| 15 | `item.container.canteen` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 16 | `item.container.water_jug` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 17 | `item.med.clean_bandage` | EXISTING | medical.bandage |
| 18 | `item.med.rag` | NEW | medical.rag |
| 19 | `item.med.antiseptic` | DEFERRED | Kontaminasyon, enfeksiyon, ağrı, kırık veya charge/treatment payload sistemi yok; yalnız kanama mevcut. |
| 20 | `item.med.painkiller` | DEFERRED | Kontaminasyon, enfeksiyon, ağrı, kırık veya charge/treatment payload sistemi yok; yalnız kanama mevcut. |
| 21 | `item.med.antibiotic` | DEFERRED | Kontaminasyon, enfeksiyon, ağrı, kırık veya charge/treatment payload sistemi yok; yalnız kanama mevcut. |
| 22 | `item.med.splint` | DEFERRED | Kontaminasyon, enfeksiyon, ağrı, kırık veya charge/treatment payload sistemi yok; yalnız kanama mevcut. |
| 23 | `item.med.pressure_dressing` | NEW | medical.pressure-dressing |
| 24 | `item.med.purification_tablet` | DEFERRED | Kontaminasyon, enfeksiyon, ağrı, kırık veya charge/treatment payload sistemi yok; yalnız kanama mevcut. |
| 25 | `item.med.first_aid_kit` | DEFERRED | Kontaminasyon, enfeksiyon, ağrı, kırık veya charge/treatment payload sistemi yok; yalnız kanama mevcut. |
| 26 | `item.tool.can_opener` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 27 | `item.tool.screwdriver` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 28 | `item.tool.wrench` | EXISTING | tool.wrench |
| 29 | `item.tool.lockpick` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 30 | `item.tool.flashlight` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 31 | `item.tool.lighter` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 32 | `item.tool.matches` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 33 | `item.tool.pot` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 34 | `item.tool.hammer` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 35 | `item.tool.bolt_cutter` | DEFERRED | İlgili açma/kilit/ışık/ateş/kesme kullanımı ve bağlı tarif yok; işlevsiz alet eklenmedi. |
| 36 | `item.tool.sewing_kit` | NEW | tool.sewing-kit |
| 37 | `item.weapon.knife` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 38 | `item.weapon.crowbar` | EXISTING | weapon.crowbar |
| 39 | `item.weapon.axe` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 40 | `item.weapon.pistol` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 41 | `item.weapon.shotgun` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 42 | `item.ammo.9mm` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 43 | `item.ammo.shell` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 44 | `item.magazine.pistol` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 45 | `item.attachment.suppressor` | DEFERRED | Silah/ammo/attachment varyantları S021 alanı; mevcut tüfek mühimmatı 9mm/shell değildir. |
| 46 | `item.clothing.jacket` | DEFERRED | Giysi yuvası, koruma, ıslaklık/ısı ve kondisyon domainleri yok; sahte bonus eklenmedi. |
| 47 | `item.clothing.raincoat` | DEFERRED | Giysi yuvası, koruma, ıslaklık/ısı ve kondisyon domainleri yok; sahte bonus eklenmedi. |
| 48 | `item.clothing.boots` | DEFERRED | Giysi yuvası, koruma, ıslaklık/ısı ve kondisyon domainleri yok; sahte bonus eklenmedi. |
| 49 | `item.clothing.gloves` | DEFERRED | Giysi yuvası, koruma, ıslaklık/ısı ve kondisyon domainleri yok; sahte bonus eklenmedi. |
| 50 | `item.clothing.cap` | DEFERRED | Giysi yuvası, koruma, ıslaklık/ısı ve kondisyon domainleri yok; sahte bonus eklenmedi. |
| 51 | `item.clothing.vest` | DEFERRED | Giysi yuvası, koruma, ıslaklık/ısı ve kondisyon domainleri yok; sahte bonus eklenmedi. |
| 52 | `item.bag.daypack` | EXISTING | equipment.field-pack |
| 53 | `item.resource.cloth` | NEW | material.cloth |
| 54 | `item.resource.scrap` | EXISTING | material.scrap |
| 55 | `item.resource.tape` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 56 | `item.resource.wood` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 57 | `item.resource.plank` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 58 | `item.resource.nails` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 59 | `item.resource.battery` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 60 | `item.container.fuel_can` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 61 | `item.resource.cable` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 62 | `item.resource.electronic_parts` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 63 | `item.resource.charcoal` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 64 | `item.resource.filter` | DEFERRED | Sıvı hacmi/temizliği, pişirme veya container payload desteği yok. Mevcut dolu yakıt kutusu ayrı eski runtime itemidir. |
| 65 | `item.resource.rope` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 66 | `item.resource.tarp` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |
| 67 | `item.key.cabin` | DEFERRED | İlgili kapı/okunabilir not/taşınabilir cihaz use binding yok; mevcut world radio item tanımı değildir. |
| 68 | `item.key.gas_station` | DEFERRED | İlgili kapı/okunabilir not/taşınabilir cihaz use binding yok; mevcut world radio item tanımı değildir. |
| 69 | `item.quest.relay_fuse` | EXISTING | quest.relay-fuse |
| 70 | `item.note.relay_route` | DEFERRED | İlgili kapı/okunabilir not/taşınabilir cihaz use binding yok; mevcut world radio item tanımı değildir. |
| 71 | `item.device.radio` | DEFERRED | İlgili kapı/okunabilir not/taşınabilir cihaz use binding yok; mevcut world radio item tanımı değildir. |
| 72 | `item.resource.generator_part` | DEFERRED | Seçilen tarif/ilerleme zincirinde tüketim veya kullanım yok; ayrı domain/çok girdili recipe gerektirir. |

Kararlar: {'EXISTING': 8, 'NEW': 6, 'DEFERRED': 55, 'REJECTED': 3}. 72-aday tablosu dışında bir yeni item: `equipment.expedition-pack` (+12 yuva, %15 toparlanma maliyeti). Eski ek runtime tanımları: `ammo.rifle`, `fuel.generator`. Böylece mevcut 10 + yeni 7 = 17 tanım; aday sayısı runtime sayısıyla karıştırılmaz.

Her seçilen aday mevcut tüketim/kanama/crafting/kapasite yoluna bağlandı. Gramlar LS-DOC-23 başlangıç değerlerinden; su 30 g boş kap + 500 g su olarak 530 g ve tek kullanımlık kabul edildi. Eski yığın sınırları uyumluluk için korundu (ör. su 3, hurda 20). Sefer çantası gram/toparlanma değerleri RC denge hipotezidir. Genel kütle sınırı/hareket bantları mevcut domaininde yoktur; bu sprintte ikinci envanter veya item-instance frameworkü kurulmadı.

Düşük veri üretim maliyeti, mevcut prefab wrapper/collider kullanımı, sabit kimlik, iki test assembly’si ve sınırlı sanat kapsamı seçimin temelidir. Referans ikonlar yerel özgün sembollerdir; 3B varyant görselleri aşağıdaki release manifestinde açık işaretlidir.
