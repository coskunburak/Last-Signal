# S019 POI production checklist

POI / scene: __________ Owner: __________ Source revision / dirty-state evidence: __________
For each row record PASS, FAIL, NOT_RUN or N/A **with reason and evidence path**. Default NOT_RUN. A screenshot alone cannot close a POI.

| Check | Acceptance |
|---|---|
| Provenance | Existing approved kit/loot/encounter assets; no vendor source edits; material pipeline supported |
| Stable identities | All required IDs valid; no duplicates after ordinary scene/prefab duplication; IDs unchanged across reopen/load |
| Entry/retreat | Markers inside POI hierarchy, safe standing volume, readable ingress and an unobstructed retreat |
| Navigation | Native surface baked for the actual zombie agent; entry, loot approach and encounter spawn reachable; no seam trap |
| Collision | Floor support and wall collision, doorway clearance, upright unit-scale loot anchors; renderer/collider agreement |
| Loot | Catalog/profile references valid; preview totals match actual selector; seed repeatability, empty/blocked behavior understood |
| Encounters | Existing encounter prefab/spawn refs; persistence identity; actual session/cell supports the intended count and ownership |
| Lighting | No unsupported/pink material, acceptable indoor/outdoor exposure and readable interactables |
| Audio | Existing mixer/routing/occlusion policy; N/A only with an explicit reason for a silent POI |
| Save/load | In a supported gameplay scene, save → exit process → load retains doors/items/encounters/ownership/progression; no duplication |
| Import | Opt-in Preset captured; repeated scoped reimports preserve scale, color/normal interpretation, shader mappings, colliders and audio intent |
| Performance | Record platform, build, quality, frame/memory/renderer observations; compare measured budget, not editor impressions |
| Validation | Project and destination scene validator errors resolved; focused tests/regressions/build evidence recorded |
| Visible acceptance | Walk complete ingress → interaction/loot → retreat route and inspect art/collision/navigation together |
| Measured cost | Actual manual actions, elapsed authoring interval, code changes, caught defects and rework recorded; unknown baseline stays UNKNOWN |

Pilot: `Assets/LastSignal/Prefabs/S019/S019RuralStore.prefab`. Its native navigation is unbaked and it contains no session/save/population owner. Runtime save acceptance is N/A **in isolation**, not PASS. Use the existing supported production route for regression and a disposable scene/profile for deliberate pilot integration; do not overwrite S013Cabin or old saves. Overall pilot acceptance NOT_RUN; D189 comparison NOT_MEASURED.


Production follow-up: after the user-run authoring command, use `Assets/LastSignal/Scenes/S019/S019Pilot.unity` for gameplay acceptance. It has the existing SessionFlow/LootPopulationService/SaveSession and one encounter, a new scene-wide native bake, and isolated `saves/s019-pilot.json`. Runtime save acceptance is no longer N/A in that generated scene; it is NOT_RUN until observed. Original template-only caveats still apply when inspecting the prefab in isolation. Record authoring.json (3 presets/6 reimports/5 routes), production-edit and pilot-play evidence, visible/audio/collision checks, and the new matched manual-versus-template timing comparison separately. Only observed criteria may be PASS; do not use historical regression counts to accept these new source changes.

## Current verified automated evidence (2026-10-09)

Technical authoring A02 PASS (3 presets, 6 reimports, 5 routes); production EditMode P02 12/12 PASS; pilot L02 1/1 PASS; current full EditMode E01 604/604 PASS; current full PlayMode R08 315/315 PASS. Evidence paths are in S019_HANDOFF.md. These verify automated contracts only. Manual rendering/audio/collision/gameplay, process-restart acceptance, measured authoring comparison and build/performance acceptance remain pending; build deferred by user.
