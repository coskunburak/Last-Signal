# FirstTownCore ilk playable kabul senaryosu

BLD-D006 · BLD-P00-DEC-006 · PROPOSED · Belge IMPLEMENTED / TEST_NOT_RUN.
Girdi D006, US-01–12, SC-01–09; kaynak bağlantıları [S001 handoff](BLD_S001_HANDOFF.md). Sahip Burak; uygulama/kabul BLD-S010/G4. Alternatif tüm modüler evi ilk demo yapma reddedildi: core loop öğrenilmeden katalog maliyeti büyür. Geri dönüş: aynı rota klasik loot kabulü olarak kalır.

## Rota ve başlangıç

Topolojik rota: claimed mevcut sığınak → mevcut yol/orman kenarı yaklaşımı → izinli resource patch → alternatif geri dönüş yolu → aynı sığınak/tezgâh → dış authored build zone. Gerçek waypoint/koordinat, orman node yerleri ve yürünebilirlik NOT_INSPECTED; sahne açılmadı. BLD-S003/S007 authoring ve BLD-S010 owner yürüyüşü önkoşuldur. İki rota aynı portal/teleport ile birleştirilmez; FirstTownCore fiziksel sürekliliği korunur.

Başlangıç fixture'ı: mevcut çalışan S010 shelter/storage/bench; garantili erişilebilir balta kaynağı; [değer haritası](SURVIVAL_VALUE_MAP.md) kadar sonlu hasat/yedek bağlayıcı; hazır cell; boş envanter kapasitesi ve geri dönüş yolu. Bu fixture bugün sahneye eklenmedi. Fixture olmazsa demo BLOCKED_SETUP olur; ücretsiz kaynak enjekte edilerek başarı diye sunulmaz.

## Mutlu yol — 15–30 dakika hedef, NOT_MEASURED

| Aşama | Hedef süre | Oyuncu eylemi / gözlenen sonuç | Gelecekte alınacak kanıt |
|---|---|---|---|
| Hazırlık | 2–4 dk | US-01; maliyet ve kapasiteyi gör, balta edin | Start inventory + source IDs, UI okunabilirliği |
| Gidiş | 3–6 dk | Orman kenarı veya ana yol seç; ışık/yağmur/tehdit değerlendir | Gerçek rota süresi, alternatif yaklaşım, kaçış |
| Hasat/taşıma | 3–6 dk | US-02/03; sonlu odun al, fazla yükte miktar seç | Node revision, world pickup ve inventory toplamı |
| Dönüş/işleme | 3–6 dk | Aynı sığınağa dön, depola, tezgâhta işle; defterde yapı seç | Craft job/escrow/output receipt |
| Preview/inşa | 2–4 dk | Çiti döndür; kapıyı açık bırakan konumu onayla | Tek nonce, tek instance, doğru maliyet; görünür fayda |
| Save/reload | 2–4 dk | Kaydet, unload/reload yap, sığınaktan aynı yapıya geri yürü | Save generation, aynı ID/pose/condition; depleted node değişmedi |

Toplam hedef 15–30 dk. Hedefin aşılması tek başına teknik veri hatası değildir; owner gözlemiyle pacing/mesafe/maliyet yeniden planlanır. Ana kabul, dışarı çıkış→kaynak→işleme→yer seçimi→kalıcı fayda zincirinin kesintisiz olmasıdır.

## Alternatif ve ret yolları

| Fixture değişimi | Beklenen sonuç / toparlanma | Kabul |
|---|---|---|
| 1 bağlayıcı eksik | MissingMaterials; eksik adet görünür, loot rotasına dön | Malzeme ve yapı delta 0; ana survival açık |
| Kapı sweep'ine veya çok eğimli yüzeye ghost | PathBlocked/Slope + metin; düz izinli yere taşı | Ret sırasında görünmez collider/nav engeli yok |
| Preview→confirm arasında stok değişir | SourceChanged; maliyet tekrar gösterilir | Eski preview zorla commit edilemez |
| Düşman yaklaşır / ölüm | Defteri kapat, kaç; ölümde yeni commit ret | Mevcut combat girdisi geri gelir; yarım nesne yok |
| Job output/refund container dolu | WaitingOutput/WaitingRefund; yer aç, collect | Hiçbir çıktı/iade silinmez veya iki kez verilmez |
| Save/scene unload commit ile yarışır | Busy veya durable sonuç; nonce sorgulanır | Hem kaynak hem yapı birlikte önce/sonra; yarım state yok |
| İnşa etmeden oyna | US-12 klasik loot, su ve gece recovery | Aynı ana ilerleme bir sonraki hedefi erişilebilir |

## G4 stop/go, G0 farkı

G4 GO: owner mutlu yol + alternatif loot rotasını oynar, ret/iptal/load kanıtları kaynak korur, çit anlamlı fayda verir. G4 REWORK: oyuncu neden kurduğunu açıklayamıyor, grind rotayı eziyor veya feedback anlaşılmıyor. Herhangi bir dupe/loss, kapalı kaçış veya save recovery hatası G4 BLOCKED. G0 yalnız bu kabul tasarımını onaya hazırlar; gameplay sonucu veya G4 yetkisi vermez.
