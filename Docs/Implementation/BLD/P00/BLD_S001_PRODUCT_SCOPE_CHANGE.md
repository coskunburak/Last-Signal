# BLD-P00-ADR-001 — Kontrollü modüler inşa kapsam değişikliği

Tarih: 2026-10-18 · Kart: BLD-D001 · Belge uygulaması: IMPLEMENTED · Karar: PENDING_OWNER · Doğrulama: TEST_NOT_RUN.

## Girdi ve karar yetkisi

Bağlayıcı plan: [BLD-D001](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/DAYS/BLD-D001_ilk-surum-snr-degisikligi.md), [karar kaydı](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/00_GOVERNANCE/01_DECISION_LOG.md). Kullanıcının P00 belgeleme yetkisi kesindir; yeni ekonomi ve release kapsamının resmî kabulü G0'da Burak'a aittir. Bu ADR tarihî GDD'yi değiştirmez, uygulanmaya hazır bir değişiklik kartıdır.

Kaynaklar: [LS-DOC-01](../../../Last_Signal_Docs_v0.2/sources/01_Kararlar_ve_Kapsam.md) §3 ADR-009, §5 REQ-GOV-002/003; [LS-DOC-02](../../../Last_Signal_Docs_v0.2/sources/02_Vizyon_Donguler_ve_Dunya.md) §1/3/7; [LS-DOC-14](../../../Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md) §1–3; [LS-PLAN-M02](../../../Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md) §Must/Should/Could ve değişiklik kartı. Kaynak satırları aşağıda mevcut HEAD'e aittir.

## Problem, seçenek ve oyuncu etkisi

Mevcut authored socket düzeni oyuncunun kaynak seferini görünür, kendisinin yerleştirdiği bir yapıya dönüştürme isteğini karşılamıyor. Yeni vaat, balta/odun/işleme/Craft Defteri üzerinden sığınak çevresinde kalıcı bir yapı kurabilmek. Bu vaat loot ve keşif yolunu zorunlu crafting zinciriyle kapatamaz.

| Seçenek | Oyuncu sonucu | Üretim ve veri maliyeti | Karar |
|---|---|---|---|
| Yalnız authored sockets | Bilinen üç modül; düşük yer seçimi özgürlüğü | En düşük; S010 korunur | Geri dönüş alternatifi; yeni vaadi tek başına karşılamaz |
| Controlled build zones | Claim edilmiş sığınak yakınındaki authored alanda sınırlı katalog ve güvenli yer seçimi | Orta/yüksek: node kalıcılığı, çok girdili escrow, zone/nav doğrulama, dinamik ID, migration | Seçilen çalışma önerisi; PENDING_OWNER |
| Sınırsız dünya inşası | Her yerde yap/yık | Çok yüksek ve ölçülmemiş: büyük dünya, fizik, nav, içerik ve QA | İlk ürün için reddedilen alternatif; OUT |

## Önce / sonra — uygulanmamış GDD patch sözleşmesi

| Belge ve paragraf | Yürürlükteki önce | G0 kabulünden sonra önerilen metin / ek |
|---|---|---|
| LS-DOC-14:18, §1 ilk cümle | “Mevcut binalar claim edilir; serbest voxel/yapı inşası ilk ürün kapsamında değildir.” | “Mevcut binalar claim edilir. Claim edilmiş sığınak yakınındaki authored build zone içinde, onaylı sınırlı katalogla kontrollü modüler inşa hedeflenir. Voxel, sınırsız arazi kazma ve dünya çapında serbest inşa/yıkım kapsam dışıdır.” Paragrafın utility/fast travel hükümleri korunur. |
| LS-DOC-14:26, §2 | Authored socket/alan, overlap ve kapı/nav açıklığı | S010 Bed/Storage/Workbench socket'ları aynı kalır; dış yapıların tek sahibi ayrı Construction domain'idir. Geçersiz placement kaynak tüketmez. |
| LS-DOC-01:50, ADR-009 | Araç, NPC fraksiyon, serbest inşa ilk slice dışında | ADR-009 silinmez; BLD-P00-ADR-001'e tarihli istisna bağlantısı eklenir; araç/NPC ve sınırsız inşa OUT kalır. |
| LS-DOC-02:68, §7 | Sınırsız serbest inşa başlangıç kapsamında değil | Bu hüküm korunur; bounded zone istisnası ve oyuncunun inşa etmeme rotası §1/3'e eklenir. |
| LS-PLAN-M02:42, scope | Kapsam kararı olmadan serbest yıkım/inşa OUT | Kontrollü zone istisnası yalnız bu ADR onaylanırsa MUST olur; diğer OUT'lar değişmez. |
| LS-DOC-07, §2/7; LS-DOC-17, §3/7 | Tek sahiplik, snapshot ve migration | Atomik çok girdili rezervasyon, durably committed receipt, unloaded delta ve yeni section migration sözleşmelerine çapraz referans; eski sözleşme zayıflatılmaz. |

Bu tablo değişiklik önerisinin tam kapsamıdır. Kaynak belgeler veya kaynak kartlar sessizce düzenlenmedi. Tarihî `S010` ile yeni programdaki `BLD-S010` farklı kimliklerdir.

## Zorunlu sınırlar ve downstream etki

MUST önerisi: kaynak hasadı, çok girdili işleme, Craft Defteri, metin/ikonlu preview, güvenli commit, kalıcı sınırlı yapılar ve ileride mütevazı modüler barınak. OUT: voxel, her araziyi kazma, tam yapısal çökme, co-op, sınırsız yapı sayısı/alanı, procedural dünya yenileme ve serbest yıkım. Sayısal limitler [kapsam bütçesinde](SCOPE_MATRIX_AND_BUDGET.md) tasarım hipotezidir.

| Phase | Plan referansı | Bu ADR'nin etkisi; şimdi uygulama yok |
|---|---|---|
| P00 | BLD-S001–002 | Ürün, teknik sözleşme ve G0 karar paketi |
| P01 | BLD-S003–004 | Node state, hasat, pickup ve feedback |
| P02 | BLD-S005–006 | Çok girdili crafting, defter ve mevcut S010 adaptörü |
| P03 | BLD-S007–009 | Validator, ghost, güvenli commit ve ilk persistence |
| P04 | BLD-S010 | İlk uçtan uca döngü; G4 genişleme kararı |
| P05 | BLD-S011–015 | Temel/duvar/çatı; küçük barınak ve G5 |
| P06 | BLD-S016–019 | Storage/istasyon/onarım/utility ve uzun vadeli üs; BLD-S019 G6 hedefidir |
| P07 | BLD-S020–022 | Ekoloji, geniş streaming/migration, ekonomi |
| P08 | BLD-S023–026 | Sanat/ses/erişilebilirlik ve optimizasyon |
| P09 | BLD-S027–028 | Alpha regresyon ve teslim |

Referanslar yalnız mevcut [program indeksinden](../../../BLD_Production_Knowledge_Base_v2_2026-10-17/PROGRAM_INDEX.md) alınmıştır; D017+ kartları bu P00 paketinin parçası veya bu çalışmanın çıktısı değildir. İlk güvenli save ve erişilebilir ret feedback'i P07/P08'e ertelenemez.

## Maliyet, geri dönüş ve kabul

Yeni MUST için takvim uzaması veya açık kapsam takası gerekir; [bütçe](SCOPE_MATRIX_AND_BUDGET.md) garanti olmayan aralıkları ve kesme sırasını verir. Öneri: takvim uzaması, önce kozmetik varyantları ve opsiyonel utility kapsamını azaltma; veri güvenliğini kesmeme. Art/audio/UI/persistence/QA ayrı maliyet kalemleridir.

G0 reddinde bu öneri REJECTED/DEFERRED olarak tutulur; tarihî GDD ve runtime zaten değişmediği için revert gerekmez. G4/G5 eğlence/değer kabulü başarısızsa modüler genişleme durur; authored socket + loot rotası sürer. Gelecekte yeni save üretilmişse eski binary ile açmak revert değildir: yeni section'ı koruyan uyumlu reader veya eski yedek gerekir.

Kabul için owner: eski paragrafı ve önerilen farkı karşılaştırır, MUST/OUT ve ekonomi maliyetini seçer, [G0 receipt](G0_GATE_RECEIPT.md) üzerindeki OD-01/02'yi imzalar. Belge hazırdır; scope onayı ve manual static evidence henüz yoktur.
