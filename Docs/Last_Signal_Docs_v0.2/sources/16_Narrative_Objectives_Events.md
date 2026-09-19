---
doc_id: LS-DOC-16
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Anlatı, görev state machine ve dünya olayları

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §5, §17. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Hikâye çerçevesi

Radyo ağını geri kazanma anlatısı keşif ve survival döngüsüne anlam sağlar. İsimler, final seçenekleri ve geçmiş olaylar GDD önerisidir; yazarın kesinleştirdiği canon olarak sunulmaz. Her notun iki görevi olabilir: insani iz ve oynanış ipucu. İkisi birden her notta şart değildir; fakat yüzlerce işlevsiz metin üretmek içerik kapsamını şişirir.

Başlangıçta zorunlu uzun cutscene yoktur. Gerekli hareket/tedavi bilgisi lore okuyana özel olamaz. Oyuncu radyo konuşmasını kaçırınca journal üzerinden yeniden görebilir.

## 2. Objective state

Locked → Available → Active → RequirementsMet → Completed. İsteğe bağlı Failed/Expired. Available olmadan item bulunması discovery flag ile kaydedilir ve görev açılınca requirement hemen evaluate edilir. Sürekli her frame tüm görevleri taramak yerine item, POI, repair ve radio eventleri ile ilgili koşullar yeniden değerlendirilir.

**REQ-OBJ-001:** Objective completion ve tek seferlik reward receipt aynı persistence transaction'ında yazılır. Load sonrası görev tamamlanmış görünürken ödülün iki kez alınması mümkün olamaz.

ObjectiveDefinition: ID, prerequisite IDs, requirement set, optional branch, journal localization keys, rewards, world phase effect, expiryClock. ObjectiveState: unlocked, accepted, condition progress, completedAt, rewardReceiptId. Tamamlanmış görev bir loot item kaybedildi diye kendiliğinden geri alınmaz; held-item requirement ile ever-acquired requirement ayrılır.

## 3. Relay mini-arc

| Beat | Koşul | İlerleme etkisi | Recovery |
|---|---|---|---|
| Cabin radio | Notu/cihazı keşfet | Fuse hedefi açılır | Not olmadan fuse bulunabilir |
| Gas station fuse | Garantili kilitli depo | Repair seçeneği mümkün | Kaybolursa recovery kaynak kaydı |
| Bring tools | Uygun tool tag | Repair valid | Başka uyumlu tool kabul |
| Repair relay | Craft/repair commit | P1 Contact + intel | İptalde malzeme korunur |
| Listen | Yayın/journal erişimi | Yeni bölge bilgisi | Tekrar dinleme |

Fuse tüketimi completed receipt oluşturur. Daha sonra oyuncu backpack recovery'de fuse'un kopyasını almamalı. Critical item fallback'i sınırsız çoğaltma musluğu olmaz: dünya receipt'i kontrol edilir.

## 4. World phase

P0 Isolation, P1 Contact, P2 Migration, P3 StormFront, P4 LastSignal, Aftermath GDD isimleridir. Phase progression player milestone ile olur; bilgisayar kapalıyken gerçek zamanla final kaçmaz. Phase değişimi loot'un yeniden roll olması değil event havuzu/tehdit/iletişim seçeneklerinin değişmesidir.

**REQ-OBJ-002:** Phase geçişi yüklenmiş ve yüklenmemiş bölgeler için aynı kuralları taşır. Bir cell eski sahne state'iyle açıldığında global phase'i geriye yazamaz.

## 5. Dynamic event sözleşmesi

EventDefinition; eligibility, cooldown world time, mutuallyExclusiveTags, duration, spawn budget, objective impact, telegraph, fallback. Event instance pending/telegraphed/active/resolved/expired state taşır. Tetiklenemeyen event oyuncunun yanında görünür spawn ile zorlanmaz; ertelenir veya başka uygun event seçilir.

Slice örnekleri: yakındaki kısa alarm ve hava uyarısı. Alpha örnekleri: supply signal, infected migration, power failure. Supply drop gökten aniden ücretsiz kaynak üretmemeli; kaynağın dünyadaki anlatımı, zaman penceresi ve riski belirli olmalıdır. Rastgelelik save reload ile seçilebilen loto haline gelmez.

## 6. Endgame ve kapsam

Evacuation, Broadcast, Stay üç GDD final adayıdır. Birini yüksek kaliteyle yapmak üç yarım finalden daha değerlidir; içerik maliyet hesabında her finalin mekan, ses, sinematik, test ve aftermath yükü ayrı yazılır. M2 sonrasında tek shipping final + alternatif epilog düşünülmesi PROPOSED scope seçeneğidir, otomatik kesinti değildir.

Final başlamadan oyuncu point-of-no-return varsa açıkça görür. Başarısız final tüm dünyayı kalıcı kilitliyorsa bunun difficulty ve save politikası açık olmalıdır. Varsayılan öneri son güvenli hazırlık state'ine recovery fırsatı.

## 7. Kabul

**REQ-OBJ-003:** Sıra dışı oynanış desteklenir: fuse önce bulunur, radio sonra okunur; repair sırasında save; reward alındıktan sonra crash; kritik item drop; not tekrar okunur; POI önceden temizlenir. Bu yollar completion count'u ve reward sayısını ikiye katlayamaz. Voice line eksikliği objective'i kilitlemez; caption/journal fallback bulunur.
