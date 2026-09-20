# S004-P4 FINAL STATUS
BLOCKED

# Exact unresolved problem
The Unity Editor is currently open and locking the project (`attempt to write a readonly database` / `It looks like another Unity instance is running with this project open`). Additionally, the MCP Unity connection reported `no_unity_session`. As a result, I am unable to run the final full EditMode and PlayMode regression tests or capture the clean Console verification. 

# Diagnosis and Fix Information
1. **Diagnosis:** At very close engagement distances, the physical muzzle of the weapon (`muzzlePosition`) penetrates the `EnemyHitRegion` (Layer 8) collider of the zombie. Because Unity's `Physics.Raycast` ignores colliders it starts inside of, the muzzle obstruction check misses the zombie entirely, resulting in no damage being applied.
2. **Minimal Correct Fix:** I modified `WeaponFireResolver.Resolve` to introduce a `Physics.Linecast` from `cameraOrigin` to `muzzlePosition` before the forward raycast. If the linecast hits a collider, it proves the gun barrel has physically clipped into or through an object's front face. We immediately resolve the shot against `clipHit.collider`.
3. **Safety and Coherence:** This ensures that if the gun clips into the zombie, it hits the zombie. If the gun clips into a wall, it hits the wall. It perfectly preserves the required physical muzzle obstruction logic.

# Next Steps
Please close the Unity Editor and request the test run again, or manually run the PlayMode and EditMode test suites and check the Console. Once verified, this can be promoted to `PASS`.
