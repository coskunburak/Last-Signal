# Mimari değişiklik ve geçiş kuralları

**LS-BLD v2 governance · PROPOSED · owner acceptance pending**

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağımlılık ilkeleri
- S001/S002 tasarım onayı olmadan GDD dışına full free building kodu uygulanmaz.
- S003/S004 harvest, S005 crafting malzeme stoğu için test fixture sağlar; yalnız fixture dependency yüzünden UI/kod bekletilmez.
- S007 placement validator, S008 ghost preview ve S009 commit aynı field/enum sözleşmesini paylaşır.
- S010 G4 vertical slice P05 large modular home expansion için go/no-go’dur.
- S020–S022 ekonomi ve persistence testleri P08 optimizasyon öncesi baseline verir.
- FirstTownCore_VisualSlice visual rebuilding ayrı stream; sahne hash/promote guardlarına dokunulmaz.

## Değişiklik kapısı
- Yeni sınıf/dosya, data version, SaveCodec, prefab GUID, render/material veya nav affecting operation için ADR gerekli olup olmadığı belirtilir.
- Breaking schema: fixture, migration, backup/recovery ve testlerin kullanıcı tarafından çalıştırılması.
- Scope replan: kapsam kesilecekse tam item ID ve downstream docs etkisi kayıtlanır.

## Uygulama zamanı
Bu sözleşme ilgili phase/sprint/day belgelerinde referans alınır. Gerçek kodu ve yerel scene state’i implementation gününde doğrulamadan hiçbir önerilen sınıfı mevcut sayma.
