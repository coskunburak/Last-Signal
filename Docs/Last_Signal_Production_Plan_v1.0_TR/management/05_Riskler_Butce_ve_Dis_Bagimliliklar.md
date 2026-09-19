---
doc_id: LS-PLAN-M05
project: Project Last Signal
version: 1.0.0
created: 2026-09-17
language: tr
status: PROPOSED_BASELINE
implementation_status: NOT_VERIFIED
---

# Risk, bütçe ve dış bağımlılık kaydı

## Başlangıç risk envanteri

Bütün olasılık/etki puanları başlangıç önerisidir. Sahip şu anda Burak'tır; var olmayan ekip üyesine atama yapılmaz. İlk kontrol S001, sonra faz sonu ve somut tetikleyicide yapılır.

| Risk | Olasılık / etki | Tetikleyici | Önlem ve kontrol | İlgili sprint |
|---|---|---|---|---|
| R01 Kapsam büyümesi | Yüksek / yüksek | Her sprint yeni Must; bitmiş POI hızı düşük | G5 kapsam baseline, karşılıklı iş çıkarma | S018,S025,S030 |
| R02 Save/streaming | Orta / kritik | Duplicate item veya kayıp cell delta | Erken save, generation, fault fixture | S002,S007,S033 |
| R03 Eğlence belirsizliği | Yüksek / kritik | Oyuncu dönüş/sefer amacını anlatamıyor | Erken mikro loop, dış playtest, sadeleştirme | S004,S012,S018 |
| R04 Asset uyumsuzluğu | Yüksek / yüksek | Her asset özel rework istiyor | Tek benchmark POI, kit standardı | S013,S019 |
| R05 AI performansı | Orta / yüksek | Çoklu hedefte frame spike | Scheduler, capture, nüfus bütçesi | S009,S022,S031 |
| R06 Windows erişimi | Orta / yüksek | Hedef build hiç oynanmıyor | S017 gerçek cihaz/erişim çöz; test BLOCKED tut | S017,S034 |
| R07 Tek geliştirici kapasitesi | Yüksek / yüksek | Sürekli taşan kartlar, bakım birikimi | WIP sınırı, rezerv, dört sprintte forecast | Tüm fazlar |
| R08 Harici paket değişimi | Orta / yüksek | Güncelleme compile/save kırıyor | Lock dosyası, ayrı upgrade spike, rollback | S001,S034,S049 |
| R09 İçerik üretim hızı | Yüksek / yüksek | Bir POI planı çok aşıyor | Pilot saat ölçümü, alan/çeşit kesimi | S013,S023,S025 |
| R10 Katılımcı yokluğu | Orta / yüksek | Test oturumu gerçekleşmiyor | Erken takvim ve davet taslağı; gerçek durum | S004,S018,S037 |
| R11 Hak/dağıtım belirsizliği | Orta / yüksek | Kaynak veya izin belgesi yok | Asset envanteri; doğrulanamayanı çıkar | S013,S030,S040 |
| R12 Yayın sonrası hata yükü | Orta / yüksek | Tekrarlayan crash/save raporu | Küçük hotfix, kopya save, bakım rezervi | S044–S048 |
| R13 Co-op dönüşüm maliyeti | Yüksek / kritik | İki oyuncuda state drift | S049–S051 küçük spike; no-go seçeneği | P16 |
| R14 Uzak co-op oyuncular | Yüksek / yüksek | RAM/AI bütçesi aşımı | Interest union, iki/dört oyuncu ayrı karar | S055 |
| R15 Guest/save exploit | Orta / kritik | Reconnect kaynak çoğaltıyor | Host snapshot, ID, receipt, kopma fixture | S054,S056 |
| R16 Ağ hizmeti maliyeti | Belirsiz / yüksek | Trafik bütçesi öngörüyü aşıyor | Görev zamanında resmi koşul + ölçüm | S049,S057 |

## Harcama planı

Bu belgede fiyat veya gelir tahmini yoktur. Bütçe kalemleri: çevre/karakter/animasyon assetleri, font/müzik/ses, gerekirse uzman sanat/ses/çeviri yardımı, test donanımı/erişimi, mağaza başvuru giderleri, araç abonelikleri, yedekleme ve koşullu co-op hizmeti. Her kalemde gereklilik, mevcut varlık, teklif tarihi, resmi kaynak, tek seferlik/tekrarlı tutar, aylık yük ve satın alma yetkisi tutulur. Plan bir satın alma talimatı değildir.

Solo proje için nakit dayanımı hesabı kişisel bütçe verilmeden yapılmaz. Minimum yöntem: ayrılmış bütçe / aylık sabit gider; değişken üretim gideri ve kişisel yaşam giderleri ayrı değerlendirilir. Satış beklentisi tamamlanmış gelir gibi bütçeye yazılmaz. Publisher görüşmesi, sözleşme veya pazarlama kampanyası ayrı açık yetki gerektirir.

## Dış bağımlılık erken hazırlığı

Windows erişimini S017'den önce, ilk pilot katılımcılarını S004'ten önce, kaliteli slice katılımcılarını S018'den önce hazırlamak gerekir. Bu hazırlık mesaj gönderme yetkisi anlamına gelmez; ihtiyaç ve taslak önceden oluşturulur. Asset erişimi yoksa placeholderla risk deneyi yapılabilir; final asset kabulü BLOCKED kalır. Çeviri kapsamı S030'da seçilir, S035'te sonlandırılır. Mağaza ve co-op hizmet kuralları uygulama anında resmi kaynaklardan doğrulanır; bugünkü plan fiyat/sürüm iddiası taşımaz.

## Risk kapatma

Risk ancak tetikleyiciye uygun kanıtla azalır. Bir test yazmak veya bir asset satın almak tek başına risk kapanışı değildir. Risk gerçekleşirse issue ve actual süreye bağlanır. Rezerv tüketimi toplamı ayrı izlenir; rezerv bitince kapsam veya tarih kararı güncellenir.
