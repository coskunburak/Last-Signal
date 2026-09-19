# Weapon Behavior Specification

## High-Level States

*   **Holstered:** Weapon is not visible; no actions are available.
*   **Equipping:** Transitioning to Ready state; fire and reload inputs are blocked.
*   **Ready:** Weapon is visible; hip-fire or ADS (Aim Down Sights) actions are available.
*   **Firing:** Executing a fire transaction (instant for hitscan weapons).
*   **Reloading:** Executing a reload (tactical or empty); interruptible before the commit point.
*   **Unequipping:** Transitioning to Holstered state; fire and reload inputs are blocked.

## Transition Table

| From | To | Trigger | Conditions |
| :--- | :--- | :--- | :--- |
| Holstered | Equipping | Equip input | No weapon currently equipping |
| Equipping | Ready | Equip animation complete | None |
| Ready | Firing | Fire input | Ammo > 0, cooldown elapsed |
| Firing | Ready | Fire transaction complete| None |
| Ready | Reloading | Reload input | Magazine not full, reserve > 0 |
| Reloading | Ready | Reload commit + finish | Or cancellation |
| Ready | Unequipping | Unequip/switch input | None |
| Unequipping | Holstered | Unequip animation complete| None |
| Any | Holstered | Forced state change | Player death, scene unload, session end |

## Action Policies

*   **Fire while reloading:** REJECTED (reload must complete or be cancelled first).
*   **Reload while firing:** REJECTED (fire cooldown must expire first).
*   **ADS while sprinting:** Sprint cancelled, ADS begins.
*   **Sprint while ADS:** ADS cancelled, sprint begins.
*   **Reload while sprinting:** Sprint cancelled, reload begins.
*   **Weapon switch while reloading:** Reload cancelled (pre-commit), unequip begins.
*   **Fire during equip:** REJECTED.
*   **Fire during unequip:** REJECTED.
*   **Reload spam:** REJECTED if already reloading.
*   **Fire spam (semi-auto):** One press = max one shot. Held = one shot. Release required before next shot.
*   **Fire spam (auto):** Held fires at cadence. Release stops immediately.
*   **Input held while pause:** Cleared by neutral-required guard in `PlayerInputReader`.
*   **Focus loss:** `PlayerInputReader.NotifyFocusLost()` → `SetGameplay(false)` → all inputs cleared.
*   **Gameplay scene unload:** `WeaponController.OnDestroy()` cleans up.
*   **Player death:** Force transition to Holstered, disable input.
*   **Returning to menu:** `SessionFlow.ReturnToMenu()` destroys player → weapon destroyed with it.

## Fire Modes

*   **Semi-automatic:** One trigger pull equals one shot. Player must release and re-press for the next shot.
*   **Automatic:** Holding the trigger fires continuously at the configured rate (rounds per minute). Frame rate does NOT change the shot count.

## Ammunition Model

*   **Magazine capacity:** Maximum rounds in the weapon (e.g., 30).
*   **Current magazine count:** Current rounds available to fire (0 to capacity).
*   **Reserve ammunition:** Total extra rounds carried (0 to max reserve).
*   **Chamber state:** No chamber state for initial implementation.
*   **Invariant:** Total ammo (magazine + reserve) must never increase except through explicitly defined gameplay events (e.g., ammo pickups).

## Reload Types

*   **Tactical reload:** Magazine has rounds remaining. Swap magazine. Transfer `min(capacity - current, reserve)` from reserve to magazine.
*   **Empty reload:** Magazine is empty. Swap magazine + optional bolt/slide action. Ammo transfer logic is the same.
*   **Commit point:** Ammo transfers at a defined point during the reload animation. Before commit = cancellation returns to pre-reload state. After commit = ammo is transferred, though animation may still need to finish.

## Cooldown

*   **Fire cooldown:** Calculated as `60 / roundsPerMinute` seconds.
*   **Tracking:** Tracked via elapsed time, not frame count, to ensure framerate independence.
*   **Reset:** Timer resets on each successful shot.
