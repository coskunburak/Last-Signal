# Visual review — recovery

Observed actual PlayMode render captures, not generated illustrations. Sequence metadata records actual controller state, actor position, memory, visibility, local search point and velocity. Initial failed stop fixtures are preserved in visual-20260919-064301 and visual-20260919-064631. Corrected full sequence in visual-20260919-064838 passed (visual-focused-03.xml).

Reviewed Idle/outside FOV, walk, crate detour, corner loss, search/resume, stop, new session images. Production Hotstrike zombie and B0B material/controller render correctly. Crate route goes around legal clearance at x≈-6.32, not through the collider. Root stays at NavMesh y≈.02 m. LastKnownPosition stays (-5,0,7) while player moves hidden; local point advances; search expires and reacquisition returns to chase. Pause freezes position/time, even though NavMeshAgent may report stale velocity on a paused frame. No position integration occurs while paused.

New-session capture exposed initial bind pose before first Animator update. Presenter initialization will explicitly evaluate Idle at delta 0, followed by regression and fresh capture. Still images and scripted state checks do not establish shipping locomotion quality at all frame rates/slopes; slight shuffling/low-speed turn slide remains a tuning limitation. No foot IK/turn clip was added.

Unchanged player world-body material renders magenta in third-person evidence view. The layer-30 body is excluded from the normal first-person camera; this is a separate pre-existing URP material issue, not zombie material failure. No vendor material was rewritten to hide it.
