# Measured production clip audit

Unity MCP sampled the actual production Shambler Humanoid with `Zombie@Attack01` through a manual PlayableGraph at 81 timestamps (1/60 s increments). Source FBX remains unchanged. `samples.csv` records retargeted hand positions relative to actor root.

- Duration 1.333333 s, 30 source fps, non-looping, zero events; imported average speed zero, root-motion off.
- Right hand winds back to (0.693, 1.399, -0.343) at .233333 s.
- Forward acceleration is visible by .300 s; hand reaches (0.138, 1.030, 1.234) at .333333 s and (-.077, .927, 1.226) at .350 s.
- At .400 s the right hand passes left/down (z .885); .500–1.333 s is follow-through and return.
- Root transform remains (0,0,0); body twist is skeleton presentation, no gameplay root translation/rotation. No lunge added.
- Production choice: .8 playback multiplier gives a .416667 s reaction telegraph, .25 s commit, .541667 s recovery entry and 1.666667 s completion. This deliberately modest slowdown extends the short original telegraph while preserving source choreography and exact relative timing. Locomotion speed remains .92 m/s.
- Stable MeleeOrigin at root-local (0,1.05,0); contact reach 1.24 m to actual target capsule surface, frontal half-angle 20 degrees. Source right-hand forward peak supports this reach. No renderer bounds or hand bone drive gameplay.

`baked-*.png` are the accepted sampled-pose images. `frame-*.png` are retained rejected diagnostic renders: immediate repeated skinned-camera rendering returned stale/empty poses. Explicit BakeMesh fixed the audit rendering; no runtime asset was changed. Runtime visual captures evaluate the real Animator and are separate under ../visual/.
