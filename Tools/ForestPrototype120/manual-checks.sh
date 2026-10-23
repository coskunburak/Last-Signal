#!/bin/bash
# Explicit user-run commands only. This script never runs at import or scene load.
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
# Existing action names remain available. Evidence is timestamped instead of overwritten.
# UNITY_EDITOR or --unity overrides executable discovery. No -nographics for HDRP.
exec python3 "$SCRIPT_DIR/../HDRPProductionOptimization/manual_checks.py" "$@" \
  --output "$PROJECT/Docs/Implementation/ForestPrototype120/Evidence"
