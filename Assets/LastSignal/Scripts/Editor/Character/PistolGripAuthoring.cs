using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>Persistent pistol-only authoring. No Play mode, gameplay simulation or acceptance gate.</summary>
    public static class PistolGripAuthoring
    {
        const string Folder = "Assets/LastSignal/Data/Combat/S021/";
        const string Prefabs = "Assets/LastSignal/Prefabs/Combat/S021/";
        // Directions measured against the grip cross-sections; thumb and each finger have distinct arcs.
        // Model axes: +Z barrel, +Y slide, +X right side. These are authoring inputs only.
        static readonly Vector3[] GripDirections = {
            new Vector3(-.029f,.035f,.028f), new Vector3(-.002f,-.003f,.022f), new Vector3(.001f,-.002f,.024f),
            new Vector3(0,.002f,.040f), new Vector3(-.002f,-.001f,.021f), new Vector3(-.004f,-.001f,.019f),
            new Vector3(-.038f,0,.020f), new Vector3(-.002f,0,-.027f), new Vector3(.002f,0,-.020f),
            new Vector3(-.036f,0,.020f), new Vector3(-.003f,0,-.025f), new Vector3(.002f,0,-.018f),
            new Vector3(-.019f,0,.023f), new Vector3(-.013f,0,-.010f), new Vector3(.002f,0,-.014f)
        };
        static readonly Vector3[] TriggerDirections = {
            new Vector3(-.019f,-.004f,.035f), new Vector3(-.016f,-.003f,-.013f), new Vector3(.003f,0,-.019f)
        };

        [MenuItem("Last Signal/Character/Author Pistol Anatomical Grip")]
        public static void Author()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before persistent pistol authoring.");
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Folder + "PistolPose.asset");
            profile.FpsHipEuler = new Vector3(5, -8, -3);
            profile.FpsRaisedEuler = new Vector3(1, -3, -2);
            profile.HipEuler = new Vector3(5, -8, -3);
            profile.RaisedEuler = new Vector3(1, -3, -2);
            profile.PistolMagazineTravel = .08f;
            Edit(Prefabs + "Weapon_TacticalPistol.prefab", root => {
                var g = root.GetComponent<FirstPersonWeaponGrip>();
                ConfigureContact(g.Sockets);
                var savedPosition = g.Visual.localPosition; var savedRotation = g.Visual.localRotation;
                var palm = new CharacterHandPose(g.RightHand, g.RightFingers, false, .012f, false);
                var pose = AuthorFingers("PistolFingers_FPS", "VAL Generic, 15 right finger joints", g.RightFingers, palm, g.Visual, g.Sockets);
                profile.FpsPistolFingers = pose;
                g.Visual.localPosition = savedPosition; g.Visual.localRotation = savedRotation;
                // Persist the new contact relative to the shared viewmodel frame.
                CharacterWeaponSockets.Attach(g.Visual, g.Sockets.PrimaryGrip, g.PoseFrame.position,
                    g.PoseFrame.rotation * Quaternion.Euler(0,0,90));
            });
            Edit(Prefabs + "Diesel_WorldPistol.prefab", root => ConfigureContact(root.GetComponentInChildren<CharacterWeaponSockets>(true), true));
            var body = PrefabUtility.LoadPrefabContents("Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab");
            var weapon = PrefabUtility.LoadPrefabContents(Prefabs + "Diesel_WorldPistol.prefab");
            try {
                var animator = body.GetComponentInChildren<Animator>(true);
                var fingers = HumanoidFingers(animator);
                var palm = new CharacterHandPose(animator, false);
                var sockets = weapon.GetComponentInChildren<CharacterWeaponSockets>(true);
                profile.WorldPistolFingers = AuthorFingers("PistolFingers_Diesel", "Diesel Humanoid, 15 right finger joints", fingers, palm, weapon.transform, sockets);
            } finally { PrefabUtility.UnloadPrefabContents(weapon); PrefabUtility.UnloadPrefabContents(body); }
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
            Debug.Log("Pistol contact and per-rig finger assets saved. Runtime/visual acceptance NOT_RUN.");
        }
        static void ConfigureContact(CharacterWeaponSockets sockets, bool world = false)
        {
            sockets.PrimaryGrip.localPosition = world ? new Vector3(.061f,.068f,-.075f) : new Vector3(.061f,.056f,-.065f);
            sockets.PrimaryGrip.localRotation = Quaternion.LookRotation(Vector3.forward,Vector3.left);
        }
        static Transform[] HumanoidFingers(Animator animator)
        {
            var result = new Transform[15]; string[] names = { "Thumb", "Index", "Middle", "Ring", "Little" };
            string[] segments = { "Proximal", "Intermediate", "Distal" };
            for(int f=0;f<5;f++)for(int s=0;s<3;s++)result[f*3+s]=animator.GetBoneTransform(
                (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),"Right"+names[f]+segments[s]));
            return result;
        }
        static PistolFingerPose AuthorFingers(string name, string rig, Transform[] fingers, CharacterHandPose palm, Transform visual, CharacterWeaponSockets sockets)
        {
            string path = Folder + name + ".asset";
            var pose = AssetDatabase.LoadAssetAtPath<PistolFingerPose>(path);
            if(!pose) { pose=ScriptableObject.CreateInstance<PistolFingerPose>(); AssetDatabase.CreateAsset(pose,path); }
            pose.Rig=rig; pose.GripRelative=new Quaternion[15]; pose.TriggerRelative=new Quaternion[15]; pose.AuthoredEndpoints=new Vector3[15];
            var rest=new Quaternion[15]; var tips=new Vector3[15];
            for(int i=0;i<15;i++) { rest[i]=fingers[i].localRotation; tips[i]=fingers[i].InverseTransformPoint(Endpoint(fingers,i)); }
            CharacterWeaponSockets.Attach(visual,sockets.PrimaryGrip,palm.PalmPosition,palm.PalmRotation);
            var model=sockets.PrimaryGrip.parent;
            try {
                for(int i=0;i<15;i++) {
                    // Diesel's thumb metacarpal begins higher and has a longer lever than VAL's.
                    var direction = rig.StartsWith("Diesel",StringComparison.Ordinal) && i == 0 ? new Vector3(-.025f,.003f,.045f) : GripDirections[i];
                    AimJoint(fingers,i,tips[i],model.TransformDirection(direction));
                    pose.GripRelative[i]=Quaternion.Inverse(rest[i])*fingers[i].localRotation;
                    pose.TriggerRelative[i]=pose.GripRelative[i];
                    pose.AuthoredEndpoints[i]=model.InverseTransformPoint(fingers[i].TransformPoint(tips[i]));
                }
                for(int i=3;i<6;i++) {
                    AimJoint(fingers,i,tips[i],model.TransformDirection(TriggerDirections[i-3]));
                    pose.TriggerRelative[i]=Quaternion.Inverse(rest[i])*fingers[i].localRotation;
                }
            } finally { for(int i=0;i<15;i++)fingers[i].localRotation=rest[i]; }
            EditorUtility.SetDirty(pose); return pose;
        }
        static Vector3 Endpoint(Transform[] fingers,int i)
        {
            if(i%3<2)return fingers[i+1].position;
            var bone=fingers[i]; if(bone.childCount>0)return bone.GetChild(0).position;
            // Diesel's distal joints may lack end bones. Continue their actual phalanx axis.
            return bone.position+(bone.position-fingers[i-1].position)*.75f;
        }
        static void AimJoint(Transform[] fingers,int i,Vector3 tip,Vector3 direction)
        {
            var bone=fingers[i]; var current=bone.TransformPoint(tip)-bone.position;
            if(current.sqrMagnitude<1e-10f)throw new InvalidOperationException("Missing phalanx length: "+bone.name);
            bone.rotation=Quaternion.FromToRotation(current,direction)*bone.rotation;
        }
        static void Edit(string path,Action<GameObject> action)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { action(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("Last Signal/Character/Preview Pistol Grip Authoring")]
        public static void Preview() => RenderPreview(false);
        [MenuItem("Last Signal/Character/Preview Pistol Finger Contact")]
        public static void PreviewContact() => RenderPreview(true);
        static void RenderPreview(bool close)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Editor preview requires Edit mode.");
            var utility=new PreviewRenderUtility();
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"Weapon_TacticalPistol.prefab"));
            try {
                utility.AddSingleGO(root);
                var g=root.GetComponent<FirstPersonWeaponGrip>(); var p=g.Profile;
                g.PoseFrame.localPosition=p.FpsHipPosition; g.PoseFrame.localRotation=Quaternion.Euler(p.FpsHipEuler);
                var palm=new CharacterHandPose(g.RightHand,g.RightFingers,false,.012f,false);
                var left=new CharacterHandPose(g.LeftHand,g.LeftFingers,true,.012f,false);
                g.RightUpper.position=p.FpsRightShoulder; g.LeftUpper.position=p.FpsLeftShoulder;
                palm.WristPose(g.Sockets.PrimaryGrip.position,g.Sockets.PrimaryGrip.rotation,out var target,out var rotation);
                PoseArm(g.RightUpper,g.RightLower,g.RightHand,target,rotation,p.RightElbow,p.MaximumReach);
                PoseArm(g.LeftUpper,g.LeftLower,g.LeftHand,p.FpsOffhandPosition,g.LeftHand.rotation,p.LeftElbow,p.MaximumReach);
                CharacterWeaponSockets.Attach(g.Visual,g.Sockets.PrimaryGrip,palm.PalmPosition,palm.PalmRotation);
                palm.Apply(1,true,0,1,p,p.FpsPistolFingers); left.Apply(.25f,false,0,.35f);
                utility.camera.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                utility.camera.fieldOfView=75; utility.camera.nearClipPlane=.01f; utility.camera.farClipPlane=10;
                if(close) {
                    var focus=g.Sockets.PrimaryGrip.position;
                    utility.camera.transform.position=focus+new Vector3(-.22f,.025f,.03f);
                    utility.camera.transform.LookAt(focus); utility.camera.fieldOfView=50;
                }
                utility.camera.clearFlags=CameraClearFlags.SolidColor; utility.camera.backgroundColor=new Color(.16f,.18f,.20f);
                utility.camera.cullingMask=~0;
                utility.lights[0].intensity=1.4f; utility.lights[0].transform.rotation=Quaternion.Euler(35,145,0);
                utility.lights[1].intensity=.8f; utility.ambientColor=Color.gray;
                utility.BeginPreview(new Rect(0,0,1280,720),GUIStyle.none); utility.Render(true);
                var texture=utility.EndPreview(); var rt=RenderTexture.GetTemporary(1280,720,0);
                Graphics.Blit(texture,rt); var previous=RenderTexture.active;
                try { RenderTexture.active=rt; var png=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();
                    File.WriteAllBytes(close ? "/private/tmp/lastsignal-pistol-contact-preview.png" : "/private/tmp/lastsignal-pistol-editor-preview.png",png.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(png);
                } finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); }
            } finally { utility.Cleanup(); if(root)UnityEngine.Object.DestroyImmediate(root); }
        }
        static void PoseArm(Transform upper,Transform lower,Transform hand,Vector3 target,Quaternion rotation,Vector3 hint,float reach)
        {
            var s=CharacterArmIK.Solve(upper.position,target,Vector3.Distance(upper.position,lower.position),Vector3.Distance(lower.position,hand.position),hint,Vector3.down,Vector3.zero,reach,Mathf.PI);
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,s.Elbow-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,s.Wrist-lower.position)*lower.rotation;
            hand.rotation=rotation;
        }
    }
}
