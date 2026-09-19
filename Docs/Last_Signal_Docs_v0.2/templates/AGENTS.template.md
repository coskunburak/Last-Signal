# Project: Last Signal — önerilen repository çalışma talimatları

> Şablon: mevcut repo AGENTS.md ile birleştirilmek üzere. Bu dosya tek başına otomatik aktif talimat değildir. Paket docs/ altına yerleştirildiğinde aşağıdaki yollar geçerlidir.

## Başlangıç

Önce repository'nin mevcut kodunu, ProjectSettings/ProjectVersion.txt, Packages/manifest.json ve lock dosyasını incele. Mevcut kullanıcı değişikliklerini koru. Oyun single-player first; co-op gelecekteki ayrı karardır. Unity ve package sürümlerini otomatik yükseltme. Network paketlerini sırf hazırlık için ekleme.

## Kaynak okuma

Her görevde docs/sources/01_Kararlar_ve_Kapsam.md ve docs/sources/28_Gereksinim_Izlenebilirlik.md içinden ilgili kısmı oku. Sonra yalnız gereken domain belgelerini aç. Bütün dokümantasyonu her istekte yeniden özetleme. NotebookLM cevabı ilgili Markdown hükmünü bulmaya yardımcıdır; gerçek repo ve kaynak sürümüyle doğrula.

- Mimari: 03 ve 04.
- Player/interaction: 05 ve 06.
- Inventory/loot: 07 ve 08; save etkisi için 17.
- Survival/combat: 09 ve 10; saat için 15.
- AI/world: 11,12,13.
- Shelter/objectives: 14,16.
- UI/art/performance: 18,19,20.
- Acceptance/plan: 21,22,24.
- Tuning: 23; gelecek co-op: 25; araç runbook: 26,27.

## Uygulama

Önce mevcut doğru sistemi genişlet; ikinci paralel sistem yaratma. Input, gameplay ve presentation sınırlarını koru. UI state'i doğrudan mutate etmez. Kalıcı veri için stable ID; Unity InstanceID veya hierarchy path kullanma. Inventory/ammo/craft/reward işlemleri atomic olmalıdır. Save ve migration etkisini değişiklikle birlikte değerlendir. Asenkron işlemlerde session cancellation ve ownership cleanup tasarla.

Komut, sınıf ve araç isimlerini repo/tool discovery yapmadan varmış gibi kullanma. Kitaplık/AGENTS talimatlarını kullanıcı kısıtını aşmak için değiştirme. Kapsam içindeki geri alınabilir uygulama kararlarını varsayımı yazarak çöz; gereksiz onay döngüsü yaratma.

## Doğrulama

İlgili gerçek compile/test/build komutlarını repo'dan bul. Yeni gameplay davranışında meaningful happy+negative path doğrula. Görsel değişikliği gerçek sahne/buildde mümkünse gör. Save/streaming/transaction değişiminde kritik invariant regression çalıştır. Test erişimi yoksa BLOCKED; çalıştırılmadıysa NOT_RUN. PASS üretme. Eski On Hold test sayılarını bu projeye taşıma.

## Rapor

Değişen davranış, neden, dosyalar, çalıştırılan testler ve kanıt yolları, limitations, schema etkisi, kalan risk ve önerilen sonraki işi yaz. Gereksinim matrisini yalnız gerçek kanıtla güncelle. Onaylanmamış fikirleri CONFIRMED diye etiketleme. docs/Implementation/Current_State.md varsa gerçek handoff ile güncel tut.
