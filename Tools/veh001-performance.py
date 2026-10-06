#!/usr/bin/env python3
"""Run the VEH-001 opt-in capture in an already successful macOS Development build."""
import argparse
import datetime
import json
import pathlib
import plistlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('build', type=pathlib.Path, help='Directory containing build.txt and LastSignal.app')
p.add_argument('--timeout', type=int, default=600)
p.add_argument('--active-infected', action='store_true')
a = p.parse_args()
build = a.build.resolve()
report = build / 'build.txt'
app = build / 'LastSignal.app'
if not report.is_file() or 'Result=Succeeded' not in report.read_text():
    p.error('A successful build.txt is required.')
with (app / 'Contents/Info.plist').open('rb') as stream:
    name = plistlib.load(stream)['CFBundleExecutable']
exe = app / 'Contents/MacOS' / name
if not exe.is_file():
    p.error('Build executable missing.')
stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ')
output = ROOT / 'Docs/Implementation/VEH-001/Evidence' / (stamp + '-performance')
output.mkdir(parents=True, exist_ok=False)
launch = ['open', '-n', '-W', str(app), '--args',
          '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
          '-veh001Performance', str(output), '-logFile', str(output / 'player.log')]
if a.active_infected: launch.append('-veh001ActiveInfected')
(output / 'command.json').write_text(json.dumps({
    'argv': launch, 'build': str(build), 'build_report': report.read_text(),
    'started_utc': datetime.datetime.now(datetime.timezone.utc).isoformat()
}, indent=2))
print('RUN=' + str(output), flush=True)
try:
    process = subprocess.run(launch, cwd=ROOT, timeout=a.timeout)
except subprocess.TimeoutExpired:
    (output / 'timeout.txt').write_text('Launcher timed out; check whether the player is still open before another run.\n')
    print('TIMEOUT: Check whether the player is still open. Do not start another run yet.', flush=True)
    sys.exit(2)
result_path = output / 'result.txt'
result = result_path.read_text() if result_path.is_file() else 'FAIL: no result.txt was produced'
print(result, flush=True)
drive = output / 'drive.txt'
if drive.is_file():
    print(drive.read_text(), flush=True)
(output / 'process-result.json').write_text(json.dumps({
    'returncode': process.returncode, 'result': result,
    'ended_utc': datetime.datetime.now(datetime.timezone.utc).isoformat()
}, indent=2))
sys.exit(0 if process.returncode == 0 and result.startswith('PASS capture completed') else 1)
