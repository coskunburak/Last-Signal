# Kapsam matrisi ve solo üretim bütçesi

BLD-D004, BLD-D007 · BLD-P00-DEC-004/007 · PROPOSED / PENDING_OWNER (takvim) · Belge IMPLEMENTED / TEST_NOT_RUN.
Girdi: [ADR](BLD_S001_PRODUCT_SCOPE_CHANGE.md), D004/D007 ve LS-PLAN-M02 §40–50. Kart bağlantıları [S001 handoff](BLD_S001_HANDOFF.md) içindedir. Sahip Burak; sayılar ölçüm veya teslim sözü değildir.

## D004 — Yapı ve kabiliyet kapsamı

| ID | Öğe | 1.0 önerisi | BLD-S010 ilk playable minimumu | BLD-S019 uzun vadeli hedef / sınır |
|---|---|---|---|---|
| SC-01 | Sonlu node hasadı, balta, odun pickup/taşıma | MUST | Tek ağaç sınıfı + mevcut item authority | Ekoloji genişlemesi BLD-S020; sonsuz kaynak yok |
| SC-02 | Çok girdili tarif, escrow, cancel/output full | MUST | Odun işleme + çit maliyeti; mevcut S010 geriye uyum | Geniş katalog gerekmez |
| SC-03 | Craft Defteri ve erişilebilir preview | MUST | Tarif/maliyet/seçim/döndür/ret/iptal; renk+metin | Gelişmiş filtreler SHOULD; temel erişilebilirlik ertelenmez |
| SC-04 | Çit segmenti | MUST | Tek tip, bounded zone içinde; kaçışı kapatamaz | Mütevazı perimeter, tam dokunulmazlık yok |
| SC-05 | Yerleştirilebilir kapı/gate | MUST, modüler barınak hedefinde | Yeni gate gerekmez; mevcut giriş açık kalır | BLD-S013 kapı/sweep/snap; sınırlı set |
| SC-06 | Temel/zemin, duvar, çatı | MUST, sonraki modüler hedef | Yeni ev inşası gerekmez; mevcut barınak kullanılır | BLD-S011–015 küçük barınak, BLD-S019 progression; yüksek kat/serbest fizik yok |
| SC-07 | Depolama ve çalışma tezgâhı | MUST survival değeri | Mevcut S010 Storage/Workbench kullanılır | BLD-S016 kontrollü yeni utility eşlemesi; kopya inventory sahibi yok |
| SC-08 | Güvenli commit, stable ID, save/reload ve tombstone | MUST | İlk yapıdan itibaren zorunlu | BLD-S021 ölçek/migration genişletir, ilk güvenliği ertelemez |
| SC-09 | Controlled zone, nav/door güvenliği, kapasite limiti | MUST | Tek authored zone, tek-cell yapılar | İki-cell ancak lease ve fixture kabulünden sonra |
| SC-10 | Onarım/söküm/yer değiştirme | SHOULD | Yok; preview cancel var | BLD-S017; ücretsiz tam refund exploit'i yok |
| SC-11 | Basit su/güç utility bağlantısı | SHOULD | Mevcut survival alternatifleri | BLD-S018; bedelsiz/sınırsız üretim yok |
| SC-12 | Kozmetik varyant, dekor, gelişmiş elektrik, tarım, otomatik savunma, büyük garaj | COULD | Yok | Ayrı değişiklik kararı olmadan MUST'a dönüşmez |
| SC-13 | Voxel, her zemini kazma, tam yapısal çökme, co-op, dünya çapında sınırsız inşa/yıkım | OUT | Yok | 1.0 dışı; veri sınırı geleceğe izin verir diye uygulama eklenmez |

Başlangıç ölçüm hipotezi: bir build zone, en çok 32 aktif yapı/world ve 16 yapı/cell, tek katlı küçük modüler barınak. Bunlar asset boyutu veya perf kanıtı değildir. BLD-S007 policy authoring ve BLD-S010/G4 ölçümüyle owner revize eder; limit değişimi policyRevision'a girer. Restore mevcut kaydı yeni düşük limit yüzünden silmez; yalnız yeni commit'i engeller. Katalog sayısı ile sahne instance limiti farklıdır.

SC-01–09 aynı ürün döngüsünün MUST'larıdır. BLD-S010'da küçük ev yapılmaması SC-06'nın sessizce OUT olması değildir; büyük genişleme G4/G5'te açık go/no-go alır. Değişirse ADR ve downstream kart durumu yeniden yazılır.

## D004 — Kesme sırası ve etkiler

1. SC-12 kozmetik çeşit/ileri utility çıkar: görsel çeşit azalır, çekirdek döngü kalır.
2. SC-11 yeni su/güç bağlantılarını ertele: mevcut su/loot/generator rotası sürer, BLD-S018/S019 değeri yeniden daraltılır.
3. SC-10 relocation ve kapsamlı sökümü ertele: preview iptal kalır; kötü yer seçimine karşı onay bilgisi güçlenir.
4. SC-05/06 katalog çeşitliliğini azalt: tek kapı/duvar/çatı ailesi; modüler barınak MUST'ı hâlâ karşılanır.
5. G4/G5 değeri kanıtlanamazsa SC-06 genişlemesini DEFER etme kararı owner'a gider; bu release vaadi değişikliğidir, sessiz kesme değildir.

Hiç kesilmeyecekler: tek sahiplik, kaynak koruma, save recovery, erişilebilir ret, ana survival rotası, legacy S010/FirstTownCore koruması. Alternatif “güvenliği erteleyip katalog büyüt” reddedildi. Revert: yalnız mevcut socket/loot rotasına dönülür, yeni save verisi silinmez.

## D007 — Efor aralıkları ve belirsizlik

Bir kişi-gün = yaklaşık 6 odaklı üretim saati varsayımı. Aşağıdaki P01–P04 ilave eforu aynı kişinin ardışık çalışması içindir; mevcut S010/S023 sürelerinden türetilmedi. Sekiz kart = sekiz gerçek gün kabul edilmedi. Maliyet sütunları birbirini içermez; QA owner'ın fiilî test zamanı + düzeltme koordinasyonudur, engine işindeki birim geliştirme ayrı tutulur.

| Maliyet sürücüsü | İlk döngü kişi-gün aralığı | Güven | Belirsizlik / erken sinyal |
|---|---:|---|---|
| Domain ve gameplay üretimi (hasat/craft/placement) | 20–35 | Düşük | Yeni rezervasyon ve lifecycle sınırları; ilk gerçek entegrasyonda yeniden tahmin |
| UI/UX ve erişilebilir feedback | 5–9 | Düşük | Ret metinleri, input focus, preview teardown |
| Persistence/ID/migration ve durability | 8–15 | Düşük | Disk recovery, node population, cell deltas ve receipt retention |
| Sanat/animasyon/ses uyarlaması | 6–12 | Düşük | Mevcut ağaç/çit/pivot/URP uyumu, FPS görünüm |
| Asset kaynak/lisans/import hazırlığı | 1–3 | Orta-düşük | Mevcut envanter uymazsa seçim gate'i; satın alma ayrı |
| Owner QA, fault/reload/regression | 10–18 | Düşük | Gerçek runner/fixture hazırlığı ve cihaz tekrarları |
| Toplam, risk payı hariç | 50–92 | Düşük | Ardışık solo efor; takvim taahhüdü değil |
| Risk payı (%25–35 başlangıç varsayımı) | 13–33 | Düşük | Yaklaşık toplam 63–125 kişi-gün; yeniden planlama gerekli |

P05–P06 modüler barınak/utility genişlemesi için ayrıca 30–60 kişi-gün domain+UI, 12–25 sanat/asset, 10–20 persistence, 15–30 QA keşif aralığı: toplam 67–135, risk payı hariç, çok düşük güven. P07–P09 release maliyeti bu aralıklara dahil değildir; G4/G5 ölçülmeden güvenilir toplam yayın tarihi verilemez. P00 yeni asset alımı 0; parasal fiyat tahmini yapılmadı.

| Risk | Tetik / etki | Azaltım ve stop/go | Sorumlu / kesme |
|---|---|---|---|
| Forestry population persistence | Node reload reset veya save boyutunun node sayısıyla patlaması | Stable authored node ID + sparse delta; dupe/loss varsa P01/G1 durur | Persistence/Gathering; kozmetik ağaç çeşitlerini kes |
| Many structure instances | Kabul edilen instance sınırında frame/save bütçesi aşılır | 16/cell, 32/world hipotezi; owner ölçer; sınırı azaltmadan mevcut kayıt silinmez | Construction/QA; katalog/alan büyümesi durur |
| UI accessibility | Ret yalnız renk veya input focus kaybı | Metin/ikon ve cancel; G3/G4 blocker | Presentation; ileri filtreleri kes |
| Art cost | Pivot/LOD/texture uyarlaması aralığı aşar | Tek kit ve varyant; gri kutu final sayılmaz | Art/owner; SC-12 ilk kesim |
| Schema upgrade | Legacy escrow/revision uyuşmazlığı | Eski fixture ve transactional migration; kayıp varsa G3 durur | Persistence; yeni recipe kataloğu açılmaz |
| VisualSlice çatışması | Paralel scene/terrain/asset hash değişir | Staged/promotion guard; source drift yeniden audit | Owner; ana sahne entegrasyonu ertelenir |

Bağımlılık: G0 owner → BLD-S003/004 resource fixture → BLD-S005/006 craft/notebook → BLD-S007/008 validator/preview → BLD-S009 durability → BLD-S010 G4. Saf kontrat fixture'ları UI'yı beklemez; gerçek entegrasyon karşılıklı acceptance gerektirir. G4 GO → BLD-S011–015/G5 → BLD-S016–019/G6. BLD-S020–022/G7 ilk güvenlik açığını kapatmak için mazeret değildir.

OD-02 önerisi takvim uzamasını kabul etmek, COULD'ları ertelemek ve her 2–3 sprintte gerçekleşen eforla aralığı yenilemektir. İlk entegrasyon üst aralığın %25 üzerine çıkarsa yeni MUST eklemek durur ve owner scope/takvim kararını yeniler. Bu eşik planlama hipotezidir. INPUT→D007; DECISION→ayrı maliyet/güven ve kesme; DELIVERABLE→bu tablolar; ACCEPTANCE→owner kapsam takası veya takvim uzamasını imzalar, hiçbir tahmini gerçekleşmiş saymaz.
