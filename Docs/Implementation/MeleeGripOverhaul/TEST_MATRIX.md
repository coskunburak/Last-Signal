> 2026-10-13 visual rejection update: arm/jacket clearance and axe framing changed after the earlier PASS run. Current revision is NOT_RUN; production visual acceptance remains pending. See [anatomy correction](ANATOMY_CORRECTION_20261013.md).

# User-owned verification matrix

Final user-operated verification after the slide correction: compile PASS, melee-grip-edit 36/36 PASS, melee-grip-play 3/3 PASS, character-regression 31/31 PASS, combat-regression 33/33 PASS, regression-edit 779/779 PASS, regression-play 430/430 PASS, development-build PASS. All eight gate results recorded zero unexpected source changes and zero compiler diagnostics. The build reported `Result=Succeeded`, `Errors=0`, `Warnings=383`. Performance benchmark and visual acceptance remain **NOT_RUN**. The assistant executed no Unity verification gate.

Runner: `Tools/weapon-grip-verify.py`, extended in place, reusing `s021-verify.py`. It resolves ProjectVersion, refuses a running Editor/held project lock, creates unique evidence directories, records exit code, NUnit XML/counts and source hashes, rejects zero tests/missing XML/skips/inconclusives/compiler errors/unexpected source changes. Existing results are preserved. New compile/melee/character/combat gates write under this folder's Evidence; older generic regression/build routing remains unchanged.

| Gate argument | Coverage |
|---|---|
| compile | User-run Unity compilation |
| melee-grip-edit | MeleeGripTests + existing WeaponGripCorrectionTests, WeaponGripOverhaulTests, TriplePistolTests, PistolAnatomyTests |
| melee-grip-play | MeleeGripPlayTests on actual Player prefab, post-LateUpdate WeaponGripProbe |
| character-regression | CharacterIntegrationPlayTests (including partial CharacterWeaponPosePlayTests file), VehicleOccupancyTests |
| combat-regression | S021CombatPlayTests, S014WeaponPresentationTests, WeaponGripCorrectionPlayTests (including anatomy/overhaul partial files), TriplePistolPlayTests |
| regression-edit / regression-play | Existing full assemblies |
| development-build | Existing S021 macOS development build entry point |

New EditMode coverage: typed hand assignment, persistent sockets, actual source scale, axis conventions, independent contacts, missing/shared/foreign/nonuniform contact rejection, invalid numeric profile rejection, neutral animation bindings/no damage events, finite authoritative phase samples, ready endpoint, unchanged firearm enum values. Existing tests cover finger restoration and pistol/rifle asset contracts.

New PlayMode coverage: both weapons' ready/attack/recovery, single accepted damage window, both final axe palms and requested reach within 20 mm, independent blade offhand, no legacy crowbar attack layer, recovery drift, crouch/sprint/slide, FOV/aspect/pitch matrix, pause/resume, recovery switching, melee/firearm changes, vehicle holster suppression, disable/re-enable, death and new player session. Existing regression fixtures supplement pistol/rifle, vehicle and action coverage. Treatment/inventory-specific melee interactions and visual finger penetration remain manual acceptance items, not claimed automated coverage.

Performed static-only checks: Python AST parsing, source resolution of every runner test-class filter, unique prefab YAML object IDs and saved contact names, targeted whitespace diff check, and byte comparison of captured pistol prefab/profile baselines. These are not Unity verification. On user results, inspect the failed gate evidence, fix the smallest area, return one focused retest command. Never chain all gates automatically.
