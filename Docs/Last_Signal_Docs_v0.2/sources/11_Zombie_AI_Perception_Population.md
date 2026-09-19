---
doc_id: LS-DOC-11
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Zombie AI, algı, navigation ve nüfus sürekliliği

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §13-14. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Algı ve hafıza

Vision distance+angle ardından obstacle query çalışır. Her agent her frame pahalı raycast yapmaz; scheduler zamanı yayar. Işık faktörü, gizlenme ve crouch farkı basitleştirilmiş authored visibility modelinden gelir; pixel-perfect shader ışığını CPU'da tekrar üretmeye çalışılmaz. Yakın temas görüşten bağımsız farkındalık yaratabilir.

Hearing event position, intensity/category, world/simulation timestamp ve TTL taşır. AudioSource sesinin çalınması AI algısı değildir. Oyuncu ses ayarını kıssa da oyun içi gürültü değişmez. Hafızada lastKnownPosition, lastSeenTick, confidence ve stimulusId tutulur.

**REQ-AI-001:** Görüş kaybolduğunda zombi oyuncunun anlık koordinatını sınırsız izleyemez. Chase'ten search'e geçiş memory confidence ve zamanla belirlenir.

## 2. State geçişleri

| State | Giriş | Çıkış | Timeout fallback |
|---|---|---|---|
| Idle/Wander | Hedef yok | Ses/görüş | Authored roam anchor |
| Investigate | Uygun stimulus | Sight confirmed / hedefe varış | Search sonra wander |
| Chase | Yeterli confidence | Attack range / lost contact | LastKnownPosition |
| Attack | Range+angle+LOS | Active window/recovery | Chase veya search |
| Search | Hedef kaybı | Reacquire / budget bitti | Return/Wander |
| Stagger | Impact threshold | Recovery | Önceki hedef validity check |
| Dead | Health zero | Çıkış yok | Corpse representation |

State machine kısa ve veri odaklıdır; behavior tree zorunlu değildir. Attack animation bitti diye state doğrulanmadan tekrar vurulmaz. Attack lock sırasında hedef menzil dışına çıkarsa strike kaçırabilir; oyuncu görünmez teleport hit almaz.

## 3. Navigation

Path requestleri priority queue'da işlenir; hedef her birkaç santimetre hareketinde yeni path istenmez. Repath minimum interval ve target displacement threshold bulunur. NavMeshAgent ilerlemiyorsa stuck timer üç basamaklı recovery uygular: repath, yakın geçerli nokta/alternatif portal, vazgeçip search. Görünür zombiyi oyuncunun arkasına teleport etmek fallback değildir.

Doors portal state sunar: passable, closed-breakable, locked-breakable, impossible. AI kırabileceği kapıya vurur; bunun cost'u route selection'a yansır. Scene collision kapalı, nav açık gibi uyumsuz durum validation testidir. Merdiven ve eğimlerin navigation ve attack range ölçümü 3D koordinatlarda tutarlı yapılır.

## 4. Archetype profilleri

Shambler temel yavaş tehdit; Lurker iç mekânda bekler ve yakın burst üretir; Runner nadir hızlı düşük health; Howler çevreyi çağırır; Armored özel hit zone gerektirir. İlk üç slice, diğerleri alpha hedefi. Her archetype'ın görünüş/ses telegraph'ı farklı olmalıdır. Renk farkı tek tanıma kanalı değildir.

Runner'ın hızını oyuncuyu sürekli yakalayacak şekilde ayarlamak kaçışı anlamsız kılar. Stamina/chase duration, obstacle traversal ve recovery penceresiyle karşı oyun tasarlanır. Howler çağrısı yeni sınırsız düşman yaratmaz, nüfus bütçesiyle çalışır.

## 5. Simulation LOD

Yakın combat agent karar/perception 10-20 Hz hedef, movement/physics uygun tick, rendering frame rate. GDD'deki full AI ifadesi bütün pahalı querylerin 60 Hz çalışacağı anlamına gelmez. 30-80 m bölgesinde yaklaşık 10 Hz, 80-200 m coarse 2 Hz, uzak hücrede token simülasyonu başlangıç hipotezidir. Hysteresis ile 30 m çizgisinde sürekli mod değişimi engellenir.

**REQ-AI-002:** LOD düşürme health, loot identity veya ölüm durumunu sıfırlayamaz. Görüşteki agent'ın hitbox'ı performans gerekçesiyle gizlice kaybolamaz. Combat/targeted agent mesafeden bağımsız geçici yüksek öncelik alır.

## 6. Nüfus muhasebesi

PopulationGroup: region/cell, species/archetype dağılımı, count, route, eta, disturbance affinity. Materialize edilen agent sayısı token'dan düşülür. Dematerialize yaşayan agent state'i token/kayıt listesine döner; öldürülen geri dönmez. Named/persistent özel agentlar count-only modele kaybedilmez.

**REQ-AI-003:** Geçiş öncesi logical+physical nüfus, geçiş sonrası logical+physical nüfusa eşittir; doğum/spawn/migration/death ayrıca ledger olayıdır. Bir grup iki komşu hücrede aynı anda materialize edilemez.

Oyuncunun görünür alanı, yakın güvenlik yarıçapı, spawn point doğruluğu ve travel time kontrol edilir. Budget doluysa spawn ertelenir; mevcut düşmanları öldürmeden silmek veya nereden geldiği belirsiz saldırı yapmak kullanılmaz. Corpse pooling active entity ID'sini yeni zombiye taşıyamaz.

## 7. Save ve kabul

Ölü agent tombstone, yaşayan agent state/memory policy, group count ve migration ETA kaydedilir. Kısa attack animation zamanının aynen restore edilmesi gerekmeyebilir; load'da güvenli non-damaging recovery state kullanılır ve politikası açıktır.

Testler: görüş kaybı, kapı kapanması, unreachable balcony, sınırda LOD thrash, reload sonrası ölü zombie, group migration cancel, 30+ yakın agent CPU, loud sound TTL expired, session stop unsubscribes. QA, kör duvarın arkasındaki oyuncuya path hedefinin hâlâ sürekli yazılmadığını debug overlay ile doğrular.
