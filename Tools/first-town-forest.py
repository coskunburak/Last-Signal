#!/usr/bin/env python3
"""Owner-run verification only; staging/promotion are explicit Unity Editor menu actions."""
import argparse
import datetime
import json
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[1]
UNITY=Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('action',choices=['compile','editmode','playmode','full-editmode','full-playmode'])
p.add_argument('--unity',type=Path,default=UNITY)
a=p.parse_args()
if (ROOT/'Temp/UnityLockfile').exists():p.error('Save and close this Unity project before batch verification. Use editor menus/Test Runner while open.')
if not a.unity.is_file() or '6000.5.0f1' not in a.unity.parts:p.error('Supply the project Unity 6000.5.0f1 executable.')
out=ROOT/'Docs/Implementation/FirstTownCoreForest/Evidence'/(datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f')+'-'+a.action)
out.mkdir(parents=True)
cmd=[str(a.unity),'-batchmode','-projectPath',str(ROOT),'-logFile',str(out/'unity.log')]
if a.action=='compile':cmd+=['-quit']
else:
 cmd+=['-runTests','-testPlatform','EditMode' if a.action.endswith('editmode') else 'PlayMode','-testResults',str(out/'tests.xml')]
 if a.action=='editmode':cmd+=['-testFilter','LastSignal.Tests.WorldTimeTests;LastSignal.Tests.FirstTownPersistenceTests']
 if a.action=='playmode':cmd+=['-testFilter','LastSignal.Tests.FirstTownPlayTests;LastSignal.Tests.WorldTimeIntegrationTests;LastSignal.Tests.VehicleIntegrationTests']
(out/'command.json').write_text(json.dumps(cmd,indent=2)+'\n')
r=subprocess.run(cmd,cwd=ROOT)
(out/'process.json').write_text(json.dumps({'exitCode':r.returncode,'acceptance':'REVIEW_LOG_AND_TEST_XML','visual':'NOT_RUN','performance':'NOT_MEASURED'},indent=2)+'\n')
print('Evidence:',out)
raise SystemExit(r.returncode)
