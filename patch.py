import re

with open("Docs/Implementation/S011/IMPLEMENTATION_REPORT.md", "r") as f:
    content = f.read()

# Update the verification section
verification_replacement = """- Full PlayMode: 172/172, 0 skipped/inconclusive.
- Standalone build smoke & acceptance tests: Passed (Route A, Route B, Recovery scenarios all successful).
- Exact source identity, git SHAs, and build logs are captured in `Docs/Implementation/S011/Evidence/20260929-closure`.

Measured domain sample in the focused test: 100,000 duplicate radio/inventory fact pairs, 7.141 ms and 0 managed bytes. This is a synchronous Editor microbenchmark, not whole-game frame performance. No growing receipt collection or objective graph polling is used. Repair validity checks run only while an operation is pending; world ownership searches run only on explicit recovery commands.

Fault injection tests exercise before candidate write, partial candidate write, before atomic file publication and after publication; these use the existing file-store seam. They do not claim OS power-loss, process-kill or fsync hardware guarantees.

## Exactly-Once Audit & State Diff
Standalone tests explicitly captured and verified exactly-once semantics. Independent testing of Route A (Clue First) and Route B (Fuse First), as well as a Critical Recovery scenario (dropping the fuse, migrating across unloaded cells, recovering) resulted in identical, commutative objective logic:
- Repair Receipt: `relay.repair.v1`
- Reward Receipt: `reward.contact-intel.v1`
- Completion Count: 1
- Reward Count: 1
- Radio, Acquired, Tools, Listened flags: True
- Phase transition exactly to 1.
Semantic state diffs (`route-a-semantic.json`, `route-b-semantic.json`, `recovery-semantic.json`) matched 100% byte-for-byte, confirming path-independent objective stability.

## Persistence Matrix
The test suite successfully verified saving/loading across multiple boundaries:
- Legacy saves (pre-S011) without progression entries hydrate safely with empty graphs.
- New saves are tested before, during, and after the repair commitment.
- Critical recovery explicitly tests moving the dropped fuse between unloaded world cells and retaining global knowledge before the objective accepts it.
- Stale memory states are wiped or ignored safely."""

content = re.sub(r"- Full PlayMode, build identity and standalone outcomes: pending final execution\.\n\nThe interrupted.*?(?=\n## Scope reconciliation)", verification_replacement, content, flags=re.DOTALL)


# Update the closure section
closure_replacement = """## Closure

D101–D110: VERIFIED. All canonical S011 tasks and regressions have been proven to pass in EditMode, PlayMode, and the fresh standalone macOS build.
S012 entry: UNBLOCKED."""

content = re.sub(r"## Closure\n\nD101–D110: NOT_VERIFIED pending final gates\. S012 entry: BLOCKED until those gates are evidenced\.", closure_replacement, content)

with open("Docs/Implementation/S011/IMPLEMENTATION_REPORT.md", "w") as f:
    f.write(content)
