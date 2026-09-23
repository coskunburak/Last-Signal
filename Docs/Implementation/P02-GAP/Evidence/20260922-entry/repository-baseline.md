# Repository baseline

- Branch: main
- Starting HEAD: 5b43cfbb8532d23ae5830e8956673b4d6b9f8fc3 (s008-complete, origin/main)
- Unity: 6000.5.0f1 (88b47c5e7076)
- Tree was already dirty: persistence-related changes to runtime models/session flow, six PlayMode test corrections, two shelter materials, historical evidence and S008 acceptance note; untracked Persistence runtime, scene, build script, tests and RoadmapRecovery documents.
- No staged changes were reported. No reset, checkout, clean, commit or push performed.
- `entry-source.diff` preserves the starting tracked script diff. New source edits integrate with untracked local persistence; that foundation is not replaced from GitHub.
- Initial `git status` was blocked by Git LFS attempting temporary metadata writes under .git. Read-only audit succeeded with sandbox escalation. No repository content was reset.
- Unity MCP entry: active ShelterAcceptance scene, not playing/compiling. User-guidelines tool returned success with no additional text.
- Entry tests: all 204 EditMode and 15 shelter/persistence PlayMode cases passed.

Historical evidence caveat: the existing test fixtures write fixed historical evidence paths. Entry baseline tests may refresh those metrics through their existing behavior; they are run outputs, not newly asserted historical human evidence. Before the full regression, 1,224 historical evidence files were backed up; full-run replacements will be archived under this P02-GAP run and prior files restored. No historical implementation directory is renamed or reclassified.
