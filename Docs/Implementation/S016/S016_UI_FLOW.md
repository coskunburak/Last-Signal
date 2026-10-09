# S016 ekran akışı — ilk kapı

Tek yürütme otoritesi SessionFlow; UI panelleri timeScale/cursor/input map değiştirmez.

- Sahnenin mevcut Start davranışı → BeginSession → Gameplay (ilk ana menü başlangıcı bu kapıda değiştirilmedi).
- MainMenu → mevcut New Game veya SaveSession.Load → Gameplay; Load sırasında Loading, başarısızlık mevcut SaveResult akışında kalır.
- Gameplay → Tab/InventoryUI.Open → Inventory: oyun durur, cursor açılır; pause/ses/kayıt panelleri gizlenir.
- Inventory → ESC veya authored Close → Gameplay: panel kapanır; SetGameplay neutralRequired uygular. Basılı click gameplay saldırısı sayılmaz.
- Gameplay → ESC → Pause → ESC/Resume → Gameplay.
- Storage/Journal → ESC/Resume → ilgili sahibin Close yöntemi → Gameplay. Bu otoriteler yeniden yazılmadı.
- Focus kaybı → SessionFlow.Pause: açık inventory korunur; geri dönüş otomatik resume yapmaz.
- Inventory disable/unbind → panel kapanır ve geçerli canlı oturum döner. Ölüm → inventory gizlenir; Resume/ESC gameplay açamaz.
- Menu'ye dönüş → inventory unbind; tekrar session tek binding. Eski Close çağrısı başka pause ekranını kapatamaz.

Screen önceliği: Loading > MainMenu > Death > Inventory > Journal > Storage > Pause > Gameplay. Bu property mevcut domain durumlarını okur; ikinci gameplay state machine değildir.

Ayrı Settings ekranı, önceki ekrana dönüş ve başlangıç menüsü kararı sonraki D151/D155 kapısında. Kayıt silme eylemi mevcut değil. Tab ile modal kapama henüz eklenmedi; güvenli çıkış ESC ve Close.
