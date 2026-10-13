# S021 save compatibility

The outer save envelope, `schemaVersion` and `contentVersion` are unchanged. The existing root `weapon` snapshot remains the rifle compatibility field. `combat.version=2` adds `selectedFirearmId`, an exact list of owned firearm snapshots, and per-firearm condition. Existing `combat.version=1` and absent combat sections continue through the legacy restore path; legacy rifle condition defaults to 1000. Current S021 player composition grants the newly authored pistol at its starting magazine on legacy load. This is an explicit migration default, not ammunition added to the old rifle reserve.

V2 validation requires the known firearm ID set exactly once, capacity and condition bounds, a valid selected ID, and root rifle duplication consistency. Unknown or duplicate ownership fails closed. `SaveSession` restores each magazine/condition and then selected firearm, melee and slot. Repeated decode returns detached data; repeated session-load conservation still awaits user-executed integration evidence.

The 9 mm item uses its own stable ID and catalog entry. Existing world, loot, inventory, rifle, shelter and zombie sections remain in the same envelope. S021 tests cover v2 roundtrip, old fixture readability and invalid owner rejection. The final load/restore and repeated-load tests require the user PlayMode gate and manual checks.
