using UnityEditor;
using UnityEngine;
using LastSignal.Inventory.Data;
using LastSignal.Inventory;

namespace LastSignal.Editor.Inventory
{
    public static class S005SeedTool
    {
        [MenuItem("Tools/Last Signal/Seed S005 Items")]
        public static void SeedItems()
        {
            EnsureFolder("Assets/Game/Items/Definitions");
            EnsureFolder("Assets/Game/Items/Prefabs");

            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();

            var items = new (string id, string name, ItemCategory cat, int stack)[]
            {
                ("medical.bandage", "Bandage", ItemCategory.Medical, 5),
                ("food.canned", "Canned Food", ItemCategory.Food, 3),
                ("drink.water", "Water Bottle", ItemCategory.Drink, 3),
                ("ammo.rifle", "Rifle Ammo", ItemCategory.Ammunition, 60),
                ("material.scrap", "Scrap", ItemCategory.Material, 20),
                ("tool.wrench", "Wrench", ItemCategory.Tool, 1)
            };

            var listField = typeof(ItemCatalog).GetField("items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var itemsList = new System.Collections.Generic.List<ItemDefinition>();

            foreach (var data in items)
            {
                string prefabPath = $"Assets/Game/Items/Prefabs/{data.id}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab == null)
                {
                    GameObject go = new GameObject(data.name);
                    var collider = go.AddComponent<BoxCollider>();
                    collider.size = new Vector3(0.2f, 0.2f, 0.2f);
                    go.AddComponent<WorldItem>();
                    go.layer = 0; // Default
                    prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                    Object.DestroyImmediate(go);
                }

                string defPath = $"Assets/Game/Items/Definitions/{data.id}.asset";
                ItemDefinition def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(defPath);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<ItemDefinition>();
                    AssetDatabase.CreateAsset(def, defPath);
                }

                var serializedDef = new SerializedObject(def);
                serializedDef.FindProperty("stableId.id").stringValue = data.id;
                serializedDef.FindProperty("displayName").stringValue = data.name;
                serializedDef.FindProperty("category").enumValueIndex = (int)data.cat;
                serializedDef.FindProperty("maxStack").intValue = data.stack;
                serializedDef.FindProperty("worldPrefab").objectReferenceValue = prefab;
                serializedDef.ApplyModifiedProperties();

                itemsList.Add(def);
            }

            listField.SetValue(catalog, itemsList);
            AssetDatabase.CreateAsset(catalog, "Assets/Game/Items/Definitions/ItemCatalog.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("S005 items and catalog seeded successfully.");
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = path.Substring(0, path.LastIndexOf('/'));
                var folder = path.Substring(path.LastIndexOf('/') + 1);
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
