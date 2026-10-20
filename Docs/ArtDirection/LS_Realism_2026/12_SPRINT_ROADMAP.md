# Art migration production roadmap — schedule hypotheses

**Program is new art program LS-AR-01..08, not renumbering existing S013/S023 gameplay sprints.** Estimates assume one primary developer with AI-assisted asset mapping/code. Technical asset import and owner-run QA may dominate; numbers are planning ranges, not deadlines. Work may proceed only when prior gate satisfies invariants.

| Workstream | Duration estimate | Output and acceptance gate | Dependencies |
|---|---:|---|---|
| LS-AR-01: Governance and decision | 0.5–2 days | merged docs, ADR/Art Bible acceptance | owner art decision (done) |
| LS-AR-02: Local asset and shader audit | 2–5 days | KEEP/ADAPT/REPLACE registry, vendor license receipts, shader architecture | first audit |
| LS-AR-03: Source acquisition and visual lab | 3–8 days | 3+ tree/cuttable candidates, terrain materials, foliage and quality/parity screenshots | legal source access |
| LS-AR-04: 120×120m terrain+forest slice | 2–6 weeks | believable terrain/forest, eye-level screenshots and route functionality | art source gate |
| LS-AR-05: Weather/light/material harmonization | 1–3 weeks overlapping | same camera day/rain/dusk/night, no secondary weather authority | pilot imported |
| LS-AR-06: Physical tree compatibility | 1–4 weeks if nontrivial geometry | cut wedge, realistic fall, save/loot safety | cuttable mesh qualification |
| LS-AR-07: Cross-platform perf + pipeline RFC | 3–7 days minimum measured work | Windows/Mac, 1080p p95/p99; URP/HDRP owner decision | representative content ready |
| LS-AR-08: FirstTownCore district rollout | 1–3+ months | staged district promotion and visual/perf/save acceptance | signed hero-slice GO |

**Schedule/critical path:** tree geometry-source fitness → hero forest assembly → cut physics compatibility + shader/depth integration → visual signoff → Windows target performance → production promotion. HDRP trial is conditional, not prerequisite to start realistic art work.

## Milestones / gates

**M0 — Documentation baseline**: old source preserved, revised documents linked, old art standard superseded for new work only, no Unity changes.
**M1 — Asset intake:** at least 3 realistic tree variants and 2 ground families in one neutral-lit URP scene with eligibility evidence; gap list for missing models.
**M2 — Hero scene:** 120×120 natural terrain, ecological foliage, road/cabin and 4 weather states, 12 fixed camera cases.
**M3 — Interaction:** hero tree reacts to cuts and fall with resource/persistence correctness; not necessarily every forest tree on first pass.
**M4 — Performance:** minimum hardware and benchmark signed; pipeline ADR closed.
**M5 — Rollout:** district-by-district with source-hash protection and retained old saves.

## Avoid schedule traps

Don't perform HDRP conversion and tree fracture refactor in same sprint. Don't bulk scatter before LOD/material economics are known. Don't evaluate screenshots in Scene View only. Don't conflate compile PASS with screenshot PASS, or vendor listing free with lawful downloaded license. If assets are unsuitable, use limited custom Blender retopo/LOD work *before* committing to new pack hunting. If owner rejects close-up bark quality, don't expand to entire map.
