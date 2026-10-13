> 2026-10-13 visual rejection update: arm/jacket clearance and axe framing changed after the earlier PASS run. Current revision is NOT_RUN; production visual acceptance remains pending. See [anatomy correction](ANATOMY_CORRECTION_20261013.md).

# Handoff — functional gates passed, visual acceptance pending

Read ROOT_CAUSE_AND_ARCHITECTURE.md for current-local findings, measured geometry and explicit limitations; VISUAL_ACCEPTANCE.md for user capture steps; TEST_MATRIX.md for gates; CHANGED_FILES.md for the task delta.

TwoHandedHeavyMelee now routes the axe to independent primary/right and secondary/left contacts in FPS and Diesel. Blade remains right-hand-only with independent offhand. Four melee prefabs and the two existing profiles were saved through targeted authoring. Existing neutral animation assets and procedural attack timing were reused; no new animation clips, damage mechanics, vendor meshes, scenes, commits or pushes.

Final user-run evidence after the slide correction: compile PASS; focused EditMode 36/36 PASS; focused PlayMode 3/3 PASS; character regression 31/31 PASS; combat regression 33/33 PASS; full EditMode 779/779 PASS; full PlayMode 430/430 PASS; macOS development build PASS (`Result=Succeeded`, `Errors=0`). Every runner result reported zero unexpected source changes. The build report records 383 warnings; no compiler diagnostics were reported by the gate. Performance and visual acceptance remain NOT_RUN. The unavailable VEIN image, real finger wrap, extreme rendered poses and long machete proportions remain visual limitations. Equip lowers the connected rig; outgoing switching restores/releases immediately rather than playing a new unequip clip.

Next step: perform the manual checks in `VISUAL_ACCEPTANCE.md` and compare the FPS axe idle with the intended VEIN reference. Provide the requested FPS and Diesel captures for visual acceptance. No more automated gates are pending for this source revision.
