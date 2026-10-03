# S012 uygulama ve teknik doğrulama kaydı

## Kaynak ve kapsam

Başlangıç: `s012-integrated-graybox-slice`, HEAD `5bcdaf823affa1111a1076b2006c487c8893e2aa`, kirli çalışma ağacı. Kullanıcının önceden var olan optik, MRPoly, animasyon, input fixture, ProjectSettings ve eski kanıt değişiklikleri korundu. Giriş durumu ve hash manifesti `Evidence/20261002T234046Z/` altındadır. Unity 6000.5.0f1, macOS, Mac15,7, M3 Pro, 18432 MB RAM; testler açık Editor üzerinde yürütüldü. Bu çalışma boyunca Git commit/push yapılmadı.

## Oyun değişikliği

- `IntegratedGraybox.unity` S011 RelayExpedition kopyasından ayrı adaydır. Resident alan x=-400..0, z=-200..200 (400×400 m) sınırları ve NavMesh'iyle yüklendi. Beş resident yapı: cabin, market, clinic, maintenance, security; iki mevcut portal hücresi korundu. Kaynak/kritik item kimlikleri aynı; yeni bağımsız authority yok.
- Direct güvenlik/market yaklaşımı ve western clinic/görüş bariyeri yaklaşımı düzenlendi. Gerçek FirstPersonMotor/CharacterController ile ölçülen direct 462.07 m / 144.39 simülasyon saniyesi; covered 613.35 m / 191.65 s; sprint direct 462.21 m / 112.14 s; crouch covered 613.30 m / 383.20 s. Bağlantı ve röle dönüşü ayrıca PlayMode'da kontrol edildi. Bu süreler hızlandırılmış fixture ölçümüdür, insan gameplay süresi değildir.
- Yeni harita için Serialized `maintenanceClue` alanı RelayMission sunumuna eklendi; eski S011 metni fallback olarak kalır. Progression, reward receipt, save schema ve inventory işlemleri değişmedi.
- Başlangıç checkpoint yükleme hatası, spawn/radyo collider çakışması nedeniyle tespit edilip yeni sahnede spawn `ShelterLoop.InsideAnchor` konumuna alındı. Save doğrulaması gevşetilmedi.
- Yeni sahne `world.s012.graybox / s012-graybox-v1` kullanır; eski world koordinatlı save'leri yanlışlıkla yüklemez. Top-level save schema ve progression extension değişmedi. Bu, eski S011 world'ü yeni haritaya migrate etme iddiası değildir.
- Üç S012 EditMode, sekiz S012 PlayMode testi eklendi. S012Acceptance gerçek interaction/loot/combat/craft/save otoritelerini fixture pozisyonlarıyla teknik olarak sınar. S012Performance day/night/rain standalone örneklemesi ve 100 cell load/unload lifecycle aracı olarak hazır; henüz çalıştırılmadı.
- `Tools/s012-verify.py` açık Editor TestRunner'a istek yollar, her çalışmada yeni XML/manifest üretir ve sabit yola yazan eski test kanıtlarını korur. macOS development build `20261003T101609Z` üretildi; standalone kabul henüz çalıştırılmadı.

## Yürütülmüş kanıt

| Kontrol | Sonuç | Kanıt |
|---|---|---|
| İlk S012 EditMode | 3/3 PASS | `Evidence/20261002T234046Z/focused-edit-r1.xml` |
| İlk S012 PlayMode | 5/6, spawn/load S1 FAIL | `Evidence/20261002T234046Z/focused-play-r1.xml` |
| Düzeltme sonrası S012 PlayMode | 6/6 PASS | `Evidence/20261002T234046Z/focused-play-r2.xml` |
| Son S012 PlayMode, levye/dönüş eklendi | 8/8 PASS | `Evidence/20261003T000005-368564Z/play.xml` |
| Tam EditMode | 444/444 PASS, 0 skip/inconclusive | `Evidence/20261003T000119-606180Z/edit.xml` |
| Tam PlayMode | 204/204 PASS, 0 skip/inconclusive | `Evidence/20261003T000152-588133Z/play.xml` |
| Kullanıcı tekrar EditMode | 444/444 PASS | `Evidence/20261003T092844-802226Z/edit.xml` |
| Kullanıcı tekrar PlayMode | 200/204 FAIL; 4 test | `Evidence/20261003T092856-394627Z/play.xml` |
| Mühimmat hedefli tekrar | 1/1 PASS | `Evidence/20261003T095838-941329Z/play.xml` |
| ZombieDamage sınıfı hedefli tekrar | 15/15 PASS | `Evidence/20261003T095901-033134Z/play.xml` |
| Düzeltme sonrası tam PlayMode | 204/204 PASS, 0 skip/inconclusive | `Evidence/20261003T100356-546593Z/play.xml` |
| macOS standalone build | PASS; 0 hata, 399 uyarı | `Builds/S012/20261003T101609Z/build.txt`; `Evidence/20261003T101556Z/editor-build.log` |
| Standalone teknik rota | PASS; 0 hata, 27.57 s | `Evidence/20261003T101935Z-standalone/standalone.txt` ve rota/checkpoint çıktıları |
| Gündüz/gece/yağmur ve 100 cell lifecycle | FAIL; geçersiz örnek, düzeltme sonrası tekrar bekleniyor | `Evidence/20261003T102140Z-performance/result.txt` ve `player.log` |
| D118 düzeltmesi sonrası macOS build | PASS; 0 hata, 359 uyarı; executable SHA-256 `4709f31c16224ab6f52d7e4b5e6b9679856caaf5154daddfdd700ac330b9cc30` | `Builds/S012/20261003T102952Z/build.txt` |
| D118 standalone tekrar | PASS veri yakalama; bütçe kararı değil | `Evidence/20261003T103218Z-performance-retry/result.txt`, koşul CSV/PNG ve `lifecycle.csv` |
| Kesintisiz yardımsız 30–45 dk, kapat/aç/load | NOT_RUN | Burak kabulü gerekli |

Test XML'leri gerçek Unity TestRunner sonuçlarıdır. Dört hatayı kapsayan hedefli tekrarların ardından tam PlayMode 204/204 PASS verdi (566.01 s); dört başarısız test bu tam koşuda da geçti. Son EditMode sonucu 444/444 PASS. Başarısız 200/204 koşusu ve önceki sonuçlar tarihsel kanıt olarak korunur. Build'in fixture kullanan standalone teknik kabulü `20261003T101935Z-standalone` altında PASS verdi: görev, sığınak, ölüm checkpointleri, iki cell, levye ve motor rotaları; gerçek 30–45 dakikalık insan oynanışı veya performans sonucu değildir. Build uygulaması 411 MB; çalıştırılabilir dosyasının SHA-256 değeri `fc28e9ad8c3d6807421cfd4d632264fd4a2a0c5559fb55e500054dc8a3816acf`. S011 tarihsel final EditMode 409/409; `final/full-playmode.xml` 167/172, kökteki daha geç `full-playmode.xml` 172/172. Eski rapordaki tek 172/172 cümlesi hangi XML olduğu belirtilmeden kullanılmamalıdır.

PlayMode fixture'ı oyuncuyu item/etkileşim konumuna koyar, hasarı ölüm/sakatlık için kontrollü verir ve motor testini hızlandırır. İtem, görev fazı, repair, ödül, craft output veya sağlık kazanımı elle verilmez. Portaldan cell geçişi oyunun gerçek CellReady kapısından geçer. Bu kayıt gerçek bağımsız insan oynanışı değildir.

## Kabulü bekleyen sınırlar

Hedef alanda 30–45 dk gerçek rota ölçümü, karşılaşma temposunun gözlenmesi, sığınak çıktısının yardımsız gerçek oynanışta kullanımı ve uygulamayı kapat/aç/load kabulü henüz yok. D118 üç koşullu başlangıç ölçümü alındı; beş tekrar, combat/streaming kapsamı ve uzun oturum belleği açık. Bu eksikler S012/G3 kararını BLOCKED tutar. `S012_VERIFICATION_HANDOFF.md` Burak için adımları açık verir. Water/food/medical stokları tüketim/healing mekaniğine dönüştürülmedi; çalışan sağlık döngüsü yatağın WorldClock üzerinden iyileşmesidir. Açık resident nüfus yayılımı yok; bölgesel baskı iki portal cell'de çalışır. İki AI/görüş yolu teknik olarak bağlı olsa da eğlence/tehdit farkı gözlemi bekler.

## Efor ve varlık sahipliği

İlk uygulama/test penceresi yaklaşık 23:40–00:12 UTC idi; sonraki kullanıcı handoff çalışması ayrı zamanda sürdü. Kesintiler ve odak süresi ölçülmediği için toplam actual odak saati bilinmiyor. D111–D120 için canonical 40–60 odak saati tahmininin gerçekleştiği iddia edilmiyor. Yeni assetler Unity primitive/navmesh ve mevcut proje materyalleri kullanır; dış asset veya lisans eklenmedi. S011/S010 sahneleri değiştirilmedi.

## Dosya değişiklik envanteri

| Durum | Yol | Amaç |
|---|---|---|
| Değişti | `Assets/LastSignal/Scripts/Runtime/Objectives/RelayMission.cs` | Sahneye özgü ipucu metni, eski metin fallback |
| Yeni | `Assets/LastSignal/Scenes/IntegratedGraybox.unity` ve `.meta` | Ayrı S012 oynanabilir aday sahne |
| Yeni | `Assets/LastSignal/S012/ResidentNavigation.asset` ve `.meta` | 400×400 m resident NavMesh |
| Yeni | `Assets/LastSignal/Scripts/Editor/S012Authoring.cs` ve `.meta` | Tek seferlik sahne authoring ve batch build giriş noktası |
| Yeni | `Assets/LastSignal/Scripts/Runtime/Slice/S012Acceptance.cs` ve `.meta` | Opt-in teknik standalone rota |
| Yeni | `Assets/LastSignal/Scripts/Runtime/Slice/S012Performance.cs` ve `.meta` | Opt-in day/night/rain capture ve lifecycle |
| Yeni | `Assets/LastSignal/Scripts/Tests/EditMode/S012SceneTests.cs` ve `.meta` | Alan/tek otorite/loot/nav doğrulaması |
| Yeni | `Assets/LastSignal/Scripts/Tests/PlayMode/S012IntegrationTests.cs` ve `.meta` | Görev/sığınak/ölüm/iki rota/hücre entegrasyonu |
| Yeni | `Tools/s012-verify.py` | Mevcut Editor üzerinden kanıtı koruyan test yürütücüsü |
| Yeni | `Docs/Implementation/S012/` denetim, bütçe, rota, performans, uygulama, test komutları, G3 ve sorun belgeleri ile değişmez Evidence runları | İzlenebilirlik ve kullanıcı handoff |
| Silinen | Yok | Kullanıcı veya vendor kaynağı kaldırılmadı |

Yeni C# klasör `.meta` dosyaları Unity import sırasında üretilmiştir. Kullanıcının girişte var olan diğer değişiklikleri bu işin dosya envanterine dahil değildir.
