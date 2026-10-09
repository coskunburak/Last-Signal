# S018 D178–D179 — kapasite ve kapsam

**PARTIAL — yöntem ve senaryolar hazır; actual üretim kapasitesi NOT_VERIFIED.** Scope baseline onayı PENDING; ticari karar Burak'a aittir. Sayısal saat/tarih/nakit veya bitmiş içerik adedi icat edilmedi.

## Gerçek emek verisi araştırması

Seçici olarak S012 implementation report actual bölümü, S014 working ledger, S015 implementation report, S016 ve S017 çalışma kayıtları incelendi. S012 açıkça kesintiler/odak saati ölçülmediği için toplam actual'ın bilinmediğini söylüyor. S014–S017 kayıtları yapılan işi ve test sürelerini gösteriyor; bitmiş birim başına tüm disiplinleri kapsayan kişi-saat kaydı bulunmadı. Bu, bütün Docs içinde hiçbir kayıt olmadığı iddiası değildir; owner kayıtları gelirse örneklem genişletilir.

S013/S014 build saniyeleri, test wall-clock süreleri, commit tarihleri veya dosya mtime'ı içerik emek saati değildir. Canonical sprint 40–60 saat **plan**dır; actual yerine kullanılamaz. Agent etkinliği de insanın art/entegrasyon/QA emeği yerine geçmez.

| Bitmiş birim | Kabul edilmiş tam-maliyet örnek n | Median / düşük–yüksek kişi-saat | Güven | Eksik veri |
|---|---|---|---|---|
| Final POI | 0 | NOT_ESTIMABLE | Yetersiz | S013 POI art+audio+integration+insan kabulü ve actual dökümü |
| Düşman varyantı | 0 | NOT_ESTIMABLE | Yetersiz | AI/presentation/anim/audio/QA ve yeniden işleme |
| Item ailesi | 0 | NOT_ESTIMABLE | Yetersiz | Model/icon/loot/catalog/UI/save/QA, üyelerin adedi |
| Silah | 0 | NOT_ESTIMABLE | Yetersiz | Kod/model/anim/audio/ammo/persistence/denge/QA |
| Quest/event | 0 | NOT_ESTIMABLE | Yetersiz | Design/scripting/art/audio/UI/save/out-of-order/reward QA |

n=0, projede bu içeriklerin olmadığı anlamına gelmez; bitmiş kabul+actual maliyet çifti yoktur. Sayısal aralık ancak ölçüm veya açıkça etiketlenmiş owner tahmini geldiğinde yazılabilir.

## Owner veri formu ve hesaplama

Her gerçek üretim birimi için: ID/tür, kapsam/adet, tamamlanma+kabul evidence, tarih aralığı; **code + art + animation + audio + UI + save/persistence + integration + QA** actual kişi-saat; rework; dış destek ve nakit gider; bekleme süresi; tekrar kullanılabilir altyapı payı; ölçüm kaynağı; eksik kalemler. Boş disiplin 0 sayılmaz. İlk kurulum ile sonraki tekrarın maliyetini ayırın; ortak maliyeti her birime tekrar yüklemeyin ve tamamen unutmayın. QA/öğrenme/rework maliyetini saklamayın.

Kabul edilmiş örnekler geldikten sonra her birim için n, median, gözlenen min/max, belirsizlik ve örnek çeşitliliği yazılır. n=1 için min=max bir güven aralığı değildir. Henüz bitmemiş örneklerin spent-to-date değeri ile estimate-to-complete ayrı kalır.

Scope emek aralığı = birim adedi × gözlenen birim maliyet aralığı + bir kez sayılan ortak entegrasyon/QA/migration + açık rezerv. Kapasite = gerçek sürdürülebilir odak saati/hafta; takvim aralığı emek/kapasite + dış bekleme. Geçmiş odak saati, gelecek müsaitlik, rezerv tüketimi ve rework verisi olmadan teslim tarihi verilmez. Nakit için mevcut runway/aylık gider/dış içerik gideri owner girdisidir; harcama yapılmaz.

## Dar ve geniş ticari sürüm karşılaştırması

Bu senaryolar kapsam onayı veya içerik taahhüdü değildir. Gerçek oyuncu kanıtı yokken hangi signature mekaniğin korunmaya değer olduğu doğrulanmış sayılmaz.

| Boyut | NARROW RELEASE | BROAD RELEASE |
|---|---|---|
| Ürün bütünlüğü | Daha küçük tamamlanabilir oyun; güvenilir save/load, okunur combat, temel survival loop, testte olumlu bulunan signature ve bitmiş içerik | Aynı temel kalite + daha fazla bölge/POI/çeşit; ek içerik ancak throughput ve talep desteklerse |
| Önce kesilecekler | Kozmetik varyasyon, ekstra silah, yan event, ikincil içerik ve bölge sayısı | Bu kalemler art/anim/audio/UI/QA maliyetini artırır; zorunlu must-have değildir |
| Dünya/kayıt | Daha az kombinasyon; migration, duplication ve progression güvenliği yine korunur | Daha çok persistent state/cell/quest kombinasyonu ve save uyumluluk yükü |
| Art/audio | Daha az ama kabul edilmiş tutarlı içerik; geçici cue/asset kabulü kapanmalı | Daha çok üretim, stil eşleştirme, lisans ve entegrasyon işi |
| QA/platform | Desteklenen cihazlarda bütün çekirdek yol ve gerçek Windows | İçerik × cihaz × platform × save-state matrisi büyür |
| Takvim/nakit | Sayısal tahmin PENDING; küçük kapsam bile mevcut unknown throughput ile taahhüt edilemez | Sayısal tahmin PENDING; maliyet ve risk artışı nitel olarak açık, katsayı icat edilmez |
| Karar için kanıt | Oyuncu tekrar isteği/loop anlaşılması + bitmiş birim maliyeti + runway | Bunlara ek tekrarlanabilir üretim pipeline'ı, geniş içeriğe gerekçe, bütçe/rezerv ve QA kapasitesi |

Varsayımlar: single-player sürer; aynı engine/paketler; yeni takım veya dış finansman varsayılmaz; co-op başlamaz; yeni asset satın alımı yok. Consumer quality'den ve save güvenliğinden keserek maliyet düşmüş gösterilmez. Owner içerik adetleri, sürdürülebilir saatler ve nakit sınırlarını henüz vermedi.

Şimdiki karar girdisi: önce gerçek veri topla. “Dar kapsam daha az riskli” yönü, onaylanmış release kapsamı demek değildir. D179 owner alanı: seçilen senaryo/adetler, dahil/dışarı, varsayımlar, toplam aralık, rezerv, tarih, onay gerekçesi **PENDING**. G5 birincil önerisi [RETEST](S018_G5_GATE_REPORT.md).


## Owner'dan gelen süre bilgisi — 2026-10-08

Owner: “2 3 haftalık süreçte toplam 1 saatlik bir denemem olmuştur.” Owner devamında bunun **oyunu oynama / manuel test süresi** olduğunu açıkça belirtti. Bu, yaklaşık 1 saatlik owner test maruziyetidir; üretim emeği, bitmiş içerik maliyeti veya haftalık sürdürülebilir geliştirme kapasitesi değildir. Oturum/build/log ayrıntısı verilmediği için belirli bir acceptance PASS veya dış oyuncu katılımı olarak sayılmaz. 2–3 hafta takvim aralığı odak süresi değildir. Bitmiş birim/type, disiplin dağılımı ve kabul evidence henüz yok; actual tam-maliyet örnek n=0 kalır.

### Bundan sonraki gerçek kayıt için tek satır şablonu

`Birim ID/tür | teslim sınırı/adet | ölçüm kaynağı | code | art | animation | audio | UI | save | integration | QA | ortak maliyet payı | rework (toplama dahil mi?) | dış bekleme | acceptance evidence | remaining work`

Saatler eksikse UNKNOWN; ölçülmüş sıfır ile boş alan ayrılır. Rework alt kırılımı ana disiplin saatine dahilse ikinci kez toplanmaz. Owner beyanı tahmin ise ESTIMATE etiketiyle actual örnekleminden ayrılır. İlk kabul edilmiş örnekten önce median/range çıkarılmaz; tek örnek üretim güvenilirliğini kanıtlamaz. Bu kayıt için yeni içerik üretme sprinti başlatılmaz.

## D179 karar tutanağı — hazırlanmış, onaylanmamış

| Karar alanı | Değer / kaynak |
|---|---|
| Baseline candidate ve player evidence | PENDING — gerçek oturumlar yok |
| Dar senaryo birimleri (POI/enemy/item family/weapon/event) | Owner adetleri PENDING; mevcut varlık sayısı yayın kapsamı değildir |
| Geniş senaryo birimleri | Owner adetleri PENDING |
| Kabul edilmiş actual örnekler / n / median / low–high | n=0; NOT_ESTIMABLE |
| Sürdürülebilir kişi-saat/hafta | PENDING; 1 saat manuel test beyanı bu alanı doldurmaz |
| Rework/rezerv / bağımsız bekleme | PENDING; plan rezervi actual tüketim değildir |
| Runway ve dış harcama sınırı | Owner girdisi PENDING; herhangi bir satın alma yetkisi yok |
| Varsayımlarla toplam emek/takvim aralığı | NOT_ESTIMABLE; yalnız yeterli girdiyle hesaplanır |
| Seçim (NARROW / BROAD / kararı ertele) | PENDING — owner kararı |
| Dahil / hariç / erteleme gerekçesi | PENDING; güvenilir save ve temel okunurluk kesilmez |
| Owner / tarih / kabul edilen riskler / yeniden değerlendirme tetikleyicisi | PENDING |

Veri hiç tutulmamışsa D178 ancak “güvenilir üretim hızı çıkarılamadı” sonucunu dürüstçe verebilir; bu sonuç BROAD affordability onayı değildir. Sonraki gerçek, zaten yetkilendirilmiş üretim işi ölçülür ve kabul edilene kadar completed sample sayılmaz. D180 için mevcut birincil öneri RETEST korunur; bu karar formu otomatik ticari onay üretmez.
