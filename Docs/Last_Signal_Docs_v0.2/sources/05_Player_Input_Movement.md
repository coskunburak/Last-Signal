---
doc_id: LS-DOC-05
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Oyuncu girdisi, kamera ve hareket sözleşmesi

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §7, §19. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Hedef ve kapsam

İlk prototipte WASD, mouse look, yürüyüş, sprint, çömelme, zıplama, gravity, grounding, slope ve step bulunur. Düşük engel aşma slice sonuna doğru eklenir; prone, wall run, combat slide yoktur. Controller seçimi mevcut repo varsa inceleme sonrası yapılır; kinematic capsule başlangıç önerisidir. Karakter kütlesi hissi input gecikmesiyle taklit edilmez.

**REQ-MOVE-001:** Hareket hızı render FPS'ine bağımlı olamaz; diagonal input normalize edilir, analog input büyüklüğü korunur. Aynı düz parkur 30/60/120 FPS'te kabul toleransında aynı sürede geçilir.

## 2. Input context

Gameplay, UI, Rebind, Console ve Cutscene action map/contextleri ayrıdır. Inventory açıldığında ateş input'u tüketilir; panel kapanan frame'deki click ateş etmez. Rebind sırasında pause/confirm tuşları gameplay'e sızmaz. Focus kaybında mouse capture bırakılır, basılı input state'i resetlenir. Focus geri gelince otomatik ateş/sprint başlamaz.

| Eylem | Varsayılan | Öncelik / davranış |
|---|---|---|
| Move / Look | WASD / mouse | Gameplay context |
| Jump | Space | Grounded veya kısa coyote window |
| Sprint | Left Shift | Hold/toggle; stamina/yük uygunsa |
| Crouch | C | Ayağa kalkış için head clearance |
| Interact | E | UI odaklıysa tüketilir |
| Reload | R | Weapon state izin verirse |
| Inventory | Tab | Modal aç/kapat |
| Pause | Escape | Açık modalı kapat; sonra pause |

Gamepad mapping bir UX hedefidir, klavye tuşlarının bire bir kopyası değildir. Interact tap ve reload hold aynı button'a atanırsa ambiguous press ateşlenmeden context resolver karar verir. UI'da açık prompt değişir.

## 3. Hareket parametreleri

Başlangıç hızları walk 3.2 m/s, sprint 5.5 m/s, crouch 1.6 m/s; acceleration 18 m/s², step 0.3 m, slope 45 derece çalışma hipotezidir. Bunların merkezi sahibi [balance](23_Balance_Katalog_ve_Ekonomi.md). Karakter capsule yüksekliği 1.8 m, crouch 1.2 m, yarıçap 0.3 m önerilir. Kapı genişliği bu ölçülere göre test edilir; görsel mesh collider yerine geçmez.

Ground probe eğimli yüzeyde normal kontrol eder. Slope limit dışındaki yüzey “grounded” diye sayılmaz; karakter kontrol edilebilir biçimde kayar veya ilerleme engellenir. Merdiven kenarında grounded jitter kamera zıplaması üretmemelidir. Düşme hasarı total y düşüşü veya landing velocity üzerinden açık politika ile hesaplanır; küçük basamaklardan birikimli ölüm oluşmaz.

**REQ-MOVE-002:** Çömelmeden kalkarken tavan overlap kontrolü yapılır. İzin yoksa crouch korunur; kamera collider'ın üstüne çıkamaz.

## 4. Eylem önceliği

Dead > modal/cutscene lock > stunned > vault > treatment/craft > locomotion. Bu sıra bütün hareketi her durumda kapatmaz: consume sırasında yavaş yürümeye izin verilip sprint iptal edilebilir. Her eylem permittedActions maskesi üretir; farklı scriptlerin aynı hız değerini her frame ezmesi engellenir.

Stamina yokken sprint reddedilir fakat normal yürüyüş açıktır. Overloaded state sprint/vault'u kapatır; carry yükü acceleration ve noise'u da etkiler. Sprint bırakılınca yavaşlama kontrollü, fakat oyuncuyu istemeden uçurumdan düşürecek uzun momentum yoktur.

## 5. Kamera ve silah temsili

Yaw player orientation; pitch camera/view pose. Pitch clamp başlangıç ±85 derece. FOV 60-100 arası kullanıcı seçeneği olabilir; ekran oranıyla aim hissi test edilir. ADS FOV geçişi sensitivity scaling ile uyumlu olur. Head bob, shake ve motion blur bağımsız sıfırlanabilir. Camera ray ile muzzle ray uyuşmazlığı duvar içinden ateş yaratamaz; [combat](10_Combat_Weapons_Damage.md).

Birinci şahıs kolların clipping çözümü gerçek collider'ı ortadan kaldırmaz. Ayrı render layer veya near-plane stratejisi seçilirse dünya etkileşim mesafesi aynı kalır. İlk prototype için full-body IK şart değildir.

## 6. Save/load ve güvenli spawn

Kayıtta region/cell + local position + rotation + son doğrulanmış safe anchor saklanır. Yükleme sonrası cell collision hazır olmadan gravity/input açılmaz. Konum artık geçersizse yakın nav/collision geçerli nokta denenir; başarısızsa explicit safe anchor kullanılır ve loglanır. Oyuncu yere düşmüş gibi kayıttan hasar almaz; kısa stabilize adımı yalnız load içindir, combat invulnerability exploit'i değildir.

## 7. Test senaryoları

**REQ-MOVE-003:** Normal/slope/step/low-ceiling parkuru gerçek buildde geçilmelidir. Kabul matrisi: spawn, look, WASD, jump, sprint depletion, recover, crouch head block, pause return, alt-tab, menu-return-repeat.

Edge durumlar: son stamina noktasında jump+sprint; moving platform kapsam dışıyken üzerine çıkış; iki collider birleşiminde takılma; load sırasında void; çok düşük FPS; gamepad çıkarılması; mouse sensitivity sıfır ayarı; aşırı FOV. Her hata engine issue diye kapatılmadan minimal sahnede yeniden üretilir.
