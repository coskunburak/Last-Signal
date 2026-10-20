# Rollout and rollback protocol — never lose current FirstTownCore

## Branch and working tree safety

Active local branch may be `s012-integrated-graybox-slice`, but the connected GitHub default is `main`; both heads and local dirty state must be rechecked at start. Package is generated against the documented **main** baseline, not live uncommitted work. Create a dated checkpoint branch for integration, and back up user-authored source scene/terrain and save slots without deleting/cleaning the working tree. **No `git reset --hard`, `git clean -fd`, forced checkout, GUID regeneration, global scene rewrite or automatic commit/push.**

## Document installation

1. Extract ZIP outside repository; inspect `CHANGELOG.md`, `MANIFEST.json` and this crosswalk.
2. Compare each `REPLACES_EXISTING` doc to local file with diff; local user edits take precedence and must be merged, never overwritten unreviewed. Four prior files in package are full-document files with dated addenda; if local differs from GitHub snapshot, merge only addendum.
3. Copy new `Docs/ArtDirection/LS_Realism_2026/` directory. Its self-contained relative references and new unique path avoid old file name collisions.
4. Do **not** replace binary GDD v0.1 docx, historical asset evidence or old S023 plans. The owner art decision is documented as an addendum; old technical history remains retrievable.
5. Review `git diff -- Docs/`, do Markdown-link check and only then commit in a deliberate doc-only commit if desired. Do not touch Unity build files.

## Visual implementation migration after docs signoff

- Isolate clone/stage scene and terrain derivative; record scene source SHA, `.meta` GUIDs, authoring inputs, camera/baseline captures and deterministic scatter output.
- Create `Assets/LastSignal/Art/Realism` prefab/material variants without writing back to `Assets/ThirdParty` source. Keep legacy Fantasy foliage references in unrelated scenes.
- Apply pilot changes only within bounded mask, with protected hero tree and artist POI exclusions, and retain the old branch/recovery snapshot.
- Promote through existing authoring guards only when source diff and navigation/save tests pass. Do not bypass FirstTownVisualSlice source/terrain hash warnings.
- Branch state and adoption dates must be recorded when updating art registers. Existing proof from older lower-quality standard is still history, not new visual acceptance.

## Rollback levels

**R0 (docs):** revert new addenda/folder in doc-only commit. Old decisions become active only through explicit owner decision; do not rewrite history.
**R1 (asset lab):** delete only owned test derivatives in isolated lab after preserving evidence; no scene affected.
**R2 (stage):** restore staged scene snapshot/terrain derivative and render settings; preserve current production scene and GUIDs.
**R3 (production visual promotion):** restore pre-promotion source scene checkpoint and matching terrain/material mapping; revalidate current saves and surface IDs; no assumption save rollback is always reversible after runtime data migration.
**R4 (pipeline):** use clean URP branch and known package manifest; HDRP experiment never shares main project directory.

## Doc/asset completion receipt

Each step needs date, owner, Git sha, changed files, baseline hash, implementation vs test vs visual statuses, explicit blockers, before/after references, user-run commands and XML/evidence, rollback asset, approval signature. No receipt = `NOT_VERIFIED`.
