#!/bin/zsh

set -u

AUTOMATION_DIR="/Users/burakcoskun/Last Signal/Docs/Implementation/S004/Automation"
PROJECT_ROOT="/Users/burakcoskun/Last Signal"

PROMPT="$AUTOMATION_DIR/S004_P2_PROMPT.md"
LOG="$AUTOMATION_DIR/S004_P2_20260918_1525.log"
STATUS="$AUTOMATION_DIR/S004_P2_20260918_1525.status"

TARGET="2026-09-18 15:25:00"

echo "SCHEDULED" > "$STATUS"
echo "Target: $TARGET" >> "$STATUS"
echo "Project: $PROJECT_ROOT" >> "$STATUS"
echo "Model: gpt-6-astra" >> "$STATUS"
echo "Reasoning: medium" >> "$STATUS"
echo "Prompt: $PROMPT" >> "$STATUS"

if [ ! -s "$PROMPT" ]; then
    echo "ERROR: Prompt file missing or empty." >> "$STATUS"
    exit 1
fi

TARGET_EPOCH=$(date -j -f "%Y-%m-%d %H:%M:%S" "$TARGET" "+%s")
NOW_EPOCH=$(date "+%s")
WAIT=$((TARGET_EPOCH - NOW_EPOCH))

if [ "$WAIT" -gt 0 ]; then
    echo "Waiting ${WAIT}s" >> "$STATUS"
    sleep "$WAIT"
fi

echo "STARTED: $(date)" >> "$STATUS"

cd "$PROJECT_ROOT" || exit 1

cat "$PROMPT" | codex exec \
    -m gpt-6-astra \
    --sandbox workspace-write \
    -c 'approval_policy="never"' \
    -c 'model_reasoning_effort="medium"' \
    - \
    > "$LOG" 2>&1

EXIT_CODE=$?

echo "FINISHED: $(date)" >> "$STATUS"
echo "EXIT_CODE: $EXIT_CODE" >> "$STATUS"

exit "$EXIT_CODE"
