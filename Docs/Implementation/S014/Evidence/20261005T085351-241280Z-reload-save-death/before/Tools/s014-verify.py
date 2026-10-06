#!/usr/bin/env python3
"""User-invoked S014 verification. Close Unity first; never overwrite prior evidence."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=['focused-edit', 'focused-play'])
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    unity = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
    if not unity.is_file():
        parser.error('Doğrulanmış Unity executable bulunamadı: ' + str(unity))
    if (root / 'Temp/UnityLockfile').exists():
        parser.error('Unity Editor kapalı olmalı. Kilit dosyasına dokunulmadı; açık Editor’dan çıkıp yeniden deneyin.')
    platform = 'EditMode' if args.gate.endswith('edit') else 'PlayMode'
    run = root / 'Docs/Implementation/S014/Evidence' / (
        datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ') + '-' + args.gate)
    run.mkdir(parents=True, exist_ok=False)
    xml = run / 'tests.xml'
    log = run / 'unity.log'
    command = [str(unity), '-batchmode', '-projectPath', str(root), '-runTests',
               '-testPlatform', platform, '-testResults', str(xml), '-logFile', str(log)]
    if args.gate == 'focused-edit':
        command += ['-assemblyNames', 'LastSignal.EditModeTests', '-testFilter', 'LastSignal.Tests.WeaponStateTests']
    elif args.gate == 'focused-play':
        command += ['-assemblyNames', 'LastSignal.PlayModeTests', '-testFilter', 'LastSignal.Tests.S014WeaponPresentationTests']
    # No -quit: Unity's test runner owns completion. No second Editor or automatic retry.
    (run / 'command.json').write_text(json.dumps(command, indent=2))
    (run / 'source-head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'source-status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root))
    files = [root / 'Assets/LastSignal/Scripts/Runtime/Combat/WeaponController.cs',
             root / 'Assets/LastSignal/Scripts/Runtime/Combat/WeaponAnimationPresenter.cs',
             root / 'Assets/LastSignal/Scripts/Tests/PlayMode/S014WeaponPresentationTests.cs']
    (run / 'changed-source-hashes.json').write_text(json.dumps({str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}, indent=2))
    print('RUN=' + str(run), flush=True)
    code = subprocess.call(command, cwd=root)
    (run / 'process-result.json').write_text(json.dumps({'exitCode': code}))
    if not xml.exists():
        print('BLOCKED: NUnit XML yok. Log: ' + str(log))
        return 2
    try:
        result = ET.parse(xml).getroot()
    except ET.ParseError as error:
        print('BLOCKED: Geçersiz XML: ' + str(error))
        return 2
    (run / 'result.json').write_text(json.dumps(result.attrib, indent=2))
    print(json.dumps(result.attrib))
    for case in result.iter('test-case'):
        if case.get('result') != 'Passed':
            print(case.get('fullname'), case.get('result'), case.findtext('failure/message', ''))
            print(case.findtext('failure/stack-trace', ''))
    passed = (code == 0 and result.get('result') == 'Passed' and
              int(result.get('total', '0')) > 0 and int(result.get('skipped', '0')) == 0)
    if args.gate == 'focused-play':
        passed = passed and int(result.get('total', '0')) == 5
    print('COMPLETE' if passed else 'FAIL')
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
