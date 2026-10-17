# Sprint carry and missing rifle sound — 2026-10-15

## Changes

The Arberry FPS authoring path disables the legacy arm Animator and WeaponAnimationPresenter. Sprint previously only added a small translation in FirstPersonWeaponGrip, leaving the rifle without the old Sprint animation or an equivalent rotation/cadence. WeaponViewPresenter now owns an explicit, opt-in FPS sprint carry pose, blended in camera space with a small gait translation/roll. It evaluates before the final hand solver. FirstPersonWeaponGrip no longer applies a second sprint translation when that carry pose is active.

Pistol support previously faded out on sprint/slide: FPS returned to FpsOffhandPosition and TPS returned to the Animator left arm. Both now keep the support palm on the weapon during sprint and slide. The gun lowers into a compact two-hand carry instead. Equip/unequip release and the existing reload reference/secondary finger transition remain. FPS/world carry fades out for ADS, reload and accepted fire; accepted shots hold the ready pose for 0.18 s before sprint carry can resume. Combat and movement authority are unchanged.

Saved character profiles: Rifle, Pistol, DXT, Glock17, Custom9mm. Rifle FPS carry is position (-0.025,-0.29,0.30), Euler (18,-25,-18), gait amplitude 0.006 m. Pistols use (0.06,-0.18,0.29), Euler (22,-10,-8), amplitude 0.004 m. TPS pistol sprint offset (0,0.015,-0.06), Euler (20,-6,-5). Profile regeneration preserves these authored carry fields.

## Sound diagnosis and repair

The live player had an enabled AudioListener, AudioListener.volume=1 and no editor mute. However, the persisted `LastSignal.Audio.MasterVolume` preference was **0**, and the production mixer MasterVolume was **-80 dB**. Rifle audio uses the ProductionAudio Effects bus, so the accepted shots were muted globally. This was a device preference issue, not a missing firing event or deleted clip.

Restored only the saved master level to **0.8**, applied through existing AudioPreferences: mixer **-1.9382 dB**. No code forcibly overrides a user's future mute preference. No fake gunshot/fallback audio was added. An accepted rifle shot consumed one round (30 -> 29), incremented production cue count (49 -> 51), and played `sfx_wpn_ar_gunshot_03` on Effects plus the wood impact cue. This is software playback evidence, not a claim about the physical speaker/headphone output.

## Live authoring observations

Evidence is in `Evidence/20261015-sprint-carry/`. Short W+LeftShift input captures used the real PlayerInputReader and FirstPersonMotor; the temporary key state was released and the player position restored afterwards.

- Rifle: IsSprinting=true, FPS carry=1, FPS reach residuals=0/0; TPS support=1, reach=0/0. Final captured FPS palm errors approximately 0.044/0.003 mm.
- DXT: IsSprinting=true, FPS carry=1, FPS support=1, TPS support=1; both views' reach residuals=0/0. FPS and front TPS screenshots inspected.
- Tests/builds NOT_RUN by the user's existing request. This correction did not modify test sources. Previously existing assertions expecting pistol support to drop to zero in sprint/slide no longer describe the intended carry contract.
- These are short live authoring observations, not all-state production acceptance. Earlier body-envelope and per-finger calibration limitations remain separately recorded in TPS_PISTOL_SUPPORT.md.

Final restart confirmed saved master=0.8 / -1.9382 dB. A short live running-shot preview confirmed FPS and TPS sprint-carry weights both fell to 0.018 after the accepted shot while IsSprinting remained true. FPS reach residuals were 0/0; TPS right/left were 0/16.36 mm during this transition (below the existing 20 mm contact threshold, but not zero). Both support weights remained 1. The final inspected Unity console had zero errors/exceptions. Play was left running with the rifle selected; temporary input was released and player position restored.
