using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Forest;
using LastSignal.Persistence;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.WorldReconstruction
{
    [Serializable] internal sealed class WorldStageAcceptance
    {
        public string status, runId, receiptSha256, sceneSha256;
    }
    [Serializable] public sealed class WorldTerrainReceipt
    {
        public string status="IN_PROGRESS", generator="WLD-G3-terrain-v1", runId, scene, sceneGuid, sceneHash, generation, g2ReceiptHash, g2SceneHash, planHash;
        public string coreAsset, westAsset, coreHash, westHash, coreMetaHash, westMetaHash, coreObjectId, linkObjectId, westObjectId;
        public string gameplay="NOT_RUN", navigation="REBAKE_REQUIRED", roads="G4_PENDING", ecology="G5_PENDING", performance="NOT_RUN", error;
        public float minimum, maximum, origin, range, quantizationTolerance, protectedDelta, seamDelta, seamAngleDegrees, splatSeamDelta;
        public int samplesPerTile, rendererMasks, protectedSamples, invalidatedNavSurfaces;
        public WorldStageObject[] expectedOriginalObjects;
        public string[] createdPaths;
    }

    /// <summary>Explicit owner-run terrain derivative. G2 scene/assets remain immutable recovery inputs.</summary>
    public static class WorldTerrainAuthoring
    {
        public const string TerrainScene="Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_TerrainStage.unity";
        public const string LatestReceipt="Docs/Implementation/World/WLD-S001/G3_Latest.json";
        public const string SaveSlot="wld_s001_naturalworld_terrain_stage";
        const string LinkName="WLD-S001 G3 Terrain Links", WestName="WLD-S001 West Terrain";
        static T[] All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();

        [MenuItem("Last Signal/World Reconstruction/WLD-S001/5 Generate G3 terrain candidate")]
        public static void Generate()
        {
            var p=WorldReconstructionProfile.Load();
            var survey=WorldReconstructionAudit.InspectAcceptedCandidate(p);
            if(File.Exists(TerrainScene)||File.Exists(TerrainScene+".meta")||File.Exists(LatestReceipt))
                throw new IOException("G3 candidate/receipt exists; no overwrite. Use 6 Validate saved G3 terrain.");
            if(WorldReconstructionAudit.Hash(WorldReconstructionProfile.PlanPath)!=WorldStageAuthoring.ApprovedPlanHash)
                throw new InvalidOperationException("Plan differs from approved G1.");
            var g2=JsonUtility.FromJson<WorldStageReceipt>(File.ReadAllText(WorldStageAuthoring.LatestReceipt));
            var acceptance=JsonUtility.FromJson<WorldStageAcceptance>(File.ReadAllText("Docs/Implementation/World/WLD-S001/G2_Acceptance.json"));
            if(acceptance.status!="ACCEPTED"||acceptance.runId!=g2.runId||acceptance.receiptSha256!=WorldReconstructionAudit.Hash(WorldStageAuthoring.LatestReceipt)||acceptance.sceneSha256!=g2.stageHash)
                throw new InvalidOperationException("G2 acceptance does not match this saved candidate.");
            var baseline=survey.terrain.terrainData; var inputScene=SceneManager.GetActiveScene();
            if(survey.terrain.transform.childCount!=0)throw new InvalidOperationException("Core terrain has child objects; explicit world-space child preservation is required before changing its origin.");
            var layerSources=baseline.terrainLayers.Select(l=>Path.GetFileNameWithoutExtension(g2.assets.Single(a=>a.destination==AssetDatabase.GetAssetPath(l)).source)).ToArray();
            if(baseline.treeInstanceCount!=0) throw new InvalidOperationException("Native tree reprojection is not approved; expected GameObject vegetation only.");
            WorldTerrainSampling grid;
            try {grid=new WorldTerrainSampling(p,survey,inputScene,(row,count)=>
                {if(EditorUtility.DisplayCancelableProgressBar("WLD G3 — full terrain sampling","Read-only planning; no assets written yet",row/(float)count))
                    throw new OperationCanceledException("Owner cancelled before terrain writes.");});}
            finally {EditorUtility.ClearProgressBar();}
            var r=new WorldTerrainReceipt { runId="G3-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8),
                scene=TerrainScene, g2ReceiptHash=WorldReconstructionAudit.Hash(WorldStageAuthoring.LatestReceipt),g2SceneHash=g2.stageHash,
                planHash=WorldReconstructionAudit.Hash(WorldReconstructionProfile.PlanPath),minimum=grid.minimum,maximum=grid.maximum,origin=grid.origin,range=grid.range,
                samplesPerTile=p.heightmapResolution*p.heightmapResolution,rendererMasks=grid.rendererMasks,protectedSamples=grid.protectedSamples,
                quantizationTolerance=2*grid.range/65535f+.0001f,coreObjectId=WorldStageAuthoring.Id(survey.terrain.gameObject),
                expectedOriginalObjects=WorldStageAuthoring.Snapshot(inputScene) };
            r.generation=p.generatedAssets+"/"+r.runId; r.coreAsset=r.generation+"/CoreTerrain.asset";r.westAsset=r.generation+"/WestTerrain.asset";
            string journal="Docs/Implementation/World/WLD-S001/Evidence/"+r.runId+".json";
            var created=new List<string>();
            void Journal(){r.createdPaths=created.ToArray();File.WriteAllText(journal,JsonUtility.ToJson(r,true));}
            Journal();
            try
            {
                WorldStageAuthoring.Folder(r.generation,created);Journal();
                created.Add(r.coreAsset);Journal();
                if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(baseline),r.coreAsset))throw new IOException("Core terrain copy failed.");
                var coreData=AssetDatabase.LoadAssetAtPath<TerrainData>(r.coreAsset);
                coreData.size=new Vector3(p.core.Width,r.range,p.core.Depth);coreData.SetHeights(0,0,grid.core);
                EditorUtility.SetDirty(coreData);AssetDatabase.SaveAssetIfDirty(coreData);
                var westData=new TerrainData {name="WLD-S001 West Terrain",heightmapResolution=p.heightmapResolution,
                    alphamapResolution=baseline.alphamapResolution,baseMapResolution=baseline.baseMapResolution,
                    size=new Vector3(p.extension.Width,r.range,p.extension.Depth),terrainLayers=baseline.terrainLayers};
                created.Add(r.westAsset);Journal();AssetDatabase.CreateAsset(westData,r.westAsset);
                westData.SetHeights(0,0,grid.west);westData.SetAlphamaps(0,0,grid.WestSurface(baseline,p,layerSources));
                EditorUtility.SetDirty(westData);AssetDatabase.SaveAssetIfDirty(westData);
                VerifyGeneratedHeights(p,grid,coreData,westData,r);
                created.Add(TerrainScene);Journal();
                if(!AssetDatabase.CopyAsset(p.proposedStage,TerrainScene))throw new IOException("G3 scene copy failed.");
                var scene=EditorSceneManager.OpenScene(TerrainScene,OpenSceneMode.Single);
                var core=All<Terrain>(scene).Single();core.terrainData=coreData;core.GetComponent<TerrainCollider>().terrainData=coreData;
                core.transform.position=new Vector3(p.core.minX,r.origin,p.core.minZ);core.allowAutoConnect=false;
                r.expectedOriginalObjects.Single(o=>o.localId==r.coreObjectId).localPosition=core.transform.localPosition;
                var links=new GameObject(LinkName);SceneManager.MoveGameObjectToScene(links,scene);
                var westGO=Terrain.CreateTerrainGameObject(westData);westGO.name=WestName;westGO.layer=core.gameObject.layer;
                westGO.transform.SetParent(links.transform,false);westGO.transform.position=new Vector3(p.extension.minX,r.origin,p.extension.minZ);
                var west=westGO.GetComponent<Terrain>();
                EditorUtility.CopySerialized(core,west);west.terrainData=westData;west.allowAutoConnect=false;
                var wc=westGO.GetComponent<TerrainCollider>();EditorUtility.CopySerialized(core.GetComponent<TerrainCollider>(),wc);wc.terrainData=westData;
                var neighbor=links.AddComponent<WorldTerrainNeighbors>();neighbor.Configure(core,west);
                foreach(var nav in All<NavMeshSurface>(scene)) {nav.RemoveData();nav.navMeshData=null;EditorUtility.SetDirty(nav);r.invalidatedNavSurfaces++;}
                var save=new SerializedObject(All<SaveSession>(scene).Single());save.FindProperty("saveSlot").stringValue=SaveSlot;save.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,TerrainScene))throw new IOException("G3 save failed.");
                r.linkObjectId=WorldStageAuthoring.Id(links);r.westObjectId=WorldStageAuthoring.Id(westGO);
                scene=EditorSceneManager.OpenScene(TerrainScene,OpenSceneMode.Single);
                ValidateStructure(scene,p,r,g2);
                VerifyGeneratedHeights(p,grid,All<WorldTerrainNeighbors>(scene).Single().Core.terrainData,All<WorldTerrainNeighbors>(scene).Single().West.terrainData,r);
                VerifyInputs(g2,r);
                r.sceneGuid=AssetDatabase.AssetPathToGUID(TerrainScene);r.sceneHash=WorldReconstructionAudit.Hash(TerrainScene);
                r.coreHash=WorldReconstructionAudit.Hash(r.coreAsset);r.westHash=WorldReconstructionAudit.Hash(r.westAsset);
                r.coreMetaHash=WorldReconstructionAudit.Hash(r.coreAsset+".meta");r.westMetaHash=WorldReconstructionAudit.Hash(r.westAsset+".meta");
                r.status="IMPLEMENTED_OWNER_ACCEPTANCE_PENDING";Journal();File.WriteAllText(LatestReceipt,JsonUtility.ToJson(r,true));
                Debug.Log($"WLD G3 TERRAIN_CREATED. Two 513x513 tiles; full sampled height {r.minimum:F3}..{r.maximum:F3}m; interval [{r.origin},{r.origin+r.range}]; seam {r.seamDelta:F6}m; protected delta {r.protectedDelta:F6}m. G2 preserved. NavMesh rebake, G4 roads, G5 ecology and gameplay NOT_RUN. Receipt: "+LatestReceipt);
            }
            catch(Exception error)
            {
                r.status="FAILED";r.error=error.ToString();
                try
                {
                    if(SceneManager.GetActiveScene().path==TerrainScene)EditorSceneManager.OpenScene(p.proposedStage,OpenSceneMode.Single);
                    AssetDatabase.StartAssetEditing();
                    try {foreach(var path in created.AsEnumerable().Reverse())
                    {if(Directory.Exists(path)&&Directory.EnumerateFileSystemEntries(path).Any())continue;
                        if((File.Exists(path)||Directory.Exists(path))&&!AssetDatabase.DeleteAsset(path))r.error+="\nRollback retained: "+path;}}
                    finally {AssetDatabase.StopAssetEditing();}
                }
                catch(Exception rollback){r.error+="\nRollback stopped: "+rollback;}
                Journal();throw;
            }
        }

        static void VerifyGeneratedHeights(WorldReconstructionProfile p,WorldTerrainSampling grid,TerrainData core,TerrainData west,WorldTerrainReceipt r)
        {
            int n=p.heightmapResolution;var c=core.GetHeights(0,0,n,n);var w=west.GetHeights(0,0,n,n);r.protectedDelta=0;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                if(Mathf.Abs(c[z,x]-grid.core[z,x])*r.range>r.quantizationTolerance||Mathf.Abs(w[z,x]-grid.west[z,x])*r.range>r.quantizationTolerance)
                    throw new InvalidOperationException("Native terrain differs from full sampled height plan.");
                if(grid.protectedCore[z,x])r.protectedDelta=Mathf.Max(r.protectedDelta,Mathf.Abs(r.origin+c[z,x]*r.range-(p.sourceOrigin.y+grid.original[z,x]*p.sourceSize.y)));
            }
            if(r.protectedDelta>r.quantizationTolerance)throw new InvalidOperationException("Protected source ground changed beyond quantization tolerance.");
            VerifySeam(core,west,r);
            if(core.holesResolution!=west.holesResolution)throw new InvalidOperationException("Hole grids differ.");
            foreach(var terrain in new[]{core,west})foreach(bool solid in terrain.GetHoles(0,0,terrain.holesResolution,terrain.holesResolution))
                if(!solid)throw new InvalidOperationException("Unexpected terrain hole.");
        }
        static void VerifySeam(TerrainData core,TerrainData west,WorldTerrainReceipt r)
        {
            int n=core.heightmapResolution;var c=core.GetHeights(0,0,2,n);var w=west.GetHeights(n-2,0,2,n);
            float step=core.size.x/(n-1);r.seamDelta=0;r.seamAngleDegrees=0;
            for(int z=0;z<n;z++)
            {
                r.seamDelta=Mathf.Max(r.seamDelta,Mathf.Abs(c[z,0]-w[z,1])*r.range);
                float a=Mathf.Atan((c[z,1]-c[z,0])*r.range/step),b=Mathf.Atan((w[z,1]-w[z,0])*r.range/step);
                r.seamAngleDegrees=Mathf.Max(r.seamAngleDegrees,Mathf.Abs(a-b)*Mathf.Rad2Deg);
            }
            int m=core.alphamapResolution;var ca=core.GetAlphamaps(0,0,1,m);var wa=west.GetAlphamaps(m-1,0,1,m);r.splatSeamDelta=0;
            for(int z=0;z<m;z++)for(int l=0;l<core.alphamapLayers;l++)r.splatSeamDelta=Mathf.Max(r.splatSeamDelta,Mathf.Abs(ca[z,0,l]-wa[z,0,l]));
            if(r.seamDelta>.0001f||r.seamAngleDegrees>5||r.splatSeamDelta>2f/255+.0001f)
                throw new InvalidOperationException($"Seam rejection: height={r.seamDelta}, first-cell tangent angle={r.seamAngleDegrees}, splat={r.splatSeamDelta}. No auto smoothing of protected ground.");
        }
        static void VerifyInputs(WorldStageReceipt g2,WorldTerrainReceipt r)
        {
            WorldStageAuthoring.VerifySourceReceipt();
            if(WorldReconstructionAudit.Hash(WorldStageAuthoring.LatestReceipt)!=r.g2ReceiptHash||WorldReconstructionAudit.Hash(g2.stageScene)!=r.g2SceneHash||
                WorldReconstructionAudit.Hash(WorldReconstructionProfile.PlanPath)!=r.planHash)throw new InvalidOperationException("G2/source/plan changed.");
            foreach(var a in g2.assets)
                if(WorldReconstructionAudit.Hash(a.destination)!=a.destinationHash||WorldReconstructionAudit.Hash(a.destination+".meta")!=a.destinationMetaHash||
                    WorldReconstructionAudit.Hash(a.source)!=a.sourceHash||WorldReconstructionAudit.Hash(a.source+".meta")!=a.sourceMetaHash)
                    throw new InvalidOperationException("G2 dependency changed: "+a.destination);
        }
        static void ValidateStructure(Scene scene,WorldReconstructionProfile p,WorldTerrainReceipt r,WorldStageReceipt g2)
        {
            var snapshot=WorldStageAuthoring.Snapshot(scene).ToDictionary(o=>o.localId,StringComparer.Ordinal);
            if(snapshot.Count!=r.expectedOriginalObjects.Length+2)throw new InvalidOperationException("Unexpected G3 object count.");
            foreach(var original in r.expectedOriginalObjects)
                if(!snapshot.TryGetValue(original.localId,out var actual)||JsonUtility.ToJson(actual)!=JsonUtility.ToJson(original))
                    throw new InvalidOperationException("G3 changed protected transform/hierarchy/identity: "+original.name);
            var links=All<WorldTerrainNeighbors>(scene).Single();
            if(!links.HasValidLayout||links.Core.leftNeighbor!=links.West||links.West.rightNeighbor!=links.Core||All<Terrain>(scene).Length!=2)
                throw new InvalidOperationException("Terrain neighbor links/layout did not survive scene reload.");
            foreach(var terrain in new[]{links.Core,links.West})
                if(terrain.GetComponent<TerrainCollider>()?.terrainData!=terrain.terrainData||terrain.terrainData.heightmapResolution!=p.heightmapResolution||
                    terrain.terrainData.size!=new Vector3(500,r.range,500)||terrain.transform.position.y!=r.origin||EditorUtility.IsDirty(terrain.terrainData))
                    throw new InvalidOperationException("Terrain/collider transform or saved asset mismatch.");
            if(AssetDatabase.GetAssetPath(links.Core.terrainData)!=r.coreAsset||AssetDatabase.GetAssetPath(links.West.terrainData)!=r.westAsset||
                links.Core.transform.position.x!=p.core.minX||links.West.transform.position.x!=p.extension.minX||links.Core.transform.position.z!=p.core.minZ||
                !links.Core.terrainData.terrainLayers.SequenceEqual(links.West.terrainData.terrainLayers)||links.Core.materialTemplate!=links.West.materialTemplate)
                throw new InvalidOperationException("G3 terrain asset/material footprint mismatch.");
            if(All<NavMeshSurface>(scene).Any(n=>n.navMeshData))throw new InvalidOperationException("Stale NavMesh remains after topology change.");
            if(All<SessionFlow>(scene).Length!=1||All<SaveSession>(scene).Single().SaveSlot!=SaveSlot||
                new SerializedObject(All<LastSignal.Vehicles.VehicleWorld>(scene).Single()).FindProperty("residentBounds").boundsValue!=WorldStageAuthoring.ResidentBounds||
                new SerializedObject(All<LastSignal.WorldCells.WorldCellManager>(scene).Single()).FindProperty("definitions").arraySize!=0)
                throw new InvalidOperationException("G3 resident/session contract changed.");
            WorldStageIsolation.VerifyNoSourceDependencies(scene.path,g2.assets);
            VerifySeam(links.Core.terrainData,links.West.terrainData,r);
        }

        [MenuItem("Last Signal/World Reconstruction/WLD-S001/6 Validate saved G3 terrain")]
        public static void ValidateSaved()
        {
            if(!File.Exists(LatestReceipt))throw new InvalidOperationException("No successful G3 receipt. Run 5 from accepted G2 first; inspect its first error if it fails.");
            WorldStageAuthoring.Guard(TerrainScene);
            var p=WorldReconstructionProfile.Load();var r=JsonUtility.FromJson<WorldTerrainReceipt>(File.ReadAllText(LatestReceipt));
            var g2=JsonUtility.FromJson<WorldStageReceipt>(File.ReadAllText(WorldStageAuthoring.LatestReceipt));VerifyInputs(g2,r);
            if(r.status!="IMPLEMENTED_OWNER_ACCEPTANCE_PENDING"||r.scene!=TerrainScene||r.sceneGuid!=AssetDatabase.AssetPathToGUID(TerrainScene)||
                r.sceneHash!=WorldReconstructionAudit.Hash(TerrainScene)||r.coreHash!=WorldReconstructionAudit.Hash(r.coreAsset)||r.westHash!=WorldReconstructionAudit.Hash(r.westAsset)||
                r.coreMetaHash!=WorldReconstructionAudit.Hash(r.coreAsset+".meta")||r.westMetaHash!=WorldReconstructionAudit.Hash(r.westAsset+".meta"))
                throw new InvalidOperationException("G3 scene/assets changed from receipt; preserve edits and reconcile.");
            ValidateStructure(SceneManager.GetActiveScene(),p,r,g2);
            Debug.Log($"WLD G3 SAVED_TERRAIN_VERIFIED. Seam={r.seamDelta:F6}m, tangent={r.seamAngleDegrees:F3}deg, splat={r.splatSeamDelta:F6}; protected delta at generation={r.protectedDelta:F6}m. G4/G5, navigation/gameplay/performance NOT_RUN.");
        }
    }
}
