# P01 / S003 giriş

2026-10-18. Local branch `s012-integrated-graybox-slice`, HEAD `84aa7358be7b3f47517b7e083ff0734e762e210f`. G0 ACCEPTED design/documentation; yeni kullanıcı isteği S003 kodunu yetkilendirir. Git remote karşılaştırması/fetch yapılmadı; paket remote gözlemi güncel yerel kanıt sayılmadı. Git mutation/Unity execution yok.

## Minimum source map

| Grup | Dosya / gerekçe |
|---|---|
| Combat | MeleeWeaponController, MeleeAttackResolver, MeleeAttackState: tek active window/stamina/target |
| Inventory | ItemCatalog, ItemDefinition, WorldItem: ID/mass/prefab ve pickup |
| Save | SaveSession, SaveGame, SaveValidation, SaveFileStore: additive section/durable publish |
| Cell | WorldCellManager, WorldCellContent: readiness/unloaded ownership |

Ek okumalar yalnız dependency için: SessionFlow lifecycle; PlayerCombatController ownership; SaveCodec optional normalization; forest authoring/state ve seçili scene adları; test asmdef/fixture örüntüleri. Başlangıç dirty dosyaları ve korunan sahne hash'leri ENTRY_BASELINE.json içinde. Aynı yedi VisualSlice user değişikliği ve Meshy klasörü korunur.

## Asset ve taxonomy kararı D017

S003 yeni harici asset 0. `material.log` Material kategorisi, 2000 g/adet, stack 8, node yield 3; BALANCE_PROPOSED. Branch/plank/stone/rope/fastener geleceğe referans, uygulanmaz; material.scrap mevcut. WoodenAxe Range=1.9, stamina=43, existing active window; weapon.axe WorldPrefab=null. Pilot axe edinimi mevcut PlayerCombatController.MeleeWeapons/FindMelee/SelectMeleeWeapon yolu, envantere sahte pickup eklenmez. Yeni kaynak definition/pickup owner-run stage tooling ile oluşturulacak; vendor dosyası değişmez.
