# Focused fixture and implementation corrections

1. `focused-editmode.xml`: 22 passed / 1 failed. Legacy schema-1 decode rejected JsonUtility's materialized empty WorldTimeSnapshot. SaveCodec now discards that nonexistent section for schema 1 after checksum verification, while schema 2 validates required fields. `focused-editmode-02.xml`: 23/23.
2. `focused-playmode.xml` and `focused-playmode-02.xml`: 5 passed / 5 failed. Initial investigation narrowed broad overlap queries to EnemyHitRegion. Live MCP inspection then established 11 colliders all belonging to the real living zombie at (-5, .02, -4), within 20m of bed. That rejection is correct; radius was not reduced to manufacture success.
3. Sleep-specific fixtures now explicitly place the unrelated encounter at a distant NavMesh position. Threat tests deliberately move it near. The production acceptance route clears the actual threat with PlayerHealth-compatible production damage authority before rest (ZombieHealth.TakeDamage).
4. Reactivating a disabled ZombieController does not automatically undo its Shutdown damage gate. Fixture now calls existing Initialize/Bind before testing death, and asserts actual IsAlive=false. No old regression test was weakened.
5. `focused-playmode-03.xml`: 10/10. Full gates run after final authoring/settings/checkpoint controls were compiled.
