#!/usr/bin/env python3
"""User-run D186/D189 authoring and technical checks; no build or gameplay test."""
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
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True, type=Path)
    p.add_argument('--unity', required=True, type=Path)
    a = p.parse_args(); root = a.project.resolve()
    if root != Path(__file__).resolve().parents[1] or not a.unity.is_file():
        p.error('Use this repository and an existing Unity executable.')
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location('s019_verification', root / 'Tools/s019-verify.py')
    helpers = importlib.util.module_from_spec(spec); spec.loader.exec_module(helpers)
    helpers.require_editor_closed(root, p)
    folder = root / 'Docs/Implementation/S019/Evidence'; folder.mkdir(parents=True, exist_ok=True)
    run = Path(tempfile.mkdtemp(prefix='Manual-' + datetime.datetime.now().strftime('%Y%m%d-%H%M%S-') + 'author-', dir=folder))
    before = helpers.source_hashes(root); helpers.write(run / 'source-before.json', before)
    settings = root / 'ProjectSettings/ProjectSettings.asset'
    (run / 'ProjectSettings.before.asset').write_bytes(settings.read_bytes())
    (run / 'head.txt').write_bytes(subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root))
    (run / 'status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root))
    helpers.write(run / 'environment.json', {'unity': (root / 'ProjectSettings/ProjectVersion.txt').read_text(), 'executor': 'user', 'gate': 'production-authoring'})
    command = [str(a.unity), '-batchmode', '-projectPath', str(root), '-logFile', str(run / 'unity.log'),
               '-executeMethod', 'LastSignal.Editor.S019.S019ProductionAuthoring.Run', '-quit']
    helpers.write(run / 'command.json', command)
    env = os.environ.copy(); env['LASTSIGNAL_S019_AUTHOR_RESULT'] = str(run / 'authoring.json')
    print('EVIDENCE=' + str(run), flush=True); print('UNITY_COMMAND=' + json.dumps(command), flush=True)
    try: code = subprocess.call(command, cwd=root, env=env)
    except OSError as error:
        helpers.write(run / 'process-result.json', {'exitCode': None, 'error': str(error)}); return 2
    helpers.write(run / 'process-result.json', {'exitCode': code})
    after = helpers.source_hashes(root); helpers.write(run / 'source-after.json', after)
    (run / 'ProjectSettings.after.asset').write_bytes(settings.read_bytes())
    changed = sorted(path for path in before.keys() | after.keys() if before.get(path) != after.get(path))
    allowed = {
        'Assets/LastSignal/Scenes/S019/S019Pilot.unity',
        'Assets/LastSignal/Data/S019/S019PilotNavigation.asset',
        'Assets/LastSignal/Settings/S019Import/EnvironmentColor.preset',
        'Assets/LastSignal/Settings/S019Import/StaticMesh.preset',
        'Assets/LastSignal/Settings/S019Import/Audio.preset',
        'Assets/LastSignal/Art/S019Import/EnvironmentColor/S019Exemplar.png',
        'Assets/LastSignal/Art/S019Import/StaticMesh/S019Exemplar.obj',
        'Assets/LastSignal/Art/S019Import/Audio/S019Exemplar.wav',
    }
    allowed |= {x + '.meta' for x in tuple(allowed)}
    unexpected = [x for x in changed if x in before or x not in allowed]
    helpers.write(run / 'source-preservation.json', {'changed': changed, 'unexpected': unexpected, 'allowedNewArtifacts': sorted(allowed)})
    try: report = json.loads((run / 'authoring.json').read_text())
    except (OSError, ValueError): report = {}
    passed = code == 0 and not unexpected and report.get('technicalAuthoring') == 'PASS' and report.get('nativePresets') == 3 and report.get('reimports') == 6 and report.get('validNavigationRoutes') == 5
    helpers.write(run / 'result.json', {'technicalAuthoring': 'PASS' if passed else 'FAIL', 'manualAcceptance': 'NOT_RUN', 'runtimeSaveAcceptance': 'NOT_RUN', 'unexpectedSourceChanges': unexpected, 'authoring': report})
    print('TECHNICAL AUTHORING PASS; manual/runtime acceptance NOT_RUN' if passed else 'FAIL: inspect authoring.json, unity.log and source-preservation.json')
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
