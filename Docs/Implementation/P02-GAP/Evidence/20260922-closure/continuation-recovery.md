# Continuation recovery

The previous full PlayMode run completed: 109 total, 109 passed, 0 failed/skipped/inconclusive, 348.6010612 seconds, ending 2026-09-22 00:22:09 UTC. This is pre-final-fix diagnostic evidence (`../20260922-entry/full-playmode.xml`).

Continuation audit found the exposure fix and new load tests absent from Assets. Temp staging and the earlier `/tmp/p02-historical-evidence-backup` were no longer present. No git restore was attempted, because it would discard pre-existing local evidence changes. The current 1,237 historical evidence files were backed up in the workspace before final test execution; final-run outputs will be archived and those exact pre-final files restored. Earlier pre-continuation overwritten metrics cannot be reconstructed safely from the unavailable backup; this provenance limitation is retained explicitly.

No Unity editor process was active. The earlier directly exposed Unity MCP tools were no longer available in the new turn; the configured relay initialized but did not return tool inventory, and HTTP MCP port 8080 refused connection. Unity native UI inspection also timed out. The installed Unity 6000.5 editor was therefore run through the same batch-test procedure already present in run_all.sh. No new package, external service or second test framework was installed.

Applied fix: SaveSession.LoadCore now calls WorldClock.Restore after Hydrate, Physics.SyncTransforms and PlayerLocationClear, immediately before CompleteRestore. WorldClock.Restore recomputes roof exposure and clears debounce state at the restored pose. Added rainy indoor/outdoor exact wetness and transform assertions, plus real schema-1 scene migration. Added malformed schema-2 wetness and +90m fatal scheduling tests.
