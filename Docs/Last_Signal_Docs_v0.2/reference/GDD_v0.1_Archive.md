# ARCHIVE — Project: Last Signal GDD v0.1

> Tarihî kaynak: 17 Eylül 2026 DOCX. Bu aktarım özgün gövde metnini ve tablolarını sıra ile korur. Üstbilgi, altbilgi, sayfa numarası ve boş layout paragrafları aktarılmadı. Güncel tasarım açıklamaları için [v0.2 karar kaydı](../sources/01_Kararlar_ve_Kapsam.md).

PROJECT: LAST SIGNAL

PRODUCTION GAME DESIGN DOCUMENT

Single-player first | Sistemik açık dünya zombie survival

| Belge sürümü | v0.1 - Pre-production baseline |
| --- | --- |

| Tarih | 17 Eylül 2026 |
| --- | --- |

| Sahip | Burak Coşkun |
| --- | --- |

| Motor | Unity 6000.5.0f1 - Universal Render Pipeline - Apple Silicon geliştirme |
| --- | --- |

| Öncelik | Single-player vertical slice; co-op yalnızca core sistemler doğrulandıktan sonra |
| --- | --- |

| HIGH CONCEPT<br>Yoğun, el yapımı bir kırsal bölgede kaynak kıtlığı, hava koşulları ve yaşayan zombie nüfusu arasında rota planlayan; gürültüsü, yağması ve bölgedeki varlığıyla dünyanın tehlike dengesini değiştiren sistemik bir hayatta kalma oyunu. |
| --- |

DURUM: INTERNAL / LIVING DOCUMENT

## Belge Kontrolü ve Kullanım Kuralları

Bu GDD, projenin tasarım niyetini, kapsamını, oyuncuya verilen sözleri ve sistemler arası sözleşmeleri tek bir yerde toplar. Kod mimarisinin ayrıntılı sınıf tasarımı ayrı bir Technical Design Document içinde tutulmalıdır; ancak tasarım davranışı, veri sahipliği ve kabul ölçütleri burada tanımlanır.

| Alan | Karar |
| --- | --- |
| Belge sahibi | Burak Coşkun / Game Director & Lead Developer |
| Güncelleme sıklığı | Her milestone kapanışında; temel pillar değişirse aynı gün |
| Karar hiyerarşisi | Player promise > design pillars > core loop > system rules > content tuning |
| Değişiklik kuralı | Locked kararlar yalnızca ölçüm, playtest kanıtı veya teknik blocker ile açılır. |
| Kaynak gerçekliği | Sayılar v0.1 tuning hedefidir; prototip ve telemetry ile doğrulanır. |
| Kapsam dili | Must / Should / Could / Won't sınıflandırması kullanılır. |

### Karar Durumları

| Etiket | Anlamı | Değiştirme koşulu |
| --- | --- | --- |
| LOCKED | Ürünün kimliğini veya teknik temelini belirleyen karar. | Director kararı + yazılı change note |
| TARGET | Başarılması istenen ölçülebilir hedef. | Test verisiyle revize edilebilir |
| PROTOTYPE | Eğlence veya uygulanabilirliği henüz kanıtlanmamış sistem. | Vertical slice review |
| TBD | Bilinçli olarak açık bırakılmış karar. | İlgili milestone öncesi kapanmalı |
| OUT | Mevcut ürün kapsamının dışında. | Yeni milestone ve bütçe onayı gerekir |

### Değişiklik Günlüğü

| Sürüm | Tarih | Özet |
| --- | --- | --- |
| 0.1 | 17.09.2026 | İlk production baseline: ürün vizyonu, core loops, World Pressure, sistem tasarımları, vertical slice, teknik hedefler ve üretim planı. |

## İçindekiler

1. Yönetici Özeti

2. Ürün Vizyonu ve Konumlandırma

3. Oyuncu Deneyimi ve Tasarım Pillarları

4. Kapsam, Platform ve Ürün Stratejisi

5. Dünya, Premise ve Anlatı

6. Oynanış Döngüleri

7. Oyuncu Kontrolü, Kamera ve Etkileşim

8. Survival Simülasyonu

9. Sağlık, Yaralanma ve Ölüm

10. Item, Inventory, Equipment ve Encumbrance

11. Loot, Kaynak Ekonomisi ve Containerlar

12. Combat, Silahlar ve Hasar

13. Stealth, Gürültü ve World Pressure

14. Zombie AI ve Nüfus Simülasyonu

15. Dünya Yapısı, POI ve Streaming

16. Sığınak Ağı, Crafting ve İlerleme

17. Görevler, Olaylar ve Endgame

18. Zaman, Hava ve Çevresel Sistemler

19. UI/UX, Erişilebilirlik ve Onboarding

20. Sanat, Animasyon, VFX ve Ses

21. Teknik Tasarım Özeti

22. Save/Load ve Veri Sözleşmeleri

23. Gelecekte Co-op Hazırlığı

24. Dengeleme, Telemetry ve Playtest

25. İçerik Bütçesi ve Production Roadmap

26. QA, Definition of Done ve Release Gates

27. Risk Kaydı ve Scope Guardrails

28. Açık Kararlar

Ek A. Vertical Slice Senaryosu

Ek B. Veri Şemaları ve İsimlendirme

Ek C. İlk İçerik Matrisleri

Ek D. Glossary

## 1. Yönetici Özeti

### 1.1 Ürün Tanımı

Project: Last Signal, birinci şahıs perspektifli, single-player öncelikli, sistemik açık dünya zombie survival oyunudur. Oyuncu yalnızca açlık barlarını yönetmez; yaptığı her baskınla bölgenin kaynak dengesini, gürültü profilini ve enfekte hareketini değiştirir. Amaç, giderek zorlaşan bölgelerde güvenli rotalar ve sığınaklar kurarak yaşam alanını genişletmek, merkezi radyo ağını yeniden çalıştırmak ve final tahliye penceresine hazırlanırken dünyanın artan baskısına karşı ayakta kalmaktır.

| OYUNCUYA VERİLEN TEMEL SÖZ<br>Hazırlık, bilgi ve dikkat; refleks kadar değerlidir. Dünya eylemlerini hatırlar, fakat seni anlamsızca cezalandırmaz. Her başarılı sefer yeni bir imkan; her hata ise okunabilir, öğrenilebilir bir sonuç üretir. |
| --- |

### 1.2 Ürünün Ayırt Edici Kimliği

| Özellik | Oyuncu değeri | Pazar anlatısı |
| --- | --- | --- |
| World Pressure | Gürültü, bölgesel varlık ve yağma; zombie yoğunluğu ile olay ihtimallerini değiştirir. | Dünya senin rutinlerine uyum sağlar. |
| Shelter Network | Tek mega-üs yerine farklı amaçlara sahip sığınaklar ve aralarındaki güvenli rotalar. | Hayatta kalmak, haritayı sahiplenmek değil bağlamaktır. |
| Readable Simulation | Derin sistemler tek tek anlaşılır, neden-sonuç arayüzde görünür. | Zor ama adil; karmaşık ama okunabilir. |
| Dense Open World | Boş kilometreler yerine hikaye taşıyan yakın POI kümeleri ve kısa riskli yolculuklar. | Küçük harita, yüksek anlam yoğunluğu. |
| Single-player Craft | Atmosfer, tempo, kayıt güvenilirliği ve oyuncu kontrolü ağ karmaşıklığından önce gelir. | Önce eksiksiz solo deneyim. |

### 1.3 Başarı Tanımı

- İlk 15 dakikada oyuncu keşif, loot, tehdit değerlendirme ve sığınağa dönüş döngüsünü kavrar.

- İlk 60 dakikada en az bir anlamlı ekipman yükseltmesi, bir rota kararı ve bir bedelli hata yaşar.

- Oyuncu ölüm veya başarısızlığı rastgele değil; hazırlık, bilgi ya da uygulama eksikliğiyle açıklayabilir.

- Vertical slice, 30 dakikalık oturum boyunca en az iki farklı geçerli yaklaşım üretir: çatışma veya kaçınma.

- Hedef donanımda frame-time istikrarı korunur; save/load dünya durumunu güvenilir biçimde geri getirir.

- Oyuncuların önemli bölümü oturum sonunda “bir sonraki sefer şu bölgeye gideceğim” hedefini kendisi kurar.

### 1.4 Anti-Hedefler

- Loot sayısını içerik derinliği sanan, yüzlerce anlamsız varyantlı bir envanter oyunu değildir.

- Sürekli zombie öldürmenin en verimli çözüm olduğu bir horde shooter değildir.

- Her metreye crafting malzemesi saçan bir toplama simülasyonu değildir.

- Prosedürel genişlik uğruna el yapımı mekan ve çevresel hikaye kalitesini feda etmez.

- İlk sürümde co-op, araç filosu, NPC kolonisi ve karmaşık base building aynı anda hedeflenmez.

## 2. Ürün Vizyonu ve Konumlandırma

### 2.1 One-Sentence Pitch

| PITCH<br>Sessiz kalmak, doğru rotayı bilmek ve sığınak ağını büyütmek; kurşun sayısından daha önemlidir. Yağmaladığın ve gürültüyle uyandırdığın her bölge, bir sonraki gelişinde seni farklı karşılar. |
| --- |

### 2.2 Referanslar ve Sınırlar

| Referans | Öğrenilecek yön | Kopyalanmayacak yön |
| --- | --- | --- |
| VEIN | Sistemik etkileşim, fiziksel dünya hissi, hayatta kalma derinliği. | Ölçeksiz özellik listesi ve erken multiplayer yükü. |
| SurrounDead | Gezilebilir açık alan, ekipman toplama, sade okunabilir progression. | Jenerik asset birleşimi ve yalnızca loot rarity odaklı ilerleme. |
| The Long Dark | Atmosfer, kaynak baskısı, yolculuk hazırlığı, hava tehdidi. | Bire bir sanat taklidi veya tamamen aynı tempo. |
| Project Zomboid | Kalıcı dünya değişimi, çok katmanlı durumlar, oyuncu hikayeleri. | Aynı kamera, aynı UI yoğunluğu veya simülasyonun her ayrıntısını alma. |
| STALKER | Tehlikeli sefer, güvenli alan dönüşü, mekan hissi. | Aynı kurgu, fraksiyon yapısı veya çatışma yoğunluğu. |

### 2.3 Hedef Oyuncu

| Segment | Aradığı şey | Tasarım cevabı |
| --- | --- | --- |
| Core survival oyuncusu | Planlama, kıtlık, keşif, kayıp riski. | Derin fakat açıklanabilir simülasyon; kalıcı dünya. |
| Immersive sim meraklısı | Bir probleme birden fazla çözüm. | Ses, ateş, kapı, ışık, çevre ve item sistemlerinin çapraz etkileşimi. |
| Atmosfer oyuncusu | Yalnızlık, gerilim, çevresel hikaye. | Ses manzarası, hava, kontrollü görüş ve düşük UI gürültüsü. |
| Loot/progression oyuncusu | Somut güçlenme ve daha tehlikeli alana erişim. | Ekipman kalitesi, bilgi, sığınak faydaları ve bölge açılımı. |

### 2.4 Oyun Tonu

Ton; umutsuzluk pornosu değil, kırılgan dayanıklılıktır. Dünya serttir fakat tamamen ölü değildir. İnsanların bıraktığı notlar, yarım kalmış günlük rutinler, doğanın geri dönüşü ve oyuncunun kurduğu küçük güvenli alanlar gerilimin yanında aidiyet üretir. Mizah çok seyrek, karakter temelli ve kara mizah düzeyinde kalır; slapstick genel tonu bozmaz.

## 3. Oyuncu Deneyimi ve Tasarım Pillarları

| Pillar | Tasarım ilkesi | Oyun içi kanıt |
| --- | --- | --- |
| P1 - Hazırlık Kazandırır | Oyuncu tehlikeyi okuyabilir, hazırlık yapabilir ve riskini azaltabilir. Bilinmeyen risk heyecan yaratır; görünmez kurallar hayal kırıklığı yaratmaz. | Harita notları, hava tahmini, mühimmat/ilaç kontrolü, rota planı. |
| P2 - Dünya Hatırlar | Loot, kapılar, cesetler, kırılan pencereler, ateşler, ses geçmişi ve bölgesel baskı kalıcı veya süreli iz bırakır. | Aynı markete ikinci gelişte farklı durum ve risk. |
| P3 - Çatışma Bir Maliyettir | Silah kullanmak güçlüdür fakat ses, mühimmat, silah durumu ve yaralanma riski üretir. | Bir zombieyi vurmak beşini çekebilir. |
| P4 - Küçük Dünya, Yoğun Karar | Yolculuklar uzun yürüyüş değil; görüş, rota, yük ve zaman kararlarından oluşur. | Her 30-60 saniyede anlamlı gözlem veya seçim. |
| P5 - İlerleme Yetkinliktir | Güçlenme yalnızca sayısal stat artışı değildir; bilgi, araç, rota ve sığınak yetenekleridir. | Yeni araçla kilitli depo açmak; radio ile hava uyarısı almak. |

### 3.1 Duygusal Eğri

MERAK  >  HAZIRLIK  >  GERILIM  >  KRIZ  >  RAHATLAMA  >  İLERLEME

Her sefer tek bir duygu üretmemelidir. Güvenli sığınaktaki sakin hazırlık, yoldaki belirsizlik, POI içindeki sıkışma ve dönüşteki rahatlama birbirini besler. Sürekli yüksek gerilim oyuncuyu yorar; sürekli güven ise survival kimliğini eritir.

### 3.2 Tasarım Testleri

| Soru | Geçer koşul |
| --- | --- |
| Bu özellik ana döngüyü güçlendiriyor mu? | Keşif, hazırlık, risk veya ilerleme kararlarından en az birini anlamlı biçimde etkiler. |
| Oyuncu sonucu önceden okuyabilir mi? | Görsel, işitsel veya UI geri bildirimiyle neden-sonuç kurulabilir. |
| Sistemin ilginç bedeli var mı? | Tek doğru seçenek yerine maliyet ve fırsat dengesi yaratır. |
| Başka sistemle etkileşiyor mu? | En az iki mevcut domain ile bağ kurar; yalnız başına menü özelliği değildir. |
| Üretim maliyeti karşılığını veriyor mu? | Birden fazla içerikte tekrar kullanılabilir veya signature experience üretir. |

## 4. Kapsam, Platform ve Ürün Stratejisi

### 4.1 Platform ve Teknik Baseline

| Başlık | Karar | Durum |
| --- | --- | --- |
| Motor | Unity 6000.5.0f1 | LOCKED |
| Render pipeline | Universal Render Pipeline, Forward+ | LOCKED |
| Geliştirme makinesi | MacBook Pro M3 Pro / Apple Silicon | LOCKED |
| İlk ticari hedef | Windows PC / Steam | TARGET |
| Geliştirme buildleri | macOS + Windows doğrulama buildleri | TARGET |
| Kamera | First-person; third-person ilk kapsam dışında | TARGET |
| Kontrol | Keyboard/mouse öncelikli, gamepad tam destek hedefi | TARGET |
| Oyun modu | Single-player | LOCKED |
| Co-op | Vertical slice ve core sistem doğrulamasından sonra prototip kararı | OUT / FUTURE |

### 4.2 Scope Katmanları

| Katman | Amaç | İçerik sınırı |
| --- | --- | --- |
| Prototype | Tek tek mekaniklerin eğlence ve teknik riskini kanıtlamak. | Graybox, tek oda/alan, placeholder ses ve görsel. |
| Vertical Slice | Nihai kalite çizgisini ve ana döngüyü 30-45 dakikada kanıtlamak. | 300-500 m alan; 5-10 bina; 60-80 item; 3 zombie archetype. |
| Alpha | Baştan sona oynanabilir ürün, eksik içerikle. | Tüm ana sistemler; 2 bölge; geçici denge. |
| Beta | Feature complete; içerik, optimizasyon, UX ve hata kapatma. | Tüm planlı bölgeler ve ana son; save migration hazır. |
| 1.0 | Stabil, dengeli, pazarlanabilir PC ürünü. | Yaklaşık 4 km² yoğun dünya; 4-6 bölge; 20-30 saat ilk kampanya. |

### 4.3 MoSCoW - 1.0

| Must | Should | Could | Won't (1.0) |
| --- | --- | --- | --- |
| Movement, interaction, inventory, loot, melee, firearms, zombie AI, survival, weather, shelter network, crafting, save/load, endgame. | Limited skill growth, dynamic events, radio intel, multiple difficulties, mod-friendly data boundaries. | Challenge mode, photo mode, expanded accessibility, alternate start scenarios. | Co-op, dedicated server, vehicles, NPC settlements, procedural world, freeform construction, PvP. |

### 4.4 Oturum ve Oyun Süresi Hedefleri

| Ölçek | Hedef |
| --- | --- |
| Micro decision | 2-10 saniye: bak, dinle, pozisyon al, etkileş. |
| Encounter | 30 saniye-4 dakika: kaçın, savaş, saklan, çevreyi kullan. |
| Supply run | 12-25 dakika: hazırlan, git, lootla, dön. |
| Session | 45-90 dakika; güvenli noktada doğal kapanış. |
| İlk kampanya | 20-30 saat; keşifçi oyuncuda 35+ saat. |
| Endless aftermath | Ana final sonrası isteğe bağlı sürdürülebilir sandbox. |

## 5. Dünya, Premise ve Anlatı

### 5.1 Premise

Salgının ilk büyük dalgasından aylar sonra dağlık bir kırsal ilçe dış dünyayla bağlantısını kaybetmiştir. Elektrik ağı parçalı, yollar kısmen kapanmış, radyo tekrarlayıcıları susmuştur. Oyuncu, başarısız bir tahliye konvoyundan sonra bölgenin kenarında tek başına kalır. İlk hedefi geceyi geçirmek; uzun vadeli hedefi ise sığınakları birbirine bağlamak, radyo altyapısını ayağa kaldırmak ve yaklaşan ağır hava cephesi öncesinde bir tahliye veya kalıcı yaşam seçeneği oluşturmaktır.

### 5.2 Anlatı İlkeleri

- Ana hikaye oynanışı kesmez; keşif, radyo, çevresel kanıt ve az sayıdaki kritik sahne üzerinden ilerler.

- Oyuncu karakterinin kişiliği hafif tanımlıdır; karar alanı ve oyuncunun kendi hikayesi korunur.

- Notlar koleksiyon yükü değil; rota, kasa kodu, kaynak, tehlike veya insan hikayesi sağlayan işlevsel keşiflerdir.

- Lore, temel hayatta kalma bilgilerini saklamak için kullanılmaz. Kritik tutorial bilgisi açık verilir.

- Final hedefi baskı yaratır ancak gerçek zamanlı geri sayım olarak oyuncuyu acele ettirmez; dünya aşamaları milestone ile değişir.

### 5.3 Bölgesel Yapı

| Bölge | Kimlik | Ana kaynak | Ana tehdit |
| --- | --- | --- | --- |
| Pine Ridge Outskirts | Orman, kulübeler, kamp alanı. | Temel gıda, odun, düşük seviye araç. | Dağınık infected, soğuk geceler. |
| Ash Creek Town | Evler, market, klinik, karakol. | İlaç, yiyecek, temel silah. | Dar iç mekanlar, yüksek nüfus. |
| Red Quarry Industrial | Atölye, depo, maden yolu. | Metal, jeneratör parçaları, ağır araçlar. | Gürültülü çevre, zırhlı infected. |
| North Relay Highlands | Radyo kuleleri, servis yolları. | Elektronik, iletişim yükseltmeleri. | Hava, düşük görüş, seyrek ama tehlikeli encounters. |
| Blackwater Evac Zone | Kontrol noktaları, sahra kampı. | Endgame ekipmanı ve anlatı cevapları. | Yüksek pressure, elit varyantlar, final event. |

### 5.4 Oyuncu Motivasyonu

| Aşama | Dış hedef | İç hedef | Sistem kanıtı |
| --- | --- | --- | --- |
| Survive | İlk geceyi geçir. | Kontrol ve güven kazan. | Temel sığınak, su, basit silah. |
| Stabilize | Düzenli yiyecek/su/ilaç hattı kur. | Kaosu rutine çevir. | Storage, rota bilgisi, crafting. |
| Expand | Yeni sığınaklar ve bölgeler aç. | Dünyaya yeniden hakim ol. | Relay perks, POI access, map intel. |
| Understand | Salgın ve kayıp konvoyun izini çöz. | Belirsizliği anlamlandır. | Radyo logları, çevresel hikaye. |
| Commit | Tahliye, kalma veya sinyal yayma kararı. | Kendi hayatta kalma tanımını seç. | Final hazırlığı ve sonuç. |

## 6. Oynanış Döngüleri

### 6.1 Ana Döngü

İSTIHBARAT  >  HAZIRLIK  >  YOLCULUK  >  KEŞIF/LOOT  >  KRIZ  >  DÖNÜŞ  >  İYILEŞTIRME

| Adım | Oyuncu sorusu | Üretilen karar |
| --- | --- | --- |
| İstihbarat | Hava, saat, rota ve bölgesel baskı nasıl? | Hedef ve risk profili. |
| Hazırlık | Ne taşımaya değer, neyi geride bırakmalıyım? | Loadout ve kapasite ödünleşimi. |
| Yolculuk | Hızlı yol mu güvenli yol mu? | Zaman, stamina, visibility ve noise dengesi. |
| Keşif/Loot | Derine girmeye değer mi? | Bilgi, kaynak ve çıkış planı. |
| Kriz | Savaşmalı, kaçmalı, saklanmalı mıyım? | Kaynak kaybı veya exposure. |
| Dönüş | Ek yük için risk uzatılmalı mı? | Greed ve güvenli kapanış. |
| İyileştirme | Bir sonraki seferi ne kolaylaştırır? | Craft, repair, shelter, bilgi, rota. |

### 6.2 İkinci-İkinci Döngü

- Gözle: siluet, kapı durumu, ışık, iz, container, çıkış noktası.

- Dinle: yakın infected, uzak gunshot, kırılan cam, hava ve yapı gıcırtısı.

- Konumlan: görüş hattı, dar boğaz, geri çekilme rotası, gölge ve yükseklik.

- Eylem seç: yaklaş, bekle, dikkat dağıt, kapıyı aç, lootla, saldır veya geri çekil.

- Sonucu oku: noise yayılımı, stamina, hasar, pressure artışı ve yeni tehdit.

### 6.3 Orta ve Uzun Vadeli Döngüler

| Horizon | Loop | Ödül |
| --- | --- | --- |
| 15-30 dakika | Supply run -> return -> heal/store/repair. | Net kaynak kazancı ve öğrenilen rota. |
| 1-3 saat | Sığınak stabilize et -> yeni araç üret -> zor POI aç. | Yeni eylem kapasitesi. |
| 3-8 saat | Yeni bölgeye eriş -> relay/shelter kur -> pressure yönet. | Dünya kapsamı ve lojistik üstünlük. |
| 8-20 saat | Ana sinyal zincirini onar -> özel kaynakları topla. | Endgame seçeneği. |
| 20+ saat | Final planı uygula veya aftermath sandbox sürdür. | Tamamlama, mastery, kişisel hikaye. |

### 6.4 Failure Loop

HATA  >  OKUNUR SONUÇ  >  KAYIP  >  TOPARLANMA PLANI  >  YENI DENEME

Başarısızlık yalnızca save yükletmemelidir. Varsayılan modda ölüm; dünyayı, açılmış rotaları ve sığınak durumunu korur. Oyuncu son güvenli sığınakta yaralı/bitkin uyanabilir ve dışarıda kalan çantasını geri alma seçeneğine sahip olur. Bu sistem PROTOTYPE etiketlidir; save scumming davranışı ve kayıp hissi playtest ile ölçülmelidir.

## 7. Oyuncu Kontrolü, Kamera ve Etkileşim

### 7.1 Kontrol Hedefleri

| Eylem | Keyboard/Mouse | Gamepad | Not |
| --- | --- | --- | --- |
| Move | WASD | Left Stick | Analog hız gamepadde korunur. |
| Look | Mouse | Right Stick | Ayrı sensitivity ve invert seçenekleri. |
| Sprint | Left Shift | L3 | Toggle/hold seçenekleri. |
| Crouch | C | B / Circle | Toggle/hold seçenekleri. |
| Interact | E | X / Square | Tap primary; hold secondary action. |
| Attack / Fire | LMB | RT | Context-sensitive equipped item. |
| Aim / Block | RMB | LT | Silaha göre aim, block veya prepare. |
| Reload | R | X / Square hold | Interruptible staged reload. |
| Quick slots | 1-6 / wheel | D-pad / radial | Input device adaptive UI. |
| Inventory | Tab | View/Touchpad | Game time pause policy difficulty dependent. |

### 7.2 Hareket Modeli

- Responsive first-person controller; ağırlık hissi vardır ancak input lag hissi yaratılmaz.

- Walk, sprint, crouch, step-up, slope handling, jump ve düşük engel vault temel kapsamdır.

- Prone, parkour zinciri, ledge shimmy ve combat slide 1.0 kapsamında yoktur.

- Stamina, sprint süresini ve melee gücünü etkiler; normal yürümeyi kapatmaz.

- Encumbrance; hızdan önce acceleration, stamina maliyeti, noise ve vault kabiliyetini etkiler.

- Head bob, motion blur, camera shake ve FOV bağımsız kapatılabilir/ayarlanabilir.

### 7.3 Etkileşim Sözleşmesi

| Kural | Davranış |
| --- | --- |
| Tek hedef | Crosshair çevresindeki görünür, mesafe ve açı kriterlerini geçen en iyi aday seçilir. |
| Önizleme=Sonuç | UI’da gösterilen aksiyon ile input sonrası gerçekleşen aksiyon aynı resolver sonucunu kullanır. |
| Mesafe | Standart 2.2 m; özel büyük etkileşimlerde data-driven override. |
| Occlusion | Duvar/kapı arkasındaki hedef seçilemez; collider katmanları açıkça tanımlanır. |
| Eylem türleri | Pickup, open/close, search, use, dismantle, repair, inspect. |
| Hold kullanımı | Geri döndürülemez veya uzun eylemler: dismantle, force open, treatment. |
| İptal | Hareket, hasar, menzil kaybı veya explicit cancel eylemi kesebilir. |
| Geri bildirim | Prompt, progress, kısa ses, animasyon ve sonuç mesajı; gereksiz popup yok. |

### 7.4 Kapılar ve Pencereler

- Kapılar open/closed/locked/barricaded/broken state taşır ve persistent ID ile kaydedilir.

- Sessiz açma normaldir; hızlı açma zaman kazandırır fakat daha fazla noise üretir.

- Lockpick, doğru anahtar, pry tool veya kırma alternatifleri farklı zaman/noise/araç maliyetine sahiptir.

- Pencere kırığı kesilme riski, cam sesi ve kalıcı traversal noktası üretir.

- Kapılar infected pathing için açık durum sinyali ve gerektiğinde geçici engel olur; exploit amaçlı sonsuz güven sağlamaz.

## 8. Survival Simülasyonu

### 8.1 Tasarım Felsefesi

Survival statları ayrı ayrı bar tüketen angarya değil; yolculuk planını, savaş kabiliyetini ve toparlanma süresini değiştiren bağlı bir sistemdir. Bir stat oyuncuyu doğrudan öldürmeden önce okunabilir hafif, orta ve ağır eşiklerden geçer. Normal zorlukta “unutup öldüm” yerine “uyarıları görmezden gelip krize girdim” sonucu hedeflenir.

### 8.2 Temel Statlar

| Stat | Normal aralık | Ana kaynak/tüketim | Gameplay etkisi |
| --- | --- | --- | --- |
| Health | 0-100 | Hasar, kanama, hastalık / tedavi ve dinlenme. | 0 ölüm/çöküş; düşük değer aim, hareket ve iyileşmeyi etkiler. |
| Stamina | 0-100 kısa döngü | Sprint, melee, vault / nefeslenme. | Eylem kapasitesi; tamamen bitince forced recovery. |
| Hydration | 0-100 | İçecek / zaman, sıcaklık, efor. | Regen ve cognition; kritik eşikte sağlık kaybı. |
| Nutrition | 0-100 | Gıda / zaman, efor. | Uzun vadeli enerji, healing ve taşıma performansı. |
| Fatigue | 0-100 rested | Uyku / uyanıklık, stres, efor. | Aim sway, stamina cap, algı ve blackout riski. |
| Core Temp | Yaklaşık 37 C | Hava, ıslaklık, kıyafet, ateş. | Hypothermia/heat stress; motor ve sağlık etkileri. |
| Pain | 0-100 | Yaralanma / analjezik, tedavi, zaman. | Stamina maliyeti, aim, etkileşim süresi. |
| Stress | 0-100 | Yakın tehdit, darkness, yara / güvenli alan. | Nefes, aim, uyku ve ses algısı. |

### 8.3 Başlangıç Tuning Hedefleri

| Değer | Baseline hedef | Tasarım notu |
| --- | --- | --- |
| Hydration 100 -> 0 | 10-14 oyun saati | Koşu, sıcak ve hastalık hızlandırır. |
| Nutrition 100 -> 0 | 24-36 oyun saati | Düşük eşikte önce performans, sonra sağlık etkilenir. |
| Fatigue 100 -> 0 | 16-20 uyanık oyun saati | Kısa dinlenme kısmi; güvenli uyku tam çözüm. |
| Stamina recovery delay | 0.8-1.5 gerçek saniye | Hasar, yük ve nefes darlığı artırır. |
| Normal gün uzunluğu | 120 gerçek dakika | 1 gerçek dakika = 12 oyun dakikası; ayarlanabilir. |
| Uyku atlatma | Risk ve koşullara göre 1-8 oyun saati | Açlık, hava ve güvenlik simüle edilir. |

### 8.4 Modifier Zinciri

KAYNAK DURUM  >  STATUS EFFECT  >  STAT MODIFIER  >  GAMEPLAY SONUCU  >  FEEDBACK

Örnek: Islak kıyafet + düşük hava sıcaklığı -> Wet effect ve yüksek heat loss -> core temperature düşüşü -> stamina recovery azalması ve titreme -> görsel nefes, ses, ikon ve karakter animasyonu. Sistem tek bir boolean yerine şiddet, süre, kaynak ve tedavi bilgisi taşır.

### 8.5 Gıda ve Su

- Food itemleri calories, hydration, morale, spoilage, contamination risk ve preparation state taşır.

- Çiğ/bozuk gıda kısa vadeli açlığı giderirken hastalık riski üretir; risk tooltipte kesin yüzde olarak verilmek zorunda değildir fakat kalite dili nettir.

- Su clean, questionable, contaminated olabilir. Kaynatma, filtre ve tablet farklı maliyet/zaman profiline sahiptir.

- Eating/drinking kısa ve interruptible bir eylemdir; anlık combat heal değildir.

- Container hacmi ve ağırlığı önemlidir; empty bottle ileride su planını etkileyen gerçek bir itemdir.

## 9. Sağlık, Yaralanma ve Ölüm

### 9.1 Damage Pipeline

HIT SOURCE  >  HIT ZONE  >  DAMAGE PACKET  >  ARMOR MITIGATION  >  INJURY ROLL  >  HEALTH/EFFECT

| Damage türü | Kaynak | Birincil sonuç | İkincil risk |
| --- | --- | --- | --- |
| Blunt | Melee, düşme, darbe. | Health + pain. | Fracture/stagger. |
| Cut | Cam, kesici silah. | Health. | Bleeding. |
| Piercing/Ballistic | Mermi, sivri silah. | Yüksek lokal hasar. | Bleeding, armor damage. |
| Bite | Infected grapple. | Health + wound. | Infection contamination. |
| Burn | Ateş, sıcak yüzey. | Zaman içinde hasar. | Pain, clothing damage. |
| Cold/Heat | Çevre. | Core temperature değişimi. | Hypothermia/heat exhaustion. |
| Toxic | Kirli su, kimyasal alan. | Sickness. | Hydration/fatigue kaybı. |

### 9.2 Yaralanma Modeli

| Durum | Etkisi | Tedavi | Kötüleşme |
| --- | --- | --- | --- |
| Bleeding | Periyodik health kaybı; kan izi. | Bandage; ağırsa pressure dressing. | Koşu ve tekrar darbe. |
| Laceration | Pain, kanama ihtimali. | Disinfect + bandage + zaman. | Kir ve ihmal. |
| Sprain | Hız/vault cezası. | Dinlenme, support wrap. | Sprint, yüksekten düşme. |
| Fracture | Büyük hareket/aim cezası. | Splint, güvenli dinlenme. | Yük ve çatışma. |
| Wound infection | Fatigue, fever, yavaş healing. | Temizlik, antibiyotik, dinlenme. | Kontamine yara. |
| Zombie infection | Ayrı difficulty rule. | Varsayılan: bite sonrası yüksek/fatal risk; net feedback. | Zaman; ayarlardan değiştirilebilir. |

### 9.3 Tedavi Sözleşmesi

- Tedavi doğru item + doğru hedef + yeterli süre gerektirir.

- Yanlış tedavi oyuncuyu gizlice cezalandırmaz; sistem neden etkisiz olduğunu söyler.

- Tedavi animasyonu kesilebilir ve item tüketimi belirli commit noktasında gerçekleşir.

- Medicine spam engeli için cooldown yerine toxicity, diminishing return ve ihtiyaç temelli kullanım tercih edilir.

- Uyku ve güvenli barınak iyileşmenin önemli parçasıdır; bandaj health potion değildir.

### 9.4 Ölüm Modları

| Mod | Dünya | Inventory | Amaç |
| --- | --- | --- | --- |
| Story | Korunur. | Kritik itemler korunur; sınırlı kayıp. | Hikaye ve keşif erişimi. |
| Survivor - default | Korunur. | Backpack ölüm noktasında; equipped temel araçlardan biri korunabilir. | Anlamlı ama toparlanabilir bedel. |
| Harsh | Korunur. | Tüm taşınanlar cesette; condition kaybı. | Yüksek risk ve geri alma seferi. |
| Permadeath | Ayrı karakter/world seçeneği. | Kalıcı. | Mastery ve challenge. |

## 10. Item, Inventory, Equipment ve Encumbrance

### 10.1 Item Ontology

Her item iki katmana ayrılır: ItemDefinition değişmeyen tasarım verisini; ItemInstance ise miktar, condition, ammo, contamination ve attachment gibi runtime/persistent durumu taşır. Prefab veri kaynağı değildir; dünyadaki görsel ve fiziksel temsil katmanıdır.

| Kategori | Örnekler | Instance state |
| --- | --- | --- |
| Food/Drink | Konserve, enerji barı, su şişesi. | Quantity, spoilage, opened, contamination. |
| Medical | Bandage, antiseptic, antibiotic. | Charges, sterility, condition. |
| Weapon | Knife, crowbar, pistol, rifle. | Condition, ammo, chamber, attachments. |
| Ammo | 9 mm, 12 gauge, .223. | Quantity, condition; gerektiğinde lot. |
| Tool | Axe, lockpick, wrench, flashlight. | Condition, charges/battery. |
| Clothing | Coat, boots, gloves. | Condition, wetness, insulation, pockets. |
| Resource | Cloth, scrap, chemical. | Quantity, quality tier gerektiğinde. |
| Quest/Knowledge | Key, code, map note. | Found/read flags; kritik item koruması. |

### 10.2 Inventory Modeli

- Başlangıç önerisi: slot + ağırlık hibriti. Tetris grid ilk vertical slice için OUT; UX ve üretim maliyeti yüksektir.

- Player inventory, backpack, clothing pockets, corpse, cabinet, fridge ve vehicle trunk gelecekte aynı IItemContainer sözleşmesini kullanır.

- Transfer, split, merge, move, equip, consume ve drop atomic operation olarak ele alınır.

- UI domain state’i doğrudan değiştirmez; command üretir ve sonuç eventini gösterir.

- Stack kuralları definition ID + uyumlu state üzerinden çalışır. Farklı condition veya contamination gerektiğinde birleşmez.

- Kritik story itemleri yanlışlıkla yok edilemez; drop/dismantle için açık uyarı veya koruma vardır.

### 10.3 Encumbrance

| Bant | Kapasite kullanımı | Etki |
| --- | --- | --- |
| Light | 0-50% | Tam hareket, düşük footstep noise. |
| Loaded | 50-80% | Hafif stamina ve acceleration maliyeti. |
| Heavy | 80-100% | Belirgin stamina/noise; vault yavaşlar. |
| Overloaded | 100-120% | Sprint yok, yavaş dönüş, hızlı fatigue; kısa mesafe taşıma için. |
| Blocked | >120% | Yeni item alınamaz; container transferiyle çözülür. |

### 10.4 Equipment Slots

| Slot | İşlev | Trade-off |
| --- | --- | --- |
| Head | Armor, light mount, weather protection. | Görüş/noise/ağırlık. |
| Torso inner/outer | Insulation, armor, pockets. | Heat, wetness, movement. |
| Hands | Grip, cold protection. | Dexterity ve reload süresi. |
| Legs/Feet | Pockets, protection, traversal. | Footstep noise, sprain risk. |
| Backpack | Ana taşıma kapasitesi. | Siluet, ağırlık, erişim süresi. |
| Primary/Secondary | Hızlı silah erişimi. | Taşıma yükü ve görünürlük. |
| Quick slots | 6 adede kadar hızlı item. | Hazırlık kolaylığı; sınırlı slot. |

### 10.5 Durability ve Repair

Condition sistemi her kullanımda monoton ve sıkıcı yüzde aşınması üretmemelidir. Silahlar kullanım, çevre, yanlış bakım ve kritik olaylarla yıpranır. Düşük condition güvenilirlik, verim veya kırılma riskini etkiler. Repair, sınırsız şekilde maksimuma döndürmez; bazı itemlerde max condition ceiling düşebilir. Common itemlerde sistem hafif; değerli silahlarda anlamlıdır.

## 11. Loot, Kaynak Ekonomisi ve Containerlar

### 11.1 Loot İlkeleri

- Loot tematik olmalıdır: ilaç klinikte, araç atölyede, konserve mutfak/markette daha olasıdır.

- Her container jackpot değildir; ancak açılan container oyuncuya kategori hakkında mantıklı beklenti verir.

- Nadirlik tek başına renk değildir. Kullanışlılık; condition, kaynak kıtlığı ve oyuncu planıyla belirlenir.

- Dünya loot’u ilk açılışta deterministik seed + loot table ile üretilebilir; save yalnızca gerçekleşen farkı tutar.

- Varsayılan kampanyada hızlı loot respawn yoktur. Uzun sandbox için düşük oranlı world event restock opsiyonu olabilir.

### 11.2 Ekonomi Muslukları ve Giderleri

| Kaynak | Musluklar | Giderler | Denge amacı |
| --- | --- | --- | --- |
| Food | Ev, market, av/forage future. | Tüketim, spoilage. | Sefer süresini ve rota ihtiyacını sınırlar. |
| Water | Şişe, musluk kalıntısı, yağmur, göl. | Tüketim, purification. | Sığınak değerini artırır. |
| Ammo | Polis/askeri POI, nadir stash. | Combat, misfire loss çok sınırlı. | Silahı güçlü ama pahalı tutar. |
| Medical | Klinik, banyo, ambulance. | Yaralanma tedavisi. | Hataların uzun vadeli maliyeti. |
| Fuel/Battery | Araç/garaj, utility POI. | Light, generator, electronics. | Gece ve shelter utility seçimi. |
| Parts | Atölye, dismantle. | Craft/repair/upgrade. | Progression gate. |

### 11.3 Loot Tierleri

| Tier | Bölge | Beklenen değer | Risk |
| --- | --- | --- | --- |
| T0 Survival | Başlangıç evleri/kamp. | Su, snack, bez, basit tool. | Düşük; öğretici. |
| T1 Civilian | Evler, küçük dükkanlar. | Backpack, kitchen tool, meds. | Düşük-orta. |
| T2 Specialist | Klinik, atölye, karakol. | Kaliteli tool, weapon, component. | Orta-yüksek. |
| T3 Restricted | Depo, relay, secured rooms. | Rare mod, electronics, armor. | Yüksek; key/tool/pressure. |
| T4 Endgame | Evac zone, sealed cache. | Final repair parts, best-in-class gear. | Çok yüksek; event bağlı. |

### 11.4 Container Davranışı

| State | Açıklama |
| --- | --- |
| Unsearched | İçerik henüz instantiate edilmemiş veya oyuncuya gösterilmemiştir. |
| Searched | İçerik yaratılmış ve persistent state’e girmiştir. |
| Locked | Key/lockpick/tool/force çözümü ister. |
| Trapped | Çok nadir; açık telegraph ile risk taşır. İlk slice için OUT. |
| Destroyed | Kalan içerik world loot olabilir; bazı hassas itemler zarar görür. |

## 12. Combat, Silahlar ve Hasar

### 12.1 Combat Hedefi

Combat kasıtlı, tehlikeli ve kaynak tüketen bir problem çözme katmanıdır. Oyuncu tek bir infected karşısında yetkin hisseder; kötü pozisyon, yorgunluk veya gürültü zinciri birkaç düşmanı ölümcül hale getirir. Hit feedback güçlü, input responsive, animasyon commitment ise okunabilir olmalıdır.

### 12.2 Melee

| Sistem | Kural |
| --- | --- |
| Attack types | Light, heavy/charged, shove; silaha göre farklı moveset değil sınırlı archetype. |
| Stamina | Her swing harcar; yetersiz stamina damage ve recovery’i kötüleştirir. |
| Range | Silah reach değeri + fiziksel hit validation; camera ray tek başına kullanılmaz. |
| Impact | Hit zone, relative velocity, weapon class ve edge/blunt tipi hesapta yer alır. |
| Crowd control | Shove/stagger kaçış alanı açar; sonsuz stun-lock engellenir. |
| Durability | Impact materyali ve kullanım tipine göre aşınır. |
| Execution | Yerdeki tek hedefe riskli finisher olabilir; kalabalıkta güvenli değildir. |

### 12.3 Firearms

| Alt sistem | Tasarım |
| --- | --- |
| Ballistics | Vertical slice: hitscan + yakın mesafe penetration modeli. Projectile yalnızca özel silahlarda gerekirse. |
| Accuracy | Silah taban spread + hareket + stance + stamina + pain + aim time. |
| Recoil | Okunabilir pattern + küçük varyans; oyuncu girdisiyle yönetilebilir. |
| Reload | Magazine/chamber state; staged ve interruptible. Tactical reload mermi kaybettirmez. |
| Malfunction | Yalnızca çok düşük condition veya kötü ammo; seyrek, telegraphed, düzeltilebilir. |
| Noise | Weapon + suppressor + environment ile NoiseEvent üretir; “sessiz” suppressor yok. |
| Penetration | Materyal ve kalan enerjiye göre ince yüzey/çoklu hedef; performans bütçeli. |

### 12.4 Hit Zone Baseline

| Zone | Damage multiplier | Ek davranış |
| --- | --- | --- |
| Head | 2.5-4.0x archetype’a göre | Kritik infected zayıflığı; helmet mitigation olabilir. |
| Upper torso | 1.0x | Standart damage, yüksek stagger. |
| Lower torso | 0.85x | Düşük kritik oran. |
| Arms | 0.55x | Attack capability/stagger etkisi sınırlı. |
| Legs | 0.65x | Limp/knockdown threshold; crawling state future/should. |

### 12.5 Silah Rol Matrisi

| Silah | Rol | Güç | Bedel |
| --- | --- | --- | --- |
| Knife | Sessiz yakın tek hedef. | Hızlı, hafif, utility. | Kısa reach, riskli. |
| Crowbar | Control + forced entry. | Stagger, kapı/container utility. | Ağır, orta damage. |
| Axe | Yüksek melee damage + wood tool. | Güçlü heavy attack. | Stamina, noise, durability. |
| 9 mm pistol | Acil savunma. | Hızlı draw, yaygın ammo. | Orta güç, gunshot pressure. |
| Shotgun | Yakın panic solution. | Yüksek stagger/damage. | Çok yüksek noise, düşük kapasite. |
| Hunting rifle | Uzak hassasiyet. | Yüksek penetration. | Yavaş, nadir ammo. |

## 13. Stealth, Gürültü ve World Pressure

### 13.1 Stealth Felsefesi

Stealth, görünmezlik modu değil bilgi ve dikkat yönetimidir. Crouch yalnızca sayısal fark yaratmaz; yüzey, hız, taşıma yükü, ışık, line-of-sight ve çevresel maskeleme birlikte çalışır. Oyuncu zombie görüş konisini HUD’da görmez; beden dili, ses ve çevreyle anlar.

### 13.2 Noise Event Modeli

| Event | Baseline radius | Duration | Not |
| --- | --- | --- | --- |
| Crouch step | 1-2 m | 0.5 s | Yüzey ve footwear etkiler. |
| Walk | 3-5 m | 0.5 s | Heavy load artırır. |
| Sprint | 8-12 m | 0.7 s | Tekrarlı eventler trail oluşturur. |
| Door slam | 12-20 m | 1 s | Door type etkiler. |
| Broken window | 20-35 m | 2 s | Kalıcı çevre değişimi. |
| Melee impact | 4-18 m | 1 s | Materyal ve weapon class. |
| Suppressed pistol | 25-45 m | 2 s | Yakında hâlâ belirgin. |
| Unsuppressed pistol | 80-140 m | 4 s | Region pressure artışı. |
| Shotgun/rifle | 140-220 m | 5 s | Migration tetikleyebilir. |
| Generator | 20-60 m sürekli | Sürekli | Shelter pressure ve utility trade-off. |

### 13.3 World Pressure - Signature System

Her world cell üç ana hafıza değeri taşır: Disturbance, Presence ve Depletion. Bu değerler doğrudan zorluk seviyesi değildir; spawn director, migration, event seçimi ve loot sonrası davranış için girdi sağlar. Amaç oyuncuyu cezalandırmak değil, tekrar edilen güvenli rutini dönüştürmektir.

| Kanal | Artıran eylemler | Azalma | Sonuç |
| --- | --- | --- | --- |
| Disturbance | Gunshot, patlama, alarm, çok sayıda kill, yangın. | Zaman, yağmur, oyuncunun uzak kalması. | Investigation, migration, higher alert. |
| Presence | Sık ziyaret, uzun kalış, jeneratör/ışık, açık sığınak faaliyeti. | Bölgeyi terk etme, sessiz dönem. | Rutin rota risklenir; stalker event ihtimali. |
| Depletion | Loot edilen containerlar, sökülen kaynaklar. | Çok sınırlı event restock veya kalıcı. | Bölgenin kaynak profili düşer; yeni alan teşvik edilir. |

### 13.4 Pressure Bantları

| Bant | Aralık | Okunabilir belirtiler | Director davranışı |
| --- | --- | --- | --- |
| Quiet | 0-24 | Az uzak ses, seyrek iz. | Düşük roaming density. |
| Uneasy | 25-49 | Uzak hareket, kuşların kaçışı, daha çok iz. | Investigation chance artar. |
| Hot | 50-74 | Yakın uluma, göç grupları, kapı/çit teması. | Spawn budget ve migration yükselir. |
| Overrun | 75-100 | Belirgin kalabalık, çevresel alarm işaretleri. | High-risk event; bölgeyi soğutmak gerekir. |

### 13.5 Adalet Kuralları

- Düşman oyuncunun görüş alanında veya doğrudan arkasında görünmez şekilde spawn olmaz.

- Pressure artışı harita sembolü, radyo intel’i, ses manzarası ve çevresel işaretlerle okunur.

- Oyuncu bölgeden uzak kalarak, dikkat dağıtarak veya tehditleri başka rotaya çekerek sistemi etkileyebilir.

- Bir yüksek ses olayı “teleport horde” yaratmaz; çevredeki nüfus ve travel time dikkate alınır.

- Director, oyuncu ağır yaralıyken garanti saldırı üretmez; gerilimi yönetir fakat sonucu hileyle belirlemez.

## 14. Zombie AI ve Nüfus Simülasyonu

### 14.1 AI Katmanları

PERCEPTION  >  MEMORY  >  DECISION  >  ACTION  >  ANIMATION/AUDIO

| Katman | Sorumluluk | Örnek |
| --- | --- | --- |
| Perception | Vision, hearing, damage, nearby alert. | NoiseEvent’i algıla. |
| Memory | Son bilinen hedef, stimulus zamanı, confidence. | Oyuncunun son görüldüğü kapı. |
| Decision | State ve hedef seçimi. | Investigate yerine chase. |
| Action | Navigate, turn, attack, break, wait. | Kapıya git ve vur. |
| Presentation | Animator, IK, SFX, VFX. | Saldırı animasyonu ve vocalization. |

### 14.2 State Modeli

| State | Giriş | Davranış | Çıkış |
| --- | --- | --- | --- |
| Dormant | Uzak LOD veya scripted rest. | Minimum simülasyon. | Stimulus/proximity. |
| Idle | Hedef yok. | Dinle, çevreyi tara. | Wander/stimulus. |
| Wander | Director hedefi. | Düşük hız rota. | Stimulus/timeout. |
| Investigate | Ses/iz. | Kaynağa git, ara. | Confirm chase / search. |
| Search | Hedef kaybedildi. | Son bilinen noktada spiral/rooms. | Reacquire / timeout. |
| Chase | Yüksek confidence sight/sound. | Hedefe en uygun rota. | Attack range / lost. |
| Attack | Range + angle + cooldown. | Telegraphed strike/grapple. | Hit/miss/recover. |
| Stagger/Down | Impact threshold. | Geçici control loss. | Recover/death. |
| Feed | Corpse stimulus. | Düşük awareness; risk/stealth fırsatı. | Threat/noise. |

### 14.3 Archetype Matrisi

| Archetype | Rol | Davranış | Counterplay |
| --- | --- | --- | --- |
| Shambler | Temel alan baskısı. | Yavaş, dayanıklı, sese duyarlı. | Pozisyon, melee control, headshot. |
| Lurker | İç mekan gerilimi. | Düşük ses, karanlıkta bekler, kısa burst. | Flashlight, kapı kontrolü, dikkatli tarama. |
| Runner | Panik ve rota bozma. | Nadir, hızlı, düşük dayanıklılık. | Erken tespit, shove, dar boğaz. |
| Howler | Threat multiplier. | Görünce yüksek noise event üretir. | Sessiz öncelikli kill/kaçış. |
| Armored | Ammo/aim testi. | Koruyucu gear, yavaş. | Açık hit zone, heavy melee, flank. |

### 14.4 Simulation LOD

| Mesafe/bağlam | Tick | Aktif sistemler | Hedef |
| --- | --- | --- | --- |
| 0-30 m / combat | Yüksek frekans | Full perception, nav, animator, hitbox. | Taktik doğruluk. |
| 30-80 m | 10 Hz sınıfı | Simplified perception/nav; animator LOD. | Görünür dünya. |
| 80-200 m | 2 Hz sınıfı | Coarse movement, group logic. | Uzak nüfus hissi. |
| 200 m+ / unloaded cell | Event tabanlı | Population token ve migration. | Ucuz kalıcı simülasyon. |

### 14.5 Spawn ve Popülasyon Kuralları

- Spawner fiziksel nüfusu yoktan yaratmaz hissi vermemeli; cell population budget ve giriş noktaları kullanılır.

- Indoor spawns ilk cell load veya persistent state ile belirlenir; temizlenen oda kısa süre sonra dolmaz.

- Migration, komşu cell tokenlarını pressure/noise yönünde taşır.

- Cesetler zamanla düşük maliyetli proxy’ye dönüşür ve daha sonra cleanup olur; loot edilmiş corpse state korunur.

- Encounter director aynı archetype’ı sürekli yığmak yerine rol bileşimi ve pacing kullanır.

## 15. Dünya Yapısı, POI ve Streaming

### 15.1 Dünya Ölçeği

| Milestone | Alan | Yoğunluk hedefi |
| --- | --- | --- |
| Vertical Slice | 0.09-0.25 km² | 5-10 girilebilir yapı; 3 landmark; 1 ana sığınak. |
| Alpha | 1.0-1.5 km² | 2 bölge; 15-25 önemli POI. |
| 1.0 target | Yaklaşık 4 km² yoğun el yapımı alan | 35-50 önemli POI; 80+ küçük encounter/location. |

| SCOPE GUARDRAIL<br>Harita, oyuncunun yeni içerik görmek için uzun süre boş arazide yürümesini gerektirmeyecek. Yeni sistem, hikaye veya rota değeri taşımayan alan üretime alınmayacak. |
| --- |

### 15.2 Hiyerarşi

WORLD  >  REGION  >  CELL  >  POI  >  BUILDING  >  PERSISTENT ENTITY

| Seviye | Sorumluluk |
| --- | --- |
| World | Global time, weather seed, progression phase, global events. |
| Region | Biome/art set, loot profile, base population, narrative arc. |
| Cell | Streaming, pressure, population token, changed entity state. |
| POI | Gameplay promise, entrances, loot story, encounter setup. |
| Building | Room flow, access states, containers, local audio/reverb. |
| Entity | Persistent ID, runtime state, save delta. |

### 15.3 POI Tasarım Şablonu

| Alan | Zorunlu tanım |
| --- | --- |
| Promise | Oyuncu uzaktan ne bekler? Kaynak ve anlatı vaadi. |
| Silhouette | Landmark, yaklaşım yönleri ve gece okunabilirliği. |
| Entrances | En az iki yaklaşım; erişim maliyetleri farklı. |
| Threat | Başlangıç nüfusu, Lurker noktaları, noise consequence. |
| Loot story | Tema, guaranteed anchor item, random support loot. |
| Micro narrative | Mekanın geçmişini 1-3 çevresel beat ile anlat. |
| Exit | Kriz sonrası geri çekilme rotası; soft-lock yok. |
| Persistence | Hangi kapı, container, kırık obje ve olay state’i kaydedilir? |
| Performance | Renderer, collider, light, audio ve AI bütçesi. |

### 15.4 Streaming Kuralları

- Bootstrap/PersistentSystems scene kalıcı; region/cell içerikleri additive yüklenir.

- Cell activate/deactivate iki aşamalıdır: data state hydrate, sonra visual/physics activation.

- Oyuncunun görüşünde belirgin pop-in engellenir; preload radius hareket hızından daha geniştir.

- Save sırasında cell unload ile yarışan mutationlar queue veya transaction mantığıyla çözülür.

- Addressables içerik paketleme için kullanılır; gameplay correctness yükleme başarı durumuna açıkça tepki verir.

- Her cell için memory, renderer, light, collider, navmesh ve active AI budget raporu tutulur.

### 15.5 Navigation

NavMesh tek dünya bake’i değildir. Cell veya region tabanlı yüzeyler, off-mesh linkler ve kapı state adaptörleri kullanılır. Dynamic obstacle carving yalnızca sınırlı objelerde kullanılır. AI path requestleri scheduler ile bütçelenir; her zombie her frame yeni rota istemez.

## 16. Sığınak Ağı, Crafting ve İlerleme

### 16.1 Shelter Network

Oyuncu tek bir sonsuz üs kurmak yerine mevcut binaları güvenli sığınaklara dönüştürür. Her sığınak haritadaki bir operasyon düğümüdür: saklama, uyku, su, crafting, radyo veya elektrik gibi işlevlerde uzmanlaşabilir. Bu yaklaşım büyük freeform building kapsamından kaçınırken güçlü mekansal ilerleme sağlar.

| Sığınak seviyesi | Gereksinim | Fayda |
| --- | --- | --- |
| Claimed | Alanı temizle, girişleri kontrol et. | Save/sleep adayı, temel stash. |
| Secured | Kapı/pencereyi güçlendir, threat check. | Güvenli uyku, düşük baskın riski. |
| Supplied | Su, yatak, temel food/medicine reserve. | Daha iyi recovery, fast planning. |
| Powered | Generator/battery/solar future. | Light, radio, refrigeration, workbench. |
| Linked | Relay/map bağlantısı ve rota keşfi. | Regional intel ve lojistik avantaj. |

### 16.2 Crafting

- Crafting; elde, sığınak workbench’inde ve özel workstation’da üç context kullanır.

- Recipe keşfi; başlangıç bilgisi, kitap/not, dismantle insight veya story reward üzerinden gelir.

- Craft time oyun zamanı tüketir; koşullar uygun değilse başlamadan önce açıkça belirtilir.

- Malzeme tüketimi craft başlangıcı/commit noktasıyla transaction olarak uygulanır; iptal exploit’i önlenir.

- Crafting kalite minigame’i yoktur. Sonuç; recipe, tool, skill knowledge ve material condition ile belirlenir.

- Yüzlerce recipe yerine çok kullanımlı 40-70 anlamlı recipe hedeflenir.

### 16.3 Progression Katmanları

| Katman | Ne büyür? | Örnek ödül |
| --- | --- | --- |
| Gear | Taşıma, koruma, combat ve utility. | Daha iyi backpack, reliable firearm, weather coat. |
| Knowledge | Tarif, harita, risk ve sistem bilgisi. | Su arıtma, relay frekansı, cache konumu. |
| Skill familiarity | Kullanımla küçük verim iyileşmeleri. | Daha hızlı bandage, daha az repair waste. |
| Shelter | Recovery, storage ve utility. | Workbench, radio, rain collector. |
| World access | Yeni bölge ve kapalı alan. | Bolt cutter ile depo, relay ile gate intel. |

### 16.4 Skill Sistemi Sınırları

Karakter, sıradan insandan süper askere dönüşmez. Skill bonusları küçük, işlevsel ve çoğunlukla hata toleransı/işlem verimidir. +50% damage gibi düz güç artışları yerine daha hızlı reload hazırlığı, daha doğru condition teşhisi, daha az crafting waste ve daha iyi harita notu tercih edilir. Skill grind, ana progression kapısı olmayacaktır.

## 17. Görevler, Olaylar ve Endgame

### 17.1 Objective Yapısı

| Tür | Örnek | Kural |
| --- | --- | --- |
| Main Signal | Relay parçasını bul, kuleyi onar. | Bölge açılımını ve world phase’i değiştirir. |
| Shelter Goal | Sığınağı powered/linked yap. | Oyuncu tarafından seçilebilir operasyon hedefi. |
| Investigation | Bir kayıp ekibin rotasını izle. | Çevresel ipucu + işlevsel ödül. |
| Radio Opportunity | Kısa süreli supply signal. | İsteğe bağlı, time window oyun zamanı. |
| World Event | Göç dalgası, fırtına, alarm. | Director tarafından ama adil telegraph ile. |

### 17.2 Görev Tasarım Kuralları

- Waypoint, hedefi otomatik çözmez; son 20-50 metre çevresel okuma gerektirebilir.

- Her ana objective, en az bir hazırlık kararı ve bir riskli uygulama anı içerir.

- Fetch quest ancak itemin konumu, erişimi veya taşıma sonucu özgün problem yaratıyorsa kullanılır.

- Quest state, itemin yanlış sırada bulunmasını ve POI’nin önceden temizlenmesini destekler.

- Kaçırılabilir içerik ana bitişi kilitlemez; kritik item kaybı için recovery path vardır.

### 17.3 World Phase

| Phase | Trigger | Dünya değişimi |
| --- | --- | --- |
| P0 Isolation | Yeni oyun. | Düşük bilgi, yerel tehdit, sınırlı hava. |
| P1 Contact | İlk relay çalışır. | Radio intel, uzak sinyaller, yeni objectives. |
| P2 Migration | İkinci bölge/major event. | Nüfus hareketi, daha sert hava, rare archetype. |
| P3 Storm Front | Endgame parçaları toplanır. | Hazırlık baskısı, supply disruptions. |
| P4 Last Signal | Final planı aktive edilir. | Final encounter zinciri ve seçim. |
| Aftermath | Final sonrası devam seçimi. | Sandbox modifiers ve world events. |

### 17.4 Endgame Seçenekleri

| Final | Hazırlık ekseni | Sonuç tonu |
| --- | --- | --- |
| Evacuation | Sinyal, savunma, belirli supply ve zaman penceresi. | Bölgeden ayrılma; hayatta kalanların izi. |
| Broadcast | Yüksek güçlü transmitter ve generator chain. | Diğerlerine rehber olma; açık uçlu umut. |
| Stay | Maksimum shelter network ve winter reserve. | Kalıcı yaşam; aftermath sandbox. |

## 18. Zaman, Hava ve Çevresel Sistemler

### 18.1 Game Clock

- Gameplay zamanı merkezi GameClock tarafından sağlanır; Unity Time.time kalıcı dünya zamanı değildir.

- Time scale seçeneklerle değişebilir; combat veya tehdit yakınında hızlandırma kapalıdır.

- Sleep/craft/wait işlemleri dünya simülasyonuna özet tick uygular: hava, stat, pressure decay ve eventler.

- Save dosyası absolute world timestamp ve phase bilgisini taşır.

### 18.2 Weather State Machine

| State | Oynanış etkisi | Geçiş işareti |
| --- | --- | --- |
| Clear | Yüksek görüş; ses normal yayılır. | Ufuk, rüzgar değişimi. |
| Overcast | Düşük kontrast; sıcaklık hafif düşer. | Bulut ve ambient shift. |
| Rain | Wetness; bazı footstep seslerini maskeler. | Damla, rüzgar, yüzey ıslaklığı. |
| Fog | Görüş azalır; yakın threat gerilimi. | Kademeli volumetric artış. |
| Storm | Yüksek wet/cold; noise masking; dış risk. | Radio uyarısı, gök gürültüsü. |
| Cold snap | Isı kaybı ve battery verimi. | Forecast, nefes, frost visuals. |

### 18.3 Işık ve Gece

Gece yalnızca ekranı karartmaz. Görüş, stealth, sıcaklık, navigation ve resource economy değişir. Ay ışığı, interior darkness ve taşınabilir ışıklar okunabilir kalmalıdır. Flashlight güçlü bilgi aracıdır fakat battery tüketir ve görsel tespit riskini artırır. Tam siyah alanlar yalnızca ışık çözümü yakında ve anlaşılırsa kullanılmalıdır.

### 18.4 Ateş ve Elektrik

| Sistem | Fayda | Maliyet/Risk |
| --- | --- | --- |
| Campfire | Heat, cooking, water boil, morale. | Smoke/light, fuel, ignition alanı. |
| Generator | Shelter power ve appliance. | Fuel, sürekli noise, maintenance. |
| Battery | Sessiz kısa süreli enerji. | Sınırlı charge, ağır item. |
| Grid fragment | Bazı POIlerde geçici güç. | Repair objective, bölgesel event. |

## 19. UI/UX, Erişilebilirlik ve Onboarding

### 19.1 HUD İlkeleri

- HUD sürekli sayı duvarı değildir; normal durumda düşük görünürlük, değişimde context feedback.

- Kritik statlar ikon + renk + animasyon + ses ile çok kanallı verilir; renk tek taşıyıcı değildir.

- Crosshair yalnızca ihtiyaç olduğunda belirir veya düşük opaklıkta kalır; hit marker seçeneklidir.

- Inventory, health ve map ekranları ortak input dili ve selection davranışı kullanır.

- Bildirimler sıraya alınır; loot spam, tutorial ve danger warning birbirini örtmez.

### 19.2 Ekranlar

| Ekran | Ana görev | Kritik bilgi |
| --- | --- | --- |
| HUD | Anlık durum ve context. | Interaction, equipped, ammo, kritik condition. |
| Inventory | Taşıma/equip/transfer. | Weight, slots, comparison, condition. |
| Health | Body condition ve treatment. | Wound location, severity, valid treatment. |
| Map | Rota ve player knowledge. | POI, pressure estimate, shelter, notes. |
| Crafting | Recipe ve üretim planı. | Requirements, missing source, time, workstation. |
| Shelter | Reserve ve utility yönetimi. | Supply days, power, safety, linked services. |
| Journal/Radio | Objective ve intel. | Clue, frequency, world phase. |
| Settings | Kontrol, görüntü, ses, erişilebilirlik. | Canlı preview ve reset per category. |

### 19.3 Onboarding

| Dakika | Öğrenilen | Yöntem |
| --- | --- | --- |
| 0-3 | Move, look, interact, pickup. | Diegetic crash/roadside alanı. |
| 3-8 | Inventory, equip, basic need. | Garantili water + backpack. |
| 8-15 | Noise, stealth, first zombie. | Tek kontrollü encounter ve alternatif rota. |
| 15-25 | Shelter, heal, save. | Yakın cabin; küçük yaralanma/tedavi. |
| 25-45 | Free objective choice. | Town, gas station veya relay clue. |

### 19.4 Erişilebilirlik Baseline

- Tam key rebinding; hold/toggle; double-tap zorunluluğu yok.

- Subtitle boyutu, speaker label, background opacity ve önemli non-speech audio captions.

- Colorblind-friendly icons; rarity yalnız renkle gösterilmez.

- FOV, motion blur, head bob, camera shake, chromatic aberration ayrı ayarlanır.

- Aim assist ve input sensitivity gamepad için; mouse acceleration kapatma.

- Difficulty bileşenleri ayrı ayarlanabilir: survival rate, zombie damage, perception, loot, death rule.

- UI scale hedefi; ultrawide ve safe-area testleri.

## 20. Sanat, Animasyon, VFX ve Ses

### 20.1 Görsel Yön

Hedef, fotogerçekçilik değil stilize gerçekçiliktir: okunabilir büyük şekiller, kontrollü doku detayları, soğuk-doğal palet ve sıcak güvenli alan kontrastı. The Long Dark’tan atmosferik sadelik ve değer gruplaması; modern PBR üretiminden materyal okunabilirliği alınır. Sonuç bire bir taklit değil, kendi siluet ve palet kurallarına sahip olmalıdır.

| Alan | Kural |
| --- | --- |
| Shape language | Dış dünyada kırık, eğik ve düzensiz; sığınakta yatay, kapalı, sıcak. |
| Palette | Doğa: düşük doygun yeşil/gri/mavi. Tehlike: pas/koyu kırmızı. Güven: amber. |
| Materials | Basitleştirilmiş fakat PBR tutarlı; roughness ve edge wear okunabilir. |
| Textures | Aşırı foto noise yok; orta frekansta el yapımı kırılma ve fırça hissi. |
| Lighting | Yönlü, atmosferik, oynanış silhouette’ını koruyan kontrast. |
| Decals | Hikaye ve yönlendirme amaçlı; yüzeyi rastgele kirletmek için değil. |

### 20.2 Asset Tutarlılık Standardı

- Tüm assetler ortak texel density, pivot, unit scale, naming, collider ve LOD standardına uyar.

- Farklı marketplace paketleri doğrudan yan yana kullanılmaz; material remap, palette pass, geometry simplification ve prop dressing ile normalize edilir.

- Hero props benzersiz; filler props modüler ve tekrar kullanılabilir. Aynı asset görünür yakın alanlarda tekrar edilmez.

- Her environment kit: walls, floors, trims, doors, windows, damage variants, clutter anchors ve decal set içerir.

- Zombie varyasyonu yalnız farklı kıyafet rengi değildir; silhouette, posture ve material break-up kontrollü olur.

### 20.3 Animasyon

| Set | Minimum |
| --- | --- |
| Player FP | Idle, locomotion camera response, sprint, crouch, vault, item use, heal, melee, firearm, reload, hit/death. |
| Hands/items | Ortak rig standardı; item category başına shared animation + data offsets. |
| Zombie | Locomotion speeds, turns, idle variants, investigate, attack variants, hit reactions, stagger, down, death. |
| Interaction | Kapı, container ve workstation için IK/hand target where visible; tam unique animasyon zorunlu değil. |

### 20.4 VFX

- Muzzle flash, impact, blood, dust, rain, breath, fire, smoke, wetness ve weather transition ana setlerdir.

- Blood okunabilir fakat gore odağı değildir; seçeneklerden azaltılabilir.

- Impact VFX materyale göre ayrılır ve gameplay hit confirmation sağlar.

- Particle limits, distance culling ve pooled emitters zorunludur.

### 20.5 Audio Pillarları

| Pillar | Uygulama |
| --- | --- |
| Information | Zombie yönü, materyal footstep, silah durumu ve hava yaklaşımı sesle okunur. |
| Space | Interior/exterior, occlusion, reverb zone ve uzaklık katmanları mekan hissi kurar. |
| Restraint | Müzik sürekli değildir; sessizlik tehdit ve yalnızlığı taşır. |
| Consequence | Oyuncu kendi çıkardığı sesi duyar ve yaklaşık etkisini öğrenir. |
| Comfort contrast | Shelter hum, fire, rain-on-roof gibi sesler güven hissini güçlendirir. |

## 21. Teknik Tasarım Özeti

### 21.1 Ana Mimari İlkeler

- Simulation ile presentation ayrıdır: gameplay sonucu animasyon/VFX’den bağımsız hesaplanır.

- Definition ile instance ayrıdır: ScriptableObject tasarım verisi, serializable runtime state dünya verisidir.

- Input doğrudan state mutate etmez; semantic action/command üretir.

- UI yalnızca view/controller’dır; inventory veya health state’ini doğrudan düzenlemez.

- AI decision ile Animator ayrıdır; animation event gameplay authority değildir.

- MonoBehaviour adaptörleri Unity yaşam döngüsünü domain servislere bağlar; core logic mümkün olduğunca saf C# kalır.

- Global GameManager.Instance kullanılmaz; açık subsystem ownership ve sınırlı composition root vardır.

### 21.2 Domainler

| Assembly/domain | Sorumluluk | Bağımlılık yönü |
| --- | --- | --- |
| Game.Core | IDs, time, result types, events, shared primitives. | Kimseye bağımlı değil. |
| Game.Items | Definitions, instances, tags, condition. | Core. |
| Game.Inventory | Containers ve atomic transfer. | Core + Items. |
| Game.Survival | Stats, effects, health, treatment. | Core + Items contracts. |
| Game.Combat | Damage, weapons, hit results. | Core + Items. |
| Game.AI | Perception, decision, scheduler, population. | Core + Combat contracts. |
| Game.World | Cells, POI, pressure, time/weather adapters. | Core + persistence contracts. |
| Game.Persistence | Snapshots, deltas, migrations. | Stable DTO contracts; presentation’a bağımlı değil. |
| Game.Presentation | Unity components, animator, audio, VFX, UI. | Domainlere adapter olarak bağımlı. |

### 21.3 Runtime Akışı

INPUT  >  INTENT/COMMAND  >  VALIDATE  >  SIMULATION  >  STATE CHANGE  >  DOMAIN EVENT  >  PRESENTATION

Örnek pickup akışı: PlayerInput “Interact” intent üretir. InteractionResolver görünür uygun hedefi seçer. PickupCommand mesafe, ownership, container capacity ve world item state’ini doğrular. Inventory transaction item instance’ı ekler; world entity state’ini consumed yapar; event UI, audio ve save dirty tracking tarafından dinlenir.

### 21.4 Unity Paket Baseline

| Paket/özellik | Kullanım | Zaman |
| --- | --- | --- |
| URP + Forward+ | Rendering ve çoklu local light senaryosu. | Project start. |
| Input System | Keyboard/mouse + gamepad action maps. | Project start. |
| AI Navigation | NavMesh surfaces, links, obstacles. | AI prototype. |
| Addressables | World/content loading ve build packaging. | World streaming öncesi. |
| Cinemachine | Yalnız ihtiyaç varsa camera utilities; FP controller bağımsız. | Evaluate. |
| Netcode/Lobby/Relay | İlk aşamada kurulmaz. | Post-vertical-slice audit. |

### 21.5 Performans Bütçeleri

| Alan | Vertical slice hedefi | 1.0 yaklaşımı |
| --- | --- | --- |
| Frame rate | 60 FPS target, 30 FPS min fallback. | 1080p 60 on target recommended spec. |
| Main thread | 16.6 ms frame içinde sistem bütçeleri görünür. | AI/streaming spike p95 ölçümü. |
| Active full AI | 20-30 aynı anda. | Ayar seviyesine göre 25-40. |
| Visible simplified AI | 50-80. | LOD ve occlusion ile budget. |
| Memory | Cell başına rapor; leak yok. | Uzun oturumda plateau. |
| GC allocation | Hot gameplay loops steady-state 0 B/frame hedefi. | Profiler gate. |
| Save | <2 s normal world; async snapshot. | Autosave hitch hissedilmez. |
| Load | <15 s target SSD slice. | Region bazlı ölçüm. |

## 22. Save/Load ve Veri Sözleşmeleri

### 22.1 Save İçeriği

| Katman | Örnek veri |
| --- | --- |
| Header | SchemaVersion, GameVersion, save ID, timestamp, playtime, difficulty profile. |
| World | Seed, GameClock, weather state, world phase, global event flags. |
| Player | Transform anchor, stats, status effects, inventory, equipment, knowledge. |
| Shelters | Claim/upgrade, storage, utilities, reserve state. |
| Cells | Pressure, population token, changed persistent entities. |
| Entities | Door/container state, world item, corpse, destroyed/placed object. |

### 22.2 Delta Save İlkesi

Statik dünya assetleri save dosyasına kopyalanmaz. Başlangıçtan farklı olan persistent entity state’leri kaydedilir. Her entity authoring sırasında kararlı PersistentEntityId alır; duplicate ID build validation ile bloklanır. Runtime spawned entity’ler save-scoped GUID kullanır.

### 22.3 Güvenilirlik Kuralları

- Atomic write: önce temp dosya, checksum/doğrulama, sonra son dosyayla değişim.

- En az bir önceki başarılı autosave backup olarak korunur.

- Save sırasında simulation state kısa snapshot ile alınır; disk I/O ana thread’i uzun süre bloklamaz.

- Schema migration zinciri testlidir; eski save destek politikası release notes’ta açıkça belirtilir.

- Load başarısızsa orijinal dosyaya yazılmaz; kullanıcıya recovery seçenekleri sunulur.

- Save dosyası scene hierarchy veya transient instance ID’ye bağımlı değildir.

- Combat yakınında autosave yalnız tutarlı checkpoint koşulunda yapılır.

### 22.4 Save Noktaları

| Tetikleyici | Davranış |
| --- | --- |
| Manual save | Güvenli/tehdit dışı koşullarda; difficulty ile sınırlandırılabilir. |
| Shelter sleep | Tam save + thumbnail + backup rotation. |
| Major objective | Checkpoint snapshot; oyuncuya kısa gösterge. |
| Region transition | Dirty cells + player state autosave. |
| Quit | Tutarlı durum varsa save; crash recovery ayrı. |

## 23. Gelecekte Co-op Hazırlığı

### 23.1 Strateji

İlk ürün ağ kodu taşımaz. Ancak gameplay mutationları açık command sınırlarından, state kararlı ID’lerden ve simulation-presentation ayrımından geçtiği için vertical slice sonrasında host-authoritative co-op prototipi yapılabilir. Co-op vaadi pazarlamada, prototip teknik ve üretim riski kanıtlanmadan verilmez.

### 23.2 Şimdiden Korunacak Sınırlar

| Sınır | Single-player bugün | Co-op yarın |
| --- | --- | --- |
| Authority | Local Game Authority. | Host/server authority. |
| Input | Local intent -> command. | Client intent -> validated server command. |
| Entity ID | PersistentEntityId. | Network entity mapping + persistent ID. |
| Inventory | Atomic local transaction. | Authoritative transaction + replication. |
| AI | Local scheduler. | Host scheduler; client presentation. |
| Save | Local world owner. | Host world owner. |
| Time/weather | Single authoritative service. | Host replicated clock/state. |

### 23.3 Co-op Go/No-Go Gate

- Core loop playtestlerde eğlenceli ve tekrar oynanabilir bulunuyor.

- Movement, interaction, inventory, combat, AI, save ve world streaming stable interface’e sahip.

- Vertical slice hedef donanımda performans bütçesini karşılıyor.

- İki oyunculu prototype için en az 8-12 haftalık ayrı risk bütçesi ayrılabiliyor.

- Host migration, disconnect, duplication ve save ownership ürün düzeyinde cevaplandırılabiliyor.

- Co-op eklenmesi 1.0 single-player kalitesini geciktirmeyecek finansal/üretim zemine sahip.

## 24. Dengeleme, Telemetry ve Playtest

### 24.1 Denge Hedefleri

| Alan | Hedef davranış | Alarm işareti |
| --- | --- | --- |
| Loot | Oyuncu eksik ama seçenekli. | Her run full inventory / ilerleme tamamen RNG. |
| Combat | Kaçınma ve savaş ikisi de geçerli. | Tüm encounterlar melee cheese veya ammo dump. |
| Survival | Planı etkiler, menü angaryası değildir. | Her 2 dakikada tüketim / statların önemsizliği. |
| Pressure | Rutini dönüştürür, hile hissi vermez. | Görüşte spawn / sebebi anlaşılmayan sürü. |
| Death | Bedelli ama geri dönmeye motive eder. | Rage quit / sıfır maliyet save reload. |
| Progression | Yeni kapasite ve erişim. | Yalnız yüzde bonusları / zorunlu grind. |

### 24.2 Ölçülecek Eventler

- Session start/end, playtime, difficulty profile ve quit context.

- Supply run başlangıç/hedef/dönüş; taşınan değer, kullanılan kaynak, alınan hasar.

- Death cause, location, carried value, son 60 saniye pressure/noise özeti.

- Weapon kullanım, hit zone, ammo expenditure ve encounter sonucu.

- Inventory abandon/drop, encumbrance bant süresi, item hiç kullanılmama oranı.

- POI first visit/completion/return, entrance choice ve dwell time.

- Tutorial prompt gösterim, başarı, tekrar ve kapatma oranı.

- Performance: frame-time p50/p95/p99, active AI, memory, streaming hitch.

Telemetry varsayılan olarak gizlilik dostu, anonim ve açık rıza/politika çerçevesinde tasarlanmalıdır. Offline oynanış eksiksiz çalışır. Ham kişisel veri veya gereksiz input kaydı toplanmaz.

### 24.3 Playtest Soruları

- Oyuncu ilk 10 dakikada bir sonraki kısa hedefini kendi cümlesiyle söyleyebiliyor mu?

- Ölümden sonra neden öldüğünü ve bir sonraki sefer neyi değiştireceğini açıklayabiliyor mu?

- Loot ekranında karar mı veriyor, yoksa her şeyi otomatik mi alıyor?

- Bir gunshot sonrası sonuç sürpriz olsa bile adil ve tutarlı geliyor mu?

- Sığınağa dönmek rahatlama ve ilerleme hissi veriyor mu?

- Oyuncu aynı POI’ye ikinci gelişte dünyanın değiştiğini fark ediyor mu?

- Survival statlarından hangisi karar üretiyor, hangisi yalnız bakım işi gibi hissediliyor?

## 25. İçerik Bütçesi ve Production Roadmap

### 25.1 İçerik Hedefleri

| İçerik | Vertical Slice | 1.0 Target |
| --- | --- | --- |
| World | 300-500 m alan | Yaklaşık 4 km² yoğun dünya |
| Major POI | 5-10 bina/alan | 35-50 |
| Minor locations | 8-15 | 80+ |
| Item definitions | 60-80 | 250-350 |
| Melee weapons | 3 | 10-14 |
| Firearms | 2-3 | 10-16 |
| Zombie archetypes | 3 | 5 temel + varyantlar |
| Craft recipes | 12-20 | 40-70 |
| Shelter upgrades | 5-7 | 15-25 |
| Weather states | 3 | 6+ |
| Main objectives | 1 mini arc | 15-25 beat |
| Dynamic events | 2-3 | 12-20 |

### 25.2 Makro Roadmap

| Faz | Tahmini süre | Çıkış kriteri |
| --- | --- | --- |
| Pre-production | 8-12 hafta | Pillars, movement, interaction, item model, AI risk spikes, art benchmark. |
| Vertical Slice | 4-6 ay | 30-45 dk polished loop; save/load; 3 zombie; 5-10 POI; performance pass. |
| Production Alpha | 8-12 ay | Ana sistemler ve dünya baştan sona oynanabilir; content pipeline stabil. |
| Beta/Content Complete | 4-6 ay | Feature lock; tüm regions/objectives; balance ve optimization. |
| Polish/Release | 3-5 ay | Bug burn, compatibility, localization, store/demo/release readiness. |

| GERÇEKÇİ SÜRE NOTU<br>Tek geliştirici veya çok küçük ekip için bu ölçekte production-level 1.0 hedefi yaklaşık 20-30+ ay bandındadır. 3-4 ay içinde gerçekçi hedef tam oyun değil; güçlü, ölçülebilir bir vertical slice veya Steam demo temelidir. |
| --- |

### 25.3 Sprint Grupları

| Sprint grubu | Odak | Ana teslimat |
| --- | --- | --- |
| S000-S005 | Bootstrap ve architecture | Project validation, scenes, assemblies, data IDs, test harness. |
| S006-S015 | Player & interaction | Movement, camera, input, resolver, doors, pickup. |
| S016-S025 | Items & inventory | Definitions, instances, containers, UI, equipment, persistence DTO. |
| S026-S035 | AI foundation | Perception, state, nav, scheduler, one polished zombie. |
| S036-S045 | Melee & health | Damage, hit zones, wounds, treatment, melee loop. |
| S046-S055 | Firearms & noise | Weapon runtime, reload, ballistics, noise integration. |
| S056-S065 | Survival | Needs, effects, weather exposure, sleep. |
| S066-S075 | Loot & economy | Tables, POI theming, scarcity tuning. |
| S076-S085 | Save/load | Persistent IDs, deltas, migration, recovery. |
| S086-S100 | World & streaming | Cells, pressure, population, Addressables. |
| S101-S115 | Vertical slice integration | Full run, art/audio benchmark, UX, optimization. |
| S116+ | Production decision | Scope review; expand single-player, then optional co-op spike. |

### 25.4 Her Sprint İçin Zorunlu Çıktılar

- Net oyuncu sonucu ve kapsam dışı maddeler.

- Design/technical acceptance criteria.

- EditMode unit tests for pure logic; PlayMode tests for Unity integration.

- Visible acceptance path: insan veya güvenilir QA otomasyonu ile gerçek sahnede doğrulama.

- Performance measurement when hot path veya allocation risk exists.

- Documentation: changed decisions, data schema, migration impact.

- Development build and regression report when milestone-relevant.

## 26. QA, Definition of Done ve Release Gates

### 26.1 Feature Definition of Done

| Katman | Done ölçütü |
| --- | --- |
| Design | Oyuncu amacı, kurallar, edge cases, feedback ve tuning data tanımlı. |
| Code | Sorumluluklar ayrılmış; no new compiler errors/warnings; public contracts documented. |
| Data | Validation, stable IDs, default values ve authoring workflow hazır. |
| Tests | Happy path + critical negative/edge tests; deterministic logic EditMode. |
| Integration | Gerçek sahne/prefab ile visible acceptance pass. |
| Performance | Budget ölçülmüş; hot loop allocation/spike kabul limitinde. |
| Persistence | Save/load/old state etkisi test edilmiş veya explicit N/A. |
| UX | Prompt, audio/visual feedback, cancel/failure mesajı ve accessibility kontrolü. |
| Build | Target platform development buildinde doğrulanmış. |

### 26.2 Test Piramidi

| Seviye | Örnek | Frekans |
| --- | --- | --- |
| Unit/EditMode | Inventory transaction, damage, loot roll, status effect, save migration. | Her commit/CI. |
| Integration/PlayMode | Interaction, nav door, weapon hit, cell load/unload. | Her merge/CI uygun set. |
| Acceptance | Spawn -> loot -> combat -> return -> save/load. | Sprint close ve nightly. |
| Soak | 2-4 saat traversal, repeated cell streaming, save cycles. | Milestone ve weekly. |
| Compatibility | Resolution, input device, OS/GPU tiers. | Beta/release cadence. |

### 26.3 Vertical Slice Release Gate

- 30-45 dakikalık ana loop blocker olmadan baştan sona oynanır.

- En az iki çözüm stili geçerlidir: stealth/avoidance ve controlled combat.

- Save -> quit -> load sonrası player, containers, doors, loot, pressure ve zombie state kabul edilen doğrulukta döner.

- Yeni oyuncuların çoğu kritik görevleri developer yönlendirmesi olmadan tamamlar.

- Hedef donanımda frame-time p95 kabul aralığındadır ve cell geçişinde belirgin hitch yoktur.

- Art, UI, audio ve animation en az bir final-quality benchmark alanında aynı kalite çizgisine ulaşır.

- Crash, progression blocker, duplication exploit ve save corruption sıfır toleranslıdır.

### 26.4 Severity

| Seviye | Tanım | Release |
| --- | --- | --- |
| S0 Blocker | Crash, save corruption, progression stop, data loss. | 0 açık. |
| S1 Critical | Ana sistem kullanılamaz, ciddi exploit, sık soft-lock. | 0 açık. |
| S2 Major | Belirgin yanlış davranış; workaround var. | Çok sınırlı ve onaylı. |
| S3 Minor | Görsel/UX kusuru, düşük etkili hata. | Known issues bütçesi. |
| S4 Polish | İyileştirme, tutarlılık, hissiyat. | Prioritized backlog. |

## 27. Risk Kaydı ve Scope Guardrails

| Risk | Olasılık/Etki | Erken sinyal | Mitigation |
| --- | --- | --- | --- |
| Açık dünya scope patlaması | Yüksek/Yüksek | Boş alan, bitmeyen environment işi. | 4 km² cap; POI value gate; slice-first. |
| AI CPU maliyeti | Yüksek/Yüksek | 20+ AI’da frame spike. | Scheduler, LOD, profiler gate, population tokens. |
| Save karmaşıklığı | Yüksek/Yüksek | Entity ID çakışması, load farkı. | Persistent ID erken; delta contract; migration tests. |
| Marketplace art uyumsuzluğu | Yüksek/Orta | Palet/ölçek/material dağınıklığı. | Art bible, remap pipeline, benchmark scene. |
| Sistem var, oyun yok | Orta/Yüksek | Uzun süre full loop playtest yok. | Her 2-3 sprintte integrated playable; slice gate. |
| Survival angaryası | Orta/Yüksek | Testçiler menü/stat bakımından sıkılıyor. | Threshold pacing, fewer meaningful stats, telemetry. |
| Mac/Windows farkı | Orta/Orta | Geç Windows build, shader/input sorunları. | Erken ve düzenli Windows build validation. |
| Co-op erken baskısı | Orta/Yüksek | RPC/network package core prototipte. | Post-slice gate; marketing promise yok. |
| İçerik üretim hızı | Yüksek/Yüksek | POI başına aşırı özel asset. | Modüler kit, template, reuse budget, outsource selectively. |

### 27.1 Scope Kesme Sırası

- Önce kozmetik varyant ve düşük etkili yan içerik kesilir.

- Sonra minor dynamic event ve ek weapon varyantları azaltılır.

- Sonra bölge sayısı azaltılır; mevcut bölgelerin yoğunluğu korunur.

- Skill depth ve advanced crafting sadeleştirilir.

- Signature World Pressure, ana loop, save güvenilirliği, readable combat ve shelter network kesilmez.

- Kalite/performans hedefi, daha büyük ama yarım içerik için feda edilmez.

### 27.2 Yeni Özellik Kabul Formülü

| Kriter | Soru |
| --- | --- |
| Player value | Yeni ve sık yaşanan anlamlı karar üretiyor mu? |
| Pillar fit | En az bir pillar’ı doğrudan güçlendiriyor mu? |
| System leverage | Mevcut sistem ve içeriklerle çoğalan değer yaratıyor mu? |
| Production cost | Kod + art + UI + audio + QA + save maliyeti kabul edilebilir mi? |
| Ongoing cost | Her yeni item/POI ile bakım yükü artıyor mu? |
| Cut substitute | Aynı oyuncu değerini daha küçük çözümle verebilir miyiz? |

## 28. Açık Kararlar

| ID | Karar | Önerilen varsayılan | Kapanış milestone |
| --- | --- | --- | --- |
| D-001 | Final ürün kamera seçimi | First-person. | Movement prototype. |
| D-002 | Zombie infection sonucu | Yüksek risk/fatal; difficulty configurable. | Health prototype. |
| D-003 | Inventory spatial grid | Hayır; slot+weight. | Inventory UX test. |
| D-004 | Death backpack recovery | Default Survivor modunda evet. | Full loop playtest. |
| D-005 | Loot respawn | Campaign’de hayır; aftermath event temelli. | Economy balance. |
| D-006 | Skill sistemi derinliği | Familiarity + knowledge; küçük bonus. | Alpha. |
| D-007 | Araçlar | 1.0 OUT. | Post-alpha scope review. |
| D-008 | Co-op | Yalnız post-slice prototype gate. | Vertical slice review. |
| D-009 | Full game dünya alanı | 4 km² yoğun cap. | World production plan. |
| D-010 | Working title | Project: Last Signal. | Brand/market research. |

## Ek A. Vertical Slice - Baştan Sona Oynanış Senaryosu

### A.1 Slice Amacı

Vertical slice, oyunun tamamını küçük ölçekte temsil eder. Ayrı ayrı çalışan mekanik demosu değildir. Oyuncu hazırlanmalı, dünyaya çıkmalı, en az bir POI’yi okumalı, loot kararları vermeli, tehditle karşılaşmalı, World Pressure sonucunu hissetmeli, sığınağa dönmeli ve save/load ile devam edebilmelidir.

### A.2 35 Dakikalık Golden Path

| Süre | Beat | Sistem kanıtı | Başarı ölçütü |
| --- | --- | --- | --- |
| 0-3 | Yol kenarında uyanış. | Movement, look, interaction. | Oyuncu promptsuz temel hareketi kurar. |
| 3-7 | Terk edilmiş araç ve backpack. | Loot, inventory, equip. | Ağırlık/slot farkı anlaşılır. |
| 7-12 | Kulübeye yaklaşım. | Stealth, sound, alternate entrance. | Oyuncu infected’ı önceden algılar. |
| 12-16 | İlk encounter. | Melee, stamina, damage, shove. | Combat okunur; ölümcül ama adil. |
| 16-20 | Yara tedavisi ve loot. | Health, treatment, container. | Doğru tedavi seçilir. |
| 20-24 | Radyo notu: benzinlikte parça. | Objective, map intel. | Oyuncu sonraki hedefi anlar. |
| 24-29 | Benzinlik baskını. | Gun, noise, pressure, AI migration. | Gunshot avantaj ve bedel üretir. |
| 29-33 | Yük altında dönüş. | Encumbrance, route choice, weather. | Greed trade-off hissedilir. |
| 33-35 | Sığınağı claim et, save/quit/load. | Shelter, crafting, persistence. | Dünya state’i doğru geri gelir. |

### A.3 Alternatifler

- Kulübeye ön kapıdan melee ile, arka pencereden sessizce veya dikkat dağıtıcı atarak girilebilir.

- Benzinlikte pistol kullanılabilir, infected dışarı çekilebilir veya hedef item alınarak kaçılabilir.

- Ağır jeneratör parçası alınırsa ek loot bırakmak veya iki aşamalı taşıma yapmak gerekir.

- Yağmur başlayınca daha sessiz hareket fırsatı oluşur fakat wet/cold riski artar.

- Ölüm halinde sığınaktan recovery run başlar; golden path tamamen sıfırlanmaz.

### A.4 Slice İçerik Listesi

| Kategori | Minimum |
| --- | --- |
| Locations | Road crash, cabin, gas station, small store, relay overlook, forest shortcut. |
| Enemies | Shambler, Lurker, Runner; toplam 12-20 aktif/dağıtılmış population. |
| Weapons | Knife, crowbar, pistol; shotgun optional benchmark. |
| Survival | Health, stamina, hydration, fatigue, temperature, bleeding, wetness. |
| Items | 60-80 definition; 20’si oyuncu kararında düzenli görünür. |
| Crafting | Bandage, boiled water, basic repair, barricade, simple meal, torch/fire. |
| Weather | Clear -> rain transition + night lighting benchmark. |
| Persistence | Player, inventory, doors, containers, world items, pressure, enemy/corpse, shelter. |

## Ek B. Veri Şemaları ve İsimlendirme

### B.1 Kimlik Standardı

| Tür | Format | Örnek |
| --- | --- | --- |
| Item definition | item.<category>.<name> | item.food.canned_beans |
| Weapon definition | weapon.<class>.<name> | weapon.pistol.m9_service |
| Status effect | effect.<family>.<name> | effect.wound.bleeding |
| Recipe | recipe.<station>.<result> | recipe.hand.bandage_clean |
| Loot table | loot.<region>.<poi>.<container> | loot.ashcreek.clinic.cabinet |
| Persistent entity | <region>.<cell>.<poi>.<type>.<index> | ashcreek.c03.gasstation.door.02 |
| Audio event | audio.<domain>.<action>.<variant> | audio.weapon.pistol_fire.01 |

### B.2 ItemDefinition Zorunlu Alanları

| Alan | Tür | Kural |
| --- | --- | --- |
| Id | Stable string/hashed ID | Release sonrası yeniden adlandırma migration ister. |
| DisplayNameKey | Localization key | Hard-coded player text yok. |
| Category/Tags | Enum + tag set | Loot, UI, recipe ve equipment query. |
| UnitWeight | Float | 0’dan büyük; exception explicit. |
| MaxStack | Int | State uyum kuralıyla. |
| WorldPrefab | Addressable reference | Eksik referans validation error. |
| Icon | Sprite reference | Silhouette okunabilir. |
| BaseValue/Rarity | Tuning data | Ekonomi/loot için; UI rengi tek anlam değil. |
| Capabilities | Data blocks | Consumable, equippable, weapon, tool vb. |

### B.3 DamageInfo Örneği

| Alan | Örnek |
| --- | --- |
| SourceEntityId | player.local / zombie GUID |
| SourceItemId | weapon.pistol.m9_service |
| DamageType | Ballistic |
| BaseAmount | 32 |
| HitZone | Head |
| Impulse | Vector + magnitude |
| Penetration | 0.42 normalized |
| Flags | Critical, SilentKillEligible, Environmental |

### B.4 Save Migration Sözleşmesi

- Her schema version yalnız bir sonraki version’a deterministic migration sağlar.

- Migration eski dosyanın kopyası üzerinde çalışır; başarı doğrulanmadan replace etmez.

- Silinen item ID’leri için replacement mapping veya safe fallback tanımlanır.

- Persistent entity taşınırsa alias/redirect table en az destek penceresi boyunca korunur.

- Migration test fixture’ları gerçek eski save örnekleriyle source control altında tutulur.

## Ek C. İlk İçerik Matrisleri

### C.1 Vertical Slice Item Set

| Grup | Önerilen itemler |
| --- | --- |
| Food | Canned beans, crackers, energy bar, soup, jerky, spoiled meal, can opener. |
| Drink | Water bottle, empty bottle, soda, dirty water, purification tablet. |
| Medical | Rag, clean bandage, antiseptic, painkiller, antibiotic, splint, first-aid kit. |
| Tools | Knife, crowbar, axe, screwdriver, wrench, lockpick, flashlight, lighter. |
| Weapons | Improvised club, kitchen knife, crowbar, pistol, optional shotgun. |
| Ammo | 9 mm loose/box, shotgun shells optional, magazine. |
| Clothing | Jacket, raincoat, boots, gloves, cap, simple vest. |
| Resources | Cloth, scrap metal, tape, chemical, wood, battery, fuel can. |
| Quest/utility | Cabin key, gas station key, relay fuse, map note, radio battery. |

### C.2 POI İçerik Şablonları

| POI | Gameplay promise | Signature beat |
| --- | --- | --- |
| Cabin | İlk güven, basit food/tool. | İçeride Lurker veya arka pencere yaklaşımı. |
| Gas station | Fuel, basic food, relay component. | Alarm/gunshot sonrası pressure spike. |
| Small clinic | Medical abundance. | Locked pharmacy + dark treatment room. |
| Workshop | Tools, parts, repair unlock. | Gürültülü forced entry ve armored worker. |
| Relay overlook | Intel ve progression. | Hava exposure + long sightline. |
| Forest camp | Low-risk rest/forage. | Terk edilmiş survivor story ve hidden cache. |

### C.3 Audio Event Öncelikleri

| Priority | Event | Gerekçe |
| --- | --- | --- |
| P0 | Footstep material set, zombie vocal state, weapon/melee impact, inventory confirm. | Core feedback ve threat reading. |
| P1 | Weather, doors/windows, shelter ambience, clothing/encumbrance. | Atmosfer + sistem bilgisi. |
| P2 | Distant world events, radio, wildlife, rare stingers. | World depth ve pacing. |
| P3 | Prop micro-variants, extensive foley alternates. | Polish; scope cut adayı. |

## Ek D. Glossary

| Terim | Tanım |
| --- | --- |
| Authority | Bir gameplay state’ini değiştirme ve sonucu doğrulama hakkı. |
| Cell | Streaming, pressure ve persistence için en küçük dünya bölümü. |
| Definition | Değişmeyen tasarım verisi; çoğunlukla ScriptableObject. |
| Instance | Belirli runtime nesnesinin değişebilir ve kaydedilebilir durumu. |
| POI | Kendine ait gameplay vaadi ve kimliği olan Point of Interest. |
| Pressure | Bir cell’in disturbance, presence ve depletion hafızası. |
| Presentation | Animation, VFX, SFX, UI ve görsel temsil; simulation sonucu değildir. |
| Simulation LOD | Uzaklık/öneme göre gameplay hesap ayrıntısının azaltılması. |
| Shelter Network | Farklı bölgelerde işlevsel güvenli noktalar ve aralarındaki rota sistemi. |
| Vertical Slice | Nihai kalite ve ana döngüyü küçük içerikle uçtan uca gösteren üretim kesiti. |
| World Phase | Ana ilerlemeye bağlı olarak dünya kurallarını ve olay havuzunu değiştiren aşama. |

## Sonuç ve Üretim Emri

| NORTH STAR<br>Oyuncu her sığınaktan çıkışında bir plan kurmalı; dünya bu planı sınamalı; dönüşte ise yalnız daha fazla eşya değil, daha fazla bilgi, erişim ve kontrol kazanmalıdır. |
| --- |

İlk üretim emri büyük dünya veya kapsamlı içerik değildir. Sıra: sağlam movement ve interaction; kararlı item/container modeli; tek iyi zombie; okunabilir melee; noise consequence; küçük bir loot rotası; güvenilir save/load; ardından 300-500 m vertical slice entegrasyonudur. Bu çekirdek eğlenceli ve teknik olarak stabil olmadan araç, co-op, NPC faction veya büyük base building üretime alınmayacaktır.

END OF DOCUMENT - v0.1
