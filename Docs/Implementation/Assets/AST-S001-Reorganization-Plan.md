# AST-S001 — HDRP öncelikli mimari

**MOVE_PLAN_READY_WITH_BLOCKERS**. [72 satırlık kesin kaynak/hedef manifesti](AST-S001-MovePlan.json) tüm satırlarda `approved: false` içerir. Bunlar koşullu öneridir; envanter taşınmış gibi gösterilmez. Mevcut fiziksel hiyerarşi korunmuştur; katalog pipeline ve kullanım alanlarıyla HDRP kaynaklarını aranabilir hale getirir.

```text
Assets/ThirdParty/
  00_Active_HDRP/{Environment,Vegetation,Buildings,Characters,Weapons,Vehicles,VFX,Weather,Terrain}/<intact package>
  10_Pipeline_Agnostic/{Audio,Animation,Models,UI,Tools}/<intact package>
  20_HDRP_Migration_Review/<intact package>
  30_Legacy_URP_Review/<confirmed pipeline-specific package>
  40_Unused_Review/<owner-reviewed candidate>
```

Bunlar plan şemasıdır; boş klasörler oluşturulmadı. UNKNOWN içeriği 20 altında önerilmesi taşıma izni veya bozukluk kararı değildir. Native/converted ayrımı ve görsel kabul tamamlanınca ilgili bütün paket 00'a aday olur. 90_Protected_DoNotMove fiziksel karantina değildir; korunan paketler yerinde kalır ve katalogda işaretlenir. 30/40 otomatik taşınmaz. Karma dağıtımın HDRP/URP/Built-in alt ağaçları bölünmez. Vendor nesneleri yeniden adlandırılmaz.

**Uygulanan araç:** `Assets/LastSignal/Scripts/Editor/ThirdPartyAssetAudit.cs`. Birinci menü salt okunur dry-run; ikinci menü dependency/forest raporu; üçüncü menü yalnız sabit UI adayı için önkoşullu MoveAsset workflow. Bugünkü UI dependency guard'ı başarısız olur, dolayısıyla etkin küçük batch yoktur. Bu korumayı kaldırmak ayrı review gerektirir. Yeni paketler kaynak koduna kontrolsüz parametre olarak verilemez.

Move workflow kaynak meta/GUID, tüm ThirdParty pairing/GUID, case collision, dış referans, sabit yol ve dosya uzantısı kontrolü yapar. Doğrulanmış paket yedeği ve kalıcı MOVE_INTENT journal ardından hedef klasörleri AssetDatabase ile oluşturur, ValidateMoveAsset/MoveAsset uygular; byte/GUID ve tüm proje sahnelerinin GUID dependency kapanışını karşılaştırır. Hata halinde ters MoveAsset dener; rollback doğrulaması başarısızsa receipt hatayı saklar. Oluşturulan boş klasörler otomatik silinmez. Başarılı MoveAsset tek başına ACCEPTED üretmez. Editor çökmesi için receipt+backup yolu mevcuttur; dosya sistemiyle canlı Unity altında taşıma yapılmamalıdır.

## Kalıcı kabul politikası

Her import öncesi creator/source/version/license/attribution ve indirme kanıtı kaydedilsin. Kaynak dosya + metalar hash manifestli kurtarma kapsamında olsun. Paket sınırları, Editor/Resources/native plugin/asmdef ve yol stringleri taransın. HDRP destek kanıtı import ve materyal/ShaderGraph kontrolüyle, görünüm owner sahne kontrolüyle doğrulansın. Yeni proje adaptasyonları `Assets/LastSignal/Art/HDRP_Integrations/` altında küçük materyal/prefab türevleri olsun; mevcut çalışan entegrasyonlar sırf isim için taşınmasın. HDRP-only yeni rendering import politikası URP'yi yeniden kurmayı içermez. Lisans belirsizliği üretim dağıtımını bloke eder; silme yetkisi vermez.

## Devam aşamasında tamamlanan manifest dry-run

`ThirdPartyMoveManifestAudit.cs` schemaVersion=1 planını ve hash korumalı mevcut baseline manifestini okur. Menü 4 tüm plan için dosya üyeliği, meta/GUID, case/ancestor/destination çakışması, güncel serialized tüketiciler, yol/name stringleri ve özel klasör semantiğini kontrol eder. Byte backup kontrolünü NOT_RUN olarak açıkça gösterir. Menü 5 yalnız Project penceresinde seçilen paket için mevcut kaynak+checkpoint SHA-256 doğrular. Varsayılan eski checkpoint yolu Windows üzerinde yoksa BLOCKED beklenir; kanıt olmadan yol değiştirmeyin. Dry-run pencereye yazar; export düğmesi ayrı olarak yeni Evidence raporu oluşturur. Tüm satırlar BLOCKED, approved=false ve global executionApproved=false; mevcut executor da bu kapılara bağlandı. Mass execute yoktur. Menü 4/5 bu oturumda çalıştırılmadı.
