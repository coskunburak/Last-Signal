#!/usr/bin/env python3
"""Bounded, reversible edits to four EXISTING stage-only render meshes.

No Unity invocation, scene regeneration, terrain write, or source-scene edit.
Default is read-only inspection. --apply writes after full preflight; --restore
reverts only these four assets when their post-edit bytes are still unchanged.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import re
import struct
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
GEN = Path('Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538')
STAGE = Path('Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity')
SOURCE = Path('Assets/LastSignal/Scenes/Production/FirstTownCore.unity')
EVIDENCE = Path('Docs/Implementation/FirstTownVisualSlice/Evidence/Refinement01')
MANIFEST = EVIDENCE / 'manifest.json'
BACKUP = EVIDENCE / 'original-render-meshes.zip'
TARGETS = {
    '010': ('66935c47296f9ae2becefdde08695b83020ab8fd9102eec93ee6a4b69484b266', '1751459635'),
    '012': ('7b764499d66db179ec01d7fbf9f05b6b2f2dd5282b551609c136d624d15e0805', '1956325815'),
    '013': ('eb5c1f28b5095588984716b76f0aba4efe6b8cf53ff2fa23bf46c46c696bda8f', '1654850001'),
    '014': ('13f4d239f60e70474a145eea04a2ebed64f11e6266a87dc53efa4158b08c59f1', '191549439'),
}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read(path):
    return (ROOT / path).read_bytes()


def atomic_write(path, data):
    dest = ROOT / path
    dest.parent.mkdir(parents=True, exist_ok=True)
    fd, temp = tempfile.mkstemp(prefix=dest.name + '.', suffix='.tmp', dir=dest.parent)
    try:
        with os.fdopen(fd, 'wb') as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temp, dest)
    finally:
        if os.path.exists(temp):
            os.unlink(temp)


def add(a, b): return tuple(x + y for x, y in zip(a, b))
def sub(a, b): return tuple(x - y for x, y in zip(a, b))
def mul(a, k): return tuple(x * k for x in a)
def dot(a, b): return sum(x * y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def length(a): return math.sqrt(dot(a, a))
def unit(a):
    size = length(a)
    if size < 1e-9: raise ValueError('Degenerate direction')
    return mul(a, 1 / size)


class Mesh:
    def __init__(self):
        self.vertices = []
        self.indices = []

    def triangle(self, p, uv):
        e1, e2 = sub(p[1], p[0]), sub(p[2], p[0])
        normal = unit(cross(e1, e2))
        du, dv = sub(uv[1], uv[0]), sub(uv[2], uv[0])
        determinant = du[0]*dv[1]-du[1]*dv[0]
        if abs(determinant) < 1e-9: raise ValueError('Degenerate UVs')
        tangent = unit(mul(sub(mul(e1, dv[1]), mul(e2, du[1])), 1/determinant))
        bitangent = mul(sub(mul(e2, du[0]), mul(e1, dv[0])), 1/determinant)
        handedness = 1.0 if dot(cross(normal, tangent), bitangent) >= 0 else -1.0
        start = len(self.vertices)
        self.indices += [start, start+1, start+2]
        for point, tex in zip(p, uv):
            self.vertices.append((*point, *normal, *tangent, handedness, *tex))

    def face(self, points, uv=None, outward=None):
        points = list(points)
        if uv is None:
            n = unit(cross(sub(points[1], points[0]), sub(points[2], points[0])))
            drop = max(range(3), key=lambda i: abs(n[i]))
            axes = [i for i in range(3) if i != drop]
            uv = [tuple(p[i]/1.5 for i in axes) for p in points]
        else: uv = list(uv)
        if outward is not None and dot(cross(sub(points[1], points[0]), sub(points[2], points[0])), outward) < 0:
            points.reverse(); uv.reverse()
        for i in range(1, len(points)-1):
            self.triangle([points[0], points[i], points[i+1]], [uv[0], uv[i], uv[i+1]])

    def beam(self, a, b, width, depth):
        forward = unit(sub(b, a))
        ref = (0, 1, 0) if abs(forward[1]) < .98 else (0, 0, 1)
        right = mul(unit(cross(ref, forward)), width/2)
        up = mul(unit(cross(forward, right)), depth/2)
        rings = [[add(c, add(mul(right, x), mul(up, y))) for x, y in [(-1,-1), (1,-1), (1,1), (-1,1)]] for c in (a,b)]
        self.face(rings[0], outward=mul(forward, -1))
        self.face(rings[1], outward=forward)
        for i in range(4):
            j = (i+1)%4
            points = [rings[0][i], rings[0][j], rings[1][j], rings[1][i]]
            outward = sub(mul(add(points[0], points[1]), .5), a)
            # Explicit metric UVs avoid degenerate projection on diagonal seams.
            edge = length(sub(points[1], points[0])) / 1.5
            run = length(sub(b, a)) / 1.5
            self.face(points, [(0,0), (edge,0), (edge,run), (0,run)], outward)

    def tube(self, points, radius, sides=12):
        rings = []
        for i, p in enumerate(points):
            direction = unit(sub(points[min(i+1,len(points)-1)], points[max(i-1,0)]))
            ref = (0,0,1) if abs(direction[2]) < .9 else (1,0,0)
            u = unit(cross(ref, direction)); v = unit(cross(direction, u))
            rings.append([add(p, add(mul(u,radius*math.cos(j*math.tau/sides)),mul(v,radius*math.sin(j*math.tau/sides)))) for j in range(sides)])
        distance = 0
        for k in range(len(points)-1):
            span = length(sub(points[k+1], points[k]))
            for j in range(sides):
                jj = (j+1)%sides
                p = [rings[k][j], rings[k][jj], rings[k+1][jj], rings[k+1][j]]
                out = sub(mul(add(p[0],p[1]),.5),points[k])
                self.face(p, [(j/sides,distance),(jj/sides if jj else 1,distance),(jj/sides if jj else 1,distance+span),(j/sides,distance+span)],out)
            distance += span

    def serialize(self, template):
        n = len(self.vertices)
        if n >= 65536: raise ValueError('16-bit mesh budget exceeded')
        lo = [min(p[i] for p in self.vertices) for i in range(3)]
        hi = [max(p[i] for p in self.vertices) for i in range(3)]
        center = [(a+b)/2 for a,b in zip(lo,hi)]
        extent = [(b-a)/2 for a,b in zip(lo,hi)]
        def vec(v): return '{x: %.9g, y: %.9g, z: %.9g}' % tuple(v)
        subs = ('  m_SubMeshes:\n  - serializedVersion: 2\n    firstByte: 0\n'
                f'    indexCount: {len(self.indices)}\n    topology: 0\n    baseVertex: 0\n    firstVertex: 0\n    vertexCount: {n}\n'
                f'    localAABB:\n      m_Center: {vec(center)}\n      m_Extent: {vec(extent)}\n')
        s, count = re.subn(r'  m_SubMeshes:\n.*?(?=  m_Shapes:)', lambda _: subs, template, count=1, flags=re.S)
        assert count == 1
        payload = b''.join(struct.pack('<12f',*p) for p in self.vertices)
        replacements = {
            r'  m_IndexBuffer: [0-9a-f]*': '  m_IndexBuffer: '+struct.pack('<'+'H'*len(self.indices),*self.indices).hex(),
            r'    m_VertexCount: \d+': f'    m_VertexCount: {n}',
            r'    m_DataSize: \d+': f'    m_DataSize: {len(payload)}',
            r'    _typelessdata: [0-9a-f]+': '    _typelessdata: '+payload.hex(),
            r'  m_LocalAABB:\n    m_Center: [^\n]+\n    m_Extent: [^\n]+': f'  m_LocalAABB:\n    m_Center: {vec(center)}\n    m_Extent: {vec(extent)}',
        }
        for pattern,value in replacements.items():
            s,count = re.subn(pattern,lambda _: value,s,count=1)
            assert count == 1, pattern
        return s.encode()


def roof(x): return 5.25 - abs(x)*1.85/5.65


def gables_and_soffit():
    m = Mesh()
    # Close the 0.20–0.313m gap to an 80mm roof lining. Wall overlap is 20mm.
    for side in (-1,1):
        z = side*4.03
        profile = [(-5,3.28,z),(-5,roof(5)-.08,z),(0,5.17,z),(5,roof(5)-.08,z),(5,3.28,z)]
        m.face(profile,outward=(0,0,side))
        inner = [(x,y,z-side*.09) for x,y,z in profile]
        m.face(inner,outward=(0,0,-side))
        for i in range(len(profile)):
            j=(i+1)%len(profile)
            m.face([profile[i],profile[j],inner[j],inner[i]], outward=((profile[i][0]+profile[j][0])/2,(profile[i][1]+profile[j][1])/2-4,0))
    # Downward-facing roof lining; keep the original top and collision mesh intact.
    for a,b in [(-5.65,0),(0,5.65)]:
        m.face([(a,roof(a)-.08,-4.65),(a,roof(a)-.08,4.65),(b,roof(b)-.08,4.65),(b,roof(b)-.08,-4.65)],outward=(0,-1,0))
        for side in (-1,1):
            z=side*4.65
            m.face([(a,roof(a),z),(b,roof(b),z),(b,roof(b)-.08,z),(a,roof(a)-.08,z)],outward=(0,0,side))
    for x in (-5.65,5.65):
        m.face([(x,3.4,-4.65),(x,3.4,4.65),(x,3.32,4.65),(x,3.32,-4.65)],outward=(x,0,0))
    return m


def joinery():
    m=Mesh()
    for z in (-4.65,4.65):
        m.beam((-5.65,3.4,z),(0,5.25,z),.18,.16)
        m.beam((0,5.25,z),(5.65,3.4,z),.18,.16)
    for x in (-5.65,5.65): m.beam((x,3.4,-4.65),(x,3.4,4.65),.16,.24)
    for x in (-5.05,5.05):
        for z in (-4.05,4.05): m.beam((x,0,z),(x,3.52,z),.15,.15)
    for side in (-1,1):
        z=side*4.065
        m.beam((-5.05,3.32,z),(5.05,3.32,z),.10,.06)
        for i in range(25):
            x=-4.8+i*.4
            m.beam((x,3.37,z),(x,roof(x)-.11,z),.035,.04)
        # Framed attic vent: geometry sits just off the cladding and shades it.
        z=side*4.105
        for x in (-.48,.48): m.beam((x,4.18,z),(x,4.70,z),.065,.05)
        for y in (4.18,4.70): m.beam((-.48,y,z),(.48,y,z),.065,.05)
    return m


def metalwork():
    m=Mesh()
    # Folded ridge cap follows the roof instead of reading as a floating square bar.
    for side in (-1,1):
        x=side*.18
        m.face([(0,5.30,-4.78),(0,5.30,4.78),(x,roof(x)+.018,4.78),(x,roof(x)+.018,-4.78)],outward=(side,1,0))
    for i in range(14):
        z=-4.3+i*.65
        for side in (-1,1): m.beam((side*5.6,roof(5.6)+.01,z),(0,5.26,z),.025,.035)
    for side in (-1,1):
        cx=side*5.69; cy=3.38
        # Double-wall half-round gutter, with closed end plates; no physics changes.
        count=10
        outer=[(cx+.095*math.cos(math.pi+i*math.pi/count),cy+.095*math.sin(math.pi+i*math.pi/count)) for i in range(count+1)]
        inner=[(cx+.086*math.cos(math.pi+i*math.pi/count),cy+.086*math.sin(math.pi+i*math.pi/count)) for i in range(count+1)]
        for i in range(count):
            for ring,inside in [(outer,False),(inner,True)]:
                a,b=ring[i],ring[i+1]
                out=((a[0]+b[0])/2-cx,(a[1]+b[1])/2-cy,0)
                if inside: out=mul(out,-1)
                m.face([(a[0],a[1],-4.68),(a[0],a[1],4.68),(b[0],b[1],4.68),(b[0],b[1],-4.68)],outward=out)
            for z in (-4.68,4.68):
                a,b=outer[i],outer[i+1]; c,d=inner[i+1],inner[i]
                m.face([(a[0],a[1],z),(b[0],b[1],z),(c[0],c[1],z),(d[0],d[1],z)],outward=(0,0,z))
        for i in (0,count):
            a,b=outer[i],inner[i]
            m.face([(a[0],a[1],-4.68),(b[0],b[1],-4.68),(b[0],b[1],4.68),(a[0],a[1],4.68)],outward=(0,1,0))
        # Swan-neck return to wall, downpipe and outward discharge above grade.
        m.tube([(cx,3.30,3.8),(cx,3.17,3.8),(side*5.14,2.97,3.8),(side*5.14,.33,3.8),(side*5.38,.17,3.8)],.05)
        for y in (.65,1.8,2.8):
            m.tube([(side*5.14,y-.025,3.8),(side*5.14,y+.025,3.8)],.064)
            m.beam((side*5.0,y,3.8),(side*5.14,y,3.8),.04,.045)
        # Attic vent louvers on each gable; preserve all existing doors and signs.
        for i in range(5):
            y=4.25+i*.09; z=side*4.13
            m.face([(-.42,y,z),(-.42,y+.05,z-side*.035),(.42,y+.05,z-side*.035),(.42,y,z)],outward=(0,0,side))
    return m


def paving():
    m=Mesh()
    def slab(x,z,width,depth,key):
        joint=.016; bevel=.006
        x+=joint/2; z+=joint/2; width-=joint; depth-=joint
        y=.032+((key*7)%5-2)*.0003
        outer=[(x,.012,z),(x,.012,z+depth),(x+width,.012,z+depth),(x+width,.012,z)]
        top=[(x+bevel,y,z+bevel),(x+bevel,y,z+depth-bevel),(x+width-bevel,y,z+depth-bevel),(x+width-bevel,y,z+bevel)]
        uv=lambda ps:[(p[0]/1.5,p[2]/1.5) for p in ps]
        m.face(top,uv(top),outward=(0,1,0))
        for i in range(4):
            j=(i+1)%4
            # Beveled edge faces catch grazing light, but introduce no new collider/step.
            ps=[outer[i],outer[j],top[j],top[i]]
            m.face(ps,[(0,0),(length(sub(outer[i],outer[j]))/1.5,0),(length(sub(outer[i],outer[j]))/1.5,.02),(0,.02)])
    for row in range(12):
        for col in range(3): slab(65+col*.8,-75+row*1.05,.8,1.05,row*3+col)
    # L-shaped landing reaches the existing entry at (70,-59.03), not a new doorway.
    for row in range(3):
        for col in range(8): slab(65+col*.8,-62.4+row*1.1,.8,1.1,36+row*8+col)
    return m


def decode(data):
    s=data.decode()
    count=int(re.search(r'm_VertexCount: (\d+)',s)[1])
    blob=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',s)[1])
    if len(blob)!=count*48: raise ValueError('Unexpected vertex layout')
    vertices=[struct.unpack_from('<12f',blob,i*48) for i in range(count)]
    ib=bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',s)[1])
    indices=struct.unpack('<'+'H'*(len(ib)//2),ib)
    return vertices,indices


def inspect_mesh(data):
    v,indices=decode(data)
    if len(indices)%3 or max(indices)>=len(v): raise ValueError('Invalid mesh indices')
    for p in v:
        if not all(math.isfinite(x) for x in p): raise ValueError('Nonfinite vertex')
        if abs(length(p[3:6])-1) > 1e-5 or abs(dot(p[3:6],p[6:9])) > 1e-5: raise ValueError('Invalid tangent frame')
    for i in range(0,len(indices),3):
        a,b,c=[v[j] for j in indices[i:i+3]]
        n=unit(cross(sub(b[:3],a[:3]),sub(c[:3],a[:3])))
        if dot(n,a[3:6]) < .999: raise ValueError('Normal/winding disagreement')
    return {'vertices':len(v),'triangles':len(indices)//3,'boundsMin':[min(p[i] for p in v) for i in range(3)],'boundsMax':[max(p[i] for p in v) for i in range(3)]}


def protected_hashes():
    paths=[STAGE,SOURCE,Path(str(STAGE)+'.meta'),Path(str(SOURCE)+'.meta'),GEN/'Terrain.asset',GEN/'Sky.mat',GEN/'Lighting.asset',GEN/'Mesh_011.asset',
           Path('Assets/LastSignal/Art/FirstTownForest/Generations/G20261016082700052/Terrain.asset'),Path('Assets/LastSignal/Prefabs/Player/Player.prefab')]
    return {str(p):digest(read(p)) for p in paths}


def preflight():
    s=read(STAGE).decode()
    records=re.findall(r'^--- !u!(\d+) &(\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)
    result={}
    for serial,(expected,filter_id) in TARGETS.items():
        p=GEN/f'Mesh_{serial}.asset'; data=read(p)
        if digest(data)!=expected: raise ValueError(f'{p} differs from audited bytes; preserve and inspect before editing.')
        guid=re.search(r'guid: (\w+)',read(Path(str(p)+'.meta')).decode())[1]
        uses=[(t,i) for t,i,b in records if guid in b]
        if uses!=[('33',filter_id)]: raise ValueError(f'{p} is not exclusively the expected render MeshFilter: {uses}')
        # Refuse to change a mesh if it has become shared with another scene or prefab.
        for ext in ('*.unity','*.prefab','*.asset'):
            for candidate in (ROOT/'Assets').rglob(ext):
                if candidate==ROOT/STAGE or candidate==ROOT/p: continue
                if guid.encode() in candidate.read_bytes(): raise ValueError(f'Mesh also referenced by {candidate}; create a scoped variant instead.')
        result[serial]=(p,data,guid)
    return result


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    actions=parser.add_mutually_exclusive_group()
    actions.add_argument('--apply',action='store_true')
    actions.add_argument('--restore',action='store_true')
    args=parser.parse_args()
    saved=json.loads(read(MANIFEST)) if (ROOT/MANIFEST).exists() else None
    if saved:
        current={p:digest(read(Path(p))) for p in saved['assets']}
        same=all(current[p]==v['afterSha256'] for p,v in saved['assets'].items())
        if args.restore:
            if saved['status']=='RESTORED_RENDER_MESHES' and all(current[p]==v['beforeSha256'] for p,v in saved['assets'].items()):
                print('Render meshes already restored; no writes.');return
            if not same: raise ValueError('Meshes changed after refinement; refusing to overwrite later edits.')
            with zipfile.ZipFile(ROOT/BACKUP) as backup:
                original={p:backup.read(p) for p in saved['assets']}
            for p,data in original.items():
                if digest(data)!=saved['assets'][p]['beforeSha256']: raise ValueError('Recovery archive mismatch')
            refined={p:read(Path(p)) for p in original}
            applied=[]
            try:
                for p,data in original.items(): atomic_write(Path(p),data); applied.append(p)
                saved['status']='RESTORED_RENDER_MESHES'; atomic_write(MANIFEST,(json.dumps(saved,indent=2)+'\n').encode())
            except Exception:
                for p in applied:
                    if read(Path(p))==original[p]: atomic_write(Path(p),refined[p])
                raise
            print('Restored only four render meshes. Scene, terrain and gameplay unchanged.');return
        if same:
            print('Refinement already present; no writes or regeneration.');return
        if saved['status']!='RESTORED_RENDER_MESHES': raise ValueError('Existing refinement differs; preserve later edits and inspect.')
    elif args.restore: raise ValueError('No refinement manifest to restore')
    originals=preflight()
    protected=protected_hashes()
    builders={'010':paving,'012':gables_and_soffit,'013':joinery,'014':metalwork}
    changes={}; report={'revision':'Refinement01','status':'PREPARED','verification':'OFFLINE_GEOMETRY_ONLY; UNITY_IMPORT_AND_VISUAL_ACCEPTANCE_PENDING','protectedBefore':protected,'assets':{}}
    for serial,(p,data,guid) in originals.items():
        mesh=builders[serial](); output=mesh.serialize(data.decode())
        before=inspect_mesh(data); after=inspect_mesh(output)
        changes[p]=output
        report['assets'][str(p)]={'guid':guid,'beforeSha256':digest(data),'afterSha256':digest(output),'before':before,'after':after}
    report['findings']={'gableToRoofGapBeforeMetres':[.20,roof(5)-3.3],'roofLiningThicknessMetres':.08,'gableToLiningGapAfterMetres':0,'pavingRightEdgeBefore':67.4,'existingEntryCenterX':70,'pavingRightEdgeAfter':71.4,'sceneRecordsChanged':0,'collidersChanged':0,'originalRoofCollisionMeshChanged':False,'capturesFound':[]}
    if args.apply:
        (ROOT/EVIDENCE).mkdir(parents=True,exist_ok=True)
        if not (ROOT/BACKUP).exists():
            with zipfile.ZipFile(ROOT/BACKUP,'x',compression=zipfile.ZIP_DEFLATED) as backup:
                for p,data,_ in originals.values(): backup.writestr(str(p),data)
        with zipfile.ZipFile(ROOT/BACKUP) as backup:
            for p,data,_ in originals.values():
                if backup.read(str(p))!=data: raise ValueError('Existing recovery archive differs; preserve it and inspect')
        # Detect intervening saves before publishing; never overwrite an artist's new asset edit.
        for p,data,_ in originals.values():
            if read(p)!=data: raise ValueError(f'{p} changed during preparation')
        if protected_hashes()!=protected: raise ValueError('Scene or protected asset changed during preparation; inspect before retrying')
        applied=[]
        try:
            for p,data in changes.items(): atomic_write(p,data); applied.append(p)
            if protected_hashes()!=protected: raise ValueError('Protected file changed concurrently; stop and inspect')
            report['status']='APPLIED_RENDER_MESHES';report['protectedAfter']=protected_hashes()
            atomic_write(MANIFEST,(json.dumps(report,indent=2)+'\n').encode())
        except Exception:
            original_by_path={p:data for p,data,_ in originals.values()}
            for p in applied:
                if read(p)==changes[p]: atomic_write(p,original_by_path[p])
            raise
    print(json.dumps({'status':report['status'],'assets':{p:{'beforeTriangles':v['before']['triangles'],'afterTriangles':v['after']['triangles']} for p,v in report['assets'].items()},'verification':report['verification']},indent=2))


if __name__=='__main__':
    main()
