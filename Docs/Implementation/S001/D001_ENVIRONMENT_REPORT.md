# D001 — Environment discovery

Run: 20260917-S001. Repository and Unity root: `/Users/burakcoskun/Last Signal`.
Branch: `main`; rollback commit: `f9a6119a835189d9621f2213f62789b91a61b27c`.
Initial user changes: `Evidence/20260917-S001/initial-git-status.txt`. No reset, checkout, push or merge performed.

Unity: 6000.5.0f1 (88b47c5e7076), installed at `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`.
Packages (manifest + lock agree): Input System 1.19.0, URP 17.5.0, Test Framework 1.7.0, uGUI 2.5.0, Coplay Unity MCP v10.0.0 (resolved 7b7db7b31f4e).
Existing multiplayer.center is template tooling, preserved; no network gameplay package added.
Render: URP, PC/Mobile renderer assets preserved. Active input handler 1 (new Input System).
Source serialization: Force Text (2); version control: Visible Meta Files. Existing binary LFS policy preserved.

No applicable AGENTS.md found in project or parent directories. Template AGENTS is documentation, not active instructions.
Existing gameplay inventory: SampleScene, template InputSystem_Actions (Player/UI maps); tutorial scripts and imported MR POLY demo assets; no player, interaction, door, build script or project test assembly.
Reuse InputSystem_Actions and URP settings. Preserve SampleScene, imported art and deleted HubForceResolve user files.

MCP preflight: no Unity tool exposed after repeated capability discovery; no Unity resources/templates. Config points to http://127.0.0.1:8080/mcp. Two read-only connection attempts, including outside sandbox, returned `curl: (7) Failed to connect to 127.0.0.1 port 8080 ... Couldn't connect to server`. No Editor process exists (process check outside sandbox); previous Editor log ends in shutdown. Thus active scene, live Console and MCP project identity are BLOCKED, not inferred from config.
Fallback: existing Unity CLI, one process at a time. Baseline sample build started before gameplay edits. Live MCP reconnection: open this project and start the installed MCP For Unity server at the configured endpoint, then reconnect the task.

Scope decisions: current user S001 overrides LS-DOC-05 Jump/stamina/vault and broad LS-DOC-03 service layers; no such systems implemented. First-person explicit user requirement resolves open camera decision. Keep URP renderer; Forward+ performance superiority is unmeasured. macOS build is the accessible target; Windows runtime acceptance requires a Windows device. Sources and reconciliation are recorded in S001_SOURCE_RECONCILIATION.md.

Minimal additions: focused runtime assembly (input/motor/look/stance/interaction/door/session/HUD); player and door prefabs; serialized acceptance parkour; Editor generation/validation/build entry point; risk-focused EditMode/PlayMode tests; acceptance evidence. No parallel existing gameplay system to replace.

Initial historical log: no error CS or warning CS matches. Fresh CLI compile/build baseline and final results are in S001_ACCEPTANCE.md; historical log is not a live Console PASS.
