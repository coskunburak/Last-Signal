# S018 D176–D177 — düzeltme ve tekrar izlenebilirliği

**NOT_RUN — gerçek S018 oyuncu bulgusu yok; gameplay düzeltmesi yapılmadı.** Önceden mevcut S017 fixture düzeltmeleri S018 oyuncu gözlemi veya D176 başarısı olarak yeniden etiketlenmedi.

| Observation / evidence | Severity × frequency × exposure | Doğrulanan kök neden | Değişiklik / dosya | Beklenen oyuncu sonucu | En küçük teknik test / sonuç | Candidate / insan retest / sonuç |
|---|---|---|---|---|---|---|

Tablo bilerek boş. Genellikle en yüksek etkili üç sorun seçilir; önce S0/S1. Bug/clarity/prompt/focus/readability/gerçek kritik balance blocker kapsamı tercih edilir. Yeni mekanik, harita, silah, düşman veya framework oluşturulmaz. Bir teknik hata kullanıcı test sonucuyla ortaya çıkarsa ayrı teknik evidence ID ile bağlanır; insan gözlemi diye gösterilmez.

Her fix için public contract/save invariant/input lifecycle etkisi kaydedilir; kaynak ve fixture korunur. Anlamlı davranış ve olumsuz yollar sınanır; assertion PASS için gevşetilmez. Verification kullanıcı tarafından çalıştırılır: önce riskle ilişkili dar test, sonra gerekirse subsystem/full regression, sonra build ve gerçek platform. Her sınırda handoff güncellenir ve `NOT_RUN — awaiting user-executed verification` denir.

PT2 ancak teknik doğrulamadan sonra insan retestine açılır. Eski ve yeni aday, değişiklik nedeni, müdahale/blocker/hedef süresi/karışıklık/completion/anlama karşılaştırması ve fresh/repeat kişi ayrımı kaydedilir. Aynı kişide `REPEAT_TESTER — LEARNING EFFECT POSSIBLE`. Teknik olarak doğru ama insan için etkisiz fix iyileşme diye raporlanmaz. Bulgular düzeltme gerektirmiyorsa ancak gerçek veriye dayanarak gerekçeli N/A verilebilir; şimdiden N/A atanmaz.


## Round 1 teknik QA bulguları — insan bulgusu değildir

Kaynak: [kullanıcı koşusu](../S017/Evidence/20261008T092103-722355Z-regression-play/tests.xml), 302/306; [devir](S018_VERIFICATION_HANDOFF.md). D176 insan gözlemi tablosu boş kalır.

| Teknik evidence | Kök neden / belirsizlik | Değişiklik | Doğrulama |
|---|---|---|---|
| R01 RealInputSwitchCancelSoakAndSaveLoad TearDown exception | InputTestFixture manager swap + eski UI asset control monitor; fixture lifetime uyumsuzluğu | R01 ve ortak Combat fixture native manager/clock; lifecycle finally; log-ignore ve input bypass kaldırıldı | Round 2 üç sınıf 11 test NOT_RUN |
| Scope ADS allocation, held resume, unequip allocation failures | Aynı eski fixture; input event/clock uyumsuzluğu aday neden, runtime optic hatası henüz kanıtlanmadı | Native fixture; AimHeld assertion ile input/presentation ayrımı; mevcut optic/ammo assertion'ları korunur | Round 2 NOT_RUN; başarısız assertion bir sonraki kök neden incelemesini yönlendirir |
| ProjectSettings kaynak mutasyonu | Inference AnalyticsDefineManager başlangıçta SENTIS_ANALYTICS_ENABLED kaldırdı | Analytics tercihi/package/define değiştirilmedi; kaynak-koruma istisnası eklenmedi; batch sonrası dosya korundu | Sonraki tur changed=[] zorunlu |


## Round 2 sonucu — 2026-10-08

Kullanıcı [20261008T094111-397740Z-focused-combat-optics](../S017/Evidence/20261008T094111-397740Z-focused-combat-optics/tests.xml) koşusu: **11/11 PASS**, failed/skipped=0, process exit=0, kaynak koruma `changed=[]`; kaynak kimliği bu koşunun snapshot’ında kayıtlı. Yukarıdaki Round 2 NOT_RUN kayıtları artık tarihsel hazırlık durumudur. R01 cleanup ve Scope input/optic/ammo/lifecycle assertion'ları geçti; önceki dört hata tekrarlanmadı. Bu sonuç fixture kaynaklı açıklamayı destekler, eski her hatanın tek mekanizmasını tek başına ispatlamaz. Analytics define mutasyonu bu turda yok; paket davranışı kalıcı olarak kaldırılmadı. Kod yeniden değiştirilmedi. Round 3: birleşik PlayMode sıralama/regresyon kontrolü NOT_RUN; insan retest hâlâ NOT_RUN.


## Round 3 birleşik kapanış

Kullanıcı [20261008T095711-389229Z-regression-play](../S017/Evidence/20261008T095711-389229Z-regression-play/tests.xml): **306/306 PASS**, failed/skipped=0, exit=0, changed=[]; koşu kaynak kimliği korundu. R01/Scope düzeltmeleri tüm PlayMode assembly'siyle birlikte geçti. Yeni gameplay düzeltmesi yok. Sonraki dar Art Editor kapısı eski sanat kanıtından sonra değişen üretim sahnesi için; aynı runtime testleri tekrar istenmiyor. G5 insan/platform/capacity açıkları sürer.

## Test öncesi son hazırlık — 2026-10-09

Offline kayıt aracı ve sentetik unit test kaynağı eklendi; çalışma sonucu NOT_RUN. Hazırlık incelemesinde canonical başarı için equip/consume/treatment ve process yeniden başlatma gözlemleri zorunlu tutuldu; S4 öneri sınıfı sentez sözlüğüyle eşlendi. Bu değişiklikler gameplay veya insan bulgusu değildir. Aracın hata yolları: boş form, withdrawn rıza, owner QA, değişmiş kanıt hash'i, eksik canonical rota, gizlenmiş yardım, farklı adayların karıştırılması, tekrarlı JSON anahtarı ve sonlu olmayan sayı. Test yürütmesi son doğrulama aşamasına ertelendi; PASS iddiası yok.

## 2026-10-09 — owner onaylı canonical önkoşul uygulaması

Owner “Eksik oyun sistemlerini de uygula; sonra test edelim” seçimini açıkça yaptı. Bu çalışma D176 dış oyuncu bulgusu gibi etiketlenmez: giriş incelemesindeki somut consume/equip/treatment açığını kapatan önkoşul geliştirmesidir. **IMPLEMENTED / NOT_VERIFIED**; bu tur hiçbir test, derleme veya build çalıştırılmadı.

| Açık / kaynak sözleşme | Uygulanan davranış | Korunan invariant / son test |
|---|---|---|
| LS-DOC-07 consume ve REQ-SURV-001 | ItemDefinition.Use ile su +40 hydration, konserve +35 nutrition; sınır 100, ihtiyaç yoksa tüketim yok. Tam seçili slot ve revision kontrol edilir | InventoryContainer.Exchange içinde eşya/stat beraber commit; observer sırasında Save BUSY; stale istek değişiklik yapmaz |
| LS-DOC-09 yaralanma / REQ-SURV-002 | Fiziksel melee/bullet hasarı aggregate kanayan yarayı en çok 3 seviyeye çıkarır; yara revision'ı güncellenir. Bandaj 3 simulation seconds; commit kanamayı durdurur, doğrudan HP vermez | Hareket/sprint/slide, fiziksel hasar, menü/focus, slot değişimi, eşya/yara revision değişimi iptal; iptal öncesi bandaj harcanmaz; tekrar komut ikinci commit yaratmaz |
| LS-DOC-07 equip / REQ-INV-002 | Yeni oyunda bir saha çantası envanterde; takıldığında ayrı equipment owner'a geçer ve 24→32 yuva sağlar. Çıkarma çantayı inventory'ye geri taşır | Dolu küçültmede işlem reddedilir; shrink başarılıysa occupied slotlar kayıpsız sıkıştırılır. Takılı eşya inventory'de hayalet kopya taşımaz |
| Dünya zamanı / kaynak tükenmesi | Hydration 100/43200, nutrition 100/86400 puan/world second azalır. Sıfırda sırasıyla 6/2 HP/world hour; her bleeding seviyesi 12 HP/world hour. Regen çarpanları hydration≤10 için .5, nutrition=0 için .5, bleeding için .75 | Clock participant, pause sırasında ilerlemez; world-time sleep ve normal tick aynı integral. Health/death mevcut PlayerHealth yolunda; wall-clock tüketimi yok |
| Save / ekipman | header.survivalVersion=1; hydration/nutrition/bleeding/woundRevision/baseCapacity/backpackId birlikte kaydedilir | Eski kayıt için marker=0 ve boş extension → full kaynaklar, kanama/çanta yok; inventory kapasitesi korunur. Yeni load ekstra starter pack vermez. Uygulanan tedavi sırasında save BUSY |
| UI / anlaşılabilirlik | Envanter Kullan/Tak ve Çantayı çıkar butonları; 32 yuvayı gösteren kompakt grid; HUD su/yemek/kanama/tedavi süresi ve survival ölüm nedeni | Mevcut SessionUi directional focus ve input altyapısı kullanılır; gerçek resolution/UI scale/gamepad kabulü NOT_RUN |

Ana kaynaklar: `Runtime/Player/SurvivalState.cs`, `PlayerSurvival.cs`; `Runtime/Inventory/InventoryContainer.cs`, `Data/ItemDefinition.cs`, `UI/InventoryUI.cs`; `Runtime/Persistence/{SaveGame,SaveCodec,SaveValidation,SaveSession}.cs`. SessionFlow ve WorldClock composition/restore bağlantısını kurar. Weapon/Melee controller tedavi sırasında ateş/vuruşu reddeder; WorldItem ölü oyuncuya pickup yapmaz. FirstUseGuide bandaj ihtiyacını anlatır.

Çanta işlevsel yeni yerel definition/prefab'dır; model zaten projede bulunan Town/storage_bag mesh'ini, material mevcut S013 URP Canvas materyalini kullanır (vendor Standard materyali kullanılmaz). Yeni indirme veya satın alma yok. Yeni kanıt sayılmaz; modelin oyun içinde görsel uygunluğu son kabulde incelenir. Su/konserve/bandaj mevcut definition'larına Use alanı eklendi; catalog çantayı içerir. S013Cabin SessionFlow.enableSurvival açıkça işaretlendi; service bu kompozisyonda katalogdaki çanta tanımıyla kurulur. Diğer legacy sahneler otomatik survival veya başlangıç ekipmanı kazanmaz.

Tasarım sınırı: bu teslim hedeflenen slice su/yemek, aggregate bleeding/bandaj ve çanta equip yoludur. Ayrı vücut bölgesi/fracture, enfeksiyon, kıyafet katmanları, ağırlık/nested bag sistemi uygulanmış sayılmaz. Mevcut firearm/melee state ve mühimmat kaydı korunur; generic silah ticareti sistemi eklenmedi. Önceden authored world scale=60 değiştirilmedi: dolu su yaklaşık 12 gerçek dakika sürer. LS-DOC-09'daki 12× dünya hızı bir tasarım önerisidir; mevcut 60× adayla fark açıkça kaydedildi, sessiz global zaman değişikliği yapılmadı. +40/+35 ve kanama oranları başlangıç tuning'idir; eğlence/balance kabulü yok.

Hazırlanan doğrulama: 13 EditMode state/save testi, 8 PlayMode production scene testi; seçili yığın tüketimi, full/stale reddi, atomic observer/save barrier, bandaj exact commit/iptal/çift istek, pause, çanta ownership/full-shrink ve tekrar load kapsanır. `focused-survival-edit` ve `focused-survival-play` kapıları mevcut s017-verify aracına eklendi. **Hepsi NOT_RUN.** Eski 541/306/10 PASS sayıları yeni kaynakların sonucu değildir. Yeni QA2 build ve gerçek Windows/player kabulü gereklidir.

## QA2 Round 2 teknik sonuç

2026-10-09 yerel tarih: yeni survival EditMode testleri 13/13 PASS; Unity exit=0. Kaynak-koruma FAIL: SENTIS_ANALYTICS_ENABLED Standalone define'ından kaldırıldı; wrapper exit=1. [Koşu](../S017/Evidence/20261008T213549-958708Z-focused-survival-edit/). Yeni gameplay/test değişikliği yapılmadı; mevcut kaynakla yalnız aynı focused-survival-edit kapısı tekrar bekliyor. PlayMode/QA2 build/insan kabulü NOT_RUN.

## QA2 Round 3 — domain/save kapısı PASS

Kullanıcı [20261008T213726-914789Z-focused-survival-edit](../S017/Evidence/20261008T213726-914789Z-focused-survival-edit/) koşusu: 13/13 PASS, failed/skipped=0, Unity/wrapper exit=0, changed=[]. Güncel manifest karşılaştırması fark göstermedi. Round 2 kaynak mutasyonu tekrarlanmadı; aynı testleri yeniden istemiyoruz. Sıradaki focused-survival-play 8 vaka NOT_RUN.

## QA2 Round 4 — survival entegrasyonu PASS

Kullanıcı [focused-survival-play](../S017/Evidence/20261008T213829-886075Z-focused-survival-play/) koşusu: 8/8 PASS, failed/skipped=0, Unity/wrapper exit=0, changed=[]. XML sınıfı ve güncel kaynak eşleşmesi incelendi. Dar EditMode 13 + PlayMode 8 testleri geçti; tam regresyon, yeni build ve gerçek cihaz/oyuncu kabulü hâlâ açık. Sonraki kapı regression-edit; yeni fix yapılmadı.

## QA2 Round 5 — tam EditMode PASS

Kullanıcı [regression-edit](../S017/Evidence/20261008T214005-960072Z-regression-edit/) koşusu 554/554 PASS; 13 yeni survival vakası tam suite içinde geçti. failed/skipped=0, Unity/wrapper exit=0, changed=[]; güncel manifest eşleşti. Yeni değişiklik yok; sonraki kapı tam PlayMode.

## QA2 Round 6 — tam PlayMode PASS

Kullanıcı [regression-play](../S017/Evidence/20261008T214103-002221Z-regression-play/) koşusu 314/314 PASS; yeni 8 survival entegrasyon vakası tam suite içinde geçti. failed/skipped=0, Unity/wrapper exit=0, changed=[]; güncel manifest eşleşti. Yeni değişiklik yok; sonraki kapı focused-art, ardından yeni Windows build. Bu sonuç Editor testidir; gerçek Windows process restart veya insan kabulünün yerine geçmez.
