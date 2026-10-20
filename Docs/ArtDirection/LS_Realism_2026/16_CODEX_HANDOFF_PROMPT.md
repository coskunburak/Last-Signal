# Codex implementation prompt — copy into an implementation session AFTER documentation merge

> **TASK TYPE:** READ-ONLY reconnaissance and proposal FIRST. Do not begin Unity scene modifications until the owner explicitly says to implement. This document is not an instruction to the current documentation-pack generator to change game code.

You are production tech-art engineer for the Unity 6000.5.0f1 project `Last Signal`. Owner-confirmed visual target: grounded realistic abandoned rural survival world with naturalistic forest quality; Sons of the Forest is a visual inspiration, not an asset/code source. `Docs/ArtDirection/LS_Realism_2026/` defines new art standard. This pilot covers reversible **120×120m** near existing safehouse, road and forest. Only relevant docs/source files are to be opened; do not hydrate entire Docs or unrelated code into model context.

## Phase 0 — bounded reading (read no more than necessary)

Read exactly:
1. `00_MASTER_INDEX.md`, `01_DECISION_RECORD.md`, `02_ART_BIBLE_GROUNDED_REALISM.md`, `03_CURRENT_STATE_AND_GAP_AUDIT.md`, `04_ASSET_RESEARCH_AND_REGISTRY.md`, `05_PIPELINE_URP_HDRP_RFC.md`, `10_HERO_SLICE_IMPLEMENTATION.md`, `11_QA_VISUAL_ACCEPTANCE.md`, `13_ROLLOUT_AND_ROLLBACK.md` from this new folder.
2. Existing `Docs/Implementation/S013/S013_ART_BIBLE.md`, `Docs/Implementation/FirstTownCoreForest/{HANDOFF,DESIGN_AND_ASSETS,MANUAL_QA}.md`, `Docs/Implementation/FirstTownAtmosphere/HANDOFF.md`, `Docs/Implementation/FirstTownVisualSlice/REFINEMENT_05.md` **only relevant sections**.
3. `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, actual local `.gitignore`, current `git status --short --branch` and source scene path. `S003` gathering/harvest implementation only if tree cutting actually involved. Read code narrowly around owner classes and dependencies after discovering exact local paths (`rg -n`, class inspection), not entire asset dumps.
4. Local `Assets/ThirdParty` inventory including newer environment packs, licensed sources, tree/trunk mesh structure, artist protected areas.

## Phase 1 — audit deliverables, before modifying anything

Provide: repo branch/HEAD/dirty diff summary, current source scene and terrain backup status, known full staged scenes, source-hash guards, shader and RP dependencies, user assets that must be preserved; candidate KEEP/ADAPT/REPLACE mapping; licensing status and recommended first 120×120 bounding box; actual performance baseline. If essential files or rights absent, return `BLOCKED` with exact requested file or owner operation.

## Phase 2 — implementation only with owner permission

Do not run `git reset`, checkout, clean, force, commit, push or modify ThirdParty vendor originals. Create new stage scene and TerrainData derivative without overwriting current source or previous first-town stages. Make normalized URP project-owned variants of approved realistic assets. Rebuild pilot terrain ground/edges, canopy 3+ geometry-distinct variants with real LOD, floor cover, weather/light integration through existing authority. Keep existing WorldClock, audio, navigation, save IDs, road collision, player/vehicle and S003 gathering authority. Any source-hash mismatch stops promotion. No install of HDRP or package change in active project.

For tree cutting, distinguish current four-hit disappearance from desired physical notch/fall/settle. Inspect actual current asset mesh topology and gathering API before proposing code. Avoid arbitrary runtime slicer if model non-manifold or cap topology unsupported. Never generate duplicate resource grants or alter existing save format without migration spec.

## QA and user-run policy

Write focused EditMode/PlayMode tests if needed, but **DO NOT EXECUTE TESTS/BUILD/PLAYMODE/benchmark**. List terminal commands that are verified to exist and exact Unity GUI authoring operations for owner to run individually. User sends command logs and screenshots; read only failures and directly relevant code; fix; resend shortest re-test commands. At final gate provide visual capture matrix, perf instrumentation checklist, honest PASS/PARTIAL/NOT_RUN, all changed filenames and their rationale. No handoff 'DONE' until owner-received evidence.

## Output requirements

- path-by-path changes with purpose and rollback snapshot.
- preserved scene/terrain GUIDs and source hashes.
- import & license ledger per selected package; no invented free/compatible claim.
- optional HDRP separate A/B RFC only; no automatic migration.
- unknowns explicit and owner art signoff pending.
