using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Atmosphere;
using LastSignal.VisualSlice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    /// <summary>Owner-operated, bounded edits to the existing stage. No regeneration or bake.</summary>
    public static class SliceRefinement04
    {
        internal const string Marker="07 Refinement04 — human-scale compound fence";
        const string Fence="Assets/LastSignal/Art/FirstTown/Prefabs/Fence.prefab";
        const float Height=2.4f;
        sealed class Plan
        {
            internal Transform original;
            internal Vector3 center,axis;
            internal float bottom,span;
            internal int count;
        }
        [Serializable] sealed class Change
        {
            public string objectPath;
            public Vector3 oldPosition,oldScale;
            public Quaternion oldRotation;
            public float oldHeight,newHeight;
            public Vector3 newCenter;
            public float newSpan;
            public int modules;
        }
        [Serializable] sealed class Report
        {
            public string status="APPLIED_EXISTING_STAGE",checkpoint,sourceHashBefore,sourceHashAfter,stageHashBefore,stageHashAfter;
            public string visual="NOT_VERIFIED",navigation="NOT_RUN",runtime="NOT_RUN";
            public List<Change> fences=new List<Change>();
            public Vector3 oldApproachLight,newApproachLight,oldApproachPole,newApproachPole;
        }
        [MenuItem("Last Signal/First Town/Visual Slice/2b Apply road and fence refinement (existing stage)")]
        public static void Apply()
        {
            SliceAuthoring.Guard();var scene=SceneManager.GetActiveScene();
            if(scene.path!=SliceAuthoring.Stage)throw new InvalidOperationException("Open the existing FirstTownCore_VisualSlice. Production is never edited by this command.");
            var state=SliceAuthoring.All<FirstTownSliceState>(scene).Single();
            if(!state.generated || !state.atmosphere)throw new InvalidOperationException("Saved stage dependencies missing; no edits made.");
            if(state.generated.transform.Find(Marker))
            {Debug.Log("Refinement04 already exists. Artist edits retained; nothing reapplied or regenerated.");return;}
            if(state.generated.transform.lossyScale!=Vector3.one || state.generated.transform.rotation!=Quaternion.identity)
                throw new InvalidOperationException("Generated root has an artist transform; preserve it and refit the operation.");
            var compound=SliceAuthoring.All<Transform>(scene).Single(t=>t.name=="02 Safehouse Compound");
            if(Vector3.Distance(compound.position,new Vector3(-180,0,-170))>.01f || compound.rotation!=Quaternion.identity || compound.lossyScale!=Vector3.one)
                throw new InvalidOperationException("Compound layout was moved by an artist; preserve it and refit the fence setbacks.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Fence);
            if(!prefab)throw new FileNotFoundException("Audited project-owned fence prefab missing",Fence);
            var native=MeshBounds(prefab.transform);
            if(native.size.x<.1f || native.size.y<.1f)throw new InvalidOperationException("Fence mesh bounds invalid.");
            float unit=Height/native.size.y,naturalWidth=native.size.x*unit;
            var plans=new List<Plan>();
            foreach(Transform t in compound)
            {
                if(!t.gameObject.activeSelf || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)!=Fence)continue;
                var bounds=SliceAssets.BoundsOf(t.gameObject);
                // Already proportioned/artist-authored fences are outside this repair.
                if(bounds.size.y<=4)continue;
                if(t.GetComponentsInChildren<MonoBehaviour>(true).Length!=0 || t.GetComponentsInChildren<Rigidbody>(true).Length!=0)
                    throw new InvalidOperationException("Fence has gameplay components; refusing replacement: "+t.name);
                if(Vector3.Dot(t.up,Vector3.up)<.999f)throw new InvalidOperationException("Tilted artist fence requires manual fitting: "+t.name);
                var local=MeshBounds(t);float span=local.size.x*Mathf.Abs(t.lossyScale.x);
                var center=bounds.center;var axis=t.right;
                // The original north fence was on the cross-street centerline z=-140.
                // Set it behind the southern shoulder, then trim the two adjoining side
                // runs to meet it. Keep the existing 24 m gate opening in X unchanged.
                const float northBoundary=-146;
                if(Mathf.Abs(t.localPosition.z-30)<.1f && Mathf.Abs(axis.x)>.999f)center.z=northBoundary;
                else if(Mathf.Abs(axis.z)>.999f && center.z+span*.5f>northBoundary)
                {
                    float south=center.z-span*.5f;span=northBoundary-south;
                    if(span<.5f)throw new InvalidOperationException("Unexpected fence footprint at road setback; preserve and refit.");
                    center.z=(south+northBoundary)*.5f;
                }
                plans.Add(new Plan{original=t,center=center,bottom=bounds.min.y,axis=axis,span=span,count=Mathf.Max(1,Mathf.RoundToInt(span/naturalWidth))});
            }
            if(plans.Count==0)throw new InvalidOperationException("No oversized original compound fences found; no scene edits made.");
            var d=state.atmosphere;
            var approach=d.lamps.Where(l=>l.light && l.light.name=="Gas approach").Select(l=>l.light.transform).Single();
            var pole=d.transform.Find("Gas approach pole");
            if(!pole)throw new InvalidOperationException("Gas approach pole missing; no scene edits made.");
            // Preserve manual relocation. Only move the original light/pole pair still inside the road.
            bool moveApproach=Vector3.Distance(approach.position,new Vector3(51,4.5f,-108))<.05f &&
                Mathf.Abs(pole.position.x-51)<.05f && Mathf.Abs(pole.position.z+108)<.05f;
            string folder=SliceAuthoring.Evidence+"/Refinement04";Directory.CreateDirectory(folder);
            string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var report=new Report{checkpoint=folder+"/stage-before-"+stamp+".unity",sourceHashBefore=SliceAuthoring.Hash(SliceAuthoring.Source),stageHashBefore=SliceAuthoring.Hash(scene.path),oldApproachLight=approach.position,oldApproachPole=pole.position};
            if(!EditorSceneManager.SaveScene(scene,report.checkpoint,true))throw new IOException("Could not save stage checkpoint. No edits made.");
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("FirstTown Refinement04");
            bool saved=false;
            try
            {
                var root=new GameObject(Marker);Undo.RegisterCreatedObjectUndo(root,"Refinement04 root");root.transform.SetParent(state.generated.transform,false);
                for(int k=0;k<plans.Count;k++)
                {
                    var p=plans[k];var original=p.original;
                    report.fences.Add(new Change{objectPath=PathOf(original),oldPosition=original.position,oldRotation=original.rotation,oldScale=original.localScale,oldHeight=SliceAssets.BoundsOf(original.gameObject).size.y,newHeight=Height,modules=p.count,newCenter=p.center,newSpan=p.span});
                    float width=p.span/p.count;
                    for(int i=0;i<p.count;i++)
                    {
                        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);Undo.RegisterCreatedObjectUndo(g,"Human-scale fence module");
                        g.name="Compound fence "+k.ToString("D2")+" / "+i.ToString("D2");
                        g.transform.rotation=original.rotation;
                        // Keep boards at their native height/thickness proportions; only adjust
                        // module width slightly so adjoining runs retain their original endpoints.
                        g.transform.localScale=new Vector3(width/native.size.x,unit,unit);
                        var b=SliceAssets.BoundsOf(g);var center=p.center+p.axis*(-p.span*.5f+width*(i+.5f));
                        g.transform.position+=new Vector3(center.x,p.bottom,center.z)-new Vector3(b.center.x,b.min.y,b.center.z);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
                    }
                    // Keep original IDs/objects for rollback; disable the oversized render AND
                    // collision together. Imported module colliders provide the corrected barrier.
                    Undo.RecordObject(original.gameObject,"Retain original oversized fence");original.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(original.gameObject);
                }
                if(moveApproach)
                {
                    var offset=new Vector3(5.2f,0,0); // road eastern edge x=54.5; pole center x=56.2
                    Undo.RecordObjects(new Object[]{approach,pole},"Move lamp out of driving lane");
                    approach.position+=offset;pole.position+=offset;
                    if(PrefabUtility.IsPartOfPrefabInstance(pole))PrefabUtility.RecordPrefabInstancePropertyModifications(pole);
                }
                report.newApproachLight=approach.position;report.newApproachPole=pole.position;
                if(SliceAuthoring.Hash(SliceAuthoring.Source)!=report.sourceHashBefore)throw new IOException("Source changed externally during authoring; reverting this operation.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Stage save failed; reverting the operation.");
                saved=true;Undo.CollapseUndoOperations(undo);
                report.sourceHashAfter=SliceAuthoring.Hash(SliceAuthoring.Source);report.stageHashAfter=SliceAuthoring.Hash(scene.path);
                File.WriteAllText(folder+"/authoring-"+stamp+".json",JsonUtility.ToJson(report,true));
                Debug.Log("Refinement04 saved in existing stage. "+plans.Count+" oversized fence runs replaced by human-scale modules; lamp "+(moveApproach?"moved out of road":"artist position preserved")+". Checkpoint: "+report.checkpoint+". No bake, tests, runtime or visual acceptance performed.");
            }
            catch
            {
                if(!saved)Undo.RevertAllDownToGroup(undo);
                // A report-write failure after saving must not silently roll back a valid saved scene.
                else Debug.LogError("Stage was saved, but refinement reporting failed. Preserve the stage and checkpoint: "+report.checkpoint);
                throw;
            }
        }
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        static Bounds MeshBounds(Transform root)
        {
            bool first=true;var result=new Bounds();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.sharedMesh)continue;var b=filter.sharedMesh.bounds;var matrix=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<8;i++)
                {
                    var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);
                }
            }
            if(first)throw new InvalidOperationException("No fence mesh geometry.");return result;
        }
    }
}
