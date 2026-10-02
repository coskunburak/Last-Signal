#!/bin/bash
# Önce bu projenin açık Unity editörünü kapatın. Ağır seçenekleri kullanıcı çalıştırır.
set -euo pipefail
UNITY_BIN='/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
PROJECT='/Users/burakcoskun/Last Signal'
MODE="${1:-focused-edit}"
OUT="$PROJECT/Docs/Implementation/S004/Evidence/20261001-BloodVFX/user-$(date +%Y%m%d-%H%M%S)-$MODE"
mkdir -p "$OUT"
COMMON=(-batchmode -projectPath "$PROJECT" -logFile "$OUT/unity.log")
case "$MODE" in
  focused-edit) PLATFORM=EditMode; FILTER='LastSignal.Tests.ZombieBloodVfxTests' ;;
  focused-play) PLATFORM=PlayMode; FILTER='LastSignal.Tests.ZombieBloodVfxPlayTests' ;;
  regression-edit) PLATFORM=EditMode; FILTER='LastSignal.Tests.ZombieBloodVfxTests;LastSignal.Tests.ZombieDamageTests;LastSignal.Tests.ZombieCompositionTests;LastSignal.Tests.WorldPopulationManagerTests' ;;
  regression-play) PLATFORM=PlayMode; FILTER='LastSignal.Tests.ZombieBloodVfxPlayTests;LastSignal.Tests.ZombieDismembermentPlayTests;LastSignal.Tests.ZombiePresentationTests' ;;
  full-edit) PLATFORM=EditMode; FILTER='' ;;
  full-play) PLATFORM=PlayMode; FILTER='' ;;
  production-ai-profile) PLATFORM=PlayMode; FILTER='LastSignal.Tests.ZombieProductionAcceptanceTests.ProfileOneTenAndTwentyFiveProductionActors' ;;
  thirty-ai-profile) PLATFORM=PlayMode; FILTER='LastSignal.Tests.R04IntegrationPlayTests.PerformanceThirtyAgentIntegrated' ;;
  validate) exec "$UNITY_BIN" "${COMMON[@]}" -executeMethod LastSignal.Editor.ZombieBloodVfxAuthoring.Validate -quit ;;
  build) exec "$UNITY_BIN" "${COMMON[@]}" -executeMethod LastSignal.Editor.StudioNewPunchBuild.BuildDevelopmentMac -quit ;;
  *) echo 'Seçenek: focused-edit focused-play regression-edit regression-play full-edit full-play validate build production-ai-profile thirty-ai-profile' >&2; exit 2 ;;
esac
if [ "$PLATFORM" = EditMode ]; then ASSEMBLY=LastSignal.EditModeTests; else ASSEMBLY=LastSignal.PlayModeTests; fi
ARGS=(-runTests -testPlatform "$PLATFORM" -assemblyNames "$ASSEMBLY" -testResults "$OUT/results.xml")
if [ -n "$FILTER" ]; then ARGS+=(-testFilter "$FILTER"); fi
# Grafik PlayMode için -nographics EKLEMEYİN. Test çalıştırıcı çıkışı kendisi yönetir.
exec "$UNITY_BIN" "${COMMON[@]}" "${ARGS[@]}"
