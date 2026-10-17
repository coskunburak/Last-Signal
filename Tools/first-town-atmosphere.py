#!/usr/bin/env python3
"""User-run focused Unity verification. Each invocation runs exactly one requested action."""
import argparse
import datetime
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('action', choices=['compile', 'references', 'editmode', 'playmode'])
parser.add_argument('--unity', type=Path, default=UNITY)
args = parser.parse_args()
if (ROOT / 'Temp/UnityLockfile').exists():
    parser.error('Close this project in Unity before batch verification. With the editor open, use the atmosphere menu / Test Runner.')
if not args.unity.is_file():
    parser.error('Unity executable missing; supply --unity.')
if '6000.5.0f1' not in args.unity.parts:
    parser.error('Use project Unity version 6000.5.0f1.')
stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f')
out = ROOT / 'Docs/Implementation/FirstTownAtmosphere/Evidence' / (stamp + '-' + args.action)
out.mkdir(parents=True)
cmd = [str(args.unity), '-batchmode', '-projectPath', str(ROOT), '-logFile', str(out / 'unity.log')]
if args.action in ['editmode', 'playmode']:
    filt = 'LastSignal.Tests.WorldTimeTests;LastSignal.Tests.FirstTownPersistenceTests' if args.action == 'editmode' else 'LastSignal.Tests.WorldTimeIntegrationTests;LastSignal.Tests.FirstTownPlayTests'
    cmd += ['-runTests', '-testPlatform', 'EditMode' if args.action == 'editmode' else 'PlayMode', '-testFilter', filt, '-testResults', str(out / 'tests.xml')]
else:
    cmd += ['-quit']
    if args.action == 'references':
        cmd += ['-executeMethod', 'LastSignal.Editor.FirstTown.FirstTownAtmosphereVerification.Validate']
(out / 'command.json').write_text(json.dumps(cmd, indent=2) + '\n')
result = subprocess.run(cmd, cwd=ROOT)
(out / 'process.json').write_text(json.dumps({'action': args.action, 'exitCode': result.returncode,
    'acceptance': 'REVIEW_LOG_AND_XML', 'visual': 'NOT_RUN', 'performance': 'NOT_RUN'}, indent=2) + '\n')
print('Evidence:', out)
raise SystemExit(result.returncode)
