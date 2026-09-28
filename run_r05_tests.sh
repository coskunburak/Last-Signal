#!/bin/bash
echo "Running R05 EditMode tests..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath . -testPlatform EditMode -testFilter "R05" -testResults "r05-editmode.xml"
echo "Running R05 PlayMode tests..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath . -testPlatform PlayMode -testFilter "R05" -testResults "r05-playmode.xml"
echo "Running Full EditMode tests..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath . -testPlatform EditMode -testResults "full-editmode-final.xml"
echo "Running Full PlayMode tests..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults "full-playmode-final.xml"
echo "Building Release Candidate..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath . -executeMethod LastSignal.Editor.R05Build.Build -logFile build.log
