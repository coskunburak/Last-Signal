# S016 — Movement fixture odaklı doğrulama devri

Son tam PlayMode `Evidence/20261007T235356-149532Z-regression-play/`: 296/297, failed=1, skipped=0, exitCode=2, kaynak değişimi yok. Slot-rebind geçti; kalan hata MouseAndCrouchActionsReachCameraAndCapsule cihaz release assertion'ı.

Unity Editor **kapalıyken** tek komut:

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/s016-verify.py focused-movement
```

Assembly `LastSignal.PlayModeTests`, filter `LastSignal.Tests.MovementAcceptanceTests`; beklenen **17/17 PASS**, failed/skipped=0, exitCode=0, kaynak değişimi yok. Fixture artık InputTestFixture ile manager/clock değiştirmez; mevcut manager üzerinde geçici test cihazları kullanır. Sınıfın input, lifecycle, parkour ve hareket kontrolleri bu fixture değişikliğini birlikte doğrular.

Tek Unity süreci; tam regresyon/build başlatılmaz. Terminal özeti ve RUN yolunu gönder. Hata varsa yalnız ilgili test incelenir. Tek başına geçiş, önceki tam regresyonu PASS yapmaz.

Tests: NOT_RUN — manual execution reserved for the user.
