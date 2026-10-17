# User-operated gates

**No compile verification, EditMode/PlayMode test, regression, benchmark or build gate was run by the agent.** Unity performed normal script imports and compiled local authoring command snippets as required for asset operations; these are not compile-gate evidence.

Save and close Unity normally. The first command is:

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" compile
```

Return the new `gate-result.json` and focused compiler diagnostics. Stop there until the result has been reviewed. Never chain gates or kill the Editor. The runner checks active Unity/lock ownership and stores each invocation in a unique `Docs/Implementation/CharacterIntegration/Evidence/<timestamp>-<gate>-<uuid>/` directory.

After reviewing each preceding result, use the same command with one subsequent gate name at a time:

| Gate | Purpose |
|---|---|
| character-edit | New Arberry importer/Avatar/skin/material/packing/profile/fixture contracts; existing Diesel fallback and character/serialized contracts |
| character-play | Existing and new character lifecycle, stance, camera visibility, actual FPS skins/contact cleanup, actual pistol variant selection; triple-pistol, melee grip, weapon correction; focused vehicle and survival integration |
| player-combat-edit | Existing health/ammo/weapon/vehicle/survival logic |
| player-combat-play | Existing movement/combat/FPS/scope/vehicle/survival behavior |
| locomotion-play | Movement and rifle stance integration |
| full-edit | Broader Editor regression |
| full-play | Broader PlayMode regression after explicit preceding-result review |
| development-build | Existing runner's **macOS development build**, not Windows acceptance |

Only character gate filters were extended. No automatic retries or new execution mechanism was introduced. The runner does not provide release-build, Windows-build or performance gates. Windows commercial-target build/benchmark commands need the project's target-specific release workflow and installed build support; do not misrepresent development-build as Windows certification. No release gate was invented or run.

## Authored coverage (all NOT_RUN)

ArberryIntegrationAssetTests: source/import settings and original remaps, own Avatar and complete bones, 11 skins/160 bone integrity, unit scale/root motion/controller, real textured external materials, texture interpretation/budget metadata, URP packing boundaries, all source→character profile overrides and contact sockets, distinct pistol finger bindings, fallback validity, Player binding and fixture presets.

ArberryFirstPersonAssetTests: seven actual-skeleton weapon assets, mesh/bindpose/material references, disabled legacy Animators, rifle sockets and unchanged weapon definitions.

CharacterIntegrationPlayTests.Arberry: actual FPS skin/material ownership, rifle two-hand/ADS contact, reload cancellation without ammo mutation, paused root stability, crowbar cancellation without an extra damage owner; actual three-variant selection and retained ammo/health/controller instances; one world visual; own Avatar and no Player movement on presenter re-enable. Existing character tests exercise locomotion/crouch/slide, reload/melee cancellation, damage/death/new health, missing combat and render-boundary visibility. Existing focused vehicle and survival tests remain selected. Existing TriplePistol/MeleeGrip assertions now resolve the character-specific world profile rather than requiring FPS and world to share the same asset.

S014WeaponPresentationTests explicitly selects the original rifle/crowbar to retain fallback Animator coverage; the active Arberry procedural FPS contract is covered separately.

New tests use one lifecycle frame or explicit presentation calls, not arbitrary wall-clock delays. Older tests retain their existing timing infrastructure; they were not broadly rewritten in this task. A failed grip/deformation test must be investigated against the exact pose and metadata, not weakened to accept an unreachable target.
