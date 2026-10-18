# Survival değer ve ekonomi sözleşmesi

BLD-D005 · BLD-P00-DEC-005 · PROPOSED · IMPLEMENTED (belge) / TEST_NOT_RUN.
Girdi: D005, LS-DOC-02 §3, LS-DOC-12 §1–3, LS-DOC-14 §1/5/6; [kapsam](SCOPE_MATRIX_AND_BUDGET.md). Karar sahibi Burak; uygulama BLD-S010/S015/S016–019/S022. Alternatif ücretsiz utility reddedildi: sefer ve risk seçimini yok eder. Revert: yeni faydalar kapatılır, mevcut loot/sığınak yolu sürer. Kart izi [handoff](BLD_S001_HANDOFF.md).

| Yapı / kapsam | Gerçek ihtiyaç | Maliyet girdisi (denge OPEN) | Fayda | Olumsuz maliyet / risk | Alternatif rota | Yeniden sefere çıkma nedeni |
|---|---|---|---|---|---|---|
| Mevcut claimed shelter; sonra küçük modüler barınak / SC-06 | Gece ve yağmurda toparlanma | Claim koşulları; sonra odun, bağlantı parçaları, zaman ve yapı bütçesi | Korumalı recovery, mekânsal ilerleme | Bakım, alan ve erişim kısıtı; tam dokunulmazlık yok | Mevcut binayı kullan, inşa etme | Su/yiyecek, alet ve bakım malzemesi |
| Storage / SC-07 | Taşıma yükünü azaltma, sefer stoklama | Mevcut S010 modül maliyeti; ileride sınırlı yeni container | Kaynak planlama, daha hafif sefer | Kapasite sonlu; uzak stash lojistik maliyetli | Sırt çantası ve kısa sefer | Eksik craft girdisi ve yeni loot |
| Workbench / SC-02/07 | Ham kaynağı yararlı malzemeye çevirme | Odun/bağlayıcı, alet, world time; recipe'ye göre güç | Güvenilir işleme ve tarif erişimi | Ses, zaman, gerekirse fuel; output full bekler | Hazır loot bul, daha az inşa | Alet bakım/parça, nadir girdi |
| Çit / SC-04 | Dönüş yaklaşımını okunur kılma, sınırlı engel | İşlenmiş odun + bağlayıcı; pozitif miktarlar | Yaklaşım yönünü düzenleme; entry corridor açık | Düşman için mutlak kalkan değil; görüş ve kendi hareketini kısıtlar | Açık kaçış yolu seç, riskli alanı dolaş | Yeni route ihtiyacı; onarım açılırsa bakım |
| Kapı/gate / SC-05 | Kontrollü giriş ve kaçış | Modüler parçalar ve bağlantılar | Aç/kapa erişim kontrolü | Sweep clearance, arıza/bakım; kilitli çıkış softlock yasak | Mevcut kapı / açık koridor | Parça ve condition bakımı |
| Su utility / SC-11 SHOULD | Güvenilir temiz su | Container, filtre/ısı girdisi ve zaman | Sonlu su işleme | Kontaminasyon/kaynak tükenmesi, kapasite | Garantili su/loot ve mevcut kaynatma | Filtre/fuel/dirty water tedariki |
| Güç/ışık / SC-11 SHOULD | Gece çalışma ve seçili istasyon | Fuel/charge ve ekipman | Operational station/light | Generator noise, presence, fuel drain; ışık görünürlüğü | Gündüz, sessiz battery veya güç istemeyen tarif | Yakıt ve bakım parçası |
| Yol güvenliği / SC-09 | Yüklü dönüşte açık rota | Yerleşim alanından feragat | Kapı/POI/spawn/nav erişimi korunur | “Her boş yere inşa” özgürlüğü azalır | Orman kenarı/ana yol iki yaklaşım | Değişen tehdit ve tükenen kaynaklar rota seçtirir |

SC-01 odunu sağlarken stamina/taşıma kapasitesi/tehdit seçimi yaratır; SC-03 planı anlaşılır kılar, ücretsiz güç vermez. SC-08 oyuncunun emeğini korur; bir ekonomi bonusu değildir. İlk playable sadece çitin anlamlı yaklaşım düzenlemesini ve mevcut storage/bench faydasını kanıtlar. Modüler ev, su ve güç aynı demo için gizli önkoşul değildir.

Hasat ve inşa sesi gerçekleşen gameplay olayından stable event ID ile üretilir; render/audio callback'i kaynak veya pressure artırmaz. Disturbance, Presence ve Depletion ayrı kalır; reload eski sesi yeniden yayınlamaz. Zombi öldürmek tek ilerleme kaynağı olmaz. Claim zombileri silmez; sessizlik depleted node'u doldurmaz. Işık/yağmur konfor etkileri mevcut sistemlere bağlanır, P00 yeni simülasyon vaat etmez.

Ekonomi fikstürü (yalnız gelecekte kabul için): 6 ham odun → 4 işlenmiş tahta; 2 tahta + 1 bağlayıcı → 1 çit. Alet tüketilmez, kullanım/condition politikası recipe'de açık olur. Bu sayılar mevcut item ID'si veya kanonik balance değildir; D011 katalog review'da ad/ID/revision ve maliyetler bağlanmadan runtime'a alınmaz. İnşa istemeyen oyuncunun temiz su ve ana objective erişimi bu girdilere bağımlı olamaz.

Kabul: owner her MUST satırını ihtiyaç→maliyet→fayda→alternatif→yeni sefer zinciriyle gözden geçirir. G4'te US-12 zorunlu, G5/G6'da modüler fayda ayrıca ölçülür. Ücretsiz sınırsız kaynak, efsanevi stat bonusu veya claim ile zombi silme kabul dışıdır.
