#!/bin/bash
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath . -testPlatform EditMode -testResults /Users/burakcoskun/Last\ Signal/full-editmode-post-integrity.xml
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults /Users/burakcoskun/Last\ Signal/full-playmode-post-integrity.xml
