# S020 ekonomi — statik olasılık hesabı

`P(item) = (1 − emptyBasisPoints/10000) × weight / totalWeight`. Miktar, çekilişten sonra ayrı seçilir. Bunlar hesaplamadır; Unity test sonucu veya oyun denge kabulü değildir.

| Profil | Eşya | Önce | Sonra |
|---|---|---:|---:|
| Kitchen | drink.water | 26.000% | 26.000% |
| Kitchen | food.canned | 32.500% | 19.500% |
| Kitchen | food.crackers | 0.000% | 13.000% |
| Kitchen | drink.soda | 0.000% | 6.500% |
| Kitchen | medical.bandage | 6.500% | 6.500% |
| Clinic | medical.bandage | 67.500% | 67.500% |
| Clinic | drink.water | 7.500% | 7.500% |
| Clinic | medical.pressure-dressing | 0.000% | 5.000% |
| Workshop | material.scrap | 52.000% | 52.000% |
| Workshop | tool.wrench | 13.000% | 6.500% |
| Workshop | material.cloth | 0.000% | 4.550% |
| Workshop | tool.sewing-kit | 0.000% | 1.950% |

Mutfak su %26 ve bandaj %6,5 korunur. Klinik su %7,5 ve bandaj %67,5 korunur. Workshop hurda %52 korunur. Yeni kayıt S013Cabin sahnesinde mutfak.0 su, klinik.0 bandaj ve atölye.4 kumaş garantilidir; bunlar rastgele çekiliş ortalamalarından ayrı değerlendirilir. Mevcut garantili hurda/yakıt/anahtar/röle sigortası referansları korunur.

S013Cabin: 4 rastgele mutfak noktası + 1 garanti; 4 rastgele klinik + 1 garanti; 1 rastgele workshop + 4 garantili workshop kaynağı (hurda/yakıt/anahtar/kumaş). Fiziksel yerleşim/erişim yine PlayMode ve manuel kabul gerektirir. Kötü seed bütün isteğe bağlı kaynakları boş bırakabilir; sefer çantasının yolu garantili kumaş + mevcut garantili hurda/anahtar + yeni dikiş seti tarifiyle korunur.

Yeni DAG: scrap → sewing-kit (2:1, wrench araç); cloth → rag (2:1); cloth → expedition-pack (30:1, sewing-kit araç). Eski scrap → rifle-ammo (2:10) korunur. Dönüşüm dönüş yolu yok; araçlar çoğaltılmaz/tüketilmez. Dikiş seti craftı hurda tüketir. İptal bir kez tam girdi iadesi; output ve refund yer yoksa job escrow içinde kalır.

Daha nadir soda sudan az hidratasyon sağlar; kraker porsiyonu küçük ve yığın başına toplam besin konserveden azdır. Hızlı pansuman daha az stack kapasitesine sahiptir. Sefer çantası kapasite karşılığında toparlanmayı azaltır. Kütle genel taşıma limiti olarak kullanılmadığından gram farkı tek başına kanıtlanmış taşıma dengesi değildir.
