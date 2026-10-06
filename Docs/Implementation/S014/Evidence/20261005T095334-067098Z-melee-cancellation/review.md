# S014 crowbar cancellation presentation

Static finding: MeleeWeaponPresenter.previous was assigned only in Update. AttackCommitted could play Swing, then Cancel could return simulation to Ready before Update. Both polled states were Ready, so Idle was never requested; production Swing has no exit transition. This is a source-level reproduction, not an executed pre-fix Unity test.

Fix: record Windup synchronously in OnSwing. Existing Ready reconciliation now sees same-frame cancellation. Damage authority, stamina policy, prefab/controller/clips remain unchanged.

Added real Player prefab tests: same-frame explicit cancel; same-frame gameplay input lock; switch during windup and reequip. Assertions cover Idle/Equip/Swing, no delayed active window, and no stamina refund on cancel. Existing authored swing/stance origin test retained. focused-melee selects 4 Crowbar methods; focused-play now selects 9 methods.

Verification: Python AST PASS; static test counts 4/9 PASS. Unity compile/runtime tests NOT_RUN. User command required; previous 6/6 does not certify this revision. Visual acceptance remains open.
