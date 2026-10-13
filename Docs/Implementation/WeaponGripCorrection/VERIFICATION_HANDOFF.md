# User-operated verification
All gates are NOT_RUN. Save and close Unity normally before invoking a gate. The runner refuses an active Unity process/open project lock; it does not launch a competing Editor. It resolves Unity from ProjectVersion.txt, retains process exit codes, requires nonempty passing NUnit cases, treats skipped/inconclusive tests as failure, records source fingerprints and writes a unique Evidence directory. No PASS evidence was created by this implementation.

Run ONE gate, send gate-result.json and failure-excerpt.txt (plus relevant XML/log excerpts), and wait for review before the next. Commands below are independent alternatives, never a chain.

| Gate | Command |
|---|---|
| First: compile | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" compile` |
| Focused EditMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" grip-edit` |
| Focused PlayMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" grip-play` |
| Character EditMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" character-edit` |
| Character + vehicle PlayMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" character-play` |
| Combat EditMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" combat-edit` |
| Combat PlayMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" combat-play` |
| Full EditMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" regression-edit` |
| Full PlayMode | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" regression-play` |
| Development build (existing macOS S021 build target) | `python3 "/Users/burakcoskun/Last Signal/Tools/weapon-grip-verify.py" development-build` |
| Manual visual route | Open S013Cabin in the existing Editor and start Play yourself; follow VISUAL_QA.md. No automated command can substitute for visual acceptance. |

The build gate uses the existing Mac development builder on this host; a Windows release build is a separate later release gate, not claimed covered here. `--filter` is available for a focused failing NUnit retest. Existing S021 regression legacy/source preservation handling is reused, not bypassed.

Editor MCP was used only for hierarchy/mesh inspection, source import needed for authoring, and saving prefabs/profiles/controllers. Incidental imports and authoring-command compilation are not a Unity compilation verification gate. No EditMode/PlayMode/batchmode/build/benchmark/gameplay acceptance was launched.

Remaining acceptance work: iron-sight picture, finger/thumb penetration and clothing twist, shoulder anchors across FOV/aspect ratios, heavy swing reach/clamping, procedural reload choreography, visibility/seating regressions. User result must drive the next smallest repair.
