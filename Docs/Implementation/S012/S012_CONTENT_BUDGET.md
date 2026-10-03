# D111 içerik bütçesi

Aday: `Assets/LastSignal/Scenes/IntegratedGraybox.unity`. Resident x=[-400,0], z=[-200,200], 1 unit=1 m. Beş resident bina; mevcut iki portal hücresindeki kapalı yapılar da sayılırsa toplam yedi. Ek görsel bina yok. Yeni dış asset/lisans yok; Unity primitive, TextMesh ve mevcut proje materyalleri/prefabları kullanıldı. Vendor assetleri değiştirilmedi.

| POI | Rol / sistem | Asset | Kalıcılık | Tahmini ek maliyet | Kabul |
|---|---|---|---|---|---|
| cabin (-360,-140) | Başlangıç, radyo, recovery, depolama, modül/craft, dinlenme | Mevcut cabin + ShelterLoop/Site | mevcut shelter ID, storage, job, radio receipt | 1–2 saat entegrasyon | S012 shelter output, checkpoint |
| market (-160,-40) | Kaynak keşfi | Mevcut Kitchen loot masası + açık çift girişli graybox | s006.kitchen.* aynı kimlik | 1 saat + traversal | Loot placement, iki rota |
| clinic (-280,35) | Alternatif yolda kaynak ve yağmurdan korunma | Clinic loot + RainRoof | s006.clinic.* | 1 saat | Crouch/kapı/roof kontrolü |
| maintenance (-100,110) | Tek sigorta, wrench, 20 scrap, 4 fuel; isteğe bağlı hücre seferine erişim | S010 profiller + S011 fuse | relay.fuse-source.v1 tek, workshop kimlikleri korunur | 1–2 saat | Clue/fuse-first, dropped recovery |
| security (-190,-95) | Mühimmat ve okunabilir erken tehdit | Security loot + tek mevcut ZombieEncounter | mevcut encounter kimliği; ölüm snapshotı | 1–2 saat | Rifle/melee, retreat, save |
| relay (-40,-130), bina sayılmaz | Üç saniye repair, radyo dönüşü | Mevcut RelayPoint | overlook.relay.v1; tek reward receipt | 0.5–1 saat | Interrupted repair, exactly-once |
| cell:1:0 / cell:2:0 | Mevcut isteğe bağlı risk/mühimmat seferi, streaming/population | Mevcut Cell1/Cell2 prefabları | mevcut cell/door/loot/enemy kimlikleri | 1–2 saat regresyon | CellReady, unload/save/load |

Tahminler planlama değeridir, harcanmış emek değildir. S012 toplam 40–60 saat canonical başlangıç kutusu gerçek ölçüm yerine kullanılamaz.

Zorunlu bağlantılar: cabin çıkışı → direct veya western rota → maintenance → güneydoğu röle → cabin dönüşü. Market ve clinic kısa sapmalarla aynı rotaya bağlanır. Açık arazide kaçış vardır; yeni lock/key/distraction mekaniği yok. İki görüş bariyeri alternatif yaklaşımın görünürlüğünü değiştirir. Bunlar oynanış engelleridir, dekor bütçesi değildir.

Katalogda su, yiyecek/medical bulunması onları tüketilebilir yapmaz. Yeni hunger/treatment sistemi bu sprintte uydurulmadı. Fiilen desteklenen survival: sağlık, stamina, rain/wetness, sığınakta uyku ile iyileşme. İnsan koşusunda bu sınır açıkça değerlendirilmelidir.
