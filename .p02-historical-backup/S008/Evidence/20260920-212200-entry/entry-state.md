# S008 entry state
Commit: 9576c85db8e7a30abb7c9b2328ddcf2a2dcd45ce
Unity: 6000.5.0f1 (88b47c5e7076). macOS development; PC/Steam commercial target.
Entry working tree is dirty with prior S007 code, materials, project settings and historical S004 evidence changes. Exact modified/untracked paths: entry-git-status.txt. No reset, clean, checkout, revert or commit performed.
Raw binary working-tree diff: entry-diff.patch. LFS clean filter initially failed because .git is read-only; command-local filter bypass captured raw content without changing Git configuration.
S007 report claims PASS with EditMode 136 and PlayMode 84 per user closure. Historical counts are not fresh verification.
Fresh compilation succeeded and entry EditMode: 136/136, failed=0, skipped=0, inconclusive=0, exit=0. PlayMode pending.
Initial sandboxed Unity launch could not connect normally to macOS/licensing services; its two owned processes were stopped. Authorized unsandboxed batch launch successfully ran EditMode. Both logs retained.
Known log noise includes missing native extension messages, duplicate ILPP hint paths and licensing entitlement query 404 messages. These must be classified separately from script/test failures.
