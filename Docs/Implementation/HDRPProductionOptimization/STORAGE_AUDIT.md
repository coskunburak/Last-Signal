# Storage audit — 2026-10-21

## Latest checkpoint — Windows working-copy reduction, 2026-10-22 22:14 Europe/Istanbul

Owner explicitly requested removal of unused asset packs and transfer size reduction, then selected **manifest only** (no destination copy). Reviewed owner runs `20261022T190249895402Z-validate-shaders`, `20261022T190315113510Z-production-audit`, `20261022T190410078824Z-full-edit`: **21 shaders / 0 errors, 910/910 tests**, 47 production roots / 269 materials / 13,166 slots, no new findings or audit errors. The three disabled blood emitter empty slots and GUI/Text exception are unchanged. Frozen per-run reports and `Evidence/post-freeblood-verification-20261022.json` preserve results. All 216 integration guards and 28 archived asset/meta pairs verified after Unity exit.

### Executed reduction

With Unity closed, moved five proven unreferenced packs (kenney_impact-sounds, VisceralBulletImpacts, barn-village-abandoned-house, old Mountain distribution, ZombieMotionClips) and seven additional unused `.unitypackage` installers out of the active project. **756 files / 1,414,234,064 bytes (1.414 decimal GB)**, preserving metas, licensing and all originals. Archive: `/Users/burakcoskun/LastSignal-Recovery/UnusedPackages-20261022T191127Z`. Every file hash was verified before and after the move. No permanent deletion; no claim of equivalent free disk space. Checkpoint/Git/LFS unchanged, 216 protected hashes remain exact. Receipt: `Evidence/unused-package-retirement-20261022.json`.

The bounded package assessment used incoming GUID references, source/config path/name references, Resources and script exclusions. Final selected file GUIDs were searched across all Assets/Packages/ProjectSettings bytes including ignored/binary files, with zero external consumers. Packages with references, automatic Resources inclusion or scripts remain. In particular, the separate HolotnaMountainURP pack is used by S013 comparison/authoring and was NOT confused with the retired unreferenced Mountain distribution. This is not a claim that every remaining asset is used at runtime.

### Transfer

`transfer_manifest.py --profile windows-work` includes all remaining Assets (ignored assets and every meta), Packages, ProjectSettings, Tools, root source helpers, documentation Markdown and current top-level HDRP evidence JSON. It excludes Git/LFS, historical binary evidence/rollback archives, Library, build outputs and local caches from the transfer, preserving them on the Mac. This is a working project copy, not a repository/history/recovery backup; historical authoring/recovery operations may require the separate complete archive. `complete --include-git` remains available unchanged in scope. Windows-name/case validation and optional copy-to-new-directory with per-file destination hash checks were added. No filesystem deletion or mirror operation is implemented.

Manifest: `Evidence/windows-work-manifest.json`; source verification: `Evidence/windows-work-verification.txt`. Create the final manifest after these documents; inspect its exact file count/bytes. No disk audit restart, Unity/tests/build, Git maintenance or Windows copy performed by the agent. The old 107,016-file transfer verification describes the earlier complete snapshot only.

URP package dependencies remain absent. Remaining used Holotna comparison shader graphs and previously documented legacy vendor/recovery references are unresolved compatibility work; this reduction does not declare project-wide URP-reference zero or production completion. Owner import/validation after the final retirement, post-removal PlayMode/build, development-profile activation/benchmark and Windows visual acceptance remain pending. Do not run consumed migration plans.

Next owner validation, Unity saved and closed:

```bash
python3 Tools/HDRPProductionOptimization/manual_checks.py full-edit &&
python3 Tools/HDRPProductionOptimization/manual_checks.py production-audit &&
LASTSIGNAL_S004_STAGE='Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019163434-8e0f05b7.unity' python3 Tools/HDRPProductionOptimization/manual_checks.py full-play
```

If Unity changes any source files, regenerate the transfer manifest before copying. Windows verification must run before opening Unity. See WINDOWS_TRANSFER.md for copy commands.

## Previous record — historical


Status: VERIFIED static measurement and bounded cache cleanup; permanent source reduction 0 bytes. Unity has not been reopened.

Before: **67.563 GiB allocated file blocks / 67.001 GiB apparent bytes** (72.55 / 71.94 decimal GB). After cleanup and initial tooling/evidence: **65.496 GiB allocated / 64.962 GiB apparent**. `du -k -d 1 .` independently reported 70,868,716 KiB initially; small differences include directory blocks, metadata and timing. The inventory sums files, not directory blocks. Final `du` is recorded in Evidence/final-size.txt: 68,677,528 KiB (65.496 GiB), versus 70,868,716 KiB initially. The ~2.090 GiB whole-tree difference also includes small pre-existing cache/log changes and added reports; only the 2.070 GiB deletion receipt is attributed to this cleanup.

The reported 60–70 GB is explained: Docs is mostly evidence, not text, and Builds is another large tree. Hidden directories are included. APFS clone blocks can be shared, so `du` is not exclusive physical consumption. Free-disk changes can include other applications and snapshots.

| Directory | Apparent GiB | Allocated GiB |
|---|---:|---:|
| `Docs` | 16.123 | 16.256 |
| `Builds` | 15.130 | 15.154 |
| `Library` | 12.893 | 13.175 |
| `.git` | 11.503 | 11.557 |
| `Assets` | 11.166 | 11.231 |
| `.p02-historical-backup` | 0.143 | 0.146 |
| `Logs` | 0.016 | 0.016 |
| `Tools` | 0.001 | 0.001 |
| `scratch` | 0.001 | 0.001 |
| `UserSettings` | 0.000 | 0.000 |
| `ProjectSettings` | 0.000 | 0.000 |
| `Packages` | 0.000 | 0.000 |

`Temp`, `Obj`, `obj`, and `Build` were absent at baseline. Root files total about 26 MiB, including two 10,000,001-byte crash blobs; they were retained. No unaccounted 30 GB cache exists.

## Ranked large files (binary contents not loaded)

| Path | Apparent GiB | Tracked |
|---|---:|---|
| `Library/Artifacts/c7/c7263d0b9d0b58ed73b24b6ca9e6c0a5` | 0.466 | no |
| `Assets/ThirdParty/MarpaStudio/HDRP/FarmHDRPPackage.unitypackage` | 0.397 | no |
| `Library/PlayerDataCache/OSXUniversal2/Data/sharedassets0.assets.resS` | 0.358 | no |
| `Docs/Implementation/CharacterIntegration/Evidence/20261014-002923-development-build-0bcc4566/LastSignal.app/Contents/Resources/Data/sharedassets0.assets.resS` | 0.358 | no |
| `.git/objects/pack/pack-849d3d0e7ad7414ec90e7fb0c75a82150602545f.pack` | 0.343 | no |
| `Docs/Implementation/S012/Evidence/20261003T102140Z-performance/player.log` | 0.309 | yes |
| `.git/lfs/objects/a0/7f/a07fea089aaf67506964cfa4acbb80d3974cf75a2e06636be2e48db92818280b` | 0.309 | no |
| `Assets/ThirdParty/MarpaStudio/URP/FarmURPPackage.unitypackage` | 0.301 | no |
| `Library/ArtifactDB` | 0.292 | no |
| `Docs/Implementation/S014/Evidence/20261007T090154-796032Z-population-pc/20-0-cpu.raw` | 0.241 | yes |
| `.git/lfs/objects/94/ec/94ec6f0b5c8cfaf358723d83bb00f0ab1deadb2c2bd1d12cb7f7a4c853fe251c` | 0.241 | no |
| `Docs/Implementation/S014/Evidence/20261007T091601-579186Z-population-low/20-0-cpu.raw` | 0.236 | yes |
| `.git/lfs/objects/68/b2/68b210432741ef60a3a87b7498dacebb3c21ff7bc250271b00ce71fabd33a563` | 0.236 | no |
| `Docs/Implementation/S014/Evidence/20261007T085519-615708Z-population-pc/10-0-cpu.raw` | 0.230 | yes |
| `.git/lfs/objects/c7/2f/c72f6c4f0ac7caf43f5abbf0266e9cc317cf5ea41c0f586f90bf43b64ee726ff` | 0.230 | no |

## Classification and removal assessment

All paths below are relative to `/Users/burakcoskun/Last Signal`. Exact removed-file counts/bytes and rollback are in Evidence/cleanup.json.

| Path | Category / owner | Allocated GiB | Tracked / referenced | Regeneration / backup | Decision and risk |
|---|---|---:|---|---|---|
| Assets | A/E source and vendor | 11.231 | Mixed; 20,343 ignored asset files | Full outside checkpoint | Preserve; high risk |
| Packages, ProjectSettings, Tools | A project source | <0.002 | Mixed; required | Full outside checkpoint | Preserve |
| Library/Artifacts | B Unity import cache | 7.290 | Untracked; import database | Reimport possible, cache not backed up | Retain to avoid full import |
| Library/PackageCache | B resolved packages | 2.812 | Untracked; package/code dependencies | UPM fetch; source lock backed up | Retain |
| Library/Bee | B Unity build cache | 0.903 | No tracked files; cache consumers only | Recompile; source checkpoint | REMOVED, low risk |
| Library/BurstCache | B Burst compile cache | 0.568 | No tracked files; cache consumers only | Recompile; source checkpoint | REMOVED, low risk |
| Library/PlayerDataCache | B player build data | 0.599 | No tracked files; cache consumers only | Rebuild; source checkpoint | REMOVED, low risk |
| Library/ShaderCache | B shader cache | 0.204 | Untracked; shader compiler | Recompile | Retain to avoid extra shader stalls |
| Builds | C candidate old outputs | 15.153 | Ignored; ownership varies | Exact old builds not checkpointed | Retain; source/config provenance unresolved |
| Docs/Implementation/**/Evidence | C/F historical evidence | bulk of 16.256 | Mixed, some LFS tracked | Full checkpoint | Preserve diagnostic/history value |
| .git | D repository/LFS | 11.557 | Git state, no asset references | Complete outside checkpoint | Preserve; see below |
| .p02-historical-backup | F historical recovery | 0.146 | Ignored | Full checkpoint | Retain until ownership reviewed |
| Assets/LastSignal/Art/*/Generations | A generated source | included above | Scene/prefab references and rollback generations | Source checkpoint; generators exist | No deletion; generation does not imply obsolescence |

Removed caches total **2,222,456,832 allocated bytes (2.070 GiB)** and **2,191,607,455 apparent bytes**. This is immediate temporary savings, not permanent source reduction. Expected after rebuilding equivalent targets: much or all of ~2.07 GiB can return; steady-state usage is unmeasured. Cache contents were not copied into the checkpoint. Reopen/recompile/rebuild is their rollback. No Unity cache was removed with an Editor process or project lock present.

34 exact-content duplicate source groups >=1 MiB were SHA-256 matched, with a theoretical 0.702 GiB duplicate payload upper bound. Evidence/baseline/duplicates.json has paths/hashes. No deduplication: different GUIDs, import settings, vendor ownership and prefab references make byte equality insufficient.

## Why .git is large

- Ordinary Git: 7 packs / 36,244 packed objects, 485.84 MiB packs; 7,317 loose objects / 72.98 MiB. The largest reachable ordinary blob is a 76,181,938-byte vendor demo terrain. Not a 12 GB loose-object problem.
- LFS: 16,277 local objects / **10.975 GiB payload**. Current checkout OIDs account for 5.412 GiB; history-only OIDs listed by `git lfs ls-files --all` account for 3.723 GiB; 1.840 GiB is not listed by that ref scan. The last group is NOT proven garbage or remotely recoverable.
- 4,642 current LFS paths, 4,335 unique OIDs; all current OIDs exist locally. All-ref scan lists 9,888 OIDs. Remote availability and historical LFS completeness have not been certified.
- `git fsck --full --no-progress`: exit 0, 413 dangling-object notices, no integrity errors. `git count-objects` also identified one 176-byte temporary loose-object file. Both preserved.
- 3 local branches, one worktree, zero stashes, 33 unique reflog commit hashes. Zero commits in local branches absent from **cached** remote refs; no fetch was performed, so this is not a current-server claim. Working tree is very dirty and remains so.
- `.git/index.lock` was already present and is open by a language-server process. It was not deleted. No gc, repack, prune, LFS prune, history rewrite or branch operation was performed. Routine packing can address only a small fraction of total space and was not justified with that active lock.

## Transfer estimate

Assets + Packages + ProjectSettings + Tools + all Docs: **27.289 GiB apparent**; with `.git`: **38.792 GiB**. Root helpers and new reports add only a small amount. Keep ignored assets. See WINDOWS_TRANSFER.md for a freshly hash-verified manifest before copying.
