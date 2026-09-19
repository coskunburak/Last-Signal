# P2 locomotion authority and production values

NavMeshAgent owns translation; ZombieNavigation owns bounded yaw. Animator root motion, agent updateRotation/updateUpAxis and autoRepath are OFF. The unchanged B0B production Humanoid/controller supplies Idle and Locomotion.

| Setting | Actual value |
|---|---|
| Speed | .92 m/s, reduced while turning away from steering direction |
| Acceleration | 2 m/s² |
| Angular speed / gameplay turn cap | 120°/s |
| Agent radius / height / base offset | .32 m / 1.81 m / 0 |
| Bake slope / climb | 40° / .25 m |
| Bake voxel / tile | .08 m / 128 |
| Geometry / mask | PhysicsColliders / Default (1) |
| Engagement stop / resume | 1.2 m / 1.6 m |
| Search arrival / sampling | .35 m / .6 m |
| Refresh cadence / movement threshold | .3 s / .45 m |
| Retry interval / recovery attempts | 2 s / 2 |
| Stuck window / minimum progress | 2 s / .12 m |
| Spawn correction bound | .3 m; initialization only |

Acceptance surface collects NavigationWorld children. Normal surface collects current normal scene world. Complete paths proceed; partial paths use legal segments then bounded endpoint retries. Invalid paths stop and back off. Stuck recovery repaths within limits; exhausted movement stops. Disabled/off-surface APIs are guarded. There is no teleport-to-player policy.

B0B measured walk reference is .918734 m/s (1.347476 m over 1.466667 s on the actual Avatar). Presenter matches measured actual velocity/reference, clamped .8–1.15 while walking; Idle speed is 1. Blend is .12 s. Speed below .025 selects Idle; entering Walk requires .08. Pause sets Animator.speed=0; resume restores state-derived speed. Root-motion counterpart remains measurement evidence only.

Low-speed turns can retain slight foot slide due to the minimum .8 playback multiplier and in-place shuffling clip. No foot IK, strafe or turn-in-place motion is added. Visual verification status and profile limitations belong in the current report. Prior B0B measurement detail is preserved in Evidence/20260919-P2-Recovery/prior-documents/ZOMBIE_LOCOMOTION_DECISION.md and original B0B evidence.
