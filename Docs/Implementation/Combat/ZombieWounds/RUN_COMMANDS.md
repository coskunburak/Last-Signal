# User-operated verification

The focused EditMode rerun passed all 147 cases and was manually accepted after review of its expected exception. The first focused PlayMode run had six failures; the corrections await a PlayMode rerun. Save your work and close Unity normally before invoking a gate. The runner refuses an open Unity Editor, never deletes its lock and never starts a second instance against the open project. It reads the project version, resolves an installed Editor, retains graphics and creates a unique UTC evidence directory. Use `--unity /path/to/Unity.app` only if automatic installation discovery cannot find yours.

Accepted focused EditMode command (run only if needed again):

```bash
cd "/Users/burakcoskun/Last Signal"
python3 Tools/zombie-wounds-verify.py focused-edit
```

Rerun this gate; return its new evidence and wait for analysis:

```bash
cd "/Users/burakcoskun/Last Signal"
python3 Tools/zombie-wounds-verify.py focused-play
```

After both focused gates are accepted, run the following separately when requested:

```bash
python3 Tools/zombie-wounds-verify.py regression-edit
```

```bash
python3 Tools/zombie-wounds-verify.py regression-play
```

```bash
python3 Tools/zombie-wounds-verify.py development-build
```

There is no implicit chain, retry, build or compilation-only gate. Focused Play excludes the pre-existing population performance benchmark; regression includes the existing suites. Development build reuses `LastSignal.Editor.CombatFeedbackBuild.DevelopmentMac`, requires its real BuildReport summary and output app, and does not treat test exit status as build evidence.

Each run prints a new directory under `Docs/Implementation/Combat/ZombieWounds/Evidence/`. Return:

- `gate-result.json`, `invocation.json`, `command.txt`, `exit-code.txt`.
- `results.xml` and `Unity.log`; if XML is missing, return the remaining files.
- Build only: `build-summary.txt` and `Unity.log`.
- Relevant `generated-artifacts/` files, including legacy fixture diagnostics copied into this run.

The runner preserves historical paths used by existing test fixtures through the existing `s015-regression.py` helper. It records Git HEAD/status/stat. It checks nonempty XML, required source-declared test methods and minimum TestCase variants, passing case results, process status and compiler/shader/crash diagnostics. Exception candidates force `REVIEW_REQUIRED` even if NUnit passed, so expected LogAssert messages can be reconciled with actual evidence rather than silently dismissed. Exit 0 alone never establishes acceptance. Runner process codes: 0 accepted automated evidence, 1 failure, 2 manual review required (or argument/preflight error before a run).

The runner was inspected and its Python source parsed by the agent. The user invoked `focused-edit` twice and `focused-play` once; focused EditMode is accepted, focused PlayMode is pending a rerun. All later gates remain pending. No tests, benchmarks, builds or compilation verification were executed by the agent.
