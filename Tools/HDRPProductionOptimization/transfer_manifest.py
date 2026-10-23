#!/usr/bin/env python3
"""Hash, copy to a new directory, or verify a portable source transfer. Never deletes files."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REQUIRED = ('Assets', 'Packages', 'ProjectSettings', 'Tools', 'Docs')


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(4 * 1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('mode', choices=('create', 'verify', 'copy'))
    p.add_argument('manifest', type=Path)
    p.add_argument('--include-git', action='store_true')
    p.add_argument('--profile', choices=('complete', 'windows-work'), default='complete')
    p.add_argument('--destination', type=Path, help='New or empty copy destination; never overwrites an existing project')
    a = p.parse_args()
    manifest = a.manifest.resolve()
    if a.mode == 'create':
        if a.profile == 'windows-work' and a.include_git:
            p.error('windows-work preserves Git/LFS on the Mac; use complete --include-git for a repository transfer')
        roots = (REQUIRED if a.profile == 'complete' else REQUIRED[:-1]) + (('.git',) if a.include_git else ())
        paths = [f for folder in roots for f in (ROOT / folder).rglob('*') if f.is_file()]
        if a.profile == 'windows-work':
            # History, build captures and rollback/source archives stay on the Mac.
            # All active/ignored Assets and metadata are still copied without filtering.
            paths += list((ROOT / 'Docs').rglob('*.md'))
            evidence = ROOT / 'Docs/Implementation/HDRPProductionOptimization/Evidence'
            paths += list(evidence.glob('*.json'))
            paths = [f for f in paths if not f.is_relative_to(ROOT / 'Docs') or
                     (not any(x in f.parts for x in ('originals', 'retired', 'before'))
                      and f.name not in ('transfer-manifest.json', 'windows-work-manifest.json'))]
        paths += [f for f in ROOT.iterdir() if f.is_file() and f.suffix in ('.py', '.sh', '.cs', '.md', '.txt')]
        paths += [ROOT / '.gitignore', ROOT / '.gitattributes']
        files = {}
        folded = {}
        for f in sorted(set(paths)):
            if f.resolve() == manifest or '__pycache__' in f.parts: continue
            if f.is_symlink(): raise RuntimeError('Review symlink before Windows transfer: ' + str(f))
            rel = f.relative_to(ROOT).as_posix()
            if a.profile == 'windows-work':
                previous = folded.setdefault(rel.casefold(), rel)
                if previous != rel: raise RuntimeError('Windows case collision: ' + previous + ' / ' + rel)
                for part in Path(rel).parts:
                    stem = part.split('.')[0].upper()
                    if (any(c in part for c in '<>:"|?*') or part.endswith((' ', '.')) or
                        stem in ('CON', 'PRN', 'AUX', 'NUL') or
                        stem in tuple('COM' + str(i) for i in range(1, 10)) + tuple('LPT' + str(i) for i in range(1, 10))):
                        raise RuntimeError('Windows-incompatible path: ' + rel)
            s = f.stat()
            files[f.relative_to(ROOT).as_posix()] = {'bytes': s.st_size, 'sha256': sha(f)}
            if f.stat().st_mtime_ns != s.st_mtime_ns: raise RuntimeError('File changed while hashing: ' + str(f))
        result = {'schemaVersion': 1, 'files': files, 'bytes': sum(f['bytes'] for f in files.values()),
                  'includesGit': a.include_git, 'profile': a.profile,
                  'note': ('All Docs evidence and ignored Assets included.' if a.profile == 'complete' else
                           'All active/ignored Assets included. Git/LFS, historical binary evidence, rollback archives, builds and caches remain on the Mac. Historical recovery/authoring operations may require the separate complete archive.') +
                          ' Close Unity and Git writers before creation/copy. Verify before opening Unity on destination.'}
        manifest.parent.mkdir(parents=True, exist_ok=True)
        manifest.write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps({'files': len(files), 'bytes': result['bytes'], 'manifest': str(manifest)}))
        return 0
    data = json.loads(manifest.read_text())
    destination = None
    if a.mode == 'copy':
        if a.destination is None: p.error('copy requires --destination')
        destination = a.destination.resolve()
        if destination == ROOT or destination.is_relative_to(ROOT):
            p.error('copy destination must be outside the source project')
        if destination.exists() and (not destination.is_dir() or any(destination.iterdir())):
            p.error('copy destination must be a new or empty directory')
    # Validate every path before creating any destination files.
    for rel in data['files']:
        if '\\' in rel or ':' in rel or Path(rel).is_absolute() or '..' in Path(rel).parts:
            raise RuntimeError('Unsafe manifest path: ' + rel)
        if not (ROOT / rel).resolve().is_relative_to(ROOT):
            raise RuntimeError('Manifest path outside project: ' + rel)
    bad = []
    for rel, expected in data['files'].items():
        f = (ROOT / rel).resolve()
        if not f.is_relative_to(ROOT): raise RuntimeError('Manifest path outside project: ' + rel)
        if not f.is_file() or f.stat().st_size != expected['bytes'] or sha(f) != expected['sha256']:
            bad.append(rel)
            continue
        if destination is not None:
            target = destination / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            with f.open('rb') as source, target.open('xb') as copied:
                shutil.copyfileobj(source, copied, 4 * 1024 * 1024)
            if target.stat().st_size != expected['bytes'] or sha(target) != expected['sha256']:
                bad.append(rel)
    if destination is not None and not bad:
        with (destination / 'LastSignal-transfer.json').open('x') as copied_manifest:
            copied_manifest.write(manifest.read_text())
    print(json.dumps({'status': ('COPIED_VERIFIED' if destination else 'VERIFIED') if not bad else 'MISMATCH', 'checked': len(data['files']), 'mismatches': bad}, indent=2))
    return 1 if bad else 0


if __name__ == '__main__':
    raise SystemExit(main())
