# S023 — Terk Edilmiş Bakım ve Lojistik Bölgesi

15 Ekim 2026. D221 tasarımı IMPLEMENTED; oynanış/sanat kabulü NOT_RUN.
Geçici pilot kimliği; incelenen S023/LS-DOC-13 kapsamı ikinci bölge için onaylı özel isim vermiyor. Yeni kanonik hikâye veya görev eklenmedi.

## Rol, topoloji ve bütçe

Kırsal sığınağın yerine geçmeyen, az miktarda bakım malzemesi için görüşü kapalı depoya girme veya açık sevkiyat sahasında daha uzun risk alma seçimi. İki yeni 64×64 m hücre: `(3,0)` ve `(4,0)`. Grid alanı **8.192 m² = 0,008192 km²**; iki 56×56 m zemin üzerinde yaklaşık 6.272 m² fiziksel apron. Bu, 4 km² hedefinin gerçekleşmesi değildir.

S013Cabin diskteki mevcut kullanıcı sürümünden ayrı `Scenes/S023/S023RegionPilot.unity` üretilir. Resident kırsal bölge ve mevcut `(1,0)/(2,0)` tanımları korunur. Yeni kapı, mevcut ShelterLoop.OutsideAnchor'ın 2 m yanında yer alır. Yeni hücrelerde sınırlar fiziksel duvardır; geçişler mevcut etkileşim terminalleriyle yapılır. Kesintisiz açık dünya veya araçla portal geçişi vaat edilmez.

| Alan | Kaynak / karar | Risk ve geri çekilme |
|---|---|---|
| Ana POI: bakım deposu | 16×12 m, 3,4 m iç yükseklik; 1,5 m ön kapı açıklığı, 4 m batı servis açıklığı; bir garantili 1–2 scrap noktası | Kalıcı S022 Lurker, iç görüş bölen tamir bölmesi. Kapıyı açmak yerine batıdan dolaşmak mümkün. Batı şeridinden başlangıca çekilme |
| Küçük POI: kontrol noktası | Yaklaşık 6×6 m açık cepheli, S013 duvar/pencere modülleri; %50 olasılıkla bir cloth | Ana binaya girmeden sınırlı ödül. Ayrı düşman veya state sahibi yok; bölge population baskısı geçerli |
| Açık encounter: yükleme sahası | Kademeli üç sevkiyat yığını, yüksek bakım gantrisi, bir shipment noktası | Kalıcı S022 Runner, uzun görüş ve batı çevre rotası. %25 boş; doluysa %80 scrap 1–2 / %20 fuel 1 |

Üç loot noktası toplamında mühimmat ve medikal ödül **yok**. En fazla dört scrap + bir cloth veya iki scrap + bir cloth + bir fuel. Bir fuel için toplam olasılık %15. Sonraki ziyarette yeniden roll yok; mevcut opportunity receipt/tombstone kullanılır. Başlangıç bölgesinin su, sağlık ve mühimmat rolü korunur. Ekonomik denge ancak gerçek oynanışla kabul edilir.

S022 WorldPopulationManager korunur: bu izole sahnede hücre başına üç mantıksal actor, compositionSeed 23023. Bu ayar kopyadaki eski hücrelere de uygulanır; asıl S013Cabin ayarı değişmez. Sabit encounter'lar cell snapshot'ta, population actor'ları mevcut population ledger/snapshot'ta tutulur. İki farklı sahip aynı actor'ı kaydetmez. Pilot yoğun encounter senaryosu doğal en fazla üç population actor + bir authored encounter ile yapılır; 20 actor stres testi diye sunulmaz.

## Yön bulma / görsel dil

Hücre minimumu çıkarılarak yerel koordinatlar:

- Entry `(9,.05,10)`, retreat `(9,.05,18)`, shelter terminal `(7,1,10)`.
- Ana depo x28–44 / z24–36; ön kapı `(32.9,0,24)`, scrap `(36,.1,34)`, Lurker `(40,.05,33)`.
- Kontrol noktası x10–16 / z28–34; cloth `(14,.1,31)`.
- Yükleme sahası: Runner `(48,.05,39)`, gantri `(48,6,43)`, shipment `(48,.1,46)`.
- Her iki hücrede karşı bölge terminali `(54,1,52)`; approach `(54,.05,50.5)`.

Doğrudan yaklaşım açık şerit, batı alternatifi yapı/perde arkasından dolaşır. Tabelalar, farklı siluetler ve zemin şeritleri birlikte yön verir. Kullanıcı gündüz/gece/yağmurda entry→loot→geri dönüş parkurunu yürüyecek. Authoring raporu 11 native NavMesh rotasının metre uzunluğunu hesaplar; gerçek yürüyüş süresi, boş mesafe algısı ve landmark görüş mesafesi **NOT_MEASURED**.

Soğuk gri metal/taş, düşük doygunlukta ahşap, sınırlı amber yönlendirme. Çevresel anlatı: yarım kalan tamir bölmesi, sahipsiz sevkiyat, açık kontrol masası. Yeni faction, metin koleksiyonu veya görev kanonu yok. Yeni doku/shader/foliage yok; S013 materyalleri ve meshleri doğrudan paylaşılır. Yeni prop türü sayısı küçük tutulur. Standart primitive tabanlı metal kabuk ve S013 modüllerin birlikte görsel uyumu kullanıcı kabulünü bekler; final environment art değildir.

## Hedefler (ölçüm sonucu değildir)

| Bütçe | Hipotez |
|---|---|
| Yeni gameplay owner | 0; yalnız authoring ve opt-in capture |
| Yeni materyal / texture / shader | 0 / 0 / 0 |
| Yeni veri | 2 cell prefab, 2 native nav asset, 3 loot profile, 1 kabul sahnesi |
| Yeni cell renderer | Hücre başına ≤150 başlangıç hedefi; runtime dependency ölçümü bekliyor |
| Art texture | Yeni texture kopyası yok; resident kaynakları scene içinde yüklü kalabilir |
| Ek steady-state bellek | Resident karşısında +128 MiB başlangıç araştırma eşiği; NOT_MEASURED |
| Küçük slice process | LS-DOC-20 geçici 4 GB, cihaz ve memory pressure ile birlikte |
| Kare | 1080p p95≤16,67 ms, p99≤25 ms; cihaz garantisi değil |
| Authoring iş bütçesi | S023 planındaki 40–60 odak saati hedef; actual süre bilinmiyor |

Genişletme ancak rota, yoğunluk veya içerik eksikliği kullanıcı kanıtıyla ortaya çıkarsa değerlendirilir.
