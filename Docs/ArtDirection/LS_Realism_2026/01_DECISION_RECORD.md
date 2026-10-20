# ADR LS-ART-2026-001 — Realistic Environmental Art Direction

**Date:** 2026-10-19 · **State:** OWNER-CONFIRMED ART GOAL / IMPLEMENTATION NOT_VERIFIED · **Owner:** Last Signal product owner · **Supersedes (art only):** S013 `stylized realism` and LS-DOC-19 painted/mid-frequency rule as final acceptance direction. **Does not supersede:** gameplay and technical safety contracts.

## Context

Owner provided current `FirstTownCore` aerial screenshot on 2026-10-19: repeatedly similar pine silhouettes, flat vivid green undergrowth, sharp road-to-terrain edge, conspicuous perimeter fence, little forest-floor detail. This is **single Scene View evidence** — not proof of actual PlayMode FPS, weather, close-up materials or Windows performance. Owner expressly chooses a visual destination close to Sons of the Forest. Historical approved Fantasy Landscape family was fit-for-purpose at the time; it is now *LEGACY/REASSESS*, not a permanent prohibition on realistic assets.

## Decision

Adopt **grounded realistic survival art** (naturalistic PBR environment, believable temperate/conifer forest, abandoned rural settlements, credible organic terrain, cinematic yet physically coherent weather/lighting). Primary target: photorealism-adjacent world; art consistency and playable readability matter more than screenshot-only density. Keep story's rural abandoned town identity; avoid replicating another game's distinctive designs or using its assets. Trees, characters, vehicles, loot and buildings must be harmonized to one material and scale standard. Mixed visual generations without documented normalization are rejected.

**Render-path decision:** Keep Unity 6000.5.0f1 URP 17.5.0 for production **until** same-content URP/HDRP comparison on **separate** project passes gate. HDRP not yet approved. Explicit NO upgrade of Unity/editor/packages as incidental art task.

## Options

| Option | Benefit | Cost/risk | Position |
|---|---|---|---|
| Continue painted Fantasy Landscape | cheap, tested plumbing | misses owner realism target | REJECT as final hero look |
| Replace visual resources on URP | reuse current authored systems, scalable PC | custom atmosphere and foliage shading | ACTIVE PATH |
| HDRP in separate comparison project | richer high-end lighting/fog workflow | shader/volume/sky/authoring migration, perf | EXPERIMENT |
| Immediate main-project HDRP conversion | early access to features | unbounded breakage/no baseline | PROHIBITED |
| Full map remodel first | complete end-state sooner if works | large sunk cost before style validation | PROHIBITED |

## Invariants (MUST)

- WorldClock, WorldSimulation and TownAtmosphere ownership remains unique; do not add a competing day/weather system.
- Existing `FirstTownCore` production source, S013Cabin, IDs and save schema are preserved. Checkpoint scene, terrain and persistent entity IDs before any later art work.
- Gameplay line of sight, navigation, zombie threat cues, loot legibility, roof protection, vehicle clearance, axe/gather ownership are unchanged unless a separately approved game-design decision exists.
- Vendor source preserved unmodified; project-owned asset variants under `Assets/LastSignal/Art/`; `Assets/ThirdParty` purchase/license requirements remain.
- Users run Unity commands, QA tests, screenshots and benchmark manually; agent defines them but does not mark PASS from authoring alone.
- Asset cost, license, rigging, shader/LOD and evidence are evaluated per item; free-store label alone is not product-readiness.

## Phase gate

`GO` to expand beyond the 120×120 m reversible pilot only when owner signs appearance, Windows target p95 performance, forest interaction and navigation/save regressions. Otherwise `ITERATE` or `REVERT`. Updating these docs is not a GO by itself.

## Status and unresolved

Game art target: **CONFIRMED**. Specific biome/vegetation species mix: **PROPOSED**. Platform minimum GPU / VRAM: **OPEN**. URP vs HDRP: **EXPERIMENT**. Asset license of previously acquired packages: **PENDING evidence**. Final render aesthetic: **NOT_YET_APPROVED**. Planned ship date: **UNSPECIFIED**.
