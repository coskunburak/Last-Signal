# QA, accessibility and realistic-world art signoff

**Tests are OWNER-OPERATED.** Documentation author does not run Unity, QA, screenshots or performance checks. Use actual local verified CLI/menu commands, never pretend a guessed command exists. There is no evidence in this ZIP of new Unity test PASS.

## Evidence naming convention

`Docs/Implementation/LS_Realism_HeroSlice/Evidence/<YYYYMMDD-HHMMSS>/<case>/` with `metadata.json` (Unity version, pipeline, project commit, branch, source scene SHA, full quality settings, FOV/resolution, weather/time, seed, CPU/GPU), `before.png`, `after.png`, `test.xml` where relevant, profiler capture, owner receipt and failure description. Keep screenshot exposure comparable; photos/moodboards/AI images are REFERENCE only.

## Camera and condition matrix

| Case | View | Conditions | Acceptance signal |
|---|---|---|---|
| V01 | safehouse front door at 1.7m eye | clear 08:00 | grounded roots, readable exit and road |
| V02 | road approach with horizon | noon | soft shoulder and worn road, no visible tiling/fence box |
| V03 | stand by cuttable trunk | noon and flashlight | bark relief, clean leaf silhouette, no UV seam |
| V04 | forest entry 10–20m | noon | ecological age variation, non-grid composition |
| V05 | forest 50–100m long shot | cloudy | layered depth, LOD silhouettes, no pop/duplicate |
| V06 | same forest entry | rain | believable wetness, foliage shadow / rain occlusion |
| V07 | cabin / gas-station porch | dusk | warm safety vs cool outdoors, materials not uniformly glossy |
| V08 | forest path pursuit | night/flashlight | zombie clearly identifiable, path usable |
| V09 | axe strikes at multiple orientations | day | progressive cut wedge, correct wood interior |
| V10 | tree collapse near obstacle/vehicle | day/wet | physical fall, bounds safe, settle/log lifecycle |
| V11 | road driving / reverse | rain/night | no road seam snag / collider misalignment |
| V12 | save after tree damage and reload | clear/rain | state restored once, no duped reward/tree |

Test 16:9 and a wide aspect (16:10 or 21:9); camera normal gameplay and at minimum one FPS and one third-person view if supported. Confirm minimap/UI pickup contrast and color-blind accessible information isn't color-only. At night an oncoming zombie cannot disappear against intentional dark ground at ordinary encounter range. Fog cannot make mandatory items invisible.

## Technical gate order

1. Files/import: checksum, license, asset GUID, no pink materials/exception shaders, explicit source paths.
2. EditMode: prefab metadata validation, source-asset isolation, seed/idempotence, collider bounds.
3. Visual baseline compare: same camera/time/capture setting, owner review.
4. PlayMode: traversal, line of sight, melee/tree collapse, roof/rain, object state and player/vehicle/camera.
5. Navigation: user-run existing bake and path validation if terrain/collider changed.
6. Save/load: legacy save in forest/cabin and during tree damage/fall, no dupe/migration errors.
7. Performance: Mac development diagnostic + true target Windows player p95/p99 GPU/CPU and VRAM; worst density weather case.
8. Final artist signoff: owner signs mandatory visual cases and accepts any deferred risk.

## Real test commands found in historical repository docs (verify local files first)

```sh
cd '/Users/burakcoskun/Last Signal'
python3 Tools/first-town-forest.py compile
python3 Tools/first-town-forest.py editmode
python3 Tools/first-town-forest.py playmode
python3 Tools/first-town.py bake
python3 Tools/first-town-forest.py full-editmode
python3 Tools/first-town-forest.py full-playmode
python3 Tools/first-town.py build
```

These commands were documented historically, are not run here, and may not match newer local uncommitted changes. For atmosphere-focused checks `python3 Tools/first-town-atmosphere.py compile|references|editmode|playmode` are documented, subject to same verification. Run **individually** after saving and closing the editor for batch runner. Capture exact stdout/command and test XML (process exit alone ≠ test PASS).

## Reject conditions

Any source scene loss/hash bypass, broken materials, gameplay collider overlap or NavMesh disconnection, duplicated harvested logs, partial cut wedge that visually teleports/disappears, visible alpha cards at 2m in hero camera, obvious map boundary wall, severe overdraw without optimization path, mismatched wetness ownership, missing license or no target hardware measurement for release claims. Unknown performance is `NOT_MEASURED`, not zero cost.
