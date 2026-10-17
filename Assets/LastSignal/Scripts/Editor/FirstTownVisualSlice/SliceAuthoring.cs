using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LastSignal.Atmosphere;
using LastSignal.Forest;
using LastSignal.VisualSlice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    /// <summary>Owner invoked, transactional native authoring only. Never runs gameplay, tests or builds.</summary>
    public static class SliceAuthoring
    {
        public const string Source="Assets/LastSignal/Scenes/Production/FirstTownCore.unity";
        public const string Stage="Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity";
        public const string Root="Assets/LastSignal/Art/FirstTownVisualSlice";
        public const string Evidence="Docs/Implementation/FirstTownVisualSlice/Evidence";
        internal static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        internal static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
        internal static string HashText(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");}
        internal static void Folder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        internal static void Guard()
        {
            RequireEditMode();
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Unsaved scene: "+SceneManager.GetSceneAt(i).name+". Save this scene before this operation. To resume an existing visual slice instead, use Visual Slice > 0 Open saved stage (preserve unsaved scenes). Nothing was discarded.");
            if(SceneManager.sceneCount!=1)throw new InvalidOperationException("Open only the target scene to avoid cross-scene authoring.");
        }
        static void RequireEditMode()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage()!=null)
                throw new InvalidOperationException("Exit PlayMode and Prefab Mode.");
        }
        [MenuItem("Last Signal/First Town/Visual Slice/0 Open saved stage (preserve unsaved scenes)")]
        public static void OpenSavedStage()
        {
            RequireEditMode();
            if(!File.Exists(Stage))throw new FileNotFoundException("No saved visual slice yet. Open FirstTownCore and use menu 1 to create it.",Stage);
            // If already loaded, select the in-memory scene. Never reload over unsaved stage edits.
            var loaded=SceneManager.GetSceneByPath(Stage);
            if(loaded.IsValid() && loaded.isLoaded)
            {
                SceneManager.SetActiveScene(loaded);
                Debug.Log("Visual slice is already open. In-memory edits retained; nothing regenerated. Save the stage before using menu 1 to regenerate.");
                return;
            }
            var recoveries=new List<string>();
            string folder=Root+"/Recovery/R"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N");
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var current=SceneManager.GetSceneAt(i);
                if(!current.isLoaded || !current.isDirty)continue;
                Folder(folder);
                string copy=folder+"/UnsavedScene-"+i+".unity";
                // Save a copy, not the source: source-hash guards and the user's original file remain valid.
                if(!EditorSceneManager.SaveScene(current,copy,true))
                    throw new IOException("Cannot preserve unsaved scene "+current.name+". Scene switch cancelled; already created copies: "+string.Join(", ",recoveries));
                recoveries.Add(current.name+" -> "+copy);
            }
            if(recoveries.Count>0)
            {
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence+"/recovery-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".txt",string.Join("\n",recoveries)+"\n");
            }
            EditorSceneManager.OpenScene(Stage,OpenSceneMode.Single);
            Debug.Log("Opened saved FirstTownCore_VisualSlice. Nothing regenerated or promoted. "+
                (recoveries.Count>0?"Unsaved scenes preserved: "+string.Join("; ",recoveries):"Production scene unchanged.")+
                " Use this scene for review; FirstTownCore still contains the original environment until explicit promotion.");
        }
        internal static FirstTownSliceProfile Profile()
        {
            Folder(Root);var path=Root+"/Profile.asset";var profile=AssetDatabase.LoadAssetAtPath<FirstTownSliceProfile>(path);
            if(!profile)
            {
                profile=ScriptableObject.CreateInstance<FirstTownSliceProfile>();
                var terrain=All<Terrain>(SceneManager.GetActiveScene()).Single();
                foreach(var pose in profile.views.Where(p=>!p.orthographic))
                {
                    pose.position.y=Mathf.Max(0,terrain.SampleHeight(pose.position)+terrain.transform.position.y)+(pose.name.Contains("Roadside") || pose.name.Contains("station") || pose.name.Contains("Service")?1.69f:1.65f);
                    pose.target.y+=Mathf.Max(0,terrain.SampleHeight(pose.target)+terrain.transform.position.y);
                }
                AssetDatabase.CreateAsset(profile,path);
            }
            return profile;
        }
        [MenuItem("Last Signal/First Town/Visual Slice/1 Create or regenerate staged slice")]
        public static void StageSlice()
        {
            RequireEditMode();var scene=SceneManager.GetActiveScene();bool first=scene.path==Source;
            if(!first && scene.path!=Stage)throw new InvalidOperationException("Open FirstTownCore or FirstTownCore_VisualSlice.");
            // Reopening production after an Editor restart is not a request to overwrite a saved candidate.
            if(first && File.Exists(Stage)){OpenSavedStage();return;}
            Guard();
            // Preflight before copying scenes or writing generation assets.
            var forest=All<ForestAuthoringState>(scene).Single();var director=All<TownAtmosphere>(scene).Single();
            if(!forest.terrain || !forest.generated || !forest.layout || !forest.heroTrees || !director.sky || !director.sun)
                throw new InvalidOperationException("Current forest/weather dependencies missing. Preserve scene and re-audit.");
            if(forest.terrain.transform.position!=new Vector3(-250,-10,-250) || forest.terrain.terrainData.size!=new Vector3(500,20,500))
                throw new InvalidOperationException("Unexpected terrain coordinates. No terrain edit performed.");
            foreach(var key in new[]{"Pine","Birch","Bush","Grass","Rock","Log"})SliceAssets.Load<GameObject>(forest.generationFolder+"/"+key+".prefab");
            foreach(var key in new[]{"Wreck","Pallet","Fence","Awning"})SliceAssets.Load<GameObject>(SliceAssets.Town+key+".prefab");
            var profile=Profile();AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Evidence);string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            if(first)
            {
                Folder(Path.GetDirectoryName(Stage).Replace('\\','/'));
                if(!EditorSceneManager.SaveScene(scene,Stage,true))throw new IOException("Cannot create staging copy.");
                scene=EditorSceneManager.OpenScene(Stage,OpenSceneMode.Single);
            }
            string checkpoint=Evidence+"/stage-before-"+stamp+".unity";
            File.Copy(Stage,checkpoint,false);
            string generation=Root+"/Generations/G"+stamp;
            try
            {
                var protectedState=All<MonoBehaviour>(scene).Where(c=>c &&
                    (c is LastSignal.Persistence.PersistentEntityId || c is LastSignal.Persistence.SaveSession ||
                     c is LastSignal.Loot.LootSpawnPoint || c is DoorInteractable || c is ZombieEncounter ||
                     c is SessionFlow || c is LastSignal.Shelter.ShelterLoop || c is LastSignal.Vehicles.VehicleWorld ||
                     c is LastSignal.WorldTime.WorldClock)).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
                var state=All<FirstTownSliceState>(scene).SingleOrDefault();
                if(!state)
                {
                    var g=new GameObject("FirstTown visual slice — owned authoring");state=g.AddComponent<FirstTownSliceState>();state.profile=profile;
                    state.sourceHash=Hash(Source);state.terrain=All<Terrain>(scene).Single();state.baselineTerrain=state.terrain.terrainData;
                    state.baselineTerrainHash=Hash(AssetDatabase.GetAssetPath(state.baselineTerrain));state.baselineTerrainMaterial=state.terrain.materialTemplate;
                    state.atmosphere=All<TownAtmosphere>(scene).Single();state.baselineSky=state.atmosphere.sky;state.baselineLighting=state.atmosphere.lightingProfile;
                    state.baselineSurfaces=state.atmosphere.surfaces;state.baselineWetRenderers=state.atmosphere.wetRenderers;state.baselineLamps=state.atmosphere.lamps;
                    state.artistContent=SliceComposition.Group("Artist additions — preserved",state.transform);
                }
                if(first)state.sourceHash=Hash(Source);
                if(state.sourceHash!=Hash(Source) || state.baselineTerrainHash!=Hash(AssetDatabase.GetAssetPath(state.baselineTerrain)))
                    throw new InvalidOperationException("Production scene or source TerrainData changed since staging. Reconcile before regenerating or promoting.");
                // A saved scene checkpoint exists before removing only this tool's previous generated subtree.
                Restore(state);
                state.generated=SliceComposition.Group("Generated — replaceable, move hand edits to Artist additions",state.transform).gameObject;
                state.generation=generation;state.trees=state.shrubs=state.rocks=state.grass=0;
                Folder(generation);
                var site=new SliceSite(scene,state);site.SuppressOldVegetation();var art=new SliceAssets(generation,site.forest);
                SliceGround.Build(site,art);new SliceComposition(site,art).Build();SliceGround.Grass(site,art);Atmosphere(state,art);
                Cameras(state);foreach(var surface in All<Unity.AI.Navigation.NavMeshSurface>(scene)){surface.RemoveData();surface.navMeshData=null;EditorUtility.SetDirty(surface);}
                foreach(var pair in protectedState)
                    if(!pair.Key || EditorJsonUtility.ToJson(pair.Key)!=pair.Value)
                        throw new InvalidOperationException("Protected gameplay authoring changed; restoring stage checkpoint: "+(pair.Key?pair.Key.GetType().Name:"destroyed component"));
                foreach(var component in All<Component>(scene))if(component && PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                AssetDatabase.SaveAssets();
                state.generatedTerrainHash=Hash(AssetDatabase.GetAssetPath(state.terrain.terrainData));
                state.atmosphereHash=AtmosphereFingerprint(state.atmosphere);
                state.generatedSkyHash=Hash(AssetDatabase.GetAssetPath(state.atmosphere.sky));
                state.generatedLightingHash=Hash(AssetDatabase.GetAssetPath(state.atmosphere.lightingProfile));
                EditorUtility.SetDirty(state);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save staged slice.");
                File.WriteAllText(Evidence+"/authoring-"+stamp+".json",JsonUtility.ToJson(new Report(state,site,art),true));
                File.WriteAllText(Evidence+"/assets-"+stamp+".tsv","Asset\tSource\n"+string.Join("\n",art.provenance.Select(p=>p.Key+"\t"+p.Value)));
                Selection.activeObject=state.gameObject;
                Debug.Log("Visual slice staged. Production scene unchanged. Native generation completed; visuals, navigation, gameplay and performance NOT_VERIFIED. Open Visual comparison studio.");
            }
            catch(Exception error)
            {
                // Only restore the staging scene. Keep immutable generated assets for diagnosis; never delete source assets.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);File.Copy(checkpoint,Stage,true);AssetDatabase.ImportAsset(Stage);
                EditorSceneManager.OpenScene(Stage,OpenSceneMode.Single);
                File.WriteAllText(Evidence+"/failed-"+stamp+".txt",error+"\nCheckpoint: "+checkpoint+"\nGeneration: "+generation);throw;
            }
        }
        static void Restore(FirstTownSliceState s)
        {
            if(s.generated && s.generated.transform.Find(SliceRefinement04.Marker))
                throw new InvalidOperationException("Refinement04 contains retained original fences and a relocated lamp. Preserve the saved stage; use its authoring checkpoint or Undo to reconcile these changes before regeneration. See REFINEMENT_04.md.");
            // The saved candidate can be refined without regenerating its scene or terrain.
            // Rebuilding would silently replace those stage-only meshes with the old recipe.
            string refinementManifest=Evidence+"/Refinement01/manifest.json";
            if(s.generated && File.Exists(refinementManifest))
            {
                var refinement=JsonUtility.FromJson<MeshRefinementStatus>(File.ReadAllText(refinementManifest));
                if(refinement==null || refinement.status!="RESTORED_RENDER_MESHES")
                    throw new InvalidOperationException("The saved stage has an in-place mesh refinement. Keep and review it; do not regenerate until that refinement is preserved or deliberately reconciled. See Docs/Implementation/FirstTownVisualSlice/REFINEMENT_01.md.");
            }
            if(s.generated && (AssetDatabase.GetAssetPath(s.terrain.terrainData)!=s.generation+"/Terrain.asset" ||
                Hash(AssetDatabase.GetAssetPath(s.terrain.terrainData))!=s.generatedTerrainHash ||
                AtmosphereFingerprint(s.atmosphere)!=s.atmosphereHash ||
                Hash(AssetDatabase.GetAssetPath(s.atmosphere.sky))!=s.generatedSkyHash ||
                Hash(AssetDatabase.GetAssetPath(s.atmosphere.lightingProfile))!=s.generatedLightingHash))
                throw new InvalidOperationException("Staged terrain, sky or atmosphere differs from the recorded generation. Keep this saved stage; inspect saved edits or asset serialization changes before regeneration so no changes are lost.");
            foreach(var entry in s.renderers)
                if(entry.target && (entry.target.enabled || !entry.target.sharedMaterials.SequenceEqual(entry.materials)))
                    throw new InvalidOperationException("An original renderer managed by the slice was manually changed: "+entry.target.name+". Preserve/reconcile that edit before regeneration. The saved checkpoint is retained.");
            foreach(var g in s.hiddenVegetation)
                if(g && g.activeSelf)throw new InvalidOperationException("A suppressed original vegetation instance was manually re-enabled. Reconcile it before regeneration: "+g.name);
            foreach(var entry in s.renderers)if(entry.target){entry.target.enabled=entry.enabled;entry.target.sharedMaterials=entry.materials;}
            foreach(var g in s.hiddenVegetation)if(g)g.SetActive(true);
            s.renderers=Array.Empty<FirstTownSliceState.RendererRecord>();s.hiddenVegetation=Array.Empty<GameObject>();
            s.terrain.terrainData=s.baselineTerrain;s.terrain.GetComponent<TerrainCollider>().terrainData=s.baselineTerrain;s.terrain.materialTemplate=s.baselineTerrainMaterial;
            s.atmosphere.sky=s.baselineSky;s.atmosphere.lightingProfile=s.baselineLighting;s.atmosphere.surfaces=s.baselineSurfaces;s.atmosphere.wetRenderers=s.baselineWetRenderers;s.atmosphere.lamps=s.baselineLamps;
            if(s.generated)Object.DestroyImmediate(s.generated);
        }
        [Serializable] sealed class MeshRefinementStatus { public string status; }
        static string AtmosphereFingerprint(TownAtmosphere d)
        {
            // Paths and authored values are stable across domain reloads; instance IDs are not.
            var b=new StringBuilder();
            string PathOf(Transform t)=>t?(t.parent?PathOf(t.parent)+"/":"")+t.name+"#"+t.GetSiblingIndex():"null";
            b.Append(AssetDatabase.GetAssetPath(d.sky)).Append('|').Append(AssetDatabase.GetAssetPath(d.lightingProfile)).Append('|').Append(d.localLightBudget);
            foreach(var v in d.surfaces)b.AppendFormat(CultureInfo.InvariantCulture,"|{0}:{1:R}:{2:R}:{3}",AssetDatabase.GetAssetPath(v.material),v.darkening,v.smoothness,v.wetOnly);
            foreach(var r in d.wetRenderers)b.Append('|').Append(r?PathOf(r.transform):"null");
            foreach(var l in d.lamps)b.AppendFormat(CultureInfo.InvariantCulture,"|{0}:{1:R}:{2}:{3}",l.light?PathOf(l.light.transform):"null",l.intensity,l.interior,l.shadows);
            return HashText(b.ToString());
        }
        static void Atmosphere(FirstTownSliceState s,SliceAssets art)
        {
            var d=s.atmosphere;var sky=new Material(s.baselineSky){name="Slice calibrated sky"};
            sky.SetVector("_Exposure",new Vector4(.78f,.76f,.67f,.18f));AssetDatabase.CreateAsset(sky,art.folder+"/Sky.mat");d.sky=sky;
            var lighting=ScriptableObject.CreateInstance<TownLightingProfile>();lighting.sunIntensity=1.5f;lighting.dayAmbient=new Color(.39f,.43f,.46f);lighting.daySkyFill=new Color(.39f,.45f,.5f);lighting.skyFillIntensity=.32f;
            AssetDatabase.CreateAsset(lighting,art.folder+"/Lighting.asset");d.lightingProfile=lighting;
            // Register only new exterior materials with the same director. Forest wind shaders already consume its globals.
            var surfaces=d.surfaces.ToList();var wet=d.wetRenderers.Where(r=>r).ToList();
            foreach(var r in s.generated.GetComponentsInChildren<MeshRenderer>())
            {
                if(r.GetComponent<TextMesh>() || r.name.Contains("retained outside"))continue;
                bool any=false;
                foreach(var m in r.sharedMaterials)
                {
                    if(!m || !m.HasProperty("_BaseColor") || !m.HasProperty("_Smoothness") || m.shader.name.StartsWith("LastSignal/Forest/"))continue;
                    bool roof=m.name.Contains("Roof") || m.name.Contains("Metal");
                    if(!surfaces.Any(v=>v.material==m))surfaces.Add(new TownAtmosphere.Surface{material=m,darkening=roof?.08f:.16f,smoothness=roof?.52f:.34f});
                    any=true;
                }
                if(any && !wet.Contains(r))wet.Add(r);
            }
            d.surfaces=surfaces.ToArray();d.wetRenderers=wet.ToArray();
            // Existing safehouse and forecourt lights remain in the shared nearest-light budget.
            EditorUtility.SetDirty(d);
        }
        static void Cameras(FirstTownSliceState s)
        {
            var root=SliceComposition.Group("07 Fixed comparison cameras — disabled",s.generated.transform);
            foreach(var pose in s.profile.views)
            {
                var t=SliceComposition.Group(pose.name,root);t.position=pose.position;t.LookAt(pose.target);
                var c=t.gameObject.AddComponent<Camera>();c.enabled=false;c.fieldOfView=pose.fieldOfView;c.orthographic=pose.orthographic;c.orthographicSize=pose.orthographicSize;c.nearClipPlane=.08f;c.farClipPlane=700;
            }
        }
        [Serializable] sealed class Report
        {
            public string status="IMPLEMENTED_NATIVE_AUTHORING",visual="VISUAL_REVIEW_PENDING",compilationAcceptance="NOT_RUN",gameplay="NOT_RUN",navigation="REBAKE_REQUIRED",performance="NOT_MEASURED";
            public string generation,sourceHash,placementDigest;public int trees,shrubs,rocks,grass,hiddenVegetation,changedRenderers;public string[] skippedProps;
            public Report(FirstTownSliceState s,SliceSite site,SliceAssets art){generation=art.folder;sourceHash=s.sourceHash;placementDigest=s.placementDigest;trees=s.trees;shrubs=s.shrubs;rocks=s.rocks;grass=s.grass;hiddenVegetation=s.hiddenVegetation.Length;changedRenderers=s.renderers.Length;skippedProps=site.skipped.ToArray();}
        }
        [MenuItem("Last Signal/First Town/Visual Slice/3 Bake STAGE navigation (user only)")]
        public static void BakeStageNavigation()
        {
            Guard();var scene=SceneManager.GetActiveScene();if(scene.path!=Stage)throw new InvalidOperationException("Open the visual slice stage, not production.");
            var s=All<FirstTownSliceState>(scene).Single();var nav=All<Unity.AI.Navigation.NavMeshSurface>(scene).Single();
            var doors=All<DoorInteractable>(scene).SelectMany(d=>d.GetComponentsInChildren<Collider>()).Where(c=>c.enabled).ToArray();
            var previous=nav.navMeshData;UnityEngine.AI.NavMeshData candidate=null;
            try
            {
                foreach(var c in doors)c.enabled=false;nav.BuildNavMesh();candidate=nav.navMeshData;
                if(!candidate)throw new InvalidOperationException("No NavMesh data produced.");
                string path=AssetDatabase.GenerateUniqueAssetPath(s.generation+"/Navigation.asset");AssetDatabase.CreateAsset(candidate,path);candidate=null;
                foreach(var c in doors)c.enabled=true;AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            catch {nav.RemoveData();nav.navMeshData=previous;nav.AddData();if(candidate)Object.DestroyImmediate(candidate);throw;}
            finally{foreach(var c in doors)if(c)c.enabled=true;}
        }
        [MenuItem("Last Signal/First Town/Visual Slice/4 Promote ACCEPTED stage with backup")]
        public static void Promote()
        {
            Guard();var scene=SceneManager.GetActiveScene();if(scene.path!=Stage)throw new InvalidOperationException("Open the saved visual slice stage.");
            var s=All<FirstTownSliceState>(scene).Single();
            if(!s.generated || s.sourceHash!=Hash(Source) || s.baselineTerrainHash!=Hash(AssetDatabase.GetAssetPath(s.baselineTerrain)))throw new InvalidOperationException("Source changed or stage incomplete; promotion refused.");
            if(!EditorUtility.DisplayDialog("Owner visual acceptance", "Promote this saved stage only after your visual and gameplay review. A source backup will be retained; GUID is preserved.","Promote accepted stage","Cancel"))return;
            Directory.CreateDirectory(Evidence);string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");string backup=Evidence+"/FirstTownCore-before-slice-"+stamp+".unity";File.Copy(Source,backup,false);
            bool isolated=s.reviewSlotEnabled;
            var save=new SerializedObject(All<LastSignal.Persistence.SaveSession>(scene).Single());
            if(isolated)
            {
                if(save.FindProperty("saveSlot").stringValue!=s.reviewSaveSlot)throw new InvalidOperationException("Review save slot was hand-edited. Reconcile before promotion.");
                save.FindProperty("saveSlot").stringValue=s.productionSaveSlot;save.ApplyModifiedPropertiesWithoutUndo();s.reviewSlotEnabled=false;
            }
            if(!EditorSceneManager.SaveScene(scene,Source,false))
            {
                if(isolated){save.FindProperty("saveSlot").stringValue=s.reviewSaveSlot;save.ApplyModifiedPropertiesWithoutUndo();s.reviewSlotEnabled=true;}
                throw new IOException("Promotion failed; backup: "+backup);
            }
            Folder(Root+"/Archives");string error=AssetDatabase.MoveAsset(Stage,Root+"/Archives/Accepted-"+stamp+".unity");if(!string.IsNullOrEmpty(error))Debug.LogWarning(error);
            Debug.Log("Accepted stage promoted; original scene backup: "+backup+". No tests or benchmarks executed by authoring.");
        }
        [MenuItem("Last Signal/First Town/Visual Slice/3b Isolate save slot before manual play")]
        public static void IsolateReviewSave()
        {
            Guard();var scene=SceneManager.GetActiveScene();if(scene.path!=Stage)throw new InvalidOperationException("Open the visual slice stage.");
            var s=All<FirstTownSliceState>(scene).Single();if(s.reviewSlotEnabled){Debug.Log("Review slot already isolated: "+s.reviewSaveSlot);return;}
            var save=new SerializedObject(All<LastSignal.Persistence.SaveSession>(scene).Single());
            s.productionSaveSlot=save.FindProperty("saveSlot").stringValue;s.reviewSaveSlot="first-town-art-review-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            save.FindProperty("saveSlot").stringValue=s.reviewSaveSlot;save.ApplyModifiedPropertiesWithoutUndo();s.reviewSlotEnabled=true;
            EditorUtility.SetDirty(s);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Manual review uses scratch save slot "+s.reviewSaveSlot+". World ID, content version and entity IDs unchanged. Promotion restores production slot "+s.productionSaveSlot+".");
        }
    }
}
