# Recovery changes and validation history

- Entry: two CS0103 errors in ZombieValidationRunner referenced absent ZombieRuntimeAuthoring. No quoting corruption. Disabled unavailable authoring commands explicitly until their implementation was completed; no stub silently passed.
- Preserved seven AI runtime files and passing core tests; added separate runtime wrapper retaining presentation-only B0B prefab and its existing tests.
- Completed explicit encounter composition for arena and SampleScene, normal NavMesh, production presenter binding and movement capsule.
- Shutdown clears controller clocks/cooldowns/flags as well as existing memory/target cleanup. Ten session cycles passed.
- Search permits true loss/reveal reacquisition even after exhausted navigation, without constantly resetting failure policy on an already-visible unreachable target.
- Stop check uses the same .08 m arrival tolerance as navigation. First visual stop failures also exposed a fixture issue: the forward reveal point could lie inside the low crate. Stable stop is now tested in a known open lane. Failed XML remains intact; it is not evidence that the tolerance alone repaired the observed failure.
- Profile 01 had insufficient real-time warmup at uncapped Editor FPS; retained but superseded by a 3-second warmup/5-second measurement. Direct editor-only synchronous AI tick allocation accounting is separate from whole-Editor GC counters. A live MCP dynamic-code query during the second profile can contaminate whole-Editor spikes; final profile will run without MCP queries during sampling.
- Existing test runner contains no broad cleanup. New tests destroy only owned objects and shut down their loaded session.
