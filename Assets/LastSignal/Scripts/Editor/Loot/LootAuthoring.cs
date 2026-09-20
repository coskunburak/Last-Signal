using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Inventory.UI;
using LastSignal.Loot;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace LastSignal.Editor
{
    public static class LootAuthoring
    {
        public const string Evidence="Docs/Implementation/S006/Evidence/20260919-entry";
        public const string ScenePath="Assets/LastSignal/Scenes/ScavengingAcceptance.unity";
        const string Content="Assets/LastSignal/Loot";
        static void Set(Object o,string name,Object value){var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static ItemDefinition Item(string id)=>AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/"+id+".asset");
        static LootProfile Profile(string name,int empty,params (string id,int weight,int min,int max)[] entries)
        {
            string path=Content+"/Profiles/"+name+".asset";
            var p=AssetDatabase.LoadAssetAtPath<LootProfile>(path);
            if(!p){p=ScriptableObject.CreateInstance<LootProfile>();AssetDatabase.CreateAsset(p,path);}
            var s=new SerializedObject(p);s.FindProperty("emptyBasisPoints").intValue=empty;var list=s.FindProperty("entries");list.arraySize=entries.Length;
            for(int i=0;i<entries.Length;i++)
            {var e=list.GetArrayElementAtIndex(i);e.FindPropertyRelative("item").objectReferenceValue=Item(entries[i].id);e.FindPropertyRelative("weight").intValue=entries[i].weight;e.FindPropertyRelative("minQuantity").intValue=entries[i].min;e.FindPropertyRelative("maxQuantity").intValue=entries[i].max;}
            s.ApplyModifiedPropertiesWithoutUndo();return p;
        }
        [MenuItem("Last Signal/Loot/Create S006 acceptance content")]
        public static void CreateAcceptance()
        {
            Directory.CreateDirectory(Content+"/Profiles");Directory.CreateDirectory(Content+"/Materials");AssetDatabase.Refresh();
            Visuals();
            var profiles=new[]{
                Profile("Kitchen",3500,("food.canned",50,1,2),("drink.water",40,1,2),("medical.bandage",10,1,1)),
                Profile("Clinic",2500,("medical.bandage",90,1,2),("drink.water",10,1,1)),
                Profile("Workshop",3500,("material.scrap",80,1,4),("tool.wrench",20,1,1)),
                Profile("Security",5000,("ammo.rifle",90,5,20),("medical.bandage",10,1,2))};
            var scene=EditorSceneManager.OpenScene("Assets/LastSignal/Scenes/ZombieAcceptance.unity",OpenSceneMode.Single);
            var session=Object.FindAnyObjectByType<SessionFlow>();var population=session.gameObject.AddComponent<LootPopulationService>();
            var seed=new SerializedObject(population);seed.FindProperty("overrideSeed").boolValue=true;seed.FindProperty("explicitSeed").intValue=12345;seed.ApplyModifiedPropertiesWithoutUndo();
            // Existing combat arena remains untouched. Copy receives bounded tabletop opportunities along its safe outer lane.
            Vector3[] centers={new Vector3(-10,0,8),new Vector3(-10,0,4),new Vector3(-10,0,0),new Vector3(-10,0,-4)};
            for(int a=0;a<4;a++)
            {
                var area=new GameObject(profiles[a].name);area.transform.position=centers[a];
                var table=GameObject.CreatePrimitive(PrimitiveType.Cube);table.name=profiles[a].name+" counter";table.transform.SetParent(area.transform,false);table.transform.localPosition=new Vector3(0,.45f,0);table.transform.localScale=new Vector3(1.1f,.9f,2.5f);
                table.GetComponent<Renderer>().sharedMaterial=Material(profiles[a].name,new[]{new Color(.42f,.30f,.17f),new Color(.55f,.67f,.62f),new Color(.24f,.29f,.31f),new Color(.27f,.31f,.20f)}[a]);
                var sign=new GameObject("Area sign",typeof(TextMesh));sign.transform.SetParent(area.transform,false);sign.transform.localPosition=new Vector3(-.58f,1.6f,0);sign.transform.localRotation=Quaternion.Euler(0,90,0);var text=sign.GetComponent<TextMesh>();text.text=profiles[a].name.ToUpperInvariant();text.fontSize=48;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;
                for(int i=0;i<5;i++)
                {var go=new GameObject("Loot "+i);go.transform.SetParent(area.transform,false);go.transform.localPosition=new Vector3(0,1,i*.45f-.9f);var p=go.AddComponent<LootSpawnPoint>();var so=new SerializedObject(p);so.FindProperty("stableId").stringValue="s006."+profiles[a].name.ToLowerInvariant()+"."+i;so.FindProperty("profile").objectReferenceValue=profiles[a];so.ApplyModifiedPropertiesWithoutUndo();}
            }
            var spawn=GameObject.Find("PlayerSpawn");spawn.transform.position=new Vector3(-8, .05f,8);spawn.transform.rotation=Quaternion.Euler(0,270,0);
            typeof(S001Project).GetMethod("CreateHud",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{session});
            var hud=Object.FindAnyObjectByType<AcceptanceHud>();hud.transform.Find("Title").GetComponent<Text>().text="LAST SIGNAL / SCAVENGING    •    TAB INVENTORY";
            hud.SetAmmoDisplay(Label(hud.transform,"Ammo",new Vector2(0,-80),new Vector2(700,40),20,new Vector2(.5f,1)));
            InventoryPanel(session);
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            ValidateScene();Distribution();
            Debug.Log("S006 authored content saved: "+ScenePath);
        }
        static Material Material(string name,Color color)
        {
            string path=Content+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;return m;
        }
        static void Visuals()
        {
            string vendor="Assets/LastSignal/Assets/Loot/Polygon-Lite Survival Collection/Prefabs/";
            var mapping=new[]{("medical.bandage","Bandage"),("food.canned","Canned_food_A_01"),("drink.water","Flask"),("material.scrap","SM_Scrap_Metal_01"),("ammo.rifle",""),("tool.wrench","")};
            foreach(var pair in mapping)
            {
                var def=Item(pair.Item1);string path=AssetDatabase.GetAssetPath(def.WorldPrefab);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(root.GetComponentInChildren<Renderer>())continue;
                    GameObject visual;
                    if(pair.Item2!="")visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(vendor+pair.Item2+".prefab"));
                    else {visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.GetComponent<Renderer>().sharedMaterial=Material(pair.Item1,pair.Item1=="ammo.rifle"?new Color(.5f,.4f,.18f):new Color(.55f,.58f,.62f));}
                    visual.name="World Visual";visual.transform.SetParent(root.transform,false);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
                    foreach(var c in visual.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                    var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));visual.transform.localScale*=.19f/size;
                    bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    visual.transform.position+=root.transform.position-bounds.center+Vector3.up*(bounds.extents.y-.1f);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        static Text Label(Transform parent,string value,Vector2 pos,Vector2 size,int fontSize,Vector2? anchor=null)
        {
            var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor??new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;
            var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.text=value;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;
        }
        static Button Button(Transform parent,string value,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(value,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchoredPosition=pos;r.sizeDelta=size;go.GetComponent<Image>().color=new Color(.12f,.25f,.27f);Label(go.transform,value,Vector2.zero,size,17);return go.GetComponent<Button>();
        }
        static void InventoryPanel(SessionFlow session)
        {
            var canvas=new GameObject("Inventory",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=20;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);
            var panel=new GameObject("Panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);panel.GetComponent<RectTransform>().sizeDelta=new Vector2(1040,650);panel.GetComponent<Image>().color=new Color(.025f,.045f,.055f,.99f);
            var ui=canvas.AddComponent<InventoryUI>();Set(ui,"panel",panel);Label(panel.transform,"INVENTORY / select a slot, then Drop",new Vector2(0,275),new Vector2(900,50),25);
            var slots=new InventorySlotUI[24];
            for(int i=0;i<24;i++)
            {var b=Button(panel.transform,"",new Vector2(-360+(i%4)*240,200-(i/4)*65),new Vector2(230,56));b.name="Slot "+i;slots[i]=b.gameObject.AddComponent<InventorySlotUI>();Set(slots[i],"quantityText",b.GetComponentInChildren<Text>());}
            var so=new SerializedObject(ui);var array=so.FindProperty("slotUIs");array.arraySize=24;for(int i=0;i<24;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=slots[i];so.ApplyModifiedPropertiesWithoutUndo();
            var drop=Button(panel.transform,"Drop selected stack",new Vector2(-180,-260),new Vector2(300,50));Set(ui,"dropButton",drop);
            var close=Button(panel.transform,"Close",new Vector2(180,-260),new Vector2(300,50));UnityEventTools.AddPersistentListener(close.onClick,ui.Close);panel.SetActive(false);
        }
        [MenuItem("Last Signal/Loot/Validate active scene")]
        public static void ValidateScene()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var points=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LootSpawnPoint>(true)).ToArray();var text=new StringBuilder();int errors=0;
            Physics.SyncTransforms();
            foreach(var group in points.GroupBy(p=>p.Profile))text.AppendLine((group.Key?group.Key.name:"MISSING")+": "+group.Count());
            foreach(var p in points)
            {
                if(!p.Validate(out string error)){text.AppendLine(p.name+": "+error);errors++;continue;}
                if(points.Count(q=>q.StableId==p.StableId)>1){text.AppendLine(p.StableId+": DUPLICATE ID");errors++;}
                foreach(var q in points)if(q!=p&&Vector3.Distance(p.transform.position,q.transform.position)<.02f){text.AppendLine(p.StableId+": COINCIDENT with "+q.StableId);errors++;}
                for(int i=0;i<p.Profile.EntryCount;i++)if(!LootPopulationService.PlacementValid(p,p.Profile.GetEntry(i).Item.WorldPrefab,null,out error)){text.AppendLine(p.StableId+": "+error);errors++;}
            }
            text.AppendLine("points="+points.Length+" errors="+errors);Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/profile-validation.txt",text.ToString());Debug.Log(text.ToString());
        }
        [MenuItem("Last Signal/Loot/Write 10000-seed distribution")]
        public static void Distribution()
        {
            var report=new StringBuilder("10000 deterministic seeds per profile, point ID=distribution. Not economy certification.\n");
            foreach(var guid in AssetDatabase.FindAssets("t:LootProfile",new[]{Content+"/Profiles"}))
            {
                var p=AssetDatabase.LoadAssetAtPath<LootProfile>(AssetDatabase.GUIDToAssetPath(guid));var counts=new SortedDictionary<string,int>();
                for(int i=0;i<10000;i++){var r=p.Select(i,"distribution");string key=r.Item?r.Item.Id.Value:r.Outcome.ToString();counts.TryGetValue(key,out int n);counts[key]=n+1;}
                report.AppendLine(p.name);foreach(var pair in counts)report.AppendLine(pair.Key+" "+pair.Value+" "+(pair.Value/100.0)+"%");
            }
            Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/distribution-report.txt",report.ToString());
        }
        public static void Build()
        {
            Directory.CreateDirectory("Builds/S006");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/S006/LastSignal.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build-result.txt","Unity "+Application.unityVersion+" macOS Development\n"+report.summary.result+"\n"+report.summary.outputPath+"\nErrors="+report.summary.totalErrors);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("S006 build failed");
        }
    }
}
