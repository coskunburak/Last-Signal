#!/usr/bin/env python3
"""Build a guarded repair-candidate inventory from owner audit and historical material provenance.

Reads only affected materials, their source metadata, and Git blobs. Never modifies Unity assets.
Candidates require texture decoding/channel validation before an authoring operation may apply them.
"""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/Implementation/HDRPProductionOptimization/Evidence/production_material_coverage.json'
OUT = ROOT / 'Docs/Implementation/HDRPProductionOptimization/MaterialRepairCandidates.json'


def sha(p):
    h = hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda: f.read(1024 * 1024), b''): h.update(b)
    return h.hexdigest()


def prop(text, name):
    m = re.search(r'^    - ' + re.escape(name) + r': ([^\n]+)', text, re.M)
    if not m: raise ValueError('Missing scalar: ' + name)
    return float(m[1])


def texture(text, name):
    m = re.search(r'^    - ' + re.escape(name) + r':\n(.*?)(?=^    - |^    m_|\Z)', text, re.M | re.S)
    if not m: raise ValueError('Missing texture property: ' + name)
    guid = re.search(r'm_Texture: .*guid: ([a-f0-9]{32})', m[1])
    return guid[1] if guid else None


def main():
    report = json.loads(REPORT.read_text())
    if report['errors'] or report['status'] != 'AUDIT_COMPLETE_REVIEW_REQUIRED':
        raise ValueError('A completed owner audit without collection errors is required')
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    rows = []
    guards = {}
    for m in report['materials']:
        if m['classification'] != 'ACTIVE_NEEDS_REPAIR': continue
        path = m['path']
        row = {'path': path, 'guid': m['guid'], 'localId': m['localId'], 'issues': m['issues'],
               'uses': m['uses'], 'status': 'REQUIRES_REPAIR_DESIGN'}
        for rel in (path, path + '.meta'):
            guards[rel] = sha(ROOT / rel)
        if any(i.startswith('Mask Map has sRGB importer: ') for i in m['issues']):
            try:
                if not path.startswith('Assets/LastSignal/') or not path.endswith('.mat'):
                    raise ValueError('Not a project-owned standalone material')
                old = subprocess.check_output(['git', 'show', head + ':' + path], cwd=ROOT)
                text = old.decode()
                keywords = re.search(r'm_ValidKeywords:(.*?)(?:m_InvalidKeywords|m_LightmapFlags)', text, re.S)[1]
                if 'guid: 933532a4fcc9baf4fa0491de14d08ed7' not in text:
                    raise ValueError('Original is not the inspected URP Lit shader')
                if prop(text, '_WorkflowMode') != 1 or '_SPECULAR_SETUP' in keywords:
                    raise ValueError('Source is not metallic workflow')
                if '_METALLICSPECGLOSSMAP' not in keywords or '_OCCLUSIONMAP' in keywords or '_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A' in keywords:
                    raise ValueError('Unexpected channel keyword contract')
                if prop(text, '_SmoothnessTextureChannel') != 0:
                    raise ValueError('Unexpected smoothness source')
                source = next(i.split(': ', 1)[1] for i in m['issues'] if i.startswith('Mask Map has sRGB importer: '))
                meta = (ROOT / (source + '.meta')).read_text()
                source_guid = re.search(r'^guid: ([a-f0-9]{32})', meta, re.M)[1]
                if texture(text, '_MetallicGlossMap') != source_guid:
                    raise ValueError('Current mask input differs from historical metallic input')
                for rel in (source, source + '.meta'):
                    guards[rel] = sha(ROOT / rel)
                row.update(status='CHANNEL_PROVENANCE_RESOLVED_NOT_APPLIED',
                           sourceEvidence='git:' + head + ':' + path,
                           originalSha256=hashlib.sha256(old).hexdigest(),
                           sourceTexture=source, sourceTextureGuid=source_guid,
                           sourceExtension=Path(source).suffix.lower(),
                           sourceMetallicScalar=prop(text, '_Metallic'),
                           sourceSmoothness=prop(text, '_Smoothness'),
                           sourceOcclusionStrength=prop(text, '_OcclusionStrength'),
                           channels={'R': 'metallic source R; mapped URP workflow replaces scalar',
                                     'G': '1 (original occlusion keyword disabled)',
                                     'B': '1 (detail behavior must be checked separately)',
                                     'A': 'metallic source A; multiply via original smoothness remap'},
                           requiredChecks=['Decode source format without editing vendor importer',
                                           'Explicitly distinguish raw data intent from prior sRGB sampling; compare source appearance',
                                           'Verify resolution, alpha, UV transform and detail-map behavior',
                                           'Create shared project-owned linear packed map, preserve material GUID',
                                           'Sample-check channels and preserve unrelated material properties',
                                           'Owner visual acceptance before URP removal'])
            except (ValueError, subprocess.CalledProcessError, AttributeError, StopIteration) as e:
                row['blocker'] = str(e)
        else:
            row['requiredChecks'] = ['Embedded vendor material: create project-owned derivative and NormalMap copy',
                                     'Override only audited project prefab renderer slot; preserve model importer',
                                     'Trace the authoring source so regeneration retains the replacement',
                                     'Verify orientation, alpha, surface properties and owner appearance']
        rows.append(row)
    result = {'schemaVersion': 1, 'status': 'DESIGN_CANDIDATES_NOT_EXECUTABLE',
              'auditSha256': sha(REPORT), 'auditTimestampUtc': report['timestampUtc'],
              'sourceHead': head, 'candidates': rows,
              'guards': [{'path': p, 'sha256': h} for p, h in sorted(guards.items())],
              'note': 'No asset writes. This inventory is not an approved material conversion or render acceptance.'}
    OUT.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'candidates': len(rows), 'provenanceResolved': sum(r['status'] == 'CHANNEL_PROVENANCE_RESOLVED_NOT_APPLIED' for r in rows),
                      'guardedFiles': len(guards), 'output': str(OUT)}, indent=2))


if __name__ == '__main__': main()
