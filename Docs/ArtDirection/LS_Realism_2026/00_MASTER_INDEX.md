# LAST SIGNAL — Visual Realism Transformation / Production Master Documentation v1.0

**Decision:** LS-ART-2026-001 · **Date:** 2026-10-19 · **Art goal:** CONFIRMED by owner · **Implementation:** NOT_STARTED under this documentation package · **Pipeline:** URP CURRENT, HDRP EXPERIMENT ONLY.

## Executive contract

The accepted target is a **grounded, high-fidelity, naturalistic first-person zombie-survival world evocative of the environmental credibility of Sons of the Forest**, not its copyrighted imagery, world layout, assets, logos, or exact shaders. We seek near-photorealistic natural environment quality within an independently identifiable abandoned rural town. Previous painted/stylized foliage is no longer automatically accepted as the hero art family. Reuse only by new benchmark acceptance. The game remains single-player-first; no co-op, gameplay/save/world identity redesign implied.

**Do not confuse these separate results:** (A) owner's art-goal decision APPROVED, (B) source package discovery CANDIDATE, (C) imported asset TECHNICAL PASS, (D) screenshot aesthetic acceptance OWNER PASS, (E) measured performance PASS, (F) shipping license approval. None of B–F follow automatically from A. This document package neither runs Unity nor imports assets nor certifies release.

## Reading order and authority

1. `01_DECISION_RECORD.md` — approved scope, supersession and decision control.
2. `02_ART_BIBLE_GROUNDED_REALISM.md` — binding new visual identity; visual do/don't and tier expectations.
3. `03_CURRENT_STATE_AND_GAP_AUDIT.md` — observed facts, bounded unknowns, critical dependencies.
4. `04_ASSET_RESEARCH_AND_REGISTRY.md` — candidates, links, due diligence, staged usage.
5. `05_PIPELINE_URP_HDRP_RFC.md` — render-pipeline RFC and measurable decision gate.
6. `06_WORLD_TERRAIN_FOLIAGE_SPEC.md` — world-building placement and procedural contract.
7. `07_MATERIAL_LIGHT_WEATHER_SPEC.md` — material/shader/weather/lighting standards.
8. `08_TREE_HARVEST_PHYSICS_CONTRACT.md` — tree asset ↔ axe physics/gather/save integration.
9. `09_PERFORMANCE_BUDGETS.md` — budgets/hardware/profiling and quality tiers.
10. `10_HERO_SLICE_IMPLEMENTATION.md` — reversible 120×120 m delivery with checkpoints.
11. `11_QA_VISUAL_ACCEPTANCE.md` — measurable signoff matrices + owner-only tests.
12. `12_SPRINT_ROADMAP.md` — phased schedule and dependencies.
13. `13_ROLLOUT_AND_ROLLBACK.md` — branch, asset, migration and preservation strategy.
14. `14_RISKS_AND_OPEN_DECISIONS.md` — risks, blockers, technical debts.
15. `15_DOCUMENT_CROSSWALK.md` — old documents vs. new supersession.
16. `16_CODEX_HANDOFF_PROMPT.md` — implementation-agent prompt with context discipline.
17. `17_REFERENCES_AND_SOURCES.md` — source-of-truth links and evidence quality.
18. `18_ACCEPTANCE_RECEIPTS.md` — reusable vendor, visual, performance and sprint receipts.

## Historical document policy

- Existing S013, LS-DOC-19, LS-DOC-20 and BLD governance docs are delivered as complete **append-only, non-destructive updated versions** at their existing repository paths; their legacy assertions remain historical context, and the dated addenda direct readers here.
- Historical comparison evidence, former asset adoption and prior runtime acceptance are not erased/relabelled as failures. Acceptance was relative to the earlier standard.
- Source `.docx` GDD v0.1 remains untouched (binary file deliberately excluded); a versioned art addendum governs the art-only portion without forging a binary update.
- Older production-plan S023/BLD-S023 cards are not rewritten as 'DONE'; use the crosswalk and new roadmap for the additional art work.

## No-change zones and completion definition

No source Unity scenes, terrain assets, `.meta` GUIDs, player prefabs, save data, assembly definitions, physics code, scripts or packages are changed by this ZIP. Do not run scene generation or global material conversion when installing the docs. Art-direction adoption is complete when local documentation merges pass review; *visual migration* completes only when owner captures and real hardware measured gates pass. Zero screenshots or benchmark output are fabricated in this delivery.
