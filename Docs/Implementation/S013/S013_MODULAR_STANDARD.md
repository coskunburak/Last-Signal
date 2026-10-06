# S013 kırsal kabin modül standardı

Birim metre. Yerleşim grid'i 1m, ana panel adımı2m; detay hizası .05m. Prefab kökü scale1, rotation0. Duvar pivotu sol alt merkez çizgisi: x0→2, y0→3.2. Duvar core .22m; dış board .038m, derz .008m. 1m filler giriş düzenini tamamlar. Köşeler ayrı iç/dış post parçalarıdır.

Kapı core açıklığı1.30×2.40m; jamb yüzeyiyle net görsel açıklık1.20m. Yaprak1.20×2.30m, menteşe tarafı pivotu x0. Gerçek CharacterController1.8m yüksek/.3m radius; crouch1.2m. PlayMode testi her iki duruşla üretim motorunu geçirir. NavMeshSurface agentType ölçüleri runtime alınarak aynı açıklıkta canlı agent geçişi sınanır; uydurma AI boyutu kullanılmaz.

Pencere açıklığı1.2×1.5m, eşik1.1m. Cam mevcut güvenli shelter duvarıyla uyumlu opak kirli yüzeydir; açık geçit değildir. Floor2×2m, köşe pivotu(0,0,0), üst yüzey0; collision top-.03m. Sahne döşemesi mevcut zemin collider'ına .018m görsel offset ile oturur. Roof2×3.8m, eğim28°, mahya4.8m; proje-owned standing seams. Üretim assembly'de dekor colliders kapalı, mevcut kapalı shelter sınırı otoriteyi korur. Reusable duvar/floor prefablarda collision aktiftir.

Kapalı cabin kapısı mevcut Leave/Return etkileşimleriyle çalışır; serbest geçilebilen açık kapı gibi sunulmaz. Bağımsız doorway modülü doğrudan motor/AI geçişine uygundur. Socket ID'leri değişmez; görsel service plates ve mobilya çevresine taşınır. Etkileşimler mevcut HUD/raycast sistemindedir.

Malzeme başına birleştirilmiş dekor meshleri kullanılır. Tüm vendor dönüşümleri ayrı `Art/S013/Production` türevleridir. Proje-owned surface texelleri512px/2m=256px/m, mipmap, repeat, aniso4. Mesh normal/UV/tangent kanal testleri yapılır. Structural collider primitive; bitki yapraklarına mesh collider yok. Ağaç capsule, kaya sade box. Çalı/çim dekoratif ve çarpışmasız.

`FloorAndWallUseContinuousTwoMetreSnap` collider uçlarını0 ve2 doğrular; son assembly birleşimleri sabit kameralarla incelenir. Bu sayısal test tek başına tüm görsel/light-leak kabulü değildir.
