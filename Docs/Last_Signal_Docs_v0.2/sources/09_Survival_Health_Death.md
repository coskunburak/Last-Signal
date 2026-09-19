---
doc_id: LS-DOC-09
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Survival statları, yaralar, tedavi ve ölüm

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §8-9, §18. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Zaman tabanı

Health/stamina kısa eylem fiziği için simulation seconds, hunger/hydration/fatigue/spoilage world seconds kullanır. Default 24 oyun saati 120 gerçek dakikadır: world rate 12. Pause bu iki zamanı durdurur; UI gerçek zamanla açık kalır. Craft/sleep time skip yalnız world service üzerinden ilerler.

**REQ-SURV-001:** Offline geçen duvar saati, varsayılan solo kayıtta karakteri açlıktan öldürmez. Load, kayıtlı world time'dan devam eder. Sistem saati değiştirmek item tazeliğini sıfırlamaz.

## 2. Değerler ve eşikler

| Stat | Başlangıç | Orta uyarı | Kritik / davranış |
|---|---|---|---|
| Health | 100 | ≤40 | 0 death transaction |
| Hydration | 100 | ≤40 | ≤10 belirgin regen cezası; 0 yavaş health drain |
| Nutrition | 100 | ≤30 | 0 recovery kaybı ve gecikmeli health etkisi |
| Restedness | 100 | ≤35 | ≤10 aim/stamina cap; blackout slice dışında |
| Stamina | 100 | ≤25 | 0 sprint/heavy attack reddi |
| Temperature | 37.0 tasarım birimi | Comfort band dışı | Süre+severity ile hypothermia |

Bu eşikler tıbbi model değildir, gameplay hipotezidir. UI fatigue adını kullanırken yüksek değerin iyi/kötü anlamı tutarlı olmalı; storage alanı restedness olarak adlandırılır. GDD'deki “Fatigue 100 rested” belirsizliği böyle çözülür.

## 3. Formüller

Hydration düşüşü = baseDrainPerWorldSecond × exertion × ambientHeat × illness. Başlangıçta 12 oyun saatinde 100 puan: base=100/43200. Bir gerçek dakikada dünya 720 saniye ilerler, kayıp yaklaşık 1.667 puan; dolu bar yaklaşık 60 gerçek dakika sürer. Bu pacing playtestte doğrulanır.

Stamina regeneration = baseRegenPerSimSecond × hydrationModifier × restednessModifier × painModifier; modifiye katsayılar bounded. Birçok negatif effect stacklenip 0.01 hız üretmesin diye minimum normal walk sınırı vardır. Health drain, damage events üzerinden aynı ölüm yoluna gider.

Temperature basitleştirilmiş exposure modelidir: wetness, ambient, wind shelter, insulation, heat source girdileri. Dış yağmur doğrudan çatı altındaki oyuncuyu ıslatmaz; exposure mask/volume gerekir. Bir metre ileri gidince core temperature aniden değişmez; smooth integration ve eşik hysteresis kullanılır.

## 4. Status effect modeli

EffectInstance: definitionId, sourceId, targetZone, appliedWorldTime, remainingDuration, severity, stackPolicy, treatmentTags. StackPolicy Refresh/ReplaceIfStronger/AdditiveCapped/Unique olabilir. Bleeding farklı woundId'lerde olabilir, aggregate drain üst sınır taşır. Painkiller ağrıyı bastırır; yarayı iyileştirmez. Wet kıyafet kuruyunca ısı etkisi azalır, global boolean kalmaz.

**REQ-SURV-002:** Aynı wound'a iki tedavi isteği aynı consumable'ı iki kez uygulayamaz. Treatment commit wound revision ve item revision doğrular.

## 5. Tedavi state machine

Inspect → SelectTreatment → Validating → Applying → Committed → Recovery. Applying sırasında hareket kısıtlı; sprint veya hasar iptal edebilir. Önerilen slice politikası: bandaj 3 simulation seconds sonunda tek commit; önce iptalde bandaj tüketilmez, harcanan zaman/stamina geri verilmez. Commit sonrası iptal bandajı geri yaratmaz. Antiseptic charge ile wound contamination aynı committe değişir.

Yanlış tedavi teklif edilmez veya neden etkisiz olduğu açıklanır. “Medicine spam” için erken aşamada toxicity alt sistemi kurmak zorunlu değildir; heal miktarı, effect cap ve uygulanabilirlik yeterlidir. Fracture/splint ileri slice/alpha; temel slice bleeding, pain, wetness ve mild cold ile başlar.

## 6. Ölüm ve recovery

**REQ-SURV-003:** Death bir kez işlenir. Health=0 sonrası yeni consume/fire/loot reddedilir; inventory kaybı, bag entity ve recovery anchor tek tutarlı state geçişi oluşturur.

Survivor önerisi: son claimed shelter'a dön, inventory policy'ye göre eşyaları death bag'e koy, geçici restedness/health cezası uygula. Shelter hiç yoksa authored başlangıç recovery anchor kullanılır. Death bag erişilemez geometrideyse yakın güvenli noktaya taşınır; void içine bırakılmaz. İkinci ölüm ilk bag'i anında silmez; aktif bag sayısı ve cleanup açıkça sınırlanır. Quest receipt bilgisi world progression'da kalır, kaybolan kritik item recovery kurallarıyla yeniden edinilir.

Permadeath veya ölümcül infection D-002/D-004 kararıdır; standart slice kuralıyla aynı save'de gizlice açılmaz. Farklı ölüm profilleri save metadata'da görünür. Ölüm ekranı kaynak, wound, son hasar ve recovery seçeneğini anlaşılır gösterir.

## 7. Uyku güvenliği

Uyku isteği shelter availability, nearby threat, warmth ve injury kontrolü yapar. İlerleme tek büyük delta değildir; bleeding death, weather transition, fuel exhaustion ve threat arrival event sınırlarında durur. Oyuncu neden uyandığını bilir. Açık pause menüsünde açlık ilerlemez. Co-op aynı uyku davranışını otomatik devralamaz.

## 8. Kabul

Bir 8 saatlik time skip ile küçük tick entegrasyonu kabul toleransında aynı stat sonucu vermeli. Frame rate stat tüketimini değiştirmemeli. Tedavi son anında hasar, aynı wound'a çift komut, death sırasında save, ilk shelter öncesi death, kirli suyu kaynatıp load, roof exposure ve wet coat swap test edilir. Ürün testleri klinik doğruluk değil tutarlı oyun davranışını hedefler.
