#!/usr/bin/env python3
"""S014 ham frame dosyalarından doğrulanabilir ölçüm özeti; kabulü kendiliğinden vermez."""
import argparse
import csv
import json
from pathlib import Path
import statistics


def summarize(run):
    result = (run / 'result.txt').read_text()
    if not result.startswith('MEASURED'):
        raise ValueError(f'Tamamlanmış ölçüm değil: {run}')
    rows = list(csv.DictReader((run / 'summary.csv').open()))
    if len(rows) != 15:
        raise ValueError('Tam 15 tekrar gerekir.')
    output = {'run': str(run.resolve()), 'result': result.strip(), 'groups': []}
    for count in (1, 10, 20):
        group = [r for r in rows if int(r['count']) == count]
        if sorted(int(r['repeat']) for r in group) != list(range(5)):
            raise ValueError('Her sayı için beş benzersiz tekrar gerekir.')
        frames = []
        metrics = {}
        for row in group:
            if int(row['active_count']) != count or int(row['contact_attempts']) <= 0:
                raise ValueError('Aktör/gerçek temas sözleşmesi başarısız.')
            prefix = f"{count}-{row['repeat']}"
            raw = [float(r['frame_ms']) for r in csv.DictReader((run / (prefix + '-frames.csv')).open())]
            if len(raw) != int(row['frames']):
                raise ValueError('Ham frame sayısı uyuşmuyor.')
            frames.extend(raw)
            for line in (run / (prefix + '-counters.txt')).read_text().splitlines():
                if '; samples=' not in line:
                    continue
                name, value = line.split('=', 1)
                value, samples = value.split('; samples=')
                metrics.setdefault(name, []).append(None if value == 'UNAVAILABLE' else float(value))
        frames.sort()
        output['groups'].append({
            'actors': count, 'repetitions': 5, 'frames': len(frames),
            'mean_of_run_means_ms': statistics.mean(float(r['mean_ms']) for r in group),
            'pooled_mean_ms': statistics.mean(frames), 'pooled_median_ms': statistics.median(frames),
            'pooled_p95_ms': frames[int(len(frames)*.95)], 'pooled_p99_ms': frames[int(len(frames)*.99)],
            'worst_repeat_p95_ms': max(float(r['p95_ms']) for r in group),
            'worst_repeat_p99_ms': max(float(r['p99_ms']) for r in group),
            'over50ms': sum(t > 50 for t in frames),
            'temporary_frame_budget_all_repeats': all(float(r['p95_ms']) <= 16.67 and float(r['p99_ms']) <= 25 for r in group),
            'gc0_total': sum(int(r['gc0']) for r in group),
            'unity_delta_bytes_by_repeat': [int(r['unity_end_bytes'])-int(r['unity_start_bytes']) for r in group],
            'managed_delta_bytes_by_repeat': [int(r['managed_end_bytes'])-int(r['managed_start_bytes']) for r in group],
            'unity_end_bytes_by_repeat': [int(r['unity_end_bytes']) for r in group],
            'contact_attempts_by_repeat': [int(r['contact_attempts']) for r in group],
            'counter_means_by_repeat': metrics})
    output['limitations'] = 'Kontrollü resident fixture; tam POI, fairness, Windows, retail veya soak kabulü değildir. Culling korunur; active_count tümünün ekranda render edildiği anlamına gelmez. CPU/GPU süreleri toplanmaz. GC nesil sayaçları Mono altında aynı collection olayını yansıtabilir. Bütün-frame allocation kritik AI tick allocation ölçümü değildir.'
    return output


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('runs', nargs='+', type=Path)
    args = parser.parse_args()
    print(json.dumps([summarize(run) for run in args.runs], indent=2, ensure_ascii=False))
