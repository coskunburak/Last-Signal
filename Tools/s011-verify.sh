#!/bin/bash
set -eu
s011_root="$(cd "$(dirname "$0")/.." && pwd)"
s011_editor="/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity"
s011_evidence="${S011_EVIDENCE:-$s011_root/Docs/Implementation/S011/Evidence/20260929-closure}"
s011_args=(-batchmode -projectPath "$s011_root")
if [ -n "${S011_LICENSE_IPC:-}" ]; then s011_args+=(-licensingIpc "$S011_LICENSE_IPC"); fi
mkdir -p "$s011_evidence"
cd "$s011_root"
suite() {
  local platform="$1" name="$2" filter="$3"
  local args=("${s011_args[@]}" -runTests -testPlatform "$platform" -testResults "$s011_evidence/$name.xml" -logFile "$s011_evidence/$name.log")
  if [ -n "$filter" ]; then args+=(-testFilter "$filter"); fi
  "$s011_editor" "${args[@]}"
}
case "${1:-focused}" in
 focused) suite EditMode focused-editmode-final S011; suite PlayMode focused-playmode-final S011 ;;
 regression) suite EditMode regression-editmode 'Inventory;Save;Shelter;WorldTime;Noise;S010'; suite PlayMode regression-playmode 'Inventory;Persistence;WorldTime;R02Noise;R05Session;WorldPopulation;S010' ;;
 full) suite EditMode full-editmode ''; suite PlayMode full-playmode '' ;;
 author) "$s011_editor" "${s011_args[@]}" -quit -executeMethod LastSignal.Editor.S011Authoring.Author -logFile "$s011_evidence/authoring.log" ;;
 build) "$s011_editor" "${s011_args[@]}" -quit -executeMethod LastSignal.Editor.S011Authoring.Build -logFile "$s011_evidence/build.log" ;;
 standalone) "$s011_root/Builds/S011/LastSignal.app/Contents/MacOS/Last Signal" -s011Acceptance "$s011_evidence" -logFile "$s011_evidence/player.log" ;;
 *) exit 2 ;;
esac
