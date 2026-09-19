# Scene recovery decision (before reconstruction)

Both missing scenes: **E — UNKNOWN removal cause**. No evidence identifies who removed them, when, or whether an intentional move occurred. Do not label the loss an accidental deletion as a proven fact.

Local Git contains only f9a6119 Initial check-in. Neither acceptance path nor meta is tracked or present in local history/reflog. Assets/LastSignal is untracked working-tree content. Therefore exact Git restoration is unavailable. S001's earlier source-sha256.json records both scene and meta hashes, proving prior working-tree existence. Combat's historical executed tests reached scene setup and assertions; its authoring recipe and integration map specify the dedicated route. These are not scenes known never to have existed.

Search of current scene files/meta GUIDs and Assets.zip finds no relocated acceptance scene or matching retained build GUID. SampleScene's GUID differs from both retained acceptance GUIDs; its floor/two-target content matches the combat layout, but no rename record establishes it as an intentional replacement. It has no S001 parkour/two-door coverage. Classification B or C is not justified.

Reconstruction is required to retain current, still-valid authored-geometry, door/session and combat acceptance contracts: no exact serialized copy is available in local Git/archive, current references remain intentional, and SampleScene cannot satisfy both. B0A authorizes this bounded recovery. Reuse the existing S001Project.CreateScene and CombatAcceptanceProject.CreateTestScene recipes, existing materials and current production prefabs. Do not call their broad asset-generation entry points. Refuse overwriting existing scene files.

Reconstructed scenes receive new Unity scene GUIDs, recorded in updated Build Settings with unchanged path order. This is reconstruction, not byte-identical historical restoration. SampleScene and all player/weapon/vendor assets must retain their pre-task hashes.

Runner hypothesis is separately proven by playmode-abort-reproduction.json: scoped existing tests logged destruction of the Code-based tests runner / PlaymodeTestsController immediately before missing-coroutine-runner errors. No completed diagnostic XML was produced.
