#!/bin/bash
set -e
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform editmode -testResults Docs/Implementation/S005/Evidence/run-1789825214/final-editmode.xml -logFile Docs/Implementation/S005/Evidence/run-1789825214/final-editmode.log || true
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform playmode -testResults Docs/Implementation/S005/Evidence/run-1789825214/final-playmode.xml -logFile Docs/Implementation/S005/Evidence/run-1789825214/final-playmode.log || true
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -executeMethod BuildUtility.BuildP5Mac -quit -logFile Docs/Implementation/S005/Evidence/run-1789825214/build.log || true
