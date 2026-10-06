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
    parser.add_argument('gate', choices=['focused-edit', 'focused-play', 'focused-melee', 'focused-persistence', 'reload-audit'])
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
    if args.gate == 'reload-audit':
        command = [str(unity), '-batchmode', '-projectPath', str(root),
                   '-executeMethod', 'LastSignal.Editor.S014ReloadTimingAudit.RunBatch',
                   '-s014Evidence', str(run), '-logFile', str(log), '-quit']
    if args.gate == 'focused-edit':
        command += ['-assemblyNames', 'LastSignal.EditModeTests', '-testFilter', 'LastSignal.Tests.WeaponStateTests']
    elif args.gate == 'focused-play':
        command += ['-assemblyNames', 'LastSignal.PlayModeTests', '-testFilter', 'LastSignal.Tests.S014WeaponPresentationTests']
    elif args.gate == 'focused-melee':
        command += ['-assemblyNames', 'LastSignal.PlayModeTests', '-testFilter',
                    'LastSignal.Tests.S014WeaponPresentationTests.Crowbar']
    elif args.gate == 'focused-persistence':
        command += ['-assemblyNames', 'LastSignal.PlayModeTests', '-testFilter',
                    'LastSignal.Tests.PersistenceIntegrationTests.S014ReloadBeforeCommitSurvivesSaveDeathAndLoad;'
                    'LastSignal.Tests.PersistenceIntegrationTests.S014ReloadAfterCommitSurvivesSaveDeathAndLoad']
    # Tests omit -quit because the test runner owns completion; the synchronous audit uses it.
    # No second Editor or automatic retry.
    (run / 'command.json').write_text(json.dumps(command, indent=2))
    (run / 'source-head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'source-status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root))
    files = [root / 'Assets/LastSignal/Scripts/Runtime/Combat/WeaponController.cs',
             root / 'Assets/LastSignal/Scripts/Runtime/Combat/WeaponAnimationPresenter.cs',
             root / 'Assets/LastSignal/Scripts/Tests/PlayMode/S014WeaponPresentationTests.cs']
    if args.gate in ('focused-play', 'focused-melee'):
        files += [root / p for p in [
            'Assets/LastSignal/Scripts/Runtime/Combat/MeleeStanceViewPresenter.cs',
            'Assets/LastSignal/Scripts/Runtime/Combat/MeleeWeaponPresenter.cs',
            'Assets/LastSignal/Scripts/Runtime/Combat/MeleeWeaponController.cs',
            'Assets/LastSignal/Scripts/Runtime/Combat/MeleeAttackState.cs',
            'Assets/LastSignal/Scripts/Runtime/Combat/PlayerCombatController.cs']]
    if args.gate == 'focused-persistence':
        files += [root / p for p in [
            'Assets/LastSignal/Scripts/Tests/PlayMode/PersistenceIntegrationTests.cs',
            'Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs',
            'Assets/LastSignal/Scripts/Runtime/Session/SessionFlow.cs',
            'Assets/LastSignal/Scripts/Runtime/Combat/WeaponRuntimeState.cs',
            'Assets/LastSignal/Scripts/Runtime/Combat/PlayerCombatController.cs']]
    if args.gate == 'reload-audit':
        files += [root / p for p in [
            'Assets/LastSignal/Scripts/Editor/S014ReloadTimingAudit.cs',
            'Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab',
            'Assets/LastSignal/Data/Combat/WeaponDefinition_AssaultRifle.asset',
            'Assets/LastSignal/Animations/VAL_MRPoly.controller',
            'Assets/ThirdParty/Characters/VAL.fbx',
            'Assets/ThirdParty/Characters/VAL.fbx.meta']]
    (run / 'changed-source-hashes.json').write_text(json.dumps({str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}, indent=2))
    print('RUN=' + str(run), flush=True)
    code = subprocess.call(command, cwd=root)
    (run / 'process-result.json').write_text(json.dumps({'exitCode': code}))
    if args.gate == 'reload-audit':
        report = run / 'reload-audit.json'
        if code != 0 or not report.is_file():
            print('BLOCKED: Ölçüm raporu üretilemedi. Log: ' + str(log))
            return 2
        before = json.loads((run / 'changed-source-hashes.json').read_text())
        changed = [p for p, digest in before.items()
                   if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != digest]
        (run / 'source-preservation.json').write_text(json.dumps({'changed': changed}, indent=2))
        if changed:
            print('BLOCKED: Ölçüm sırasında kaynak değişti: ' + ', '.join(changed))
            return 2
        print(report.read_text())
        print('COMPLETE_AUDIT — ölçüm üretildi; görsel kabul PASS değildir.')
        return 0
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
        passed = passed and int(result.get('total', '0')) == 9
    elif args.gate == 'focused-melee':
        passed = passed and int(result.get('total', '0')) == 4
    elif args.gate == 'focused-persistence':
        passed = passed and int(result.get('total', '0')) == 2
    print('COMPLETE' if passed else 'FAIL')
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
