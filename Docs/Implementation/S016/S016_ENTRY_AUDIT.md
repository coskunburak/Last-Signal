# S016 başlangıç incelemesi — 2026-10-07

Dal `s012-integrated-graybox-slice`, HEAD `7f5ffb783f396f9675c28da18be7021d90a02eb7`; Unity `6000.5.0f1`. Input System 1.19.0, uGUI 2.5.0, Test Framework 1.7.0. MCP incelemesinde S013Cabin etkin, Play kapalı, sahne temizdi.

Kullanıcının yanlış S015 talebi üzerine bu chat'te yapılan MovementAcceptanceTests düzenlemesi SHA-256 kontrollü yedekten geri alındı; yalnız bu chat'in oluşturduğu iki source-only kayıt kaldırıldı. Önceden mevcut S015 sistemi ve kanıtları değiştirilmedi. S016 başlangıcında S014 ledger/handoff, VEH-001 iki belge ve ProjectSettings.asset dirty; önceki kanıt/tool dosyaları ve Noto_Sans untracked. Bunlar korunuyor. Commit/push yok.

## Mevcut otoriteler ve açıklar

- SessionFlow pause, cursor ve input map sahibi. InventoryUI ayrı panel boole'ı nedeniyle ESC sonrasında açık kalabiliyordu. AcceptanceHud, AudioSettingsView ve WorldTimeSaveControls aynı anda görünebiliyordu.
- InventoryContainer move/merge/split; PlayerInventory drop; ShelterLoop + ItemTransferService transfer otoritesi. Domain sabit slot kullanıyor, revision/mass/equip/consume varsayılmadı. Transfer sığanı taşıyabiliyor; UI bunu açık anlatmalı.
- InteractionController ilk collider engelini koruyor, committe yeniden Resolve yapıyor. Kenar titremesi/örtüşme kararlılığı henüz doğrulanmadı.
- AudioPreferences beş bus ve captions için PlayerPrefs sahibi. FOV/sensitivity FirstPersonLook'ta serialized; kalıcılık/UI scale henüz yok.
- WorldClock.Update Paused/Restoring/Dead halinde ilerlemiyor; bu tüm simülasyonların pause kabulü değildir.
- SaveSession/SaveResult/SaveError mevcut. WorldTimeSaveControls teknik Message gösteriyor; oyuncuya dönük kategoriler sonraki kapıda.
- Production InventoryUI: 1040×650 panel, 24 slot ve authored Close butonu. Aktif metin fontu LegacyRuntime. Kullanıcının eklediği `Assets/Noto_Sans/static/NotoSans-Regular.ttf` MCP HasCharacter kontrolünde `çğıİöşüÇĞIÖŞÜ` içeriyor. Font ataması ve +%30 metin görsel kabulü NOT_RUN; asset değiştirilmedi.

## Okuma sınırı

Yalnız canonical S016 ve kaynak 05_Player_Input_Movement, 07_Item_Inventory_Equipment, 18_UI_UX_Accessibility, 22_QA_Acceptance_Traceability okundu. Bunlar kapsam, otorite ve test sözleşmesini belirlemek için gerekliydi. Ek tarihsel belge okunmadı. Runtime sahipleri ve doğrudan ilgili testler incelendi; geniş Docs taraması yapılmadı.
