---
doc_id: LS-PLAN-M01
project: Project Last Signal
version: 1.0.0
created: 2026-09-17
language: tr
status: PROPOSED_BASELINE
implementation_status: NOT_VERIFIED
---

# Planlama modeli, kapasite ve takvim

## Öncelik ve bağlayıcılık

Kullanıcının kesin talimatı önce single-player, en son co-op'tur. Bu paket 600 **günlük iş paketi** içerir; 600 sprint içermez. S001–S048 single-player geliştirme, yayın ve stabilizasyonudur. S049–S060 yalnız G15 kapanışı ve ayrı co-op GO kararı sonrasında açılır. Single-player aşamasında NetworkObject, RPC, lobby, relay veya local network server kurma işi yoktur. Command, kalıcı ID ve presentation ayrımı bugünkü kayıt/test ihtiyaçları için korunur.

Takvim, tek ana geliştirici Burak için sıralı kaynak planıdır. AI çalışma desteğidir; bağımsız tam zamanlı personel kapasitesi gibi çarpan uygulanmaz. Harici sanatçı, testçi veya Windows cihazı var kabul edilmez; ilgili girdiler yoksa BLOCKED görünür. Mimari ve içerik yeni projede henüz doğrulanmamıştır. On Hold test sonuçları taşınmaz.

## Sayısal model

| Kalem | Single-player | Koşullu co-op | Birlikte |
|---|---:|---:|---:|
| Faz | 16 | 4 | 20 |
| Sprint | 48 | 12 | 60 |
| Günlük kart | 480 | 120 | 600 |
| Kart başına plan aralığı | 4–6 saat | 4–6 saat | 4–6 saat |
| Kartların odaklı emek aralığı | 1.920–2.880 saat | 480–720 saat | 2.400–3.600 saat |
| İki haftalık sprint üretim süresi | 96 hafta | 24 hafta | 120 hafta |
| Ayrı rezerv önerisi | 8 hafta | 4 hafta | 12 hafta |
| Nominal takvim | 104 hafta | 28 hafta | 132 hafta |

Bu saatler ayrıntılı tahminlenmiş bottom-up teklif değildir; 600 gün kutusunun kapasite modelidir. Bir kartın içeriği 4–6 saate sığmıyorsa kutuya sığmış gibi DONE yapılmaz. Kart bölünür, içerik azaltılır veya takvim güncellenir. Büyük harita/art kartları bu yüzden ilk gerçek üretim hızından sonra yeniden tahminlenir. Yaklaşık 104 hafta iki yıl; 132 hafta iki buçuk yıl civarıdır. Co-op dahil bütün planın iki yılda biteceği iddia edilmez.

Haftada 30–40 saat çalışma varsayılır: 20–30 saat kartlara odaklı çalışma, kalan kapasite iletişim, indirme/import beklemesi, günlük küçük düzeltme ve üretim organizasyonu için bırakılır. Her kartın kendi doğrulaması 4–6 saate dahildir; QA iki kez eklenmez. Büyük hata yeniden çalışması ve izin/hastalık ayrı rezervdedir. Haftada 15–20 saat ayırılıyorsa aynı kapsamın takvimi kabaca uzar; net süre ölçülen kapasiteden hesaplanır. Haftasonu zorunlu mesai planlanmaz.

## Gün, sprint ve faz ayrımı

Bir gün kartı 4–6 saatlik hedef çıktıdır; gerçek hayatta birden fazla güne taşabilir. Bir sprint 10 iş günü / iki hafta planlama aralığıdır. Bir faz üç sprint / altı üretim haftasıdır. Faz kapısı yalnız tarihle kapanmaz. Sprint bitince kalan iş görünürce taşınır; sonraki sprint sessizce üstüne bindirilmez. Ayrı gating hatası varsa feature çalışması yerine bu risk çözülür.

Günlük akış: 20–30 dakika önceki kanıt/kapsam okuma; 2–3 saat ana uygulama; 1–2 saat entegrasyon ve uygun doğrulama; 20–30 dakika sonuç/actual saat/handoff. Saatler günün ihtiyacına göre değişir. Metin düzeltmesi için gereksiz Unity testi yazılmaz. Inventory/save değişiminde anlamlı negatif test ve ilgili build akışı gerekir.

Her iki haftalık sprintin ilk gününde 30 dakikalık scope ve bağımlılık kontrolü; beşinci gününde 20 dakikalık risk/kapsam değerlendirmesi; onuncu gününde görünür demo ve 30 dakikalık retro vardır. Bunlar günlük kutu içinde planlanır. Her sprintin sonunda tüm oyunu saatlerce test etmek zorunlu değildir; risk matrisi uygulanır.

## Rezerv yerleşimi

Single-player rezervleri S012 sonrası 2 hafta, S024 sonrası 2 hafta, S036 sonrası 2 hafta, S042 sonrası 1 hafta, S045 sonrası 1 haftadır. Co-op rezervleri S051 sonrası 1 hafta, S054 sonrası 1 hafta, S057 sonrası 1 hafta, S059 sonrası 1 haftadır. Rezerv 600 karta yeni özellik gibi yazılmamıştır. Rezerv kayıtlarında hangi hata, öğrenme, izin veya dış engelin tükettiği tutulur. Kullanılmayan rezerv takvimi öne çekebilir; sırf boş diye yeni silah eklenmez.

Faz/sprint dosyalarındaki hafta numaraları **rezerv hariç üretim haftasıdır**. Gerçek başlangıç tarihi bilinmediği için takvim tarihi atanmaz. Co-op hafta 97 yazması SP yayınından önce başlayabileceği anlamına gelmez; bütün önceki rezervler ve G15 kararı önkoşuldur.

## Tahmin güveni ve yenileme

S001–S006 yakın dönem ayrıntı, S007–S018 orta güvenli, S019–S048 düşük güvenli üretim tahmini, S049–S060 koşullu taslaktır. Uzak kartlar ayrıntılıdır ama tasarım keşfini dondurmaz. Dört sprintte bir gerçek süre, yeniden açılma, blocker günü ve bitmiş içerik başına süreyle kalan iş yeniden tahminlenir.

Hesap: kalan olası odak saati / ölçülen haftalık net saat + ayrı rezerv ve dış bekleme. Sadece tamamlanan küçük işler değil yeniden çalışma ve yarım kalan işler de örnekleme katılır. Üç noktalı tahmin için iyimser, olası ve riskli saat ayrı tutulur; olası değeri kesin tarih gibi sunma. Henüz bu proje için gerçek çalışma verisi yoktur.

## Önceki 12–18 aylık senaryo ile ilişki

Önceki konuşmada 12–18 ay daha dar ticari sürüm içindi. Buradaki 48 sprint kapsamlı üretim, yayın ve bakım planı yaklaşık iki yıllık üst çalışma baseline'ıdır. S018 kapısında dar sürüm seçilirse bölge, item, silah, yan görev ve sığınak çeşitleri azaltılarak bütün bağımlılıklar yeniden çizilir. 48 sprinti sadece daha kısa adlandırmak süre kazandırmaz. Kayıt güvenliği, ana yol, gerçek platform testi ve yayın kabulü dar sürümde de korunur.

İlk güçlü kanıt hedefi S004 / 8. üretim haftasında mikro loop'tur. S012 / 24. haftada bütünleşik gri kutu; S018 / 36. haftada geniş kapsamlı kaliteli slice ve üretime geçiş kararı vardır. Daha erken demo ancak ayrı azaltılmış kapsamla mümkündür. Bu ayrıntılı plandaki S018 hedefi önceki küçük demo tahminiyle aynı kapsam değildir.
