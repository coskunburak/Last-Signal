# Tree system compatibility — realistic tree art and physical chopping

## Distinguish current state from desired behavior

Owner report from 2026-10-18: current tree is destroyed after about four hits and produces a small reward drop, lacking a growing cut wedge and physical fall. The desired **future state** is local per-strike damage, visible progressive cuts, physically credible fall (Sons of the Forest is a feel reference, not source code), grounded collision, appropriate salvage and persistence. Earlier developer logs mention OpenFracture MIT and two local tree examples; these are **not proof** that final runtime fracture or visual acceptance works. Inspect the *local* S003/harvest implementation before deciding integration.

## Interaction and state-machine proposal

`StandingIntact -> Notched(0..n) -> CriticalCut -> FallingDynamic -> SettledLog/Segments -> Harvested/Tombstoned`. Damage acceptance stays authoritative in current Gathering/Melee modules; tree visual meshes and physics are presentation/interaction clients. Never award loot twice during ragdoll or repeated inputs. Events are idempotent across interruption and save/load. Cut direction influenced by hit side, wedge geometry/remaining hinge and mass/collision constraints. Never assume four hits must always fell every species; tune by axe strength, trunk size and wood condition.

## Prefab requirements

`TreeRoot`: stable persistent entity ID, hitbox/axe query target, trunk radius/base transform and forest scatter identity, authoritative health/damage record. `StandingVisual`: canopy + bark trunk materials; collider correctly aligned. `CutVisual`: supported local wedge/decal/mesh deformation whose cost is bounded, displays clean bark-to-wood cut cap without texture stretch. `FallingAssembly`: Rigidbody(s), compound/convex colliders, mass/COM/drag/angular velocity limits, trunk/canopy geometry that need not be the same complex collision mesh. `SettledResource`: pickup/log segments using existing resource inventory schema. These are proposed roles, **not claims of current class names**.

## Geometry and fracture policies

- Imported heroic photoreal tree must have clean UV, accessible trunk topology and separate bark/foliage material handling. A single material with geometry-only tree cards may not be cuttable as-is.
- Prefer deterministic authored sectional cut/mesh templates for commonly sized stems if runtime mesh slicing is fragile or costly. Do not promise true arbitrary physical fracturing without verified topology and budget.
- If runtime fracturing is tested: convex/mesh collision safety, number of new pieces, non-manifold failure, submesh material indices (including wood interior cap), normal generation, cuts at rotated/scaled trunks, geometry LOD transitions, maximum physics contacts and pooling.
- Disable or adapt standing collider at commit so it does not block the falling trunk; keep corpse/trunk interaction queries meaningful. Physics changes must not move save IDs or spawn duplicate resources. Cap impulse against players/vehicles/zombies; use collision matrix and fail-safe settle timer.
- Tree canopy wind and leaves are presentational and must not drive physics authority or fresh random weather systems.

## Save / reload correctness

Serialize minimal authoritative state (standing hit progress/cut orientation or pre-authored notch index; falling transition receipt; consumed resource IDs; settled log pose where necessary), with migration if schema changes. Do not serialize raw dynamically generated Mesh bytes by default. Old-save tree state must have safe restoration default, tree placement identity stable even after new vegetation density seeds. Test reload at mid-notch, first falling frame, mid-fall, settled and after harvesting. Never spawn standing tree and fallen copy simultaneously.

## Minimum owner acceptance matrix

Hit left/right, same spot vs opposite, miss and invalid tool, quick successive hits, interrupted attack, 1/2/4/large trunk, near slope, near road/structure, tree against another tree, player/vehicle collision, zombie pursuit/path, rain/wind, FPS animation and sound, pause/load at each state, tree unload/reload, low graphic setting, 60 FPS combat-heavy mode. Screenshots/videos show notch shape from eye level, actual fall acceleration/rotation and settled loot. Performance counts slicing CPU ms, memory allocations, Rigidbody/collider count and contacts, GPU foliage load and result lifetime.

**Do not implement harvesting simultaneously with render-pipeline migration.** First qualify source mesh and prototype on URP, then test the same cut asset in HDRP experiment if still needed.
