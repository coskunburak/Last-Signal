#!/usr/bin/env python3
"""Read-only filesystem/dependency inventory. Never starts Unity or removes files."""
import argparse
import collections
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
URP = re.compile(r'com\.unity\.render-pipelines\.universal|UnityEngine\.Rendering\.Universal|Unity\.RenderPipelines\.Universal|UniversalRenderPipeline|UniversalAdditional(?:Camera|Light)Data|UniversalRendererData|UniversalForward|UniversalGBuffer|UniversalFragment(?:PBR|BlinnPhong)|UniversalPipeline|Universal Render Pipeline|Universal RP|URP/|ScriptableRendererFeature|ScriptableRenderPass|USING_URP')
TEXT_EXT = {'.cs', '.shader', '.hlsl', '.shadergraph', '.shadersubgraph', '.asmdef', '.json'}


def git(*args):
    return subprocess.check_output(['git', '--no-optional-locks', *args], cwd=ROOT)


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--duplicates', action='store_true', help='Hash same-size source files >=1 MiB')
    args = parser.parse_args()
    out = args.output.resolve()
    totals = collections.defaultdict(lambda: [0, 0, 0])
    files = []
    collisions = collections.defaultdict(list)
    duplicate_sizes = collections.defaultdict(list)
    errors = []
    for base, dirs, names in os.walk(ROOT, followlinks=False, onerror=lambda e: errors.append(str(e))):
        # Evidence produced by this tool is excluded to keep repeated inventories comparable.
        dirs[:] = [d for d in dirs if (Path(base) / d).resolve() != out]
        for name in names:
            p = Path(base) / name
            rel = p.relative_to(ROOT).as_posix()
            try:
                s = p.lstat()
                apparent = s.st_size
                blocks = getattr(s, 'st_blocks', 0) * 512
                files.append((apparent, blocks, rel))
                parts = Path(rel).parts
                for depth in range(min(len(parts), 4)):
                    key = '/'.join(parts[:depth]) or '.'
                    totals[key][0] += apparent
                    totals[key][1] += blocks
                    totals[key][2] += 1
                if parts[0] in ('Assets', 'Packages', 'ProjectSettings', 'Tools'):
                    collisions[rel.casefold()].append(rel)
                if args.duplicates and parts[0] == 'Assets' and apparent >= 1024**2 and not p.is_symlink():
                    duplicate_sizes[apparent].append(rel)
            except OSError as e:
                errors.append(str(e))
    tracked = set(git('ls-files', '-z').decode().split('\0'))
    ignored = git('ls-files', '--others', '--ignored', '--exclude-standard', '-z', 'Assets').decode().split('\0')
    refs = []
    portable = []
    for _, _, rel in files:
        p = ROOT / rel
        if Path(rel).parts[0] not in ('Assets', 'Packages', 'Tools') or p.suffix not in TEXT_EXT | {'.sh', '.ps1'}:
            continue
        if p.suffix in TEXT_EXT | {'.sh', '.ps1'}:
            for line, content in enumerate(p.read_text(errors='replace').splitlines(), 1):
                if Path(rel).parts[0] != 'Tools' and URP.search(content):
                    kind = 'vendor' if not rel.startswith(('Assets/LastSignal/', 'Packages/')) else 'project'
                    if 'Rollback' in p.name: kind = 'recovery-record-not-runtime'
                    elif '/Tests/' in rel: kind = 'test'
                    elif '/Editor/' in rel: kind += '-editor'
                    elif p.suffix in ('.shader', '.hlsl', '.shadergraph', '.shadersubgraph'): kind += '-shader'
                    refs.append({'path': rel, 'line': line, 'kind': kind, 'text': content.strip()[:500]})
                if '/Users/' in content or '/Applications/' in content or '/usr/bin/pgrep' in content:
                    portable.append({'path': rel, 'line': line, 'text': content.strip()[:300]})
    lock = json.loads((ROOT / 'Packages/packages-lock.json').read_text())['dependencies']
    lfs = {}
    try:
        current = json.loads(git('lfs', 'ls-files', '--json'))['files']
        history = json.loads(git('lfs', 'ls-files', '--all', '--json'))['files']
        current_oids = {x['oid'] for x in current}
        history_oids = {x['oid'] for x in history}
        local = {Path(p).name: size for size, _, p in files if p.startswith('.git/lfs/objects/') and re.fullmatch('[a-f0-9]{64}', Path(p).name)}
        lfs = {'current_paths': len(current), 'current_unique_oids': len(current_oids), 'all_ref_oids': len(history_oids),
               'local_objects': len(local), 'local_bytes': sum(local.values()),
               'local_current_bytes': sum(v for k, v in local.items() if k in current_oids),
               'local_history_only_bytes': sum(v for k, v in local.items() if k in history_oids - current_oids),
               'local_not_listed_by_all_refs_bytes': sum(v for k, v in local.items() if k not in history_oids),
               'missing_current_oids': sorted(current_oids - local.keys()),
               'note': 'Not-listed is NOT disposable: reflogs, unpublished objects and remote availability require review.'}
    except (subprocess.CalledProcessError, KeyError, ValueError) as e:
        errors.append('LFS inventory: ' + str(e))
    duplicates = []
    for size, paths in duplicate_sizes.items():
        if len(paths) < 2: continue
        hashes = collections.defaultdict(list)
        for p in paths: hashes[digest(ROOT / p)].append(p)
        for sha, group in hashes.items():
            if len(group) > 1: duplicates.append({'bytes_each': size, 'sha256': sha, 'paths': group})
    report = {'status': 'STATIC_ONLY' if not errors else 'INCOMPLETE', 'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
              'root': str(ROOT), 'head': git('rev-parse', 'HEAD').decode().strip(),
              'branch': git('branch', '--show-current').decode().strip(),
              'sizes': dict(totals), 'size_columns': ['apparent_bytes', 'allocated_bytes', 'files'],
              'largest_files': [{'path': p, 'apparent_bytes': a, 'allocated_bytes': b, 'tracked': p in tracked} for a, b, p in sorted(files, reverse=True)[:50]],
              'ignored_asset_files': len([p for p in ignored if p]),
              'case_collisions': [v for v in collisions.values() if len(v) > 1],
              'urp_package_owners': [k for k, v in lock.items() if 'com.unity.render-pipelines.universal' in v.get('dependencies', {})],
              'lfs': lfs, 'errors': errors,
              'limitations': ['Allocated blocks may be shared by APFS clones; not exclusive physical usage.', 'Static URP occurrences are not proof of active use.', 'No Unity compilation, package resolution, shader rendering or gameplay executed.']}
    out.mkdir(parents=True, exist_ok=True)
    for name, obj in [('storage.json', report), ('urp-occurrences.json', refs), ('portability.json', portable), ('duplicates.json', duplicates)]:
        (out / name).write_text(json.dumps(obj, indent=2) + '\n')
    (out / 'git-status.z').write_bytes(git('status', '--porcelain=v1', '-z', '--untracked-files=all'))
    (out / 'ignored-assets.txt').write_text('\n'.join(p for p in ignored if p) + '\n')
    print(json.dumps({'status': report['status'], 'total': totals['.'], 'lfs': lfs, 'urp_occurrences': len(refs), 'urp_files': len({r['path'] for r in refs}), 'duplicate_groups': len(duplicates), 'case_collisions': report['case_collisions'], 'errors': errors}, indent=2))
    return 1 if errors else 0


if __name__ == '__main__':
    raise SystemExit(main())
