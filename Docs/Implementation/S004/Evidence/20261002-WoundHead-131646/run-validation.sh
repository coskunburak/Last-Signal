#!/bin/bash
# Bu projenin açık Unity editörünü kapatın; her çağrı ayrı kanıt klasörü oluşturur.
set -euo pipefail
UNITY_BIN='/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
PROJECT='/Users/burakcoskun/Last Signal'
EVIDENCE="$(cd "$(dirname "$0")" && pwd)"
MODE="${1:-edit}"
case "$MODE" in
 edit) PLATFORM=EditMode; ASSEMBLY=LastSignal.EditModeTests; FILTER='LastSignal.Tests.ZombieBloodVfxTests' ;;
 play) PLATFORM=PlayMode; ASSEMBLY=LastSignal.PlayModeTests; FILTER='LastSignal.Tests.ZombieBloodVfxPlayTests;LastSignal.Tests.ZombieDismembermentPlayTests' ;;
 *) echo 'Kullanım: bash run-validation.sh edit|play' >&2; exit 2 ;;
esac
OUT="$(mktemp -d "$EVIDENCE/user-$(date +%Y%m%d-%H%M%S)-$MODE-XXXXXX")"
printf 'XML: %s/results.xml\nLOG: %s/unity.log\n' "$OUT" "$OUT"
set +e
"$UNITY_BIN" -batchmode -projectPath "$PROJECT" -logFile "$OUT/unity.log" \
 -runTests -testPlatform "$PLATFORM" -assemblyNames "$ASSEMBLY" \
 -testFilter "$FILTER" -testResults "$OUT/results.xml"
STATUS=$?
set -e
printf '%s\n' "$STATUS" > "$OUT/exit-code.txt"
printf 'Sonuç klasörü: %s\nÇıkış kodu: %s (XML incelemesi gereklidir)\n' "$OUT" "$STATUS"
exit "$STATUS"
