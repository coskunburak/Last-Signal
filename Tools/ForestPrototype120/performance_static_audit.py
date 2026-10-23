#!/usr/bin/env python3
"""Read-only, standard-library audit of the saved forest. Never launches Unity.

This intentionally supports this generated scene's flat prefab layout only. It
fails on nested instances/structural overrides rather than inventing totals.
Mesh topology in imported binary models requires the owner-run Editor audit.
"""
import argparse
from collections import Counter, defaultdict
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
SCENE = 'Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity'
HEADER = re.compile(r'^--- !u!(\d+) &(-?\d+)( stripped)?\n', re.M)
REF = r'\{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: \d+\}'
NAMES = {1: 'GameObject', 4: 'Transform', 20: 'Camera', 23: 'MeshRenderer',
         33: 'MeshFilter', 54: 'Rigidbody', 64: 'MeshCollider', 65: 'BoxCollider',
         108: 'Light', 114: 'MonoBehaviour', 135: 'SphereCollider',
         136: 'CapsuleCollider', 137: 'SkinnedMeshRenderer', 143: 'CharacterController',
         154: 'TerrainCollider', 198: 'ParticleSystem', 199: 'ParticleSystemRenderer',
         215: 'ReflectionProbe', 218: 'Terrain', 205: 'LODGroup'}


def blocks(text):
    matches = list(HEADER.finditer(text))
    return [(int(m[1]), m[2], bool(m[3]), text[m.end():matches[i+1].start() if i+1 < len(matches) else len(text)])
            for i, m in enumerate(matches)]


def value(text, key, default=None):
    m = re.search(r'^  ' + re.escape(key) + r': (.*)$', text, re.M)
    return m[1] if m else default


def audit():
    path = ROOT / SCENE
    scene = path.read_text()
    generation = value(scene, 'generationFolder')
    if not generation or not generation.startswith('Assets/LastSignal/Art/ForestPrototype120/Generations/'):
        raise ValueError('Unknown generation; inspect scene ownership before continuing.')
    # Only generated prefab metadata; no ThirdParty crawl or binary model load.
    prefabs = {}
    for meta in (ROOT / generation / 'Prefabs').glob('*.prefab.meta'):
        guid = re.search(r'^guid: (\w+)', meta.read_text(), re.M)[1]
        if guid in prefabs:
            raise ValueError('Duplicate generated prefab GUID: ' + guid)
        prefabs[guid] = meta.with_suffix('')
    native = blocks(scene)
    expanded = [(SCENE, i, t, b) for t, i, stripped, b in native if not stripped and t != 1001]
    instances, duplicates, mesh_refs, material_refs = Counter(), defaultdict(list), Counter(), Counter()
    families, hashes = {}, {SCENE: hashlib.sha256(path.read_bytes()).hexdigest()}
    for t, instance_id, stripped, body in native:
        if t != 1001:
            continue
        guid = re.search(r'm_SourcePrefab: ' + REF, body)[2]
        if guid not in prefabs:
            raise ValueError('Unresolved prefab: ' + guid)
        prefab = prefabs[guid]
        rel = str(prefab.relative_to(ROOT))
        instances[rel] += 1
        if rel not in families:
            ptext = prefab.read_text()
            pb = blocks(ptext)
            if any(t == 1001 or stripped for t, _, stripped, _ in pb):
                raise ValueError('Nested/variant prefab requires native audit: ' + rel)
            families[rel] = pb
            hashes[rel] = hashlib.sha256(prefab.read_bytes()).hexdigest()
        for key in ('m_RemovedGameObjects', 'm_AddedGameObjects', 'm_AddedComponents'):
            if not re.search(r'^    ' + key + r': \[\]$', body, re.M):
                raise ValueError('Structural override requires native audit: ' + instance_id + ' ' + key)
        # Current scene changes transform/name and distant LOD references only.
        props = re.findall(r'propertyPath: (.+)', body)
        if any(not p.startswith(('m_Local', 'm_Name', 'm_Size', 'm_LODs.')) for p in props):
            raise ValueError('New component-state override requires native audit: ' + instance_id)
        removed_section = body.split('m_RemovedComponents:', 1)[1].split('m_RemovedGameObjects:', 1)[0]
        removed = {m[0] for m in re.findall(REF, removed_section)}
        expanded.extend((rel, i, kind, text) for kind, i, _, text in families[rel] if i not in removed)
        positions = tuple(re.findall(r'propertyPath: m_LocalPosition\.[xyz]\n      value: (.+)', body))
        parent = re.search(r'm_TransformParent: \{fileID: (-?\d+)\}', body)[1]
        duplicates[(guid, parent, positions)].append(instance_id)
    counts = Counter(NAMES.get(t, str(t)) for _, _, t, _ in expanded)
    scripts = Counter()
    inactive = shadow = enabled_lights = 0
    for source, object_id, t, body in expanded:
        if t in (33, 137):
            ref = re.search(r'm_Mesh: ' + REF, body)
            if ref:
                mesh_refs[(ref[2], ref[1])] += 1
        if t in (23, 137):
            shadow += value(body, 'm_Enabled') == '1' and value(body, 'm_CastShadows') != '0'
            section = body.split('  m_Materials:', 1)[1].split('\n  m_', 1)[0]
            for fid, guid in re.findall(REF, section):
                material_refs[(guid, fid)] += 1
        if t == 114:
            ref = re.search(r'm_Script: ' + REF, body)
            if ref:
                scripts[ref[2]] += 1
        inactive += t == 1 and value(body, 'm_IsActive') == '0'
        enabled_lights += t == 108 and value(body, 'm_Enabled') == '1'
    return {
        'schema': 1, 'status': 'STATIC_ONLY_NOT_BENCHMARKED', 'scene': SCENE,
        'sceneGuid': re.search(r'^guid: (\w+)', Path(str(path)+'.meta').read_text(), re.M)[1],
        'generation': generation, 'placementDigest': value(scene, 'placementDigest'),
        'scope': 'Saved scene; flat prefabs expanded and removed components subtracted; stripped references not double-counted. LOD totals include inactive LOD levels, not per-frame visibility.',
        'counts': dict(sorted(counts.items())), 'inactiveSelfGameObjects': inactive,
        'enabledLights': enabled_lights, 'enabledShadowCasterRendererSlotsAllLods': shadow,
        'prefabInstances': dict(sorted(instances.items())),
        'serializedAuthoringCounts': {k: int(value(scene, k)) for k in ('canopyCount','fernCount','shrubCount','rockCount','detailCount')},
        'exactSamePrefabParentPositionCandidates': [dict(guid=k[0], parent=k[1], position=k[2], sceneFileIDs=v)
                                                  for k, v in duplicates.items() if len(v) > 1],
        'meshReferencesAllLods': [dict(guid=k[0], fileID=k[1], rendererSlots=v) for k, v in mesh_refs.most_common()],
        'materialReferences': [dict(guid=k[0], fileID=k[1], slots=v) for k, v in material_refs.most_common()],
        'scriptGuids': dict(scripts), 'inputHashes': hashes,
        'unavailable': ['Imported mesh vertex/index counts', 'Effective camera LOD and visible renderers',
                        'GPU shader variants actually executed', 'Terrain detail density data', 'CPU/GPU frame timings']}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, help='New report file; refuses overwrite. Default: stdout.')
    args = parser.parse_args()
    report = audit()
    report['capturedUtc'] = datetime.now(timezone.utc).isoformat()
    text = json.dumps(report, indent=2) + '\n'
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open('x') as stream:
            stream.write(text)
        print(args.output)
    else:
        print(text, end='')


if __name__ == '__main__':
    main()
