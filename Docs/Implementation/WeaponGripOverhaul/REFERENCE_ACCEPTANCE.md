# Reference acceptance — NOT_RUN
Three VEIN target images were supplied on 2026-10-12. These are reference evidence, not screenshots of the current Last Signal implementation. Visual acceptance remains NOT_RUN. Do not approve reference-level/AAA quality from source code or NUnit alone.

| Supplied image | Visible evidence | Limits / applicable user requirement |
|---|---|---|
| 1 — outdoor pistol, 1920×1080 | Pistol projects from lower-right toward the center; hand surrounds the grip, with a connected wrist/forearm silhouette and clear central view | A second hand is visible below. Follow the user's explicit right-hand-only normal pistol requirement instead of copying that support hand. A still cannot establish recoil or ready transitions. |
| 2 — axe at tree, 1320×704 | Fingers visibly wrap a diagonal handle seated against the palm; the near hand and shaft read as one connected assembly | This is an action frame, with another hand visible and the head partly outside the image. It is not evidence for a fixed idle angle or required head cropping. Preserve the requested one-handed normal stance. |
| 3 — indoor pistol, 1024×576 | Lower-right pistol, visible hand/wrist connection, barrel pointing inward, clear central view | Use composition and anatomical continuity, not the exact mesh or screen coordinates. Raising on fire/ADS is the user's requested behavior, not motion demonstrated by this still. |

Source attachment identifiers, in order: `codex-clipboard-5fcc0631-40d2-4254-885d-08d2409cacab.png`, `codex-clipboard-293833b9-b261-4684-9c96-96e8e54d17c3.png`, `codex-clipboard-11bd47fc-6d91-4bdd-ba91-bd2800001586.png`. No proprietary assets are extracted. No new pose offsets are inferred without current gameplay evidence.

Use a fresh user-started S013Cabin session. Keep the real Player camera. Repeat at 16:9, 16:10 and 21:9 with 75° and 100° FOV (60° edge case also covered in authored tests). Grip must remain spatially stable; identical pixel composition is not required.

| Reference / weapon | Required observation | Fail example |
|---|---|---|
| Images 1 and 3 / pistol low-ready | Lower-right slide, visible connected right hand/forearm, unobstructed forward view; fingers wrap the grip | Raised rifle stance, floating/open hand, wrist kink |
| User requirement / pistol raised | Accepted shot smoothly raises posture; arm/gun stay connected; returns after hold | Hand lags gun, snapping, permanent raised state |
| Pistol ADS/fire | Actual sights stable; no left support; coordinated restrained recoil | Sight obstruction, black lens/trigger remnant, detached recoil |
| Pistol reload | Real magazine area reached transiently, returns on completion/cancel/switch | Stale support hand or magazine pose |
| User requirement / axe idle | Head upper-right, shaft descends diagonally to lower-right grasp, cutting edge readable; image 2 informs palm/finger contact only | Head toward camera, screen-filling head, hovering fingers |
| Image 2 contact + user motion requirement / axe attack | Palm stays seated on shaft and fingers wrap it throughout arm anticipation, acceleration through active phase, follow-through/recovery | Stops at contact, free-spinning wrist, clamped/stunted swing, handle sliding through palm |
| Machete idle/attack | Distinct forward-inclined lighter stance and quicker stroke | Copied axe motion or blade held instead of handle |

For all: equip, walking, sprint, crouch, slide, stops/turns, extreme pitch, rapid switches, inventory/pause, treatment, death, disable/enable, session restart and vehicle entry/exit. Inspect wrists, thumbs, fingertips and sleeve seams. No visible hand masking is allowed to hide anatomy defects.

Use existing 6/7 inspection controls and Scene view: front, side, rear and three-quarter, moving and attacking. FPS-only arms must not duplicate into external views; Diesel weapon must disappear after release.

After automated gates/build are reviewed, please supply nine screenshots or short clips from Last Signal: (1) pistol low-ready, (2) ADS, (3) firing, (4) axe idle, (5) axe swing, (6) machete idle, (7) machete attack, (8) third-person pistol, (9) third-person axe. Include FOV/aspect. The three target references are already supplied; no re-upload is needed. Reference comparison and profile refinement happen from that gameplay evidence.
