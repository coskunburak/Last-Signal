#!/bin/bash
set -eu
unity_editor="/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity"
s010_evidence="$PWD/Docs/Implementation/S010/Evidence/20260928-closure"
s010_ipc="${S010_LICENSE_IPC:-Unity-LicenseClient-burakcoskun-6000.5.0}"
run_suite() {
  local platform="$1" name="$2" filter="$3"
  local args=(-batchmode -licensingIpc "$s010_ipc" -projectPath "$PWD" -runTests -testPlatform "$platform" -testResults "$s010_evidence/$name.xml" -logFile "/tmp/s010-$name.log")
  if [ -n "$filter" ]; then args+=(-testFilter "$filter"); fi
  "$unity_editor" "${args[@]}"
}
case "${1:-focused}" in
 focused) run_suite EditMode focused-editmode-final S010ProductionTests; run_suite PlayMode focused-playmode-final S010IntegrationTests ;;
 regression) run_suite EditMode regression-editmode 'Inventory;Save;Shelter;WorldTime;Noise'; run_suite PlayMode regression-playmode 'Inventory;Persistence;WorldTime;R02Noise;R05Session;WorldPopulation' ;;
 full) run_suite EditMode full-editmode ''; run_suite PlayMode full-playmode '' ;;
 build) "$unity_editor" -batchmode -licensingIpc "$s010_ipc" -projectPath "$PWD" -quit -executeMethod LastSignal.Editor.S010Authoring.Build -logFile /tmp/s010-build.log ;;
 *) exit 2 ;;
esac
