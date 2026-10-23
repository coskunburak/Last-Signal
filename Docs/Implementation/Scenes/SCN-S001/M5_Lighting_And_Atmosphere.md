# SCN-S001 M5 — sınırlı HDRP kalibrasyonu

**IMPLEMENTED, FULL WEATHER/INTERIOR ACCEPTANCE PENDING.** İlk editör önizlemesinde 100000 lux güneşe karşı yalnız dört postprocess bileşeni bulunan profil vardı; Exposure/VisualEnvironment eksikti. İzole volume artık 13 bileşenli, physically based sky, mevcut hava sisteminin volumetric clouds/fog ayarları, ACES, SSAO, contact shadows ve kontrollü SSR içeriyor.

Forest Prototype kalibrasyonundan gündüz EV12.8, kapalı hava EV11.5; gece başlangıç EV4.5. Normalize phase güneşi 42000 lux/unit, ay .5 lux/unit ile ölçeklenir. Preview 38864.7 lux / EV11.955. Sis taban 3 m, üst 32 m; contrast/bloom mevcut phase verisini kullanır, aday bloom düşürüldü. Kuru editör görünümünde 12 wet-only puddle renderer kapalı; mevcut TownAtmosphere yağmurda yönetmeye devam eder.

TownLightingProfile.usePhysicalHdrpLighting varsayılan false. Yalnız izole aday profili true. TownAtmosphere mevcut WorldClock'u örnekler; ek zaman/hava manager'ı yok. RenderPipelineAdapter fiziksel profil aktifken her postprocess örneğinde authored sky/fog değerlerini eski unit-scale default'a sıfırlamaz. Varsayılan çağrılar ve tarihsel profiller eski davranışta kalır.

Ortak üç kod dosyasının önceki sürümü ve hashleri LightingCodeChanges.json'da. Unity bu yeni API'leri derleyip candidate kalibrasyon komutunu çalıştırdı; bu EditMode/PlayMode test sonucu değildir. Gündüz/gece/yağmur, iç mekân ve pratik lambaların fiziksel pozlamayla okunabilirliği owner kontrolü bekler. Night/interior ayarları kabul edilmiş sayılmaz.
