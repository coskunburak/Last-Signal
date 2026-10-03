# D112–D115 aday rota

Başlangıç cabin. Önce radyo notu (J ile kapat), E ile sığınaktan ayrıl. Direct rota security yakınından markete, ardından maintenance'a ilerler. Western rota clinic üzerinden iki görüş bariyerinin batısından dolaşır; daha uzun ve yaklaşım görüşü kısmen kapalıdır. Maintenance'da gerçek pickup ile tek fuse, wrench, scrap ve fuel alınır. Röle güneydoğudadır. Repair sonrası cabin radyosuna dönülür. Kaynaklarla modüller kurulur, depolama ve powered rifle-ammo üretimi yapılır; mevcut menüden save/menu/load uygulanır.

Alternatif sıra: radyo okunmadan önce fuse alınabilir; repair için notun sonradan okunması gerekir. Bilgi, item sahipliği ve repair receipt aynı RelayProgression otoritesindedir.

Beş yapının dördü iki geniş açık girişe sahip; cabin mevcut kısa mesafeli entry terminalini korur. Mevcut portal seferleri açıkça konum geçişidir; bunlar yürünmüş mesafe sayılmaz. Resident alanın doğu duvarı yüklenmemiş hücrelere yürüyerek erişimi önler. Cell dönüş terminali tarihsel resident dönüş noktasına çıkar; bu konum da yeni alan içindedir, fakat bakım girişine yakın değildir.

`S012Acceptance.Traverse` gerçek CharacterController ve FirstPersonMotor.Simulate kullanır. Başlangıç konumlandırma fixture'dır; sonraki segmentler collision üzerinden ilerler. Hızlandırılmış frame-batched simülasyon saniyeleri insan oturum süresi değildir. Yürüme/sprint/crouch sonuçları run kanıtında saklanır. Stamina sprint zamanını sınırlar. AI NavMesh path kontrolü ayrıca yapılır; tek başına path sonucu gerçek chase davranışının yerine geçmez.

30–45 dakika henüz ölçülmedi. Yol kıvrımı süreyi tutturmak amacıyla artırılmayacak. Gerçek oyuncu koşusunda keşif, inventory, encounter, dönüş ve crafting süresi ayrı kaydedilmeli; kısa kalan rota dürüstçe raporlanmalı.
