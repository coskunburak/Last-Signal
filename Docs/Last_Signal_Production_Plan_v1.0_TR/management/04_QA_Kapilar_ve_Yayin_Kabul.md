---
doc_id: LS-PLAN-M04
project: Project Last Signal
version: 1.0.0
created: 2026-09-17
language: tr
status: PROPOSED_BASELINE
implementation_status: NOT_VERIFIED
---

# QA, kabul kapıları ve release disiplini

## Test katmanları ve sonuçlar

EditMode domain invariantları; PlayMode input, collider, nav ve lifecycle; bağımsız build gerçek UI/render/platform; soak uzun oturum; insan playtest eğlence ve anlaşılabilirlik içindir. Bunların hiçbiri diğerinin yerine otomatik geçmez. PASS gerçek koşulan kontrol; FAIL beklenenin ihlali; NOT_RUN koşulmamış; BLOCKED gerekli erişim/girdi yok; N/A uygulanamaz ve gerekçelidir. Bu pakette oyun test sonucu yoktur.

## Değişiklik etkisine göre doğrulama

| Değişen alan | Asgari anlamlı doğrulama | Daha geniş kapı |
|---|---|---|
| Item/transfer | Full bag, duplicate pickup, toplam adet/tek owner | Save etkisi varsa roundtrip ve migration |
| Silah/reload | Cancel sınırları, ammo conservation, namlu engeli | Combat build rotası |
| AI/population | LOS kaybı, tek ölüm, logical/physical korunumu | Streaming ve yoğun rota |
| World streaming | Ready öncesi erişim, stale callback, tombstone | Tekrarlı dolaşım ve bellek |
| Save/schema | Eski fixture, başarısız load, atomic snapshot | Fault injection ve release upgrade |
| Time/sleep | Yakıt/ölüm/uyanış sınırı, normal tick karşılaştırması | Uzak cell/job persistence |
| UI/input | Focus, back, input leakage, hedef cihaz | İlk kullanım ve büyük metin |
| Sanat/ses | Benchmark uyumu, tehdit okunurluğu | Platform render ve performans |
| Metin/plan | İçerik, kimlik ve bağlantı kontrolü | Oyun testi gerekmez |
| Networking | Host validation, retry, concurrency, loss/jitter | Solo regression + gerçek farklı ağ |

Coverage yüzdesi keyfi kalite kapısı değildir. Bir test yalnız implementasyon formülünü kopyalamamalıdır. Negatif akış, public contract ve oyuncu etkisini doğrular. Test sayısı artışı sprint başarısı diye raporlanmaz.

## Kritik invariantlar

Item tam bir canonical owner taşır. Ammo yalnız gerçek ateş veya tanımlı discard ile azalır. Death ve objective reward bir kez işlenir. Logical ve physical nüfus geçişlerde korunur. Load bozuk orijinal dosyayı değiştirmez. CellReady restore+collision+nav hazır olmadan oluşmaz. Presentation seçeneği authority kuralını değiştirmez. Tek oyunculu oyun internet olmadan çalışır. Co-op geldiğinde bunlar gevşetilmez.

## Hata önem düzeyi

S0: veri kaybı, save corruption, yaygın crash. S1: ana ilerleme kilidi, kritik duplication, oynanışı imkânsız kılan hata. S2: workaround bulunan önemli sorun. S3: kozmetik/küçük pürüz. S4: iyileştirme önerisi. Öncelik önem, sıklık ve oyuncu maruziyetiyle belirlenir. Nadir save corruption kozmetik sayılmaz.

## Kapı sonuçları

PASS tüm zorunlu kanıtlar vardır. CONDITIONAL yalnız kritik olmayan kalan işler, sahip ve son tarih ile mümkündür. FAIL zorunlu davranış kırık. BLOCKED doğrulama erişimi yok. S0/S1 veya kritik NOT_RUN bulunan release kapısı PASS/CONDITIONAL olamaz. Faz dosyaları kendi özel kabulünü ekler. Planlanan tarihin gelmesi kapı sonucu değildir.

## Kanıt paketi

Önerilen repo yolu `docs/Implementation/Evidence/Sxxx/Dxxx/run-id/`. İçerik gereken kadar: build/commit/schema/catalog kimliği, ortam, test başlangıç koşulu, expected/actual, log, test çıktısı, screenshot/video ve açık sınırlama. Render işi için inventory logu gerekmez; item conservation için yalnız screenshot yeterli değildir. Dosya adında tarih ve run-id kullan, eski kanıtı sessizce üzerine yazma.

## Release adayı tanımı

Temiz checkout'tan üretilmiş değişmez artifact; tam ana yol; desteklenen save upgrade; gerçek hedef platform; seçilen input/dil kapsamı; performans capture; bilinen sorun listesi; rollback ve recovery provası. Test edilen hash ile yayınlanan hash aynı olmalı. Küçük son değişiklik yeni adaydır; etki alanına göre gerekli kabul tekrar edilir.

## Yayın yetkisi

S044 ve S060'taki yayın kartları koşulludur. Kullanıcı bu planı istedi diye mağaza yayını veya mesaj gönderimi yapılmaz. Önce somut aday, metin, kanıt ve risk hazırlanır; mevcut açık yetki yoksa o son eylem için karar alınır. Daha önce verilmiş yetki kapsamı yeterliyse aynı izin tekrar istenmez. Başarısız veya bekleyen platform işlemi yayınlandı diye kaydedilmez.
