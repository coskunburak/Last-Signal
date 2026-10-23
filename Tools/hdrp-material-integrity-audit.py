#!/usr/bin/env python3
"""Non-Unity P0 evidence/plan generator. Never changes Unity assets. Requires Pillow.
Run before the first owner repair; do not regenerate the approved plan after applying it.
"""
import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Docs/Implementation/VisualRealismMigration/Evidence'
HDRP = 'Assets/LastSignal/Art/HdrpMaterialRecovery'


def sha(path):
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


def floats(text):
    return {k: float(v) for k, v in re.findall(r'^    - (\w+): ([-+\d.eE]+)$', text, re.M)}


def textures(text):
    return dict(re.findall(r'- (\w+):\s+m_Texture: \{fileID: [^,}]+, guid: (\w+)', text))


def keywords(text):
    match = re.search(r'm_ValidKeywords:(.*?)m_InvalidKeywords:', text, re.S)
    return re.findall(r'- (\S+)', match[1]) if match else []


def main():
    if (OUT / 'material_integrity_execution.json').exists():
        raise SystemExit('Existing execution receipt: preserve the approved plan; inspect receipt before generating another plan.')
    index = {}
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(errors='replace')[:220], re.M)
        if match:
            index[match[1]] = str(meta.relative_to(ROOT))[:-5]
    reverse = {p: g for g, p in index.items()}
    historical = json.loads((OUT / 'material_migration_audit.json').read_text())
    execution = json.loads((OUT / 'material_migration_execution.json').read_text())
    rollback = json.loads((ROOT / 'Assets/LastSignal/Settings/Rendering/HDRP/MaterialRollbackManifest.json').read_text())
    entries = {x['assetPath']: x for x in rollback['entries']}
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    texture_stats = {}

    def inspect_texture(path):
        if not path or path in texture_stats:
            return
        with Image.open(ROOT / path) as im:
            texture_stats[path] = {'guid': reverse[path], 'mode': im.mode, 'size': list(im.size),
                                   'extrema': im.getextrema(), 'sha256': sha(path),
                                   'sRGB': re.findall(r'sRGBTexture: (\d)', (ROOT / (path + '.meta')).read_text())}

    tracked_at_head = set(subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', '-z', head], cwd=ROOT).decode().split('\0'))
    records = []
    for old in historical['records']:
        path = old['assetPath']
        source = entries.get(path, {})
        text = (ROOT / path).read_text(errors='replace') if path.endswith('.mat') else ''
        props = floats(text)
        tex = textures(text)
        # Primary category precedence: embedded, vendor, surface, source-channel family, review.
        if not path.endswith('.mat'):
            category = 'H'
        elif path.startswith('Assets/ThirdParty/'):
            category = 'G'
        elif old['isAlphaClipped'] or not old['isOpaque']:
            category = 'F'
        elif source.get('metallicMapGuid') and source.get('occlusionMapGuid'):
            category = 'A' if source['metallicMapGuid'] == source['occlusionMapGuid'] else 'C'
        elif source.get('metallicMapGuid'):
            category = 'B'
        elif source.get('occlusionMapGuid'):
            category = 'D'
        elif source and source.get('metallic', 0) == 0:
            category = 'E'
        else:
            category = 'I'
        tracked = path in tracked_at_head
        records.append({'sourceGuid': old['guid'], 'sourcePath': path, 'targetPath': path,
                        'category': category, 'historicalAction': old['migrationAction'],
                        'originalShader': old['currentShaderName'], 'gitHeadRestoreAvailable': tracked,
                        'rollbackPropertyRecord': bool(source), 'maskGuid': tex.get('_MaskMap', ''),
                        'metallicRemapMax': props.get('_MetallicRemapMax'),
                        'aoRemapMin': props.get('_AORemapMin')})
        if path.startswith('Assets/LastSignal/'):
            for g in [source.get('metallicMapGuid'), source.get('occlusionMapGuid')]:
                if g:
                    inspect_texture(index[g])

    recipes = []
    guards = {}

    def guard(path):
        for p in [path, path + '.meta']:
            if (ROOT / p).is_file():
                guards[p] = sha(p)

    def recipe(path, target=None):
        result = subprocess.run(['git', 'show', head + ':' + path], cwd=ROOT, text=True, capture_output=True)
        source = entries[path]
        original = result.stdout
        evidence = f'git:{head}:{path}'
        if result.returncode:
            # Only scalar-only vendor derivatives can use the incomplete legacy property receipt.
            assert not source['metallicMapGuid'] and not source['occlusionMapGuid'], path
            original = ''
            evidence = 'MaterialRollbackManifest.json: scalar-only property record; not a full vendor restore source'
        kw, fs, tx = keywords(original), floats(original), textures(original)
        assert source['originalShaderName'] in ['Universal Render Pipeline/Lit', 'Standard'], path
        assert not any(k in kw for k in ['_SPECULAR_SETUP', '_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A']), path
        assert fs.get('_WorkflowMode', 1) == 1, path
        use_metal = any(k in kw for k in ['_METALLICSPECGLOSSMAP', '_METALLICGLOSSMAP'])
        use_ao = '_OCCLUSIONMAP' in kw
        metal = index[tx['_MetallicGlossMap']] if use_metal else ''
        ao = index[tx['_OcclusionMap']] if use_ao else ''
        for p in [metal, ao]:
            if p:
                inspect_texture(p)
                assert texture_stats[p]['sRGB'] == ['0'], p
                guard(p)
        target = target or path
        r = {'sourcePath': path, 'sourceGuid': reverse[path], 'sourceLocalId': 2100000,
             'targetPath': target, 'kind': 'existing' if target == path else 'vendor',
             'metallicPath': metal, 'occlusionPath': ao, 'roughnessPath': '',
             'maskPath': f'{HDRP}/Masks/{reverse[path]}.png' if metal or ao else '',
             'metallic': fs.get('_Metallic', source['metallic']),
             'smoothness': fs.get('_Smoothness', fs.get('_Glossiness', source['smoothness'])),
             'occlusionStrength': fs.get('_OcclusionStrength', 1),
             'scale': {'x': 1, 'y': 1}, 'offset': {'x': 0, 'y': 0},
             'baseMapPath': '', 'maxSize': 2048,
             'sourceEvidence': evidence, 'sourceKeywords': kw, 'emissionEnabled': source['emissionEnabled']}
        # All scoped sources use the common base UV with identity transform.
        for scale in re.findall(r'm_Scale: \{x: ([^,]+), y: ([^}]+)', original):
            assert tuple(map(float, scale)) == (1, 1), path
        for offset in re.findall(r'm_Offset: \{x: ([^,]+), y: ([^}]+)', original):
            assert tuple(map(float, offset)) == (0, 0), path
        guard(path)
        recipes.append(r)
        return r

    for name in ['Custom9mm', 'CustomMagazine', 'DXT']:
        recipe(f'Assets/LastSignal/Art/TriplePistols/{name}/PBR.mat')
    vendors = [
        'Assets/ThirdParty/Loot/Polygon-Lite Survival Collection/Materials/MainMaterialFree.mat',
        'Assets/ThirdParty/Environments/TT3D/LowPolySci-FiStarterPack/Character/Materials/space_crew_man.mat',
        'Assets/ThirdParty/Environments/TT3D/LowPolySci-FiStarterPack/Character/Materials/space_crew_man_helmet.mat',
        'Assets/ThirdParty/Zombies/NewPunch/ShirtlessZombieFree/Materials/URP/ZombieBB_Body_URP.mat',
        'Assets/ThirdParty/Zombies/NewPunch/ShirtlessZombieFree/Materials/URP/ZombieBB_Clothes_URP.mat']
    for path in vendors:
        recipe(path, f'{HDRP}/Materials/{reverse[path]}.mat')
    med = 'Assets/ThirdParty/Loot/MedicinePack_JimmyPV/'
    model = med + 'MedicinePack_JimmyPV.fbx'
    guard(model)
    for name in ['Material_BaseColor.1001.png', 'Material_Metallic.1001.png', 'Material_Roughness.1001.png']:
        guard(med + name)
        inspect_texture(med + name)
    recipes.append({'sourcePath': model, 'sourceGuid': reverse[model], 'sourceLocalId': 2372058336338979677,
                    'targetPath': HDRP + '/Materials/Medicine.mat', 'kind': 'medicine',
                    'metallicPath': med + 'Material_Metallic.1001.png', 'occlusionPath': '',
                    'roughnessPath': med + 'Material_Roughness.1001.png',
                    'maskPath': HDRP + '/Masks/Medicine.png', 'metallic': 1, 'smoothness': 1,
                    'occlusionStrength': 1, 'scale': {'x': 1, 'y': 1}, 'offset': {'x': 0, 'y': 0},
                    'baseMapPath': med + 'Material_BaseColor.1001.png', 'maxSize': 1024,
                    'sourceEvidence': 'Historical material audit: MedicinePack is opaque, not alpha-clipped; named metallic/roughness PNG inputs.',
                    'sourceKeywords': []})
    prefabs = set()
    vendor_guids = {reverse[p] for p in vendors[:3]}
    for p in (ROOT / 'Assets/LastSignal').rglob('*.prefab'):
        if vendor_guids.intersection(re.findall(r'guid: ([0-9a-f]{32})', p.read_text(errors='replace'))):
            prefabs.add(str(p.relative_to(ROOT)))
    prefabs.update(['Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab',
                    'Assets/LastSignal/Art/FirstTown/Prefabs/Medicine.prefab'])
    for p in prefabs:
        guard(p)
    # Guard the nested source prefab as well as its material sources.
    guard(index['ef26b2dc882f2d741b1e5521ca67f8ef'])
    validations = []
    repaired_paths = {r['targetPath'] for r in recipes}
    for path, original in entries.items():
        if not path.startswith('Assets/LastSignal/') or path in repaired_paths:
            continue
        if not (original.get('metallicMapGuid') or original.get('occlusionMapGuid')):
            continue
        text = (ROOT / path).read_text()
        fs, tx = floats(text), textures(text)
        assert tx.get('_MaskMap'), path
        validations.append({'path': path, 'guid': reverse[path], 'maskGuid': tx['_MaskMap'],
                            'metallicMin': fs['_MetallicRemapMin'], 'metallicMax': fs['_MetallicRemapMax'],
                            'smoothnessMin': fs['_SmoothnessRemapMin'], 'smoothnessMax': fs['_SmoothnessRemapMax'],
                            'aoMin': fs['_AORemapMin'], 'aoMax': fs['_AORemapMax']})
        guard(path)
    plan = {'schemaVersion': 1, 'sourceHead': head, 'recipes': recipes, 'prefabs': sorted(prefabs), 'validationMaterials': validations,
            'guards': [{'path': p, 'sha256': h} for p, h in sorted(guards.items())]}
    for r in records:
        match = next((x for x in recipes if x['sourcePath'] == r['sourcePath']), None)
        if match:
            r['targetPath'] = match['targetPath']
    counts = {k: execution[k] for k in ['migratedToHdrpLit', 'migratedToHdrpUnlit', 'migratedToDualParticleUnlit',
              'migratedToHdrpTerrainLit', 'dualPipelineRetained', 'alreadyHdrpValidated', 'skippedOther', 'errorCount']}
    assert sum(counts.values()) == historical['totalMaterialsScanned'] == len(records) == len({r['sourceGuid'] for r in records})
    report = {'status': 'STATIC_INSPECTION_ONLY', 'unityTests': 'TEST_NOT_RUN', 'visual': 'VISUAL_NOT_APPROVED',
              'head': head, 'historicalCounts': counts, 'categoryCounts': dict(collections.Counter(r['category'] for r in records)),
              'scope': {'materialRecipes': len(recipes), 'prefabs': len(prefabs), 'preservedMaterialChecks': len(validations)},
              'records': records, 'textureEvidence': texture_stats,
              'limitations': ['Historical audit enumerated one material per asset GUID, not all model subassets.',
                             'Categories are source/ownership accounting, not visual acceptance.',
                             '508 vendor rollback entries are property records, not complete original backups.',
                             '188 historical skipped materials require usage-specific review; not silently accepted.',
                             'Vendor sources are left in their existing migrated state pending verified restoration.',
                             'Other model subassets and scene material overrides require owner reference/visual review.']}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT.parent / 'MaterialIntegrityRepairPlan.json').write_text(json.dumps(plan, indent=2) + '\n')
    (OUT / 'material_integrity_static_audit.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'historicalCounts': counts, 'categoryCounts': report['categoryCounts'], 'scope': report['scope']}, indent=2))


if __name__ == '__main__':
    main()
