# Document impact and precedence matrix (GitHub 2026-10-19)

**Priority:** user's newest explicitly confirmed art direction > ADR LS-ART-2026-001 > new art bible and matching acceptance contracts > preserved GDD v0.1 historical art language > prior S013/B/Fantasy Landscape acceptance for older builds. Technical gameplay/save contracts remain in force throughout. All new status claims must include implementation evidence, not just documentation.

| Existing repo path or group | Historical behavior | Change action | Precedence status |
|---|---|---|---|
| `Docs/Implementation/S013/S013_ART_BIBLE.md` | painted stylized realism, Fantasy Landscape family | full file copied into ZIP with dated **art-only addendum** | older selections LEGACY, not final quality gate |
| `Docs/Last_Signal_Docs_v0.2/sources/19_Art_Audio_Asset_Pipeline.md` | The Long Dark reference, mid-frequency painted | full file with future-asset normalization addendum | new realistic art bible supersedes conflicting aesthetics; rig/sfx rules remain |
| `Docs/Last_Signal_Docs_v0.2/sources/20_Performance_Build_Operations.md` | initial URP budgets/PC testing | full file with realism benchmark addendum | all existing perf correctness retained |
| `Docs/BLD_Production_Knowledge_Base_v2_2026-10-17/00_GOVERNANCE/05_ASSET_ART_RULES.md` | Last Signal stylized realism | full file with v1 realism policy addendum | new quality target for future art tasks |
| `Docs/Last_Signal_Docs_v0.2/sources/01_Kararlar_ve_Kapsam.md` | ADR-002 URP baseline | **retain**, refer to new ADR and separate pipeline RFC | URP baseline still accurate; HDRP experimental |
| `Docs/Implementation/S013/S013_ASSET_REGISTRY.md` | historical vendor license and S013 decisions | **retain as immutable evidence**, new registry added here | old vendor and rights facts not rewritten |
| `Docs/Implementation/S013/S013_ASSET_COMPARISON.md` | past painted-vs-painted benchmark | **retain historical comparison** | not new art acceptance |
| `Docs/Implementation/FirstTownCoreForest/{HANDOFF,DESIGN_AND_ASSETS,PERFORMANCE_PLAN,MANUAL_QA}.md` | forest stage/production and URP shaders | **retain as code/evidence source**, new tree/forest spec for future work | don't claim replaced implementation |
| `Docs/Implementation/FirstTownAtmosphere/{HANDOFF,MANUAL_QA}.md` | clock/sky/rain/wetness/probes | **retain**, new weather spec adds quality acceptance only | single authority preserved |
| `Docs/Implementation/FirstTownVisualSlice/REFINEMENT_05.md` | stage-only gas canopy pending visual acceptance | **retain**; don't automatically promote | hash gate remains |
| `Docs/Last_Signal_Town_S023_Asset_Research_2026-10-15.md` | 500m town and free asset survey | **retain as historical candidate record**, rescreen photo-real assets | no new environment package assumed acquired |
| `Docs/Last_Signal_Production_Plan_v1.0_TR/sprints/S023.md` | historical second-region planning | **retain**; art road map separate namespace | no sprint history rewrite |
| `Docs/BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P08_.../BLD-S023.../SPRINT.md` | artist kit/snap, `PROPOSED` | **retain**; realism art precedence new | statuses still PROPOSED unless proved |
| `Docs/BLD_Production_Knowledge_Base_v2_2026-10-17/PHASES/P08_.../BLD-S024.../SPRINT.md` | FX/weather/fall visual tasks | **retain**, new 08 tree physics acceptance supplements | not implemented automatically |
| `Docs/Project_Last_Signal_Production_GDD_v0.1_TR.docx` | binary source GDD historical | **do not overwrite**, art-only v1 addendum above | full GDD revision optional separately |
| `Packages/manifest.json` | URP 17.5 installed | **do not edit** | HDRP not authorized |

## Later update policy

At successful visual pilot acceptance, write a separate `VISUAL_ACCEPTANCE_RECEIPT` referencing project/scene/art assets. Then update BLD day cards and affected phase scope with new requirement IDs and honest PASS/PARTIAL. Do not rewrite all 400+ historic daily cards merely because art direction changed. Current producer can choose a new consolidated GDD version later, rather than silently patch binary GDD without source review.
