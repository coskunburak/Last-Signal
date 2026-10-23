using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastSignal.Gathering;
using LastSignal.Persistence;
using LastSignal.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    /// <summary>Explicit owner invocation only. Never invoked on import, play, reload or by runtime code.</summary>
    public static class TreePhysicsAuthoring
    {
        [MenuItem("Last Signal/BLD/P01/S004/Validate Current Physics Stage")]
        public static void ValidateCurrentStage()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop play and compilation before stage validation.");
            var scene=SceneManager.GetActiveScene();
            if(!scene.IsValid() || scene.isDirty || SceneManager.sceneCount!=1 ||
                !scene.path.StartsWith("Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_",StringComparison.Ordinal))
                throw new InvalidOperationException("Open exactly one saved generated FirstTownTreePhysics scene; the FirstTownHarvest source is not a physical stage.");
            var nodes=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResourceNodeView>(true)).Where(n=>n.physicsBody).ToArray();
            if(nodes.Length!=1) throw new InvalidOperationException("Expected exactly one explicitly authored physical test tree; found "+nodes.Length+".");
            var node=nodes[0]; var body=node.physicsBody;
            if(!body.Valid || !body.gameObject.activeInHierarchy || !body.topMesh.GetComponent<Renderer>().enabled ||
                !body.stumpMesh.GetComponent<Renderer>().enabled || (node.standingVisual && node.standingVisual.activeSelf) ||
                !node.world || !node.world.manifest || !node.world.manifest.Valid)
                throw new InvalidOperationException("Physical tree references, visibility or manifest are incomplete.");
            var entry=node.world.manifest.anchors.Single(a=>a.nodeId==node.nodeId);
            if(!entry.definition.physicalTree || !body.profile.Supports(TreeStructure.CreateCut(entry.definition)))
                throw new InvalidOperationException("Tree still has a legacy definition/profile; regenerate the stage from the S003 source.");
            Selection.activeGameObject=node.world.gameObject;
            Debug.Log((node.world.clearanceApproved?"S004_STAGE_VALIDATED":"S004_STAGE_REVIEW_REQUIRED")+
                " scene="+scene.path+" node="+node.nodeId+" position="+node.transform.position+" cutLevels="+body.profile.StageCount+
                " clearanceApproved="+node.world.clearanceApproved+" audioApproved="+(body.audioBank&&body.audioBank.mappingApproved)+
                " TEST_NOT_RUN VISUAL_NOT_APPROVED PERFORMANCE_NOT_MEASURED");
        }
        [MenuItem("Last Signal/BLD/P01/S004/Create Single Tree Physics Stage")]
        public static void CreateStage()
        {
            CreateStage(false);
        }
        // Explicit automation entry; retains all source, selection and unsaved-work guards.
        public static void CreateStageForSelectedTree() => CreateStage(true);

        internal static void CreateStage(bool useSelection)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop play and compilation first.");
            var scene=SceneManager.GetActiveScene();
            if(!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                OpenSourceStageForSelection();
                return;
            }
            if(SceneManager.sceneCount!=1)
            {
                var openScenes=Enumerable.Range(0,SceneManager.sceneCount)
                    .Select(i=>SceneManager.GetSceneAt(i))
                    .Select(s=>(string.IsNullOrEmpty(s.path)?s.name:s.path)+(s.isDirty?" [unsaved changes]":""));
                throw new InvalidOperationException("S004_MULTIPLE_SCENES: Expected exactly one open scene; found "+SceneManager.sceneCount+
                    ". Save any intentional work and close the other scenes manually. Open scenes: "+string.Join("; ",openScenes));
            }
            if(!IsSourceStagePath(scene.path))
            {
                // An owner retry may begin in the isolated draft left by a failed generation.
                // Recover the original source instead of treating the derivative as new input.
                if(!useSelection && scene.path.StartsWith("Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_",StringComparison.Ordinal) &&
                    scene.path.EndsWith(".unity",StringComparison.Ordinal))
                {
                    OpenSourceStageForSelection();
                    return;
                }
                throw new InvalidOperationException("S004_WRONG_SOURCE_SCENE: Active scene is '"+scene.path+
                    "'. Open the existing FirstTownHarvest staging scene from Assets/LastSignal/Scenes/Validation. Do not rename a production or generated physics scene to bypass this check.");
            }
            if(scene.isDirty)
            {
                if(!EditorUtility.DisplayDialog("S004 — Kaynak sahneyi kaydet",
                    "Kaynak sahnede kaydedilmemiş değişiklikler var:\n\n"+scene.path+
                    "\n\nKaydet ve devam et, bu sahnedeki mevcut değişiklikleri kaydeder; ardından ayrı bir fizik test sahnesi oluşturma işlemine devam eder."+
                    "\n\nDeğişiklikleri önce incelemek için İptal'i seçin. İptal, değişiklikleri silmez.",
                    "Kaydet ve devam et","İptal")) return;
                if(!EditorSceneManager.SaveScene(scene) || scene.isDirty)
                {
                    Debug.LogError("S004_SOURCE_SAVE_FAILED: Could not save '"+scene.path+
                        "'. Physics stage generation has not started; review the scene and retry.");
                    return;
                }
            }
            var selected=Selection.gameObjects.Select(g=>g.GetComponent<ResourceNodeView>()).Where(v=>v).ToArray();
            if(!useSelection || selected.Length!=1)
            {
                ShowSourceNodePicker(scene);
                return;
            }
            var wood=Selection.objects.OfType<Material>().SingleOrDefault();
            if(!wood) wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Art/S013/Production/Materials/Wood.mat");
            if(!RenderPipelineAdapter.IsOpaqueUnclippedStandardLitMaterial(wood) ||
                (RenderPipelineAdapter.IsHDRP && wood.shader.name != RenderPipelineAdapter.HdrpLitShaderName) ||
                (RenderPipelineAdapter.IsURP && wood.shader.name != RenderPipelineAdapter.UrpLitShaderName))
                throw new InvalidOperationException("Select one S003 ResourceNodeView root. The production Wood material or selected override must be opaque, unclipped Lit for the active render pipeline (HDRP/Lit under HDRP).");
            var node=selected[0];
            if(node.gameObject.scene!=scene)
                throw new InvalidOperationException("S004_SELECTION_OUTSIDE_SOURCE: Select a ResourceNodeView root in the active FirstTownHarvest scene Hierarchy, not a Project prefab or Prefab Mode object.");
            if(!node.world || !node.world.manifest || !node.world.manifest.Valid || node.physicsBody || !node.standingVisual)
                throw new InvalidOperationException("Selected node must be an intact authored S003 node.");
            var filters=node.standingVisual.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length!=1 || node.standingVisual.GetComponentInChildren<SkinnedMeshRenderer>(true))
                throw new InvalidOperationException("Pilot supports one static two-submesh Pine/Birch renderer. Other topology requires separate review.");
            var source=filters[0]; var renderer=source.GetComponent<MeshRenderer>();
            if(!source.sharedMesh||!source.sharedMesh.isReadable||source.sharedMesh.subMeshCount!=2||!renderer||renderer.sharedMaterials.Length!=2||
                source.sharedMesh.vertexCount>100000||!renderer.sharedMaterials[0].name.ToLowerInvariant().Contains("bark")||
                !renderer.sharedMaterials[1].name.ToLowerInvariant().Contains("leav"))
                throw new InvalidOperationException("Expected readable bark[0]/leaves[1] geometry. Original mesh/importer will not be changed.");
            var anchor=node.world.manifest.anchors.Single(a=>a.nodeId==node.nodeId);
            if(anchor.cellId!="resident") throw new InvalidOperationException("First prototype is resident only; streamed-cell authoring is a later verification gate.");
            var originalPath=scene.path; var hash=Hash(originalPath); var id=node.nodeId;
            var stamp=DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
            var folder="Assets/LastSignal/Data/Gathering/Physics-"+stamp;
            AssetDatabase.CreateFolder("Assets/LastSignal/Data/Gathering","Physics-"+stamp);
            var stage="Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_"+stamp+".unity";
            if(!EditorSceneManager.SaveScene(scene,stage,true)||Hash(originalPath)!=hash) throw new InvalidOperationException("Source copy failed.");
            scene=EditorSceneManager.OpenScene(stage,OpenSceneMode.Single);
            node=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResourceNodeView>(true)).Single(v=>v.nodeId==id);
            source=node.standingVisual.GetComponentInChildren<MeshFilter>(true); renderer=source.GetComponent<MeshRenderer>();
            // Bake original instance transform into new project-owned meshes. Stable anchor remains unchanged.
            var matrix=node.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
            if(node.transform.lossyScale!=Vector3.one || Vector3.Dot(node.transform.up,Vector3.up)<.999f)
                throw new InvalidOperationException("Pilot anchor must be unit scale and upright. Source visual scale may differ.");
            var bark=Extract(source.sharedMesh,0,matrix); var leaves=Extract(source.sharedMesh,1,matrix);
            const float cut=.9f;
            Mesh top=null, stump=null;
            try
            {
                PrepareTrunk(bark,cut,out top,out stump);
                var cap=top.GetTriangles(1); var vertices=top.vertices;
                var capBounds=new Bounds(vertices[cap[0]],Vector3.zero); foreach(int i in cap) capBounds.Encapsulate(vertices[i]);
                float radius=(capBounds.extents.x+capBounds.extents.z)*.5f;
                if(radius<.04f||radius>1.5f||new Vector2(capBounds.center.x,capBounds.center.z).magnitude>radius*.25f ||
                    Mathf.Max(capBounds.extents.x,capBounds.extents.z)/Mathf.Min(capBounds.extents.x,capBounds.extents.z)>1.6f)
                    throw new InvalidOperationException("Cut section is off-axis/noncircular; requires topology-specific authoring.");
                var profile=ScriptableObject.CreateInstance<TreePhysicsProfile>();
                profile.radius=radius; profile.cutHeight=cut; profile.height=bark.bounds.max.y;
                // Tapered trunk approximation, plus 12% crown/branch mass. Values scale with selected tree dimensions.
                profile.mass=Mathf.Clamp(550*Mathf.PI*radius*radius*(profile.height-cut)*.42f*1.12f,20,10000);
                profile.centerOfMass=new Vector3(leaves.bounds.center.x*.12f,cut+(profile.height-cut)*.36f,leaves.bounds.center.z*.12f);
                profile.intactTop=SaveMesh(top,folder+"/Trunk.asset"); top=null;
                profile.intactStump=SaveMesh(stump,folder+"/Stump.asset"); stump=null;
                SaveMesh(leaves,folder+"/Foliage.asset");
                profile.cutDepths=Enumerable.Range(1,TreeStructure.ProgressiveLevels).Select(TreeStructure.ProgressiveDepth).ToArray();
                profile.cutTops=new Mesh[TreeStructure.Sides*profile.StageCount]; profile.cutStumps=new Mesh[profile.cutTops.Length];
                for(int side=0;side<TreeStructure.Sides;side++) for(int level=1;level<=profile.StageCount;level++)
                {
                    float depth=profile.cutDepths[level-1];
                    BuildNotch(profile.intactTop,profile.intactStump,radius,cut,side,depth,out var upper,out var lower);
                    int index=side*profile.StageCount+level-1;
                    profile.cutTops[index]=SaveMesh(upper,folder+"/Top-"+side+"-"+level+".asset");
                    profile.cutStumps[index]=SaveMesh(lower,folder+"/Stump-"+side+"-"+level+".asset");
                }
                if(!profile.Valid) throw new InvalidOperationException("Generated geometry/profile failed validation. Source remains intact; inspect isolated draft.");
                AssetDatabase.CreateAsset(profile,folder+"/Physics.asset");
                var bank=ScriptableObject.CreateInstance<TreeAudioBank>();
                bank.auditionCandidates=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/ThirdParty/Audio/Tree"})
                    .Select(g=>AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                ConfigureValidationAudio(bank);
                AssetDatabase.CreateAsset(bank,folder+"/Audio-Audition.asset");
                var original=node.standingVisual; // Keep visible until the physical replacement is fully validated.
                var bodyGo=new GameObject("Physical trunk — owner QA pending"); bodyGo.transform.SetParent(node.transform,false);
                bodyGo.layer=node.gameObject.layer; bodyGo.SetActive(false);
                var staticBark=new Material(renderer.sharedMaterials[0]); staticBark.name="Physical bark";
                staticBark.EnableKeyword("_LS_FOREST_STATIC"); AssetDatabase.CreateAsset(staticBark,folder+"/Bark-Static.asset");
                var topFilter=Visual(bodyGo.transform,"Bark",profile.intactTop,new[]{staticBark,wood});
                var canopy=Visual(bodyGo.transform,"Canopy",leaves,new[]{renderer.sharedMaterials[1]});
                var oldLod=original.GetComponent<LODGroup>();
                if(oldLod && oldLod.lodCount==1)
                {
                    var lod=bodyGo.AddComponent<LODGroup>(); lod.fadeMode=oldLod.fadeMode; lod.animateCrossFading=oldLod.animateCrossFading;
                    var sourceLod=oldLod.GetLODs()[0];
                    lod.SetLODs(new[]{new LOD(sourceLod.screenRelativeTransitionHeight,new Renderer[]{topFilter.GetComponent<Renderer>(),canopy.GetComponent<Renderer>()}) {fadeTransitionWidth=sourceLod.fadeTransitionWidth}});
                    lod.RecalculateBounds();
                }
                var stumpGo=new GameObject("Root stump"); stumpGo.transform.SetParent(node.transform,false);
                stumpGo.layer=node.gameObject.layer;
                var stumpFilter=Visual(stumpGo.transform,"Capped stump",profile.intactStump,new[]{staticBark,wood});
                var stumpCollider=stumpGo.AddComponent<BoxCollider>(); stumpCollider.size=new Vector3(radius*1.4f,cut,radius*1.4f);
                stumpCollider.center=Vector3.up*(cut*.5f);
                var trunk=bodyGo.AddComponent<CapsuleCollider>(); trunk.radius=radius;
                trunk.height=Mathf.Max(radius*2,profile.height*.55f-cut); trunk.center=Vector3.up*(cut+trunk.height*.5f);
                var upperTrunk=bodyGo.AddComponent<CapsuleCollider>(); upperTrunk.radius=radius*.45f;
                upperTrunk.height=profile.height*.4f; upperTrunk.center=Vector3.up*(profile.height*.55f+upperTrunk.height*.5f);
                var rb=bodyGo.AddComponent<Rigidbody>(); rb.isKinematic=true; rb.mass=profile.mass;
                var physical=bodyGo.AddComponent<TreePhysicsBody>(); physical.node=node; physical.profile=profile; physical.audioBank=bank;
                physical.topMesh=topFilter; physical.stumpMesh=stumpFilter; physical.trunkCollider=trunk; physical.stumpCollider=stumpCollider;
                physical.chips=Particles(node.transform,"Bark chips",renderer.sharedMaterials[0],.025f,new Color(.35f,.22f,.1f));
                physical.dust=Particles(node.transform,"Contact dust",renderer.sharedMaterials[0],.09f,new Color(.38f,.32f,.22f));
                node.physicsBody=physical;
                ActivatePhysicalProjection(physical);
                original.SetActive(false);
                node.gameObject.name="S004 PHYSICS TEST TREE — "+node.gameObject.name;
                var manifest=UnityEngine.Object.Instantiate(node.world.manifest);
                manifest.manifestId="harvest-physics:"+Guid.NewGuid().ToString("N"); manifest.revision=1;
                manifest.sourceScene=originalPath; manifest.sourceSha256=hash;
                var a=manifest.anchors.Single(v=>v.nodeId==id);
                var definition=UnityEngine.Object.Instantiate(a.definition); definition.physicalTree=true; definition.revision++;
                // Preserve the selected definition's resistance/efficiency and derive cutting effort
                // from its configured values and actual trunk radius. No fixed hit-count release.
                float radiusScale=Mathf.Clamp(radius/.25f,.5f,3);
                definition.health=Mathf.Clamp(Mathf.RoundToInt(definition.health*radiusScale),definition.strikeDamage,10000);
                // Separate definition ID if another selected-family node still uses the original S003 behavior.
                definition.definitionId+=".physics";
                AssetDatabase.CreateAsset(definition,folder+"/Resource.asset"); a.definition=definition;
                AssetDatabase.CreateAsset(manifest,folder+"/Manifest.asset"); node.world.manifest=manifest;
                node.world.clearanceApproved=false;
                var save=node.world.GetComponent<SaveSession>(); var so=new SerializedObject(save);
                so.FindProperty("saveSlot").stringValue="tree_physics_"+stamp; so.ApplyModifiedPropertiesWithoutUndo();
                var approach=CreateTestApproach(node,profile);
                var flowSettings=new SerializedObject(node.world.GetComponent<SessionFlow>());
                flowSettings.FindProperty("spawnPoint").objectReferenceValue=approach;
                flowSettings.ApplyModifiedPropertiesWithoutUndo();
                // Prefab contains the stable anchor and visual derivative; world link is scene-owned.
                var boundWorld=node.world; node.world=null;
                try
                {
                    if(!PrefabUtility.SaveAsPrefabAsset(node.gameObject,folder+"/HarvestableTree.prefab"))
                        throw new InvalidOperationException("Physical prefab could not be saved; inspect the isolated draft.");
                }
                finally { node.world=boundWorld; }
                File.WriteAllText(folder+"/AUTHORING.txt","S004 owner-authored physics stage\nSource: "+originalPath+"\nSource SHA256: "+hash+
                    "\nStage: "+stage+"\nNode: "+id+"\nRoot position: "+node.transform.position.ToString("F5")+
                    "\nTest spawn: "+approach.position.ToString("F5")+"\nProfile: "+folder+"/Physics.asset\nRadius/mass: "+radius+" / "+profile.mass+
                    "\nCut levels: "+profile.StageCount+"\nPenetration per full-condition hit: "+definition.PhysicalPenetration+
                    "\nWood interior: "+AssetDatabase.GetAssetPath(wood)+"\nAudio: Editor validation preview segments; mappingApproved=false"+
                    "\nClearance: owner review required\nTEST_NOT_RUN VISUAL_NOT_APPROVED PERFORMANCE_NOT_MEASURED\n");
                foreach(var generated in new UnityEngine.Object[]{profile,bank,manifest,definition,staticBark}) AssetDatabase.SaveAssetIfDirty(generated);
                AssetDatabase.ImportAsset(folder+"/AUTHORING.txt");
                if(!EditorSceneManager.SaveScene(scene)||Hash(originalPath)!=hash) throw new InvalidOperationException("Stage/source fingerprint mismatch.");
                Debug.Log("S004_STAGE_PREPARED scene="+stage+" assets="+folder+" sourceSHA256="+hash+" TEST_NOT_RUN VISUAL_NOT_APPROVED PERFORMANCE_NOT_MEASURED");
                Selection.activeGameObject=node.gameObject;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bark);
                if(top) UnityEngine.Object.DestroyImmediate(top); if(stump) UnityEngine.Object.DestroyImmediate(stump);
                if(leaves && !AssetDatabase.Contains(leaves)) UnityEngine.Object.DestroyImmediate(leaves);
            }
        }
        public static void ConfigureValidationAudio(TreeAudioBank bank)
        {
            const string path="Assets/LastSignal/Audio/S004/ValidationPreview/";
            bank.woodHit=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"WoodHit.wav");
            bank.barkFracture=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"BarkFracture.wav");
            bank.structuralCrack=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"StructuralCrack.wav");
            bank.creak=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"Creak.wav");
            bank.fracture=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"JointFailure.wav");
            bank.fallingMotion=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"FallingMotion.wav");
            bank.groundImpact=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"GroundImpact.wav");
            bank.branchContact=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"BranchContact.wav");
            bank.settling=AssetDatabase.LoadAssetAtPath<AudioClip>(path+"Settling.wav");
            bank.previewInValidationScene=true;
        }
        public static void ActivatePhysicalProjection(TreePhysicsBody physical)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before authoring a physical projection.");
            if(!physical) throw new ArgumentNullException(nameof(physical));
            if(physical.Body) physical.Body.isKinematic=true;
            // Collider.attachedRigidbody is a live physics binding. Register the assembled body
            // before checking it, while retaining the original visual until validation succeeds.
            physical.gameObject.SetActive(true);
            Physics.SyncTransforms();
            if(physical.Valid) return;
            var reasons=new System.Collections.Generic.List<string>();
            if(!physical.Body) reasons.Add("missing Rigidbody");
            if(!physical.node) reasons.Add("missing node");
            if(!physical.profile || !physical.profile.Valid) reasons.Add("invalid profile");
            if(!physical.topMesh || !physical.topMesh.sharedMesh || !physical.topMesh.GetComponent<Renderer>()) reasons.Add("incomplete top mesh/renderer");
            if(!physical.stumpMesh || !physical.stumpMesh.sharedMesh || !physical.stumpMesh.GetComponent<Renderer>()) reasons.Add("incomplete stump mesh/renderer");
            if(!(physical.trunkCollider is CapsuleCollider) || physical.trunkCollider.isTrigger) reasons.Add("invalid trunk collider");
            if(!physical.stumpCollider || physical.stumpCollider.isTrigger) reasons.Add("invalid stump collider");
            if(physical.trunkCollider && physical.trunkCollider.attachedRigidbody!=physical.Body) reasons.Add("trunk Rigidbody binding mismatch");
            if(physical.stumpCollider && physical.stumpCollider.attachedRigidbody) reasons.Add("stump must be static");
            if(physical.node && physical.transform.parent!=physical.node.transform) reasons.Add("wrong node parent");
            if(physical.transform.lossyScale!=Vector3.one || (physical.node && physical.node.transform.lossyScale!=Vector3.one)) reasons.Add("non-unit scale");
            physical.gameObject.SetActive(false);
            throw new InvalidOperationException("S004_PHYSICAL_PROJECTION_INCOMPLETE: "+string.Join("; ",reasons)+
                ". Source visual retained. Inspect the isolated draft.");
        }

        static bool IsSourceStagePath(string path) => !string.IsNullOrEmpty(path) &&
            path.StartsWith("Assets/LastSignal/Scenes/Validation/FirstTownHarvest_",StringComparison.Ordinal) &&
            path.EndsWith(".unity",StringComparison.Ordinal);

        static Transform CreateTestApproach(ResourceNodeView node,TreePhysicsProfile profile)
        {
            Physics.SyncTransforms();
            for(int side=0;side<TreeStructure.Sides;side++)
            {
                var p=node.transform.position+node.transform.TransformDirection(TreeStructure.Direction(side))*(profile.radius+3);
                if(!Physics.Raycast(p+Vector3.up*8,Vector3.down,out var hit,20,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore) ||
                    hit.normal.y<.75f || hit.collider.transform.IsChildOf(node.transform)) continue;
                var feet=hit.point+Vector3.up*.08f;
                if(Physics.CheckCapsule(feet+Vector3.up*.4f,feet+Vector3.up*1.45f,.35f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) continue;
                var marker=new GameObject("S004 Test Spawn — faces physical tree").transform;
                marker.position=feet;
                var facing=node.transform.position-feet; facing.y=0;
                marker.rotation=Quaternion.LookRotation(facing,Vector3.up);
                return marker;
            }
            throw new InvalidOperationException("No clear grounded test approach within three metres of this tree. Select a different resident tree; source scene is unchanged.");
        }

        static void OpenSourceStageForSelection()
        {
            // Owner-invoked recovery from an empty/untitled scene or a generated S004 draft.
            // Never rename/save the current scene as an S003 source, or silently discard its contents.
            var path=AssetDatabase.GetAssetPath(Selection.activeObject);
            if(!IsSourceStagePath(path))
            {
                var candidates=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/LastSignal/Scenes/Validation"})
                    .Select(AssetDatabase.GUIDToAssetPath).Where(IsSourceStagePath).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
                if(candidates.Length==1) path=candidates[0];
                else
                {
                    var absolute=EditorUtility.OpenFilePanel("Choose the existing S003 FirstTownHarvest source scene",
                        Path.GetFullPath("Assets/LastSignal/Scenes/Validation"),"unity");
                    if(string.IsNullOrEmpty(absolute)) return;
                    path=FileUtil.GetProjectRelativePath(absolute);
                }
            }
            if(!IsSourceStagePath(path) || !AssetDatabase.LoadAssetAtPath<SceneAsset>(path))
            {
                EditorUtility.DisplayDialog("S004 source scene",
                    "Choose an existing FirstTownHarvest_*.unity scene under Assets/LastSignal/Scenes/Validation. No scene was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var sourceScene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            Debug.Log("S004_SOURCE_OPENED: "+path+". Choose the resident tree in the S004 selection window.");
            ShowSourceNodePicker(sourceScene);
        }

        static void ShowSourceNodePicker(Scene scene)
        {
            var nodes=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResourceNodeView>(true))
                .Where(n=>n && n.world && n.world.manifest && n.world.manifest.Valid && !n.physicsBody && n.standingVisual &&
                    n.world.manifest.anchors.Any(a=>a.nodeId==n.nodeId && a.cellId=="resident"))
                .OrderBy(n=>n.nodeId,StringComparer.Ordinal).ToArray();
            if(nodes.Length==0)
            {
                EditorUtility.DisplayDialog("S004 tree selection",
                    "No intact resident S003 ResourceNodeView was found in '"+scene.path+"'. The source scene has not been modified.","OK");
                return;
            }
            TreeSourcePickerWindow.Open(scene.path,nodes);
        }

        public static Mesh Extract(Mesh source,int submesh,Matrix4x4 matrix)
        {
            if(!source || !source.isReadable || submesh<0 || submesh>=source.subMeshCount || matrix.determinant<=0)
                throw new ArgumentException("Invalid geometry or mirrored transform.");
            ValidateMesh(source);
            var v=source.vertices; var n=source.normals; var uv=source.uv;
            if(n.Length!=v.Length||uv.Length!=v.Length) throw new ArgumentException("Normals/UV0 required.");
            var indices=source.GetTriangles(submesh); var map=new System.Collections.Generic.Dictionary<int,int>();
            var vertices=new System.Collections.Generic.List<Vector3>(); var normals=new System.Collections.Generic.List<Vector3>();
            var uvs=new System.Collections.Generic.List<Vector2>(); var colors=new System.Collections.Generic.List<Color>(); var sourceColors=source.colors;
            var normalMatrix=matrix.inverse.transpose;
            for(int i=0;i<indices.Length;i++)
            {
                int old=indices[i];
                if(!map.TryGetValue(old,out int next))
                { next=vertices.Count; map.Add(old,next); vertices.Add(matrix.MultiplyPoint3x4(v[old])); normals.Add(normalMatrix.MultiplyVector(n[old]).normalized); uvs.Add(uv[old]); if(sourceColors.Length==v.Length) colors.Add(sourceColors[old]); }
                indices[i]=next;
            }
            var mesh=new Mesh { name=source.name+"-submesh-"+submesh,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uvs); if(colors.Count>0) mesh.SetColors(colors);
            mesh.SetTriangles(indices,0); mesh.RecalculateBounds(); mesh.RecalculateTangents(); return mesh;
        }
        public static void PrepareTrunk(Mesh bark,float cutHeight,out Mesh top,out Mesh stump)
        {
            ValidateMesh(bark);
            if(!float.IsFinite(cutHeight) || cutHeight<=bark.bounds.min.y || cutHeight>=bark.bounds.max.y)
                throw new ArgumentException("Cut plane must lie inside the bark bounds.");
            Split(bark,Vector3.up,Vector3.up*cutHeight,out top,out stump);
            ValidateCap(top); ValidateCap(stump);
        }
        public static void BuildNotch(Mesh top,Mesh stump,float radius,float cutHeight,int side,float depth,out Mesh upper,out Mesh lower)
        {
            if(!float.IsFinite(radius)||radius<=0||!float.IsFinite(depth)||depth<=0||depth>.975f||side<0||side>=TreeStructure.Sides)
                throw new ArgumentException("Invalid localized notch request.");
            var direction=TreeStructure.Direction(side);
            var origin=Vector3.up*cutHeight+direction*(radius*(1-2*depth));
            Split(top,(Vector3.up-direction*.5f).normalized,origin,out upper,out var discardedTop);
            UnityEngine.Object.DestroyImmediate(discardedTop);
            Split(stump,(-Vector3.up-direction*.5f).normalized,origin,out lower,out var discardedBottom);
            UnityEngine.Object.DestroyImmediate(discardedBottom);
            ValidateCap(upper); ValidateCap(lower);
        }
        static void Split(Mesh mesh,Vector3 normal,Vector3 origin,out Mesh upper,out Mesh lower)
        {
            MeshSlicer.Slice(new FragmentData(mesh),normal,origin,Vector2.one,Vector2.zero,out var a,out var b);
            upper=a.ToMesh(); lower=b.ToMesh(); upper.RecalculateTangents(); lower.RecalculateTangents();
        }
        static void ValidateCap(Mesh mesh)
        {
            if(!mesh||mesh.vertexCount<3||mesh.subMeshCount!=2||mesh.GetIndexCount(1)<3||mesh.normals.Length!=mesh.vertexCount||mesh.uv.Length!=mesh.vertexCount)
                throw new InvalidOperationException("Slicing produced empty/uncapped geometry; stop before gameplay QA.");
            ValidateMesh(mesh);
        }
        public static void ValidateMesh(Mesh mesh)
        {
            if(!mesh || !mesh.isReadable || mesh.vertexCount<3 || mesh.vertexCount>200000 ||
                !TreeStructure.Finite(mesh.bounds.center) || !TreeStructure.Finite(mesh.bounds.size) || mesh.bounds.size.sqrMagnitude<.000001f)
                throw new ArgumentException("Invalid mesh/bounds.");
            var vertices=mesh.vertices; var normals=mesh.normals; var uv=mesh.uv;
            if(normals.Length!=vertices.Length || uv.Length!=vertices.Length) throw new ArgumentException("Missing normals/UV0.");
            for(int i=0;i<vertices.Length;i++)
                if(!TreeStructure.Finite(vertices[i]) || !TreeStructure.Finite(normals[i]) || normals[i].sqrMagnitude<.001f ||
                    !float.IsFinite(uv[i].x) || !float.IsFinite(uv[i].y)) throw new ArgumentException("Invalid vertex attributes.");
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                if(mesh.GetTopology(sub)!=MeshTopology.Triangles) throw new ArgumentException("Triangle topology required.");
                var indices=mesh.GetTriangles(sub);
                if(indices.Length%3!=0) throw new ArgumentException("Invalid triangle count.");
                foreach(int index in indices) if(index<0 || index>=vertices.Length) throw new ArgumentException("Invalid triangle index.");
            }
        }
        static Mesh SaveMesh(Mesh mesh,string path) { mesh.name=Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(mesh,path); return mesh; }
        static MeshFilter Visual(Transform parent,string name,Mesh mesh,Material[] materials)
        { var go=new GameObject(name); go.transform.SetParent(parent,false); var f=go.AddComponent<MeshFilter>(); f.sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterials=materials; return f; }
        static ParticleSystem Particles(Transform parent,string name,Material material,float size,Color color)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); var p=go.AddComponent<ParticleSystem>(); p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main; main.playOnAwake=false; main.loop=false; main.maxParticles=24; main.startLifetime=.7f; main.startSpeed=.7f; main.startSize=size; main.startColor=color; main.gravityModifier=.6f; main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=p.emission; emission.enabled=false; p.GetComponent<ParticleSystemRenderer>().sharedMaterial=material; return p;
        }
        static string Hash(string path)
        { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(); }
    }
}

namespace LastSignal.Editor
{
    public sealed class TreeSourcePickerWindow : EditorWindow
    {
        [SerializeField] string sourcePath;
        [SerializeField] ResourceNodeView[] candidates;
        Vector2 scroll;

        public static void Open(string path,ResourceNodeView[] nodes)
        {
            var window=GetWindow<TreeSourcePickerWindow>(true,"S004 Ağaç Seçimi",true);
            window.sourcePath=path;
            window.candidates=nodes;
            window.minSize=new Vector2(520,300);
            window.ShowUtility();
            window.Focus();
            Debug.Log("S004_TREE_PICKER_OPENED: source="+path+" eligibleTrees="+nodes.Length+
                ". Select a tree and click Seç ve oluştur in the S004 Ağaç Seçimi window.");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Fizik test sahnesi için bir ağaç seç",EditorStyles.boldLabel);
            EditorGUILayout.LabelField(sourcePath??"",EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("Seç ve oluştur, seçilen ağacı ayrı bir FirstTownTreePhysics sahnesinde hazırlar.",MessageType.Info);
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                SceneManager.sceneCount!=1 || SceneManager.GetActiveScene().path!=sourcePath ||
                candidates==null || candidates.Length==0)
            {
                EditorGUILayout.HelpBox("Play modundan çıkın, kaynak sahneyi açın ve Create Single Tree Physics Stage menüsünü yeniden çalıştırın.",MessageType.Warning);
                return;
            }
            scroll=EditorGUILayout.BeginScrollView(scroll);
            ResourceNodeView chosen=null;
            foreach(var candidate in candidates)
            {
                if(!candidate) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(candidate.name,EditorStyles.boldLabel);
                EditorGUILayout.LabelField(candidate.nodeId??"",EditorStyles.wordWrappedLabel);
                if(GUILayout.Button("Seç ve oluştur")) chosen=candidate;
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            if(GUILayout.Button("İptal")) Close();
            if(!chosen) return;
            var expectedSource=sourcePath;
            Close();
            // Leave the GUI event before opening dialogs or replacing the active scene.
            EditorApplication.delayCall+=()=>
            {
                if(!chosen || SceneManager.sceneCount!=1 || SceneManager.GetActiveScene().path!=expectedSource ||
                    chosen.gameObject.scene!=SceneManager.GetActiveScene())
                {
                    Debug.LogWarning("S004_TREE_SELECTION_CHANGED: Reopen Create Single Tree Physics Stage; the source scene or selected tree changed.");
                    return;
                }
                Selection.activeGameObject=chosen.gameObject;
                TreePhysicsAuthoring.CreateStage(true); // Revalidate before generating assets.
            };
        }
    }

    [CustomEditor(typeof(LastSignal.Gathering.TreePhysicsBody))]
    public sealed class TreePhysicsBodyInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var tree=(LastSignal.Gathering.TreePhysicsBody)target;
            EditorGUILayout.HelpBox("Owner verification only. Falling is PhysX-driven. Material, collision, grip and sound quality require Game View evidence.",MessageType.Info);
            if(!Application.isPlaying) return;
            var body=tree.Body; var joint=tree.Connection;
            EditorGUILayout.LabelField("Motion",tree.Motion.ToString());
            EditorGUILayout.LabelField("Cut depth / support",tree.CutDepth.ToString("F4")+" / "+tree.RemainingSupport.ToString("F4"));
            if(body)
            {
                EditorGUILayout.LabelField("Mass / dynamic",body.mass+" kg / "+!body.isKinematic);
                EditorGUILayout.Vector3Field("World center of mass",body.worldCenterOfMass);
                EditorGUILayout.Vector3Field("Angular velocity",body.angularVelocity);
                EditorGUILayout.Vector3Field("Inertia tensor",body.inertiaTensor);
            }
            EditorGUILayout.LabelField("Joint / break torque",joint?joint.breakTorque+" N m":"Released");
            EditorGUILayout.LabelField("Last contact impulse",tree.LastCollisionImpulse.ToString("F2"));
            if(GUILayout.Button("Log current physics evidence (read only)"))
                Debug.Log("S004_PHYSICS node="+(tree.node?tree.node.nodeId:"")+" motion="+tree.Motion+
                    " mass="+(body?body.mass:0)+" dynamic="+(body&&!body.isKinematic)+" COM="+(body?body.worldCenterOfMass:Vector3.zero)+
                    " angularVelocity="+(body?body.angularVelocity:Vector3.zero)+" joint="+(bool)joint+" cutDepth="+tree.CutDepth+
                    " remainingSupport="+tree.RemainingSupport+" contactImpulse="+tree.LastCollisionImpulse);
        }
    }
}
