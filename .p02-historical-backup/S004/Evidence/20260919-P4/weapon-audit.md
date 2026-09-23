# Entry weapon audit

WeaponController.TryFire consumes one legal shot, then calls WeaponFireResolver.Resolve; ShotFired publishes its result to existing VFX/audio. Camera ray chooses aim, muzzle short obstruction check rejects blocked shots, full muzzle ray selects ONE nearest non-trigger collider. IDamageable is currently found in parents and called once. The static HashSet adds no protection beyond this single dispatch; no penetration/pellets exist. P4 will retain resolution and presentation ownership.

Rifle asset: base damage 30; muzzle obstruction .5 m. Serialized weapon mask 51 excludes movement layer 2. Region layer 8 will be added to the mask, isolated from all physical collisions. Non-trigger region colliders remain eligible for existing Ignore-trigger queries. The movement capsule stays separate. Root ZombieHealth must reject collider-addressed damage not forwarded by an owned region (unrelated child collider safety).

DamageInfo currently carries amount, source position, impact point/normal, instigator. Add minimal collider, direction, category and per-shot trace identity; target-owned region enrichment carries base amount/multiplier/region without type-specific weapon logic.

Initial playtest tuning: HP 100, body x1 (30, 4 shots), head x2 (60, 2 shots). A .5-second real recoil and 1.5-second eligibility interval leave a full second of unstaggered gameplay. Repeated hits do not restart recoil. Eligible recoil cancels any pending melee contact in all three attack phases; death always cancels. Damage provides no new target knowledge.
