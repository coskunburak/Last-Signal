#!/usr/bin/env python3
"""User-only native S023 authoring/validation. Never runs tests or builds."""
import argparse
import datetime
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path)
    parser.add_argument('--unity', required=True, type=Path)
    args = parser.parse_args()
    root = args.project.resolve()
    if root != Path(__file__).resolve().parents[1] or not args.unity.is_file() or not os.access(args.unity, os.X_OK):
        parser.error('Use this repository and an executable Unity path.')
    version = (root / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(':', 1)[1].strip()
    if version not in args.unity.parts:
        parser.error('Unity Hub version must match ' + version)
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location('s023_helpers', root / 'Tools/s023-verify.py')
    helpers = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helpers)
    helpers.require_editor_closed(root, parser)
    evidence = root / 'Docs/Implementation/S023/Evidence'
    evidence.mkdir(parents=True, exist_ok=True)
    stamp = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=3))).strftime('%Y%m%d-%H%M%S-')
    run = Path(tempfile.mkdtemp(prefix='Manual-' + stamp + 'author-', dir=evidence))
    before = helpers.source_hashes(root)
    helpers.write(run / 'source-before.json', before)
    (run / 'head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root))
    settings = root / 'ProjectSettings/ProjectSettings.asset'
    settings_before = settings.read_bytes()
    (run / 'ProjectSettings.before.asset').write_bytes(settings_before)
    helpers.write(run / 'environment.json', {'executor': 'user', 'unity': version, 'packages': json.loads((root / 'Packages/manifest.json').read_text())})
    command = [str(args.unity), '-batchmode', '-projectPath', str(root), '-logFile', str(run / 'unity.log'),
               '-executeMethod', 'LastSignal.Editor.S023.S023RegionAuthoring.Run', '-quit']
    helpers.write(run / 'command.json', command)
    env = os.environ.copy()
    env['LASTSIGNAL_S023_AUTHOR_RESULT'] = str(run / 'authoring.json')
    env['LASTSIGNAL_S023_SOURCE_HEAD'] = (run / 'head.txt').read_text().strip()
    env['LASTSIGNAL_S023_SOURCE_DIRTY'] = 'true' if (run / 'status.z').stat().st_size else 'false'
    print('EVIDENCE=' + str(run), flush=True)
    print('UNITY_COMMAND=' + json.dumps(command), flush=True)
    try:
        code = subprocess.call(command, cwd=root, env=env)
    except OSError as error:
        helpers.write(run / 'process-result.json', {'exitCode': None, 'error': str(error)})
        return 2
    helpers.write(run / 'process-result.json', {'exitCode': code})
    after = helpers.source_hashes(root)
    helpers.write(run / 'source-after.json', after)
    settings_after = settings.read_bytes()
    (run / 'ProjectSettings.after.asset').write_bytes(settings_after)
    paths = ['Assets/LastSignal/Scenes/S023/S023RegionPilot.unity']
    paths += ['Assets/LastSignal/Prefabs/S023/' + name + '.prefab' for name in ('MaintenanceDepot', 'LoadingYard')]
    paths += ['Assets/LastSignal/Data/S023/' + name + '.asset' for name in ('Cell3Navigation', 'Cell4Navigation', 'Maintenance', 'Checkpoint', 'Shipment')]
    allowed = set(paths + [p + '.meta' for p in paths])
    changed = sorted(p for p in before.keys() | after.keys() if before.get(p) != after.get(p))
    expected = ['ProjectSettings/ProjectSettings.asset'] if 'ProjectSettings/ProjectSettings.asset' in changed and helpers.sentis_analytics_only(settings_before, settings_after) else []
    unexpected = [p for p in changed if p not in expected and (p in before or p not in allowed)]
    helpers.write(run / 'source-preservation.json', {'changed': changed, 'expectedChanged': expected, 'unexpected': unexpected})
    try:
        report = json.loads((run / 'authoring.json').read_text())
    except (OSError, ValueError):
        report = {}
    passed = code == 0 and not unexpected and report.get('authoring') == 'PASS' and len(report.get('routes', [])) == 11 and all((root / p).is_file() for p in allowed)
    helpers.write(run / 'result.json', {'authoring': 'PASS' if passed else 'FAIL', 'gameplay': 'NOT_RUN', 'visual': 'NOT_RUN', 'performance': 'NOT_MEASURED', 'report': report, 'unexpected': unexpected})
    print('AUTHORING PASS; gameplay/visual NOT_RUN' if passed else 'FAIL: send authoring.json, unity.log, source-preservation.json and result.json')
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
