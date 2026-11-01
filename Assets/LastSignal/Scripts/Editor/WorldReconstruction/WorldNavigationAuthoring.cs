using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Persistence;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.WorldReconstruction
{
    [Serializable] public sealed class WorldSpawnNavigation
    {
        public string identity;
        public Vector3 position, sampledPosition;
        public int agentType;
        public float radius, correctionDistance;
        public bool found;
    }
    [Serializable] public sealed class WorldNavigationReceipt
    {
        public string status="IN_PROGRESS", runId, generation, scene, sceneHash, sceneGuid, sourceHash, terrainReceiptHash;
        public string navAsset, navHash, navMetaHash, error, saveSlot;
        public string gameplay="OWNER_RETEST_PENDING", routes="G4_ROADS_PENDING", performance="NOT_RUN";
        public int agentType;
        public WorldSpawnNavigation[] spawns;
        public string[] createdPaths;
    }

    // G4 navigation foundation only: owner-run bake. Does not move encounters, alter roads or silence runtime checks.
    public static class WorldNavigationAuthoring
    {
        public const string NavigationScene="Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_NavigationStage.unity";
        public const string LatestReceipt="Docs/Implementation/World/WLD-S001/G4_Navigation_Latest.json";
        public const string SaveSlot="wld_s001_naturalworld_navigation_stage";
        static T[] All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();

        static WorldSpawnNavigation[] CheckSpawns(Scene scene, int agentType, bool query)
        {
            var results=new List<WorldSpawnNavigation>();
            foreach(var encounter in All<ZombieEncounter>(scene))
            {
                var serialized=new SerializedObject(encounter);
                var prefab=serialized.FindProperty("prefab").objectReferenceValue as ZombieController;
                var spawn=serialized.FindProperty("spawn").objectReferenceValue as Transform;
                var identity=encounter.GetComponent<PersistentEntityId>();
                if(!prefab||!spawn||!prefab.Definition||!identity||!prefab.GetComponent<NavMeshAgent>())
                    throw new InvalidOperationException("Incomplete encounter navigation input: "+encounter.name);
                var agent=prefab.GetComponent<NavMeshAgent>();
                if(agent.agentTypeID!=agentType)throw new InvalidOperationException("Encounter agent differs from resident surface: "+identity.Id);
                var check=new WorldSpawnNavigation {identity=identity.Id,position=spawn.position,agentType=agent.agentTypeID,radius=prefab.Definition.SpawnSampleRadius};
                if(query)
                {
                    check.found=NavMesh.SamplePosition(spawn.position,out var hit,check.radius,new NavMeshQueryFilter {agentTypeID=agent.agentTypeID,areaMask=agent.areaMask});
                    if(check.found){check.sampledPosition=hit.position;check.correctionDistance=Vector3.Distance(spawn.position,hit.position);}
                }
                results.Add(check);
            }
            if(results.Count==0||results.Select(r=>r.identity).Distinct().Count()!=results.Count)throw new InvalidOperationException("Missing or duplicated encounter IDs.");
            return results.OrderBy(r=>r.identity,StringComparer.Ordinal).ToArray();
        }

        [MenuItem("Last Signal/World Reconstruction/WLD-S001/7 Create navigation candidate and bake")]
        public static void CreateAndBake()
        {
            // This validates the saved G3 baseline without running gameplay or altering it.
            WorldTerrainAuthoring.ValidateSaved();
            if(File.Exists(NavigationScene)||File.Exists(NavigationScene+".meta")||File.Exists(LatestReceipt))
                throw new IOException("Navigation candidate/receipt already exists. Use 8 Validate saved navigation; no overwrite.");
            var source=SceneManager.GetActiveScene();var sourceSurface=All<NavMeshSurface>(source).Single();
            if(!sourceSurface.isActiveAndEnabled||sourceSurface.navMeshData||sourceSurface.transform.lossyScale!=Vector3.one||sourceSurface.transform.rotation!=Quaternion.identity)
                throw new InvalidOperationException("Expected one enabled, unbaked, unscaled resident navigation surface.");
            var settings=NavMesh.GetSettingsByID(sourceSurface.agentTypeID);
            if(settings.agentTypeID!=sourceSurface.agentTypeID||settings.agentTypeID==-1)throw new InvalidOperationException("Resident agent build settings not found.");
            CheckSpawns(source,sourceSurface.agentTypeID,false);
            var baselineObjects=WorldStageAuthoring.Snapshot(source);
            var r=new WorldNavigationReceipt {runId="G4Nav-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8),
                scene=NavigationScene,sourceHash=WorldReconstructionAudit.Hash(WorldTerrainAuthoring.TerrainScene),
                terrainReceiptHash=WorldReconstructionAudit.Hash(WorldTerrainAuthoring.LatestReceipt),saveSlot=SaveSlot,agentType=sourceSurface.agentTypeID};
            r.generation=WorldReconstructionProfile.Load().generatedAssets+"/"+r.runId;r.navAsset=r.generation+"/ResidentNavMesh.asset";
            string journal="Docs/Implementation/World/WLD-S001/Evidence/"+r.runId+".json";
            var created=new List<string>();void Journal(){r.createdPaths=created.ToArray();File.WriteAllText(journal,JsonUtility.ToJson(r,true));}Journal();
            try
            {
                WorldStageAuthoring.Folder(r.generation,created);created.Add(NavigationScene);Journal();
                if(!AssetDatabase.CopyAsset(WorldTerrainAuthoring.TerrainScene,NavigationScene))throw new IOException("Navigation scene copy failed.");
                var scene=EditorSceneManager.OpenScene(NavigationScene,OpenSceneMode.Single);
                var surface=All<NavMeshSurface>(scene).Single();
                // Bound collection to the approved resident world; include both core and western terrain.
                surface.collectObjects=CollectObjects.Volume;
                surface.center=surface.transform.InverseTransformPoint(WorldStageAuthoring.ResidentBounds.center);
                surface.size=WorldStageAuthoring.ResidentBounds.size;
                surface.BuildNavMesh();
                if(!surface.navMeshData)throw new InvalidOperationException("Navigation bake produced no data.");
                created.Add(r.navAsset);Journal();AssetDatabase.CreateAsset(surface.navMeshData,r.navAsset);
                AssetDatabase.SaveAssetIfDirty(surface.navMeshData);
                r.spawns=CheckSpawns(scene,r.agentType,true);Journal();RequireSpawnCoverage(r);
                var save=new SerializedObject(All<SaveSession>(scene).Single());save.FindProperty("saveSlot").stringValue=SaveSlot;save.ApplyModifiedPropertiesWithoutUndo();
                CompareObjects(baselineObjects,WorldStageAuthoring.Snapshot(scene));
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,NavigationScene))throw new IOException("Navigation candidate save failed.");
                scene=EditorSceneManager.OpenScene(NavigationScene,OpenSceneMode.Single);
                CompareObjects(baselineObjects,WorldStageAuthoring.Snapshot(scene));
                ValidateSurface(scene,r);r.spawns=CheckSpawns(scene,r.agentType,true);RequireSpawnCoverage(r);VerifyInputs(r);
                r.sceneHash=WorldReconstructionAudit.Hash(NavigationScene);r.sceneGuid=AssetDatabase.AssetPathToGUID(NavigationScene);
                r.navHash=WorldReconstructionAudit.Hash(r.navAsset);r.navMetaHash=WorldReconstructionAudit.Hash(r.navAsset+".meta");
                r.status="IMPLEMENTED_OWNER_ACCEPTANCE_PENDING";Journal();File.WriteAllText(LatestReceipt,JsonUtility.ToJson(r,true));
                Debug.Log("WLD G4 NAVIGATION_CREATED. Spawn coverage="+r.spawns.Length+"/"+r.spawns.Length+"; source G3 unchanged; slot="+SaveSlot+". Run 8 Validate saved navigation, then owner Play test. Full road/traversal/performance acceptance pending.");
            }
            catch(Exception error)
            {
                r.status="FAILED";r.error=error.ToString();
                try
                {
                    if(SceneManager.GetActiveScene().path==NavigationScene)EditorSceneManager.OpenScene(WorldTerrainAuthoring.TerrainScene,OpenSceneMode.Single);
                    AssetDatabase.StartAssetEditing();
                    try {foreach(string path in created.AsEnumerable().Reverse())
                    {if(Directory.Exists(path)&&Directory.EnumerateFileSystemEntries(path).Any())continue;
                        if((File.Exists(path)||Directory.Exists(path))&&!AssetDatabase.DeleteAsset(path))r.error+="\nRollback retained: "+path;}}
                    finally {AssetDatabase.StopAssetEditing();}
                }
                catch(Exception rollback){r.error+="\nRollback stopped: "+rollback;}
                Journal();throw;
            }
        }
        static void CompareObjects(WorldStageObject[] expected,WorldStageObject[] actual)
        {
            if(expected.Length!=actual.Length)throw new InvalidOperationException("Navigation candidate changed object count.");
            for(int i=0;i<expected.Length;i++)if(JsonUtility.ToJson(expected[i])!=JsonUtility.ToJson(actual[i]))
                throw new InvalidOperationException("Navigation bake changed transform/identity: "+expected[i].name);
        }
        static void RequireSpawnCoverage(WorldNavigationReceipt r)
        {
            var failed=r.spawns.Where(s=>!s.found).Select(s=>s.identity).ToArray();
            if(failed.Length>0)throw new InvalidOperationException("Baked navigation misses encounter spawns: "+string.Join(", ",failed)+". Positions preserved; see journal. No automatic spawn moves or enlarged correction radius.");
        }
        static void ValidateSurface(Scene scene,WorldNavigationReceipt r)
        {
            var surface=All<NavMeshSurface>(scene).Single();
            if(!surface.isActiveAndEnabled||!surface.navMeshData||surface.agentTypeID!=r.agentType||AssetDatabase.GetAssetPath(surface.navMeshData)!=r.navAsset||
                surface.collectObjects!=CollectObjects.Volume||surface.size!=WorldStageAuthoring.ResidentBounds.size||
                (surface.transform.TransformPoint(surface.center)-WorldStageAuthoring.ResidentBounds.center).sqrMagnitude>.000001f||
                All<SaveSession>(scene).Single().SaveSlot!=SaveSlot||All<SessionFlow>(scene).Length!=1)
                throw new InvalidOperationException("Saved resident navigation ownership differs from bake contract.");
            var terrainLinks=All<LastSignal.Forest.WorldTerrainNeighbors>(scene).Single();
            if(!terrainLinks.HasValidLayout||terrainLinks.Core.leftNeighbor!=terrainLinks.West||terrainLinks.West.rightNeighbor!=terrainLinks.Core)
                throw new InvalidOperationException("Terrain neighbors missing in navigation candidate.");
            var g3=JsonUtility.FromJson<WorldTerrainReceipt>(File.ReadAllText(WorldTerrainAuthoring.LatestReceipt));
            if(AssetDatabase.GetAssetPath(terrainLinks.Core.terrainData)!=g3.coreAsset||AssetDatabase.GetAssetPath(terrainLinks.West.terrainData)!=g3.westAsset)
                throw new InvalidOperationException("Navigation candidate references unexpected terrain assets.");
        }
        static void VerifyInputs(WorldNavigationReceipt r)
        {
            WorldStageAuthoring.VerifySourceReceipt();
            if(WorldReconstructionAudit.Hash(WorldTerrainAuthoring.TerrainScene)!=r.sourceHash||WorldReconstructionAudit.Hash(WorldTerrainAuthoring.LatestReceipt)!=r.terrainReceiptHash)
                throw new InvalidOperationException("G3 source changed during navigation work.");
            var g3=JsonUtility.FromJson<WorldTerrainReceipt>(File.ReadAllText(WorldTerrainAuthoring.LatestReceipt));
            foreach(var pair in new[]{new[]{g3.coreAsset,g3.coreHash},new[]{g3.westAsset,g3.westHash},new[]{g3.coreAsset+".meta",g3.coreMetaHash},new[]{g3.westAsset+".meta",g3.westMetaHash}})
                if(WorldReconstructionAudit.Hash(pair[0])!=pair[1])throw new InvalidOperationException("G3 terrain changed: "+pair[0]);
            var g2=JsonUtility.FromJson<WorldStageReceipt>(File.ReadAllText(WorldStageAuthoring.LatestReceipt));
            if(WorldReconstructionAudit.Hash(WorldStageAuthoring.LatestReceipt)!=g3.g2ReceiptHash||WorldReconstructionAudit.Hash(g2.stageScene)!=g3.g2SceneHash)
                throw new InvalidOperationException("G2 baseline changed.");
            foreach(var a in g2.assets)
                if(WorldReconstructionAudit.Hash(a.destination)!=a.destinationHash||WorldReconstructionAudit.Hash(a.destination+".meta")!=a.destinationMetaHash||
                    WorldReconstructionAudit.Hash(a.source)!=a.sourceHash||WorldReconstructionAudit.Hash(a.source+".meta")!=a.sourceMetaHash)
                    throw new InvalidOperationException("Shared source dependency changed: "+a.destination);
        }
        [MenuItem("Last Signal/World Reconstruction/WLD-S001/8 Validate saved navigation")]
        public static void ValidateSaved()
        {
            if(!File.Exists(LatestReceipt))throw new InvalidOperationException("No successful navigation receipt. Run 7 from saved G3, or inspect its first failure.");
            WorldStageAuthoring.Guard(NavigationScene);
            var r=JsonUtility.FromJson<WorldNavigationReceipt>(File.ReadAllText(LatestReceipt));
            if(r.status!="IMPLEMENTED_OWNER_ACCEPTANCE_PENDING"||r.scene!=NavigationScene||r.sceneHash!=WorldReconstructionAudit.Hash(NavigationScene)||
                r.sceneGuid!=AssetDatabase.AssetPathToGUID(NavigationScene)||r.navHash!=WorldReconstructionAudit.Hash(r.navAsset)||r.navMetaHash!=WorldReconstructionAudit.Hash(r.navAsset+".meta"))
                throw new InvalidOperationException("Navigation candidate differs from receipt. Preserve and reconcile edits.");
            var scene=SceneManager.GetActiveScene();VerifyInputs(r);ValidateSurface(scene,r);
            r.spawns=CheckSpawns(scene,r.agentType,true);RequireSpawnCoverage(r);
            Debug.Log("WLD G4 SAVED_NAVIGATION_VERIFIED. Spawn coverage="+r.spawns.Length+"/"+r.spawns.Length+". Ready for owner Play test; enemy chase, complete routes and performance still pending.");
        }
    }
}
