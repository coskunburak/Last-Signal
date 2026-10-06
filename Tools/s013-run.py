#!/usr/bin/env python3
"""Run an existing successful S013/S012 macOS build; never edits project/save files."""
import argparse, datetime, json, pathlib, plistlib, subprocess, sys
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser()
p.add_argument('mode',choices=['acceptance','performance'])
p.add_argument('build',type=pathlib.Path,help='Directory containing build.txt and LastSignal.app')
p.add_argument('--tier',choices=['Low','Medium','High'])
p.add_argument('--quick',action='store_true')
p.add_argument('--variant',choices=['shadow-off','vegetation-off'])
p.add_argument('--output',type=pathlib.Path)
a=p.parse_args();build=a.build.resolve();report=build/'build.txt';app=build/'LastSignal.app'
if not report.is_file() or 'Result=Succeeded' not in report.read_text():p.error('Successful build.txt required; a pending/failed build is not runnable evidence.')
if a.mode=='acceptance' and (a.tier or a.quick or a.variant):p.error('Quality and quick flags apply only to performance.')
with (app/'Contents/Info.plist').open('rb') as f:name=plistlib.load(f)['CFBundleExecutable']
exe=app/'Contents/MacOS'/name
if not exe.is_file():p.error('Build executable missing.')
output=a.output.resolve() if a.output else root/'Docs/Implementation/S013/Evidence'/datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ')
output.mkdir(parents=True,exist_ok=False)
cmd=[str(exe),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-s012Acceptance' if a.mode=='acceptance' else '-s013Performance',str(output),'-logFile',str(output/'player.log')]
if a.tier:cmd+=['-s013Tier',a.tier]
if a.quick:cmd+=['-s013Quick']
if a.variant:cmd+=['-s013Variant',a.variant]
(output/'command.json').write_text(json.dumps({'argv':cmd,'build_report':report.read_text(),'started_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()},indent=2))
print('RUN='+str(output),flush=True)
launch=['open','-n','-W',str(app),'--args']+cmd[1:]
(output/'launch.json').write_text(json.dumps({'argv':launch,'reason':'LaunchServices registers one foreground macOS app with the acceptance arguments.'},indent=2))
result=subprocess.run(launch,cwd=root)
result_file=output/('standalone.txt' if a.mode=='acceptance' else 'result.txt')
text=result_file.read_text() if result_file.is_file() else 'BLOCKED: process ended without a result artifact'
print(text,flush=True)
(output/'process-result.json').write_text(json.dumps({'returncode':result.returncode,'ended_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'result':text},indent=2))
sys.exit(0 if result.returncode==0 and 'PASS' in text and 'FAIL' not in text else 1)
