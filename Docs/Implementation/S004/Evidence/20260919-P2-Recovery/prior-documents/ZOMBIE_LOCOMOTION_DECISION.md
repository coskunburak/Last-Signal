# First Shambler locomotion authority — B0B accepted asset handoff

**Navigation/gameplay owns movement; animation is presentation.** Production prefab has `Animator.applyRootMotion=false`. No NavMeshAgent, movement, AI or yaw controller is added in B0B. P2 remains a separate implementation task.

Walk: `Assets/LastSignal/Assets/Kevin Iglesias/Zombie Animations/Animations/Zombie@Walk01.fbx` → **`Zombie@Walk01`**, 1.466667 s, loop enabled, Loop Pose disabled after visual inspection. It bakes root XZ/Y/rotation, with zero measured actor drift. Idle/Attack/Damage/Death also bake all root transforms; body motion remains visible inside the rig. No source curves/settings erased.

The same FBX includes **`Zombie@Walk01 [RM]`**, with unbaked XZ and .899837 m/s forward imported average speed. A 120-step Mecanim root-delta integration on the actual Hotstrike Avatar measures **1.347476 m forward per cycle / .918734 m/s**, zero vertical displacement and yaw. Root variant is retained as measurement evidence, not referenced by production Locomotion. All non-RM probes have zero accumulated root translation/yaw, including the corrected Death.

**VISUAL WALK SPEED APPROXIMATION: ~0.92 m/s at playback speed 1.0**, grounded in the target-rig root-motion counterpart. This is a starting value, not a fabricated tuning range or final contact-speed guarantee. Initial P1 1.0/1.8 m/s values were hypotheses; 1.8 m/s cannot be claimed stride-matched at current cadence. P2 should compare actual planar velocity with accepted cadence (starting playback multiplier actualSpeed/.9187), tune foot contact over ground, and avoid animating a blocked actor as moving.

Walk has a deliberate high knee/shuffling cycle. Skin minimum height across the cycle is .0089–.0362 m with no floor penetration; modest float/slide needs scene-based P2 tuning. Idle has up to ~1 cm transient contact penetration. No foot IK or motion warp is silently introduced. Model standing reference is 1.808236 m versus player capsule 1.8 m, identity actor scale and +Z forward.

The future navigation adapter is the sole position writer; gameplay owns bounded yaw (`agent.updateRotation=false` as the P1 design), with no concurrent OnAnimatorMove or Rigidbody translation. Stop/revalidate on blocked paths or invalid lifecycle. Dead/reaction/attack behavior is future P3/P4 work. Retain last-known-position Searching and finite memory from [behavior contract](ZOMBIE_BEHAVIOR_SPEC.md).

**RUN = NOT REQUIRED FOR SHAMBLER V1.** [Root samples](Evidence/20260918-B0B/motion-summary.txt), [full samples](Evidence/20260918-B0B/motion-samples.csv), [three-view playback](Evidence/20260918-B0B/retarget-continuous-three-views.mp4).
