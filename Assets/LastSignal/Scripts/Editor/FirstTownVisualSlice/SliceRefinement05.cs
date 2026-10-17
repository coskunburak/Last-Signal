using System;
using System.IO;
using System.Linq;
using LastSignal.Atmosphere;
using LastSignal.VisualSlice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    /// <summary>Owner-operated canopy repair in the already saved stage. No scene regeneration.</summary>
    public static class SliceRefinement05
    {
        const string Marker="08 Refinement05 — weatherproof station canopy";
        const string Art="Assets/LastSignal/Art/FirstTownVisualSlice/Refinement05";
        [Serializable] sealed class Report
        {
            public string status="APPLIED_EXISTING_STAGE",checkpoint,sourceHashBefore,sourceHashAfter,stageHashBefore,stageHashAfter;
            public string visual="NOT_VERIFIED",navigation="NOT_RUN",runtime="NOT_RUN";
            public Bounds vendorRoofBounds;public Vector3 approachPoleBefore,approachPoleAfter;
            public int mountedLights;
        }
        [MenuItem("Last Signal/First Town/Visual Slice/2c Repair station canopy and lighting (existing stage)")]
        public static void Apply()
        {
            SliceAuthoring.Guard();var scene=SceneManager.GetActiveScene();
            if(scene.path!=SliceAuthoring.Stage)throw new InvalidOperationException("Open the existing FirstTownCore_VisualSlice. This command never edits production.");
            var state=SliceAuthoring.All<FirstTownSliceState>(scene).Single();
            if(!state.generated || !state.generated.transform.Find(SliceRefinement04.Marker))
                throw new InvalidOperationException("The saved Refinement04 stage is required. Nothing was changed.");
            if(state.generated.transform.Find(Marker))
            {Debug.Log("Refinement05 already saved. Artist changes retained; nothing reapplied.");return;}
            if(state.generated.transform.lossyScale!=Vector3.one || state.generated.transform.rotation!=Quaternion.identity)
                throw new InvalidOperationException("Generated root has an artist transform; refit the canopy operation manually.");
            var gas=SliceAuthoring.All<Transform>(scene).Single(t=>t.name=="04 Gas Station");
            var canopy=gas.Find("Canopy");
            if(!canopy || Vector3.Distance(canopy.position,new Vector3(72,0,-83))>.05f)
                throw new InvalidOperationException("Station canopy was moved or removed by an artist; preserve it and refit the operation.");
            var roofs=canopy.GetComponentsInChildren<BoxCollider>(true)
                .Where(c=>c.size.x*c.size.z>40 && c.center.y>4).ToArray();
            if(roofs.Length!=1)throw new InvalidOperationException("Expected one imported canopy roof collider; no invented geometry applied.");
            var roof=roofs[0].bounds;
            if(roof.size.x<5 || roof.size.x>9 || roof.size.z<18 || roof.size.z>25 || roof.min.y<7 || roof.max.y>10)
                throw new InvalidOperationException("Vendor canopy roof dimensions changed; preserve asset and refit manually: "+roof);
            var director=state.atmosphere;
            if(!director || !director.lightingProfile)throw new InvalidOperationException("Stage lighting dependencies missing.");
            if(director.lamps.Any(l=>l.light && l.light.name.StartsWith("Station canopy bay ",StringComparison.Ordinal)))
                throw new InvalidOperationException("Canopy lights already exist outside Refinement05 marker; preserve artist work.");
            var pole=director.transform.Find("Gas approach pole");
            var approach=director.lamps.Where(l=>l.light && l.light.name=="Gas approach").Select(l=>l.light.transform).Single();
            if(!pole)throw new InvalidOperationException("Gas approach pole missing.");
            bool movePole=Vector3.Distance(pole.position,new Vector3(56.2f,0,-108))<.05f &&
                Vector3.Distance(approach.position,new Vector3(56.2f,4.5f,-108))<.05f;
            foreach(string path in new[]{Art+"/CanopySheet.asset",Art+"/CanopyMetalwork.asset",Art+"/CanopyLens.asset",Art+"/CanopyLens.mat"})
                if(File.Exists(path) || File.Exists(path+".meta"))throw new IOException("Partial/artist refinement asset exists; preserve and inspect: "+path);
            var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Art/S013/Production/Materials/RoofMetal.mat");
            var trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Art/S013/Production/Materials/Metal.mat");
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!metal || !trim || !shader)throw new InvalidOperationException("Project-owned roof materials or URP shader missing.");
            string folder=SliceAuthoring.Evidence+"/Refinement05";Directory.CreateDirectory(folder);
            string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var report=new Report{checkpoint=folder+"/stage-before-"+stamp+".unity",sourceHashBefore=SliceAuthoring.Hash(SliceAuthoring.Source),stageHashBefore=SliceAuthoring.Hash(scene.path),vendorRoofBounds=roof,approachPoleBefore=pole.position};
            if(!EditorSceneManager.SaveScene(scene,report.checkpoint,true))throw new IOException("Could not save stage checkpoint. No edits made.");
            SliceAuthoring.Folder(Art);
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("FirstTown Refinement05");
            bool saved=false;
            try
            {
                var root=new GameObject(Marker);Undo.RegisterCreatedObjectUndo(root,"Refinement05 root");root.transform.SetParent(state.generated.transform,false);
                // The vendor's perforated support geometry and colliders remain. Fit a
                // weatherproof sheet to its measured roof collider, with overhang and
                // separate fascia, standing seams, gutters and mounted bay fixtures.
                float x0=roof.min.x-.18f,x1=roof.max.x+.18f,z0=roof.min.z-.20f,z1=roof.max.z+.20f;
                float y=roof.max.y+.035f;
                var sheet=new SliceMesh();sheet.Box(new Vector3((x0+x1)*.5f,y-.06f,(z0+z1)*.5f),new Vector3(x1-x0,.12f,z1-z0),Quaternion.identity);
                var sheetGO=sheet.CreateOwned("Continuous standing-seam canopy sheet",root.transform,Art+"/CanopySheet.asset",metal);
                Undo.RegisterCreatedObjectUndo(sheetGO,"Canopy sheet");
                var details=new SliceMesh();
                // An aged, deep fascia conceals the vendor lattice from first-person views.
                details.Box(new Vector3((x0+x1)*.5f,y-.34f,z0),new Vector3(x1-x0,.64f,.21f),Quaternion.identity);
                details.Box(new Vector3((x0+x1)*.5f,y-.34f,z1),new Vector3(x1-x0,.64f,.21f),Quaternion.identity);
                foreach(float x in new[]{x0,x1})
                {
                    details.Box(new Vector3(x,y-.34f,(z0+z1)*.5f),new Vector3(.18f,.64f,z1-z0),Quaternion.identity);
                    details.Beam(new Vector3(x,y-.70f,z0),new Vector3(x,y-.70f,z1),.12f,.10f);
                }
                for(float x=x0+.46f;x<x1-.2f;x+=.56f)
                    details.Beam(new Vector3(x,y+.018f,z0+.13f),new Vector3(x,y+.018f,z1-.13f),.025f,.035f);
                var detailGO=details.CreateOwned("Fascia gutters and raised roof seams",root.transform,Art+"/CanopyMetalwork.asset",trim);
                Undo.RegisterCreatedObjectUndo(detailGO,"Canopy metalwork");
                var lensMesh=new SliceMesh();lensMesh.Box(Vector3.zero,new Vector3(1.8f,.075f,.32f),Quaternion.identity);
                var firstLens=lensMesh.CreateOwned("CanopyLensMesh",root.transform,Art+"/CanopyLens.asset",trim);
                var lens=firstLens.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(firstLens);
                var lensMat=new Material(shader){name="Station canopy warm lens",enableInstancing=true};
                lensMat.SetColor("_BaseColor",new Color(.50f,.43f,.31f));lensMat.SetColor("_EmissionColor",new Color(1,.68f,.35f)*1.3f);lensMat.EnableKeyword("_EMISSION");
                AssetDatabase.CreateAsset(lensMat,Art+"/CanopyLens.mat");
                var lamps=director.lamps.ToList();var practicals=director.lightingProfile.practicals.ToList();
                for(int i=0;i<2;i++)
                {
                    string name="Station canopy bay "+(i+1);
                    var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Canopy bay light");go.transform.SetParent(root.transform,false);
                    go.transform.position=new Vector3(roof.center.x,roof.min.y-.18f,Mathf.Lerp(z0,z1,i==0?.27f:.73f));
                    go.transform.rotation=Quaternion.Euler(90,0,0);
                    var light=Undo.AddComponent<Light>(go);light.type=LightType.Spot;light.spotAngle=115;light.innerSpotAngle=75;
                    light.range=16;light.intensity=10;light.color=new Color(1,.78f,.53f);light.shadows=LightShadows.None;
                    var fixture=new GameObject("Fixture");Undo.RegisterCreatedObjectUndo(fixture,"Mounted lens");fixture.transform.SetParent(go.transform,false);
                    fixture.transform.localPosition=new Vector3(0,0,0);fixture.transform.localRotation=Quaternion.Euler(-90,0,0);
                    fixture.AddComponent<MeshFilter>().sharedMesh=lens;
                    var renderer=fixture.AddComponent<MeshRenderer>();renderer.sharedMaterial=lensMat;renderer.shadowCastingMode=ShadowCastingMode.Off;
                    lamps.Add(new TownAtmosphere.Lamp{light=light,intensity=10,interior=false,shadows=false});
                    practicals.Add(new TownLightingProfile.Practical{fixtureName=name,color=light.color,gain=1.2f,priority=2.5f,shadows=false,levels=new Vector4(0,.35f,1,1)});
                    report.mountedLights++;
                }
                Undo.RecordObject(director,"Add bounded canopy practicals");director.lamps=lamps.ToArray();EditorUtility.SetDirty(director);
                Undo.RecordObject(director.lightingProfile,"Add bounded canopy light calibration");director.lightingProfile.practicals=practicals.ToArray();EditorUtility.SetDirty(director.lightingProfile);
                if(movePole)
                {
                    Undo.RecordObjects(new UnityEngine.Object[]{pole,approach},"Place approach light beside forecourt");
                    pole.position+=new Vector3(0,0,10);approach.position+=new Vector3(0,0,10);
                    if(PrefabUtility.IsPartOfPrefabInstance(pole))PrefabUtility.RecordPrefabInstancePropertyModifications(pole);
                }
                report.approachPoleAfter=pole.position;
                if(SliceAuthoring.Hash(SliceAuthoring.Source)!=report.sourceHashBefore)throw new IOException("Source scene changed externally; reverting this operation.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Stage save failed; reverting this operation.");
                saved=true;AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(undo);
                report.sourceHashAfter=SliceAuthoring.Hash(SliceAuthoring.Source);report.stageHashAfter=SliceAuthoring.Hash(scene.path);
                File.WriteAllText(folder+"/authoring-"+stamp+".json",JsonUtility.ToJson(report,true));
                Debug.Log("Refinement05 saved to existing stage. Weatherproof canopy roof, metal detailing and two mounted lights authored; approach pole "+(movePole?"repositioned":"artist position preserved")+". No bake, runtime test or visual acceptance was run. Checkpoint: "+report.checkpoint);
            }
            catch
            {
                if(!saved)
                {
                    Undo.RevertAllDownToGroup(undo);
                    foreach(string path in new[]{Art+"/CanopySheet.asset",Art+"/CanopyMetalwork.asset",Art+"/CanopyLens.asset",Art+"/CanopyLens.mat"})
                        if(AssetDatabase.LoadMainAssetAtPath(path))AssetDatabase.DeleteAsset(path);
                }
                else Debug.LogError("Stage was saved but Refinement05 reporting failed. Preserve stage and checkpoint: "+report.checkpoint);
                throw;
            }
        }
    }
}
