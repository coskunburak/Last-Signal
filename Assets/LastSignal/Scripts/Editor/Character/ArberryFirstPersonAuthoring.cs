using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace LastSignal.Editor
{
    /// <summary>Deterministic local skin partition and weapon binding. No verification gates or Play mode.</summary>
    public static class ArberryFirstPersonAuthoring
    {
        public const string Root=ArberryCharacterAuthoring.Root;
        public const string ArmsPath=Root+"/Prefabs/Arberry_FirstPersonArms.prefab";
        public static readonly string[] Keys={"Rifle","DXT","Crowbar","Machete","WoodenAxe"};
        public static readonly string[] Fields={"startingWeapon","secondaryWeapon","startingMelee","fastMelee","heavyMelee"};
        public static readonly string[] Originals={
            "Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab",
            "Assets/LastSignal/Art/TriplePistols/DXT/FPS.prefab",
            "Assets/LastSignal/Prefabs/Combat/Crowbar/Crowbar_Viewmodel.prefab",
            "Assets/LastSignal/Prefabs/Combat/S021/Machete_Viewmodel.prefab",
            "Assets/LastSignal/Prefabs/Combat/S021/WoodenAxe_Viewmodel.prefab"};
        public static string WeaponPath(string key)=>Root+"/Prefabs/FPS_"+key+".prefab";
        static float rightPalmDepth,leftPalmDepth;
        static T Save<T>(T value,string path) where T:Object
        {
            value.name=Path.GetFileNameWithoutExtension(path);var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old){EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);return old;}
            AssetDatabase.CreateAsset(value,path);return value;
        }
        [MenuItem("Last Signal/Character/Arberry/7 Author actual FPS arms and all weapon bindings")]
        public static void Author()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before authoring");
            Directory.CreateDirectory(Root+"/Meshes");AssetDatabase.Refresh();MeasurePalms();
            ArberryPresentationAuthoring.RebuildPistolFingers(rightPalmDepth);CreateArms();
            File.WriteAllText(ArberryCharacterAuthoring.Docs+"/fps-contact-authoring.txt","Local geometric contact fitting; not an executed test or visual acceptance. Distances are metres.\n");
            for(int i=0;i<Keys.Length;i++)Weapon(i);
            // Additional starting variants use the same gameplay pistol identity and complete own arm rig.
            foreach(string key in new[]{"Glock17","Custom9mm"})Weapon(1,key);
            var world=PrefabUtility.LoadPrefabContents(ArberryCharacterAuthoring.BodyPath);
            try {
                var obsolete=world.GetComponent<CharacterViewmodelPalette>();if(obsolete)Object.DestroyImmediate(obsolete);
                var rig=new SerializedObject(world.GetComponent<CharacterWeaponRig>());rig.FindProperty("rightPalmDepth").floatValue=rightPalmDepth;rig.FindProperty("leftPalmDepth").floatValue=leftPalmDepth;rig.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(world,ArberryCharacterAuthoring.BodyPath);
            }
            finally {PrefabUtility.UnloadPrefabContents(world);}
            var player=PrefabUtility.LoadPrefabContents(DieselCharacterAuthoring.PlayerPath);
            try{ConfigurePlayer(player,true);PrefabUtility.SaveAsPrefabAsset(player,DieselCharacterAuthoring.PlayerPath);}
            finally{PrefabUtility.UnloadPrefabContents(player);}
        }
        static void MeasurePalms()
        {
            var source=PrefabUtility.LoadPrefabContents(ArberryCharacterAuthoring.BodyPath);var mesh=new Mesh();
            try{
                var skin=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s=>s.name=="Gloves");skin.BakeMesh(mesh);
                var vertices=mesh.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();var triangles=mesh.triangles;
                var a=source.GetComponent<Animator>();
                foreach(bool left in new[]{false,true}){
                    var hand=a.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                    var middle=a.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
                    var index=a.GetBoneTransform(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal);
                    var little=a.GetBoneTransform(left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal);
                    Vector3 origin=Vector3.Lerp(hand.position,middle.position,.65f);
                    Vector3 direction=Vector3.Cross(index.position-little.position,(middle.position-hand.position).normalized).normalized*(left?1:-1);
                    float distance=float.PositiveInfinity;
                    for(int i=0;i<triangles.Length;i+=3){
                        Vector3 v=vertices[triangles[i]],e1=vertices[triangles[i+1]]-v,e2=vertices[triangles[i+2]]-v;
                        Vector3 h=Vector3.Cross(direction,e2);float determinant=Vector3.Dot(e1,h);if(Mathf.Abs(determinant)<1e-9f)continue;
                        float inverse=1/determinant;Vector3 offset=origin-v;float u=inverse*Vector3.Dot(offset,h);if(u<0||u>1)continue;
                        Vector3 q=Vector3.Cross(offset,e1);float w=inverse*Vector3.Dot(direction,q);if(w<0||u+w>1)continue;
                        float t=inverse*Vector3.Dot(e2,q);if(t>0&&t<distance)distance=t;
                    }
                    if(!float.IsFinite(distance)||distance>.06f)throw new InvalidOperationException("Cannot locate "+(left?"left":"right")+" glove palm surface from anatomical landmarks");
                    if(left)leftPalmDepth=distance;else rightPalmDepth=distance;
                }
                File.WriteAllText(ArberryCharacterAuthoring.Docs+"/palm-surface-authoring.txt",$"Local ray/triangle glove-surface measurement from wrist/knuckle landmarks.\nRight palm depth={rightPalmDepth:F6} m; left palm depth={leftPalmDepth:F6} m.\nNo source geometry exported. Contact deformation acceptance NOT_RUN.\n");
            }finally{Object.DestroyImmediate(mesh);PrefabUtility.UnloadPrefabContents(source);}
        }
        public static void ConfigurePlayer(GameObject player,bool arberry)
        {
            var so=new SerializedObject(player.GetComponent<PlayerCombatController>());
            for(int i=0;i<Fields.Length;i++){
                string path=arberry?WeaponPath(Keys[i]):Originals[i];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(!prefab)throw new InvalidOperationException("Missing FPS asset: "+path);
                so.FindProperty(Fields[i]).objectReferenceValue=i<2?(Object)prefab.GetComponent<WeaponController>():prefab.GetComponent<MeleeWeaponController>();
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static bool ArmBone(Transform bone)
        {
            for(var t=bone;t;t=t.parent)if(t.name=="upperarm_l"||t.name=="upperarm_r")return true;
            return false;
        }
        static float ArmWeight(BoneWeight w,bool[] allowed)=>(allowed[w.boneIndex0]?w.weight0:0)+(allowed[w.boneIndex1]?w.weight1:0)+(allowed[w.boneIndex2]?w.weight2:0)+(allowed[w.boneIndex3]?w.weight3:0);
        static Mesh Partition(SkinnedMeshRenderer skin,int submesh,bool crop)
        {
            var source=skin.sharedMesh;var weights=source.boneWeights;var allowed=skin.bones.Select(ArmBone).ToArray();var triangles=source.GetTriangles(submesh);var selected=new List<int>();
            for(int i=0;i<triangles.Length;i+=3){if(crop&&(ArmWeight(weights[triangles[i]],allowed)<.65f||ArmWeight(weights[triangles[i+1]],allowed)<.65f||ArmWeight(weights[triangles[i+2]],allowed)<.65f))continue;selected.Add(triangles[i]);selected.Add(triangles[i+1]);selected.Add(triangles[i+2]);}
            if(selected.Count==0)throw new InvalidOperationException("Empty FPS partition: "+skin.name);
            var indices=selected.Distinct().OrderBy(x=>x).ToArray();var remap=indices.Select((v,i)=>new{v,i}).ToDictionary(x=>x.v,x=>x.i);
            var mesh=new Mesh{indexFormat=indices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};var vertices=source.vertices;var normals=source.normals;var tangents=source.tangents;var uv=source.uv;
            mesh.vertices=indices.Select(i=>vertices[i]).ToArray();mesh.normals=indices.Select(i=>normals[i]).ToArray();
            if(tangents.Length==source.vertexCount)mesh.tangents=indices.Select(i=>tangents[i]).ToArray();mesh.uv=indices.Select(i=>uv[i]).ToArray();
            // Preserve weighted deformation and UVs. Only triangle/vertex membership changes; original asset is never edited.
            mesh.bindposes=source.bindposes;mesh.boneWeights=indices.Select(i=>weights[i]).ToArray();mesh.SetTriangles(selected.Select(i=>remap[i]).ToArray(),0);mesh.RecalculateBounds();
            return Save(mesh,Root+"/Meshes/FPS_"+skin.name+".asset");
        }
        static void CreateArms()
        {
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ArberryCharacterAuthoring.SourcePath));root.name="Arberry_FirstPersonArms";
            try{
                foreach(var animator in root.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(animator);
                var report=new System.Text.StringBuilder("# FPS local partition authoring\n\nOriginal binary files untouched. No protected geometry/pixels exported to a model.\n\n");
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                    string material=null;int submesh=0;bool crop=true;
                    if(skin.name=="Jacket")material="Jacket1";else if(skin.name=="Gloves"){material="Gloves1";crop=false;}else if(skin.name=="Body3"){material="Arms";submesh=2;}
                    if(material==null){Object.DestroyImmediate(skin.gameObject);continue;}
                    skin.sharedMesh=Partition(skin,submesh,crop);skin.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+material+".mat")};
                    skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=true;skin.updateWhenOffscreen=false;skin.quality=SkinQuality.Bone4;
                    skin.localBounds=new Bounds(skin.rootBone.InverseTransformPoint(new Vector3(0,1.2f,.3f)),Vector3.one*4);
                    report.AppendLine($"- {skin.name}: {skin.sharedMesh.vertexCount} vertices, {skin.sharedMesh.triangles.Length/3} triangles; material {material}; source bone/bindpose indices retained.");
                }
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
                PrefabUtility.SaveAsPrefabAsset(root,ArmsPath);File.WriteAllText(ArberryCharacterAuthoring.Docs+"/fps-partition-authoring.md",report.ToString());
            }finally{Object.DestroyImmediate(root);}
        }
        static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
        static Transform New(Transform parent,string name,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        static void Weapon(int index,string variant=null)
        {
            string key=variant??Keys[index];string source=variant==null?Originals[index]:"Assets/LastSignal/Art/TriplePistols/"+variant+"/FPS.prefab";
            var root=PrefabUtility.LoadPrefabContents(source);
            try{
                var profile=AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Root+"/Profiles/"+key+".asset");
                // A calibrated pistol profile is the source of truth on regeneration.
                if (!(profile.Pistol && profile.FpsTwoHandedPistol)) {
                    profile.FpsWeaponOwnsContacts=true;profile.FpsRightShoulder=new Vector3(.19f,-.18f,-.06f);profile.FpsLeftShoulder=new Vector3(-.19f,-.18f,-.06f);
                    profile.FpsOffhandPosition=new Vector3(-.23f,-.32f,.16f);profile.FpsPistolFingers=profile.WorldPistolFingers;
                    profile.RightElbow=new Vector3(.31f,-.17f,.07f);profile.LeftElbow=new Vector3(-.31f,-.17f,.15f);
                    profile.FpsRightElbow=new Vector3(.34f,-.35f,.05f);profile.FpsLeftElbow=new Vector3(-.34f,-.35f,.08f);
                    // Camera-space targets based on .5502m Arberry arms. Grip rotation comes from actual sockets.
                    if(index==0){profile.FpsHipPosition=new Vector3(.025f,-.195f,.31f);profile.FpsHipEuler=new Vector3(3,-2,0);profile.SightDistance=.22f;profile.PistolMagazineTravel=.12f;}
                    else if(index==1){profile.FpsHipPosition=new Vector3(.10f,-.18f,.32f);profile.FpsHipEuler=new Vector3(3,-5,0);profile.FpsRaisedPosition=new Vector3(.09f,-.13f,.34f);profile.FpsRaisedEuler=new Vector3(1,-2,0);profile.SightDistance=.32f;}
                    else if(index==4){profile.FpsHipPosition=new Vector3(.12f,-.02f,.32f);profile.FpsHipEuler=new Vector3(8,-8,-38);}
                    else{profile.FpsHipPosition=new Vector3(.13f,-.20f,.29f);profile.FpsHipEuler=index==2?new Vector3(35,10,-20):new Vector3(22,0,-25);}
                }
                EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
                var frame=Find(root,index<2?"ViewmodelRoot":"VisualRoot");
                var grip=root.GetComponent<FirstPersonWeaponGrip>();if(!grip)grip=root.AddComponent<FirstPersonWeaponGrip>();
                Transform visual;CharacterWeaponSockets sockets;
                if(index==0){
                    visual=New(frame,"Arberry_RifleVisual",Vector3.zero);var rifle=Find(root,"MRPoly_RifleBody");var mag=Find(root,"MRPoly_Magazine");
                    rifle.SetParent(visual,false);rifle.localPosition=Vector3.zero;rifle.localRotation=Quaternion.identity;rifle.localScale=Vector3.one;rifle.gameObject.SetActive(true);rifle.GetComponent<Renderer>().enabled=true;
                    mag.SetParent(visual,false);mag.localPosition=new Vector3(.055f,.13f,0);mag.localRotation=Quaternion.identity;mag.localScale=Vector3.one;mag.gameObject.SetActive(true);mag.GetComponent<Renderer>().enabled=true;
                    sockets=visual.gameObject.AddComponent<CharacterWeaponSockets>();DieselCharacterAuthoring.ConfigureRifleSockets(sockets);sockets.Magazine=mag;sockets.WeaponVisualRoot=visual;
                    // Preserve the existing scope/lens and VFX references in the detached rifle model.
                    var ctrl=new SerializedObject(root.GetComponent<WeaponController>());
                    sockets.Muzzle=(Transform)ctrl.FindProperty("muzzle").objectReferenceValue;sockets.AimReference=(Transform)ctrl.FindProperty("aimReference").objectReferenceValue;
                    sockets.ReloadReference.SetParent(mag,true);
                }else if(index==2){
                    visual=Object.Instantiate(profile.WorldPrefab,frame).transform;visual.name="Arberry_CrowbarVisual";sockets=visual.GetComponent<CharacterWeaponSockets>();
                    var old=Find(root,"GripAnchor");old.gameObject.SetActive(false);
                }else{visual=grip.Visual;sockets=grip.Sockets;}
                // Retain legacy objects for serialized compatibility, but never render/evaluate their arm rig.
                foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))Object.DestroyImmediate(r);
                foreach(var a in root.GetComponentsInChildren<Animator>(true)){a.enabled=false;a.runtimeAnimatorController=null;}
                var animation=root.GetComponent<WeaponAnimationPresenter>();if(animation)animation.enabled=false;
                var arms=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArmsPath),root.transform);
                arms.transform.localPosition=new Vector3(0,-1.62f,-.03f);arms.transform.localRotation=Quaternion.identity;arms.transform.localScale=Vector3.one;
                grip.Profile=profile;grip.PoseFrame=frame;grip.Visual=visual;grip.Sockets=sockets;
                grip.AnatomicalRoot=arms.transform;
                grip.RightPalmDepth=rightPalmDepth;grip.LeftPalmDepth=leftPalmDepth;
                grip.RightUpper=Find(arms,"upperarm_r");grip.RightLower=Find(arms,"lowerarm_r");grip.RightHand=Find(arms,"hand_r");
                grip.LeftUpper=Find(arms,"upperarm_l");grip.LeftLower=Find(arms,"lowerarm_l");grip.LeftHand=Find(arms,"hand_l");
                grip.RightFingers=Fingers(arms,false);grip.LeftFingers=Fingers(arms,true);
                grip.RightFingerRest=grip.RightFingers.Select(t=>t.localRotation).ToArray();grip.LeftFingerRest=grip.LeftFingers.Select(t=>t.localRotation).ToArray();grip.AuthoredFingerCurl=false;
                var mappings=new List<CharacterWeaponRig.PresentationOverride>();
                foreach(string name in new[]{"DXT","Glock17","Custom9mm"})mappings.Add(new CharacterWeaponRig.PresentationOverride{Source=AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>("Assets/LastSignal/Art/TriplePistols/"+name+"/Pose.asset"),Character=AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Root+"/Profiles/"+name+".asset")});
                grip.ConfigureOverrides(mappings.ToArray());
                if(index==1)CharacterWeaponSockets.Attach(visual,sockets.PrimaryGrip,frame.position,frame.rotation*Quaternion.Euler(0,0,90));
                if(index<2)root.GetComponent<WeaponViewPresenter>().SetGripProfile(profile);
                else{
                    frame.localPosition=Vector3.zero;frame.localRotation=Quaternion.identity;frame.localScale=Vector3.one;
                    var presenter=new SerializedObject(root.GetComponent<MeleeWeaponPresenter>());presenter.FindProperty("gripProfile").objectReferenceValue=profile;presenter.FindProperty("animator").objectReferenceValue=null;presenter.ApplyModifiedPropertiesWithoutUndo();
                    var stance=new SerializedObject(root.GetComponent<MeleeStanceViewPresenter>());stance.FindProperty("wideFovPositionOffset").vector3Value=Vector3.zero;stance.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
                foreach(var t in arms.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
                // Do not translate a manually calibrated two-hand pistol on regeneration.
                if (!profile.FpsTwoHandedPistol) FitContacts(grip,key,index<2);
                PrefabUtility.SaveAsPrefabAsset(root,WeaponPath(key));
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void FitContacts(FirstPersonWeaponGrip grip,string key,bool firearm)
        {
            var p=grip.Profile;var frame=grip.PoseFrame;var visual=grip.Visual;var s=grip.Sockets;
            var framePosition=frame.localPosition;var frameRotation=frame.localRotation;
            var visualPosition=visual.localPosition;var visualRotation=visual.localRotation;
            var magazinePosition=s.Magazine?s.Magazine.localPosition:Vector3.zero;
            var right=new CharacterHandPose(grip.RightHand,grip.RightFingers,false,grip.RightPalmDepth,false);
            var left=new CharacterHandPose(grip.LeftHand,grip.LeftFingers,true,grip.LeftPalmDepth,false);
            float rightReach=(Vector3.Distance(grip.RightUpper.position,grip.RightLower.position)+Vector3.Distance(grip.RightLower.position,grip.RightHand.position))*p.MaximumReach-.012f;
            float leftReach=(Vector3.Distance(grip.LeftUpper.position,grip.LeftLower.position)+Vector3.Distance(grip.LeftLower.position,grip.LeftHand.position))*p.MaximumReach-.012f;
            Vector3 correction=Vector3.zero;float remaining=0;
            try{
                for(int iteration=0;iteration<80;iteration++){
                    remaining=0;
                    for(int sample=0;sample<9;sample++){
                        Vector3 position=p.FpsHipPosition+correction;Quaternion rotation=Quaternion.Euler(p.FpsHipEuler);
                        if(!firearm){p.AttackPose(sample/8f,out var motion,out var turn);position+=motion;rotation*=turn;}
                        else if(sample==7)position+=Vector3.down*.10f; // Equip lowering.
                        else if(sample==8)position+=new Vector3(0,-.11f,-.07f); // Existing slide offset.
                        if(firearm){frame.SetLocalPositionAndRotation(position,rotation);visual.SetLocalPositionAndRotation(visualPosition,visualRotation);}
                        else{frame.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);CharacterWeaponSockets.Attach(visual,s.PrimaryGrip,frame.TransformPoint(position),frame.rotation*rotation*Quaternion.Euler(0,0,90));}
                        right.WristPose(s.PrimaryGrip.position,s.PrimaryGrip.rotation,out var rw,out _);
                        Vector3 delta=rw-p.FpsRightShoulder;float excess=Mathf.Max(0,delta.magnitude-rightReach);remaining=Mathf.Max(remaining,excess);correction-=delta.normalized*excess*.5f;
                        if(p.TwoHandedMelee||p.TwoHandedFirearm||(p.Pistol&&sample==6)){
                            var contact=p.Pistol?s.ReloadReference:s.SecondaryGrip;
                            if(!contact)continue;
                            left.WristPose(contact.position,contact.rotation,out var lw,out _);delta=lw-p.FpsLeftShoulder;excess=Mathf.Max(0,delta.magnitude-leftReach);remaining=Mathf.Max(remaining,excess);correction-=delta.normalized*excess*.5f;
                        }
                    }
                    if(remaining<.0001f)break;
                }
                if(correction.magnitude>.15f||remaining>.005f)throw new InvalidOperationException(key+" needs manual contact authoring; required offset="+correction+" residual="+remaining);
                p.FpsHipPosition+=correction;
                if(p.Pistol)p.FpsRaisedPosition+=correction;
                EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);
                File.AppendAllText(ArberryCharacterAuthoring.Docs+"/fps-contact-authoring.txt",key+": ready translation correction="+correction.ToString("F4")+" right/left available reach="+rightReach.ToString("F4")+"/"+leftReach.ToString("F4")+"; fit residual="+remaining.ToString("F6")+". ADS/animation/render gates NOT_RUN.\n");
            }finally{frame.SetLocalPositionAndRotation(framePosition,frameRotation);visual.SetLocalPositionAndRotation(visualPosition,visualRotation);if(s.Magazine)s.Magazine.localPosition=magazinePosition;}
        }
        static Transform[] Fingers(GameObject root,bool left)
        {string[] names={"thumb","index","middle","ring","pinky"};return names.SelectMany(n=>Enumerable.Range(1,3).Select(i=>Find(root,n+"_0"+i+(left?"_l":"_r")))).ToArray();}
    }
}
