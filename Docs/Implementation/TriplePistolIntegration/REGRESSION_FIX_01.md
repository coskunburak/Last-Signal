# User-run regression evidence and focused correction — 2026-10-12

User evidence: compile PASS; pistol-edit 6/6; pistol-play 9/9; character-regression 31/31; combat-regression 23/23; full EditMode 754/754. No unexpected source changes and no compiler diagnostics were reported. Evidence folders are the 20261012T170717Z through 20261012T171401Z runs in Evidence/.

Full PlayMode: 417/418 passed, one failed, zero skipped, zero diagnostics, zero source changes. Evidence: `Evidence/20261012T171440Z-regression-play-c448670a`. Development build did not execute because the user's command chain stopped here. Manual visual QA remains NOT_RUN.

Failure: `LastSignal.Tests.WeaponGripCorrectionPlayTests.PistolFingerWrapSurvivesAdsFireAndReloadCommit` expected a distal X coordinate in [.023,.045] but observed -.0103489114. The production Player now selects normalized DXT, while this assertion and its handle box were authored in the retired MR POLY model's offset coordinates. The observed coordinate also corresponds to the DXT-authored endpoint; it is not evidence of an unexplained runtime translation.

Correction (IMPLEMENTED_NOT_VERIFIED): the test queries the selected normalized Frame mesh with a temporary collider, using collider-specific opposing rays to obtain each finger sample's local cross-section. It rejects bone segments within the central third of the frame cross-section and bounds distal endpoints relative to the actual opposite surface (16 mm outside / 8 mm inside pad allowance). It requires actual handle overlap. These remain conservative structural bone checks, not proof of skin-surface contact or visual quality. No baked finger endpoint is used as a passing oracle.

The same idle/ADS/fire/reload/commit/cancel-release sequence now covers DXT, Glock17 and Custom9mm, with fixture cleanup in finally. The normal pistol-play filter now includes this case so the focused gate will not omit it again. No gameplay, mesh, material, pose or save implementation was changed for this correction. No Unity gate was run by the assistant.

Next: user runs this exact test through the runner's --filter option. Its outcome is NOT_RUN. Do not rerun the full suite until this focused result is available.
