# D160 — Kullanıcı kabul taslağı

Durum NOT_RUN. Bu listeyi ilk test kapısıyla birlikte çalıştırma; uygulama ve build kapıları tamamlanınca gerçek executable üzerinden uygulanacak. Eksik özellik işaretlenmeden PASS verilmez.

- [ ] Başlat / New Game / Continue; sonuç doğru ekran ve kayıt durumunu gösteriyor.
- [ ] FOV, sensitivity ve UI ölçeği değiştir; yeniden başlatınca korunuyor (otomatik profil 3/3 ve gerçek sahne 1/1 PASS, insan kontrolü açık).
- [ ] Envanter aç; oyun duraklatıldı bilgisi açık, başka modal üzerine binmiyor.
- [ ] Item seç / taşı / birleştir / split / drop; toplam doğru, başarısız işlem eşyayı kaldırmıyor (split UX bekliyor).
- [ ] Depoya bir/yığın transfer; dolu hedef ve kısmi sonuç açık, yanlış item seçili kalmıyor.
- [ ] Mouse ile kapat; aynı click silah ateşlemiyor. Bırakıp yeniden basınca çalışıyor.
- [ ] ESC geri yolu; tekrar ESC pause; Resume doğru ekranı getiriyor.
- [ ] Settings aç/kapat önceki ekranı koruyor (uygulama bekliyor).
- [ ] Pause sırasında hareket/AI/combat/world/araç ilerlemiyor; UI çalışıyor.
- [ ] Save → menu → load; ayarlar slot dışında korunuyor; bozuk kayıt yeni oyun başarısı gibi sunulmuyor.
- [ ] Alt-tab sırasında hareket/saldırı basılı tut; dönüşte release gerektiriyor.
- [ ] Ölüm ekranı kontrol açmıyor; menü ve mevcut checkpoint dönüşü anlaşılır.
- [ ] 1920×1080 ve büyük UI ölçeğinde Türkçe glifler / +%30 metin / renk dışı seçili-uyarı işaretleri okunuyor.

Her adım: beklenen/gerçek sonuç, gereksiz adım sayısı, hatalı click sayısı, blocker ve build kimliği kaydedilecek. Generic consume/treatment/equipment/corpse UI otoritesi olmayan eylemler kabul rotasına sahte adım olarak eklenmez.
