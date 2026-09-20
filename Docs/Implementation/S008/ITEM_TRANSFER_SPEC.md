# Item transfer specification
ItemTransferService.Deposit(PlayerInventory, ShelterStorage, ItemDefinition, requested) and Withdraw(reverse) operate synchronously on the Unity main thread. ShelterLoop.Transfer is the gameplay access gate: living current player, Preparing state and open preparation panel. Low-level service is independent of scene UI for deterministic tests and future adapters.

Result includes Requested, Moved, Remaining and Reason: Complete, Partial, InvalidRequest, SourceEmpty, DestinationFull, Busy or Unavailable. Remaining is max(0, requested)-Moved. Invalid/nonpositive input moves zero. Insufficient source moves only available quantity, not a negative stack. Full destination is an unchanged no-op.

Validation rejects identical containers, missing/invalid definitions, incompatible same-ID aliases and reentrant transfer notifications. Determine available quantity and destination capacity first; moved=min(requested,available,capacity). Reuse existing add/remove methods with notifications suppressed. Both owners commit before callbacks. No asynchronous operations or external calls occur between the commits. An unexpected source rejection removes the accepted destination amount before returning failure. Normal validated main-thread commit cannot encounter that branch without a programming defect.

Transfer notifications lock both containers against observer mutations/reentry, publish each owner once, then release in finally. Exceptions in notification handlers are logged after commit and do not roll back ownership or suppress the other owner's notification. Source+destination remains constant, including partial/full/invalid cases. Presentation allocations are separate from domain work.

Example: carry20 + stash53 in one 60-round slot, request20 => carry13/stash60, moved7/remaining13. Reverse: stash30 + carry50 in one slot, request20 => stash20/carry60, moved10/remaining10.

Only reserve ammo stacks move. Loaded rounds never appear as stash/inventory slots. Withdrawal changes S007 reserve through PlayerInventory, and only the normal reload transaction moves it into the magazine. Deposit never unloads a weapon.
