# S017 — Uygulama ve kullanıcı doğrulama devri

Tarih: 2026-10-08. S017 kapanmadı. Güncel kart durumları: [çalışma kaydı](S017_WORKING_LEDGER.md). İlk kaynak denetimi: [gap audit](S017_GAP_AUDIT.md).

## Hazır uygulama grubu

Mevcut Input System ve SessionFlow üzerinde controller look/profil, merkezi cihaz ve UI action sahipliği, menü/settings/journal odağı, görünür seçim ve scroll navigation, miktarlı inventory split, stale-selection koruması, shelter/cargo UI, güvenli rebinding, binding kaynaklı prompt/glyph, ilk kullanım rehberi ve disconnect/focus neutral gate uygulandı. Ayrı input, inventory, menu veya save framework'ü eklenmedi. Dünya save şeması değişmedi.

Genel item equip/consume ve medikal wound/bandage treatment domain komutları mevcut değil; bunlar tamamlandı sayılmıyor. Rehberin recovery adımı mevcut shelter iyileşmesine bağlı. Fiziksel cihaz, görsel çözünürlük/ölçek ve Windows kabulü bekliyor.

Kullanıcının son yönlendirmesiyle uygulama grupları arasında kullanıcı tarafından odaklı test çalıştırma düzenine dönüldü. Agent Unity test, Play Mode veya build çalıştırmadı.

## Şimdi çalıştırılacak tek kapı

Bu komutu çalıştırmadan önce Unity Editor'ü kapat. Koşu bitene kadar proje kaynaklarını değiştirme.

```sh
cd "/Users/burakcoskun/Last Signal"
python3 Tools/s017-verify.py regression-play
```

Gerçek assembly `LastSignal.PlayModeTests`; filtre yok, tamamı birlikte çalıştırılır. Helper keşif alt sınırı 306 testtir. Önceki odaklı regresyonda 46/47 PASS ardından kalan pause testinin dar tekrarında 1/1 PASS elde edildi; tüm bu koşularda kaynak korunmuştur. Bunlar tek tam regresyon PASS'i yerine geçmez; şimdi son kaynak ve fixture izolasyonuyla tam PlayMode tekrarı isteniyor. Tarihsel evidence koruması etkin; sonraki build kapısı otomatik başlamaz.

Terminal çıktısı ve `RUN=` yolunu paylaş. Helper aynı koşuda `tests.xml`, `unity.log`, süreç sonucu, kaynak hashleri ve ProjectSettings önce/sonra kopyalarını saklar. Bir sonraki kapıyı kendiliğinden çalıştırmaz; kilit dosyasını silmez. Kaynak değişirse testler geçse de kapı FAIL kalır.

## Sonraki doğrulama sırası

1. Policy sonucu incelendi: 5/5 PASS, kaynak koruma PASS.
2. `focused-flow`: 20261008T082553-327919Z koşusunda 9/9 PASS, kaynak koruma PASS. Gerçek S013Cabin üzerinde dokuz PlayMode testi; controller menü/settings/back, analog look, disconnect/reconnect, rebind/persistence, focus loss, miktarlı split/revision, shelter/cargo, cihaz drift ve journal/inventory.
3. `focused-input`: 20261008T082826-106543Z koşusunda 6/6 PASS, kaynak koruma PASS.
4. `regression-edit`: 20261008T082950-052794Z koşusunda 541/541 PASS, kaynak koruma PASS. Şimdi `regression-play`; S016'dan devralınan Movement tam-regresyon hatası yeni sonuçla değerlendirilir.
5. `windows-build`: mevcut `LastSignal.Art.Editor.S013ProductionAuthoring.BuildS017Windows`, gerçek S013Cabin ve Windows64 Development. `Builds/S017/<run-id>/LastSignal.exe` ile kaynak/build kimliği saklanır. WindowsStandaloneSupport yerelde mevcut; fiili build NOT_RUN.
6. Gerçek Windows, fiziksel controller ve kesintisiz 30–45 dakika canonical slice kabulü; bulgulara göre D168 düzeltmeleri ve dar tekrar.

Şimdilik yalnız yukarıdaki regression-play komutu isteniyor. Sentetik gamepad testleri fiziksel cihaz kabulü değildir.

## Önemli davranış ve kanıt sınırları

Üretim standalone S013Cabin ana menüyle açılır. Editor ve eski validation sahneleri mevcut test/çalışma akışı için otomatik BeginSession davranışını korur; menü ReturnToMenu üzerinden test edilir. Standalone ilk açılış ayrıca Windows kabulünde doğrulanmalıdır.

Kritik movement/look/UI navigation/Pause recovery bindingleri sabittir. Rebinding desteklenen Player/Vehicle button eylemleriyle sınırlıdır; aynı bağlamda çakışma reddedilir, bozuk profil güvenli varsayılana döner. Rebind dinleme sırasında gameplay ve UI submit engellenir; Escape/Start ve görünür iptal yolu korunur.

Kenney CC0 yerel texture referansları kullanılır; yeni import yok. Generic gamepad için Xbox ikonu tahmin edilmez, binding metni gösterilir. İkon kapsamı dar katalogla sınırlı; diğer bindingler metin fallback kullanır.
