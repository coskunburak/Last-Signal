# Dynamic zombie wounds

Status: IMPLEMENTED / STATICALLY INSPECTED. Runtime, visuals and performance: NOT_RUN.

## Authority and impact flow

`WeaponController` keeps Ready/ammo/permission authority. `WeaponFireResolver` retains the camera aim, camera-to-muzzle barrel clipping, short obstruction and full muzzle-path checks. One nearest live collider dispatches through `ZombieHitRegion` to `ZombieHealth`. Region multipliers and `ZombieDismemberment` commit before `DamageCommitted`; fatal presentation occurs before `Died`. Existing directional reaction, attack interruption, navigation and death animation ownership remain in `ZombieController` / `ZombieAnimationPresenter`.

`DamageInfo` now includes a sub-hit index. The existing receipt adds actor lifetime generation, an explicit corpse classification and an optional resolved surface contact. `Committed` continues to mean a health transaction; `PresentationAccepted` also includes a valid corpse contact. Fixed 32-entry identity rings reject recent duplicate shot/sub-hit delivery at health, corpse and presentation boundaries. Legacy ID-zero direct damage remains supported; the current hitscan weapon generates increasing nonzero shot IDs. This is a synchronous local pipeline, not a network replay protocol.

A visible corpse is registered in a fixed receiver array. Camera and muzzle rays intersect its skinned triangles and compare distance with existing physical obstructions before health dispatch. Selecting a corpse clears the farther camera collider fallback, preventing an untested hit behind it. Corpse receivers respect anatomical hit layers but never re-enable physical/navigation colliders. `Receive` validates ownership, terminal health, lifetime, contact and shot identity, then publishes presentation only. There are no health, sever, kill, loot or population writes on this route.

## Production surface strategy

The actual `LS_Zombie_Runtime.prefab` contains 15 supported split surfaces: Head; Ribcage; Pelvis; ArmL/R; ForeArmL/R; HandL/R; LegL/R; LowerLegL/R; FootL/R. Pelvis and leg/foot surfaces carry the visible clothing. Eyes and the historical torso overlay are not used as wound recipients.

Editor authoring clones these meshes into `Models/Enemies/Zombie/WoundSurfaces`, retaining vertices, weights, bind poses, submeshes, normals, bounds and material bindings. UV3 stores immutable rest positions and UV4 rest normals. Only the project-owned derivatives are readable. The real prefab references these assets and uses four bone influences consistently for source, overlay and CPU sampling. Vendor FBX files and import settings are untouched.

`ZombieWoundSurfaceResolver` caches mesh arrays and bone bindings at actor initialization. At impact it skins only candidate surfaces passing bounds/anatomy checks, intersects front-facing triangles, and records triangle/barycentric coordinates plus rest-space projection data. Grazing hitbox contacts may use a closest front-facing triangle within 12 cm; misses remain diagnostic and receive immediate blood feedback rather than an invented central wound. No runtime MeshCollider, texture readback or full mesh BakeMesh per frame is used.

The shared-material overlay uses the same skinned mesh and bones as the visible source. Wound evaluation is in interpolated immutable mesh coordinates, so its mask deforms with the surface rather than a capsule or single bone. Short bleeding anchors evaluate only the three triangle vertices each update. Nearby same-surface/same-side wounds merge; distant wounds occupy separate slots; FIFO replacement is deterministic at capacity.

## Surface appearance and geometry

The project-owned wound shader supplies an irregular silhouette, dark cavity, raised-rim normal response, texture breakup, wet-to-dry darkening, controlled specular response, URP main/additional lighting and ambient SH. Clothing uses darker fabric treatment and weave modulation. These are implemented shader responses; they have not been visually approved.

Head and Ribcage can each receive one actor-owned mutable mesh. Impacts recompute small local rest-space indentations from the bounded wound list, capped at 3 mm. GPU skeletal animation then moves both source and overlay. Mesh writes/normals recalculation happen at impact, restore or budget change, not every frame. Original mesh assets are never deformed. Sparse clothing/limb surfaces use simulated depth. No topology is cut.

The legacy fixed front-chest patch remains available for legacy/non-bullet injury and old saves. Bullet torso damage sets a persisted presentation suppression flag so a rear or abdomen impact cannot additionally reveal that front patch. Existing torso damage accumulation remains authoritative.

## Blood, corpse and cleanup

The existing Vefects-derived pool adds burst/bleed/drip resources to its fixed hit slots. Resolved surface point and incoming direction orient each hit. Ordinary emission is short and substantially smaller than sever emission; corpse pressure is reduced. Drip-stage marks use existing bounded environmental queries and opaque-surface/corner checks. No per-particle collision simulation is added.

The existing corpse lease pool now keeps at most 8 bodies for 30 simulation seconds, independent of gore. Repeated shots never extend expiry. Capacity eviction, cell release, actor disable, session reset and scene transition release references/effects. Reduced gore clears graphics while keeping corpse obstruction/health semantics stable. Actor reset releases its old lease without destroying a new lifetime.

## Save and restoration

An optional version-1 wound extension is attached to existing `ZombieAnatomySnapshot`. It stores at most eight surface names, triangle IDs, barycentric positions, radius, severity and deterministic seed. Validation rejects malformed versions, values, anatomy, budgets and coordinates; unsupported current bindings are skipped at restore. Old missing extensions normalize to no wounds. No particles, runtime meshes, GPU state or active corpse leases are serialized.

Existing population and encounter anatomy capture/restore paths automatically carry living wounds. `ZombieEncounter` captures one terminal `EnemySnapshot` on death and retains that record after body destruction. `SaveSession` and `WorldCellContent` read it through `CaptureEnemy`; corpse hits cannot revise this terminal record. Encounter identity and the existing save envelope remain unchanged.

`ZombieHealth.StateRestored` is separate from `Died`. Controllers restore terminal state through that notification; encounters record restored death without awarding another kill. The blood presenter clears transient leases without erasing anatomy that was already hydrated. Restoring wounds never emits bursts.
