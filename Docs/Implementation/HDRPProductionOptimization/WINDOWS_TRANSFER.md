# Windows transfer — reduced working copy

Latest choice: **prepare the manifest only**, no external disk copy. The working copy includes every remaining active/ignored asset and meta, Packages, ProjectSettings, Tools, root source helpers, Markdown docs and current top-level HDRP evidence JSON. Historical binary evidence and rollback archives, Git/LFS, build outputs and Mac caches remain preserved on the Mac. This profile supports the current project; it is not a complete history/recovery transfer. Old recovery/authoring workflows can need the separately retained full Docs archive.

Five unreferenced asset packs and seven additional package installers were moved out of the active project: **1,414,234,064 bytes / 756 files**. All originals/metas are hash-verified in `/Users/burakcoskun/LastSignal-Recovery/UnusedPackages-20261022T191127Z`. This reduces project/transfer size, not total Mac disk usage. The original 108,402-file checkpoint and all Git LFS objects remain unchanged.

The exact transfer size and file list are recorded in `Evidence/windows-work-manifest.json`; local verification is recorded in `Evidence/windows-work-verification.txt`. These describe the current source snapshot, not an already copied or Unity-validated Windows installation. The last owner tests preceded the final unused-pack retirement; see CURRENT_HANDOFF.md for remaining verification.

## Regenerate after any source edit or Unity import

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/transfer_manifest.py create Docs/Implementation/HDRPProductionOptimization/Evidence/windows-work-manifest.json --profile windows-work
python3 Tools/HDRPProductionOptimization/transfer_manifest.py verify Docs/Implementation/HDRPProductionOptimization/Evidence/windows-work-manifest.json
```

## Later copy to a new external-disk directory

Replace the example destination with your actual mounted drive. Source files remain untouched. The destination must be new or empty; existing projects are never overwritten. Copy verifies each file against the manifest and writes `LastSignal-transfer.json` into the destination after success.

```bash
python3 Tools/HDRPProductionOptimization/transfer_manifest.py copy Docs/Implementation/HDRPProductionOptimization/Evidence/windows-work-manifest.json --destination '/Volumes/TRANSFER/LastSignal-Windows'
```

After moving that whole folder onto Windows, verify before launching Unity:

```powershell
Set-Location 'D:\Projects\LastSignal-Windows'
py -3 .\Tools\HDRPProductionOptimization\transfer_manifest.py verify .\LastSignal-transfer.json
```

Use Unity **6000.5.0f1** with Windows Build Support, Python 3.10+, Git for pinned Git package resolution, and compatible Blender for existing .blend source imports. Do not copy Mac Library. Enable Windows long-path support/use a short destination path where needed. Verify actual Windows import/build/rendering before considering the transfer accepted. Keep the Mac source, Git/LFS, complete Docs and recovery archives until then.

For a complete repository/history transfer instead, use the default `complete` profile with `--include-git`; it intentionally includes all Docs and the full Git/LFS payload and is much larger. Do not merge that snapshot into an existing work copy using a destructive synchronization operation.

## Earlier complete-transfer record (historical scope)

# Windows transfer

Status: local transfer checksum verification completed: **107,016 files checked, zero mismatches**, process exit 0. Portable tools implemented; no Windows import/build performed. Local files are authoritative; GitHub is insufficient.

Copy **Assets (including all ignored folders and every .meta), Packages, ProjectSettings, Tools, all Docs including evidence/rollback inputs, .gitignore, .gitattributes, and root source/helper scripts**. Include the complete `.git` if preserving local branches, reflogs and LFS history. Keep `.p02-historical-backup` and unknown root scratch/recovery material in a separate archive until its owner approves retirement; they remain in the outside checkpoint.

Do not transfer Library, Temp, Obj/obj, Build/Builds or active logs as required project source. UserSettings are optional local preferences; archive separately if desired. Full portable source including Docs: approximately **27.289 GiB apparent**; with Git approximately **38.792 GiB**. New files/root helpers add a small amount. All Docs is intentionally retained because evidence includes recovery inputs, not just disposable screenshots.

The current local file list and SHA-256 manifest are in `Evidence/transfer-manifest.json`; `Evidence/transfer-verification.json` records local readability/hash verification. This verifies the source set before final handoff-document edits, not a Windows copy. The receipt remains evidence for that snapshot; these subsequent documentation edits require a fresh manifest before transfer. Regenerate immediately before a later transfer if any listed file changes.

## Verified copy workflow

Close Unity and pause Git writes on both machines. Generate a fresh manifest immediately before copying:

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/transfer_manifest.py create '/Volumes/TRANSFER/LastSignal-transfer.json' --include-git
```

`/Volumes/TRANSFER` is a placeholder for an actual mounted transfer disk; no external disk was mounted during this task. Copy the manifest and listed paths preserving relative paths and hidden `.git`. Never run a mirror/delete operation against the active source. Copying the whole project and excluding only regenerable directories is acceptable if the destination has space. Avoid case-folding or filename normalization during copy. Verify **before opening Unity** on Windows:

```powershell
Set-Location 'D:\Projects\Last Signal'
py -3 .\Tools\HDRPProductionOptimization\transfer_manifest.py verify 'E:\LastSignal-transfer.json'
```

Stop on any mismatch. The manifest checks SHA-256 of each listed file; it does not delete extra destination files. Destination paths are examples. The current outside recovery checkpoint has already been hash-verified, but a future transfer needs a fresh manifest because this task added/changed files.

## Setup and portability findings

- Install Unity 6000.5.0f1, Windows Build Support and Python 3.10+. Do not upgrade HDRP independently. Let UPM resolve the pinned manifest/lock; public Git packages OpenFracture and unity-mcp need working Git/network access.
- Install Git LFS if using repository history. The current 4,335 checkout OIDs are locally present; remote historical completeness is unknown. Copy local LFS objects if preserving the repo. `git lfs pull` cannot restore ignored vendor files.
- Three actual .blend source assets require a compatible Blender installation for fresh imports: fern_02_4k.blend, forrest_ground_03_4k.blend and fir_tree_01_4k.blend (1). The existing cache can hide this dependency on the Mac. Validate imported orientation/normal channels on Windows.
- New manual runner supports Mac/Windows discovery and --unity/UNITY_EDITOR. The existing forest shell entrypoint remains compatible. Older unrelated Tools scripts still contain Mac-specific paths/process guards; Evidence/baseline/portability.json lists exact files/lines. They have not all been rewritten or certified portable.
- No case-folded path collision was found in Assets/Packages/ProjectSettings/Tools. No .dll/.so/.dylib files were found under Assets by filename scan; package/native architecture still needs target import verification. This is not a native-plugin build certification.
- Saved graphics API overrides exist only for iOS/Android, not a deliberate Windows production API selection. macOS uses Metal; review DX11/DX12 for the Windows profile and verify shaders on the chosen target. Ray tracing is off in High.
- Existing enabled build scenes are acceptance scenes; production FirstTownCore boot/save flow and ForestPrototype120 loading must be checked explicitly. Do not replace the scene list without an owner-run build/profile review.
- A copied `.git/index.lock` from the checkpoint is historical evidence, not proof of a live Windows process. Resolve it only after verifying ownership on the destination; this task did not delete the original lock.

Follow MANUAL_QA.md after the manifest verifies. Keep the Mac project and outside checkpoint until Windows rendering, save/load, regressions and build acceptance are complete.
