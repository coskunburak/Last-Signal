#!/bin/zsh
set -euo pipefail
cd "${0:A:h:h}"
unity_editor='/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
run_name="${2:-local-$(date -u +%Y%m%dT%H%M%SZ)}"
evidence_path="Docs/Implementation/S001/Evidence/$run_name"
mkdir -p "$evidence_path" Logs
export LAST_SIGNAL_EVIDENCE="$evidence_path"
case "${1:-}" in
  author) "$unity_editor" -batchmode -quit -projectPath "$PWD" -executeMethod LastSignal.Editor.S001Project.CreateAssets -logFile "Logs/$run_name-author.log" ;;
  edit) "$unity_editor" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter LastSignal.Tests -testResults "$evidence_path/test-results-editmode.xml" -logFile "Logs/$run_name-edit.log" ;;
  play) "$unity_editor" -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode -testFilter LastSignal.Tests -testResults "$evidence_path/test-results-playmode.xml" -logFile "Logs/$run_name-play.log" ;;
  build) "$unity_editor" -batchmode -quit -projectPath "$PWD" -executeMethod LastSignal.Editor.S001Project.Build -logFile "Logs/$run_name-build.log" ;;
  *) print 'Usage: zsh Tools/s001.sh author|edit|play|build [run-id]'; exit 2 ;;
esac
