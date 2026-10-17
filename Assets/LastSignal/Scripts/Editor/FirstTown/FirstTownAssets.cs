using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.FirstTown
{
    // Authoring only: selected existing assets, never vendor edits or package installation.
    internal sealed class FirstTownAssets
    {
        internal const string ZombieRuntime = "Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab";
        internal const string Root = "Assets/LastSignal/Art/FirstTown";
        internal const string Kit = "Assets/LastSignal/Art/S013/Production";
        const string Gas = "Assets/ThirdParty/VGtiXN/Countryside gas station/Prefabs/";
        const string Shipping = "Assets/ThirdParty/Shipping Container Environment/Prefabs/";
        const string European = "Assets/ThirdParty/Modular European House/Prefabs/";
        const string Farm = "Assets/ThirdParty/MarpaStudio/Built-In/Prefabs/";
        internal static readonly Dictionary<string, string> Sources = new Dictionary<string, string>
        {
            { "Canopy", Gas + "Canopy/Station_Canopy.prefab" },
            { "Pump", Gas + "Petrol_pump/Petrol_pump.prefab" },
            { "GasSign", Gas + "Sign/Gas_Station_Sign.prefab" },
            { "GasOffice", Gas + "Operator 's_room/Operator 's_room.prefab" },
            { "Cistern", Gas + "Gas_Cistern.prefab" },
            { "RoadPart", Gas + "Road_Part/Road_Part1.prefab" },
            { "Wreck", Gas + "Old_Rust_Car.prefab" },
            { "Warehouse", Shipping + "Structures/warehouse_1.prefab" },
            { "Container", Shipping + "Containers/container_1.prefab" },
            { "OpenContainer", Shipping + "Containers/container_2_open.prefab" },
            { "Pallet", Shipping + "Props/pallet_full_1.prefab" },
            { "Shelf", Shipping + "Structures/shelf_1.prefab" },
            { "EuropeanHouse", European + "Modular European House.prefab" },
            { "Awning", European + "Building_Moduls/Awning.prefab" },
            { "Cornice", European + "Building_Moduls/Cornice.prefab" },
            { "Fence", "Assets/ThirdParty/S23 NEW ASSETS/Industrial Props/Prefabs/Fence.prefab" },
            { "Barrier", "Assets/ThirdParty/S23 NEW ASSETS/Industrial Props/Prefabs/RoadDivider.prefab" },
            { "WaterTank", "Assets/ThirdParty/S23 NEW ASSETS/Industrial Props/Prefabs/WaterTank.prefab" },
            { "LightPole", "Assets/ThirdParty/S23 NEW ASSETS/Industrial Props/Prefabs/LightPole.prefab" },
            { "Garage", "Assets/ThirdParty/S23 NEW ASSETS/Industrial building/Garage/Models/garage.fbx" },
            { "Workbench", "Assets/ThirdParty/Factory Tools/Prefabs/WoodenWorkbench.prefab" },
            { "ToolStand", "Assets/ThirdParty/Factory Tools/Prefabs/DesktopToolStand.prefab" },
            { "GarageRack", "Assets/ThirdParty/AssetsStore/Garage_props/Assets/Prefabs/rack.prefab" },
            { "WoodDoor", "Assets/ThirdParty/Free Wood Door Pack/Prefab/Wood/Door_2/Door_2_Brown.prefab" },
            { "Medicine", "Assets/ThirdParty/Loot/MedicinePack_JimmyPV/MedicinePack_JimmyPV.fbx" },
            { "Barn", Farm + "BarnBase.prefab" }, { "Silo", Farm + "Silo.prefab" },
            { "Shed", Farm + "OldShed.prefab" }, { "Hay", Farm + "HaiCylinder.prefab" },
            { "Pine", Kit + "/Prefabs/Pine.prefab" }, { "Birch", Kit + "/Prefabs/Birch.prefab" },
            { "Rock", Kit + "/Prefabs/Rock.prefab" }, { "Bush", Kit + "/Prefabs/Bush.prefab" }
        };
        readonly Dictionary<string, Vector3> dimensions = new Dictionary<string, Vector3>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        readonly List<string> created;
        internal FirstTownAssets(List<string> created) { this.created = created; }
        internal static T Load<T>(string path) where T : Object
        { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (!asset) throw new FileNotFoundException("Required existing asset: " + path); return asset; }
        internal static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        internal void Save(Object asset, string path)
        {
            if (File.Exists(path) || File.Exists(path + ".meta")) throw new IOException("Refusing to overwrite " + path);
            Folder(Path.GetDirectoryName(path).Replace('\\', '/')); created.Add(path); AssetDatabase.CreateAsset(asset, path);
        }
        internal static void Preflight()
        {
            foreach (var path in Sources.Values) Load<GameObject>(path);
            var actor=Load<GameObject>(ZombieRuntime);
            if(!actor.GetComponent<ZombieController>() || !actor.GetComponent<UnityEngine.AI.NavMeshAgent>()) throw new InvalidOperationException("Existing runtime zombie composition is missing.");
            var catalog=Resources.Load<ZombieArchetypeCatalog>("S022/ZombieCatalog");
            if(!catalog || !catalog.IsValid) throw new InvalidOperationException("Existing S022 archetype catalog is missing or invalid.");
            Load<GameObject>(S019.PoiAuthoring.TemplatePath);
            foreach (var name in new[] { "Stone", "GroundSoil", "GroundPath", "Iron", "Wood", "InteriorPlaster", "RoofMetal" })
                Load<Material>(Kit + "/Materials/" + name + ".mat");
            if (!Shader.Find("Universal Render Pipeline/Lit") || !Shader.Find("Universal Render Pipeline/Terrain/Lit")) throw new InvalidOperationException("URP Lit shader unavailable.");
        }
        internal void Prepare()
        {
            Folder(Root + "/Prefabs"); Folder(Root + "/Materials");
            foreach (var pair in Sources) MakeWrapper(pair.Key, pair.Value);
            Folder("Assets/LastSignal/Prefabs/FirstTown");
            foreach(var kind in new[]{"Shambler","Runner","Lurker"})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(ZombieRuntime));
                try
                {
                    var actor=go.GetComponent<ZombieController>();
                    var id=kind=="Runner"?ZombieArchetypes.Runner:kind=="Lurker"?ZombieArchetypes.Lurker:ZombieArchetypes.Shambler;
                    actor.Configure(ZombieArchetypes.Load(id)); actor.ConfigureAmbush(kind=="Lurker");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
                    string path="Assets/LastSignal/Prefabs/FirstTown/Zombie_"+kind+".prefab";
                    if(File.Exists(path) || File.Exists(path+".meta"))throw new IOException("Existing runtime variant: "+path);
                    created.Add(path); prefabs.Add("Zombie_"+kind,PrefabUtility.SaveAsPrefabAsset(go,path));
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
        internal ZombieController Zombie(string kind) => prefabs["Zombie_"+kind].GetComponent<ZombieController>();
        internal Material Mat(string name)
        {
            if (palette.TryGetValue(name, out var material)) return material;
            if (name == "Asphalt") material = Normalize(Load<Material>("Assets/ThirdParty/VGtiXN/Countryside gas station/Art/Materials/Road.mat"));
            else if (name == "Stripe" || name == "Water" || name == "School" || name == "Clinic")
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
                material.SetColor("_BaseColor", name == "Stripe" ? new Color(.72f,.64f,.39f) : name == "Water" ? new Color(.10f,.19f,.20f) : name == "Clinic" ? new Color(.68f,.71f,.66f) : new Color(.40f,.27f,.20f));
                material.SetFloat("_Smoothness", name == "Water" ? .83f : .18f);
                Save(material, Root + "/Materials/" + name + ".mat");
            }
            else material = Load<Material>(Kit + "/Materials/" + name + ".mat");
            palette.Add(name, material); return material;
        }
        Material Normalize(Material source)
        {
            if (!source) throw new InvalidOperationException("Missing source material; no silent replacement.");
            if (source.shader && source.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)) return source;
            if (converted.TryGetValue(source, out var existing)) return existing;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Unsaved source material " + source.name);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string ignoredGuid, out long localId);
            var target = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = source.name, enableInstancing = true };
            string color = source.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (source.HasProperty(color)) target.SetColor("_BaseColor", source.GetColor(color));
            string texture = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            CopyTexture(source, target, texture, "_BaseMap");
            CopyTexture(source, target, "_BumpMap", "_BumpMap");
            CopyTexture(source, target, "_MetallicGlossMap", "_MetallicGlossMap");
            if (target.GetTexture("_BumpMap")) target.EnableKeyword("_NORMALMAP");
            if (target.GetTexture("_MetallicGlossMap")) target.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (source.HasProperty("_Metallic")) target.SetFloat("_Metallic", source.GetFloat("_Metallic"));
            target.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? Mathf.Min(.5f, source.GetFloat("_Glossiness")) : .2f);
            bool cutout = source.IsKeywordEnabled("_ALPHATEST_ON") || (source.HasProperty("_Mode") && source.GetFloat("_Mode") == 1);
            if (cutout) { target.SetFloat("_AlphaClip", 1); target.EnableKeyword("_ALPHATEST_ON"); target.renderQueue = 2450; }
            Save(target, Root + "/Materials/Source_" + guid + "_" + localId + ".mat");
            converted.Add(source, target); return target;
        }
        static void CopyTexture(Material source, Material target, string from, string to)
        {
            if (!source.HasProperty(from) || !source.GetTexture(from)) return;
            target.SetTexture(to, source.GetTexture(from)); target.SetTextureScale(to, source.GetTextureScale(from)); target.SetTextureOffset(to, source.GetTextureOffset(from));
        }
        void MakeWrapper(string key, string path)
        {
            var wrapper = new GameObject(key);
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
                visual.transform.SetParent(wrapper.transform, false);
                // Only the new instance is unpacked; vendor scripts cannot become new gameplay owners.
                if (PrefabUtility.IsPartOfPrefabInstance(visual)) PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var t in visual.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) if (script) Object.DestroyImmediate(script);
                foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
                foreach (var light in visual.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);
                foreach (var audio in visual.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(audio);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(Normalize).ToArray();
                Bounds bounds = BoundsOf(wrapper);
                visual.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                // Existing colliders are retained. Static solid art gets mesh collision when absent.
                bool foliage = key == "Pine" || key == "Birch" || key == "Bush";
                if (!foliage && key != "Medicine" && key != "WoodDoor" && visual.GetComponentsInChildren<Collider>(true).Length == 0)
                    foreach (var mesh in visual.GetComponentsInChildren<MeshFilter>(true))
                        if (mesh.sharedMesh) mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
                if (foliage) foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                if (key == "Pine" || key == "Birch")
                {
                    bounds = BoundsOf(wrapper); var trunk = wrapper.AddComponent<CapsuleCollider>();
                    trunk.height = Mathf.Min(bounds.size.y, 5); trunk.radius = .22f;
                    trunk.center = Vector3.up * trunk.height * .5f;
                }
                string output = Root + "/Prefabs/" + key + ".prefab";
                if (File.Exists(output) || File.Exists(output + ".meta")) throw new IOException("Existing wrapper: " + output);
                dimensions.Add(key, BoundsOf(wrapper).size);
                created.Add(output); prefabs.Add(key, PrefabUtility.SaveAsPrefabAsset(wrapper, output));
            }
            finally { Object.DestroyImmediate(wrapper); }
        }
        internal static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No renderers: " + go.name);
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            if (b.size.sqrMagnitude < .001f) throw new InvalidOperationException("Empty bounds: " + go.name);
            return b;
        }
        internal GameObject Place(string key, Transform parent, Vector3 position, float yaw = 0, float width = 0)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[key], parent);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            if (width > 0)
            {
                // Measure in unrotated wrapper space; uniform scaling preserves vendor proportions.
                var size = dimensions[key];
                bool footprint = key == "Container" || key == "OpenContainer" || key == "EuropeanHouse" || key == "Warehouse" ||
                    key == "Garage" || key == "GasOffice" || key == "Canopy" || key == "Barn" || key == "Shed" || key == "Cistern";
                go.transform.localScale = Vector3.one * (width / (footprint ? Mathf.Max(size.x, size.z) : size.x));
            }
            return go;
        }
        internal static GameObject Module(string name, Transform parent, Vector3 position, float yaw = 0)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Kit + "/Prefabs/" + name + ".prefab"), parent);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
            return go;
        }
    }
}
