# S020 manuel gameplay kabulü — NOT_RUN

Gerçek sahne: `Assets/LastSignal/Scenes/Production/S013Cabin.unity`. Otomatik testlerden sonra açın. Kişisel kaydınızı test için kullanmayın: Play başlamadan SaveSession `saveSlot` alanını `s020-manual` yapın; bu değişikliği production sahnesine kaydetmeyin. Sahneyi kapatırken bu test ayarını discard edin. Development build’in normal kayıt yolunu kullanmadan önce kişisel kaydı yedekleyin.

Karşılaştırma için miktar, HP, hydration/nutrition, kanama ve slot sayısını önce/sonra kaydedin. Yeni itemler `Assets/LastSignal/Prefabs/Items/` altında kendi adıyla bulunur. Random loot gerektirmeden sunumu incelemek için **Play sırasında** prefabı sahneye oyuncunun önüne bırakıp normal etkileşimle alın; test objelerini sahneye kaydetmeyin. Bu, dünya pickup → inventory → use akışını korur. Actor pozisyonu ve runtime state production kabul kanıtına ayrıca yazılmalıdır.

| İşlev | Hazırlık / oyuncu eylemi | Beklenen UI/runtime ve inventory sonucu | Ekonomi/sağlık sonucu / hata belirtisi |
|---|---|---|---|
| Başlangıç su/tedavi | Yeni oturum; Hierarchy’de `s006.kitchen.0` ve `s006.clinic.0` stable ID’li mevcut noktalar | Sırasıyla 1 su ve 1 bandaj; normal etkileşimle alınır, dünya kaybolur | Aynı oturum/load ile yeniden oluşmamalı; Blocked placement warning hata |
| Yemek | Bir süre oyun dünyasında zaman geçirerek besin <100 yapın; krakeri seçip Kullan | İkon, 180 g/adet, maxStack 4 ve 20 besin açıklaması; seçilen yığın −1 | Besin +20, 100’e clamp; su/HP değişmez. Tam tokken tüketim reddedilir |
| Su / soda | Hydration <100; su veya soda seçip Kullan | Su 530 g/+40, soda 350 g/+25; seçilen stack −1 | Besin/HP değişmez; dolu statta eşya korunur |
| Kanama / bez sargı | Gerçek yakın dövüş hasarıyla kanama oluşturun; bez sargı kullanıp 5 sn hareketsiz kalın | Tedavi geri bildirimi; sonunda 1 sargı tüketilir | Kanama bir kademe azalır; HP heal yok. Hareket/hasar/pause tüketmeden iptal etmeli |
| Basınçlı pansuman | Kanama varken pansuman kullanın | 1 sn uygulama, 1 item tüketimi; kalan kanama geri bildirimi | Tüm kanama durur; HP/nutrition/hydration heal yok. Kanama yoksa reddedilmeli |
| Çanta takas | Eski saha çantası ve yeni sefer çantasını sırayla takın | Saha +8, sefer +12 slot; eski çanta inventory’ye tek adet döner; UI slot sayısı eşleşir | HP korunur. Sefer çantasında stamina toparlanması %15 düşük; çıkarınca normale dönmeli |
| Dolu çanta küçültme | Sefer çantası takılıyken küçük çantaya sığmayacak sayıda dolu stack oluşturun; küçük çantayı takmayı/çantayı çıkarmayı deneyin | İşlem reddedilir; çanta/slot/miktar ve stackler değişmez | Sessiz item kaybı veya tekrar denemeyle çoğalma hata |
| Kütle | Envanterde stack böl/taşı/birleştir/tüket; çanta tak/çıkar | Toplam gram gerçek miktara göre değişir, takılı çanta dahil; capacity’nin yuva temelli olduğu yazılı | Gram limiti/hareket bantları uygulanıyor diye kabul edilmez |
| Tezgâh kurulumu | Kabin tehdidini temizleyin; claim, storage ve workbench modüllerini mevcut soketlerden kurun | Scrap mevcut maliyetlerle tüketilir, depo erişimi açılır | Yeni sistem veya bedava modül beklenmez |
| Bez tarifi | Depoya 2 kumaş koyun; tezgâhta Next recipe → Bez; Start craft; paneli kapatıp 60 dünya sn ilerletin | Başlangıçta 2 kumaş escrow’a gider; Collect ile 1 bez sargı | Elektrik gerekmez; tekrar Collect output üretmez |
| Dikiş seti | Depoda 2 scrap + 1 wrench; Next recipe → Dikiş Seti; 300 dünya sn | 2 scrap tüketilir, 1 set üretilir; wrench kalır | Araç yoksa tüketimsiz reddetmeli |
| Çanta üretimi | Garantili `s006.workshop.4` kaynağından kumaşı alın; depoda 30 kumaş + dikiş seti; 900 dünya sn | 30 kumaş tüketilir, 1 çanta üretilir; dikiş seti kalır | Elektrik gerekmez. Araç/malzeme eksikse hiçbir girdi harcanmaz |
| İptal ve dolu depo | İş başlat → Cancel; depoyu doldur → Collect; yer aç → Collect; tekrar Collect | Yer yokken pending refund korunur; sonra girdinin tamamı bir kez döner | Output/refund kaybı/çift ödeme hata |
| Eski mühimmat | Default recipe seçin, eski yakıt/jeneratör ve wrench şartıyla üretin | 2 scrap → 10 ammo, 600 dünya sn; elektrik yokken ilerleme durur | Yeni tarifler eski recipe revision/maliyetini değiştirmemeli |
| Sunum | 17 itemin adı/ikon/açıklaması, yeni prefab etkileşim adı ve collider erişimini inceleyin | Yanlış ikon/ref, pembe material, kesilmiş metin veya slotta görünmeyen ikon olmamalı | Yeni dünya modelleri provisional listesinde; realistic survival sanat kabulü ayrıca açık |
| Save/load | Yaralı/kısmen aç ve yeni çanta takılıyken, yeni stackler ve running/pending recipe ile izole slota save; menü/load; iki kez tekrar | Miktar, çanta, kapasite, recipeId/progress, kanama ve HP korunur; starter pack eklenmez | Tüketilmiş loot geri dönmez; pending job doğru recipe ile bir kez output/refund verir |

Ekran görüntüsü/video, kullanılan seed, başlangıç/save adı ve gözlenen sonuçları S020 Evidence altındaki yeni manuel koşu klasörüne koyun. Şüpheli sonucu “PASS” diye genellemeyin. Otomatik testler gerçek oyuncu akışı, sanat okunurluğu ve denge kabulünün yerine geçmez.
