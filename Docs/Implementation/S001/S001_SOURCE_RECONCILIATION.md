# S001 source reconciliation

User production prompt v1.1.0 (2026-09-17) governs this implementation. Actual repository folder is `Docs/` (capital D); preserve its spelling for case-sensitive checkouts.

## Sources read and decisions

All paths below are relative to repository root. Modular system sources are canonical; MASTER_REFERENCE is a duplicated compilation used only for discovery. No Current_State or approved ADR implementation evidence existed.

| Source | Status at discovery | Decision used |
|---|---|---|
| Docs/Last_Signal_Docs_v0.2/README.md | design package v0.2 | Source index; no implementation claims |
| Docs/Last_Signal_Docs_v0.2/reference/GDD_v0.1_Archive.md §4, §7, §21 | historical GDD | First person, 2.2 m interaction, Unity/URP baseline; broader features deferred |
| Docs/Last_Signal_Docs_v0.2/sources/01_Kararlar_ve_Kapsam.md | design_specification / NOT_VERIFIED | Solo first; proposals are not accepted implementation |
| Docs/Last_Signal_Docs_v0.2/sources/03_Teknik_Mimari.md | design_specification / NOT_VERIFIED | Explicit session ownership; no singleton/event bus; stop idempotent |
| Docs/Last_Signal_Docs_v0.2/sources/05_Player_Input_Movement.md | design_specification / NOT_VERIFIED | 3.2/5.5/1.6 m/s, 1.8/1.2 m capsule, radius .3 m, step .3 m, slope 45°, pitch ±85° |
| Docs/Last_Signal_Docs_v0.2/sources/06_Interaction_Door_WorldItem.md | design_specification / NOT_VERIFIED | First occluder, revalidation on commit, busy rejection, door collision |
| Docs/Last_Signal_Docs_v0.2/sources/18_UI_UX_Accessibility.md | design_specification / NOT_VERIFIED | Simple uGUI prompt/context; pause freezes simulation; full localization/rebind deferred |
| Docs/Last_Signal_Docs_v0.2/sources/20_Performance_Build_Operations.md | design_specification / NOT_VERIFIED | Real platform build metadata; no performance claims without measurement |
| Docs/Last_Signal_Docs_v0.2/sources/22_QA_Acceptance_Traceability.md | design_specification / NOT_VERIFIED | Physics/input in PlayMode, build distinct; actual statuses |
| Docs/Last_Signal_Docs_v0.2/sources/24_Production_Roadmap_Backlog.md | design_specification / NOT_VERIFIED | W00-W03 responsibilities mapped to S001; no pickup this sprint |
| Docs/Last_Signal_Docs_v0.2/sources/27_Codex_MCP_Runbook.md | design_specification / NOT_VERIFIED | Tool capability and project identity require real evidence; CLI fallback |
| Docs/Last_Signal_Production_Plan_v1.0_TR/MASTER_PLAN.md | PROPOSED_BASELINE / NOT_VERIFIED | S001 before S002, no calendar-based PASS; later sprint table inspected for boundaries |
| Docs/Last_Signal_Production_Plan_v1.0_TR/phases/P00.md | PROPOSED_BASELINE / NOT_VERIFIED | P00 includes later inventory/combat; S001 does not |
| Docs/Last_Signal_Production_Plan_v1.0_TR/sprints/S001.md | PLANNED / NOT_VERIFIED | D001–D010 and visible build acceptance |
| Docs/Last_Signal_Production_Plan_v1.0_TR/management/04_QA_Kapilar_ve_Yayin_Kabul.md | PROPOSED_BASELINE / NOT_VERIFIED | Critical unrun gates cannot become PASS |
| Docs/Last_Signal_Production_Plan_v1.0_TR/management/07_Kaynaklar_ve_Izlenebilirlik.md (S001 mapping and relevant REQs) | PROPOSED_BASELINE / NOT_VERIFIED | REQ-MOVE-001/002/003, INT-001/003, ARC-002, AGENT-001/002/003 |

## Conflicts resolved

1. LS-DOC-05 and historical GDD include jump, vault and stamina. Current explicit sprint is walking/sprint/crouch; no existing jump motor to retain. Template unused Jump action is preserved, not implemented. No survival stats.
2. LS-DOC-03 proposes six assemblies, catalog and services. Current prompt prohibits framework growth: one focused runtime, one Editor, separate test assemblies; direct serialized references.
3. GDD locks Forward+; LS-DOC-01 marks superiority EXPERIMENT. Existing PC renderer mode 2 and Mobile mode 0 are preserved; no render upgrade or unmeasured performance assertion.
4. LS-DOC-24 W03 includes pickup, but detailed S001 and current prompt limit interaction to door. Inventory/persistence move to S002.
5. GDD Windows/Steam target vs available macOS machine: macOS development validation only; Windows remains BLOCKED for runtime acceptance.
6. MCP package/config exists but server port refused connections and no tool is exposed. Package presence is not connectivity. CLI authoring/validation is explicitly identified, never called MCP evidence.
7. Template Interact used Hold. S001 requires one tap = one request: change to default Button press. Preserve all other template actions/maps and existing action IDs; add Pause.
8. Full localization/gamepad/rebind in LS-DOC-18 is later UX scope; this lab uses Turkish uGUI strings and keyboard/mouse. No second UI framework.

Original design and production packages are preserved as historical/proposed inputs. Implementation updates live in this S001 folder and Current_State.md rather than falsely marking all broader requirements complete.
