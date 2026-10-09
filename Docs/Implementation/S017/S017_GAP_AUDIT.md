# S017 Gap Audit — 2026-10-08

Bu belge başlangıç denetimidir; aşağıdaki açıklar denetim anını anlatır. Güncel uygulama ve kabul durumu: [çalışma kaydı](S017_WORKING_LEDGER.md). S017 kapanmadı.

## Başlangıç kimliği

- Yerel root: `/Users/burakcoskun/Last Signal`.
- Branch: `s012-integrated-graybox-slice`; HEAD: `7f5ffb783f396f9675c28da18be7021d90a02eb7`.
- Çalışma ağacı dirty: S016 runtime/test/scene değişiklikleri, yeni inventory factory/save feedback/font, S014/VEH kayıtları ve yerel Noto/Kenney assetleri zaten vardı. Korundu; checkout/reset/cleanup yapılmadı.
- İlk `git status` LFS clean filtresinin sandbox yazma sınırında durdu. Salt okuma için süreç filtresi komut düzeyinde kapatıldı; bu sorgudaki tarihsel PNG satırları gerçek görsel değişiklik kanıtı sayılmadı. Git config değiştirilmedi.
- Unity `6000.5.0f1`, Input System `1.19.0`; executable `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity` mevcut.
- Root/üst dizin ve proje dosya keşfinde AGENTS.md bulunmadı. Unity MCP GetUserGuidelines başarı döndürdü, ek yönerge içeriği vermedi. Current_State adıyla yalnız şablonlar bulundu; gerçek durum için sprint çalışma kayıtları kullanıldı.
- S015 report PARTIAL. En güncel ilgili S016 ledger: EditMode 530/530 PASS; son tam PlayMode 296/297 FAIL; Movement native-clock fixture değişikliği sonrası odaklı 17 test NOT_RUN. Bunlar geçmiş kayıttan okunmuştur; yeni S017 kanıtı değildir. S016 D160 insan kabulü ve bazı UI/domain açıkları sürüyor.

## Gerçek runtime ve farklar

| Kart | Audit bulgusu / sonraki işlem |
|---|---|
| D161 | Windows keyboard/mouse zorunlu; Xbox/XInput ve generic gamepad aday. Kod/sentetik/fiziksel/mağaza ayrımı aşağıda. |
| D162 | Reader ve motor ClampMagnitude ile analog büyüklüğü koruyor; motor dt alt adımlı. Player stick bakışı fare deltası sanılıyor, Vehicle `1000*dt` kullanıyor. Profil S016 AudioPreferences v1. İlk grup bunları genişletir. |
| D163 | S013Cabin EventSystem aktif, navigation açık, firstSelected=0. InputSystemUIInputModule paket DefaultInputActions asset'ini kullanıyor (`ca9f...`), player `052faa...` asset'inin runtime klonunu kullanıyor. Default/return focus ve görünür focus kurulmalı; UI action sahipliği birleştirilmeli. SessionFlow mevcut tek modal/pause otoritesi; Start otomatik BeginSession yapıyor, ayrı settings ekranı yok. |
| D164 | S016 InventoryUI public komutlarla move/drop/split-half; ShelterStorageUI iki pane ve domain ret mesajları içeriyor. Focus/Submit/Back akışı, miktarlı split ve instance güvenliği gözden geçirilecek. Genel equip/consume public domain komutu S016 matrisinde eksik; UI üzerinden sahte state mutasyonu yapılmayacak. |
| D165 | Interact dahil sabit E promptları var. Merkezi anlamlı cihaz takibi, binding-display/glyph resolver ve interactive rebind bulunmadı. UI modülünün ayrı asset'i rebind ile senkronize edilmeden güvenli kabul verilemez. |
| D166 | Dar runtime aramasında tutorial/onboarding servisi bulunmadı. Context olayları üzerinden küçük profil tabanlı bir defalık hareket/loot/tedavi öğretimi gerekli; tedavi public domain yolu önce doğrulanacak. |
| D167 | BuildUtility.BuildP5Mac eski validation sahnesi; gerçek üretim builder S013ProductionAuthoring.Build, rota S013Cabin, yalnız macOS. WindowsStandaloneSupport yerelde var; Windows64 üretim yolu ilk grupta hazırlanır. Fiziksel Windows koşusu NOT_RUN. |
| D168 | Henüz gerçek Windows sonucu yok. Spekülatif platform yeniden yazımı yapılmaz; NOT_RUN. |
| D169 | FocusLost/neutralRequired var; device-change dinleyicisi yok. Reconnect, drift eşikleri, menüde tekrar odak ve input abonelikleri sonraki grupta. |
| D170 | S013Cabin üretim rotası korunur; 30–45 dk golden path, settings/rebind, loot/inventory, treatment, combat, araç, shelter/progression, save/quit/load ve device switch NOT_RUN. Journal ve araç inspection içinde doğrudan Keyboard okumaları da tam kontrol rotasına engel adayı. |

Tarihsel binding endişelerinin üçü de yerelde mevcut: Player Pause Start yok; Attack/Reload buttonWest paylaşımı; MeleeSlot/FirearmSlot keyboard-only. İlk grup çakışmayı kaldırır. Kullanılmayan Next/Previous actionları mevcut; yeni slot komutları onların d-pad sağ/sol bindinglerini kopyalamaz.

## Destek sözleşmesi

| Platform / cihaz | Kod kapsamı | S017 otomasyon | Fiziksel kabul | Mağaza/shipping vaadi |
|---|---|---|---|---|
| Windows / keyboard+mouse | Mevcut zorunlu yol, regresyon korunacak | NOT_RUN | NOT_RUN | Henüz kabul kanıtı yok |
| Windows / Xbox-XInput | Aday; gameplay binding + look, UI/rebind açık | NOT_RUN | NOT_RUN | Yok; model/bağlantı kanıtı gerekli |
| Windows / Generic Gamepad | Aday; Unity Gamepad tabanı, yanlış Xbox ikonu gösterilmez | NOT_RUN | NOT_RUN | Yok; model bazında değerlendirilir |
| macOS / keyboard+mouse | Geliştirme ve regresyon ortamı | NOT_RUN (S017) | NOT_RUN (S017) | Windows kabulü yerine geçmez |
| macOS / gamepad | Geliştirme adayı | NOT_RUN | NOT_RUN | Yok |
| PlayStation, Switch, XR, Touch, Joystick | S017 destek kapsamı dışında; eski asset bindingleri destek sözü değil | N/A | N/A | Yok |

Sentetik Gamepad testi fiziksel cihaz kabulü değildir. Destek matrisi model/OS/bağlantı ve gerçek kanıt geldikçe daraltılıp doğrulanır.

## Asset ve build

Kenney `Assets/kenney_input-prompts_1.5/License.txt`: Input Prompts 1.5A, CC0. Keyboard & Mouse, Xbox Series, Generic aileleri yerelde; yeni import yapılmadı. S017 runtime katalog/atlas henüz bağlanmadı; D165 görsel kabul NOT_RUN. Yalnız ihtiyaç duyulan sprite referansları ve binding text fallback planlanır.

EditorBuildSettings hâlâ eski validation sahnelerini içeriyor. Üretim builder açıkça S013Cabin'i seçer; Build Settings listesinden eski mini-scene alınmaz. Windows shader/input/font/path/save ve gerçek donanım sonucu henüz yok.

## Güvenli sıra ve riskler

D161 → binding/D162 profil ve look → dar kullanıcı EditMode kapısı → gerçek reader/motor/scene PlayMode kapısı → D163 focus ve action sahipliği → D164 public inventory işlemleri → D165 cihaz/prompt/rebind → D166 öğretim → D169 hot-plug → kullanıcı regresyon/build → D167/D168/D170 fiziksel kabul.

Muhtemel sonraki dosyalar: SessionFlow, AcceptanceHud, InventoryUI/InventorySlotUI, ShelterStorageUI/ShelterSlotUI, InteractionController ve prompt sağlayıcıları, AudioPreferences/AudioSettingsView, PlayerInputReader partialları, RelayMission/VehicleInspectionView, S013Cabin UI referansları. Ayrı input/inventory/menu/save framework'ü kurulmaz; networking yok.

S0/S1: gerçek triage sayısı ölçülmedi, sıfır iddiası yok. Mevcut S016 tam regresyon FAIL açık. S018 giriş durumu BLOCKED: S017 kabulü ve devralınan önkoşul açıkları kapanmadı.
