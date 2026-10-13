# User-operated gates — PASS, visual acceptance pending

On 2026-10-12 the user completed compile, pistol-edit/play, combat-edit/play, character-edit/play, regression-edit/play and development-build. Inspected NUnit results: focused pistol 24 EditMode + 7 PlayMode; full regression 748 EditMode + 409 PlayMode, all passed with zero skipped/inconclusive. See HANDOFF.md for individual evidence directories, build warning count and source-preservation results. No gate was executed by the assistant.

`Tools/pistol-grip-verify.py` wraps the existing lock-aware S021 runner with unique evidence under this directory's `Evidence`. It does not run on import. An open Unity Editor is rejected rather than launching a competing process. Existing real exit-code, nonempty NUnit and skipped/inconclusive failure handling remains intact.

Order: compile → pistol-edit → pistol-play → combat-edit/play → character-edit/play → regression-edit/play → development-build → visual acceptance. Run independently and inspect failures before expanding scope. The existing development builder targets macOS, not Windows.

PistolAnatomyTests adds separate-rig pose/socket validation, finger segment rejection inside the measured handle core, opposite-side wrapping, repeated pose restoration, and tactical/empty reload insertion/commit/cancellation. Existing grip/animation/finite-scale tests are included.

The real Player PlayMode fixture adds final-frame finger geometry through ADS, fire and reload with an observed magazine extraction and commit. Existing tests cover low/raised-ready, support-hand exclusion, reload interruption, disable/enable, pause, movement/crouch/slide, pitch extremes, rapid switches, death, session restart and viewport changes. World contact is sampled in the same probe. Existing firearm/character/vehicle regressions remain separate gates.

No test thresholds were relaxed. The model-core test is intentionally narrower than a skin-surface intersection test; actual flesh/glove contact and aesthetic quality require the user captures in VISUAL_ACCEPTANCE.md.
