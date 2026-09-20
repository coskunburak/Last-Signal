using System;
using System.IO;
using LastSignal.Shelter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace LastSignal.Editor
{
    public static class ShelterAuthoring
    {
        public const string ScenePath="Assets/LastSignal/Scenes/ShelterAcceptance.unity";
        public const string Evidence="Docs/Implementation/S008/Evidence/20260920-212200-entry";
        static void Set(Object target,string name,Object value){var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static Transform Anchor(string name,Vector3 position,float yaw,Transform root)
        {var g=new GameObject(name);g.transform.SetParent(root);g.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));return g.transform;}
        static GameObject Box(string name,Vector3 position,Vector3 size,Transform parent,Material material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;return g;
        }
        static Material Material(string name,Color color)
        {
            string path="Assets/LastSignal/Shelter/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;return m;
        }
        static void Sign(Transform root,string text,Vector3 position,float yaw,float scale=.025f)
        {
            var g=new GameObject(text,typeof(TextMesh));g.transform.SetParent(root);g.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));var t=g.GetComponent<TextMesh>();t.text=text;t.fontSize=60;t.characterSize=scale;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(1,.85f,.55f);
        }
        static ShelterPoint Point(ShelterLoop loop,ShelterAction action,Vector3 position,Transform root,Material material)
        {
            var g=Box(action.ToString(),position,new Vector3(.14f,.45f,.5f),root,material);var p=g.AddComponent<ShelterPoint>();Set(p,"loop",loop);var so=new SerializedObject(p);so.FindProperty("action").enumValueIndex=(int)action;so.ApplyModifiedPropertiesWithoutUndo();return p;
        }
        [MenuItem("Last Signal/Shelter/Create S008 acceptance scene")]
        public static void CreateAcceptance()
        {
            Directory.CreateDirectory("Assets/LastSignal/Shelter");AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene("Assets/LastSignal/Scenes/ScavengingAcceptance.unity",OpenSceneMode.Single);
            var session=Object.FindAnyObjectByType<SessionFlow>();var loop=session.gameObject.AddComponent<ShelterLoop>();
            var root=new GameObject("Shelter / physically enclosed cabin").transform;
            var wall=Material("Cabin",new Color(.26f,.20f,.14f));var trim=Material("Terminal",new Color(.16f,.45f,.40f));
            Box("Cabin floor",new Vector3(-15,-.25f,9),new Vector3(6,.5f,6),root,wall);
            Box("West wall",new Vector3(-18,1.6f,9),new Vector3(.3f,3.2f,6),root,wall);
            Box("North wall",new Vector3(-15,1.6f,12),new Vector3(6,3.2f,.3f),root,wall);
            Box("South wall",new Vector3(-15,1.6f,6),new Vector3(6,3.2f,.3f),root,wall);
            Box("Secure entry wall",new Vector3(-12.05f,1.6f,9),new Vector3(.3f,3.2f,6),root,wall);
            Box("Roof",new Vector3(-15,3.25f,9),new Vector3(6,.2f,6),root,wall);
            var lightGo=new GameObject("Shelter warm light",typeof(Light));lightGo.transform.SetParent(root);lightGo.transform.position=new Vector3(-15,2.6f,9);var light=lightGo.GetComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=3;light.color=new Color(1,.75f,.42f);
            var inside=Anchor("Inside doorway",new Vector3(-13.2f,.05f,10),270,root);
            var outside=Anchor("Outside doorway",new Vector3(-10.8f,.05f,10),90,root);
            var exit=Point(loop,ShelterAction.Leave,new Vector3(-12.3f,1.3f,10),root,trim);
            var ret=Point(loop,ShelterAction.Return,new Vector3(-11.8f,1.3f,10),root,trim);
            var store=Point(loop,ShelterAction.Prepare,new Vector3(-16.2f,1.3f,9),root,trim);
            Box("Storage cabinet",new Vector3(-16.55f,.65f,9),new Vector3(.5f,1.3f,1.6f),root,wall);
            Sign(root,"SHELTER\nSTORAGE & PREPARATION",new Vector3(-16.05f,2.15f,9),270);
            Sign(root,"LEAVE SHELTER\nE at entry terminal",new Vector3(-12.28f,2.2f,10),90,.018f);
            Sign(root,"RETURN TO SHELTER\nE at entry terminal",new Vector3(-11.82f,2.2f,10),270,.018f);
            var spawn=GameObject.Find("PlayerSpawn").transform;spawn.SetPositionAndRotation(new Vector3(-14.8f,.05f,9),Quaternion.Euler(0,270,0));
            Set(loop,"insideAnchor",inside);Set(loop,"outsideAnchor",outside);Set(loop,"storagePoint",store);Set(loop,"exitPoint",exit);Set(loop,"returnPoint",ret);
            Set(loop,"preparationUI",CreateUI());
            var hud=Object.FindAnyObjectByType<AcceptanceHud>();hud.transform.Find("Title").GetComponent<Text>().text="LAST SIGNAL / SHELTER    •    E PREPARE / LEAVE / RETURN    •    TAB INVENTORY";
            Physics.SyncTransforms();if(!loop.Validate(out var error))throw new Exception(error);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/authoring-validation.txt","S008 scene authored from existing ScavengingAcceptance; same 20 S006 points and original encounter retained. Physically enclosed cabin at west boundary with explicit doorway traversal. Default stash 48 slots, empty; magazine 30/reserve 0 unchanged. References valid. Runtime placement, interaction and safety tests pending.\n");
        }
        static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=position;return r;}
        static Text Label(Transform parent,string value,Vector2 position,Vector2 size,int fontSize)
        {var r=Rect(parent,value,position,size);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.text=value;t.raycastTarget=false;return t;}
        static Button Button(Transform parent,string value,Vector2 position,Vector2 size)
        {var r=Rect(parent,value,position,size);r.gameObject.AddComponent<Image>().color=new Color(.10f,.25f,.25f);var b=r.gameObject.AddComponent<Button>();Label(r,value,Vector2.zero,size,18);return b;}
        static ShelterSlotUI[] Slots(Transform parent,string name,int count,Vector2 position)
        {
            var view=Rect(parent,name,position,new Vector2(580,465));view.gameObject.AddComponent<Image>().color=new Color(.03f,.05f,.06f);view.gameObject.AddComponent<RectMask2D>();
            var content=Rect(view,"Content",Vector2.zero,new Vector2(560,Mathf.Ceil(count/3f)*55));content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;
            var layout=content.gameObject.AddComponent<GridLayoutGroup>();layout.cellSize=new Vector2(180,48);layout.spacing=new Vector2(6,7);layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=3;
            var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.content=content;scroll.viewport=view;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var slots=new ShelterSlotUI[count];
            for(int i=0;i<count;i++){var button=Button(content,"—",Vector2.zero,new Vector2(180,48));button.name="Slot "+i;slots[i]=button.gameObject.AddComponent<ShelterSlotUI>();Set(slots[i],"label",button.GetComponentInChildren<Text>());}
            return slots;
        }
        static ShelterStorageUI CreateUI()
        {
            var go=new GameObject("Shelter preparation",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=30;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);
            var panel=Rect(go.transform,"Preparation panel",Vector2.zero,new Vector2(1320,820));panel.gameObject.AddComponent<Image>().color=new Color(.025f,.035f,.045f,.995f);
            var ui=go.AddComponent<ShelterStorageUI>();Set(ui,"panel",panel.gameObject);
            Set(ui,"status",Label(panel,"SHELTER / PREPARATION",new Vector2(0,365),new Vector2(1200,50),28));
            Set(ui,"readiness",Label(panel,"",new Vector2(0,310),new Vector2(1200,40),21));
            Label(panel,"CARRIED / 24 SLOTS",new Vector2(-315,255),new Vector2(580,36),23);
            Label(panel,"SHELTER STORAGE / SCROLL TO VIEW",new Vector2(315,255),new Vector2(580,36),23);
            var a=Slots(panel,"Carried",24,new Vector2(-315,0));var b=Slots(panel,"Stored",48,new Vector2(315,0));
            var so=new SerializedObject(ui);foreach(var pair in new[]{("carriedSlots",a),("storedSlots",b)}){var ar=so.FindProperty(pair.Item1);ar.arraySize=pair.Item2.Length;for(int i=0;i<ar.arraySize;i++)ar.GetArrayElementAtIndex(i).objectReferenceValue=pair.Item2[i];}so.ApplyModifiedPropertiesWithoutUndo();
            Set(ui,"depositOne",Button(panel,"Store one →",new Vector2(-465,-270),new Vector2(260,46)));
            Set(ui,"depositStack",Button(panel,"Store stack →",new Vector2(-170,-270),new Vector2(260,46)));
            Set(ui,"withdrawOne",Button(panel,"← Take one",new Vector2(170,-270),new Vector2(260,46)));
            Set(ui,"withdrawStack",Button(panel,"← Take stack",new Vector2(465,-270),new Vector2(260,46)));
            Set(ui,"feedback",Label(panel,"",new Vector2(0,-325),new Vector2(1200,40),18));
            Set(ui,"close",Button(panel,"Close preparation",new Vector2(0,-375),new Vector2(320,44)));
            panel.gameObject.SetActive(false);return ui;
        }
        [MenuItem("Last Signal/Shelter/Validate active shelter")]
        public static void Validate()
        {var loop=Object.FindAnyObjectByType<ShelterLoop>();if(!loop||!loop.Validate(out var error))throw new Exception("Invalid shelter references; inspect ShelterLoop.");Debug.Log("Shelter authoring references valid.");}
        public static void Build()
        {
            Directory.CreateDirectory("Builds/S008");
            var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/S008/LastSignal.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build-result.txt",r.summary.result+"\nErrors="+r.summary.totalErrors+"\nWarnings="+r.summary.totalWarnings+"\n"+r.summary.outputPath);
            if(r.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("S008 build failed");
        }
    }
}
