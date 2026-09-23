# S002 / P00 reconciliation

S002: PARTIAL. P00: PARTIAL; entry into full canonical P01/P02 is not certified. The current save gate closes a real foundation without claiming all historical scope is complete. The gap matrix retains the original entry assessment and a separate recovery delta.

Sources: canonical sprints/S002.md, phases/P00.md, management/02_Kapsam_Kararlar_ve_Degisiklik.md (PLAN-ADR-006), system 07_Item_Inventory_Equipment.md and actual S005–S008 implementation contracts. Canonical files remain historical planning sources; this report is the current evidence reconciliation.

| Card | Current decision and evidence | Remaining gate |
|---|---|---|
| D011 catalog | Six production resource definitions with stable IDs; no filler definitions added. Rifle is the accepted current firearm. | Useful catalog expansion tied to real verbs; twelve-item target remains open. |
| D012 definition/instance | Immutable definitions and independent quantity/container state exist; persistent world entity IDs now separate from definition IDs. | Two equipment instances with independent condition are not implemented. |
| D013 capacity | Fixed slot/stack limits preserved and validated before restore. | PLAN-ADR-006 still specifies slot + weight. Actual S005 deferral is not permanent cancellation. |
| D014 pickup | Scoped barrier, staged world/container commit and existing observer exceptions preserved. | See acceptance XML; no new gameplay requirement invented. |
| D015 move/split/merge | Existing domain suite retained; real saved route exercises split/drop. | No duplicate implementation needed. |
| D016 inventory UI | Existing binding retained; restored slots publish coherent snapshot. | Weight display follows weight authority, not fabricated UI data. |
| D017 bag/equipment | No bag-size or equipment-condition authority. | Explicit next bounded gate after mass/capacity: bag removal cannot delete items; reject overflow. |
| D018–D020 persistence | Current-world snapshot, safe disk boundary and actual saved route implemented. | SAVE_ACCEPTANCE.md controls PASS; canonical twelve-definition breadth remains D011's open scope. |
| D026 melee | Rifle combat and zombie melee are verified. No accepted decision permanently cancels player crowbar. | Player melee remains STILL_REQUIRED until product owner records a superseding decision. Do not add it inside persistence. |
| D029 persistent death | Current encounter's zero-health state survives load. | Corpse loot/container and generalized persistent actors remain partial. |
| D030 fairness | Automated encounter regressions and standalone route. | Qualifying human observation is not evidenced. |

G0 requires the first real save roundtrip and encounter invariants. Passing those technical checks does not retroactively implement weight, equipment, twelve useful definitions or player melee. No planning schedule or phase title overrides missing behavior.

Next concrete gate: **S002 D013 mass/capacity foundation and its save compatibility policy**. Introduce immutable per-definition mass and validated container limits as a bounded extension of the existing transaction API; preserve fixed-slot semantics, test exact threshold/one over/partial pickup, and define a content/schema compatibility decision before accepting older saves. Do not invent twelve filler items or silently remove PLAN-ADR-006. This implementation is not started in the current save recovery gate.

Then D017 bag/equipment and D012 condition, D011 useful catalog, D026 melee decision/implementation and P00 evidence review. D038 survival-loop save remains partial because wounds/death recovery do not yet exist. Medical consumables, survival needs and rifle gameplay noise/hearing remain subsequent explicit gates. Human pilot D039 is BLOCKED_EXTERNAL; it can be scheduled separately from technical implementation. Death bag is an unresolved later survival policy, not a fabricated persistence default.

Actual labor/reserve consumption was not measured; no estimated-hours value is presented as actual. No permanent product scope cancellation was made by this run.
