# S018 D171 / D173–D174 — dış oyuncu test kiti

Durum: **protokol hazır; human playtest NOT_RUN**. Teslim edilmiş gerçek oturum kaydı yok. T01–T08 yalnız ayrılabilir anonim kodlardır; kayıt oluşturulması katılım değildir. Henüz oyuncu daveti veya dış paylaşım yapılmadı.

## Katılımcı ve oturum planı

Bu tablo gerçek insan roster'ı değil, sekiz boş planlama slotudur. T kodu yalnız bir kişi kabul edip atandığında verilir. Grup/segment hedefi aday havuzuna göre güncellenebilir; gerçekleşen profil ayrıca kaydedilir. Kontak bilgisi bu dosyada tutulmaz.

| Slot | Dalga | Hedef profil (gerçek kişi değil) | Gerçek T kodu / önceki maruziyet | Katılım durumu / zaman |
|---|---|---|---|---|
| 1 | D173 | Düzenli survival/FPS | Atanmadı | UNASSIGNED / planlanmadı |
| 2 | D173 | Orta survival veya FPS | Atanmadı | UNASSIGNED / planlanmadı |
| 3 | D173 | Düşük survival deneyimi | Atanmadı | UNASSIGNED / planlanmadı |
| 4 | D174 | Düzenli survival/FPS | Atanmadı | UNASSIGNED / planlanmadı |
| 5 | D174 | Orta/düşük deneyim | Atanmadı | UNASSIGNED / planlanmadı |
| 6 | D174 | İlk beşte az temsil edilen profil | Atanmadı | UNASSIGNED / planlanmadı |
| 7 | D174 | İlk beşte az temsil edilen profil | Atanmadı | UNASSIGNED / planlanmadı |
| 8 | D174 | İlk beşte az temsil edilen profil | Atanmadı | UNASSIGNED / planlanmadı |

Gerçek durum zinciri: UNASSIGNED → CONTACTED → CONFIRMED → SCHEDULED → COMPLETED; DECLINED/CANCELLED/WITHDRAWN ayrı. Yalnız owner'ın gerçekten yaptığı temas kaydedilir; Codex davet göndermez. İlk beşin sırası session start ile belirlenir, başarılı kişiler seçilerek payda oluşturulmaz. Son üç slot gerekli toplam/segment hedefi için kullanılabilir; gerçek katılım 5'in altındaysa eksik n açıkça raporlanır.

Kayda izin vermeyen katılımcıya okunacak metin: “Ses veya görüntü kaydı almayacağım. İzin verdiğiniz ölçüde anonim davranış notları tutacağım. İstediğiniz zaman durabilir veya not tutmayı bırakmamı isteyebilirsiniz.” Geri çekilmede oturum durdurulur; kişiyle kararlaştırılan silme/saklama tercihi uygulanır. Geri çekilen kaydın verisi analize katılmaz; neden söylemek zorunda değildir. Genel eksik veri sayımında sonuçları ifşa etmeyen kayıt durumu tutulabilir. Not tutmaya da izin yoksa araştırma verisi toplanmaz.

## Araştırma soruları ve açılış koşulu

Oyuncu ilk kaynağı/çantayı kendi bulabiliyor mu? Hedefi, tehdidi ve geri dönme nedenini anlayabiliyor mu? Hangi an merak/gerilim yaratıyor, hangi an koparıyor? Öldüğünde nedenini ve sonraki eylemini açıklayabiliyor mu? Kayıttan devam ederken hedef ve sahiplik korunuyor mu? Bunlar teknik test sonucu ve eğlence yargısından ayrı kaydedilir.

Genel pilot ancak [freeze kaydı](S018_BUILD_FREEZE.md) READY olduğunda, temel teknik QA ve owner dry-run bitince başlar. 2026-10-09 canonical consume/treatment/çanta implementasyonu eklendi; yeni kaynak teknik ve owner kabulü olmadan tam slice testi başlatılmaz. Dar kapsam verisi canonical tam-loop kabulü yerine geçmez. S0/S1 varsa genel pilot ertelenir; kusur araştırma oturumu ayrıca etiketlenir.

Toplam 5–8 gerçek kişi; tercihen hem survival/FPS deneyimli hem orta/düşük deneyimli oyuncular. Örneğin ilk 3 kişi / sonraki 2–5 kişi planlanabilir; bunlar gerçekleşmiş sayı değildir. Aynı kişiyi tekrar sayarak hedef doldurulmaz. Tek uzman başarısı genel kullanılabilirlik kanıtı değildir. Cihaz karşılaştırması için her alt gruptaki gerçek n ayrı raporlanır.

## Ön tarama — kısa ve tarafsız

1. Survival ve FPS oyunlarını ne sıklıkla oynarsınız? Her biri: hiç / ara sıra / düzenli; süre aralığı isteğe bağlı.
2. Benzer oyunlardan hangilerini biliyorsunuz? Bu projeyi daha önce gördünüz veya oynadınız mı?
3. Genellikle hangi kontrol cihazını kullanırsınız; bu oturumda hangisini kullanacaksınız?
4. OS/sürüm, CPU, GPU, RAM, çözünürlük, kontrolcü modeli/bağlantısı ve ses çıkışı nedir?
5. Rahat oynayabilmek için değiştirmek istediğiniz metin boyutu, duyarlılık, FOV veya ses ayarı var mı? Sağlık teşhisi istemeyin.

Kimlik, adres, e-posta ve yaş gibi gereksiz PII toplanmaz. Oturum kaydı yalnız anonim kod içerir. Ses/video/ekran paylaşımı için önceden açık onay alınır; red durumunda yalnız izin verilen gözlem notları kullanılır. Onay kapsamı, kaydı görebilecek kişi (owner), saklama/silme tarihi ve vazgeçme yolu oturumdan önce belirtilir. Ad/hesap bilgisi içeren ekranlar saklanmadan önce ayıklanır. Bilgi verilmemişse alan UNKNOWN bırakılır.

## Owner hazırlığı — oyuncuya çözüm anlatmayın

- Tek frozen paket hashini doğrulayın; build ID, source digest, platform ve ayarları forma girin. PT1 sırasında balance/loot/UI/enemy/nav/tutorial değiştirmeyin.
- Aynı başlangıç seed'i (12345; buildde doğrulanacak), yeni oyun durumu ve tutorial reset uygulayın. Ayrı test OS hesabı/profili tercih edin; kişisel save veya PlayerPrefs'i silmeyin. Aynı makinedeki ikinci oyuncuya birincinin tutorial completed/rebind/save durumunu devretmeyin. Profil sıfırlama yolunu owner dry-run'da doğrulayın.
- Varsayılan PC/1920×1080 windowed başlangıcını cihazla doğrulayın. Rahatlık için oyuncunun FOV/input/metin değişikliğine izin verin; değer ve zamanı kaydedin. Bunlar gameplay değişikliği değildir, karşılaştırmada confounder'dır.
- Kayıt ve kronometreyi hazır edin. Gözlemci gerçek zamanı/elapsed zamanı tutar. Doğal oynama tercih edilir; think-aloud istenirse ayrı koşul etiketi verilir.

Oyuncuya okunacak açılış metni:

> Bu oyunun ilk deneyimini anlamaya çalışıyoruz. Sizi değerlendirmiyoruz. Yaklaşık 30–45 dakika, normalde nasıl oynarsanız öyle oynayın; sonra yaklaşık 10 dakika konuşacağız. İstediğiniz zaman durabilirsiniz. Başlangıçta nasıl ilerleyeceğinizi anlatmayacağım. Devam edemediğinizi düşünürseniz söyleyebilirsiniz.

İlk koşuda öğretme, hedef yerini söyleme, doğru eşyayı gösterme veya mekanik savunması yapmayın. Gerçek blocker'da önce davranışı/zamanı kaydedin, sonra gereken en küçük yardımı verin. Her ipucu, menü açıklaması veya teknik kurtarma ayrı müdahaledir. Gözlemci yardımı olmadan oyunun kendi tutorial/hint'i UNAIDED tanımına dahildir; kullanımı ayrıca işaretlenir. Bir müdahale sonrası koşu tamamen UNAIDED olamaz. Ölümü veya yarayı zorla yaratmayın; yaşanmayan olay N/A ve gerekçesiyle kalır.

## Completion tanımı — yalnız gözlemci için

Önceden dondurulmuş rota kontrol listesi: hazırlık → keşif/loot → tehdit kararı → relay seferi/progression → sığınağa dönüş/uygulanmış upgrade → save → uygulamadan çık → yeniden başlat → load → ilerlemeye devam. Aşağıdaki başarı işaretleri mevcut QA1 kaynak sözleşmesinden çıkarıldı; gerçek build'de görünürlük/doğruluk kabulü NOT_RUN. Oturum sonrası başarı tanımı değiştirilmez. Treatment/consume BLOCKED adımları atlanarak canonical completion verilmez.

| Ölçüm | Oyuncuya görünen başarı işareti | Observer/teknik kanıt sınırı |
|---|---|---|
| İlk kaynak / onboarding | Gerçek pickup inventory'de görünür; oyuncu inventory açıp eşyanın yerini kendi bulur | Su edinimi ve su tüketimi ayrı. Bir resource pickup tüm onboarding'i PASS yapmaz. |
| Sefer / relay | Kabin radyosu notu, fuse ve wrench ile repair; ardından radyoya dönüş ve journal'da “Radio contact heard. Expedition completed.” | `RelayMission.Journal`, `RelayProgression`: Radio/Repaired/Listened; repairReceipt=relay.repair.v1, rewardReceipt=reward.contact-intel.v1, rewardCount=1, completionCount=1, phase=1. Fuse-first geçerli; sırayı zorla öğretmeyin. |
| Shelter üretim / upgrade | Claim ve modüller görünür; mevcut storage üzerinden bir craft çıktısı alınmış ve paid upgrade uygulanmış | `ShelterSite.Act`, `S012Acceptance.Shelter`: installed bed/storage/workbench, output commit, Upgraded. Kontrollü fixture'ın 10 ammo/6 retained scrap gibi başlangıç sayıları doğal oturuma sabit beklenen toplam diye dayatılmaz. Önce/sonra conservation kaydı tutulur. |
| Tehdit kararı | Oyuncu tehdide davranışla tepki verir (çatışma, kaçınma, geri dönüş vb.) | Hasar almak/ölmek veya zombiyi öldürmek başarı şartı değil. Oyuncu tehditle karşılaşmadıysa NOT_OBSERVED. |
| Persistence | Save başarı mesajı; uygulama tamamen kapandıktan sonra Load; kaydedilmiş inventory/progression/shelter state ile sonraki eylem yapılır | Menüye dönmek process quit değildir. Save header schema/seed/content ve receipt/state karşılaştırması teknik kanıttır, oyuncu anlama puanı değildir. |

Kaynaklar: `Assets/LastSignal/Scripts/Runtime/Objectives/{RelayMission,RelayProgression}.cs`, `Runtime/Shelter/ShelterSite.cs`, `Runtime/Slice/S012Acceptance.cs`. Observer bu çözüm/receipt tablosunu oyuncuya göstermez. Canonical tedavi/consume eksikleri açık kalır; bu tablo yeni oyun davranışı eklemez veya geniş slice kabulünü sessizce daraltmaz.

Onboarding (ilk su **edinimi**, inventory kullanımı), sefer ve tam loop üç ayrı sonuçtur. Su edinimi, su tüketimi değildir. Süre sınırında bitmemesi, blocker nedeniyle bitmemesi, oyuncunun bırakması ve performans engeli ayrı end-state olur. Yardımlı/yardsımsız başarı için hem binary outcome hem yardım türü tutulur; teknik destek de tam unaided sayımını etkiler.

## Kopyalanacak boş oturum formu

Dosya: `Evidence/<run-id>/<candidate>-<Txx>-<platform>-session.md`. Gerçek oturum olmadan dosyayı sonuçlarla doldurmayın. Kayıt dosyaları aynı öneki kullanır; eski kanıtın üzerine yazmayın.

```text
Status: NOT_RUN
Tester ID: [yalnız gerçek katılımcıya T01–T08 atanır]
Candidate / platform artifact SHA-256 / source digest:
Scene / seed / initial save-fixture hash / tutorial initial state:
First exposure or REPEAT_TESTER — LEARNING EFFECT POSSIBLE:
Survival experience / FPS experience / similar games familiarity:
Project familiarity / segment rationale:
OS version / CPU / GPU / RAM:
Input type / controller exact model / wired-wireless:
Resolution / quality / FOV / UI scale / sensitivity / audio output:
Observation mode: natural / think-aloud
Recording consent / allowed media / access / deletion date:
Session start / end (ISO-8601 offset, e.g. +03:00) / active duration / pauses:

Milestone | elapsed time | observable behavior | evidence | help before this event?
First water/resource acquired:
First inventory open / close / total inventory dwell:
Inventory confusion (attempts, wrong actions, discarded item; not guessed motive):
First meaningful objective understood (spontaneous words/action):
First threat detection (visible player response):
First damage / first death:
Return decision / relay progress / upgrade:
Save / quit / process restart / load / continued progression:

Intervention elapsed time | blocker/reason | exact help | gameplay/technical | resumed?
[one row for EVERY intervention; explicit NONE only after a real completed session]

Onboarding: UNAIDED / ASSISTED / INCOMPLETE / NOT_OBSERVED
Expedition: UNAIDED / ASSISTED / INCOMPLETE / NOT_OBSERVED
Canonical full loop: UNAIDED / ASSISTED / INCOMPLETE / BLOCKED
Intervention count / blocker count / serious friction count:
End state: completed / death / voluntary stop / time cap / blocked / crash / other
Death cause understood? yes / partial / no / N/A (no death); exact player statement:
Next intended action understood? yes / partial / no; exact player statement:
S0/S1 suspected issues (repro, frequency, exposed opportunities, evidence):
Performance / input / FOV / recording-overhead confounders and setting-change timestamps:
Memorable moments / boring moments / repeated failed actions (observed separately):
Observer / note timestamp / evidence references / missing-data reason:
```

İlk su/hedefe erişilmediyse 0 saniye yazmayın: `NOT_REACHED; observed until mm:ss`. Threat/death yaşanmadıysa başarısız anlama diye saymayın. Inventory dwell ilgisizlik veya kafa karışıklığı diye tek başına yorumlanmaz; davranış ve görüşme birlikte değerlendirilir. Aynı tester/bulgu birden çok oluşabilir; kişi sıklığı ve olay adedi ayrıdır.

## Oturum sonrası görüşme — yaklaşık 10 dakika

1. En çok aklınızda kalan an neydi? O anda ne yapıyordunuz?
2. En zayıf veya sıkıcı bulduğunuz bölüm hangisiydi?
3. Bir şeyin nasıl çalıştığını anlamadığınız bir an oldu mu? Ne bekliyordunuz?
4. Sizce amacınız neydi; devam etseydiniz ne yapardınız?
5. Hasar/ölüm yaşandıysa sizce nedeni neydi? Farklı ne deneyebilirdiniz?
6. Oynamaya devam etmek istemenize neden olacak şey nedir? Bırakmanıza neden olacak şey nedir?
7. Olmasını beklediğiniz ama gerçekleşmeyen ne vardı?
8. Kendi isteğinizle başka bir koşuya başlar mıydınız? Neden?

“Minimap olsa daha iyi değil mi?” gibi çözüm telkinleri kullanılmaz. İsteğe bağlı sayısal puan yalnız tamamlayıcıdır; eğlenceyi tek puana indirmeyin. Oyuncu sözü tırnak içinde tam veya açıkça paraphrase olarak yazılır; gözlemci yorumu ayrı alana girer.

## Hesaplama ve ikinci tur

Her candidate/platform/segment için gerçek n ve eksik veri belirtilir. Unaided/assisted/incomplete sayıları ayrı; blocker kişi oranı `en az bir blocker yaşayan / ilgili senaryoya maruz kalan` olarak raporlanır. İlk beş katılımcıda yaklaşık 4/5 onboarding, 3/5 blockersız sefer, çoğunluk ölüm/sonraki eylem anlayışı yön göstericidir; pazar eşiği veya istatistiksel başarı garantisi değildir. Küçük n için ham sayılar ve bireysel süreler gösterilir.

D177: sadece teknik olarak kabul edilmiş yeni PT2 ile; mümkünse yeni kişiler. Tekrarlayanlar `REPEAT_TESTER — LEARNING EFFECT POSSIBLE`. PT1/PT2 ve fresh/repeat ayrı; yardım, blocker, hedef süresi, completion ve comprehension karşılaştırılır. İyileşme ölçülmeden ilan edilmez. Kritik kusur build değiştirirse yeni ID verilir ve kalan PT1 oturumları durdurulur; farklı adaylar birleştirilmez.


## Owner kabulü ile dış pilot arasındaki ayrım

Windows Round 7 owner teknik kontrolüdür; bu oturum T01–T08'e eklenmez. Aynı build üzerinden owner'ın görsel/ses kabulü toplanabilir; aşağıdaki boş form teknik sonuca eşlik eder. Katılımcı oyunu öğrenmeden önce bu kontrol owner tarafından yapılır.

```text
Candidate / ZIP SHA-256 / Windows hardware:
Ana menü, Yeni Oyun, pause/settings: expected / actual / evidence / status
Gündüz-gece-yağmur görünürlüğü: observed condition / readability / status
Kapı/loot/relay/shelter prompt okunurluğu ve focus: actual / evidence
Combat tehdidi, hit/miss/death okunurluğu: actual / evidence
Kulaklık/hoparlör kullanılan çıkış: device / hangi koşul gerçekten denendi?
Footstep/yön/mesafe, reload ses-görüntü, yağmur masking, loop/clipping: actual
Mute/master/effects/caption davranışı: ayar / actual / evidence
Alt-tab/pause sonrası yanlış hareket/ateş: actual / evidence
Save başarı mesajı, process exit, restart/load, sonraki progression: actual
Eksik consume/treatment adımları: BLOCKED; yerine shelter recovery sayılmadı
Sorun ID / severity gerekçesi / tekrar sayısı ve maruziyet / log zamanı:
Ölçülmeyen koşullar: NOT_RUN (dinlenmeyen hoparlör veya görülmeyen hava dahil)
```

İlk tur katılımcı atamaları gerçek kayıt gelene kadar boş tutulur. Teknik engel çıkarsa o adayı genel pilota açmayın; önce hata sınıflaması ve dar doğrulama. Ready kararı için G5 raporundaki kabul matrisi kullanılır.

## Offline kayıt aracı

Uygulama: `Tools/s018-evidence.py`; stdlib dışında bağımlılık yok. Doğrulama **NOT_RUN**; son test aşamasından önce kullanıma hazır/PASS sayılmaz. Araç yalnız kayıt bütünlüğünü denetler, insan beyanlarını doğrulamaz. D175 sentezi için otomatik karar vermez.

Son doğrulamada unit kontrolleri geçtikten ve gerçek PT1 aday kimliği dondurulduktan sonra kullanım:

```text
python3 Tools/s018-evidence.py form --candidate-id PT1_CANDIDATE_ID --artifact-sha256 ACTUAL_64_CHAR_LOWERCASE_SHA256 --tester T01 --output session-T01.json
python3 Tools/s018-evidence.py validate session-T01.json
python3 Tools/s018-evidence.py summarize session-T01.json session-T02.json --output synthesis.json
```

Bunlar parametre örnekleridir; büyük harfli değerler gerçek kimlikle değiştirilir. QA1 otomatik PT1 değildir. Araç `form` çıktısı NOT_RUN kalır; session_id veya insan katılımı uydurulmaz. Gerçek oturum bittikten sonra observer kaydı doldurup `record_state=RECORDED` yapar. RECORDED oyuncunun başarılı olduğu anlamına gelmez. Onboarding, expedition ve canonical_loop sonuçları ayrı UNAIDED/ASSISTED/INCOMPLETE/NOT_OBSERVED/BLOCKED olur.

Alan sözleşmesi:

- Kimlik: `candidate.source_digest` gerçek freeze manifestinden; `phase` PT1/PT2; `exposure` FRESH/REPEAT; `scope` CANONICAL/NARROW/DEFECT_INVESTIGATION. Platform ve segment açıklaması zorunlu. Profil/ortam/başlangıç durumunda bilinmeyen her alan için `missing_data["section.field"]` gerekçe içerir. Yeni save için save_fixture_sha256 null ve açıklaması kullanılır; var olan fixture için gerçek SHA-256 yazılır.
- Rıza: `state=GRANTED`, `allowed_media` örneğin yalnız `["notes"]`; erişim sahibi, silme tarihi YYYY-MM-DD ve çekilme talimatı gerekir. Video/ses kaydı zorunlu değildir. WITHDRAWN kayıt özete alınmaz; izin verilen kapsamın dışında medya bağlanmaz.
- Provenance: anonim observer kodu, timezone içeren ISO recorded_at, `human_recorded=true` ve gerçek kaynak notu gerekir. Her evidence nesnesi `id`, `description`, `media`, `path`, `sha256` taşır. Path session JSON'un klasörüne göre çözülür. Kaynak notları ayrı dosyadır; JSON kendi kaynağı olamaz. Kişisel veriyi çıktı/repoya koymadan önce maskeleyin; hash son maskelenmiş dosyaya ait olmalıdır.
- Zaman: started_at/ended_at timezone içeren ISO; bütün elapsed_seconds oturum başlangıcından itibaren **duvar saati saniyesidir**. Pause nesnesi `start_seconds`, `duration_seconds`, `reason`; active_seconds + pause toplamı oturum süresidir. Olay olmayan hedefin elapsed_seconds değeri null kalır; 0 yazılmaz.
- Gözlem: OBSERVED için elapsed_seconds, behavior, help_before_event ve evidence_refs; NOT_REACHED/NOT_OBSERVED için observed_until_seconds ve missing_reason; NOT_APPLICABLE için gerekçe. Inventory dwell değerini `value` içinde saniye olarak açıklayın. Outcome süresi aynı duvar saati eksenindedir. Canonical tamamlanma equip/consume/treatment, relay/shelter ve gerçek save/quit/process_restart/load/continued_progress gözlemleri gerektirir. Eksik uygulanmış domain bu alanları gözlenmiş yapmaz.
- Müdahale nesnesi: elapsed_seconds, kind GAMEPLAY/TECHNICAL, reason, exact_help, resumed boolean, evidence_refs. Yardım yoksa ancak observer kontrolünden sonra intervention_review_complete=true ve boş liste. Araç muhafazakâr olarak her müdahaleyi yardım sayar; teknik yardımın etkisi yorumda ayrıca açıklanır.
- Issue nesnesi: id, description, severity S0–S4, event_count, exposed_opportunities, evidence_refs. Issue yoksa ancak gerçek gözlemden sonra issue_review_complete=true ve boş liste. Olay/fırsat ve benzersiz kişi paydaları aynı şey değildir.
- Comprehension: YES/PARTIAL/NO için gerçek statement, statement_kind VERBATIM/PARAPHRASE ve evidence_refs. UNKNOWN/NOT_APPLICABLE için missing_reason. Interview yanıtı ANSWERED/DECLINED/NOT_APPLICABLE/NOT_COLLECTED; yanıt olmayan durumda answer=null ve gerekçe. Hiçbir söz AI ile tamamlanmaz.
- Ayar değişimi nesnesi: elapsed_seconds, setting, before, after, reason, evidence_refs. Confounder/observer_interpretations gerçek gözlemden ayrı tutulur. Kayıt tamlığı kontrolü insan editörün kaynak okumasının yerini tutmaz.

Exit code: 0 kayıt biçimi geçerli/çıktı oluşturuldu; 1 kayıt reddedildi; 2 dosya/JSON/CLI hatası. Çıktı varsa üzerine yazılmaz. Geçersiz batch için summary dosyası üretilmez. Bütün oturumları birlikte validate/summarize edin: session_id yinelenmesi ve bir kişiye birden çok FRESH iddiası batch içinde yakalanır; ayrı çağrılar arasında katılımcı geçmişi tutulmaz. Repeat oturumun önceki kaydı pakette yoksa bunu sentezde eksik veri olarak belirtin.

Özet yalnız aday/hash/platform/segment/exposure/kapsam grupları ve ham sonuçları içerir. G5 NOT_EVALUATED; 5–8 gerçek kişi, scope ve öneri kararını owner kaynak notlarıyla değerlendirir. Fonksiyonel araç doğrulaması son aşamaya ertelenmiştir; sentetik test fixture'ları hiçbir zaman bu kitin gerçek katılımcı kayıtları değildir.

## Canonical aday güncellemesi — 2026-10-09

QA2 kaynaklarında su/konserve kullanımı, üç saniyelik bandaj ve çanta takma/çıkarma uygulandı; teknik test ve yeni build NOT_RUN. Yukarıdaki QA1 eksik-domain notları tarihsel baseline'dır. Yeni oturumda: bag inventory'den equipment owner'a geçmeli, kapasite 24→32 olmalı; su/yemekte seçili yığın bir azalmalı ve ilgili kaynak artmalı; bandaj sırasında kanama devam etmeli, committe bir bandaj eksilip kanama durmalı. Hasar/hareket/menü iptalinde bandaj eksilmemeli. Gerçek oturum gözlemi olmadan bu alanlara OBSERVED yazılmaz. Eski QA1, bu yeni davranışları içermez.

Kayıt aracı son sıkılaştırma: onboarding için water_acquired/inventory_open, expedition için relay_progress gözlemi gerekir; olay tamamlanmadan completion zamanı yazılamaz. Aynı aday ID farklı source/artifact hash'i taşıyamaz (platform paketleri ayrı ID alır). Aynı kişinin çakışan oturum zamanları batch'te reddedilir. Bu kontrollerin unit kaynakları hazır, yürütmesi NOT_RUN.
