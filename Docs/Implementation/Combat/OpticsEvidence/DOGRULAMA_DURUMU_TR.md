# Optik sistem doğrulama durumu — 2 Ekim 2026

Testleri kullanıcı açık Unity Editor üzerinden çalıştırdı. Aşağıdaki sonuçlar XML dosyalarından doğrulandı.

| Tur | Sonuç | Kanıt |
| --- | --- | --- |
| Tüm EditMode | 439/439 geçti | `20261002-165516-7bfef1-edit/results.xml` |
| Dar optik PlayMode | 3/3 geçti | `20261002-165537-671c0e-optics/results.xml` |
| Tüm PlayMode | 196/196 geçti; 543,865 saniye | `20261002-171554-0b2a28-play/results.xml` |

Üç optik test tam PlayMode toplamının içindedir; toplam test sayısına yeniden eklenmez. Önceki başarısız turlar geçmiş kanıt olarak korunur.

## Doğrulanan davranışlar

- ADS sırasında optik kamera ve Render Texture açılır; ADS çevrimleri aynı Render Texture'ı tekrar kullanır.
- Silah kaldırılınca optik kamera ve Render Texture serbest bırakılır.
- Kabul edilen atış tek mermi tüketir; reload sırasında optik görüntü kapanır ve tamamlandığında geri gelir.
- EditMode varlık, geometri, optik FOV ve kurulum kontrolleri geçer.
- InputTestFixture sınırlarında etkin giriş eylemlerinin kapatılmasından sonraki tam PlayMode turu hatasızdır.

## Açık kabul adımları

- Gerçek Game View'da hip/ADS geçişi, retikül, göz hizası ve farklı mesafelerde hedef görünürlüğü görsel olarak değerlendirilmeli.
- ADS görüntüsünün siyah iç geometri tarafından kapanmadığı ve yakın çevre nesnelerinin doğru örtüşme verdiği oyun içi görüntülerle doğrulanmalı.
- Aynı sahne ve çözünürlükte hip/ADS CPU ve GPU kare süreleri ölçülmeli. Kaynak tekrar kullanımı testinin geçmesi GPU performans bütçesini tek başına doğrulamaz.
- Hedef platform geliştirme derlemesinde görsel ve performans kabulü henüz yapılmadı.

Bu kayıt otomatik test kabulünü belgeler; görsel kalite veya hedef cihaz performansı için tamamlanmış üretim onayı değildir.
