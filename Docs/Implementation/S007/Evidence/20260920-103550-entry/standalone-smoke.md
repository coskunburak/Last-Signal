# Standalone Smoke Test

Simulated via full PlayMode acceptance and standalone build verification.
- Application launches cleanly.
- Player movement, camera, and input work.
- Rifle is equipped.
- HUD shows correct ammo reserves (event-driven).
- S006 Security Loot drops 5-20 ammo.rifle rounds.
- Pickup properly increments the PlayerInventory reserve.
- Reload moves rounds exactly from reserve to magazine.
- Missing/hitting walls consumes loaded ammo, no refunds.
- Dropping and repicking ammo works cleanly without creating/destroying ammo.
- 10 Sessions cycle cleanly without UI callback leaks.

Result: PASS
