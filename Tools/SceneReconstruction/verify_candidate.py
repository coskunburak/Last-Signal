#!/usr/bin/env python3
"""Read-only SCN-S001 receipt, provenance and original scene invariant checks.
Does not launch Unity, execute gameplay/tests or grant visual/performance acceptance.
"""
import hashlib
import json
from pathlib import Path
import re
import sys
ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/Implementation/Scenes/SCN-S001'
def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''): h.update(chunk)
    return h.hexdigest()
def blocks(text):
    return {m[2]: (m[1], m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}
def field(text, name):
    m = re.search(r'^  ' + re.escape(name) + r':.*$', text, re.M)
    return m[0] if m else None
def main():
    ownership = json.loads((DOC/'SceneOwnership.json').read_text())
    receipt = json.loads((DOC/'CandidateReceipt.json').read_text())
    preserved = json.loads((DOC/'PreservationManifest.json').read_text())['sha256']
    prototype = json.loads((DOC/'PrototypeSourcePreservation.json').read_text())['sha256']
    code = json.loads((DOC/'LightingCodeChanges.json').read_text())
    failures = []
    def verify(label, entries):
        for name, expected in entries.items():
            p = ROOT/name
            if not p.is_file() or digest(p) != expected: failures.append(label+': '+name)
    # Explicit, reviewable code amendments: baseline is never silently rewritten.
    for name, entry in code.items():
        if name in preserved and preserved[name] != entry['beforeSha256']:
            failures.append('Code baseline mismatch: '+name)
        verify('code checkpoint', {entry['backup']: entry['beforeSha256']})
    verify('preserved', {k:v for k,v in preserved.items() if k not in code})
    verify('prototype', prototype)
    verify('candidate', receipt['sha256'])
    assets = ownership['isolated'] + ownership['prototypeAssets']
    inverse = {a['guid']:a['sourceGuid'] for a in assets}
    source = blocks((ROOT/ownership['source']).read_text())
    text = (ROOT/ownership['scene']).read_text()
    normalized = re.sub(r'guid: ([0-9a-f]{32})',lambda m:'guid: '+inverse.get(m[1],m[1]),text)
    normalized = normalized.replace('  saveSlot: scn_s001_main_hdrp_review\n','  saveSlot: harvest_20261018150846-032db4e9\n')
    candidate = blocks(normalized)
    missing = set(source)-set(candidate)
    if missing: failures.append('Original scene object IDs missing: '+str(sorted(missing)))
    transform_count = gameplay_count = 0
    runtime = {}
    for meta in (ROOT/'Assets/LastSignal/Scripts/Runtime').rglob('*.cs.meta'):
        m = re.search(r'^guid: (\w+)',meta.read_text(),re.M)
        if m: runtime[m[1]] = meta.stem
    for key,(kind,before) in source.items():
        if key not in candidate: continue
        after = candidate[key][1]
        if kind in ('4','224'):
            transform_count += 1
            for prop in ('m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father','m_GameObject'):
                if field(before,prop) != field(after,prop): failures.append('Original transform changed: '+key+'/'+prop)
        if kind == '114':
            script = re.search(r'm_Script:.*guid: ([0-9a-f]{32})',before)
            if script and script[1] in runtime:
                name = runtime[script[1]]
                if name == 'ResourceNodeView.cs':
                    strip_visual = lambda value: re.sub(r'^  standingVisual:.*\n', '', value, flags=re.M)
                    if strip_visual(before) != strip_visual(after): failures.append('Resource identity/physics wiring changed: '+key)
                    continue
                if name == 'ForestAuthoringState.cs':
                    strip_roots = lambda value: re.sub(r'^  treeShadowRoots:.*\n(?:  - .*\n)*', '', value, flags=re.M)
                    if strip_roots(before) != strip_roots(after): failures.append('Forest authoring changed beyond shadow roots: '+key)
                    continue
                gameplay_count += 1
                if before != after: failures.append('Original gameplay component changed: '+key+'/'+name)
    guids = [ownership['sceneGuid']] + [a['guid'] for a in assets]
    if len(guids) != len(set(guids)): failures.append('Duplicate candidate GUID')
    for a in assets:
        meta = ROOT/(a['path']+'.meta')
        m = re.search(r'^guid: (\w+)$',meta.read_text(),re.M) if meta.is_file() else None
        if not m or m[1] != a['guid'] or a['guid'] == a['sourceGuid']: failures.append('Invalid isolated GUID: '+a['path'])
    if ownership['sceneGuid'] in (ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text(): failures.append('Premature build promotion')
    report = dict(status='FAIL' if failures else 'VERIFIED', stage=receipt['stage'],
        preservedFiles=len(preserved)-len(set(preserved)&set(code)), prototypeFiles=len(prototype),
        explicitCodeAmendments=len(code), candidateFiles=len(receipt['sha256']), isolatedAssets=len(assets),
        originalObjectsMissing=len(missing), originalTransformsChecked=transform_count, originalGameplayComponentsChecked=gameplay_count,
        failures=failures, knownInheritedMissingGuid=ownership['knownMissingGuid'],
        functionalPerformance='NOT_RUN', visual='EDITOR_PREVIEW_ONLY', ownerAcceptance='OWNER_ACCEPTANCE_PENDING')
    print(json.dumps(report,indent=2)); return bool(failures)
if __name__ == '__main__': sys.exit(main())
