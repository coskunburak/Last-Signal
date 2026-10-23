using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.EditorTools
{
    /// <summary>Bounded, persistent authoring of the three supplied packages. No tests, builds or Play mode.</summary>
    public static class TriplePistolAuthoring
    {
        public const string Folder = "Assets/LastSignal/Art/TriplePistols/";
        const string Sources = "Assets/ThirdParty/Weapons/PISTOLS/";
        const string Template = "Assets/LastSignal/Prefabs/Combat/S021/Weapon_TacticalPistol.prefab";
        static readonly string[] Names = { "DXT", "Glock17", "Custom9mm" };
        static readonly string[] Ids = { PistolVisualVariant.DxtId, PistolVisualVariant.GlockId, PistolVisualVariant.CustomId };
        static readonly string[] Source = { "DXT_ready/dxt_ready.fbx", "Glock17/Glock17.fbx", "pistol-9mm-custom/source/CustomPistol9mmPack.fbx" };
        static readonly Vector3[] Origins = { new Vector3(-.034f,-.043f,-.04084f), new Vector3(0,-.003f,.002f), new Vector3(0,.060f,.224f) };
        static readonly float[] Scales = { 1.8f, 3.1f, 1.1f };
        static readonly Vector3[] Grips = { new Vector3(.013f,.025f,-.009f), new Vector3(.014f,.013f,-.006f), new Vector3(.015f,.022f,-.004f) };
        static readonly Vector3[] Sights = { new Vector3(-.043f,.0032f,-.04084f), new Vector3(0,.022f,.010f), new Vector3(0,.131f,.246f) };
        static readonly Vector3[] Muzzles = { new Vector3(.056f,-.006f,-.04084f), new Vector3(0,.016f,-.051f), new Vector3(0,.116f,.084f) };
        static Matrix4x4 Normalization(int i) => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0,i==0?-90:180,0),Vector3.one*Scales[i]) * Matrix4x4.Translate(-Origins[i]);

        [MenuItem("Last Signal/Combat/Author Triple Pistol Integration")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before authoring.");
            for(int i=0;i<3;i++) if(!AssetDatabase.LoadAssetAtPath<GameObject>(Sources+Source[i])) throw new InvalidOperationException("Missing source: "+Source[i]);
            var variants = new PistolVisualVariant[3];
            for(int i=0;i<3;i++)
            {
                Directory.CreateDirectory(Folder+Names[i]); AssetDatabase.Refresh();
                var material = Material(Names[i]);
                var magazineMaterial = i==2 ? Material("CustomMagazine") : material;
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(Folder+Names[i]+"/Visual.prefab");
                if(!visual) visual=Visual(i,material,magazineMaterial);
                var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Folder+Names[i]+"/Pose.asset");
                if(!profile)
                {
                    profile=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>("Assets/LastSignal/Data/Combat/S021/PistolPose.asset"));
                    profile.name=Names[i]+"Pose"; profile.WorldScale=1; profile.WorldPrefab=visual;
                    profile.FpsHipPosition += new Vector3(i*.003f,0,-i*.004f);
                    profile.FpsRaisedPosition += new Vector3(0,0,-i*.003f);
                    profile.SightDistance=.34f+i*.005f;
                    profile.PistolMagazineTravel=i==0?.105f:i==1?.095f:.10f;
                    profile.RecoilDistance=i==0?.009f:.008f; profile.RecoilPitch=i==0?2.8f:2.6f;
                    profile.FpsPistolFingers=null;profile.WorldPistolFingers=null;
                    AssetDatabase.CreateAsset(profile,Folder+Names[i]+"/Pose.asset");
                }
                variants[i]=AssetDatabase.LoadAssetAtPath<PistolVisualVariant>(Folder+Names[i]+"/Variant.asset");
                if(!variants[i])
                {
                    variants[i]=ScriptableObject.CreateInstance<PistolVisualVariant>();
                    variants[i].Id=Ids[i]; variants[i].DisplayName=new[]{"DXT · 9 mm görünüm","Glock17 · 9 mm görünüm","Custom 9mm"}[i];
                    variants[i].SourcePath=Sources+Source[i];variants[i].VisualPrefab=visual;variants[i].Pose=profile;
                    AssetDatabase.CreateAsset(variants[i],Folder+Names[i]+"/Variant.asset");
                }
            }
            for(int i=0;i<3;i++) AuthorFps(i,variants);
            var player=PrefabUtility.LoadPrefabContents("Assets/LastSignal/Prefabs/Player/Player.prefab");
            try
            {
                var serialized=new SerializedObject(player.GetComponent<PlayerCombatController>());
                serialized.FindProperty("secondaryWeapon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"DXT/FPS.prefab").GetComponent<WeaponController>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player,"Assets/LastSignal/Prefabs/Player/Player.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            AuthorDryFireBindings();
            AssetDatabase.SaveAssets();
            Debug.Log("Three pistol variants authored and production player linked. User verification NOT_RUN.");
        }
        public static void AuthorDryFireBindings()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
            var catalog=AssetDatabase.LoadAssetAtPath<LastSignal.Audio.AudioCatalog>("Assets/LastSignal/Audio/Config/S015AudioCatalog.asset");
            var cue=catalog?catalog.Get(LastSignal.Audio.AudioCue.DryFire):null;
            if(cue==null||cue.clips==null||cue.clips.Length==0||!cue.clips[0])throw new InvalidOperationException("Missing S015 dry-fire source.");
            foreach(var name in Names)
            {
                string path=Folder+name+"/FPS.prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var audio=new SerializedObject(root.GetComponent<WeaponAudioPresenter>());
                    if(!audio.FindProperty("dryFireClip").objectReferenceValue)
                    {audio.FindProperty("dryFireClip").objectReferenceValue=cue.clips[0];audio.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);}
                }
                finally {PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name)
        {
            string folder=Folder+name+"/";
            string path=folder+"PBR.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing)return existing;
            var material=new Material(Shader.Find("HDRP/Lit"));
            foreach(var map in new[]{"BaseColor","Normal","MetallicSmoothness","AO"})
            {
                string texturePath=folder+"Textures/"+map+".png";
                if(!File.Exists(texturePath)){if(map=="AO")continue;throw new InvalidOperationException("Run local texture authoring first: "+texturePath);}
                AssetDatabase.ImportAsset(texturePath);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.sRGBTexture=map=="BaseColor";importer.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.isReadable=false;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.SaveAndReimport();
                if(map=="BaseColor" || map=="Normal") material.SetTexture(map=="BaseColor"?"_BaseColorMap":"_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }
            // Owned linear inputs: R metallic, G separate AO, B neutral detail, A smoothness.
            string metal=folder+"Textures/MetallicSmoothness.png", ao=folder+"Textures/AO.png";
            bool hasAo=File.Exists(ao);
            var mask=global::LastSignal.Editor.Rendering.HdrpMaterialMaskUtility.WritePacked(folder+"Textures/HDRPMask.png",metal,hasAo?ao:"","","",2048,false);
            global::LastSignal.Editor.Rendering.HdrpMaterialMaskUtility.Apply(material,mask,true,true,1,1,1,hasAo,Vector2.one,Vector2.zero);
            material.SetFloat("_NormalScale",1);
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(material);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static GameObject Visual(int i,Material material,Material magazineMaterial)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Sources+Source[i]);
            var groups=new Dictionary<string,List<CombineInstance>>();
            var temporary=new List<Mesh>();
            var normalize=Normalization(i);
            foreach(var renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                var filter=renderer.GetComponent<MeshFilter>();var skin=renderer as SkinnedMeshRenderer;
                var mesh=filter?filter.sharedMesh:skin?skin.sharedMesh:null;if(!mesh)continue;
                if(i==0)
                {
                    var weights=mesh.boneWeights;var triangles=mesh.triangles;
                    var parts=new Dictionary<int,List<int>>();
                    for(int n=0;n<triangles.Length;n+=3)
                    {
                        int bone=weights[triangles[n]].boneIndex0;
                        // Rigid mechanical source. A mixed triangle stays with the frame to avoid tearing it apart.
                        if(weights[triangles[n+1]].boneIndex0!=bone||weights[triangles[n+2]].boneIndex0!=bone)bone=0;
                        if(!parts.ContainsKey(bone))parts[bone]=new List<int>();
                        parts[bone].AddRange(new[]{triangles[n],triangles[n+1],triangles[n+2]});
                    }
                    foreach(var pair in parts)
                    {
                        var part=Object.Instantiate(mesh);part.subMeshCount=1;part.SetTriangles(pair.Value,0);temporary.Add(part);
                        string bone=skin.bones[pair.Key].name;
                        string key=bone=="slide"?"Slide":bone=="mag"?"Magazine":bone=="trigger"?"Trigger":bone=="hammer"?"Hammer":"Frame";
                        Add(groups,key,part,normalize*renderer.transform.localToWorldMatrix);
                    }
                }
                else if(i==1) Add(groups,renderer.name=="Glock17"?"Frame":renderer.name,mesh,normalize*renderer.transform.localToWorldMatrix);
                else
                {
                    int n=Suffix(renderer.name,"CustomPistol9mm");
                    int mag=Suffix(renderer.name,"Magazine9mm");
                    if(n>=0&&n<23)
                    {
                        string key=n==16||n==17?"Trigger":new[]{0,6,7,9,12,13}.Contains(n)?"Slide":"Frame";
                        Add(groups,key,mesh,normalize*renderer.transform.localToWorldMatrix);
                    }
                    else if(mag>=0&&mag<4)Add(groups,"Magazine",mesh,normalize*renderer.transform.localToWorldMatrix);
                }
            }
            var root=new GameObject(Names[i]+"_WeaponRoot");
            try
            {
                root.layer=30;
                var sockets=root.AddComponent<CharacterWeaponSockets>();var mechanics=root.AddComponent<PistolMechanics>();
                foreach(var group in groups)
                {
                    var mesh=new Mesh { name=Names[i]+"_"+group.Key };
                    mesh.CombineMeshes(group.Value.ToArray(),true,true);
                    Compact(mesh);mesh.RecalculateBounds();
                    // Root-normalized vertices, then center the real mechanical pivot.
                    Vector3 pivot=group.Key=="Frame"?Vector3.zero:mesh.bounds.center;
                    if(group.Key=="Trigger"||group.Key=="Hammer")pivot=MechanicalPivot(i,group.Key);
                    var vertices=mesh.vertices;for(int j=0;j<vertices.Length;j++)vertices[j]-=pivot;mesh.vertices=vertices;mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh,Folder+Names[i]+"/"+group.Key+".asset");
                    var part=new GameObject(group.Key);part.layer=30;part.transform.SetParent(root.transform,false);part.transform.localPosition=pivot;
                    part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=group.Key=="Magazine"?magazineMaterial:material;
                    if(group.Key=="Magazine")sockets.Magazine=part.transform;
                    if(group.Key=="Slide")mechanics.Slide=part.transform;
                    if(group.Key=="Trigger")mechanics.Trigger=part.transform;
                    if(group.Key=="Hammer")mechanics.Hammer=part.transform;
                }
                sockets.RightGrip=Socket(root.transform,"PrimaryGrip",Grips[i]);sockets.PalmContact=sockets.RightGrip;
                sockets.PrimaryGrip.localRotation=Quaternion.LookRotation(Vector3.forward,Vector3.left);
                sockets.AimReference=Socket(root.transform,"AimReference",normalize.MultiplyPoint3x4(Sights[i]));
                sockets.Muzzle=Socket(root.transform,"Muzzle",normalize.MultiplyPoint3x4(Muzzles[i]));
                sockets.ReloadReference=Socket(sockets.Magazine,"ReloadReference",new Vector3(-.018f,-.025f,0));
                sockets.ReloadReference.localRotation=Quaternion.LookRotation(Vector3.forward,Vector3.right);
                sockets.WeaponVisualRoot=root.transform;sockets.SilhouetteReference=sockets.Muzzle;
                mechanics.EjectionPort=Socket(root.transform,"EjectionPort",new Vector3(.018f,sockets.Muzzle.localPosition.y,-.005f));
                mechanics.EjectionPort.localRotation=Quaternion.Euler(0,90,0);
                mechanics.SlideTravel=new Vector3(0,0,i==0?-.031f:i==1?-.028f:-.027f);
                return PrefabUtility.SaveAsPrefabAsset(root,Folder+Names[i]+"/Visual.prefab");
            }
            finally {Object.DestroyImmediate(root);foreach(var mesh in temporary)Object.DestroyImmediate(mesh);}
        }
        static Vector3 MechanicalPivot(int i,string name)
        {
            if(name=="Hammer")return Normalization(i).MultiplyPoint3x4(new Vector3(-.04821f,-.01349f,-.04092f));
            var pivots=new[]{new Vector3(-.00895f,-.02096f,-.04092f),new Vector3(0,.00934f,-.014f),new Vector3(0,.0892f,.178f)};
            return Normalization(i).MultiplyPoint3x4(pivots[i]);
        }
        public static void AuthorMechanicalPivots()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
            for(int i=0;i<3;i++)foreach(var name in i==0?new[]{"Trigger","Hammer"}:new[]{"Trigger"})
            {
                string folder=Folder+Names[i]+"/";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Visual.prefab");
                var part=prefab.transform.Find(name);var desired=MechanicalPivot(i,name);var delta=desired-part.localPosition;
                if(delta.sqrMagnitude<1e-12f)continue;
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+name+".asset");
                var vertices=mesh.vertices;for(int j=0;j<vertices.Length;j++)vertices[j]-=delta;
                mesh.vertices=vertices;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                foreach(var path in new[]{folder+"Visual.prefab",folder+"FPS.prefab"})
                {
                    var root=PrefabUtility.LoadPrefabContents(path);
                    try {var visual=root.GetComponent<FirstPersonWeaponGrip>()?.Visual??root.transform;
                        visual.Find(name).localPosition=desired;PrefabUtility.SaveAsPrefabAsset(root,path);}
                    finally {PrefabUtility.UnloadPrefabContents(root);}
                }
            }
            AssetDatabase.SaveAssets();
        }
        static void Compact(Mesh mesh)
        {
            var triangles=mesh.triangles;var used=triangles.Distinct().ToArray();
            var map=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)map.Add(used[i],i);
            var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var tangents=mesh.tangents;
            mesh.boneWeights=Array.Empty<BoneWeight>();mesh.bindposes=Array.Empty<Matrix4x4>();
            mesh.Clear(false);mesh.vertices=used.Select(i=>vertices[i]).ToArray();
            if(normals.Length==vertices.Length)mesh.normals=used.Select(i=>normals[i]).ToArray();
            if(uv.Length==vertices.Length)mesh.uv=used.Select(i=>uv[i]).ToArray();
            if(tangents.Length==vertices.Length)mesh.tangents=used.Select(i=>tangents[i]).ToArray();
            mesh.triangles=triangles.Select(i=>map[i]).ToArray();mesh.RecalculateBounds();
        }
        // One-time upgrade of derivatives authored before unused source vertices were compacted.
        public static void CompactDxtDerivatives()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
            var shifts=new Dictionary<string,Vector3>();
            foreach(var name in new[]{"Frame","Slide","Magazine","Trigger","Hammer"})
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"DXT/"+name+".asset");if(!mesh)continue;
                Compact(mesh);Vector3 shift=name=="Frame"?Vector3.zero:mesh.bounds.center;
                var v=mesh.vertices;for(int j=0;j<v.Length;j++)v[j]-=shift;mesh.vertices=v;mesh.RecalculateBounds();
                shifts[name]=shift;EditorUtility.SetDirty(mesh);
            }
            foreach(var path in new[]{Folder+"DXT/Visual.prefab",Folder+"DXT/FPS.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var visual=root.GetComponent<FirstPersonWeaponGrip>()?.Visual ?? root.transform;
                    foreach(var item in shifts){var part=visual.Find(item.Key);if(part)part.localPosition+=item.Value;}
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally {PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
        }
        static int Suffix(string name,string prefix)
        {if(name==prefix)return 0;if(name.StartsWith(prefix+".",StringComparison.Ordinal)&&int.TryParse(name.Substring(prefix.Length+1),out int n))return n;return -1;}
        static void Add(Dictionary<string,List<CombineInstance>> groups,string name,Mesh mesh,Matrix4x4 matrix)
        {if(!groups.ContainsKey(name))groups[name]=new List<CombineInstance>();groups[name].Add(new CombineInstance{mesh=mesh,transform=matrix});}
        static Transform Socket(Transform parent,string name,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        static void AuthorFps(int i,PistolVisualVariant[] variants)
        {
            string path=Folder+Names[i]+"/FPS.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path))return;
            var root=PrefabUtility.LoadPrefabContents(Template);
            try
            {
                var grip=root.GetComponent<FirstPersonWeaponGrip>();var old=grip.Visual;
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(variants[i].VisualPrefab,grip.PoseFrame);
                PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
                var sockets=visual.GetComponent<CharacterWeaponSockets>();
                var vfx=root.GetComponent<WeaponVfxPresenter>();if(vfx)vfx.BindPistolSockets(sockets,visual.GetComponent<PistolMechanics>().EjectionPort);
                Object.DestroyImmediate(old.gameObject);
                grip.Visual=visual.transform;grip.Sockets=sockets;grip.Profile=variants[i].Pose;
                CharacterWeaponSockets.Attach(visual.transform,sockets.PrimaryGrip,grip.PoseFrame.position,grip.PoseFrame.rotation*Quaternion.Euler(0,0,90));
                var ctrl=new SerializedObject(root.GetComponent<WeaponController>());
                ctrl.FindProperty("muzzle").objectReferenceValue=sockets.Muzzle;ctrl.FindProperty("aimReference").objectReferenceValue=sockets.AimReference;ctrl.ApplyModifiedPropertiesWithoutUndo();
                root.GetComponent<WeaponViewPresenter>().SetGripProfile(variants[i].Pose);
                var presenter=root.AddComponent<PistolVariantPresenter>();presenter.Variants=variants;presenter.InitialId=Ids[i];
                if(!variants[i].Pose.FpsPistolFingers)
                    variants[i].Pose.FpsPistolFingers=Fingers(i,"FPS",grip.RightFingers,new CharacterHandPose(grip.RightHand,grip.RightFingers,false,.012f,false),visual.transform,sockets);
                if(!variants[i].Pose.WorldPistolFingers)
                {
                    var body=PrefabUtility.LoadPrefabContents("Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab");
                    try
                    {
                        var animator=body.GetComponentInChildren<Animator>(true);var fingers=new Transform[15];
                        var names=new[]{"Thumb","Index","Middle","Ring","Little"};var segments=new[]{"Proximal","Intermediate","Distal"};
                        for(int f=0;f<5;f++)for(int j=0;j<3;j++)fingers[f*3+j]=animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),"Right"+names[f]+segments[j]));
                        variants[i].Pose.WorldPistolFingers=Fingers(i,"Diesel",fingers,new CharacterHandPose(animator,false),visual.transform,sockets);
                    }
                    finally {PrefabUtility.UnloadPrefabContents(body);}
                }
                CharacterWeaponSockets.Attach(visual.transform,sockets.PrimaryGrip,grip.PoseFrame.position,grip.PoseFrame.rotation*Quaternion.Euler(0,0,90));
                EditorUtility.SetDirty(variants[i].Pose);
                root.name="Weapon_"+Names[i];PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        static PistolFingerPose Fingers(int model,string rig,Transform[] fingers,CharacterHandPose palm,Transform visual,CharacterWeaponSockets sockets)
        {
            string path=Folder+Names[model]+"/Fingers_"+rig+".asset";
            var pose=AssetDatabase.LoadAssetAtPath<PistolFingerPose>(path);if(pose)return pose;
            pose=ScriptableObject.CreateInstance<PistolFingerPose>();pose.Rig=rig+" / "+Names[model]+" / 15 right joints";
            var rest=fingers.Select(x=>x.localRotation).ToArray();var tips=new Vector3[15];
            for(int j=0;j<15;j++)tips[j]=fingers[j].InverseTransformPoint(j%3<2?fingers[j+1].position:fingers[j].childCount>0?fingers[j].GetChild(0).position:fingers[j].position+(fingers[j].position-fingers[j-1].position)*.75f);
            var position=visual.localPosition;var rotation=visual.localRotation;
            CharacterWeaponSockets.Attach(visual,sockets.PrimaryGrip,palm.PalmPosition,palm.PalmRotation);
            // Distinct measured grip widths/depths drive recalculation on each actual skeleton.
            float width=new[]{.026f,.029f,.030f}[model],depth=new[]{.038f,.042f,.045f}[model];
            var directions=new[]{new Vector3(-width,.018f,.030f),new Vector3(-.002f,-.003f,.022f),new Vector3(.001f,-.002f,.024f),
                new Vector3(0,.002f,depth),new Vector3(-.002f,-.001f,.021f),new Vector3(-.004f,-.001f,.019f),
                new Vector3(-width,0,.018f),new Vector3(-.002f,0,-depth*.65f),new Vector3(.002f,0,-.020f),
                new Vector3(-width*.95f,0,.018f),new Vector3(-.003f,0,-depth*.60f),new Vector3(.002f,0,-.018f),
                new Vector3(-width*.55f,0,.021f),new Vector3(-width*.4f,0,-.010f),new Vector3(.002f,0,-.014f)};
            if(rig=="Diesel")directions[0]=new Vector3(-width*.85f,.003f,.045f);
            try
            {
                for(int j=0;j<15;j++)
                {
                    Aim(fingers[j],tips[j],visual.TransformDirection(directions[j]));
                    pose.GripRelative[j]=Quaternion.Inverse(rest[j])*fingers[j].localRotation;pose.TriggerRelative[j]=pose.GripRelative[j];
                    pose.AuthoredEndpoints[j]=visual.InverseTransformPoint(fingers[j].TransformPoint(tips[j]));
                }
                var trigger=new[]{new Vector3(-width*.65f,-.004f,depth*.85f),new Vector3(-width*.5f,-.003f,-.013f),new Vector3(.003f,0,-.019f)};
                for(int j=3;j<6;j++){Aim(fingers[j],tips[j],visual.TransformDirection(trigger[j-3]));pose.TriggerRelative[j]=Quaternion.Inverse(rest[j])*fingers[j].localRotation;}
            }
            finally {for(int j=0;j<15;j++)fingers[j].localRotation=rest[j];visual.localPosition=position;visual.localRotation=rotation;}
            AssetDatabase.CreateAsset(pose,path);return pose;
        }
        static void Aim(Transform bone,Vector3 endpoint,Vector3 direction)
        {bone.rotation=Quaternion.FromToRotation(bone.TransformPoint(endpoint)-bone.position,direction)*bone.rotation;}
    }
}
