# Preservation and recovery

Checkpoint: `/Users/burakcoskun/LastSignal-Recovery/HDRPProduction-20261021-181319`.

`VERIFIED.json` reports 108,402 files / 41,851,617,436 bytes. Method: APFS `cp -cRp`, SHA-256 of source before, copied file, and source after; all inventories identical. Manifest SHA-256: `e2f09fdeb465d10f7878a1bc388e206deacc71ee4512cc8d7cac488b3fdfc6bf`.

Includes all Assets and metadata (tracked, untracked and ignored), Packages, ProjectSettings, Tools, Docs/evidence, root files, UserSettings, historical local backup, and complete `.git` including LFS/reflogs/index. Excludes only Library, Temp, Obj/obj, Build/Builds. No original build output was deleted. This is a same-disk rollback checkpoint, not an independent disaster backup; APFS block sharing makes summed backup `du` misleading. Do not count the checkpoint as disk savings.

At checkpoint time: no Unity Editor process, no Temp/UnityLockfile, and Library/LastSceneManagerSetup.txt had an empty scene list. Unity Hub was running. This establishes no live unsaved Editor scene at that moment; it does not imply old recovery scenes are disposable. The existing Git index lock was held by a language server and was preserved.

Protected source verification is recorded in Evidence/preservation.json: 51,145 original files checked, zero missing files, zero unexpected changes. All original Assets (including both primary scenes, S004 runtime, movements, weapons, rigs, AI, vehicles, inventory, persistence, audio, weather, forest generations and every old `.meta`), packages, settings, Tools and Git files were compared with the checkpoint. Only the explicitly listed editor/tool edits are allowed differences; no original source deletion is allowed. New test files have new GUIDs; existing GUIDs remain unchanged.

Branch remains `s012-integrated-graybox-slice`, HEAD `3fa3161f0d3d22f40d422fd5b404605f25088ce0`. No reset, clean, checkout, commit, force push or repository maintenance. Ignored vendor/source assets cannot be recovered by cloning GitHub alone.

## Rollback this phase

Save and close Unity. Inspect this task's CHANGED_FILES list. Restore only its three modified existing editor/wrapper files from the checkpoint's corresponding `project/` paths, preserving all other later user edits. The audit tool was created before the checkpoint and needs no original-user restore. Remove the new test source and its `.meta` together through Unity asset deletion if abandoning this phase. New tools/docs can simply remain unused.

Example for one existing file, after reviewing later changes:

```bash
cp '/Users/burakcoskun/LastSignal-Recovery/HDRPProduction-20261021-181319/project/Assets/LastSignal/Scripts/Editor/TreePhysicsAuthoring.cs' '/Users/burakcoskun/Last Signal/Assets/LastSignal/Scripts/Editor/TreePhysicsAuthoring.cs'
```

For full recovery, **copy the checkpoint `project` into a new sibling directory** and open that recovery copy with Unity 6000.5.0f1; do not overlay or delete the active project. Reimport recreates excluded caches. Keep the saved Git index lock as evidence; do not use the restored repository for writes until verifying no lock owner and handling the copied lock in that recovery copy. No automatic restore command deletes locks.

The checkpoint is the current pre-task HDRP working state, not the project's old URP state. Earlier partial material rollback manifests are not equivalent to a full source snapshot. Keep this checkpoint until post-removal visual/gameplay acceptance and an independent backup exist.

## Handoff completion

The pending transfer verifier exited 0: 107,016 files checked, zero mismatches. The recovery checkpoint manifest SHA-256 was rechecked at handoff and matches its VERIFIED receipt. No checkpoint, Git/LFS object, ignored vendor asset, forest asset or S004 simulation file was modified during this handoff. Only the handoff documents were finalized. The transfer receipt describes its pre-final-document snapshot; regenerate the transfer manifest before copying.
