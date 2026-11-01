#!/usr/bin/env python3
"""Read-only WLD foundation and optional G2 receipt validation. Never launches Unity."""
import hashlib
import json
import math
from pathlib import Path
import re
import sys
ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT/'Docs/Implementation/World/WLD-S001'
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()
def check_owner_preview(plan, problems):
    """Bind returned owner evidence to this plan; never infer gameplay or design acceptance."""
    path=DOC/'Evidence/G1_OwnerPreview_20261024.json'
    if not path.exists():return {'status':'OWNER_RUN_PENDING'}
    before=len(problems)
    try:
        report=json.loads(path.read_text())
        baseline=json.loads((DOC/'ProtectedObjectInventory.json').read_text())
        expected={'sourceScene':plan['sourceScene'],'sourceHash':plan['sourceSceneSha256'],
            'terrainGuid':plan['sourceTerrainGuid'],'terrainHash':plan['sourceTerrainSha256'],
            'planHash':sha(ROOT/'Assets/LastSignal/Data/WorldReconstruction/WLD-S001/Plan.json'),
            'generatorVersion':plan['generatorVersion'],'seed':plan['seed'],
            'sourceHasUnsavedChanges':False,'sessionFlows':1,'missingScripts':0,
            'stageGeneration':'NOT_RUN','gameplay':'NOT_RUN','performance':'NOT_RUN'}
        for key,value in expected.items():
            if report.get(key)!=value:problems.append('Owner evidence mismatch: '+key)
        for key in ('protectedObjects','perimeterObjectIds','protectedBounds','vehicleResidentBounds'):
            if report.get(key)!=baseline[key]:problems.append('Owner inventory differs from G0: '+key)
        summary=report.get('previewSummary','')
        metrics=re.search(r'Coarse plan samples (\d+)×(\d+), height ([-\d.]+)…([-\d.]+) m',summary)
        delta=re.search(r'Protected sampled delta=([\d.]+) m \((\d+) samples\)',summary)
        if not metrics or not delta:
            problems.append('Owner evidence lacks supported preview measurements')
        elif (int(metrics[1]),int(metrics[2]))!=(2*plan['previewResolution']-1,plan['previewResolution']) or float(delta[1])!=0 or int(delta[2])<=0:
            problems.append('Owner preview sampling/protection check failed')
        return {'status':'OWNER_EVIDENCE_MATCHED' if len(problems)==before else 'EVIDENCE_MISMATCH',
            'path':str(path.relative_to(ROOT)),'sha256':sha(path),
            'protectedObjects':len(report.get('protectedObjects',[])),
            'perimeterCandidates':len(report.get('perimeterObjectIds',[])),
            'reportedHeightMetersRounded':[float(metrics[3]),float(metrics[4])] if metrics else None,
            'reportedProtectedSamples':int(delta[2]) if delta else None,
            'scope':'Owner-returned coarse preview; no independent terrain evaluation or visual/gameplay acceptance'}
    except (ValueError,KeyError,TypeError) as error:
        problems.append('Invalid owner evidence: '+str(error))
        return {'status':'EVIDENCE_MISMATCH'}
def check_stage(plan, problems):
    path=DOC/'G2_Latest.json'
    stage=ROOT/plan['proposedStage']
    if not path.exists():
        if stage.exists():problems.append('Stage exists without a successful G2 receipt')
        if (ROOT/plan['generatedAssets']).exists():problems.append('Unreceipted generation output exists; inspect failed-run journal')
        journals=sorted((DOC/'Evidence').glob('G[0-9]*.json'))
        if journals:
            try:
                last=json.loads(journals[-1].read_text())
                remaining=[p for p in last['createdPaths'] if (ROOT/p).exists() or (ROOT/(p+'.meta')).exists()]
                if last['status']=='FAILED' and not remaining and not stage.exists():return 'FAILED_ROLLED_BACK_OWNER_RETRY_PENDING'
                problems.append('Incomplete G2 journal/output requires review: '+journals[-1].name)
                return 'INCOMPLETE'
            except (OSError,ValueError,KeyError,TypeError) as error:
                problems.append('Invalid G2 journal: '+str(error));return 'FAILED'
        return 'NOT_RUN'
    try:
        report=json.loads(path.read_text())
        expected={'status':'IMPLEMENTED_OWNER_ACCEPTANCE_PENDING','stageScene':plan['proposedStage'],
            'sourceScene':plan['sourceScene'],'sourceHash':plan['sourceSceneSha256'],
            'planHash':sha(ROOT/'Assets/LastSignal/Data/WorldReconstruction/WLD-S001/Plan.json'),
            'saveSlot':'wld_s001_naturalworld_stage','terrainGeneration':'NOT_RUN',
            'gameplay':'NOT_RUN','navigation':'NOT_RUN','performance':'NOT_RUN'}
        for key,value in expected.items():
            if report.get(key)!=value:problems.append('G2 receipt mismatch: '+key)
        if not stage.is_file() or sha(stage)!=report['stageHash']:problems.append('Stage differs from G2 receipt')
        bounds=report['residentBounds']
        if bounds!={'m_Center':{'x':-250.0,'y':20.0,'z':0.0},'m_Extent':{'x':500.0,'y':60.0,'z':250.0}}:problems.append('G2 resident bounds mismatch')
        generation=report['generation']
        if not generation.startswith(plan['generatedAssets']+'/G') or '..' in generation:raise ValueError('Unowned generation path')
        for asset in report['assets']:
            if not asset['destination'].startswith(generation+'/'):raise ValueError('Unowned asset path')
            for field,hashfield in [('source','sourceHash'),('destination','destinationHash')]:
                p=ROOT/asset[field]
                if not p.is_file() or sha(p)!=asset[hashfield]:problems.append('G2 asset mismatch: '+asset[field])
            if sha(ROOT/(asset['source']+'.meta'))!=asset['sourceMetaHash']:problems.append('G2 source metadata changed')
            if sha(ROOT/(asset['destination']+'.meta'))!=asset['destinationMetaHash']:problems.append('G2 candidate importer metadata changed')
            meta=(ROOT/(asset['destination']+'.meta')).read_text()
            if 'guid: '+asset['destinationGuid'] not in meta or asset['sourceGuid']==asset['destinationGuid']:problems.append('G2 asset GUID isolation failed')
        return 'RECEIPT_CHECKED_STATIC_ONLY'
    except (OSError,ValueError,KeyError,TypeError) as error:
        problems.append('Invalid G2 receipt: '+str(error));return 'FAILED'
def check_terrain_stage(plan, problems):
    receipt_path=DOC/'G3_Latest.json'
    scene='Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_TerrainStage.unity'
    if not receipt_path.exists():
        if (ROOT/scene).exists():problems.append('G3 scene exists without successful receipt')
        return 'NOT_RUN'
    try:
        r=json.loads(receipt_path.read_text())
        if r['status']!='IMPLEMENTED_OWNER_ACCEPTANCE_PENDING' or r['scene']!=scene or r['generator']!='WLD-G3-terrain-v1':raise ValueError('Unexpected G3 ownership/version/status')
        if r['g2ReceiptHash']!=sha(DOC/'G2_Latest.json') or r['planHash']!=sha(ROOT/'Assets/LastSignal/Data/WorldReconstruction/WLD-S001/Plan.json'):raise ValueError('G3 input receipt changed')
        generation=r['generation']
        if not generation.startswith(plan['generatedAssets']+'/G3-') or '..' in generation:raise ValueError('Unowned G3 generation')
        for asset,hash_key,meta_key in [(scene,'sceneHash',None),(r['coreAsset'],'coreHash','coreMetaHash'),(r['westAsset'],'westHash','westMetaHash')]:
            if asset!=scene and not asset.startswith(generation+'/'):raise ValueError('Unowned G3 asset')
            if sha(ROOT/asset)!=r[hash_key]:problems.append('G3 hash changed: '+asset)
            if meta_key and sha(ROOT/(asset+'.meta'))!=r[meta_key]:problems.append('G3 metadata changed: '+asset)
        if r['samplesPerTile']!=513*513 or r['seamDelta']>.0001 or r['seamAngleDegrees']>5 or r['splatSeamDelta']>2/255+.0001 or r['protectedDelta']>r['quantizationTolerance']:raise ValueError('G3 measurements exceed generation bounds')
        if r['navigation']!='REBAKE_REQUIRED' or r['gameplay']!='NOT_RUN' or r['performance']!='NOT_RUN':raise ValueError('G3 has unexpected acceptance claims')
        return 'RECEIPT_CHECKED_STATIC_ONLY'
    except (OSError,ValueError,KeyError,TypeError) as error:
        problems.append('Invalid G3 receipt: '+str(error));return 'FAILED'
def main():
    receipt=json.loads((DOC/'SourceReceipt.json').read_text())
    plan=json.loads((ROOT/'Assets/LastSignal/Data/WorldReconstruction/WLD-S001/Plan.json').read_text())
    problems=[]
    for path,expected in receipt['sha256'].items():
        if not (ROOT/path).is_file() or sha(ROOT/path)!=expected:problems.append('Source changed: '+path)
    core,ext=plan['core'],plan['extension']
    if not(ext['maxX']==core['minX'] and ext['minZ']==core['minZ'] and ext['maxZ']==core['maxZ'] and ext['maxX']-ext['minX']==core['maxX']-core['minX']):problems.append('Tile seam/layout mismatch')
    if plan['sourceSceneSha256']!=receipt['sha256'][plan['sourceScene']]:problems.append('Plan/source receipt mismatch')
    stage=check_stage(plan,problems)
    terrain_stage=check_terrain_stage(plan,problems)
    navigation='NOT_RUN'
    nav_receipt=DOC/'G4_Navigation_Latest.json'
    nav_scene='Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_NavigationStage.unity'
    if nav_receipt.exists():
        try:
            nav=json.loads(nav_receipt.read_text())
            if nav['scene']!=nav_scene or nav['status']!='IMPLEMENTED_OWNER_ACCEPTANCE_PENDING':raise ValueError('Unexpected navigation ownership/status')
            if nav['terrainReceiptHash']!=sha(DOC/'G3_Latest.json'):raise ValueError('G3 receipt changed')
            if nav['sourceHash']!=sha(ROOT/'Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_TerrainStage.unity'):raise ValueError('G3 scene changed')
            if not nav['navAsset'].startswith(plan['generatedAssets']+'/G4Nav-') or '..' in nav['navAsset']:raise ValueError('Unowned NavMesh asset')
            for path,expected in [(nav_scene,nav['sceneHash']),(nav['navAsset'],nav['navHash']),(nav['navAsset']+'.meta',nav['navMetaHash'])]:
                if sha(ROOT/path)!=expected:raise ValueError('Navigation hash changed: '+path)
            if not nav['spawns'] or any(not s['found'] for s in nav['spawns']):raise ValueError('Spawn coverage incomplete')
            navigation='RECEIPT_CHECKED_STATIC_ONLY'
        except (OSError,ValueError,KeyError,TypeError) as error:problems.append('Navigation receipt: '+str(error));navigation='FAILED'
    elif (ROOT/nav_scene).exists():problems.append('Navigation candidate exists without a receipt')
    if plan['heightmapResolution']!=513 or plan['previewResolution']!=129:problems.append('Sampling changed; review budget')
    if any(a['y']<b['y'] for a,b in zip(plan['drainage'],plan['drainage'][1:])):problems.append('Drainage rises downstream')
    if any(r['maximumGrade']!=0 for r in plan['routes']):problems.append('Unaccepted road grade limit introduced')
    if len([r for r in plan['routes'] if r['preserveExisting']])!=17:problems.append('Existing road/trail protection count changed')
    def finite(x):
        if isinstance(x,float) and not math.isfinite(x):problems.append('Non-finite plan value')
        elif isinstance(x,dict):
            for v in x.values():finite(v)
        elif isinstance(x,list):
            for v in x:finite(v)
    finite(plan)
    folder=ROOT/'Assets/LastSignal/Scripts/Editor/WorldReconstruction'
    forbidden=('SetHeights(', 'SetAlphamaps(', 'SaveScene(', 'OpenScene(', 'CreateAsset(', 'SaveAssets(', '[InitializeOnLoad', '[ExecuteAlways', 'AssetDatabase.Refresh(')
    for script in folder.glob('*.cs'):
        text=script.read_text()
        # Only the explicit G2 utilities may save/open/copy an isolated scene. No generation or auto callbacks anywhere.
        disallowed=forbidden if script.name not in ('WorldStageAuthoring.cs','WorldStageIsolation.cs','WorldTerrainAuthoring.cs','WorldNavigationAuthoring.cs') else (
            'SetHeights(', 'SetAlphamaps(', 'CreateAsset(', 'SaveAssets(', '[InitializeOnLoad', '[ExecuteAlways', 'AssetDatabase.Refresh(')
        if script.name=='WorldTerrainAuthoring.cs':
            disallowed=('SaveAssets(', '[InitializeOnLoad', '[ExecuteAlways', 'AssetDatabase.Refresh(')
        if script.name=='WorldNavigationAuthoring.cs':
            disallowed=('SetHeights(', 'SetAlphamaps(', 'SaveAssets(', '[InitializeOnLoad', '[ExecuteAlways', 'AssetDatabase.Refresh(')
        for token in disallowed:
            if token in text:problems.append('Unexpected G0/G1 mutation hook: '+script.name+'/'+token)
    guids=[]
    for folder in (folder,ROOT/'Assets/LastSignal/Data/WorldReconstruction'):
        for meta in folder.rglob('*.meta'):
            match=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(),re.M)
            if not match:problems.append('Invalid metadata: '+str(meta.relative_to(ROOT)))
            else:guids.append(match[1])
    if len(guids)!=len(set(guids)):problems.append('Duplicate new asset GUID')
    preview=check_owner_preview(plan,problems)
    g2_acceptance='OWNER_ACCEPTANCE_PENDING'
    if (DOC/'G2_Acceptance.json').exists():
        acceptance=json.loads((DOC/'G2_Acceptance.json').read_text())
        if acceptance.get('status')=='ACCEPTED' and acceptance.get('receiptSha256')==sha(DOC/'G2_Latest.json'):
            g2_acceptance='ACCEPTED'
        else:problems.append('G2 acceptance does not match receipt')
    result=dict(status='FAIL' if problems else 'VERIFIED_STATIC_ONLY',sourceFilesChecked=len(receipt['sha256']),
        footprintMeters=[core['maxX']-ext['minX'],core['maxZ']-core['minZ']],protectedRouteDefinitions=17,
        actualTerrainGeneration=terrain_stage,navigationCandidate=navigation,unityTests='NOT_RUN',preview=preview,
        architectureApproval='ACCEPTED_2026-10-24',candidateStage=stage,
        g2Acceptance=g2_acceptance,ownerAcceptance='G3_OWNER_ACCEPTANCE_PENDING' if g2_acceptance=='ACCEPTED' else 'G2_OWNER_ACCEPTANCE_PENDING',failures=problems)
    print(json.dumps(result,indent=2));return bool(problems)
if __name__=='__main__':sys.exit(main())
