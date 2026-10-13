# Third-party asset layout

`Assets/ThirdParty` groups imported content by use. Each vendor package remains an intact subtree with its original assets, import settings, licences and `.meta` files. Project-authored assets remain under `Assets/LastSignal`.

| Category | Packages and sources |
| --- | --- |
| `Animation` | Animation libraries, Kevin Iglesias, crouch/slide, zombie motion clips and zombie attack animation |
| `Audio` | NOX, FreeWeaponSFX, horror audio, Kenney impacts and the individual visceral impact clip |
| `Characters` | Diesel, VAL, FirstPersonArms and the S14 FPS arms pack |
| `Environments` | FantasyEnvironments, Holotna, mountain, barn, Silver Cats and TT3D |
| `Frameworks` | FirstPersonARPGStarterPack |
| `Loot` | MedicinePack, Polygon-Lite Survival Collection and food/kitchen pack |
| `Samples` | Unity URP template scene and readme |
| `UI` | Kenney input prompts |
| `Vehicles` | MotionCore CharacterController, Pack_Pickup and Stylized Vehicles |
| `VFX` | BloodDecals and Free Blood VFX |
| `Weapons` | CrowbarLowPoly, MRPoly, Gece Studio HK416, machete and wooden axe |
| `Zombies` | Zombie character/model packages |

The old collection folders `S14 ASSETS`, `S15 Sound Pack`, `Vehicle Entegrations`, `weapon` and `ZOMBIE COMBAT, BALLISTIC FEEDBACK & GORE OVERHAUL` are no longer asset locations. Their packages were moved to the categories above. The two identical Diesel FBX files retain **different original GUIDs**: the active source is `Characters/Diesel/Diesel.fbx`, and the S14 copy is in `Characters/Diesel/S14 Source/Diesel.fbx`. Neither was deleted or deduplicated.

## Reference safety

- Every asset moved with its existing `.meta` file. The migration preserved 8,932 existing asset/folder GUIDs and all 8,374 non-meta asset files. Seven new organizing folders received new GUIDs.
- Five now-empty wrapper folders were retired. A scan found no serialized asset reference to their GUIDs. The prior migration manifest marks its two affected wrapper records as retired and points to their successor package paths.
- Editor authoring, tests, project settings and active tool paths that used literal `Assets/ThirdParty/...` strings were updated. Unity prefab, scene, material, animation and Inspector references use the preserved GUIDs.
- Historical evidence and one-time migration plans retain original paths as records of the earlier layout. Do not run `Tools/reorganize_assets.py --apply` again; its existing manifest prevents that operation.

`ThirdPartyLayoutManifest.json` records the moved groups, retired wrapper GUIDs, counts and GUID inventory hashes. Run the read-only audit after changes to imported packages:

After the relocation, a Mountain package import added 202 assets and six folder GUIDs under `Environments/Mountain (Unity 2022.3 1.16f1)/Holotna`. The manifest retains the original relocation counts and hashes and records the later verified inventory separately. The imported scene, settings and source assets remain together with their `.meta` files.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/thirdparty_audit.py
```

The static audit checks directory structure, `.meta` pairing, GUID uniqueness and inventory, retired references, and stale active source paths. Unity import/compilation and Inspector presentation must still be checked in the Editor after opening the project. Inspect the Player, Diesel WorldBody/WorldRifle, zombie, vehicle and audio prefabs for missing scripts/materials; then run the relevant existing EditMode/PlayMode suites. No Unity test or build was run as part of this layout change.
