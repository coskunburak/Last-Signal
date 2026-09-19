---
doc_id: LS-DOC-25
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Gelecekte co-op için mimari sınırlar ve karar kapısı

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §23; kullanıcı single-player önceliği. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Ürün kararı

Co-op şimdi implementation hedefi değildir. Local Game Authority tek process içindeki state sahibidir; local network host açmak anlamına gelmez. İlk aşamada NetworkObject, RPC, Lobby, Relay veya dedicated server sistemi kurulması gerekmez. Kullanıcının ileride proje büyürse co-op değerlendirme niyeti korunur.

**REQ-NET-001:** Single-player core'un çalışması ağ oturumu veya internet servisine bağımlı olamaz. “İleride lazım olur” gerekçesiyle bütün domain network serialization attribute'larıyla kaplanmaz.

## 2. Korunacak sınırlar

Mutationlar command service'lerinden, state stable ID'lerden, presentation committed eventlerden geçer. Bunlar network dönüşümünü kolaylaştırır; bedelsiz veya yalnız wrapper değişikliğiyle co-op garantisi değildir. Hareket prediction, latency, client hit verification, interest management, streaming ownership ve UI concurrency ayrıca tasarlanacaktır.

| Alan | Bugün | Gelecekte araştırılacak |
|---|---|---|
| Movement | Local input/controller | Prediction/reconciliation |
| Combat | Local validation | Host hit/damage authority, latency policy |
| Inventory | Revision+atomic transfer | Concurrent clients, idempotency |
| AI | Local scheduler | Host simulation, client interpolation |
| World | Tek oyuncu load ring | Çok oyuncu interest union |
| Time | Pause/sleep | Shared clock, consensus sleep |
| Save | Local world owner | Host save ve guest profile |

## 3. İki oyunculu spike kapsamı

Post-M3 yalnız onaylanan ayrı görevde: host/client spawn, hareket, bir container, bir zombi, pistol, shared door, disconnect/reconnect ve host save/load. Host migration ilk spike şartı değildir; host çıkınca session kapanma davranışı açık yazılabilir. İnternetten önce loopback/LAN correctness, sonra latency/loss simulation, sonra seçilecek hizmet entegrasyonu.

Protokol aktarımı definition catalog checksum ve build compatibility ile başlar. Aynı isimli farklı damage tablolarıyla iki client kabul edilmez. PersistentEntityId ile network session ID mapping ayrı; reconnect eski transient ID'yi valid varsaymaz.

## 4. Eşzamanlı inventory

İki client aynı itemi alır: host aynı source revision'da ilk geçerli command'ı commitler, diğeri StaleRevision/Unavailable alır. Client optimistic UI gösterebilir, ama canonical inventory sonucu host'tan gelir. Retry aynı command ID ile yapılırsa ikinci ödül yok. Guest disconnect sırasında craft escrow host world'ünde sahipliğini korur veya defined cancel olur; item kaybolmaz.

**REQ-NET-002:** Co-op deneyi single-player save ownership invariantlarını gevşetemez. Network duplicate testleri mevcut transfer sözleşmesine dayanır.

## 5. Birbirinden uzak oyuncular

World ring oyuncuların interest alanlarının birleşimidir; 4 kişi dört köşeye dağılınca memory/AI maliyeti dört katına yaklaşabilir. Player-count slider yalnız bağlantı kapasitesi değildir. Population birden çok oyuncu tarafından iki kez materialize edilmez. Pressure shared world state; tek global difficulty rubber-band bütün haritayı aniden güçlendirmez.

Prototype map sınırı veya party distance limiti düşünülebilir; ürün kararı olarak açık anlatılır. Sessizce uzak oyuncunun çevresini unload edip düşürmek çözüm değildir. Shelter save/world owner politikası guest karakter transferinde migration ister.

## 6. Pause ve sleep

Solo pause globaldir; co-op'ta client menu açması dünyayı durduramaz. Sleep unanimous/majority veya kişisel dinlenme seçeneği ayrı tasarım deneyidir. Solo 8 saat time-skip davranışı multiplayer'a aynen taşınırsa uyanık oyuncunun dünyası bozulur. Survival pacing buna göre tekrar denge ister; bu maliyet upfront tahminde bulunur.

## 7. Go/no-go

Core eğlence kanıtı, kararlı save/transactions, hedef donanım bütçesi, geliştirici zamanı ve gerçek iki oyuncu talebi değerlendirilir. GDD 8-12 hafta spike rezervi söyler; bu production co-op süresi değildir. Networking seçimi yapılmadan bir kitaplığın en iyi olduğu iddia edilmez; aktif Unity sürümü ve ihtiyaçlarla karşılaştırılır.

**REQ-NET-003:** Co-op başarılı spike olmadan mağaza vaadi olamaz. Failed spike sonrası solo proje güvenli branch/content contract ile devam edebilmelidir. Bu doküman co-op kurulum talimatı veya uygulanmış entegrasyon raporu değildir.
