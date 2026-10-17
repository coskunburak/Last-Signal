# FirstTown visual slice implementation — 2026-10-16

**Implemented as a reversible, owner-run native authoring pass. Owner generation is recorded; visual acceptance is pending.** The assistant has not modified the production scene. A later owner/session save changed its bytes relative to the initial generation; preserve that file and reconcile before regeneration or promotion. The original 120 m-square brief was adapted only after the owner explicitly authorized a narrow corridor between the existing, approximately 260 m-separated safehouse and gas station.

The implementation contains the actual terrain, road, architectural mesh, asset placement, material, vegetation and atmosphere operations. It is not a documentation-only plan. The owner executed generation at approximately 14:29 Istanbul on 2026-10-16. The saved stage, generation assets and authoring report exist. Their existence and recorded instance counts do not prove production visual quality.

**Current refinement status:** Owner captures confirm the Lighting03 color-export repair, but expose the road UV-atlas misuse and oversized fences. Refinement04 applies compatible asphalt/normal textures and stronger named practical lights to existing assets. Its owner-operated **2b Apply road and fence refinement (existing stage)** command replaces only oversized compound fence runs, sets them behind the road, and relocates the approach pole. The scene command has not been executed by the assistant. See `REFINEMENT_04.md`; next comparison is **04 Day, 04 Dusk, 03 Sunset**. No regeneration or promotion.

## Implementation

| Requirement | Source implementation | Native / visual status |
|---|---|---|
| Preserve current scene and unrelated work | Stage copy, immutable generation assets, checkpoints, hash guards, protected gameplay component comparison, explicit backed-up promotion | Owner generation report exists; original scene meta/player prefab unchanged; source scene subsequently differs from initial hash |
| Safehouse surroundings and forest transition | Scoped replanting, organic trail, road shoulders, abandoned yard detail; existing cabin, shed, fences and supply points retained | IMPLEMENTED_IN_SOURCE; ground/contact/composition awaiting capture |
| Main road / station approach | Existing route and bridge retained; road segments use consistent world-space UV scale inside focus; original UV/material retained outside; apron/junction stretched cube render replaced | IMPLEMENTED_IN_SOURCE; no tested driving or nav claim |
| Terrain / material blending | Clone existing TerrainData; bounded shallow berm/drainage relief, four-layer local splat blend, existing layers/pixels outside focus preserved | Owner-generated Terrain.asset exists; current bytes differ from recorded hash; cause unverified |
| Hero architecture | Existing vendor station canopy/pumps/office preserved; service workshop gets pitched roof, gables, ridge, standing seams, fascia/bargeboards, corner boards, gutter/downpipes and imported awning | IMPLEMENTED_IN_SOURCE; no visual acceptance yet |
| Environmental storytelling | Reused wrecks/pallets/fences, selected vendor bin/tyre/extinguisher/electrical panel; uniform scale and conservative clearance checks | Conditional placement; skipped props reported as incomplete |
| Vegetation / ground clutter | Existing approved wind/LOD pine, birch, bush, grass, rock and bark assets; seeded clusters/age bands; old scoped generated plants disabled; terrain grass replaced only in scope | Owner report: 81 trees, 230 shrubs, 60 rocks, 5,788 grass; no FPS claim |
| Signage | Scoped lettering rebuilt on both board faces, fitted to boards; source boards, gameplay IDs and HUD retained | IMPLEMENTED_IN_SOURCE; sign orientation/readability unverified |
| Sky / lighting / weather | Optional calibration on existing TownAtmosphere; owned sky/lighting assets; new exterior renderers registered for its wet response; no new simulation owner | IMPLEMENTED_IN_SOURCE; runtime rain, exposure and night acceptance NOT_RUN |
| Rain roof integration | Existing roofs retained; new service roof and thin collision-only contacts under station canopy/awning bounds use existing RainRoof contract | Must inspect roof bounds and verify actual protection in PlayMode |
| Visual comparison | Six fixed 75°/orthographic compositions, baseline-grounded once; 1080p manifests; day/sunset/overcast/rain-lighting/night/inspection-flashlight samples | Owner-operated Editor renders only; none captured by assistant |
| Gameplay / save / vehicles / zombies | Existing IDs and references retained; optional scratch review slot, restored at promotion | Manual verification NOT_RUN |
| Navigation | Invalidated only in stage; dedicated stage-only owner bake operation | REBAKE_REQUIRED after generation |
| Performance | Bounded budgets, shared source assets, no per-tree runtime logic | NOT_MEASURED |

## Modified source

- `Assets/LastSignal/Scripts/Runtime/Atmosphere/TownAtmosphere.cs`: optional lighting calibration, plus explicit lighting sampler used by the owner's capture fixture. Null calibration retains the existing runtime lighting constants. Weather, clock, persistence, camera/player/weapon and combat authority are unchanged.

## Created implementation files

Each new C# file and each new script directory has a new `.meta` with its own GUID. Existing `.meta` files were not rewritten.

- `Assets/LastSignal/Scripts/Runtime/Atmosphere/TownLightingProfile.cs` — optional visual calibration asset type.
- `Assets/LastSignal/Scripts/Runtime/VisualSlice/FirstTownSliceProfile.cs` — footprint, route, trail, deterministic budgets and fixed camera definitions.
- `Assets/LastSignal/Scripts/Runtime/VisualSlice/FirstTownSliceState.cs` — serialized ownership/rollback records; no runtime update logic.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceAuthoring.cs` — transactional staging, restoration, weather registration, protected-component checks, stage nav bake, review save isolation, accepted-stage promotion.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceSite.cs` — scene-derived topology, collision/pad/route/river exclusions and bounded vegetation suppression.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceGround.cs` — local terrain shaping, splat blending and grass details.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceAssets.cs` — reuse registry, bounds-based placement and URP conversion for selected new vendor props.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceMesh.cs` — persisted, metre-scaled UV meshes for road surfaces and architectural detail.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceComposition.cs` — road, roof, facade detail, stories, forest and sign composition.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceVisualReview.cs` — explicit owner-operated comparison window and capture manifest.

Created supporting files in `Docs/Implementation/FirstTownVisualSlice/`:

- `AUDIT_AND_PLAN.md`: source evidence, P0/P1/P2 priorities, real-coordinate composition, ownership and fidelity limits.
- `ASSETS.csv`: 31 verified local source/candidate paths and hashes, uses, verdicts and risks; rejected/benchmark candidates are explicitly distinguished from selected content.
- `COORDINATE_PLAN.svg`: endpoint/corridor diagram, clearly not a rendered scene.
- `MANUAL_ACCEPTANCE.md`: all exact menu operations/methods, six cameras, condition matrix, detailed visual/gameplay checklist, regeneration and recovery procedure.
- `entry-hashes.json`: baseline hashes captured before edits.
- `STATIC_REVIEW.md`: checks performed without invoking Unity and their limits.
- `changed-files.txt`: complete task-owned file list including `.meta` files.
- `IMPLEMENTATION_REPORT.md`: this report.

The owner-generated scene is `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity`; assets are under `Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538/`. See `Evidence/authoring-20261016-112859-538.json` and its matching assets TSV for recorded results and actual asset provenance. One bin placement was skipped for clearance. The assistant has not executed native authoring or rendered this stage.

## Assets selected

Exact paths and hashes are in `ASSETS.csv`. Main sources:

- `Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/{Pine,Birch,Bush,Grass,Rock,Log}.prefab` and its existing grass/soil/road/rock layers. The tool reads the actual installed forest generation from the scene rather than pinning future generations to this timestamp.
- `Assets/LastSignal/Art/FirstTown/Prefabs/{Canopy,Pump,GasOffice,GasSign,Cistern}.prefab`: retained existing station composition.
- `Assets/LastSignal/Art/FirstTown/Prefabs/{Wreck,Pallet,Fence,Awning}.prefab`: reused directly without duplicate wrappers.
- `Assets/ThirdParty/VGtiXN/Countryside gas station/Prefabs/Trash_can/Trash_can.prefab`, `wheel.prefab`, `Fire_extinguisher.prefab`, `Electric_panel.prefab`: project-owned URP derivatives created at generation time.
- `Assets/LastSignal/Art/S013/Production/Materials/{DarkTimber,PaintedTimber,RoofMetal,Metal,Stone,GroundPath}.mat`: established style family. Existing diffuse/detail maps are retained; no invented PBR normal-map claims.

## Limits and remaining risks

- No generated result has been rendered or accepted. Native compile/import errors, bounds/placement errors and visual mismatch can still be found by the owner. This is **not ART_ACCEPTED**.
- Existing stylized plant silhouettes and LODs cannot reproduce the supplied photoreal forest references exactly. Bespoke roots, richer leaf litter, high-quality reduced LODs/impostors and volumetric shafts remain gaps.
- Existing office/canopy and playable service-room modules are retained. Interior rebuilds, new residential houses and unfinished distant districts are outside this endpoint/corridor pass.
- Large props can be skipped to preserve existing clearances. Review the output list before deciding a composition is complete.
- Roof contacts are measured from the existing canopy/awning render bounds and need owner inspection. No user traversal, suspension contact, collider penetration or AI navigation was tested.
- Rain captures in the comparison studio test lighting/wet-surface appearance, not particle simulation or WorldClock exposure. The inspection flashlight is a fixture, not a delivered inventory feature.
- Foliage overdraw, light/reflection cost, new shadow geometry and scene-memory impact are unmeasured. No performance gain is claimed.
- Local asset existence is verified; commercial license/acquisition records are not certified by this pass. Vendor files remain untouched.
- After staging, do not rerun older full-world/forest authoring tools over the same candidate. Their ownership differs. Regenerate through the slice menu or preserve edits in its Artist additions root.

Detailed commands and acceptance criteria: `MANUAL_ACCEPTANCE.md`. The next step is the focused Refinement 01 review described above. Do not rerun generation.
