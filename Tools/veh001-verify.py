#!/usr/bin/env python3
"""VEH-001 verification through existing open-Editor bridges; never starts another Unity.
Copies completed run evidence to a fresh VEH folder, including failures. Zero tests cannot PASS.
"""
import argparse
import datetime
import json
import pathlib
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('stage', choices=['zombie', 'focused', 'edit', 'play', 'build'])
p.add_argument('--timeout', type=int, default=1800)
p.add_argument('--clean-cache', action='store_true', help='For build stage, force a fresh Unity player build.')
p.add_argument('--test-class', help='Run one registered class from the focused suite, preserving VEH evidence.')
a = p.parse_args()
classes = [
    ('edit', 'VehicleZombieImpactTests'), ('play', 'VehicleZombieIntegrationTests'), ('play', 'VehicleZombieHearingTests'), ('play', 'VehicleZombieDominanceTests'),
    ('edit', 'VehicleFoundationTests'), ('edit', 'VehicleNoiseTests'),
    ('edit', 'VehicleExitTests'), ('edit', 'VehicleSaveTests'),
    ('edit', 'VehiclePresentationMathTests'),
    ('play', 'VehicleIntegrationTests'), ('play', 'VehicleOccupancyTests'),
    ('play', 'VehiclePresentationTests'),
]
runs = classes[:4] if a.stage == 'zombie' else classes if a.stage == 'focused' else [(a.stage, '')]
if a.test_class:
    if a.stage != 'focused': p.error('--test-class is only valid for focused')
    runs = [(mode, name) for mode, name in classes if name == a.test_class]
    if not runs: p.error('Unknown focused test class: ' + a.test_class)
if a.clean_cache and a.stage != 'build': p.error('--clean-cache is only valid for build')
stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ')
# Create this only after the legacy wrapper has restored its historical evidence backup.
checkpoint = ROOT / 'Docs/Implementation/VEH-001/Evidence' / (stamp + '-' + a.stage + ('-clean' if a.clean_cache else ''))
reports = []
exit_code = 0
for mode, test_class in runs:
    if mode == 'build':
        cmd = [sys.executable, str(ROOT / 'Tools/s013-build.py'), 'production', '--timeout', str(a.timeout)]
        if a.clean_cache: cmd.append('--clean-cache')
    else:
        cmd = [sys.executable, str(ROOT / 'Tools/s012-verify.py'), mode, '--timeout', str(a.timeout)]
        if test_class:
            cmd += ['--filter', 'LastSignal.Tests.' + test_class]
    print('\nRUNNING ' + (test_class or mode), flush=True)
    process = subprocess.Popen(cmd, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    lines = []
    source = None
    for line in process.stdout:
        print(line, end='', flush=True)
        lines.append(line)
        if line.startswith('RUN=') or line.startswith('BUILD='):
            source = pathlib.Path(line.partition('=')[2].strip())
    code = process.wait()
    if code == 2:
        # A timeout can mean Unity is still running. Do not write into the wrapper's
        # backup scope or dispatch another job while its state is unresolved.
        print('BEKLE: Unity koşusu bitmemiş olabilir. Yeniden çalıştırmadan bu çıktıyı paylaş.', flush=True)
        sys.exit(2)
    checkpoint.mkdir(parents=True, exist_ok=True)
    label = test_class or mode
    target = checkpoint / label
    target.mkdir()
    (target / 'console.txt').write_text(''.join(lines))
    summary = {'stage': mode, 'class': test_class, 'exit': code, 'source': str(source) if source else None}
    if source and source.is_dir():
        if mode == 'build':
            for name in ['build.txt', 'warnings.txt']:
                file = source / name
                if file.is_file(): shutil.copy2(file, target / name)
        else:
            for file in source.iterdir():
                if file.is_file(): shutil.copy2(file, target / file.name)
            xml = source / (mode + '.xml')
            if xml.is_file():
                result = ET.parse(xml).getroot()
                summary.update(result.attrib)
                if int(result.get('total', '0')) == 0 or int(result.get('passed', '0')) == 0 or result.get('failed') != '0' or result.get('result') != 'Passed':
                    code = 1
                for case in result.iter('test-case'):
                    if case.get('result') == 'Failed':
                        print(case.get('fullname'), case.findtext('failure/message'), case.findtext('failure/stack-trace'), flush=True)
            else:
                code = 1
    else:
        code = 1
    summary['verifiedExit'] = code
    reports.append(summary)
    (checkpoint / 'summary.json').write_text(json.dumps(reports, indent=2))
    if code:
        exit_code = code
        break
print('\nVEH-001 RESULT=' + ('PASS' if exit_code == 0 else 'FAIL'), flush=True)
print('EVIDENCE=' + str(checkpoint), flush=True)
for report in reports:
    print((report['class'] or report['stage']) + ': ' + report.get('passed', '?') + '/' + report.get('total', '?') + ' passed; exit=' + str(report['verifiedExit']), flush=True)
sys.exit(exit_code)
