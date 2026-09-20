# Reload Transaction Spec (S007)

## Conditions
- Reload begins only if Magazine < Capacity AND Reserve > 0.
- Full Magazine: Safe, no transaction occurs.
- Zero Reserve: Safe, no transaction occurs, no legacy refill.

## Timings
- **Tactical Reload Duration**: ~2.5 seconds
- **Tactical Reload Commit**: ~1.667 seconds (normalized ~0.66)
- **Empty Reload Duration**: ~3.333 seconds
- **Empty Reload Commit**: ~1.83 seconds (normalized ~0.55)

## Mechanics
- **Exact Reserve Transfer**: Computes exact missing rounds up to available reserve and transfers them at commit.
- **Multi-stack Removal**: Safely removes rounds spanning multiple inventory slots.
- **Partial Reload**: Fills the magazine with whatever reserve is available if insufficient for a full magazine.
- **Commit-time Revalidation**: Re-reads exact PlayerInventory quantity at the instant of commit, preventing negative ammo or dupes from mid-reload drops.
- **Commit Exactly Once**: Synchronous notification and boolean locks ensure no double execution.
- **Repeated Reload**: Input is ignored while a reload is already active.
- **Cancellation Before Commit**: Weapon unequip, Player death, or disable will safely cancel the reload without transferring ammo.
- **Cancellation After Commit**: The committed transfer is preserved, no phantom refunds or second transfers.
- **Weapon Disable / Player Death**: Cancels further state progression but respects any prior commit.
- **Pause / Large Timesteps / ReturnToMenu**: Framerate robust 30/60/120 Hz, properly scales pauses, safe teardown.

No ambiguous semantics. Zero ammo is created, zero ammo is lost.
