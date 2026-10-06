#!/usr/bin/env python3
"""Ask the already-open Unity Editor for a fresh immutable S013/baseline Mac build."""
import argparse, datetime, json, pathlib, subprocess, sys, time
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser()
p.add_argument('variant',choices=['production','baseline'])
p.add_argument('--timeout',type=int,default=1800)
p.add_argument('--clean-cache',action='store_true',help='Force Unity to rebuild cached player data.')
a=p.parse_args()
heartbeat=root/'Temp/LastSignalZombieValidation-heartbeat'
if not heartbeat.is_file() or time.time()-heartbeat.stat().st_mtime>15:
 p.error('Unity Editor must be open and responsive; do not start a second Editor.')
request=root/'Temp/LastSignalS013BuildRequest.json'
if request.exists():p.error('A S013 build request is already pending.')
now=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ')
output=root/'Builds/S013'/(now+'-'+a.variant)
result=root/'Temp'/('LastSignalS013BuildResult-'+now+'.txt')
if output.exists() or result.exists():p.error('Immutable output/result already exists.')
(root/'Builds/S013').mkdir(parents=True,exist_ok=True)
request.write_text(json.dumps({'output':str(output),'baseline':a.variant=='baseline','clean':a.clean_cache,'result':str(result)}))
print('BUILD='+str(output),flush=True)
started=time.monotonic()
while not result.exists() and time.monotonic()-started<a.timeout:
 time.sleep(2)
if not result.exists():
 print('BLOCKED: Unity did not report completion. Inspect Editor.log and the build directory. Do not retry until the Editor is idle.',flush=True)
 sys.exit(2)
message=result.read_text();print(message,flush=True)
report=output/'build.txt'
if report.is_file():print(report.read_text(),flush=True)
sys.exit(0 if message.startswith('PASS:') and report.is_file() and 'Result=Succeeded' in report.read_text() else 1)
