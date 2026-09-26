#!/bin/bash
rm -f EditModeRegression.xml PlayModeRegression.xml

echo "Running EditMode regression..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults EditModeRegression.xml
echo "EditMode regression finished. Exit code: $?"

echo "Running PlayMode regression..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults PlayModeRegression.xml
echo "PlayMode regression finished. Exit code: $?"

echo "Producing macOS Development Build..."
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -executeMethod BuildUtility.BuildP5Mac -quit
echo "Build finished. Exit code: $?"

echo "RESULTS:"
grep -Eo "total=\"[0-9]+\" passed=\"[0-9]+\" failed=\"[0-9]+\"" EditModeRegression.xml | head -n 1
grep -Eo "total=\"[0-9]+\" passed=\"[0-9]+\" failed=\"[0-9]+\"" PlayModeRegression.xml | head -n 1
