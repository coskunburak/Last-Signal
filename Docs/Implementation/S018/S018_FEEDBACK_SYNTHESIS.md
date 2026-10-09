# S018 D175 — boş sentez şablonu

**NOT_RUN — gerçek dış oyuncu kaydı teslim edilmedi.** Bu dosyada bulgu veya oyuncu sözü yok. Gerçek tester/session adedi, completion ve blocker istatistikleri henüz hesaplanamaz; unknown değerler sıfır değildir. T01–T08 sonuçları uydurulmaz.

## Kanıt kapsamı

Candidate / platform artifacts / source digest: PENDING. Dahil edilen session paths, gerçek kişi sayısı, survival/FPS segmentleri, cihaz/OS, first-time/repeat, eksik/veri dışlama gerekçeleri: PENDING. Farklı adayları, assisted/unaided veya fresh/repeat sonuçlarını tek toplamda saklamayın. İlk beş oyuncunun yön gösterici oranları ve toplam 5–8 kişinin sonuçları ayrı gösterilir.

| Observation ID | Candidate / tester / timestamp / evidence | Oyuncu sözü | Gözlenen davranış | Altta yatan ihtiyaç (hipotez) | Olası kök neden | Çözüm seçenekleri |
|---|---|---|---|---|---|---|

Oyuncu sözü ile gözlem birbirinin yerine geçmez. Önerilen özellik otomatik backlog taahhüdü değildir. İhtiyaç/kök neden kanıtla doğrulanana kadar hipotez olarak kalır. Oyuncu söylemediyse tırnak içine söz üretmeyin.

| Cluster / issue ID | Severity | Etkilenen gerçek kişi / maruz kalan kişi | Olay / fırsat sayısı | Segment / confounder | Tekrar kanıtları | Güven / karşı kanıt | Öncelik ve gerekçe |
|---|---|---|---|---|---|---|---|

S0 crash/veri kaybı/save corruption; S1 ana progression blocker/kritik duplication; diğerleri etki ve workaround'a göre. Frequency ile exposure ayrı; hiç karşılaşmayan tester başarı paydasına eklenmez. Tek ciddi S0 düşük sıklıkla düşürülmez. Cosmetic kusur sırf görünür olduğu için S1 olmaz.

S2: kullanılabilir workaround'u olan önemli aksama; S3: küçük/görsel sorun; S4: iyileştirme önerisi. Öncelik önce S0/S1, sonra etkilenen kişi/maruz kalan kişi, tekrarlayan başarısız deneme ve core loop'a etkidir. Eşitlikte kanıt güveni ve küçük değişiklikle beklenen yarar değerlendirilir. Keyfi ağırlıklı bir puan gerçek riski gizlemek için kullanılmaz. Top üç seçimi ve seçilmeyen önemli bulgunun erteleme gerekçesi birlikte kaydedilir.

Analiz kuralları: session ID tekildir; aynı kişinin tekrar koşusu yeni kişi sayılmaz. Withdrawn/eksik kayıtlar başarı paydasına eklenmez; dışlama nedeni görünür tutulur. Sadece daha iyi sonucu veren bir retry seçilmez. Farklı build hashleri tek başarı oranında birleştirilmez. Özgür metinden AI tarafından “ölüm sebebini anladı” sonucu türetilmez; observer'ın gerçek alıntı ve sınıflaması gerekir. n<5 veya küçük segment örnekleri kendi n'leriyle ve sınırlamayla raporlanır; p-değeri/pazar tahmini üretilmez.

## Sonuçlar — veri gelince doldurulacak

Onboarding/sefer/tam loop için unaided, assisted, incomplete ve not observed sayıları; tüm müdahaleler; ilk su/hedef süreleri ve gözlem sınırı; inventory dwell+karışıklık; threat/damage/death; ölüm nedeni ve sonraki eylem anlayışı; blocker, ciddi UX, performans/input/FOV confounder'ları; end-state dağılımı. Ham n ve bireysel süreler saklanır, erişilmeyen hedefler 0 saniye yazılmaz.

Pozitif anlar, en sıkıcı anlar, devam/bırakma nedenleri, beklenti farkı ve gönüllü tekrar isteği ayrı kanıtla doldurulur. “Fun” tek ortalama skora veya bir uzmanın başarısına indirgenmez. Negatif gözlem segment uyumsuzluğu bahanesiyle silinmez.

D176 için en çok üç öncelikli issue seçimi: PENDING. [Fix traceability](S018_FIX_TRACEABILITY.md) içine gerçek gözlem bağlantısı olmadan değişiklik eklenmez. Retest karşılaştırması: PENDING; yeni candidate/teknik kabul, aynı ölçütler, fresh/repeat ayrımı ve öğrenme etkisi gerekir.

## Veri işleme teslimi

`Tools/s018-evidence.py` uygulandı; işlevsel doğrulama NOT_RUN. [Kitteki alan sözleşmesi](S018_PLAYTEST_KIT.md#offline-kayıt-aracı) ve [son doğrulama sırası](S018_VERIFICATION_HANDOFF.md) izlenir. Araç aday/hash/segment/fresh-repeat gruplarını ve ham kayıtları korur; cluster, ihtiyaç hipotezi, top üç seçimi, fun veya iyileşme iddiasını otomatik üretmez. Bu insan incelemesi gerçek kayıtlar gelince yapılır. Mevcut gerçek kayıt sayısı 0, bu nedenle gerçek summary üretilmedi.

## QA2 Round 1 araç sonucu — 2026-10-09

Kullanıcı tarafından çalıştırılan offline araç unit testleri 13/13 PASS, exit=0; [kanıt](Evidence/qa2-round1-PwLDYL/terminal.txt). Önceki araç NOT_RUN ifadeleri hazırlık tarihçesidir. Gerçek oyuncu kayıtları/sentez hâlâ NOT_RUN; sentetik unit fixture'lar insan verisi değildir.
