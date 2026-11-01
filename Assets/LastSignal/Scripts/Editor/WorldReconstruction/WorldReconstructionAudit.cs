using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastSignal.Forest;
using LastSignal.Gathering;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.Vehicles;
using LastSignal.VisualSlice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.WorldReconstruction
{
    [Serializable] public sealed class ProtectedWorldObject
    {
        public string category, identity, objectId, hierarchy;
        public Vector3 position;
    }
    [Serializable] public sealed class WorldPlanReport
    {
        public string status="OWNER_ACCEPTANCE_PENDING", sourceScene, sourceHash, terrainGuid, terrainHash, planHash, generatorVersion;
        public string stageGeneration="NOT_RUN", gameplay="NOT_RUN", performance="NOT_RUN", previewSummary="NOT_RUN";
        public bool sourceHasUnsavedChanges;
        public int seed, protectedBounds, perimeterCandidates, sessionFlows, missingScripts, missingTreePhysics;
        public float sourceMinHeight, sourceMaxHeight;
        public Bounds vehicleResidentBounds;
        public string[] blockers;
        public ProtectedWorldObject[] protectedObjects;
        public string[] perimeterObjectIds;
    }
    public sealed class WorldPlanSurvey
    {
        public WorldPlanReport report;
        public Terrain terrain;
        public ProtectionIndex protections;
        public float[,] originalHeights;
    }
    public static class WorldReconstructionAudit
    {
        public static string Hash(string path)
        {
            using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        public static WorldPlanSurvey Inspect(WorldReconstructionProfile p, bool requireSaved = true)
            => InspectBaseline(p, requireSaved, false);

        internal static WorldPlanSurvey InspectAcceptedCandidate(WorldReconstructionProfile p)
        {
            WorldStageAuthoring.ValidateSavedCandidate();
            return InspectBaseline(p, true, true);
        }

        static WorldPlanSurvey InspectBaseline(WorldReconstructionProfile p, bool requireSaved, bool candidate)
        {
            p.Validate();var s=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||PrefabStageUtility.GetCurrentPrefabStage()!=null||
                SceneManager.sceneCount!=1||s.path!=(candidate?p.proposedStage:p.sourceScene)||(requireSaved&&s.isDirty))
                throw new InvalidOperationException("Expected one FirstTownMain_HDRP source outside Play/prefab/compilation. Preview requires a saved scene. Current="+s.path+", dirty="+s.isDirty+", play="+EditorApplication.isPlaying+". No automatic save or scene switching.");
            if(!candidate&&(AssetDatabase.AssetPathToGUID(s.path)!=p.sourceSceneGuid||Hash(s.path)!=p.sourceSceneSha256))
                throw new InvalidOperationException("Source scene differs from the G0 receipt. Reconcile owner edits; do not overwrite or silently refresh the receipt.");
            var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var terrain=all.SelectMany(t=>t.GetComponents<Terrain>()).Single();var data=terrain.terrainData;
            string terrainPath=AssetDatabase.GetAssetPath(data);
            if((!candidate&&(AssetDatabase.AssetPathToGUID(terrainPath)!=p.sourceTerrainGuid||Hash(terrainPath)!=p.sourceTerrainSha256))||EditorUtility.IsDirty(data)||
                terrain.transform.position!=p.sourceOrigin||terrain.transform.rotation!=Quaternion.identity||terrain.transform.lossyScale!=Vector3.one||
                data.size!=p.sourceSize||data.heightmapResolution!=p.heightmapResolution||terrain.GetComponent<TerrainCollider>()?.terrainData!=data)
                throw new InvalidOperationException("Terrain source, unsaved state, transform, sampling or collider differs from G0.");
            var forest=all.SelectMany(t=>t.GetComponents<ForestAuthoringState>()).Single();
            var slice=all.SelectMany(t=>t.GetComponents<FirstTownSliceState>()).Single();
            var excluded=new List<Transform>();
            if(forest.generated)excluded.Add(forest.generated.transform);
            if(forest.legacyForest)excluded.Add(forest.legacyForest.transform);
            if(slice.generated)foreach(Transform group in slice.generated.transform)
                if(group.name.StartsWith("04 Canopy",StringComparison.Ordinal)||group.name.StartsWith("05 Understory",StringComparison.Ordinal))excluded.Add(group);
            // This exact generated root is recorded by SCN-S001; unknown roots remain conservatively protected.
            excluded.AddRange(s.GetRootGameObjects().Where(g=>g.name=="SCN-S001 Natural Forest").Select(g=>g.transform));
            bool Decorative(Transform t)=>!(forest.heroTrees&&t.IsChildOf(forest.heroTrees))&&!t.GetComponentInParent<ResourceNodeView>()&&excluded.Any(e=>t==e||t.IsChildOf(e));
            var index=new ProtectionIndex(p.protectionBlendMeters);
            foreach(var c in all.SelectMany(t=>t.GetComponents<Collider>()))
                if(c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&!Decorative(c.transform))index.Add(c.bounds,2);
            if(forest.layout)foreach(var bound in forest.layout.artistExclusions)index.Add(bound,2);
            if(forest.heroTrees)foreach(var r in forest.heroTrees.GetComponentsInChildren<Renderer>(true))index.Add(r.bounds,3);
            if(slice.artistContent)foreach(var r in slice.artistContent.GetComponentsInChildren<Renderer>(true))index.Add(r.bounds,3);
            var entries=new List<ProtectedWorldObject>();var keys=new HashSet<string>(StringComparer.Ordinal);var blockers=new List<string>();
            void Protect(Transform t,string category,string id,float radius)
            {
                index.Add(new Bounds(t.position,new Vector3(radius*2,20,radius*2)));
                var names=new List<string>();for(var a=t;a;a=a.parent)names.Add(a.name);names.Reverse();
                entries.Add(new ProtectedWorldObject{category=category,identity=id,objectId=GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString(),hierarchy=string.Join("/",names),position=t.position});
                if(!string.IsNullOrEmpty(id)&&!keys.Add(category+":"+id))blockers.Add("Duplicate "+category+" identity: "+id);
            }
            foreach(var t in all)
            {
                foreach(var id in t.GetComponents<PersistentEntityId>())Protect(t,"persistent",id.Id,3);
                foreach(var loot in t.GetComponents<LootSpawnPoint>())Protect(t,"loot",loot.StableId,3);
                foreach(var n in t.GetComponents<ResourceNodeView>())Protect(t,"harvest",n.nodeId,8);
                if(t.name=="PlayerSpawn"||t.name.StartsWith("Route -",StringComparison.Ordinal)||t.GetComponent<ZombieEncounter>())Protect(t,"route_or_spawn",null,4);
            }
            var vehicle=all.SelectMany(t=>t.GetComponents<VehicleWorld>()).Single();var so=new SerializedObject(vehicle);
            var resident=so.FindProperty("residentBounds").boundsValue;var spawn=so.FindProperty("spawnPosition").vector3Value;
            index.Add(new Bounds(spawn,new Vector3(18,20,18)));
            entries.Add(new ProtectedWorldObject{category="vehicle_spawn",position=spawn,identity="existing resident pickup",objectId=GlobalObjectId.GetGlobalObjectIdSlow(vehicle).ToString(),hierarchy=vehicle.name});
            if(resident.min.x>p.extension.minX||resident.max.x<p.extension.maxX||resident.min.z>p.extension.minZ||resident.max.z<p.extension.maxZ)
                blockers.Add("EXPANSION_OWNERSHIP: Source VehicleWorld.residentBounds excludes the west tile. Owner-approved G2 command configures bounds only in the isolated candidate; source remains unchanged. G3 terrain and restore acceptance still pending.");
            if(!candidate&&File.Exists(p.proposedStage))blockers.Add("STAGE_ALREADY_EXISTS: reconcile it before any later create operation.");
            var perimeter=all.Where(t=>t.parent&&t.parent.name=="00 Landscape"&&
                (Mathf.Abs(Mathf.Abs(t.position.x)-245)<.01f||Mathf.Abs(Mathf.Abs(t.position.z)-245)<.01f)).ToArray();
            var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);float lo=float.MaxValue,hi=float.MinValue;
            foreach(var value in heights){float world=p.sourceOrigin.y+value*p.sourceSize.y;lo=Mathf.Min(lo,world);hi=Mathf.Max(hi,world);}
            int physicsMissing=all.SelectMany(t=>t.GetComponents<ResourceNodeView>()).Count(n=>!n.physicsBody);
            if(physicsMissing>0)blockers.Add("HARVEST_BASELINE: "+physicsMissing+" resource roots have no physicsBody; not a terrain regression or working tree-cutting proof.");
            blockers.Add("NAVIGATION: existing NavMesh covers the old terrain; expansion and changed slopes require owner bake and route acceptance.");
            var report=new WorldPlanReport{sourceHasUnsavedChanges=s.isDirty,sourceScene=s.path,sourceHash=Hash(s.path),terrainGuid=AssetDatabase.AssetPathToGUID(terrainPath),terrainHash=Hash(terrainPath),
                planHash=Hash(WorldReconstructionProfile.PlanPath),generatorVersion=p.generatorVersion,seed=p.seed,protectedBounds=index.Count,
                perimeterCandidates=perimeter.Length,sessionFlows=all.Count(t=>t.GetComponent<SessionFlow>()),
                missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),missingTreePhysics=physicsMissing,
                sourceMinHeight=lo,sourceMaxHeight=hi,vehicleResidentBounds=resident,blockers=blockers.ToArray(),protectedObjects=entries.ToArray(),
                perimeterObjectIds=perimeter.Select(t=>GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString()).ToArray()};
            if(report.sessionFlows!=1||report.missingScripts>0)throw new InvalidOperationException("Source composition has missing scripts or duplicate session authority.");
            return new WorldPlanSurvey{report=report,terrain=terrain,protections=index,originalHeights=heights};
        }
    }
}
