#!/usr/bin/env python3
"""Burak'ın başlatacağı S016 odaklı doğrulama; ikinci Editor ve otomatik tekrar yok."""
import argparse
import datetime
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=['focused-play', 'focused-interaction', 'focused-settings',
                                         'focused-settings-play', 'focused-readability', 'focused-pause',
                                         'focused-save-feedback', 'focused-save-feedback-play', 'persistence',
                                         'regression-edit', 'regression-play', 'focused-regression-fixes',
                                         'focused-movement'])
    parser.add_argument('--filter', help='Seçilen kapıdaki başarısız testin tam adı')
    args = parser.parse_args()
    regression = args.gate in ('regression-edit', 'regression-play')
    if regression and args.filter:
        parser.error('Tam regresyon kapısı filtre kabul etmez; başarısız test için ayrı odaklı kapı hazırlanır.')
    root = Path(__file__).resolve().parents[1]
    unity = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
    if not unity.is_file():
        parser.error('Unity executable bulunamadı: ' + str(unity))
    if (root / 'Temp/UnityLockfile').exists():
        parser.error('Bu komuttan önce Unity Editor’ı kapat. Kilit dosyasına dokunulmadı.')
    suite = {'focused-play': 'LastSignal.Tests.S016ModalTests',
             'focused-interaction': 'LastSignal.Tests.S016InteractionTests',
             'focused-settings': 'LastSignal.Tests.S016SettingsTests',
             'focused-settings-play': 'LastSignal.Tests.S016SettingsPlayTests',
             'focused-readability': 'LastSignal.Tests.S016ReadabilityTests',
             'focused-pause': 'LastSignal.Tests.S016PauseTests',
             'focused-save-feedback': 'LastSignal.Tests.S016SaveFeedbackTests',
             'focused-save-feedback-play': 'LastSignal.Tests.S016SaveFeedbackPlayTests',
             'persistence': 'LastSignal.Tests.PersistenceIntegrationTests',
             'regression-edit': None, 'regression-play': None,
             'focused-movement': 'LastSignal.Tests.MovementAcceptanceTests',
             'focused-regression-fixes': 'LastSignal.Tests.LootCompatibilityTests.RebindingSlotDoesNotDoubleInvokeClick;LastSignal.Tests.MovementAcceptanceTests.MouseAndCrouchActionsReachCameraAndCapsule'}[args.gate]
    edit_mode = args.gate in ('focused-settings', 'focused-save-feedback', 'regression-edit')
    platform = 'EditMode' if edit_mode else 'PlayMode'
    assembly = 'LastSignal.EditModeTests' if edit_mode else 'LastSignal.PlayModeTests'
    valid_filter = args.filter in suite.split(';') if args.gate == 'focused-regression-fixes' else not args.filter or args.filter.startswith(suite + '.')
    if args.filter and not valid_filter:
        parser.error('--filter seçilen kapının sınıfındaki tam test adı olmalı.')
    run = root / 'Docs/Implementation/S016/Evidence' / (
        datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ') + '-' + args.gate)
    run.mkdir(parents=True, exist_ok=False)
    xml, log = run / 'tests.xml', run / 'unity.log'
    command = [str(unity), '-batchmode', '-projectPath', str(root), '-runTests',
               '-testPlatform', platform,
               '-assemblyNames', assembly, '-testResults', str(xml), '-logFile', str(log)]
    if suite:
        command.extend(['-testFilter', args.filter or suite])
    (run / 'environment.json').write_text(json.dumps({'platform': sys.platform, 'unity': (root / 'ProjectSettings/ProjectVersion.txt').read_text(), 'executor': 'user-invoked', 'gate': args.gate}, indent=2))
    (run / 'command.json').write_text(json.dumps(command, indent=2))
    (run / 'source-head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'source-status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root))
    paths = [p for folder in ['Assets/LastSignal/Scripts', 'Assets/LastSignal/Audio', 'Assets/LastSignal/Prefabs',
                             'Assets/LastSignal/Scenes/Production',
                             'Assets/LastSignal/Scenes/Validation'] for p in (root / folder).rglob('*') if p.is_file()]
    paths += [p for p in (root / 'Assets/Noto_Sans/static/NotoSans-Regular.ttf',
                          root / 'Assets/Noto_Sans/static/NotoSans-Regular.ttf.meta') if p.is_file()]
    before = {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    (run / 'source-hashes.json').write_text(json.dumps(before, indent=2))
    print('RUN=' + str(run), flush=True)
    if regression:
        # Reuse only the historical artifact preservation helpers, never S015's multi-stage main.
        spec = importlib.util.spec_from_file_location('s015_preservation', root / 'Tools/s015-regression.py')
        preservation = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(preservation)
        backup = Path(tempfile.mkdtemp(prefix='s016-regression-legacy-'))
        restored = False
        try:
            historical = preservation.save_legacy(backup)
            try:
                code = subprocess.call(command, cwd=root)
            finally:
                preservation.restore_legacy(backup, historical, run)
                restored = True
        finally:
            if restored:
                shutil.rmtree(backup)
            else:
                print('HISTORICAL_BACKUP=' + str(backup), flush=True)
    else:
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
    expected_by_gate = {'focused-play': 9, 'focused-interaction': 3, 'focused-settings': 3,
                        'focused-settings-play': 1, 'focused-readability': 1, 'focused-pause': 1,
                        'focused-save-feedback': 3, 'focused-save-feedback-play': 2, 'persistence': 9,
                        'regression-edit': 530, 'regression-play': 297, 'focused-regression-fixes': 2,
                        'focused-movement': 17}
    expected = 1 if args.filter else expected_by_gate[args.gate]
    count_ok = total >= expected if regression else total == expected
    passed = code == 0 and not changed and result.get('result') == 'Passed' and count_ok and int(result.get('skipped', '0')) == 0
    if changed:
        print('BLOCKED: Koşu sırasında kaynak değişti: ' + ', '.join(changed[:10]))
    print('COMPLETE' if passed else 'FAIL — beklenen test sayısı: ' + ('en az ' if regression else '') + str(expected))
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
