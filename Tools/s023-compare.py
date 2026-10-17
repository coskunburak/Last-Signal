#!/usr/bin/env python3
"""Compare two USER-produced steady-state S023 captures. Does not run Unity."""
import argparse
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--region', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error('Use a new immutable evidence filename.')
    left = json.loads(args.baseline.read_text())
    right = json.loads(args.region.read_text())
    keys = ('sourceRevision', 'sourceDirty', 'unity', 'editor', 'platform', 'cpu', 'gpu', 'quality', 'seed', 'width', 'height', 'raining', 'profilerOverhead')
    mismatch = [key for key in keys if key not in left or key not in right or left[key] != right[key]]
    for label, value in (('baseline', left), ('region', right)):
        if value.get('status') != 'CAPTURED_NOT_ACCEPTED' or value.get('samples', 0) < 300 or value.get('pausedSamples', 0) or value.get('errors', 0):
            mismatch.append(label + ': invalid/short/paused capture')
        if not value.get('sourceRevision') or value['sourceRevision'] == 'UNKNOWN':
            mismatch.append(label + ': unknown source revision')
    if abs(left['worldSeconds'] % 86400 - right['worldSeconds'] % 86400) > 120:
        mismatch.append('time of day differs by more than 120 seconds')
    result = {'status': 'NOT_COMPARABLE' if mismatch else 'COMPARED_NOT_ACCEPTED', 'mismatch': mismatch,
              'baseline': str(args.baseline.resolve()), 'region': str(args.region.resolve()),
              'hypothesis': '1080p p95 <=16.67ms, p99 <=25ms; device acceptance pending',
              'overhead': 'Observer/profiler cost requires a matched uninstrumented capture',
              'buildIdentity': 'Attach runner head/status/source hashes; capture revision is the authoring snapshot.'}
    if not mismatch:
        result['frameMs'] = {key: {'baseline': left['frame'][key], 'region': right['frame'][key],
                                   'delta': right['frame'][key] - left['frame'][key]}
                             for key in ('median', 'p95', 'p99')}
        result['metrics'] = []
        a = {m['name']: m for m in left['counters']}
        b = {m['name']: m for m in right['counters']}
        for name in sorted(a.keys() | b.keys()):
            x, y = a.get(name, {}), b.get(name, {})
            if x.get('status') != 'MEASURED' or y.get('status') != 'MEASURED' or x.get('unit') != y.get('unit'):
                result['metrics'].append({'name': name, 'status': 'UNAVAILABLE'})
            else:
                result['metrics'].append({'name': name, 'unit': x['unit'], 'medianDelta': y['median'] - x['median'], 'p95Delta': y['p95'] - x['p95']})
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('x') as handle:
        json.dump(result, handle, indent=2)
        handle.write('\n')
    print(result['status'] + ': ' + str(args.output))
    return 1 if mismatch else 0


if __name__ == '__main__':
    raise SystemExit(main())
