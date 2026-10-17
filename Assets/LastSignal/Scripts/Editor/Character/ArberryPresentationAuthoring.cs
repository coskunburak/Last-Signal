using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    public static class ArberryPresentationAuthoring
    {
        const string Root=ArberryCharacterAuthoring.Root;
        static void Guard(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Author outside Play mode.");}
        static T Save<T>(T value,string path) where T:Object
        {
            value.name=Path.GetFileNameWithoutExtension(path);
            var current=AssetDatabase.LoadAssetAtPath<T>(path);
            if(current){EditorUtility.CopySerialized(value,current);Object.DestroyImmediate(value);EditorUtility.SetDirty(current);}
            else {AssetDatabase.CreateAsset(value,path);current=value;}
            AssetDatabase.SaveAssetIfDirty(current);return current;
        }
        [MenuItem("Last Signal/Character/Arberry/5 Author anatomical weapon profiles")]
        public static void Profiles()
        {
            Guard();var body=PrefabUtility.LoadPrefabContents(ArberryCharacterAuthoring.BodyPath);
            try {
                var animator=body.GetComponent<Animator>();
                var originals=AssetDatabase.LoadAssetAtPath<GameObject>(DieselCharacterAuthoring.BodyPath).GetComponent<CharacterWeaponRig>().Profiles;
                var sources=originals.Concat(new[]{"DXT","Glock17","Custom9mm"}.Select(n=>AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>("Assets/LastSignal/Art/TriplePistols/"+n+"/Pose.asset"))).ToArray();
                var bindings=new List<CharacterWeaponRig.PresentationOverride>();var profiles=new List<CharacterWeaponPoseProfile>();
                float length=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position,animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position)+Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position,animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                for(int i=0;i<sources.Length;i++) {
                    var source=sources[i];if(!source)throw new InvalidOperationException("Missing source weapon profile");
                    string key=i<5?new[]{"Rifle","Crowbar","Pistol","Machete","WoodenAxe"}[i]:new[]{"DXT","Glock17","Custom9mm"}[i-5];
                    var p=Object.Instantiate(source);p.name="Arberry_"+key;
                    // These positions are actor metres measured from Arberry's anatomical shoulder/chest,
                    // not rotations/offsets copied from Diesel bone space. Weapon contact geometry is reused.
                    p.RightElbow=new Vector3(.31f,-.17f,.07f);p.LeftElbow=new Vector3(-.31f,-.17f,.15f);
                    p.MaximumReach=.97f;p.ShoulderReachDegrees=6;p.ForearmTwistDegrees=45;p.TorsoReachYawDegrees=10;
                    if(i==0){p.GripMode=WeaponGripMode.TwoHandedRifle;p.HipStockOffset=new Vector3(-.12f,.035f,.07f);p.AimStockOffset=new Vector3(-.125f,.065f,.07f);p.TorsoYaw=35;p.HipEuler=new Vector3(14,-8,0);}
                    else {
                        p.HipGripOffset=new Vector3(.015f,-length*.32f,length*.62f);
                        p.AimGripOffset=new Vector3(-.08f,-length*.10f,length*.74f);
                        p.RaisedGripOffset=new Vector3(.015f,-length*.24f,length*.64f);
                        if(i==4){p.WorldMeleeSlideOffset=new Vector3(-.06f,.04f,-.03f);p.MeleeClearanceRotation=12;p.HipGripOffset=new Vector3(-.18f,.015f,length*.70f+.035f);p.TorsoYaw=12;}
                        if(i==3){p.HipGripOffset=new Vector3(.05f,-length*.40f,length*.52f);p.TorsoYaw=10;}
                    }
                    if(p.Pistol)p.WorldPistolFingers=Fingers(animator,p,key,i>=5?i-5:-1);
                    if(i==1){
                        p.WorldPrefab=CrowbarContact(source.WorldPrefab);p.WorldScale=1;
                        p.GripMode=WeaponGripMode.OneHandedHeavyMelee;p.AttackOwnership=WeaponPoseOwnership.ProfileArmTargets;
                        p.HipGripOffset=new Vector3(.04f,-length*.38f,length*.53f);p.HipEuler=new Vector3(35,10,-20);
                        p.WindupOffset=new Vector3(.035f,.09f,-.07f);p.WindupEuler=new Vector3(-30,20,15);
                        p.StrikeOffset=new Vector3(-.04f,-.04f,.015f);p.StrikeEuler=new Vector3(35,-15,-15);
                        p.FollowThroughOffset=new Vector3(-.06f,-.065f,0);p.FollowThroughEuler=new Vector3(45,-20,-20);
                        p.FingerClosure=.8f;
                    }
                    // Keep manually authored TPS support contacts and finger calibration on regeneration.
                    var existing=AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Root+"/Profiles/"+key+".asset");
                    if(p.Pistol && existing && existing.WorldTwoHandedPistol) {
                        p.WorldTwoHandedPistol=true;
                        p.WorldPrefab=existing.WorldPrefab;
                        p.WorldSupportFingerClosure=existing.WorldSupportFingerClosure;
                        p.SecondaryFingerFlexion=existing.SecondaryFingerFlexion;
                        p.SecondaryThumbFlexion=existing.SecondaryThumbFlexion;
                        p.SecondaryThumbOpposition=existing.SecondaryThumbOpposition;
                        p.RightElbow=existing.RightElbow;
                        p.LeftElbow=existing.LeftElbow;
                        p.ElbowTurnDegreesPerSecond=existing.ElbowTurnDegreesPerSecond;
                        p.HipGripOffset=existing.HipGripOffset;
                        p.RaisedGripOffset=existing.RaisedGripOffset;
                        p.AimGripOffset=existing.AimGripOffset;
                    }
                    if(p.Pistol && existing && existing.FpsTwoHandedPistol) {
                        p.FpsTwoHandedPistol=true;
                        p.FpsWeaponOwnsContacts=existing.FpsWeaponOwnsContacts;
                        p.FpsPrimaryPalmOffset=existing.FpsPrimaryPalmOffset;
                        p.FpsSupportPalmOffset=existing.FpsSupportPalmOffset;
                        p.FpsSupportPalmEuler=existing.FpsSupportPalmEuler;
                        p.FpsSupportFingerClosure=existing.FpsSupportFingerClosure;
                        p.FpsHipPosition=existing.FpsHipPosition;p.FpsHipEuler=existing.FpsHipEuler;
                        p.FpsRaisedPosition=existing.FpsRaisedPosition;p.FpsRaisedEuler=existing.FpsRaisedEuler;
                        p.FpsRightElbow=existing.FpsRightElbow;p.FpsLeftElbow=existing.FpsLeftElbow;
                        p.FpsRightShoulder=existing.FpsRightShoulder;p.FpsLeftShoulder=existing.FpsLeftShoulder;
                        p.FpsOffhandPosition=existing.FpsOffhandPosition;
                        p.FpsPistolFingers=existing.FpsPistolFingers;p.SightDistance=existing.SightDistance;
                    }
                    if(existing && existing.FpsSprintCarry) {
                        p.FpsSprintCarry=true;p.FpsSprintPosition=existing.FpsSprintPosition;
                        p.FpsSprintEuler=existing.FpsSprintEuler;p.FpsSprintBobAmplitude=existing.FpsSprintBobAmplitude;
                        p.SprintOffset=existing.SprintOffset;p.SprintEuler=existing.SprintEuler;
                    }
                    var saved=Save(p,Root+"/Profiles/"+key+".asset");bindings.Add(new CharacterWeaponRig.PresentationOverride{Source=source,Character=saved});
                    if(i<5)profiles.Add(saved);
                }
                var rig=body.GetComponent<CharacterWeaponRig>();rig.Configure(profiles.ToArray());rig.ConfigureOverrides(bindings.ToArray());
                var so=new SerializedObject(rig);so.FindProperty("torsoCenterOffset").vector3Value=new Vector3(0,.15f,.015f);so.FindProperty("torsoClearanceRadii").vector3Value=new Vector3(.24f,.30f,.19f);so.ApplyModifiedPropertiesWithoutUndo();
                var oldPalette=body.GetComponent<CharacterViewmodelPalette>();if(oldPalette)Object.DestroyImmediate(oldPalette);
                PrefabUtility.SaveAsPrefabAsset(body,ArberryCharacterAuthoring.BodyPath);
                File.WriteAllText(ArberryCharacterAuthoring.Docs+"/profile-authoring.txt",$"Arberry measured right arm length: {length:F4} m\nEight own world profiles; five default definitions and three identity-based pistol variant overrides.\nOwn bind-relative finger calibration; unchanged rifle/pistol/axe/machete source weapon contacts.\nCrowbar: own contact transferred from the existing FPS anatomical palm through the shared mesh coordinate frame.\nAll movement/reload/attack/deformation acceptance NOT_RUN.\n");
            }finally{PrefabUtility.UnloadPrefabContents(body);}
        }
        static GameObject CrowbarContact(GameObject original)
        {
            var fps=PrefabUtility.LoadPrefabContents("Assets/LastSignal/Prefabs/Combat/Crowbar/Crowbar_Viewmodel.prefab");
            var world=Object.Instantiate(original);world.name="Arberry_WorldCrowbar";
            try{
                var all=fps.GetComponentsInChildren<Transform>(true);var fingers=new Transform[15];
                string[] names={"thumb","point","middle","ring","pink"};
                for(int f=0;f<5;f++)for(int s=0;s<3;s++)fingers[f*3+s]=all.Single(t=>t.name=="R_"+names[f]+(s+1));
                var palm=new CharacterHandPose(all.Single(t=>t.name=="R_wrist"),fingers,false,.012f,false);
                var sourceMesh=all.Single(t=>t.name=="CrowbarMesh");var targetMesh=world.transform.Find("CrowbarMesh");
                var contact=new GameObject("PrimaryGrip_R").transform;contact.SetParent(world.transform,false);
                contact.SetPositionAndRotation(targetMesh.TransformPoint(sourceMesh.InverseTransformPoint(palm.PalmPosition)),targetMesh.rotation*Quaternion.Inverse(sourceMesh.rotation)*palm.PalmRotation);
                var sockets=world.AddComponent<CharacterWeaponSockets>();sockets.PalmContact=contact;sockets.RightGrip=contact;sockets.WeaponVisualRoot=world.transform;
                foreach(var t in world.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                return PrefabUtility.SaveAsPrefabAsset(world,Root+"/Prefabs/Arberry_WorldCrowbar.prefab");
            }finally{Object.DestroyImmediate(world);PrefabUtility.UnloadPrefabContents(fps);}
        }
        // Local authoring only: fit a planar three-segment route around the selected rigid frame.
        // This writes bind-relative poses; no runtime collision query, scaling or bone stretching.
        static void FitFingerRoutes(Transform[] fingers,Vector3[] tips,Quaternion[] rest,GameObject visual,PistolFingerPose pose)
        {
            var frame=visual.transform.Find("Frame");
            if(!frame)return; // Legacy non-variant pistol retains its separate model contract.
            var mesh=frame.GetComponent<MeshFilter>().sharedMesh;
            var vertices=mesh.vertices.Select(v=>visual.transform.InverseTransformPoint(frame.TransformPoint(v))).ToArray();
            var triangles=mesh.triangles;
            for(int finger=2;finger<5;finger++) {
                int start=finger*3;Vector3 origin=visual.transform.InverseTransformPoint(fingers[start].position);
                var sections=new Dictionary<int,float[]>();
                float[] Section(float z) {
                    int key=Mathf.RoundToInt(z/.00025f);
                    if(sections.TryGetValue(key,out var cached))return cached;
                    float sampleZ=key*.00025f;var xs=new List<float>();
                    for(int k=0;k<triangles.Length;k+=3) {
                        var a=vertices[triangles[k]];var b=vertices[triangles[k+1]];var c=vertices[triangles[k+2]];
                        float by=b.y-a.y,bz=b.z-a.z,cy=c.y-a.y,cz=c.z-a.z,det=by*cz-bz*cy;
                        if(Mathf.Abs(det)<1e-10f)continue;
                        float py=origin.y-a.y,pz=sampleZ-a.z,u=(py*cz-pz*cy)/det,v=(by*pz-bz*py)/det;
                        if(u>=0&&v>=0&&u+v<=1)xs.Add(a.x+u*(b.x-a.x)+v*(c.x-a.x));
                    }
                    xs.Sort();return sections[key]=xs.ToArray();
                }
                bool Clear(Vector3 a,Vector3 b) {
                    for(int k=1;k<=20;k++) {
                        var p=Vector3.Lerp(a,b,k/20f);var xs=Section(p.z);
                        if(xs.Length<2||(xs.Length&1)!=0)continue;
                        for(int j=0;j<xs.Length;j+=2)if(p.x>xs[j]-.001f&&p.x<xs[j+1]+.001f)return false;
                    }
                    return true;
                }
                var lengths=new float[3];
                for(int j=0;j<3;j++)lengths[j]=Vector3.Distance(visual.transform.InverseTransformPoint(fingers[start+j].TransformPoint(tips[start+j])),visual.transform.InverseTransformPoint(fingers[start+j].position));
                Vector3 Direction(float angle)=>new Vector3(-Mathf.Cos(angle*Mathf.Deg2Rad),0,Mathf.Sin(angle*Mathf.Deg2Rad));
                float score=float.PositiveInfinity;Vector3 best=Vector3.zero;
                for(int a=10;a<=85;a+=5) {
                    var p1=origin+Direction(a)*lengths[0];if(!Clear(origin,p1))continue;
                    for(int b=-85;b<=30;b+=5) {
                        var p2=p1+Direction(b)*lengths[1];if(!Clear(p1,p2))continue;
                        for(int c=-120;c<=-40;c+=5) {
                            var p3=p2+Direction(c)*lengths[2];var section=Section(p3.z);
                            if(section.Length<2||(section.Length&1)!=0||p3.x<section[0]-.014f||p3.x>section[0]-.002f||!Clear(p2,p3))continue;
                            float cost=Mathf.Abs(p3.x-(section[0]-.005f))*1000+Mathf.Abs(a-35)*.03f+Mathf.Abs(b+35)*.03f+Mathf.Abs(c+85)*.02f;
                            if(cost<score){score=cost;best=new Vector3(a,b,c);}
                        }
                    }
                }
                if(!float.IsFinite(score))throw new InvalidOperationException("No finger route around "+pose.Rig+" finger "+finger+"; retain previous asset and inspect contact authoring.");
                for(int j=0;j<3;j++) {
                    int index=start+j;var bone=fingers[index];
                    bone.rotation=Quaternion.FromToRotation(bone.TransformPoint(tips[index])-bone.position,visual.transform.TransformDirection(Direction(best[j])))*bone.rotation;
                    pose.GripRelative[index]=Quaternion.Inverse(rest[index])*bone.localRotation;
                    pose.TriggerRelative[index]=pose.GripRelative[index];pose.AuthoredEndpoints[index]=visual.transform.InverseTransformPoint(bone.TransformPoint(tips[index]));
                }
            }
        }
        public static void RebuildPistolFingers(float palmDepth)
        {
            Guard();
            if(!float.IsFinite(palmDepth)||palmDepth<=0)throw new ArgumentOutOfRangeException(nameof(palmDepth));
            var body=PrefabUtility.LoadPrefabContents(ArberryCharacterAuthoring.BodyPath);
            try {
                var animator=body.GetComponent<Animator>();
                string[] keys={"Pistol","DXT","Glock17","Custom9mm"};
                for(int i=0;i<keys.Length;i++) {
                    var profile=AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Root+"/Profiles/"+keys[i]+".asset");
                    profile.WorldPistolFingers=Fingers(animator,profile,keys[i],i-1,palmDepth);
                    profile.FpsPistolFingers=profile.WorldPistolFingers;
                    EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
                }
            } finally {PrefabUtility.UnloadPrefabContents(body);}
        }
        static PistolFingerPose Fingers(Animator animator,CharacterWeaponPoseProfile profile,string key,int variant,float palmDepth=.036470f)
        {
            var fingers=new Transform[15];string[] names={"Thumb","Index","Middle","Ring","Little"},segments={"Proximal","Intermediate","Distal"};
            for(int f=0;f<5;f++)for(int s=0;s<3;s++)fingers[f*3+s]=animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),"Right"+names[f]+segments[s]));
            var visual=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(profile.WorldPrefab));
            var rest=fingers.Select(x=>x.localRotation).ToArray();var tips=new Vector3[15];
            var pose=ScriptableObject.CreateInstance<PistolFingerPose>();pose.name="Arberry_"+key+"_Fingers";pose.Rig="Arberry Humanoid / "+key;
            try {
                for(int j=0;j<15;j++)tips[j]=fingers[j].InverseTransformPoint(j%3<2?fingers[j+1].position:fingers[j].position+(fingers[j].position-fingers[j-1].position)*.75f);
                var sockets=visual.GetComponent<CharacterWeaponSockets>();var palm=new CharacterHandPose(animator,false,palmDepth);
                CharacterWeaponSockets.Attach(visual.transform,sockets.PrimaryGrip,palm.PalmPosition,palm.PalmRotation);
                // Existing weapon measurements are transferable; local joint rotations are recalculated on this rig.
                float width=variant<0?.029f:new[]{.026f,.029f,.030f}[variant],depth=variant<0?.040f:new[]{.038f,.042f,.045f}[variant];
                var directions=new[]{new Vector3(-width,.018f,.030f),new Vector3(-.002f,-.003f,.022f),new Vector3(.001f,-.002f,.024f),
                    new Vector3(0,.002f,depth),new Vector3(-.002f,-.001f,.021f),new Vector3(-.004f,-.001f,.019f),
                    new Vector3(-width,0,.018f),new Vector3(-.002f,0,-depth*.65f),new Vector3(.002f,0,-.020f),
                    new Vector3(-width*.95f,0,.018f),new Vector3(-.003f,0,-depth*.60f),new Vector3(.002f,0,-.018f),
                    new Vector3(-width*.55f,0,.021f),new Vector3(-width*.4f,0,-.010f),new Vector3(.002f,0,-.014f)};
                for(int j=0;j<15;j++) {
                    fingers[j].rotation=Quaternion.FromToRotation(fingers[j].TransformPoint(tips[j])-fingers[j].position,visual.transform.TransformDirection(directions[j]))*fingers[j].rotation;
                    pose.GripRelative[j]=Quaternion.Inverse(rest[j])*fingers[j].localRotation;pose.TriggerRelative[j]=pose.GripRelative[j];pose.AuthoredEndpoints[j]=visual.transform.InverseTransformPoint(fingers[j].TransformPoint(tips[j]));
                }
                FitFingerRoutes(fingers,tips,rest,visual,pose);
                var trigger=new[]{new Vector3(-width*.65f,-.004f,depth*.85f),new Vector3(-width*.5f,-.003f,-.013f),new Vector3(.003f,0,-.019f)};
                for(int j=3;j<6;j++){fingers[j].rotation=Quaternion.FromToRotation(fingers[j].TransformPoint(tips[j])-fingers[j].position,visual.transform.TransformDirection(trigger[j-3]))*fingers[j].rotation;pose.TriggerRelative[j]=Quaternion.Inverse(rest[j])*fingers[j].localRotation;}
                return Save(pose,Root+"/Profiles/"+key+"_Fingers.asset");
            }finally{for(int j=0;j<15;j++)fingers[j].localRotation=rest[j];PrefabUtility.UnloadPrefabContents(visual);}
        }
        [MenuItem("Last Signal/Character/Arberry/6 Create isolated material fixture")]
        public static void Fixture()
        {
            Guard();string path=Root+"/Prefabs/Arberry_MaterialComparison.prefab";var root=new GameObject("Arberry_MaterialComparison");
            try {
                var baseline=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ArberryCharacterAuthoring.SourcePath),root.transform);baseline.name="Baseline";
                var candidate=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ArberryCharacterAuthoring.BodyPath),root.transform);candidate.name="Candidate";
                foreach(var behaviour in candidate.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
                foreach(var a in root.GetComponentsInChildren<Animator>(true)){a.runtimeAnimatorController=null;a.enabled=false;}
                baseline.SetActive(false); // Identical position, exclusive A/B selection by inspector.
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Environment_Wood";floor.transform.SetParent(root.transform,false);floor.transform.localPosition=new Vector3(0,-.07f,0);floor.transform.localScale=new Vector3(4,.1f,4);floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Art/S013/Production/Materials/Wood.mat");
                for(int i=0;i<2;i++){var swatch=GameObject.CreatePrimitive(PrimitiveType.Sphere);swatch.transform.SetParent(root.transform,false);swatch.name=i==0?"Environment_Metal":"Environment_Rock";swatch.transform.localPosition=new Vector3(i==0?-1:1,.3f,0);swatch.transform.localScale=Vector3.one*.5f;swatch.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Art/S013/Production/Materials/"+(i==0?"Metal":"B_Rock1")+".mat");}
                string[] lighting={"NeutralDaylight","Overcast","Shelter","NightFlashlight"};
                for(int i=0;i<4;i++){
                    var group=new GameObject(lighting[i]);group.transform.SetParent(root.transform,false);
                    Light(group.transform,"Key",i==3?LightType.Spot:LightType.Directional,i==2?new Color(1,.72f,.46f):i==1?new Color(.72f,.81f,1):Color.white,i==0?1.2f:i==1?.55f:i==2?.7f:5,new Vector3(0,1.65f,2),new Vector3(i==3?0:40,180,0));
                    Light(group.transform,"Fill",LightType.Directional,new Color(.65f,.75f,1),i==3?.025f:.25f,Vector3.zero,new Vector3(20,20,0));group.SetActive(i==0);
                }
                string[] names={"Front","Back","Side","ThreeQuarter","Face","JacketGloves"};
                Vector3[] positions={new Vector3(0,1,3.5f),new Vector3(0,1,-3.5f),new Vector3(3.5f,1,0),new Vector3(2.5f,1.2f,2.5f),new Vector3(0,1.73f,.65f),new Vector3(.7f,1.2f,1.3f)};
                for(int i=0;i<names.Length;i++) {var camera=new GameObject("Camera_"+names[i]).AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.transform.localPosition=positions[i];camera.transform.LookAt(new Vector3(0,i==4?1.72f:i==5?1.22f:1,0));camera.fieldOfView=i==4?35:40;camera.nearClipPlane=.03f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.11f,.12f);camera.allowHDR=true;camera.enabled=i==0;}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{Object.DestroyImmediate(root);}
            var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try{
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.08f,.09f,.11f);RenderSettings.fog=false;
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
                EditorSceneManager.SaveScene(scene,"Assets/LastSignal/Scenes/Validation/ArberryMaterialComparison.unity");
            }
            finally{if(previous.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static void Light(Transform parent,string name,LightType type,Color color,float intensity,Vector3 position,Vector3 rotation)
        {var l=new GameObject(name).AddComponent<Light>();l.transform.SetParent(parent,false);l.type=type;l.color=color;l.intensity=intensity;l.range=8;l.spotAngle=45;l.shadows=LightShadows.Soft;l.transform.localPosition=position;l.transform.localEulerAngles=rotation;}
    }
}
