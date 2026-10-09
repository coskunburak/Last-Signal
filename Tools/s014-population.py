#!/usr/bin/env python3
"""Mevcut doğrulanmış macOS Development build'inde S014 1/10/20 yük ölçümü."""
import argparse
import datetime
import json
from pathlib import Path
import plistlib
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('build', type=Path)
    parser.add_argument('--low', action='store_true', help='Mevcut S013 Low render adayı; gameplay aynı kalır.')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    build = args.build.resolve()
    report = build / 'build.txt'
    identity = build / 's014-build-identity.json'
    if not report.is_file() or 'Result=Succeeded' not in report.read_text():
        parser.error('Başarılı build.txt gereklidir.')
    if not identity.is_file():
        parser.error('Bu build için s014-build-identity.json kaynak kaydı gereklidir.')
    app = build / 'LastSignal.app'
    with (app / 'Contents/Info.plist').open('rb') as stream:
        executable = app / 'Contents/MacOS' / plistlib.load(stream)['CFBundleExecutable']
    if not executable.is_file():
        parser.error('Build executable bulunamadı.')
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ')
    output = root / 'Docs/Implementation/S014/Evidence' / (stamp + '-population-' + ('low' if args.low else 'pc'))
    output.mkdir(parents=True, exist_ok=False)
    command = ['open', '-n', '-W', str(app), '--args', '-screen-width', '1920', '-screen-height', '1080',
               '-screen-fullscreen', '0', '-s014Population', str(output), '-logFile', str(output / 'Player.log')]
    if args.low:
        command.append('-s014Low')
    (output / 'command.json').write_text(json.dumps(command, indent=2))
    (output / 'build-identity.json').write_bytes(identity.read_bytes())
    (output / 'build.txt').write_bytes(report.read_bytes())
    (output / 'source-status.z').write_bytes(subprocess.check_output(['git', 'status', '--porcelain=v1', '-z'], cwd=root))
    print('RUN=' + str(output), flush=True)
    code = subprocess.call(command, cwd=root)
    result = output / 'result.txt'
    message = result.read_text() if result.is_file() else 'BLOCKED — süreç ölçüm sonucu üretmeden sonlandı.'
    (output / 'process-result.json').write_text(json.dumps({'exitCode': code, 'result': message}, indent=2))
    print(message, flush=True)
    return 0 if code == 0 and message.startswith('MEASURED') else 2


if __name__ == '__main__':
    sys.exit(main())
