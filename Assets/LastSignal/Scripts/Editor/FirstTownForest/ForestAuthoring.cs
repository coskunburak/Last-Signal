using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastSignal.Atmosphere;
using LastSignal.Forest;
using LastSignal.WorldTime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace LastSignal.Editor.FirstTownForest
{
    /// <summary>Explicit user-run staging. Never modifies production scene until Promote, never bakes or runs tests.</summary>
    public static class ForestAuthoring
    {
        public const string SourceScene="Assets/LastSignal/Scenes/Production/FirstTownCore.unity";
        public const string StageScene="Assets/LastSignal/Scenes/Validation/FirstTownCore_ForestStage.unity";
        public const string Root="Assets/LastSignal/Art/FirstTownForest";
        public const string Evidence="Docs/Implementation/FirstTownCoreForest/Evidence";
        internal static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Guard()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new InvalidOperationException("Exit PlayMode/prefab mode.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits first; no unsaved work is discarded.");
        }
        internal static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
        internal static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
        [MenuItem("Last Signal/First Town/Forest/1 Create or regenerate staging scene")]
        public static void Stage()
        {
            Guard();var active=SceneManager.GetActiveScene();
            if(active.path!=SourceScene && active.path!=StageScene)throw new InvalidOperationException("Open FirstTownCore or the existing forest staging scene.");
            bool fromProduction=active.path==SourceScene;
            if(fromProduction)
            {
                if(File.Exists(StageScene))throw new IOException("Forest stage already exists. Open that scene to regenerate; it is never silently replaced.");
                Folder(Path.GetDirectoryName(StageScene).Replace('\\','/'));
                if(!EditorSceneManager.SaveScene(active,StageScene,true))throw new IOException("Could not stage a copy.");
                active=EditorSceneManager.OpenScene(StageScene,OpenSceneMode.Single);
            }
            string checkpoint="Temp/forest-stage-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+".unity";
            if(!EditorSceneManager.SaveScene(active,checkpoint,true))throw new IOException("Cannot checkpoint stage.");
            string folder=Root+"/Generations/G"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            try
            {
                var state=All<ForestAuthoringState>(active).SingleOrDefault();
                if(!state)
                {
                    var terrain=All<Terrain>(active).Single();
                    if(terrain.terrainData.size!=new Vector3(500,20,500) || terrain.transform.position!=new Vector3(-250,-10,-250))throw new InvalidOperationException("Unexpected terrain topology. Re-audit layout before authoring.");
                    var go=new GameObject("FirstTown Forest — owned authoring");state=go.AddComponent<ForestAuthoringState>();state.terrain=terrain;state.baselineTerrain=terrain.terrainData;
                    state.sourceHash=Hash(SourceScene);
                    state.legacyForest=All<Transform>(active).Single(t=>t.name=="Authored forest clumps").gameObject;
                    var hero=new GameObject("HeroTrees — artist protected");hero.transform.SetParent(go.transform,false);state.heroTrees=hero.transform;
                    var profile=AssetDatabase.LoadAssetAtPath<ForestLayout>(Root+"/Layout.asset");
                    if(!profile){profile=ScriptableObject.CreateInstance<ForestLayout>();AssetDatabase.CreateAsset(profile,Root+"/Layout.asset");}state.layout=profile;
                    var view=go.AddComponent<ForestPresentation>();view.clock=All<WorldClock>(active).Single();view.atmosphere=All<TownAtmosphere>(active).Single();
                }
                if(fromProduction)state.sourceHash=Hash(SourceScene);
                if(state.sourceHash!=Hash(SourceScene))throw new InvalidOperationException("Production scene changed since staging. Preserve/reconcile the stage instead of overwriting production.");
                if(!state.layout || !state.baselineTerrain || !state.heroTrees)throw new InvalidOperationException("Forest authoring dependencies missing.");
                Folder(folder);
                var site=new ForestSite(active,state);var sources=new ForestSources(folder);sources.Build();
                if(state.generated)Object.DestroyImmediate(state.generated);
                state.generated=new GameObject("Generated — regenerate only this subtree");state.generated.transform.SetParent(state.transform,false);
                state.trees=state.bushes=state.rocks=state.grassInstances=0;
                ForestTerrain.Shape(state,site,sources,folder);
                state.placementDigest=ForestDistribution.Scatter(state,site,sources);ForestTerrain.Grass(state,site,sources,folder);
                state.legacyForest.SetActive(false);state.generationFolder=folder;
                foreach(var nav in All<Unity.AI.Navigation.NavMeshSurface>(active)){nav.RemoveData();nav.navMeshData=null;EditorUtility.SetDirty(nav);}
                EditorUtility.SetDirty(state);EditorSceneManager.MarkSceneDirty(active);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(active);
                Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/stage-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json",JsonUtility.ToJson(new Report(state),true));
                Selection.activeObject=state.gameObject;
                Debug.Log("Forest stage authored. Production untouched. Reference/visual/navigation/performance acceptance NOT_RUN.");
            }
            catch
            {
                // Restore ONLY this operation's saved staging checkpoint; immutable generation assets remain for diagnosis.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);File.Copy(checkpoint,StageScene,true);AssetDatabase.ImportAsset(StageScene);
                EditorSceneManager.OpenScene(StageScene,OpenSceneMode.Single);throw;
            }
        }
        [Serializable] sealed class Report
        {
            public string status="IMPLEMENTED",visual="NOT_RUN",navigation="NOT_RUN",performance="NOT_MEASURED",placementDigest,generation;
            public int trees,bushes,rocks,grass;
            public Report(ForestAuthoringState s){trees=s.trees;bushes=s.bushes;rocks=s.rocks;grass=s.grassInstances;placementDigest=s.placementDigest;generation=s.generationFolder;}
        }
        [MenuItem("Last Signal/First Town/Forest/3 Promote saved stage to FirstTownCore")]
        public static void Promote()
        {
            Guard();var scene=SceneManager.GetActiveScene();if(scene.path!=StageScene)throw new InvalidOperationException("Open and save the forest staging scene first.");
            var state=All<ForestAuthoringState>(scene).Single();if(state.sourceHash!=Hash(SourceScene))throw new InvalidOperationException("Source changed; promotion refused.");
            if(!state.generated || !state.terrain || state.terrain.terrainData==state.baselineTerrain)throw new InvalidOperationException("Stage generation is incomplete.");
            Directory.CreateDirectory(Evidence);string backup=Evidence+"/FirstTownCore-before-forest-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+".unity";
            File.Copy(SourceScene,backup,false);
            if(!EditorSceneManager.SaveScene(scene,SourceScene,false))throw new IOException("Promotion failed; source backup: "+backup);
            Folder(Root+"/Archives");
            string archive=Root+"/Archives/ForestStage-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".unity";
            string error=AssetDatabase.MoveAsset(StageScene,archive);if(!string.IsNullOrEmpty(error))Debug.LogWarning("Stage archive pending: "+error);
            // Keep production open; a future Stage copies current production again, including protected hero edits.
            Debug.Log("Forest stage promoted; backup "+backup+". NavMesh bake and acceptance remain user-owned.");
        }
        [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
        static void Draw(ForestAuthoringState state,GizmoType kind)
        {
            if(!state.layout)return;
            Gizmos.color=new Color(.2f,.7f,.4f,.65f);
            foreach(var trail in state.layout.trails)for(int i=1;i<trail.points.Length;i++)
            {var a=trail.points[i-1];var b=trail.points[i];Gizmos.DrawLine(new Vector3(a.x,1,a.y),new Vector3(b.x,1,b.y));}
            Gizmos.color=new Color(1,.7f,.2f,.5f);foreach(var b in state.layout.artistExclusions)Gizmos.DrawWireCube(b.center,b.size);
        }
    }
}
