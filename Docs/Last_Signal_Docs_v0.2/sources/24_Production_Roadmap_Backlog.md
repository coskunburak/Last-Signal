---
doc_id: LS-DOC-24
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Üretim planı, bağımlılıklar ve uygulanabilir iş paketleri

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §25-27. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Planlama modeli

GDD'nin S000-S115 grupları tarihi makro öneridir; bunların tamamlandığına dair kanıt yok. Bu paket W00-W23 iş paketlerini önerir. Her paket gün/sprint sayısıyla sabitlenmez; oyuncuya görünen çıktı ve doğrulama kapısıyla kapanır. İlk hedef eksiksiz küçük loop, sonra final-quality slice, sonra içerik genişlemesidir.

**REQ-PROD-001:** Yeni iş paketi en az bir görünür sonuç veya kritik teknik riski azaltır. Aylar boyunca yalnız interface/event bus altyapısı üretmek kabul edilmez.

## 2. Fazlar

| Milestone | Amaç | Kapanış |
|---|---|---|
| M0 | Repo/engine/ölçüm temeli | W00-W01, gerçekten açılan build |
| M1 | Çıplak playable loop | W02-W09, 10-15 dk keşif-loot-combat-save |
| M2 | Signature+streaming | W10-W16, pressure ve shelter decision kanıtı |
| M3 | Polished vertical slice | W17-W23, 30-45 dk oyuncu testi |
| M4 | Production expansion | Kanıtlı pipeline ile ikinci bölge |
| M5 | Content complete/beta | Feature lock, migration, platform matrisi |
| M6 | Release candidate | Critical zero, temiz build ve yayın varlıkları |

## 3. İlk 24 iş paketi

| ID | Bağımlılık | Teslimat | Kabul / kanıt |
|---|---|---|---|
| W00 | Yok | Mevcut repo/Unity/paket/donanım keşfi | Gerçek sürüm, path, baseline hata raporu |
| W01 | W00 | Bootstrap, session, test scene | NewGame/Menu 10 döngü, leaksiz lifecycle |
| W02 | W01 | FP movement/input parkuru | Ground/slope/step/focus build kabul |
| W03 | W02 | Interaction+door+world pickup | Occlusion, busy, çift input red |
| W04 | W03 | 12 item, inventory, basit UI | Atomic transfer/weight/slots |
| W05 | W04 | ID+ilk save/load | Player/item/door roundtrip |
| W06 | W03 | Tek Shambler vision/hearing/nav | LOS loss, kapı, stuck fallback |
| W07 | W04,W06 | Crowbar, health, death temel | Hit dedupe, stamina, tek death |
| W08 | W05,W07 | Su/bleeding/treatment/recovery | Item-wound commit, no-shelter recovery |
| W09 | W05,W08 | 10-15 dk micro-loop | İlk dış oyuncu pilotu ve sorun listesi |
| W10 | W07,W09 | Pistol + noise | Ammo conservation, wall obstruction |
| W11 | W05,W06 | İki-cell streaming spike | Unload/load state, memory ölçümü |
| W12 | W10,W11 | Pressure D/P ve population | Tek shot grubu çekiyor, duplicate yok |
| W13 | W08,W11 | GameClock+yağmur+uyku | Time-skip boundary ve roof exposure |
| W14 | W04,W05,W13 | Shelter modules+craft escrow | İptal/output full/power tükenme |
| W15 | W14 | Relay objective ve out-of-order | Erken fuse, tek reward, phase save |
| W16 | W12,W15 | Integrated core review | Fun riskleri ve kapsam kararı |
| W17 | W16 | Art/audio benchmark POI | Gün/gece/yağmur + performans |
| W18 | W17 | 5-10 bina, 72 item adayı triage | Validation, lisans, loot theming |
| W19 | W16 | Lurker+Runner ve farklı combat | Telegraph ve counterplay testi |
| W20 | W18,W19 | Tam UI/gamepad/accessibility | Mouse'suz full loop, localization |
| W21 | W20 | Save hardening+platform build | Fault injection, Windows gerçek test |
| W22 | W21 | 5-8 oyuncu slice testi | Yardımsız completion, anlaşılırlık |
| W23 | W22 | Polish+go/no-go | Critical zero, evidence ve backlog |

W11 sonrası Addressables seçimi gerçek spike sonuçlarıyla kesinleşir. W05 basit tek cell kayıt yapabilir; W21'e kadar save yok diye beklenmez. W17 sanat beklenirken W19 sistem işi yapılabilir; bu bağımlılık tablosu otomatik ajan delegasyonu talimatı değildir.

## 4. W00 ayrıntılı brief

Amaç: mevcut projenin gerçeğini öğrenmek. Girdi: repo yolu, ProjectVersion.txt, Packages/manifest.json ve lock, AGENTS kuralları, mevcut scene/tests. Çıktı: environment report, açık compile error/warning baseline, kullanılan pipeline, test runner capability, target build path. Bu aşamada motor yükseltme, hazır sistemi yeniden yazma veya co-op paket kurma yapılmaz.

Kabul: Unity projesi açılıyor mu, boş/var olan sahne oynuyor mu, en küçük target build alınabiliyor mu belgelenir. Editor erişimi yoksa rapor BLOCKED, gerekli kullanıcı adımı tek ve somut belirtilir. Dosyaların var olması “build geçti” demek değildir.

## 5. İş paketi kartı standardı

Problem/oyuncu etkisi, kapsam, dış kapsam, dependencies, requirement IDs, veri değişiklikleri, rollback, test planı, visible acceptance, performance riski, definition of done. Bir iş paketi çok fazla bağımsız risk taşıyorsa bölünür. Çıktı ekran görüntüsü, build veya fixture sonuçlarından en az uygun olanı içerir.

**REQ-PROD-002:** Paket bitince “yüzde tamam” yerine tamamlanan davranış, kalan limitation ve kanıt listesi yazılır. Dosya sayısı veya yazılan satır gelişme metriği değildir.

## 6. Süre ve kapasite

GDD faz süreleri yaklaşık 21-32 ay toplam tahmin verir; tek geliştirici için garanti tarih değildir. Haftalık kullanılabilir saat, öğrenme süresi, asset pipeline, test donanımı ve dış destek belirlenmeden calendar taahhüdü yapılamaz. İlk dört W paketinde tahmin/gerçek saat kaydedilir, sonra rolling forecast yapılır.

Plan: kullanılabilir hafta saati × odak oranı = net üretim kapasitesi. Örnek 30 saat ×0.65=19.5 net saat, yalnız yöntem örneği. İşler most-likely/risk-high aralığıyla değerlendirilir, kritik zincire 20-30% keşif/hata payı eklenmesi başlangıç yaklaşımıdır. AI yardımı derleme, art yönü, içerik seçimi ve test maliyetini sıfırlamaz.

## 7. Scope kesimi ve üretim ekonomisi

Önce kozmetik varyantlar, ek silahlar ve yan eventler; sonra region sayısı; ardından skill derinliği. Save güvenilirliği, okunur combat, temel loop kesilmez. World Pressure veya Shelter Network eğlence testinde başarısızsa signature diye kör korunmaz; önce sade deney, sonra Burak'ın tasarım kararıyla değişim yapılır. Bu GDD'nin “signature kesilmez” ifadesini kanıtla yönetilebilir hâle getiren öneridir.

**REQ-PROD-003:** Yeni özellik için kod+art+audio+UI+save+QA maliyeti birlikte hesaplanır. Sadece C# script süresiyle araç veya co-op kapsamı kabul edilmez.

## 8. Risk kayıtları

En yüksek riskler: save/world streaming birleşimi; AI yoğunluğu; uyumsuz asset stili; içerik üretim hızı; mekaniklerin eğlenceli olmaması; Windows doğrulamasının gecikmesi. Her risk owner, trigger, mitigation ve next review date taşır. Burak mevcut tek karar sahibidir; olmayan ekip üyeleri atanmış gibi yazılmaz. İlk review M1, ikinci M2, üçüncü M3 kapanışında yapılır.
