using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LastSignal.VisualSlice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    internal sealed class SliceComposition
    {
        readonly SliceSite site; readonly SliceAssets art; readonly FirstTownSliceState s;
        readonly List<FirstTownSliceState.RendererRecord> changed=new List<FirstTownSliceState.RendererRecord>();
        readonly StringBuilder digest=new StringBuilder();
        internal SliceComposition(SliceSite site,SliceAssets art){this.site=site;this.art=art;s=site.state;}
        internal static Transform Group(string name,Transform parent)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        void Hide(Renderer r)
        {if(!r || changed.Any(c=>c.target==r))return;changed.Add(new FirstTownSliceState.RendererRecord{target=r,enabled=r.enabled,materials=r.sharedMaterials});r.enabled=false;}
        internal void Build()
        {
            Roads();Architecture();Props();Vegetation();Signs();
            s.renderers=changed.ToArray();s.placementDigest=SliceAuthoring.HashText(digest.ToString());
        }
        void Roads()
        {
            var root=Group("01 Continuous roads and aprons",s.generated.transform);
            var network=site.town.Find("01 Connected road network");
            foreach(string name in new[]{"Safehouse approach","South cross street","Gas station access"})
            {
                var tr=network.Find(name+"/Road surface");if(!tr)throw new InvalidOperationException("Road surface missing: "+name);
                var mf=tr.GetComponent<MeshFilter>();var source=tr.GetComponent<MeshRenderer>();var b=mf.sharedMesh.bounds;
                if(mf.sharedMesh.vertexCount!=4 || mf.sharedMesh.uv.Length!=4)
                    throw new InvalidOperationException("Road topology has been hand-edited: "+name+". Keep it and refit the surface authoring.");
                var focus=new SliceMesh();var retained=new SliceMesh();var originalUV=mf.sharedMesh.uv;
                int count=Mathf.CeilToInt(b.size.z/2);
                for(int i=0;i<count;i++)
                {
                    float za=Mathf.Lerp(b.min.z,b.max.z,i/(float)count),zb=Mathf.Lerp(b.min.z,b.max.z,(i+1f)/count);
                    var a=tr.TransformPoint(new Vector3(b.min.x,b.center.y,za));var bb=tr.TransformPoint(new Vector3(b.min.x,b.center.y,zb));
                    var c=tr.TransformPoint(new Vector3(b.max.x,b.center.y,zb));var d=tr.TransformPoint(new Vector3(b.max.x,b.center.y,za));
                    var mid=(a+c)*.5f;bool inside=s.profile.Contains(new Vector2(mid.x,mid.z),2);
                    if(inside)focus.Quad(a,bb,c,d,new Vector2(a.x,a.z)/6,new Vector2(bb.x,bb.z)/6,new Vector2(c.x,c.z)/6,new Vector2(d.x,d.z)/6);
                    else retained.Quad(a,bb,c,d,Vector2.Lerp(originalUV[0],originalUV[1],i/(float)count),Vector2.Lerp(originalUV[0],originalUV[1],(i+1f)/count),Vector2.Lerp(originalUV[3],originalUV[2],(i+1f)/count),Vector2.Lerp(originalUV[3],originalUV[2],i/(float)count));
                }
                Hide(source);focus.Create(name+" — metric asphalt",root,art,art.Asphalt(source.sharedMaterial));retained.Create(name+" — retained outside focus",root,art,source.sharedMaterial);
            }
            // The original collision slabs remain; replace only their stretched cube render with texel-consistent surfaces.
            foreach(var tr in new[]{site.station.Find("Drive-through apron"),site.compound.Find("Vehicle parking")}.Concat(network.Cast<Transform>().Where(t=>t.name=="Clear junction" && s.profile.Contains(new Vector2(t.position.x,t.position.z),8))))
            {
                if(!tr)continue;var renderer=tr.GetComponent<Renderer>();if(!renderer)continue;var b=renderer.bounds;Hide(renderer);
                var mesh=new SliceMesh();float y=b.max.y;
                var a=new Vector3(b.min.x,y,b.min.z);var bb=new Vector3(b.min.x,y,b.max.z);var c=new Vector3(b.max.x,y,b.max.z);var d=new Vector3(b.max.x,y,b.min.z);
                mesh.Quad(a,bb,c,d,new Vector2(a.x,a.z)/6,new Vector2(bb.x,bb.z)/6,new Vector2(c.x,c.z)/6,new Vector2(d.x,d.z)/6);
                mesh.Create(tr.name+" — contact surface",root,art,tr.name=="Vehicle parking"?art.Mat("GroundPath"):art.Asphalt(renderer.sharedMaterial));
            }
            // A pedestrian strip to the service entry, with flush traversable top and gaps in the planted verge.
            var paving=new SliceMesh();
            for(int i=0;i<9;i++)
            {
                float z=-75+i*1.7f;var a=new Vector3(65,.032f,z);var b=new Vector3(65,.032f,z+1.66f);var c=new Vector3(67.4f,.032f,z+1.66f);var d=new Vector3(67.4f,.032f,z);
                paving.Quad(a,b,c,d,1.5f);
            }
            paving.Create("Service pedestrian paving — flush",root,art,art.Mat("Stone"));
        }
        void Architecture()
        {
            var root=Group("02 Service workshop architecture",s.generated.transform);root.position=site.service.position;root.rotation=site.service.rotation;
            var roof=site.service.Find("Rain roof");if(!roof)throw new InvalidOperationException("Existing service roof missing.");
            Hide(roof.GetComponent<Renderer>());
            var fascia=site.service.Find("Roof fascia");if(fascia)Hide(fascia.GetComponent<Renderer>());
            const float x=5.65f,z=4.65f,eave=3.4f,ridge=5.25f;
            var roofMesh=new SliceMesh();
            roofMesh.Quad(new Vector3(-x,eave,-z),new Vector3(-x,eave,z),new Vector3(0,ridge,z),new Vector3(0,ridge,-z));
            roofMesh.Quad(new Vector3(0,ridge,-z),new Vector3(0,ridge,z),new Vector3(x,eave,z),new Vector3(x,eave,-z));
            var g=roofMesh.Create("Pitched standing-seam roof",root,art,art.Mat("RoofMetal"),true);g.AddComponent<LastSignal.WorldTime.RainRoof>();
            var timber=new SliceMesh();var trim=new SliceMesh();
            timber.Triangle(new Vector3(-5,3.3f,-4.03f),new Vector3(0,5.05f,-4.03f),new Vector3(5,3.3f,-4.03f));
            timber.Triangle(new Vector3(5,3.3f,4.03f),new Vector3(0,5.05f,4.03f),new Vector3(-5,3.3f,4.03f));
            // Gable bargeboards, fascia, rain gutters, corner boards and a restrained porch canopy.
            foreach(float end in new[]{-z,z})
            {trim.Beam(new Vector3(-x,eave,end),new Vector3(0,ridge,end),.18f,.16f);trim.Beam(new Vector3(0,ridge,end),new Vector3(x,eave,end),.18f,.16f);}
            foreach(float side in new[]{-x,x})trim.Beam(new Vector3(side,eave,-z),new Vector3(side,eave,z),.16f,.24f);
            foreach(float cx in new[]{-5.05f,5.05f})foreach(float cz in new[]{-4.05f,4.05f})trim.Box(new Vector3(cx,1.65f,cz),new Vector3(.15f,3.3f,.15f),Quaternion.identity);
            for(float xx=-4.8f;xx<5;xx+=.4f)
            {
                float h=1.7f*(1-Mathf.Abs(xx)/5);
                if(h>.05f)trim.Box(new Vector3(xx,3.3f+h*.5f,-4.065f),new Vector3(.035f,h,.04f),Quaternion.identity);
            }
            timber.Create("Timber gable infill",root,art,art.Mat("PaintedTimber"));trim.Create("Bargeboards and corner joinery",root,art,art.Mat("DarkTimber"));
            var metal=new SliceMesh();
            metal.Beam(new Vector3(0,5.28f,-4.8f),new Vector3(0,5.28f,4.8f),.24f,.08f);
            foreach(float side in new[]{-5.5f,5.5f})
            {
                metal.Beam(new Vector3(side,3.35f,-4.6f),new Vector3(side,3.35f,4.6f),.14f,.12f);
                metal.Beam(new Vector3(side,3.35f,3.8f),new Vector3(side,.18f,3.8f),.09f,.09f);
            }
            // Narrow raised seams articulate roofing at human scale without new plain roof planes.
            for(float zz=-4.3f;zz<4.4f;zz+=.65f)
            {metal.Beam(new Vector3(-5.6f,3.43f,zz),new Vector3(0,5.28f,zz),.025f,.035f);metal.Beam(new Vector3(0,5.28f,zz),new Vector3(5.6f,3.43f,zz),.025f,.035f);}
            metal.Create("Standing seams ridge cap and drainage",root,art,art.Mat("Metal"));
            // Reuse the imported awning; keep all door swings and both published entrances clear.
            var awning=art.Place("Awning",s.generated.transform,site.service.TransformPoint(new Vector3(0,2.65f,-4.55f)),3.2f,180);
            RainContact("Service awning rain contact",SliceAssets.BoundsOf(awning));
            var canopy=site.station.Find("Canopy");
            if(!canopy)throw new InvalidOperationException("Existing station canopy missing; refuse an invented replacement.");
            RainContact("Station canopy rain contact",SliceAssets.BoundsOf(canopy.gameObject));
        }
        void RainContact(string name,Bounds roof)
        {
            // A thin, invisible solid under the actual roof bounds supports upward roof queries too.
            // Single-sided imported roof triangles need not be raycastable from below.
            var g=new GameObject(name);g.transform.SetParent(s.generated.transform,false);
            g.transform.position=new Vector3(roof.center.x,roof.max.y-.08f,roof.center.z);
            g.AddComponent<BoxCollider>().size=new Vector3(Mathf.Max(.1f,roof.size.x-.4f),.12f,Mathf.Max(.1f,roof.size.z-.4f));
            g.AddComponent<LastSignal.WorldTime.RainRoof>();
        }
        void Props()
        {
            var root=Group("03 Anchored roadside stories",s.generated.transform);
            void Ground(string key,float x,float z,float size,float yaw,bool solid=false)
            {
                var p=new Vector2(x,z);if(!site.Allowed(p,size*.5f)){site.skipped.Add(key+" at "+p+" excluded by existing clearance");return;}
                var g=art.Place(key,root,new Vector3(x,site.Height(p)-.025f,z),size,yaw,solid);
                if(solid){site.solids.Add(SliceAssets.BoundsOf(g));}
            }
            Ground("Wreck",-207,-150,4.7f,28,true);
            Ground("Wreck",100,-99,4.5f,-24,true);
            Ground("Pallet",96,-67,1.5f,12);
            Ground("Tyre",98,-69,.7f,76);
            Ground("Bin",91,-61,.7f,180);
            Ground("Pallet",-205,-184,1.6f,7);
            Ground("Tyre",-207,-185,.65f,43);
            // Mounted accessories are deliberately collision-free and offset from existing wall surfaces.
            art.Place("Extinguisher",root,site.service.TransformPoint(new Vector3(1.45f,.8f,-4.14f)),.5f,0,false,true);
            art.Place("ElectricPanel",root,site.service.TransformPoint(new Vector3(5.15f,1.0f,2.2f)),.6f,90,false,true);
            // Fence fragments divide neglected yard from forecourt, leaving service and rear escape routes clear.
            Ground("Fence",102,-52,5,0,true);Ground("Fence",108,-52,5,0,true);
        }
        void Vegetation()
        {
            var canopy=Group("04 Canopy — clustered age bands",s.generated.transform);var low=Group("05 Understory and forest floor",s.generated.transform);
            var rng=new System.Random(s.profile.seed);var accepted=new List<Vector2>(site.treeCenters);var small=new List<Vector2>();
            Vector2 Point()=>new Vector2(-234+(float)rng.NextDouble()*352,-215+(float)rng.NextDouble()*182);
            void Record(string key,Vector2 p,float size,float yaw)=>digest.AppendFormat(CultureInfo.InvariantCulture,"{0}:{1:F4},{2:F4}:{3:F4}:{4:F4};",key,p.x,p.y,size,yaw);
            for(int i=0;i<22000 && s.trees<s.profile.treeBudget;i++)
            {
                var p=Point();float density=Mathf.PerlinNoise((p.x+413)*.045f,(p.y+113)*.045f);
                // Keep the station forecourt open, concentrate taller canopy at the western woodland and rear lots.
                float factor=p.x<-190?.9f:p.x>84 && p.y>-83?.55f:.16f;
                if(rng.NextDouble()>density*factor || !site.Allowed(p,3.5f) || accepted.Any(v=>(v-p).sqrMagnitude<49))continue;
                string key=rng.NextDouble()<.3?"Birch":"Pine";float h=key=="Pine"?12+(float)rng.NextDouble()*7:9+(float)rng.NextDouble()*4,yaw=(float)rng.NextDouble()*360;
                var g=art.Place(key,canopy,new Vector3(p.x,site.Height(p)-.1f,p.y),h,yaw,true,true);
                // The original tree colliders are trunk capsules; never introduce a foliage mesh collider.
                foreach(var c in g.GetComponentsInChildren<Collider>())if(!(c is CapsuleCollider))c.enabled=false;
                accepted.Add(p);s.trees++;Record(key,p,h,yaw);
            }
            for(int i=0;i<16000 && s.shrubs<s.profile.shrubBudget;i++)
            {
                var p=Point();float density=Mathf.PerlinNoise((p.x+13)*.16f,(p.y+93)*.16f);
                if(rng.NextDouble()>density*.7f || !site.Allowed(p,.8f) || small.Any(v=>(v-p).sqrMagnitude<2.2f))continue;
                float h=.35f+(float)rng.NextDouble()*.42f,yaw=(float)rng.NextDouble()*360;
                art.Place("Bush",low,new Vector3(p.x,site.Height(p)-.04f,p.y),h,yaw,false,true);small.Add(p);s.shrubs++;Record("Bush",p,h,yaw);
            }
            for(int i=0;i<8000 && s.rocks<s.profile.rockBudget;i++)
            {
                var p=Point();if(!site.Allowed(p,1.4f) || small.Any(v=>(v-p).sqrMagnitude<4))continue;
                float size=.55f+(float)rng.NextDouble()*1.4f,yaw=(float)rng.NextDouble()*360;
                var g=art.Place("Rock",low,new Vector3(p.x,site.Height(p)-.12f,p.y),size,yaw,size>1.4f);
                if(size>1.4f)site.solids.Add(SliceAssets.BoundsOf(g));small.Add(p);s.rocks++;Record("Rock",p,size,yaw);
            }
            for(int i=0;i<accepted.Count;i+=5)
            {
                var p=accepted[i]+new Vector2(3,3);if(!site.Allowed(p,2.4f))continue;
                art.Place("Log",low,new Vector3(p.x,site.Height(p)-.12f,p.y),3,37+i*41);Record("Log",p,3,37+i*41);
            }
        }
        void Signs()
        {
            var root=Group("06 Legible two-sided signs",s.generated.transform);
            // Exact existing sign ownership only; persistent interaction text and HUD remain untouched.
            foreach(var tr in new[]{site.compound.Find("Sign - FIELD STATION STORAGE / WORKSHOP"),site.service.Find("Sign - SERVICE SHOP")})
            {
                if(!tr)continue;var old=tr.GetComponentInChildren<TextMesh>();if(!old)continue;
                Hide(old.GetComponent<Renderer>());var board=tr.Find("Board");if(!board)continue;
                float width=board.localScale.x;
                foreach(int side in new[]{-1,1})
                {
                    var label=Group(tr.name+" / "+(side<0?"front":"rear"),root);label.position=tr.TransformPoint(new Vector3(0,0,side*.066f));label.rotation=tr.rotation*Quaternion.Euler(0,side<0?0:180,0);
                    var text=label.gameObject.AddComponent<TextMesh>();text.font=old.font?old.font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=.055f;
                    text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.83f,.80f,.68f);text.text=old.text;
                    var r=text.GetComponent<MeshRenderer>();r.sharedMaterial=text.font.material;r.shadowCastingMode=ShadowCastingMode.Off;
                    // Bounds in label space, independent of sign yaw; leave a 10% board margin.
                    var size=r.localBounds.size;float scale=Mathf.Min(1,width*.9f/Mathf.Max(.01f,size.x),.72f/Mathf.Max(.01f,size.y));label.localScale=Vector3.one*scale;
                }
            }
        }
    }
}
