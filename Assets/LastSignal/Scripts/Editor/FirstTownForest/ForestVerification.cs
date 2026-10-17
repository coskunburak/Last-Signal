using System;
using System.IO;
using System.Linq;
using LastSignal.Forest;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace LastSignal.Editor.FirstTownForest
{
    /// <summary>Explicit owner-run checks, never invoked by import, generation or promotion.</summary>
    public static class ForestVerification
    {
        static Action cancelNavigation;
        static ForestAuthoringState State()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit PlayMode.");
            var s=SceneManager.GetActiveScene();if(s.path!=ForestAuthoring.StageScene && s.path!=ForestAuthoring.SourceScene)throw new InvalidOperationException("Open the forest stage or FirstTownCore.");
            return ForestAuthoring.All<ForestAuthoringState>(s).Single();
        }
        [MenuItem("Last Signal/First Town/Forest/2 Validate saved stage references and clearance")]
        public static void Validate()
        {
            var state=State();var scene=state.gameObject.scene;Physics.SyncTransforms();
            if(!state.generated || !state.layout || !state.terrain || !state.baselineTerrain)throw new InvalidOperationException("Incomplete forest state.");
            var site=new ForestSite(scene,state);
            foreach(var c in state.generated.GetComponentsInChildren<Collider>(true))
            {
                if(c is MeshCollider)throw new InvalidOperationException("Generated vegetation has a MeshCollider.");
                var b=c.bounds;var p=new Vector2(b.center.x,b.center.z);
                if(!site.Allowed(p,Mathf.Max(b.extents.x,b.extents.z)))throw new InvalidOperationException("Clearance conflict: "+c.name+" at "+p);
            }
            foreach(var r in state.generated.GetComponentsInChildren<Renderer>(true))
                foreach(var m in r.sharedMaterials)ValidateMaterial(m,r.name);
            if(state.terrain.terrainData.terrainLayers.Length!=4)throw new InvalidOperationException("Expected four terrain layers.");
            ValidateMaterial(state.terrain.materialTemplate,"Terrain");
            if(state.trees>state.layout.treeLimit || state.bushes>state.layout.bushLimit || state.rocks>state.layout.rockLimit || state.grassInstances>state.layout.grassBudget)throw new InvalidOperationException("Density budget exceeded.");
            // Check every trail segment, not just its endpoints. Native collision and slope acceptance remains manual.
            foreach(var trail in state.layout.trails)for(int i=1;i<trail.points.Length;i++)
            {
                var a=trail.points[i-1];var b=trail.points[i];int steps=Mathf.CeilToInt(Vector2.Distance(a,b));
                float previous=site.Height(a);
                for(int j=1;j<=steps;j++)
                {
                    var p=Vector2.Lerp(a,b,j/(float)steps);float h=site.Height(p),grade=Mathf.Abs(h-previous)/(Vector2.Distance(a,b)/steps);
                    if(grade>Mathf.Tan((trail.vehicle?10:28)*Mathf.Deg2Rad))throw new InvalidOperationException("Trail grade too steep: "+trail.name+" "+p);
                    // Ignore roadbed/low ground colliders, detect walls, rocks, trunks, fences at body height.
                    var center=new Vector3(p.x,h+1,p.y);var half=new Vector3(trail.vehicle?2.2f:.45f,.7f,trail.vehicle?2.2f:.45f);
                    foreach(var hit in Physics.OverlapBox(center,half,Quaternion.identity,~((1<<8)|(1<<2)),QueryTriggerInteraction.Ignore))
                        if(!(hit is TerrainCollider) && hit.bounds.max.y>h+.5f)throw new InvalidOperationException("Trail body clearance blocked by "+hit.name+" on "+trail.name+" at "+p);
                    previous=h;
                }
            }
            Write("references","PASS","NOT_RUN");Debug.Log("Forest authored reference/clearance checks PASS. Runtime and visual acceptance NOT_RUN.");
        }
        [MenuItem("Last Signal/First Town/Forest/4 Validate baked forest navigation")]
        public static void Navigation()
        {
            if(cancelNavigation!=null)throw new InvalidOperationException("Forest navigation validation is already pending.");
            var state=State();var surface=ForestAuthoring.All<NavMeshSurface>(state.gameObject.scene).Single();
            if(!surface.navMeshData)throw new InvalidOperationException("Navigation has not been baked. Open production FirstTownCore, run Last Signal > First Town > 2 Bake navigation (user only), then run Forest > 4 Validate baked forest navigation. Do not regenerate or promote the forest again.");
            if(!state.generated)throw new InvalidOperationException("Forest generation is missing.");
            // Match FirstTown's baked-route check: openable doors are runtime barriers,
            // not permanent disconnections in the baked terrain. Keep every other obstacle active.
            var doors=ForestAuthoring.All<DoorInteractable>(state.gameObject.scene)
                .SelectMany(d=>d.GetComponentsInChildren<NavMeshObstacle>(true))
                .Where(o=>o.isActiveAndEnabled && o.carving).Distinct().ToArray();
            var data=surface.navMeshData;var scene=state.gameObject.scene;
            EditorApplication.CallbackFunction tick=null;
            Action<PlayModeStateChange> modeChanged=null;
            AssemblyReloadEvents.AssemblyReloadCallback reload=null;
            Action restore=null;
            restore=()=>
            {
                EditorApplication.update-=tick;
                EditorApplication.playModeStateChanged-=modeChanged;
                AssemblyReloadEvents.beforeAssemblyReload-=reload;
                EditorApplication.quitting-=restore;
                foreach(var door in doors)if(door)door.enabled=true;
                cancelNavigation=null;
                EditorApplication.QueuePlayerLoopUpdate();
            };
            modeChanged=_=>{restore();Debug.LogWarning("Forest navigation validation cancelled by PlayMode transition; door obstacles restored.");};
            reload=()=>restore();
            int updates=0;double readyAt=EditorApplication.timeSinceStartup+.25;
            tick=()=>
            {
                try
                {
                    if(!state || !surface || !scene.isLoaded || SceneManager.GetActiveScene()!=scene || surface.navMeshData!=data)
                        throw new InvalidOperationException("Scene or baked navigation changed during forest validation; run the check again.");
                    // Carving changes are applied by the native navigation update, not by
                    // Physics.SyncTransforms. Queue editor player-loop updates before querying.
                    if(++updates<3 || EditorApplication.timeSinceStartup<readyAt)
                    {EditorApplication.QueuePlayerLoopUpdate();return;}
                    CheckBakedPaths(state,surface);
                    restore();
                }
                catch(Exception error){restore();Debug.LogException(error);}
            };
            cancelNavigation=restore;
            EditorApplication.playModeStateChanged+=modeChanged;
            AssemblyReloadEvents.beforeAssemblyReload+=reload;
            EditorApplication.quitting+=restore;
            try
            {
                surface.AddData();
                foreach(var door in doors)door.enabled=false;
                EditorApplication.update+=tick;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch {restore();throw;}
        }
        static void CheckBakedPaths(ForestAuthoringState state,NavMeshSurface surface)
        {
            var filter=new NavMeshQueryFilter{agentTypeID=surface.agentTypeID,areaMask=NavMesh.AllAreas};
            var spawn=ForestAuthoring.All<Transform>(state.gameObject.scene).Single(t=>t.name=="PlayerSpawn");
            if(!NavMesh.SamplePosition(spawn.position,out var start,.7f,filter))throw new InvalidOperationException("Player spawn off navmesh.");
            foreach(var point in state.generated.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Route - forest") || t.name.StartsWith("Clearing —")))
            {
                if(!NavMesh.SamplePosition(point.position,out var target,.7f,filter))throw new InvalidOperationException("Off-mesh forest marker "+point.name+" "+point.position);
                var path=new NavMeshPath();
                if(!NavMesh.CalculatePath(start.position,target.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete)
                {
                    var corners=path.corners;var end=corners.Length>0?corners[corners.Length-1]:start.position;
                    throw new InvalidOperationException("Disconnected forest marker "+point.name+" "+point.position+
                        "; status="+path.status+"; sampled start="+start.position+"; sampled target="+target.position+
                        "; last reachable corner="+end+". Checked with openable-door carving temporarily disabled; inspect this endpoint for a terrain/collider gap.");
                }
            }
            Write("navigation","NOT_RUN","PASS");Debug.Log("Forest baked path checks PASS (openable-door carving temporarily disabled). Live door, zombie and vehicle acceptance NOT_RUN.");
        }
        static void ValidateMaterial(Material material,string owner)
        {
            if(!material)throw new InvalidOperationException("Missing material on "+owner);
            if(!material.shader)throw new InvalidOperationException("Missing shader on "+owner+": "+AssetDatabase.GetAssetPath(material));
            if(!ShaderUtil.ShaderHasError(material.shader))return;
            var messages=ShaderUtil.GetShaderMessages(material.shader);
            var details=string.Join("\n",messages.Select(m=>m.file+"("+m.line+"): "+m.message));
            throw new InvalidOperationException("Shader error on "+owner+": "+AssetDatabase.GetAssetPath(material)+"\nShader: "+material.shader.name+"\n"+details);
        }
        static void Write(string action,string refs,string nav)
        {Directory.CreateDirectory(ForestAuthoring.Evidence);File.WriteAllText(ForestAuthoring.Evidence+"/"+action+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json","{\"references\":\""+refs+"\",\"navigation\":\""+nav+"\",\"visual\":\"NOT_RUN\",\"runtime\":\"NOT_RUN\",\"performance\":\"NOT_MEASURED\"}");}
    }
}
