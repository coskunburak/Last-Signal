// SCN-S001 explicit, one-time candidate migration. Not an import callback or regeneration menu.
// Executed through Unity RunCommand only after checkpoint and source inspection.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LastSignal;
using LastSignal.Forest;
using LastSignal.Gathering;
using LastSignal.VisualSlice;
using LastSignal.RenderingQuality;
using Object = UnityEngine.Object;
internal class CommandScript : IRunCommand
{
    const string ScenePath = "Assets/LastSignal/Scenes/Production/FirstTownMain_HDRP.unity";
    const string Root = "Assets/LastSignal/Art/FirstTownMain_HDRP/PrototypeVisuals";
    const string Docs = "Docs/Implementation/Scenes/SCN-S001/";
    Action<Object> created, modified;
    Terrain terrain;
    ForestAuthoringState forest;
    FirstTownSliceState slice;
    List<Bounds> protectedBounds = new List<Bounds>();
    StringBuilder mapping = new StringBuilder("kind\toldObject\tposition\tnewPrefab\tscale\tcollision\n");
    List<Transform> vegetation = new List<Transform>();
    Dictionary<Transform, string> roles = new Dictionary<Transform, string>();
    float[,] oldHeights;
    int treeCount, fernCount, rockCount, branchCount, heightCells;
    float maxRaise;
    public void Execute(ExecutionResult result)
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || SceneManager.sceneCount != 1 || scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Requires exactly one saved candidate scene, outside Play/compilation.");
        if (!File.Exists(Docs + "PromotionCheckpoint.json")) throw new InvalidOperationException("Checkpoint required.");
        if (scene.GetRootGameObjects().Any(g => g.name == "SCN-S001 Natural Forest")) throw new InvalidOperationException("Already migrated; do not rerun.");
        created = o => result.RegisterObjectCreation(o); modified = o => result.RegisterObjectModification(o);
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        forest = all.Select(t => t.GetComponent<ForestAuthoringState>()).Single(x => x);
        slice = all.Select(t => t.GetComponent<FirstTownSliceState>()).Single(x => x);
        terrain = forest.terrain;
        if (!AssetDatabase.GetAssetPath(terrain.terrainData).StartsWith("Assets/LastSignal/Art/FirstTownMain_HDRP/", StringComparison.Ordinal)) throw new InvalidOperationException("Terrain is not isolated.");
        foreach (string path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories).Where(p => !p.EndsWith(".meta"))) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in new[] { "Spruce1", "Spruce4", "Spruce6", "Spruce8", "Spruce9", "Birch2", "fern_02_a", "fern_02_d", "Rock", "FallenBranch", "GrassDetail" }) Prefab(name);
        var nodes = all.Select(t => t.GetComponent<ResourceNodeView>()).Where(x => x).ToArray();
        if (nodes.Any(n => n.physicsBody)) throw new InvalidOperationException("Physical authoring changed since audit; explicit migration needed.");
        var gameplay = all.SelectMany(t => t.GetComponents<MonoBehaviour>()).Where(m => m && m.GetType().Assembly.GetName().Name == "LastSignal.Runtime" && !(m is ForestAuthoringState) && !(m is ResourceNodeView)).ToDictionary(m => m, m => EditorJsonUtility.ToJson(m));
        var transforms = all.ToDictionary(t => t, t => t.localPosition.ToString("R") + t.localRotation.ToString("R") + t.localScale.ToString("R"));
        foreach (Transform group in forest.generated.transform)
        {
            string kind = new[] { "ForestEdge", "MixedWoodland", "DeepForest" }.Contains(group.name) ? "tree" :
                new[] { "GroundCover", "TrailVegetation" }.Contains(group.name) ? "fern" : group.name == "RockClusters" ? "rock" : group.name.StartsWith("Natural debris") ? "branch" : null;
            if (kind == null) continue;
            foreach (Transform t in group) { vegetation.Add(t); roles[t] = kind; }
        }
        var sliceCanopy = slice.generated.transform.Cast<Transform>().Single(t => t.name.StartsWith("04 Canopy"));
        foreach (Transform t in sliceCanopy) { vegetation.Add(t); roles[t] = "tree"; }
        var sliceFloor = slice.generated.transform.Cast<Transform>().Single(t => t.name.StartsWith("05 Understory"));
        foreach (Transform t in sliceFloor) { vegetation.Add(t); roles[t] = t.name.StartsWith("Rock") ? "rock" : t.name.StartsWith("Log") ? "branch" : "fern"; }
        foreach (var c in all.SelectMany(t => t.GetComponents<Collider>()).Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && !(c is TerrainCollider)))
            if (!vegetation.Any(v => c.transform == v || c.transform.IsChildOf(v)) && !c.GetComponentInParent<ResourceNodeView>()) protectedBounds.Add(c.bounds);
        foreach (var t in all.Where(t => t.name.StartsWith("Route -") || t.name == "PlayerSpawn" || t.GetComponent<ZombieEncounter>())) protectedBounds.Add(new Bounds(t.position, new Vector3(8, 20, 8)));
        foreach (var n in nodes) protectedBounds.Add(new Bounds(n.transform.position, new Vector3(16, 20, 16)));
        protectedBounds.Add(new Bounds(new Vector3(-180, 0, -172), new Vector3(78, 30, 64))); // Entire compound, doors and fence.
        protectedBounds.AddRange(forest.layout.artistExclusions);
        File.WriteAllText(Docs + "Evidence/promotion-gameplay-before.tsv", string.Join("\n", gameplay.Select(p => GlobalObjectId.GetGlobalObjectIdSlow(p.Key) + "\t" + p.Value)));
        SculptAndSurface();
        var parent = new GameObject("SCN-S001 Natural Forest"); created(parent);
        var ridge = Child("Weathered outcrops — owner navigation bake pending", parent.transform);
        var floor = Child("Fern colonies and fallen branches", parent.transform);
        int index = 0;
        foreach (var t in vegetation)
        {
            if (!t.gameObject.activeInHierarchy || t.GetComponentInParent<ResourceNodeView>() || t.GetComponentInChildren<ResourceNodeView>(true)) continue;
            string kind = roles[t];
            string name = kind == "tree" ? (t.name.StartsWith("Birch") ? "Birch2" : new[] { "Spruce1", "Spruce4", "Spruce6", "Spruce8", "Spruce9" }[index % 5]) : kind == "fern" ? (index % 2 == 0 ? "fern_02_a" : "fern_02_d") : kind == "rock" ? "Rock" : "FallenBranch";
            var oldRenderers = t.GetComponentsInChildren<Renderer>(true); if (oldRenderers.Length == 0) continue;
            var b = BoundsOf(oldRenderers); var source = Prefab(name); var nb = BoundsOf(source.GetComponentsInChildren<Renderer>(true));
            float scale = kind == "tree" ? Mathf.Clamp(b.size.y / Mathf.Max(.1f, nb.size.y), .7f, 1.25f) : kind == "fern" ? (1.5f + (index % 5) * .18f) : kind == "rock" ? Mathf.Clamp(b.size.y / Mathf.Max(.1f, nb.size.y), .3f, 2.2f) : Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) / Mathf.Max(nb.size.x, nb.size.z), .7f, 2f);
            HideOld(t, true);
            var pos = t.position; pos.y += HeightDelta(pos);
            var g = Place(source, t, pos, t.eulerAngles.y, scale, true);
            g.name = "HDRP " + name;
            if (kind == "tree") { Shadow(t.gameObject); treeCount++; }
            else if (kind == "fern") fernCount++; else if (kind == "rock") rockCount++; else branchCount++;
            mapping.AppendLine(kind + "\t" + GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject) + "\t" + t.position.ToString("F4") + "\t" + AssetDatabase.GetAssetPath(source) + "\t" + scale.ToString("R") + "\tdecorative old colliders disabled; prototype collider used");
            index++;
        }
        foreach (var n in nodes)
        {
            var old = n.standingVisual; if (!old) throw new InvalidOperationException("Missing harvest visual");
            var b = BoundsOf(old.GetComponentsInChildren<Renderer>(true)); string name = old.name.StartsWith("Birch") ? "Birch2" : "Spruce6";
            var source = Prefab(name); float scale = Mathf.Clamp(b.size.y / BoundsOf(source.GetComponentsInChildren<Renderer>(true)).size.y, .7f, 1.25f);
            // Only replace visual representation. Resource retry sphere, entity pose, IDs, authority and physics reference stay intact.
            HideOld(old.transform, false);
            var g = Place(source, n.transform, n.transform.position, old.transform.eulerAngles.y, scale, false);
            g.name = "HDRP standing visual — physics authoring pending";
            modified(n); n.standingVisual = g; PrefabUtility.RecordPrefabInstancePropertyModifications(n);
            Shadow(n.gameObject); treeCount++;
            mapping.AppendLine("resource_visual\t" + n.nodeId + "\t" + n.transform.position.ToString("F4") + "\t" + AssetDatabase.GetAssetPath(source) + "\t" + scale.ToString("R") + "\tretry collider and old physical proxy preserved; new visual collider disabled; PhysicsNotReady unchanged");
        }
        // Full authored forest registration: both historical forest and VisualSlice canopy. No second settings authority.
        modified(forest);
        forest.treeShadowRoots = forest.treeShadowRoots.Concat(new[] { sliceCanopy }).Distinct().ToArray();
        // Deliberate clusters, kept away from the road, compound and harvest clearance.
        Vector2[] crests = { new Vector2(-236,-112), new Vector2(-232,-91), new Vector2(-231,-157), new Vector2(-224,-82) };
        int addedRocks = 0, addedFerns = 0, addedBranches = 0;
        for (int c = 0; c < crests.Length; c++) for (int i = 0; i < 7; i++)
        {
            float angle = i * 2.399963f + c, radius = 1.4f + i * .8f; Vector2 p = crests[c] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (Clearance(p) < 4 || forest.layout.TrailDistance(p) < 4 || !InFocus(p)) continue;
            float scale = 1.4f + (i % 3) * .55f;
            var g = Place(Prefab("Rock"), ridge, Ground(p) - Vector3.up * scale * .24f, angle * Mathf.Rad2Deg, scale, true);
            g.name = "Weathered rock " + addedRocks; g.transform.rotation = Quaternion.Euler(12 + i * 7, angle * Mathf.Rad2Deg, (i % 3 - 1) * 14); addedRocks++;
        }
        var rng = new System.Random(261023);
        foreach (var anchor in vegetation.Where(t => roles[t] == "tree" && t.gameObject.activeInHierarchy && InFocus(new Vector2(t.position.x,t.position.z))).Take(45))
        {
            for (int i = 0; i < 5; i++)
            {
                float a = (float)rng.NextDouble() * 6.283185f, r = 2 + (float)rng.NextDouble() * 3;
                var p = new Vector2(anchor.position.x + Mathf.Cos(a)*r,anchor.position.z + Mathf.Sin(a)*r);
                if (!InFocus(p) || Clearance(p) < 1.5f || forest.layout.TrailDistance(p) < 1.7f) continue;
                var g = Place(Prefab(i % 2 == 0 ? "fern_02_a" : "fern_02_d"), floor, Ground(p)-Vector3.up*.015f, a*Mathf.Rad2Deg, 1.2f+(float)rng.NextDouble()*.8f, false);g.name="Fern colony";addedFerns++;
            }
            if (addedBranches < 12)
            {
                var p = new Vector2(anchor.position.x + 3,anchor.position.z + 2);
                if (InFocus(p) && Clearance(p) > 3 && forest.layout.TrailDistance(p) > 3)
                {Place(Prefab("FallenBranch"), floor, Ground(p), (float)rng.NextDouble()*360, 1.2f, false);addedBranches++;}
            }
        }
        foreach (var pair in gameplay) if (!pair.Key || EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("Gameplay component changed: " + pair.Key);
        foreach (var pair in transforms) if (!pair.Key || pair.Key.localPosition.ToString("R")+pair.Key.localRotation.ToString("R")+pair.Key.localScale.ToString("R") != pair.Value) throw new InvalidOperationException("Original Transform changed: " + pair.Key);
        terrain.Flush(); EditorUtility.SetDirty(terrain.terrainData); AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Candidate scene save failed; inspect in-memory edits.");
        File.WriteAllText(Docs + "PrototypeObjectMapping.tsv", mapping.ToString());
        var report = "IMPLEMENTED_OWNER_ACCEPTANCE_PENDING\nTrees="+treeCount+" fernsReplaced="+fernCount+" rocksReplaced="+rockCount+" branchesReplaced="+branchCount+" addedRockOutcrops="+addedRocks+" addedFerns="+addedFerns+" addedBranches="+addedBranches+"\nRaisedHeightCells="+heightCells+" maxRaiseMeters="+maxRaise.ToString("F4")+"\nOriginalTransformsPreserved="+transforms.Count+" unchangedGameplayComponents="+gameplay.Count+"\nSource prototype and historical assets not modified. Existing scene coordinates and road/compound/harvest clearances protected.\nGraphics authority=SessionFlow -> GraphicsSettingsManager / existing SessionUi GraphicsSettingsPresenter. Low/Medium/High/Ultra + Global/Tree independent settings.\nTerrain now needs owner NavMesh bake after visual/collision approval. No test, Play, regeneration menu, benchmark or bake executed.\n";
        File.WriteAllText(Docs + "PrototypePromotionReport.txt",report);result.Log(report);
    }
    Transform Child(string name, Transform parent) { var g=new GameObject(name);created(g);g.transform.SetParent(parent,false);return g.transform; }
    GameObject Prefab(string name) { var p=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab");if(!p)throw new InvalidOperationException("Missing "+name);return p; }
    Bounds BoundsOf(Renderer[] rs) { if(rs.Length==0)throw new InvalidOperationException("No renderers");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b; }
    void HideOld(Transform root, bool colliders)
    {
        foreach(var r in root.GetComponentsInChildren<Renderer>(true)){modified(r);r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
        foreach(var l in root.GetComponentsInChildren<LODGroup>(true)){modified(l);l.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(l);}
        if(colliders)foreach(var c in root.GetComponentsInChildren<Collider>(true)){modified(c);c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    }
    GameObject Place(GameObject prefab, Transform parent, Vector3 pos, float yaw, float scale, bool collisions)
    {
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);created(g);
        g.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));var s=parent.lossyScale;g.transform.localScale=new Vector3(scale/s.x,scale/s.y,scale/s.z);
        foreach(var c in g.GetComponentsInChildren<Collider>(true))if(!collisions){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);return g;
    }
    void Shadow(GameObject g){if(!g.GetComponent<VegetationShadowController>()){var c=g.AddComponent<VegetationShadowController>();created(c);}}
    bool InFocus(Vector2 p)=>p.x>=-240 && p.x<=-120 && p.y>=-185 && p.y<=-65;
    float Distance(Vector2 p, Bounds b)=>new Vector2(Mathf.Max(b.min.x-p.x,0,p.x-b.max.x),Mathf.Max(b.min.z-p.y,0,p.y-b.max.z)).magnitude;
    float Clearance(Vector2 p){float d=1000;foreach(var b in protectedBounds)d=Mathf.Min(d,Distance(p,b));return d;}
    Vector3 Ground(Vector2 p)=>new Vector3(p.x,terrain.SampleHeight(new Vector3(p.x,0,p.y))+terrain.transform.position.y,p.y);
    float HeightDelta(Vector3 p)
    {
        var d=terrain.terrainData;float u=(p.x-terrain.transform.position.x)/d.size.x, v=(p.z-terrain.transform.position.z)/d.size.z;
        float x=Mathf.Clamp01(u)*(oldHeights.GetLength(1)-1),z=Mathf.Clamp01(v)*(oldHeights.GetLength(0)-1);int ix=Mathf.Min((int)x,oldHeights.GetLength(1)-2), iz=Mathf.Min((int)z,oldHeights.GetLength(0)-2);
        float old=Mathf.Lerp(Mathf.Lerp(oldHeights[iz,ix],oldHeights[iz,ix+1],x-ix),Mathf.Lerp(oldHeights[iz+1,ix],oldHeights[iz+1,ix+1],x-ix),z-iz)*d.size.y+terrain.transform.position.y;
        return Ground(new Vector2(p.x,p.z)).y-old;
    }
    void SculptAndSurface()
    {
        var d=terrain.terrainData;int n=d.heightmapResolution;oldHeights=d.GetHeights(0,0,n,n);var h=(float[,])oldHeights.Clone();
        modified(d);modified(terrain);
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {
            var p=new Vector2(terrain.transform.position.x+x*d.size.x/(n-1),terrain.transform.position.z+z*d.size.z/(n-1));
            if(!InFocus(p))continue;
            float clearance=Clearance(p);if(clearance<=4 || forest.layout.TrailDistance(p)<=3)continue;
            float edge=Mathf.Min(p.x+240,-120-p.x,p.y+185,-65-p.y);
            float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/8))*Mathf.SmoothStep(0,1,Mathf.Clamp01((clearance-4)/9))*Mathf.SmoothStep(0,1,Mathf.Clamp01((forest.layout.TrailDistance(p)-3)/7));
            float a=Ridge(p,new Vector2(-234,-111),new Vector2(11,25))*5.8f+Ridge(p,new Vector2(-229,-82),new Vector2(15,14))*3.4f+Ridge(p,new Vector2(-230,-156),new Vector2(10,19))*3.8f;
            float raise=a*blend*(.78f+.22f*Mathf.PerlinNoise((p.x+400)*.09f,(p.y+400)*.09f));
            h[z,x]=Mathf.Min(.97f,h[z,x]+raise/d.size.y);float actual=(h[z,x]-oldHeights[z,x])*d.size.y;
            if(actual>.001f){heightCells++;maxRaise=Mathf.Max(maxRaise,actual);}
        }
        d.SetHeights(0,0,h);
        var weights=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);if(weights.GetLength(2)!=4)throw new InvalidOperationException("Expected audited four-layer terrain.");
        // Preserve layer meaning: old grass -> prototype grass, old soil -> woodland, road -> soil, rock -> rock.
        d.terrainLayers=new[]{"grass","Woodland","soil","rock"}.Select(name=>AssetDatabase.LoadAssetAtPath<TerrainLayer>(Root+"/"+name+".terrainlayer")).ToArray();
        for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++)
        {
            var p=new Vector2(terrain.transform.position.x+x*d.size.x/(d.alphamapWidth-1),terrain.transform.position.z+z*d.size.z/(d.alphamapHeight-1));
            if(!InFocus(p))continue;float edge=Mathf.Min(p.x+240,-120-p.x,p.y+185,-65-p.y);float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/7))*Mathf.SmoothStep(0,1,Mathf.Clamp01((Clearance(p)-1)/6));
            float woodland=blend*Mathf.Clamp01(forest.layout.Density(p)*2+.2f)*.8f;float transfer=weights[z,x,0]*woodland;weights[z,x,0]-=transfer;weights[z,x,1]+=transfer;
            float slope=d.GetSteepness((p.x-terrain.transform.position.x)/d.size.x,(p.y-terrain.transform.position.z)/d.size.z);
            float rock=blend*Mathf.InverseLerp(18,40,slope)*.65f;float fromSoil=weights[z,x,1]*rock;weights[z,x,1]-=fromSoil;weights[z,x,3]+=fromSoil;
        }
        d.SetAlphamaps(0,0,weights);terrain.materialTemplate=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Terrain.mat");
        var details=d.detailPrototypes;foreach(var detail in details){detail.prototype=Prefab("GrassDetail");detail.usePrototypeMesh=true;detail.useInstancing=true;detail.renderMode=DetailRenderMode.VertexLit;detail.minWidth=.65f;detail.maxWidth=1.2f;detail.minHeight=.65f;detail.maxHeight=1.3f;detail.healthyColor=detail.dryColor=Color.white;}d.detailPrototypes=details;
        // Authored density unchanged; only render source and width/height interpretation updated.
        terrain.drawInstanced=true;terrain.detailObjectDistance=38;terrain.basemapDistance=100;
    }
    float Ridge(Vector2 p,Vector2 center,Vector2 radius){var v=p-center;float q=v.x*v.x/(radius.x*radius.x)+v.y*v.y/(radius.y*radius.y);return Mathf.Exp(-q*1.6f);}
}
