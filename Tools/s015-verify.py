#!/usr/bin/env python3
"""Burak'ın başlatacağı S015 odaklı doğrulama; ikinci Editor ve otomatik tekrar yok."""
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
    parser.add_argument('--filter', help='Yalnız başarısız S015 testinin tam adı')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    unity = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
    if not unity.is_file():
        parser.error('Unity executable bulunamadı: ' + str(unity))
    if (root / 'Temp/UnityLockfile').exists():
        parser.error('Bu komuttan önce Unity Editor’ı kapat. Kilit dosyasına dokunulmadı.')
    edit = args.gate == 'focused-edit'
    suite = 'LastSignal.Tests.S015AudioTests' if edit else 'LastSignal.Tests.S015AudioPlayTests'
    if args.filter and not args.filter.startswith(suite + '.'):
        parser.error('--filter seçilen S015 sınıfının tam test adı olmalı.')
    run = root / 'Docs/Implementation/S015/Evidence' / (
        datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ') + '-' + args.gate)
    run.mkdir(parents=True, exist_ok=False)
    xml, log = run / 'tests.xml', run / 'unity.log'
    command = [str(unity), '-batchmode', '-projectPath', str(root), '-runTests',
               '-testPlatform', 'EditMode' if edit else 'PlayMode',
               '-assemblyNames', 'LastSignal.EditModeTests' if edit else 'LastSignal.PlayModeTests',
               '-testFilter', args.filter or suite, '-testResults', str(xml), '-logFile', str(log)]
    (run / 'command.json').write_text(json.dumps(command, indent=2))
    (run / 'source-head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'source-status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root))
    paths = [p for folder in ['Assets/LastSignal/Scripts', 'Assets/LastSignal/Audio', 'Assets/LastSignal/Prefabs',
                             'Assets/LastSignal/Scenes/Production'] for p in (root / folder).rglob('*') if p.is_file()]
    before = {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    (run / 'source-hashes.json').write_text(json.dumps(before, indent=2))
    print('RUN=' + str(run), flush=True)
    code = subprocess.call(command, cwd=root)
    (run / 'process-result.json').write_text(json.dumps({'exitCode': code}))
    changed = [p for p, digest in before.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != digest]
    (run / 'source-preservation.json').write_text(json.dumps({'changed': changed}, indent=2))
    if not xml.is_file():
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
    total = int(result.get('total', '0'))
    expected = 1 if args.filter else 14 if edit else 6
    passed = code == 0 and not changed and result.get('result') == 'Passed' and total == expected and int(result.get('skipped', '0')) == 0
    if changed:
        print('BLOCKED: Koşu sırasında kaynak değişti: ' + ', '.join(changed[:10]))
    print('COMPLETE' if passed else 'FAIL — beklenen test sayısı: ' + str(expected))
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
