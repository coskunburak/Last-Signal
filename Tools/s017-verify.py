#!/usr/bin/env python3
"""Kullanıcı tarafından başlatılan tek S017 kapısı; otomatik test/build zinciri yok."""
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET


GATES = {
    'focused-survival-edit': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.S018SurvivalTests', 13),
    'focused-survival-play': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S018SurvivalPlayTests', 8),
    'focused-art': ('EditMode', 'LastSignal.Art.EditorTests',
                    'LastSignal.Art.Tests.S013PrefabValidationTests;LastSignal.Art.Tests.S013ProductionAssetTests', 10),
    'focused-input': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.S017InputSettingsTests', 6),
    'focused-policy': ('EditMode', 'LastSignal.EditModeTests', 'LastSignal.Tests.S017PolicyTests', 5),
    'focused-flow': ('PlayMode', 'LastSignal.PlayModeTests', 'LastSignal.Tests.S017InputFlowTests', 9),
    'focused-regression': ('PlayMode', 'LastSignal.PlayModeTests', ';'.join(
        'LastSignal.Tests.' + name for name in (
            'CombatAcceptanceTests', 'LootPopulationTests', 'MovementAcceptanceTests',
            'R01CombatPlayTests', 'RuntimeSmokeTests', 'S014WeaponPresentationTests',
            'S016InteractionTests', 'S016PauseTests')), 47),
    'focused-combat-optics': ('PlayMode', 'LastSignal.PlayModeTests', ';'.join(
        'LastSignal.Tests.' + name for name in (
            'CombatAcceptanceTests', 'R01CombatPlayTests', 'ScopeOpticPlayTests')), 11),
    'regression-edit': ('EditMode', 'LastSignal.EditModeTests', None, 541),
    'regression-play': ('PlayMode', 'LastSignal.PlayModeTests', None, 306),
}


def source_hashes(root):
    folders = ('Assets/LastSignal', 'Packages', 'ProjectSettings')
    paths = [p for folder in folders for p in (root / folder).rglob('*') if p.is_file()]
    paths.append(Path(__file__).resolve())
    return {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(paths)}


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=tuple(GATES) + ('windows-build',))
    parser.add_argument('--unity', type=Path, default=Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'))
    parser.add_argument('--filter', help='Odaklı kapıdaki başarısız testin tam adı')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    suite = GATES[args.gate][2] if args.gate in GATES else None
    if args.filter and (not suite or not any(args.filter.startswith(name + '.') for name in suite.split(';')) or ';' in args.filter):
        parser.error('--filter yalnız seçilen odaklı sınıftaki tek tam test adını kabul eder.')
    if not args.unity.is_file():
        parser.error('Unity executable bulunamadı: ' + str(args.unity))
    if (root / 'Temp/UnityLockfile').exists():
        parser.error("Bu komutu çalıştırmadan önce Unity Editor'ü kapat. Kilit dosyası değiştirilmedi.")
    run_id = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ') + '-' + args.gate
    run = root / 'Docs/Implementation/S017/Evidence' / run_id
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root).decode().strip()
    status = subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root)
    run.mkdir(parents=True, exist_ok=False)
    (run / 'source-status.z').write_bytes(status)
    (run / 'source-head.txt').write_text(head + '\n')
    before = source_hashes(root)
    write_json(run / 'source-hashes.json', before)
    shutil.copy2(root / 'ProjectSettings/ProjectSettings.asset', run / 'ProjectSettings.before.txt')
    identity = {'run': run_id, 'timestampUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                'head': head, 'dirty': bool(status), 'host': sys.platform,
                'unity': (root / 'ProjectSettings/ProjectVersion.txt').read_text(),
                'packages': json.loads((root / 'Packages/manifest.json').read_text())['dependencies'],
                'sourceDigest': hashlib.sha256(json.dumps(before, sort_keys=True).encode()).hexdigest(),
                'executor': 'user-invoked', 'gate': args.gate, 'physicalDeviceAcceptance': 'NOT_RUN'}
    log = run / 'unity.log'
    command = [str(args.unity), '-batchmode', '-projectPath', str(root), '-logFile', str(log)]
    env = os.environ.copy()
    output = None
    if args.gate in GATES:
        platform, assembly, suite, expected = GATES[args.gate]
        command += ['-runTests', '-testPlatform', platform, '-assemblyNames', assembly, '-testResults', str(run / 'tests.xml')]
        if suite:
            command += ['-testFilter', args.filter or suite]
    else:
        output = root / 'Builds/S017' / run_id
        if output.exists():
            parser.error('Build yolu zaten var; üzerine yazılmadı.')
        env['LASTSIGNAL_S017_BUILD_OUTPUT'] = str(output)
        command += ['-buildTarget', 'Win64', '-executeMethod',
                    'LastSignal.Art.Editor.S013ProductionAuthoring.BuildS017Windows', '-quit']
        identity.update({'platform': 'StandaloneWindows64', 'buildType': 'Development',
                         'scene': 'Assets/LastSignal/Scenes/Production/S013Cabin.unity',
                         'output': str(output), 'windowsRuntimeAcceptance': 'NOT_RUN'})
    write_json(run / 'environment.json', identity)
    write_json(run / 'command.json', command)
    print('RUN=' + str(run), flush=True)
    try:
        if args.gate.startswith('regression-') or args.gate in ('focused-regression', 'focused-combat-optics', 'focused-survival-play'):
            # Reuse only the established historical-evidence preservation boundary.
            import importlib.util
            import tempfile
            spec = importlib.util.spec_from_file_location('s015_preservation', root / 'Tools/s015-regression.py')
            preservation = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(preservation)
            backup = Path(tempfile.mkdtemp(prefix='s017-regression-legacy-'))
            historical = preservation.save_legacy(backup)
            try:
                code = subprocess.call(command, cwd=root, env=env)
            finally:
                preservation.restore_legacy(backup, historical, run)
            shutil.rmtree(backup)
        else:
            code = subprocess.call(command, cwd=root, env=env)
    except OSError as error:
        write_json(run / 'process-result.json', {'exitCode': None, 'error': str(error), 'status': 'BLOCKED'})
        print('BLOCKED: ' + str(error))
        return 2
    write_json(run / 'process-result.json', {'exitCode': code})
    shutil.copy2(root / 'ProjectSettings/ProjectSettings.asset', run / 'ProjectSettings.after.txt')
    after = source_hashes(root)
    changed = sorted(p for p in before.keys() | after.keys() if before.get(p) != after.get(p))
    write_json(run / 'source-preservation.json', {'changed': changed})
    if changed:
        print('FAIL: Koşu sırasında kaynak değişti: ' + ', '.join(changed[:10]))
    if output is not None:
        built = code == 0 and (output / 'LastSignal.exe').is_file() and (output / 'build.txt').is_file()
        identity['sourceChangedDuringBuild'] = changed
        identity['buildProcessExitCode'] = code
        if output.exists():
            write_json(output / 's017-build-identity.json', identity)
            shutil.copy2(run / 'source-hashes.json', output / 's017-source-hashes.json')
            shutil.copy2(run / 'source-status.z', output / 's017-source-status.z')
        passed = built and not changed
        write_json(run / 'result.json', {'build': 'PASS' if passed else 'FAIL', 'windowsRuntime': 'NOT_RUN',
                                        'physicalGamepad': 'NOT_RUN'})
        print('Build: ' + ('PASS' if passed else 'FAIL') + '; Windows çalışma/cihaz kabulü: NOT_RUN')
        return 0 if passed else 1
    xml = run / 'tests.xml'
    if not xml.is_file():
        print('BLOCKED: NUnit XML yok. Derleme/altyapı ayrımı için unity.log dosyasını paylaş.')
        return 2
    try:
        result = ET.parse(xml).getroot()
    except ET.ParseError as error:
        print('BLOCKED: Geçersiz XML: ' + str(error))
        return 2
    write_json(run / 'result.json', result.attrib)
    cases = list(result.iter('test-case'))
    expected = 1 if args.filter else GATES[args.gate][3]
    count_ok = len(cases) >= expected if suite is None else len(cases) == expected
    passed = code == 0 and not changed and result.get('result') == 'Passed' and count_ok
    passed = passed and all(case.get('result') == 'Passed' and (suite is None or any(
        case.get('fullname', '').startswith(name + '.') for name in suite.split(';'))) for case in cases)
    for case in cases:
        if case.get('result') != 'Passed':
            print(case.get('fullname'), case.get('result'), case.findtext('failure/message', ''))
            print(case.findtext('failure/stack-trace', ''))
    print(json.dumps(result.attrib))
    print('PASS' if passed else 'FAIL — test keşfi ve unity.log dahil sonucu paylaş.')
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
