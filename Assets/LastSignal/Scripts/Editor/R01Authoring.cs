using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using LastSignal.Inventory.Data;
namespace LastSignal.EditorTools
{
    public static class R01Authoring
    {
        const string Root="Assets/LastSignal/Combat/Crowbar/";
        static T Asset<T>(string p) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(p);
        static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value)
        { var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
        public static void Run()
        {
            var def=Asset<MeleeWeaponDefinition>(Root+"Crowbar.asset");
            if(!def) { def=ScriptableObject.CreateInstance<MeleeWeaponDefinition>();AssetDatabase.CreateAsset(def,Root+"Crowbar.asset"); }
            var metal=Material("M_Crowbar",Root+"Source/Crowbar- Texture.png",new Color(.72f,.72f,.7f),.32f);
            var skin=Material("M_Arms","Assets/LastSignal/Assets/first-person-arms/textures/armColor.png",new Color(.8f,.8f,.78f),.18f);
            // Bake unit conversion and orientation into a project-owned mesh; root remains unit scale.
            var source=AssetDatabase.LoadAllAssetsAtPath(Root+"Source/Crowbar.obj").OfType<Mesh>().First();
            var mesh=Asset<Mesh>(Root+"CrowbarMesh.asset");
            if(!mesh)
            {
                mesh=UnityEngine.Object.Instantiate(source);mesh.name="Crowbar_Normalized";
                var v=mesh.vertices;var q=Quaternion.Euler(90,0,0);
                for(int i=0;i<v.Length;i++) v[i]=q*(v[i]-source.bounds.center)*.09f;
                mesh.vertices=v;var normals=mesh.normals;for(int i=0;i<normals.Length;i++) normals[i]=q*normals[i];mesh.normals=normals;
                mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Root+"CrowbarMesh.asset");
            }
            var root=new GameObject("Crowbar_Viewmodel");
            var visual=Child(root.transform,"VisualRoot",new Vector3(0,-.22f,.32f));
            var arms=UnityEngine.Object.Instantiate(Asset<GameObject>("Assets/LastSignal/Assets/first-person-arms/source/fpsarms.fbx"),visual);
            arms.name="Arms";
            foreach(var a in arms.GetComponentsInChildren<Animator>()) UnityEngine.Object.DestroyImmediate(a);
            foreach(var r in arms.GetComponentsInChildren<Renderer>()) r.sharedMaterial=skin;
            var wrist=arms.GetComponentsInChildren<Transform>().First(t=>t.name=="R_wrist");
            var grip=Child(wrist,"GripAnchor",new Vector3(-.025f,0,-.07f));
            // Initial grip placement; CrowbarPoseAuthoring refines the saved rig below.
            grip.rotation=visual.rotation * Quaternion.Euler(0,0,-18);
            var weapon=Child(grip,"CrowbarMesh",new Vector3(0,.20f,.0416f));
            weapon.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            weapon.gameObject.AddComponent<MeshRenderer>().sharedMaterial=metal;
            Child(grip,"ImpactReference",new Vector3(0,.52f,0));
            var origin=Child(root.transform,"MeleeOrigin",new Vector3(0,-.05f,.12f));
            var controller=root.AddComponent<MeleeWeaponController>(); Ref(controller,"definition",def);Ref(controller,"meleeOrigin",origin);
            var animator=visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController=Animations();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var audio=root.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;
            var presenter=root.AddComponent<MeleeWeaponPresenter>();Ref(presenter,"controller",controller);Ref(presenter,"animator",animator);Ref(presenter,"audioSource",audio);
            Ref(presenter,"impact",Asset<AudioClip>("Assets/LastSignal/Assets/First Person ARPG Starter Pack/Scripts/Characters/Player/Sounds/Damage_Tick.mp3"));
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=2;
            foreach(var r in root.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false; }
            foreach(var c in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"Crowbar_Viewmodel.prefab");UnityEngine.Object.DestroyImmediate(root);
            CrowbarPoseAuthoring.Apply();
            var player=PrefabUtility.LoadPrefabContents("Assets/LastSignal/Prefabs/Player.prefab");
            if(!player.GetComponent<PlayerStamina>())player.AddComponent<PlayerStamina>();
            Ref(player.GetComponent<PlayerCombatController>(),"startingMelee",prefab.GetComponent<MeleeWeaponController>());
            PrefabUtility.SaveAsPrefabAsset(player,"Assets/LastSignal/Prefabs/Player.prefab");PrefabUtility.UnloadPrefabContents(player);
            Item(); AssetDatabase.SaveAssets();
            if(!prefab.GetComponent<MeleeWeaponController>().HasValidAuthoring) throw new Exception("Invalid Crowbar authoring");
            File.WriteAllText("Docs/Implementation/PreS010-Recovery/Evidence/R01/20260926/prefab-authoring.txt","Crowbar created via Unity PrefabUtility; mesh, definition and origin valid. Root scale=1. Colliders=0; Animator=1; AudioSource=1. Visual acceptance pending.\n");
        }
        static Transform Child(Transform parent,string name,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        static Material Material(string name,string texture,Color tint,float smooth)
        {
            var m=Asset<Material>(Root+name+".mat");if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Root+name+".mat");}
            m.SetTexture("_BaseMap",Asset<Texture2D>(texture));m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        static RuntimeAnimatorController Animations()
        {
            string path=Root+"Crowbar.controller";
            var ac=Asset<AnimatorController>(path);if(ac)return ac;
            ac=AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm=ac.layers[0].stateMachine;
            foreach(string name in new[]{"Idle","Equip","Swing"})
            {
                var clip=new AnimationClip { name="Crowbar_"+name };
                // Camera-relative whole-rig motion. The generic source has no Humanoid Avatar.
                float duration=name=="Swing"?.8f:name=="Equip"?.25f:1;
                clip.SetCurve("",typeof(Transform),"localPosition.y", name=="Equip"?AnimationCurve.EaseInOut(0,-.65f,duration,-.22f):AnimationCurve.Constant(0,duration,-.22f));
                clip.SetCurve("",typeof(Transform),"localPosition.z",AnimationCurve.Constant(0,duration,.32f));
                if(name=="Swing")
                {
                    clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.x",new AnimationCurve(new Keyframe(0,0),new Keyframe(.22f,-30),new Keyframe(.34f,32),new Keyframe(.8f,0)));
                    clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.z",new AnimationCurve(new Keyframe(0,0),new Keyframe(.22f,-22),new Keyframe(.34f,18),new Keyframe(.8f,0)));
                }
                else {clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.x",AnimationCurve.Constant(0,duration,0));clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.z",AnimationCurve.Constant(0,duration,0));}
                AssetDatabase.CreateAsset(clip,Root+clip.name+".anim");var state=sm.AddState(name);state.motion=clip;
                if(name=="Idle")sm.defaultState=state;
            }
            var equip=sm.states.First(x=>x.state.name=="Equip").state;var idle=sm.states.First(x=>x.state.name=="Idle").state;
            var transition=equip.AddTransition(idle);transition.hasExitTime=true;transition.exitTime=1;transition.duration=0;
            return ac;
        }
        static void Item()
        {
            string path="Assets/Game/Items/Definitions/Crowbar.asset";
            var item=Asset<ItemDefinition>(path);
            if(!item)
            {
                item=ScriptableObject.CreateInstance<ItemDefinition>();var s=new SerializedObject(item);
                s.FindProperty("stableId").FindPropertyRelative("id").stringValue=MeleeWeaponDefinition.CrowbarId;
                s.FindProperty("displayName").stringValue="Crowbar";s.FindProperty("category").enumValueIndex=(int)ItemCategory.Tool;
                s.FindProperty("maxStack").intValue=1;s.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(item,path);
            }
            var catalog=Asset<ItemCatalog>("Assets/Game/Items/Definitions/ItemCatalog.asset");var so=new SerializedObject(catalog);var items=so.FindProperty("items");
            bool found=false;for(int i=0;i<items.arraySize;i++) if(items.GetArrayElementAtIndex(i).objectReferenceValue==item)found=true;
            if(!found){items.arraySize++;items.GetArrayElementAtIndex(items.arraySize-1).objectReferenceValue=item;so.ApplyModifiedPropertiesWithoutUndo();}
        }
    }
}
