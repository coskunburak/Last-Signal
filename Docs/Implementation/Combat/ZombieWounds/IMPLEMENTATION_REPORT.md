# Implementation report

**IMPLEMENTATION COMPLETE — FOCUSED EDITMODE ACCEPTED; PLAYMODE RERUN PENDING.** Not production accepted.

Date: 2026-10-11. Branch `s012-integrated-graybox-slice`, HEAD `c41dcf1a0e0942377556227e938ef847391623bb` at baseline and continuation inspection.

## Recovered state and completed work

The initial local tree already had accepted/fatal damage receipts, newer rifle obstruction handling, per-hit Vefects blood, a hitbox-anchored localized overlay, directional reactions and visually gated anatomical severing. Those local versions were preserved and extended. The user's continuation arrived during implementation; work resumed from these current files without resetting discovery or replacing the working tree.

At continuation, triangle mapping, profile, blood/corpse route, mesh derivatives, tests and a draft runner were present. Static serialization inspection confirms all 15 project-owned derivatives have `m_IsReadable: 1` and GUID references in the production prefab. The authoring utility explicitly sets readability on derivatives; vendor meshes remain untouched.

Final integration includes stable mesh-space overlays and triangle anchors, bounded merging, head/ribcage dents, clothing shading, per-impact temporary bleed/drip, a nearest-surface corpse route, bounded retention, gore parity and optional versioned wound save state. The legacy fixed torso patch is suppressed for bullet-localized injuries, including after save/load/gore toggling.

The final persistence review added a retained encounter terminal snapshot and moved snapshot consumers to `CaptureEnemy`, allowing save after corpse destruction. Health hydration now uses `StateRestored`, not the combat `Died` event. Its controller, encounter and blood subscriptions have matching unsubscribe paths. This prevents restoring a completed encounter from issuing a new kill notification. A real-scene persistence test was added for kill → body removal → save → load → save again.

## Compact discovery / gap matrix

| Area | Baseline | Final work |
|---|---|---|
| Shot commit and physical hit | Ready/ammo gate, camera/muzzle rays, single nearest collider already present | Preserved; corpse contacts compete by distance, camera fallback invalidated when aiming at corpse |
| Owner, region, overlap | Explicit region owner/multiplier; nearest collider dispatch | Added recent shot/sub-hit dedup and generation checks |
| Ordinary/fatal health | Health/anatomy commit then receipt; fatal receipt before death | Preserved; cosmetic failures cannot undo HP |
| Existing blood/sever | Bounded Vefects and detached-part pools | Reused hit slots; bleed/drip added; sever tuning/gates retained |
| Mesh/material | 15 split skinned surfaces, flesh plus trouser/foot materials | Project-owned readable derivatives with rest-position/normal channels; visible clothing receives overlays |
| Mapping | Hitbox transform; lower-leg names missing | Triangle/barycentric contact; LowerLegL/R covered; bounded grazing fallback |
| Depth/progression | Single flat localized mask; legacy torso patch | Lit layered wound, merge/severity/drying, local head/ribcage dents; no topological cutting |
| Corpse lifecycle | All colliders disabled; short bleeding-dependent lease | Mesh-only receiver, 8 bodies / 30 s, no collider reactivation or health writes |
| Save/streaming | Anatomy already persisted; encounter required an Actor | Optional wound extension and compact terminal record; restoration separated from kill notification |
| QA/handoff | Existing combat and blood tests; no wound gate | Extended focused tests, explicit one-gate runner, coverage/manual QA/resource notes |

D01–D12 are implemented for the supported production surfaces. Source-art-dependent approved cut surfaces and post-mortem detachment remain blocked/deferred as documented, without blocking wounds, blood, corpse reception or the handoff.

## Feature acceptance matrix

| Feature | Implementation | Acceptance |
|---|---|---|
| Location-accurate / persistent animated wounds | IMPLEMENTED | STATICALLY INSPECTED; runtime NOT_RUN |
| Layered realistic surface shading | IMPLEMENTED | Visual QA NOT_RUN |
| Local deformation | IMPLEMENTED: Head/Ribcage, ≤3 mm | Runtime/visual NOT_RUN |
| Clothing damage | IMPLEMENTED: visible fabric surface/depth shading | True torn topology DEFERRED; visual NOT_RUN |
| Progressive damage | IMPLEMENTED: local merge, severity, radius, dents | Runtime NOT_RUN |
| Anatomical severing | Existing authority/pools preserved | Approved cap/cut art BLOCKED; production visual gates preserved |
| Live bleeding and environmental marks | IMPLEMENTED | Runtime/visual NOT_RUN |
| Corpse bullets and local post-mortem progression | IMPLEMENTED | Runtime NOT_RUN |
| Post-mortem limb detachment | DEFERRED | Source-art approval BLOCKED |
| Gore and reduced effects | IMPLEMENTED | Parity/cleanup tests NOT_RUN |
| Save/load and encounter terminal records | IMPLEMENTED | Tests NOT_RUN |
| World streaming and session cleanup | IMPLEMENTED | Tests NOT_RUN |
| Resource budgets/instrumentation | IMPLEMENTED | Performance QA NOT_RUN |
| Production prefab bindings | IMPLEMENTED | STATICALLY INSPECTED |
| Tests | Written/extended | Focused EditMode accepted, 147/147. First focused PlayMode: 72/78 passed; six failures addressed, rerun pending. Later gates NOT_RUN |
| Compilation verification / build | Prepared user gates | No independent gate/build run |
| Manual visual acceptance | Checklist prepared | NOT_RUN |

## Evidence boundaries

The user ran the first focused EditMode gate on 2026-10-11. Its 147 cases produced one failure: the legacy `TorsoWound_Visual` skin retained Auto quality while its Ribcage parent used Bone4. The production prefab and repeatable wound authoring path now keep those qualities equal. The logged `Expected blood presentation failure` exception belongs to a passing test that deliberately injects that failure. The original evidence remains in `Evidence/20261011T074706-568510Z-focused-edit-796cca1d`.

The user's focused EditMode rerun in `Evidence/20261011T075055-385736Z-focused-edit-ca423e28` exited 0, with 147/147 XML cases passed, zero skipped, zero diagnostics and all required methods discovered. The `REVIEW_REQUIRED` marker was due solely to the same exception candidate. Source inspection confirms that `PresentationExceptionCannotPreventFatalHealthCommit` deliberately throws it and calls `LogAssert.Expect`; XML shows that test passed and no other case contains the exception. This gate is manually accepted. Focused PlayMode is next.

The user's first focused PlayMode run in `Evidence/20261011T075227-454716Z-focused-play-02b06061` exited 2: 72/78 cases passed, six failed, none skipped, no compiler diagnostics. Review found one stale duplicate-presentation expectation, a fixture assigning a `GameObject` where `ZombieController` is required, two point-location errors caused by entry-ray selection before nearer triangles, one attachment test that did not impose a measurable arm pose, and a restore race before starting equipment was initialized. Those paths have been corrected in source and tests; a new focused PlayMode run is required before any regression gate.

Agent work: source/serialization inspection, scoped Git comparisons, Python source parsing and two targeted Editor authoring executions to create/bind derivatives and correct readability. Editor imports/compilation incidental to authoring are not a compilation gate or a runtime test result. The agent has run no EditMode/PlayMode tests, Test Runner, automated gameplay, benchmark or build. No screenshots, frame rates, PASS counts or build reports were fabricated.

`INITIAL_STATUS.txt`, `INITIAL_HASHES.json` and `CONTINUATION_STATUS.txt` distinguish existing work. `CHANGED_FILES.md` lists only this task's scope; `STATIC_INSPECTION.json` records serialization checks. No reset, clean, restore, stash, commit or push occurred. Start with the first command in `RUN_COMMANDS.md` and return its evidence before proceeding.
