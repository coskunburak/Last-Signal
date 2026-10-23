#!/usr/bin/env python3
"""Bounded binary-FBX topology diagnostic; reads Medicine only and never writes source assets.
Reports projected intersections as candidates, not proof of an imported visible defect.
"""
import struct,zlib,json,hashlib,math,collections,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
os.chdir(ROOT)
p=Path('Assets/ThirdParty/Loot/MedicinePack_JimmyPV/MedicinePack_JimmyPV.fbx')
b=p.read_bytes();assert b.startswith(b'Kaydara FBX Binary  \x00\x1a\x00'), 'Expected binary FBX'
version=struct.unpack_from('<I',b,23)[0];wide=version>=7500;header='<QQQB' if wide else '<IIIB';hs=25 if wide else 13

def prop(pos):
 t=chr(b[pos]);pos+=1
 formats={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
 if t in formats:
  f='<'+formats[t];return struct.unpack_from(f,b,pos)[0],pos+struct.calcsize(f)
 if t in ['S','R']:
  n=struct.unpack_from('<I',b,pos)[0];pos+=4;v=b[pos:pos+n]
  return v.decode('utf-8',errors='replace') if t=='S' else v,pos+n
 if t in ['f','d','l','i','b','c']:
  n,encoding,length=struct.unpack_from('<III',b,pos);pos+=12;raw=b[pos:pos+length]
  if encoding:raw=zlib.decompress(raw)
  f={'f':'f','d':'d','l':'q','i':'i','b':'b','c':'b'}[t]
  return list(struct.unpack('<'+str(n)+f,raw)),pos+length
 raise ValueError(t)

def node(pos):
 end,count,length,nlen=struct.unpack_from(header,b,pos)
 if end==0:return None,pos+hs
 if end>len(b) or end<=pos:raise ValueError('Invalid FBX node bounds')
 pos+=hs;name=b[pos:pos+nlen].decode();pos+=nlen;properties=[]
 for _ in range(count):v,pos=prop(pos);properties.append(v)
 children=[]
 while pos<end-hs:
  n,pos=node(pos)
  if n is None:break
  children.append(n)
 return {'name':name,'props':properties,'children':children},end
nodes=[];pos=27
while pos<len(b)-hs:
 n,pos=node(pos)
 if n is None:break
 nodes.append(n)
objects=next(n for n in nodes if n['name']=='Objects')['children']

models=[n for n in objects if n['name']=='Model' and n['props'][1].split('\0')[0]=='PharmaSyringe2']
assert len(models)==1, 'Target model must be unique'
model=models[0]
connections=next(n for n in nodes if n['name']=='Connections')['children']
connected=[c['props'][1] for c in connections if len(c['props'])>2 and c['props'][2]==model['props'][0]]
geometries=[n for n in objects if n['name']=='Geometry' and n['props'][0] in connected]
assert len(geometries)==1, 'Target geometry must be unique'
g=geometries[0]
def child(name):return next(n for n in g['children'] if n['name']==name)
flat=child('Vertices')['props'][0]
vertices=[flat[i:i+3] for i in range(0,len(flat),3)]
assert all(math.isfinite(v) for xyz in vertices for v in xyz)
polygons=[];current=[]
for index in child('PolygonVertexIndex')['props'][0]:
 current.append(-index-1 if index<0 else index)
 if index<0:polygons.append(current);current=[]
assert not current and all(0<=i<len(vertices) for face in polygons for i in face)
def sub(a,b):return [x-y for x,y in zip(a,b)]
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def orient(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
findings=[];edges=collections.Counter()
for number,face in enumerate(polygons):
 for i,v in enumerate(face):edges[tuple(sorted((v,face[(i+1)%len(face)])))]+=1
 if len(face)<4:continue
 xyz=[vertices[i] for i in face]
 normals=[cross(sub(xyz[i],xyz[0]),sub(xyz[i+1],xyz[0])) for i in range(1,len(xyz)-1)]
 normal=max(normals,key=lambda n:dot(n,n));length=math.sqrt(dot(normal,normal))
 if length<1e-14:
  findings.append({'polygonIndex':number,'kind':'DEGENERATE_PLANE_CANDIDATE','vertexIndices':face});continue
 normal=[c/length for c in normal];axis=max(range(3),key=lambda i:abs(normal[i]))
 points=[[c for i,c in enumerate(x) if i!=axis] for x in xyz]
 crossings=[]
 for i in range(len(face)):
  for j in range(i+1,len(face)):
   if j==i+1 or (i==0 and j==len(face)-1):continue
   a,b2=points[i],points[(i+1)%len(face)];c,d=points[j],points[(j+1)%len(face)]
   if orient(a,b2,c)*orient(a,b2,d)<-1e-18 and orient(c,d,a)*orient(c,d,b2)<-1e-18:crossings.append([i,j])
 if crossings:
  findings.append({'polygonIndex':number,'kind':'PROJECTED_EDGE_INTERSECTION_CANDIDATE',
   'vertexIndices':face,'positionsInSourceUnits':xyz,'edgePairs':crossings,'projectionDroppedAxis':axis,
   'maxPlaneDeviationInSourceUnits':max(abs(dot(sub(x,xyz[0]),normal)) for x in xyz)})
layers=[]
for n in g['children']:
 if n['name'] not in ['LayerElementUV','LayerElementNormal','LayerElementMaterial']:continue
 row={'type':n['name']}
 for c in n['children']:
  if c['name'] in ['MappingInformationType','ReferenceInformationType']:row[c['name']]=c['props'][0]
  elif c['props'] and isinstance(c['props'][0],list):row[c['name']+'ValueCount']=len(c['props'][0])
 layers.append(row)
report={'status':'SOURCE_TOPOLOGY_INSPECTED_VISUAL_NOT_APPROVED','sourcePath':str(p),
 'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'fbxVersion':version,
 'modelId':model['props'][0],'modelName':'PharmaSyringe2','geometryId':g['props'][0],
 'geometryName':g['props'][1].split('\0')[0], 'unityMeshLocalId':8586387579714453275,
 'vertexCount':len(vertices),'polygonCount':len(polygons),
 'polygonSizeCounts':dict(collections.Counter(map(len,polygons))),
 'sourceBoundsMin':[min(v[i] for v in vertices) for i in range(3)],
 'sourceBoundsMax':[max(v[i] for v in vertices) for i in range(3)],
 'boundaryEdges':sum(n==1 for n in edges.values()),'nonManifoldEdges':sum(n>2 for n in edges.values()),
 'layers':layers,'findings':findings,
 'decision':'No mesh edit. Owner close-up and Unity-imported mesh diagnostics required.',
 'limitations':['Projection intersections in nonplanar polygons do not identify Unity discarded polygons with certainty.',
 'Source units are not Unity world meters; model/import transforms are not applied.',
 'Boundary edges alone do not prove an unintended visible hole.',
 'No vendor FBX, importer, prefab, UV, normal or material reference was modified.']}
out=Path('Docs/Implementation/VisualRealismMigration/Evidence/medicine_geometry_source_audit.json')
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ['status','modelName','geometryName','vertexCount','polygonCount','boundaryEdges','nonManifoldEdges','findings']},indent=2))
