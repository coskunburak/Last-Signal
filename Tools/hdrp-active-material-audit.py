#!/usr/bin/env python3
"""Read-only static dependency preflight. Unity resolves effective slots in a separate owner run.
Does not certify runtime reachability or shader compatibility, mutate assets, or restore vendors.
"""
from pathlib import Path
import re,json,collections,subprocess,hashlib,os
ROOT=Path(__file__).resolve().parents[1]
os.chdir(ROOT)
OUT=Path("Docs/Implementation/VisualRealismMigration/Evidence")
OUT.mkdir(parents=True,exist_ok=True)
index={}
for root in ['Assets','Library/PackageCache']:
 for meta in Path(root).rglob('*.meta'):
  m=re.search(r'^guid: (\w+)',meta.read_text(errors='replace')[:220],re.M)
  if m:index[m[1]]=str(meta)[:-5]
roots=['Assets/LastSignal/Scenes/Production/FirstTownCore.unity','Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab','Assets/LastSignal/Data/Enemies/Zombie/Resources/S022/ZombieCatalog.asset']
roots += [p for p in json.loads(Path('Docs/Implementation/VisualRealismMigration/MaterialIntegrityRepairPlan.json').read_text())['prefabs'] if 'WorldBody_tt3d' not in p]
roots += [str(p) for p in Path('Assets/LastSignal/Art/TriplePistols').rglob('*.prefab')]
refs=re.compile(r'\{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: \d+\}')
queue=collections.deque(roots);visited=set();materials={};models={};links=collections.defaultdict(set);unknown={};prefabs=[]
while queue:
 p=queue.popleft()
 if p in visited:continue
 visited.add(p)
 file=Path(p)
 if file.suffix.lower() in ['.fbx','.obj','.blend']:
  models[p]=sorted(links[p]);continue
 if file.suffix not in ['.unity','.prefab','.asset','.mat','.controller','.overrideController']:continue
 with file.open('rb') as f:prefix=f.read(64)
 if not prefix.startswith(b'%YAML'):continue
 s=file.read_text()
 if file.suffix=='.prefab':prefabs.append(p)
 if file.suffix=='.mat':
  sh=re.search(r'm_Shader: \{fileID: (-?\d+)(?:, guid: (\w+))?',s);sp=index.get(sh[2],'') if sh and sh[2] else 'BUILTIN'
  st=Path(sp).read_text(errors='replace') if sp!='BUILTIN' and Path(sp).is_file() else ''
  name=re.search(r'Shader\s+"([^"]+)',st)
  materials[p]={'shader':name[1] if name else sp,'shaderPath':sp,'hdrpSourceTag':'HDRenderPipeline' in st,'references':sorted(links[p])}
  continue
 for local,guid in refs.findall(s):
  target=index.get(guid)
  if not target:
   if not guid.startswith('0000000000000000'):unknown[guid]=p
   continue
  links[target].add(p)
  if Path(target).suffix.lower() in ['.unity','.prefab','.asset','.mat','.controller','.overridecontroller','.fbx','.obj','.blend']:
   queue.append(target)
# These are dependency candidates; nested vendor originals can be overridden in project prefabs.
# Never present this raw graph as resolved renderer-slot coverage.
head=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()
tracked=set(subprocess.check_output(['git','ls-tree','-r','--name-only','-z',head]).decode().split('\0'))
rollback=json.loads(Path('Assets/LastSignal/Settings/Rendering/HDRP/MaterialRollbackManifest.json').read_text())
plan=json.loads(Path('Docs/Implementation/VisualRealismMigration/MaterialIntegrityRepairPlan.json').read_text())
derivatives={r['sourcePath']:r['targetPath'] for r in plan['recipes'] if r['kind']!='existing'}
vendor=[]
for r in rollback['entries']:
 p=r['assetPath']
 if not p.startswith('Assets/ThirdParty/'):continue
 row={'sourcePath':p,'sourceGuid':r['materialGuid'],'inStaticDependencySet':p in visited,
      'projectDerivative':derivatives.get(p,''),'restoreStatus':'SOURCE_RECOVERY_REQUIRED',
      'currentSha256':hashlib.sha256(Path(p).read_bytes()).hexdigest() if Path(p).is_file() else '',
      'note':'Legacy property manifest is not a complete original file backup.'}
 if p in tracked:
  original=subprocess.check_output(['git','show',head+':'+p])
  row['gitHeadSha256']=hashlib.sha256(original).hexdigest()
  row['restoreStatus']='GIT_HEAD_CANDIDATE_REQUIRES_PROVENANCE_REVIEW'
  row['differsFromGitHead']=row['gitHeadSha256']!=row['currentSha256']
 vendor.append(row)
report={'status':'STATIC_PREFLIGHT_ONLY','head':head,'roots':roots,'visited':len(visited),
        'materials':materials,'models':models,'unresolvedGuids':unknown,'prefabs':sorted(prefabs),
        'vendorRecovery':vendor,'limitations':['Graph includes overridden nested source references.',
        'Model subassets need owner-run AssetDatabase resolution.',
        'Serialized dependency does not prove runtime use.',
        'HDRP source tag does not prove correct material channels or visual acceptance.']}
(OUT/'active_material_static_audit.json').write_text(json.dumps(report,indent=2)+'\n')
scope={'schemaVersion':1,'scenes':[roots[0]],'prefabs':sorted(set(p for p in roots if p.endswith('.prefab'))),
       'dataAssets':[p for p in roots if p.endswith('.asset')],
       'runtimePathsReviewed':['Assets/LastSignal/Scripts/Runtime/Atmosphere/TownAtmosphere.cs',
       'Assets/LastSignal/Scripts/Runtime/Character/CharacterViewmodelPalette.cs',
       'Assets/LastSignal/Scripts/Runtime/AI/WorldPopulationManager.cs',
       'Assets/LastSignal/Scripts/Runtime/VFX/ZombieLocalizedWounds.cs'],
       'runtimeLimit':'Authored references and explicit resource roots only; not a PlayMode trace.'}
Path('Docs/Implementation/VisualRealismMigration/MaterialCoverageScope.json').write_text(json.dumps(scope,indent=2)+'\n')
print(json.dumps({'visited':len(visited),'standaloneMaterialCandidates':len(materials),
 'modelDependencies':len(models),'unresolvedNonBuiltinGuids':unknown,
 'shaderFamilies':dict(collections.Counter(m['shader'] for m in materials.values())),
 'vendorRecoveryStatuses':dict(collections.Counter(v['restoreStatus'] for v in vendor))},indent=2))
