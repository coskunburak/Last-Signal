---
doc_id: LS-PLAN-M02
project: Project Last Signal
version: 1.0.0
created: 2026-09-17
language: tr
status: PROPOSED_BASELINE
implementation_status: NOT_VERIFIED
---

# Kapsam ve karar yönetimi

## Kaynak önceliği

Güncel kullanıcı talimatı → kabul edilmiş ürün kararı → yürürlükteki sistem sözleşmesi → bu sprint planının uygulama sırası → tarihi GDD önerileri. Bu plan gameplay sayıları veya save sözleşmesini keyfi değiştirmez; üretim sırasını ve iş paketlerini genişletir. Çelişkiler karar kaydına yazılır. Codex veya NotebookLM yanıtı tek başına ürün kararı değildir.

## Yeni açık kararlar

| Kimlik | Durum | Kural / karar | Son kontrol |
|---|---|---|---|
| PLAN-ADR-001 | CONFIRMED | Önce SP; co-op yalnız SP yayın ve stabilizasyonundan sonra | G15 |
| PLAN-ADR-002 | PROPOSED | 60 iki haftalık sprint, 600 günlük kart | S001 kapasite kontrolü |
| PLAN-ADR-003 | PROPOSED | 48 SP + 12 koşullu co-op sprinti | S018 ve G15 |
| PLAN-ADR-004 | BASELINE | Unity 6/URP; gerçek repo sürümünü koruyarak doğrula | S001 |
| PLAN-ADR-005 | OPEN | Haftalık gerçek zaman ve asset bütçesi | S001; dört sprintte bir |
| PLAN-ADR-006 | BASELINE | First-person, slot+ağırlık inventory | S001–S002 UX |
| PLAN-ADR-007 | EXPERIMENT | Pressure ve çoklu sığınak değerli mi? | S009, S027 |
| PLAN-ADR-008 | OPEN | Son harita ve içerik miktarı | S018, S025 |
| PLAN-ADR-009 | OPEN | Gamepad ve ilk yayın dilleri | S017, S030 |
| PLAN-ADR-010 | OPEN | Yayın adı, mağaza, fiyat ve tarih | S030–S043 |
| PLAN-ADR-011 | DEFERRED | Co-op 2 mi 4 kişi mi? Ağ çözümü hangisi? | S049, S055 |
| PLAN-ADR-012 | PROPOSED | Repo Markdown ana kaynak; bilgi araçları türetilmiş erişim | S001, S047 |

CONFIRMED yalnız açık kullanıcı tercihidir. Plan ayrıntı istemi ağ entegrasyonu, asset satın alımı, dış davet veya ticari yayın izni değildir. Geri alınabilir normal uygulama ve düzeltmeler için tekrar tekrar onay istenmez. Dış yayın gibi eylemden önce somut aday hazırlanır; mevcut açık yetki varsa aynı izni yeniden isteme.

## Eski belgelerden sapmalar

LS-DOC-25 §3'teki post-M3 co-op spike imkânı bu plan için ertelenmiştir: S049 öncesi co-op implementation yoktur. LS-DOC-24 W00–W23 makro işler artık S001–S018 içinde ayrıntılandırılır; eski işlerin tamamlandığı iddia edilmez. GDD'deki 4 km², yüzlerce item ve çoklu final bu planla otomatik kesinleşmez. S025 harita baseline'ı gerçek üretim hızından çıkar. Netcode eklememek domain/test/persistence sınırlarını kaldırmaz.

## Must / Should / Could

Must: tam single-player loop, adil combat/AI, tek sahiplik, güvenli save/load, erişilebilir temel UI, hedef platform buildi, tamamlanabilir ana yol, tutarlı görsel/ses kimliği, kritik hatasız aday. Should: ek arketip, ikinci firearm, sınırlı crafting, seçilen gamepad/dil desteği ve anlamlı sığınak ilerlemesi; açık ürün vaadi olursa Must'a yükselir. Could: çoklu final, ek region, geniş katalog, ek event ailesi ve kozmetik varyasyonlar. Out: başlangıçta co-op; ayrıca kapsam kararı olmadan araç, PvP, büyük NPC fraksiyon, serbest yıkım/inşa, procedural dünya ve dedicated server.

## Değişiklik kartı

Her ürün değişikliği problem, oyuncu etkisi, etkilenen S/D kartları, sistem/REQ kimlikleri, art-audio-UI-save-QA maliyeti, seçenekler, geri dönüş ve karar durumunu taşır. Küçük teknik düzeltme mevcut kapsamda ilerler; büyük yeni içerik için kaynak karşılığı belirtilir. Yeni Must kabul edildiğinde başka iş çıkarılır veya takvim uzar. Planı izlemek uğruna yanlış hipotez korunmaz.

## Kapsam kesme sırası

Önce kozmetik varyant ve alternatif silah; sonra yan görev/event; sonra harita alanı/POI sayısı; sonra crafting/sığınak katmanı. Pressure veya shelter signature deneyi başarısızsa sadeleştirilebilir. Veri güvenliği, ana yolun sonu, anlaşılır ret feedback'i ve desteklenen platform kabulü kesilmez. Kesilen içerik için objective, loot ve persistent ID referansları ayrıca taranır.

## Durum modeli

Plan kartları başlangıçta SP için PLANNED, co-op için DEFERRED'dır; gerçek kod IMPLEMENTATION_NOT_VERIFIED'dır. Yürütmede NOT_STARTED → READY → IN_PROGRESS → VERIFYING → DONE; engelde BLOCKED, ürün kararında DEFERRED/CANCELLED. FAILED test sonucu kartın ürün durumundan ayrı kaydedilir. DONE için kanıt gerekir. Sadece MD kutusunun işaretlenmesi oyunun yapılması değildir.
