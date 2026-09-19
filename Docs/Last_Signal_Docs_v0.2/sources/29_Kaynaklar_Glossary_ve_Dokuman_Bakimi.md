---
doc_id: LS-DOC-29
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Kaynaklar, terimler ve dokümantasyon bakım standardı

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: GDD bütün bölümler; dış kaynaklar aşağıda. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Birincil proje kaynağı

Project_Last_Signal_Production_GDD_v0.1_TR.docx, 17 Eylül 2026, 34 sayfa. Güncel dosya bu görevde okunmuştur. Paket `reference/GDD_v0.1_Archive.md` içinde tablo ve metinleri düzen sırasıyla Markdown'a aktarır; Word sayfa/üstbilgi görünümü yeniden üretilmez. Arşiv içerik kaynağıdır, v0.2 karar sapmalarını içermez. Önceki On Hold oyunu kaynak kodu veya test sonuçları bu projenin kanıtı değildir.

Kullanıcının görünür talimatı: single-player öncelikli survival projesi; detaylanıp büyürse co-op; GDD'den ayrıntılı .md kaynakları; NotebookLM ve gelecekte MCP/Codex kullanımı. Çalışma adı, kurgu, dünya alanı ve signature mekanikler tasarım önerileridir. Bu paket resmi bir proje kabul toplantısı tutanağı değildir.

## 2. Kontrol edilen dış belgeler

| Ref | Birincil kaynak | Bu pakette desteklediği dar konu | Kontrol |
|---|---|---|---|
| EXT-01 | [OpenAI Codex MCP](https://developers.openai.com/codex/mcp) | MCP yönetim biçimleri ve bağlantı kavramı | 2026-09-17 |
| EXT-02 | [OpenAI AGENTS.md](https://developers.openai.com/codex/guides/agents-md) | Proje talimat dosyası keşfi | 2026-09-17 |
| EXT-03 | [Google kaynak türleri](https://support.google.com/notebooklm/answer/16215270) | Markdown kaynak desteği ve kaynak çalışma biçimi | 2026-09-17 |
| EXT-04 | [Unity Addressables handles](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/AddressableAssetsAsyncOperationHandle.html) | Asenkron handle yaşam döngüsü | 2026-09-17 |

Bu bağlantılar oyun sayıları, süre tahmini, mimari tercihler veya ticari başarıyı doğrulamaz. Kaynak başlıkları/URL yönlendirmeleri değişebilir. Paket sürümü belirlemek için gerçek manifest ve ilgili sürüm belgeleri yeniden kontrol edilir. NotebookLM MCP sağlayıcısının varlığı veya resmi Google desteği bu dört kaynakla kanıtlanmış değildir.

## 3. Terim sözlüğü

| Terim | Bu projede anlam |
|---|---|
| Authority | State değişikliğini doğrulayıp uygulayan sahip |
| Definition | Değişmeyen catalog verisi |
| Instance | Tekil eşyanın/dünya varlığının değişebilir durumu |
| DTO | Disk/transfer için bağımsız veri temsili |
| Transaction | Birlikte başarıya ulaşan veya hiç değişmeyen mutation grubu |
| Commit | İşlemin authoritative sonuca dönüştüğü sınır |
| Revision | Bir aggregate'ın değişiklik sayacı |
| Idempotency | Tek isteğin tekrarının ek sonuç üretmemesi |
| Tombstone | Başlangıç entity'sinin artık yok/consumed olduğunu koruyan kayıt |
| Hydration | Kayıtlı domain state'ini aktif dünya temsiline kurma |
| Lease | Cell/asset ömrünü belirli işlem boyunca koruyan sahiplik |
| Escrow | Craft sırasında rezerv edilen ama henüz tüketilmeyen malzeme |
| Population token | Uzak grubun fiziksel agent olmadan tutulan nüfus temsili |
| Simulation LOD | Önem/mesafeye göre hesap ayrıntısı |
| POI | Ayrı oynanış vaadi olan keşif noktası |
| Pressure | D/P/depletion bölgesel hafıza sistemi |
| Telegraph | Tehlikenin sonucu gelmeden önce okunur işareti |
| Vertical slice | Küçük içerikle nihai kalite ve tam döngü kanıtı |
| Gate | Sonraki aşamaya geçmek için kanıtlanacak koşullar |
| Baseline | Çalışma başlangıcı; kesin kullanıcı onayı değildir |
| p95 | Ölçümlerin %95'inin altında kaldığı değer |
| World time | Açlık/hava/uzun olaylar için hızlandırılmış saat |
| Simulation time | Eylem/hareket/çatışma için geçen süre |

## 4. Belge güncelleme kuralları

Yeni gereksinime yeni REQ kimliği; mevcut kimlik başka anlam için tekrar kullanılmaz. Cümle küçük düzeltildiğinde ID korunur, change note yazılır. Davranış kaldırılırsa requirement deprecated kaydıyla arşivlenir. Linkte dosya adı korunması mümkünse tercih edilir; dosya rename'de tüm inbound linkler güncellenir.

Front matter: doc_id, project, version, date, status, implementation_status, source_gdd. Dosya adı ASCII; içerik UTF-8 Türkçe. Kod/şema identifier'ları İngilizce. Başlıklar kendi bağlamını taşır; “yukarıdaki sistem” gibi yalnız bir birleşik dosyada anlamlı referanslardan kaçınılır.

## 5. Güncelleme etki örnekleri

Walk speed 3.2→3.6: balance, movement kabul toleransı, slice yolculuk süreleri ve test fixture. Save schema değişmesi gerekmez. Item instance ownership modeli değişirse inventory, interaction, craft, save migration, co-op araştırması ve requirement matrix etkilenir. Kamera first→third kararı player, art rig, animation, stealth visibility ve UI'ı etkiler; küçük tuning değildir.

## 6. Doküman QA

UTF-8 okunabilirlik, benzersiz doc/REQ ID, relative link varlığı, code fence dengesi, metadata varlığı, dosya hash'i, ZIP integrity. Ayrıca içerik düzeyinde clock units, scope aşamaları, save sahipliği ve “uygulandı” iddiaları kontrol edilir. Hash güncellemesi içerik doğruluğu kanıtı değildir; yalnız dağıtılan dosyanın kimliğini doğrular.

Bu paket içinde script veya çalışan oyun projesi teslim edilmez. Kod/JSON blokları örnek sözleşme ve prompttur. Detaylar production geliştirmeyi yönlendirmek içindir; uygulama, platform doğrulaması ve oyuncu testi tamamlanmadan ürün hazır kabul edilemez.
