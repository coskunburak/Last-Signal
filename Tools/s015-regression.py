#!/usr/bin/env python3
"""Burak'ın başlattığı S015 tam regresyon; tarihsel sabit test kanıtlarını korur."""
import argparse
import datetime
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity')
# Static write targets found in the current EditMode/PlayMode test sources.
LEGACY = (
    'P02-GAP/Evidence/20260922-entry',
    'P02-GAP-S007/Evidence/20260924-entry',
    'P02-GAP-S009/Evidence/20260925-independent',
    'P02-GAP-S009/Evidence/manual-20260926-performance-soak',
    'S004/Evidence/20260919-P4',
    'S007/Evidence/20260920-103550-entry',
    'S008/Evidence/20260920-212200-entry',
    'S010/Evidence/20260928-closure',
    'PreS010-Recovery/Evidence/R02/20260927-foundation',
    'PreS010-Recovery/Evidence/R03/20260927-foundation',
    'PreS010-Recovery/Evidence/R04',
    'PreS010-Recovery/Evidence/R05',
)


def digest(path):
    result = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            result.update(chunk)
    return result.hexdigest()


def snapshot(path):
    return {str(p.relative_to(path)): digest(p) for p in path.rglob('*') if p.is_file()} if path.exists() else {}


def save_legacy(backup):
    state = {}
    for rel in LEGACY:
        src = ROOT / 'Docs/Implementation' / rel
        state[rel] = snapshot(src)
        if src.exists():
            dst = backup / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            # APFS clone: no historical artifact is overwritten or copied into repo evidence.
            subprocess.run(['cp', '-cRP', str(src), str(dst)], check=True)
    return state


def restore_legacy(backup, before, run):
    changes = []
    for rel in LEGACY:
        src = ROOT / 'Docs/Implementation' / rel
        current = snapshot(src)
        old = before[rel]
        for item in sorted(set(old) | set(current)):
            if old.get(item) == current.get(item):
                continue
            changes.append(f'{rel}/{item}')
            target = src / item
            if target.is_file():
                saved = run / 'generated-artifacts' / rel / item
                saved.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(target, saved)
            if item in old:
                original = backup / rel / item
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(original, target)
            elif target.exists():
                target.unlink()
    (run / 'preserved-historical-paths.json').write_text(json.dumps(changes, indent=2))
    return changes


def source_manifest():
    paths = [p for folder in ('Assets/LastSignal/Scripts', 'Assets/LastSignal/Audio',
                              'Assets/LastSignal/Prefabs', 'Assets/LastSignal/Scenes/Production')
             for p in (ROOT / folder).rglob('*') if p.is_file()]
    return {str(p.relative_to(ROOT)): digest(p) for p in paths}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('gate', choices=('full-edit', 'full-play', 'art-editor'))
    parser.add_argument('--filter', action='append', help='Tam test veya desteklenen fixture adı; yalnız dar tekrar koşusu')
    args = parser.parse_args()
    if args.filter and args.gate == 'full-edit':
        parser.error('Filtreli koşu için art-editor veya full-play kapısını seç.')
    if args.filter and any(not name.startswith('LastSignal.') for name in args.filter):
        parser.error('--filter tam LastSignal test adı olmalı.')
    if args.filter and args.gate == 'art-editor' and any(name == 'LastSignal.Tests.MovementAcceptanceTests' for name in args.filter):
        parser.error('MovementAcceptanceTests yalnız full-play kapısında çalışır.')
    if not UNITY.is_file():
        parser.error('Unity executable bulunamadı: ' + str(UNITY))
    if (ROOT / 'Temp/UnityLockfile').exists():
        parser.error('Bu komuttan önce Unity Editor’ı kapat. Kilit dosyasına dokunulmadı.')
    run = ROOT / 'Docs/Implementation/S015/Evidence' / (
        datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ') + '-' + args.gate)
    run.mkdir(parents=True, exist_ok=False)
    (run / 'source-head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT))
    (run / 'source-status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=ROOT))
    before_sources = source_manifest()
    (run / 'source-hashes.json').write_text(json.dumps(before_sources, indent=2))
    print('RUN=' + str(run), flush=True)
    if args.gate == 'full-edit':
        stages = (('editmode', 'EditMode', 'LastSignal.EditModeTests', 524, None),
                  ('art-editor', 'EditMode', 'LastSignal.Art.EditorTests', 10, None))
    elif args.gate == 'art-editor':
        stages = (('art-editor', 'EditMode', 'LastSignal.Art.EditorTests', 10, None),)
    else:
        stages = tuple((f'playmode-{i + 1}' if args.filter else 'playmode', 'PlayMode',
                        'LastSignal.PlayModeTests',
                        17 if test_filter == 'LastSignal.Tests.MovementAcceptanceTests'
                        else 1 if args.filter else 280, test_filter)
                       for i, test_filter in enumerate(args.filter or [None]))
    if args.gate == 'art-editor' and args.filter:
        stages = tuple((f'art-editor-{i + 1}', 'EditMode', 'LastSignal.Art.EditorTests', 1, test_filter)
                       for i, test_filter in enumerate(args.filter))
    completed = []
    backup = Path(tempfile.mkdtemp(prefix='s015-regression-legacy-'))
    preserved = False
    try:
        historical = save_legacy(backup)
        for name, platform, assembly, minimum, test_filter in stages:
            xml, log = run / (name + '.xml'), run / (name + '.log')
            command = [str(UNITY), '-batchmode', '-projectPath', str(ROOT), '-runTests',
                       '-testPlatform', platform, '-assemblyNames', assembly,
                       '-testResults', str(xml), '-logFile', str(log)]
            if test_filter:
                command.extend(('-testFilter', test_filter))
            (run / (name + '-command.json')).write_text(json.dumps(command, indent=2))
            code = subprocess.call(command, cwd=ROOT)
            data = {'stage': name, 'filter': test_filter, 'exitCode': code, 'xml': str(xml), 'log': str(log)}
            if xml.is_file():
                try:
                    result = ET.parse(xml).getroot()
                    data.update(result.attrib)
                    for case in result.iter('test-case'):
                        if case.get('result') not in ('Passed', 'Skipped'):
                            print(case.get('fullname'), case.get('result'),
                                  case.findtext('failure/message', ''),
                                  case.findtext('failure/stack-trace', ''), flush=True)
                except ET.ParseError as error:
                    data['parseError'] = str(error)
            else:
                data['missingXml'] = True
            total = int(data.get('total', '0'))
            count_ok = total == minimum if test_filter else total >= minimum
            data['complete'] = (code == 0 and data.get('result') == 'Passed'
                                and int(data.get('failed', '-1')) == 0
                                and int(data.get('skipped', '-1')) == 0 and count_ok)
            if total < minimum:
                data['countReviewRequired'] = f'Historical baseline plus S015 suggests at least {minimum}; investigate discovery.'
            (run / (name + '-result.json')).write_text(json.dumps(data, indent=2))
            completed.append(data)
            print(json.dumps(data), flush=True)
            if not data['complete']:
                break
    finally:
        # Preserve generated outputs first, then restore bytes present before the run.
        if 'historical' in locals():
            restore_legacy(backup, historical, run)
            preserved = True
        if preserved:
            shutil.rmtree(backup)
        else:
            print('HISTORICAL_BACKUP=' + str(backup), flush=True)
    after_sources = source_manifest()
    changed_sources = [p for p in sorted(set(before_sources) | set(after_sources))
                       if before_sources.get(p) != after_sources.get(p)]
    (run / 'source-preservation.json').write_text(json.dumps({'changed': changed_sources}, indent=2))
    (run / 'summary.json').write_text(json.dumps({'stages': completed, 'changedSources': changed_sources}, indent=2))
    if changed_sources:
        print('SOURCE_CHANGED=' + ', '.join(changed_sources[:10]), flush=True)
    ok = len(completed) == len(stages) and all(stage['complete'] for stage in completed) and not changed_sources
    print('COMPLETE' if ok else 'FAIL_OR_REVIEW_REQUIRED', flush=True)
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
