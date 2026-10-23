#!/usr/bin/env python3
"""AST-S001 static audit. Never writes Assets; each run gets a new evidence directory."""
import collections as C
import csv, datetime, hashlib, json, os, re, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
TP='Assets/ThirdParty'
GUID=re.compile(r'guid:\s*([0-9a-f]{32})')
TYPES={'prefabs':{'.prefab'},'models':{'.fbx','.obj','.blend','.glb','.gltf','.dae'},'materials':{'.mat'},'textures':{'.png','.jpg','.jpeg','.tga','.tif','.tiff','.psd','.exr','.hdr','.dds'},'shaders':{'.shader','.shadergraph','.shadersubgraph','.hlsl','.cginc'},'scenes':{'.unity'},'scripts':{'.cs','.js'},'audio':{'.wav','.mp3','.ogg','.aiff','.flac'},'animations':{'.anim','.controller','.overridecontroller'}}
TEXT={'.meta','.asset','.prefab','.unity','.mat','.controller','.overridecontroller','.anim','.playable','.terrainlayer','.vfx','.shader','.shadergraph','.shadersubgraph','.hlsl','.cginc','.cs','.json','.asmdef','.asmref','.rsp','.txt','.md','.py','.sh','.yaml','.yml','.uxml','.uss'}
WRAPPERS={'Animation','Audio','Characters','Environments','Frameworks','Loot','Samples','UI','Vehicles','VFX','Weapons','Zombies','AssetsStore','S23 NEW ASSETS','VGtiXN','Animation/AnimationLibraries','Weapons/PISTOLS','Environments/TT3D','Environments/Silver_Cats','Zombies/ArtStore3D','Zombies/NewPunch','Animation/KevinIglesias'}
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT).decode(errors='replace')
def scan():
 os.chdir(ROOT)
 out=ROOT/'Docs/Implementation/Assets/Evidence'/datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ');out.mkdir(parents=True)
 def dump(n,v): (out/n).write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
 (out/'git-status.txt').write_text(git('status','--porcelain=v1','--untracked-files=all'))
 baseline={'branch':git('branch','--show-current').strip(),'commit':git('rev-parse','HEAD').strip(),'status_lines':len((out/'git-status.txt').read_text().splitlines())}
 files=[]; dirs=[]
 for base in ['Assets','ProjectSettings','Packages','Tools']:
  for d,ds,fs in os.walk(base):
   ds[:]=[x for x in ds if not x.startswith('.') and x!='__pycache__']
   dirs.append(d)
   files.extend(str(Path(d)/f) for f in fs if not f.startswith('.'))
 vendor=[p for p in files if p.startswith(TP+'/')]
 roots=[]
 def boundaries(p):
  rel=str(p.relative_to(Path(TP)))
  if rel=='.' or rel in WRAPPERS:
   for child in sorted(p.iterdir()):
    if not child.name.startswith('.') and child.suffix!='.meta': boundaries(child)
  else:roots.append(str(p))
 boundaries(Path(TP))
 def owner(p):
  return next((r for r in roots if p==r or p==r+'.meta' or p.startswith(r+'/')),None)
 guidmap={}; dupguid=C.defaultdict(list); metas={}; contents={}; refs={}; reverse=C.defaultdict(set); risks=[]
 for p in files:
  if Path(p).suffix=='.meta':
   t=Path(p).read_text(errors='replace');m=re.search(r'^guid: ([a-f0-9]{32})',t,re.M)
   if m:metas[p[:-5]]=m[1];guidmap[m[1]]=p[:-5];dupguid[m[1]].append(p[:-5])
 # Resolve package shader GUIDs without bringing all Library data into inventory.
 for p in Path('Library/PackageCache').glob('*/**/*.meta'):
  if p.name.endswith(('.shader.meta','.shadergraph.meta','.shadersubgraph.meta')):
   m=re.search(r'^guid: ([a-f0-9]{32})',p.read_text(errors='replace'),re.M)
   if m:guidmap[m[1]]=str(p)[:-5]
 for p in files:
  if Path(p).suffix.lower() not in TEXT:continue
  with open(p,'rb') as f:b=f.read()
  if b'\x00' in b[:2048]:continue
  t=b.decode(errors='replace');contents[p]=t
  refs[p]={guidmap[g] for g in GUID.findall(t) if g in guidmap and guidmap[g]!=p}
  for dep in refs[p]:reverse[dep].add(p)
  if Path(p).suffix in {'.cs','.py','.sh','.json','.asmdef','.asmref','.rsp'}:
   for i,line in enumerate(t.splitlines(),1):
    if re.search(r'Assets/ThirdParty|Resources\.Load|Addressables|AssetBundle|StreamingAssets|LoadAssetAtPath|Shader\.Find',line):risks.append({'path':p,'line':i,'text':line.strip()[:600]})
 def closure(root):
  seen=set();q=[root]
  while q:
   p=q.pop()
   if p in seen:continue
   seen.add(p);q.extend(refs.get(p,()))
  return seen
 scenes=[p for p in files if p.endswith('.unity') and not p.startswith(TP+'/')]
 scene_deps={p:closure(p) for p in scenes}
 implicit_roots=[p for p in files if '/Resources/' in p and not p.endswith('.meta')] + [p for p in files if p.startswith('ProjectSettings/') and p.endswith('.asset')]
 implicit_deps=set().union(*(closure(p) for p in implicit_roots))
 critical=[p for p in scenes if p.endswith('/FirstTownCore.unity') or p.endswith('/ForestPrototype120.unity')]
 build=re.findall(r'path: (.+)',contents.get('ProjectSettings/EditorBuildSettings.asset',''))
 missing=[]
 for p in vendor+[d for d in dirs if d.startswith(TP+'/')]:
  if p.endswith('.meta'):
   if not Path(p[:-5]).exists():missing.append({'kind':'orphan_meta','path':p})
  elif not Path(p+'.meta').exists():missing.append({'kind':'missing_meta','path':p})
 owners={p:owner(p) for p in vendor}
 records=[]; snapshots=[]; sizegroups=C.defaultdict(list)
 tracked=set(git('ls-files','-z',TP).split('\0'))
 for p in vendor:
  st=Path(p).stat();snapshots.append({'path':p,'size':st.st_size,'sha256':sha(Path(p)),'guid':metas.get(p),'tracked':p in tracked,'allocated_bytes':st.st_blocks*512})
  if not p.endswith('.meta'):sizegroups[st.st_size].append(p)
 hashes={x['path']:x['sha256'] for x in snapshots}
 duplicates=[]
 for size,ps in sizegroups.items():
  if len(ps)<2 or size==0:continue
  hs=C.defaultdict(list)
  for p in ps:hs[hashes[p]].append(p)
  for h,group in hs.items():
   if len(group)>1:
    duplicates.append({'sha256':h,'size_each':size,'upper_bound_bytes':size*(len(group)-1),'paths':group,'importer_hashes':{p:hashes.get(p+'.meta') for p in group},'referencers':{p:sorted(reverse[p]) for p in group}})
 for idx,r in enumerate(roots,1):
  ps=[p for p in vendor if owners[p]==r];assets=[p for p in ps if not p.endswith('.meta')];ext=C.Counter(Path(p).suffix.lower() for p in assets)
  counts={k:sum(ext[e] for e in es) for k,es in TYPES.items()}
  shaders=C.Counter(); shader_evidence=[]
  for p in assets:
   if p.endswith('.mat'):
    m=re.search(r'm_Shader: (.+)',contents.get(p,''));s=m[1] if m else 'UNKNOWN';g=GUID.search(s)
    target=('BUILTIN:'+s if g[1]=='0000000000000000f000000000000000' else guidmap.get(g[1],'UNKNOWN')) if g else 'BUILTIN:'+s
    if target not in contents and Path(target).is_file() and Path(target).suffix in TEXT:contents[target]=Path(target).read_text(errors='replace')
    st=contents.get(target,'')
    kind='HDRP' if 'high-definition' in target or 'HDRenderPipeline' in st or 'HighDefinition.HD' in st else 'URP' if 'universal' in target or 'UniversalPipeline' in st or 'UniversalTarget' in st else 'BUILTIN' if target.startswith('BUILTIN:') and 'fileID: 0' not in target else 'UNKNOWN'
    shaders[kind]+=1;shader_evidence.append({'material':p,'shader':target,'classification':kind})
  indicators=[{'path':p,'hdrp':bool(re.search('HDRenderPipeline|HighDefinition|high-definition',contents.get(p,''))),'urp':bool(re.search('UniversalPipeline|UniversalTarget|Rendering.Universal|render-pipelines.universal',contents.get(p,'')))} for p in assets if Path(p).suffix in TYPES['shaders']|{'.cs'}]
  if not counts['materials'] and not counts['shaders'] and not counts['scripts'] and not counts['scenes'] and not ext['.asset']:compat='PIPELINE_AGNOSTIC'
  elif shaders['URP'] or (shaders['HDRP'] and (shaders['BUILTIN'] or shaders['UNKNOWN'])):compat='HDRP_MIGRATION_REQUIRED'
  elif shaders['BUILTIN'] and not shaders['HDRP']:compat='BUILTIN_LEGACY'
  else:compat='UNKNOWN' # HDRP references alone do not establish working or conversion provenance.
  consumers=sorted({q for p in assets for q in reverse[p] if not q.startswith(r+'/') and q!=r})
  sceneuses=[s for s,deps in scene_deps.items() if any(p in deps for p in assets)]
  special=sorted({part for p in ps for part in Path(p).parts if part in {'Editor','Resources','StreamingAssets','Plugins','Gizmos','Editor Default Resources','Standard Assets'}})
  special+=sorted({Path(p).suffix for p in ps if Path(p).suffix in {'.asmdef','.asmref','.rsp','.dll','.so','.dylib','.bundle'}})
  pathrefs=[x for x in risks if r in x['text'] or (r.startswith(TP+'/') and any(v in x['text'] for v in [r+'/']))]
  dynamic=bool('Resources' in special or 'StreamingAssets' in special or any(re.search(r'assetBundleName:[ \t]*[^\s]',contents.get(p,'')) for p in ps if p.endswith('.meta')))
  dynamic = dynamic or any(p in implicit_deps for p in assets) or bool(pathrefs)
  usage='ACTIVE_PRODUCTION' if any(s in critical and s.endswith('FirstTownCore.unity') or s in build for s in sceneuses) else 'ACTIVE_DEVELOPMENT' if sceneuses else 'DYNAMIC_USAGE_POSSIBLE' if dynamic else 'REFERENCED_LEGACY' if consumers else 'UNREFERENCED_CANDIDATE'
  licenses=[p for p in assets if re.search(r'license|licence|copyright|readme|credit|attribution|terms',Path(p).name,re.I)]
  urls=sorted({u.rstrip(').,') for p in licenses for u in re.findall(r'https?://[^\s<>"\]]+',contents.get(p,''))})
  license_label='LICENSE_UNVERIFIED';attrib='UNKNOWN'
  for p in licenses:
   t=contents.get(p,'')
   if re.search(r'Creative Commons Zero|CC0 1.0|creativecommons.org/publicdomain/zero',t,re.I):license_label='CC0_TEXT_FOUND_SCOPE_REVIEW_REQUIRED'
   elif 'MIT License' in t:license_label='MIT_TEXT_FOUND_SCOPE_REVIEW_REQUIRED'
  category='Audio' if counts['audio'] and not counts['models'] and not counts['materials'] else 'Animation' if '/Animation/' in r else 'UI' if '/UI/' in r else 'Models'
  destination=TP+('/10_Pipeline_Agnostic/'+category+'/' if compat=='PIPELINE_AGNOSTIC' else '/20_HDRP_Migration_Review/')+Path(r).name
  record={'id':f'AST-{idx:03d}','folder':Path(r).name,'original_path':r,'boundary':'curated collection boundary; nested vendor hierarchy preserved', 'limitations':'Binary model subassets, texture resolution, visual suitability and live shader compilation require Editor inspection. Static GUID closure is not proof of runtime use.','vendor':('Quaternius' if any('Models by @Quaternius' in contents.get(p,'') for p in licenses) else 'Kenney' if any('Created/distributed by Kenney' in contents.get(p,'') for p in licenses) else 'Digital Ruby, LLC / Jeff Johnson' if any('Created by Jeff Johnson' in contents.get(p,'') for p in licenses) else 'MutantGopher' if any('Author: MutantGopher' in contents.get(p,'') for p in licenses) else 'UNKNOWN'),'source_urls_unverified':urls,'license':license_label,'license_evidence':licenses,'attribution':attrib,'redistribution':'UNKNOWN','bytes':sum(Path(p).stat().st_size for p in ps),'allocated_bytes':sum(Path(p).stat().st_blocks*512 for p in ps),'file_count':len(assets),'meta_count':len(ps)-len(assets),**counts,'extensions':dict(ext),'pipeline':compat,'material_shader_counts':dict(shaders),'material_evidence':shader_evidence,'pipeline_indicators':indicators,'usage':usage,'referencing_scenes':sceneuses,'direct_external_referencers':consumers,'special_paths':special,'path_references':pathrefs,'dynamic_usage_possible':dynamic,'guid':metas.get(r,'UNKNOWN'),'suggested_destination':destination,'action':'PROTECTED_DO_NOT_MOVE','risks':['SOURCE_BACKUP_NOT_VERIFIED','OWNER_EDITOR_VALIDATION_PENDING']+(['PATH_DEPENDENCY'] if pathrefs else [])+(['SPECIAL_PATHS'] if special else []),'quality':'UNKNOWN_VISUAL_REVIEW_REQUIRED' if counts['models'] or counts['materials'] else 'NOT_APPLICABLE','confidence':'HIGH_COUNTS_MEDIUM_STATIC_USAGE_LOW_VISUAL','recommendation':'Keep source; review HDRP adaptation' if compat in {'BUILTIN_LEGACY','HDRP_MIGRATION_REQUIRED'} else 'Keep as-is pending verification','tracked_files':sum(p in tracked for p in ps)}
  records.append(record)
 baseline.update({'package_units':len(records),'top_level_entries':len([p for p in Path(TP).iterdir() if p.suffix!='.meta' and not p.name.startswith('.')]),'asset_files':sum(x['file_count'] for x in records),'meta_files':sum(p.endswith('.meta') for p in vendor),'bytes':sum(x['size'] for x in snapshots),'allocated_bytes':sum(x['allocated_bytes'] for x in snapshots),'tracked_vendor_files':sum(x['tracked'] for x in snapshots),'duplicate_content_upper_bound_bytes':sum(x['upper_bound_bytes'] for x in duplicates),'duplicate_groups':len(duplicates),'integrity_issues':len(missing),'duplicate_guid_groups':len([x for x in dupguid.values() if len(x)>1]),'critical_scenes':critical,'build_scenes':build,'guid_inventory_sha256':hashlib.sha256(json.dumps(sorted((p,g) for p,g in metas.items() if p.startswith(TP+'/'))).encode()).hexdigest()})
 dump('summary.json',baseline);dump('inventory.json',records);dump('file-manifest.json',snapshots);dump('duplicates.json',sorted(duplicates,key=lambda x:-x['upper_bound_bytes']));dump('integrity.json',{'pairing':missing,'duplicate_guids':{g:p for g,p in dupguid.items() if len(p)>1}});dump('path-loading-risks.json',risks);dump('scene-dependencies.json',{s:sorted(d) for s,d in scene_deps.items()});dump('move-plan.json',{'status':'DRY_RUN_ONLY','items':[{'source':r['original_path'],'destination':r['suggested_destination'],'guid':r['guid'],'count':r['file_count'],'classification':r['pipeline'],'risks':r['risks'],'path_references':r['path_references'],'scenes':r['referencing_scenes'],'collision':Path(r['suggested_destination']).exists(),'approved':False} for r in records]})
 with (out/'inventory.csv').open('w') as f:
  fields=['id','original_path','bytes','file_count',*TYPES,'pipeline','usage','license','quality','suggested_destination','action'];w=csv.DictWriter(f,fields,extrasaction='ignore');w.writeheader();w.writerows(records)
 print(json.dumps({'evidence':str(out.relative_to(ROOT)),**baseline},indent=2))
 return out
if __name__=='__main__':scan()
