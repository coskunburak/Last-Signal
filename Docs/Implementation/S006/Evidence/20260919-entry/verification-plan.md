# Executable verification plan

Entry full 97 EditMode / actual PlayMode gate must pass before feature changes. Preserve all existing tests.

## Unit coverage
- Fixed RNG/hash fixtures; same seed, quantity and emptiness; independent points; global Random unchanged.
- One entry, weighted exact interval boundaries, large totals, zero/negative weights, missing definitions/prefabs, duplicate entries and invalid quantities/probability.
- Inclusive quantity endpoints over deterministic seed set; 0/100 percent empty.
- 10,000 seed counts per authored profile, expected broad distribution tolerance, distinct seeds yield multiple outcomes.

## Runtime coverage
- Same seed twice after End reproduces point ID/item ID/quantity/empty signatures.
- Two Populate calls and pickup followed by Populate do not duplicate/reroll.
- One invalid point does not prevent valid points; duplicate IDs reject both; overlap resolution stable by ID.
- Missing support, wall overlap, player overlap fail without instantiation; ground point succeeds.
- Additive unrelated scene excluded; runtime root assigned to owning scene.
- Generated item through InteractionController.Resolve/TryInteract, full inventory, partial remainder, drop/re-pickup conservation.
- SessionFlow Begin/Menu/Begin and ten cycles, consumed/remaining/dropped item cleanup and no stale registry.
- Real Shambler/rifle regression and no added obstacle/oversized collider.

## Runtime measurements and visual gates
25/50/100 valid populated points: Stopwatch total initialization + thread managed allocation, instantiation marker, warm idle ProfilerRecorder frames. Distinguish whole-editor GC from loot-owned cost; no false zero attribution.
Normal first-person semantic areas, readable visible items, supporting surfaces, blocked/empty points, inventory UI/drop and second session. Fresh development build via existing build pipeline; actual standalone interaction and Player.log. Final full suites and actual XML counts.
