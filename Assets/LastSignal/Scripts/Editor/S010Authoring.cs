using System;
using System.IO;
using LastSignal.Shelter;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    public static class S010Authoring
    {
        public const string Scene = "Assets/LastSignal/Scenes/WorldPopulationAcceptance.unity";
        const string Folder = "Assets/LastSignal/Shelter";
        static void Set(Object target, string name, Object value)
        { var s = new SerializedObject(target); s.FindProperty(name).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        static void Text(Object target, string name, string value)
        { var s = new SerializedObject(target); s.FindProperty(name).stringValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        public static void Author()
        {
            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var flow = Object.FindAnyObjectByType<SessionFlow>();
            var site = flow.GetComponent<ShelterSite>(); if (!site) site = flow.gameObject.AddComponent<ShelterSite>();
            var scrap = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/material.scrap.asset");
            var ammo = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/ammo.rifle.asset");
            var wrench = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Game/Items/Definitions/tool.wrench.asset");
            string fuelPath = "Assets/Game/Items/Definitions/fuel.generator.asset";
            var fuel = AssetDatabase.LoadAssetAtPath<ItemDefinition>(fuelPath);
            if (!fuel) { fuel = ScriptableObject.CreateInstance<ItemDefinition>(); AssetDatabase.CreateAsset(fuel, fuelPath); }
            var fs = new SerializedObject(fuel); fs.FindProperty("stableId").FindPropertyRelative("id").stringValue = "fuel.generator";
            fs.FindProperty("displayName").stringValue = "Generator fuel can"; fs.FindProperty("maxStack").intValue = 4;
            fs.FindProperty("description").stringValue = "One can powers the shelter generator for 30 world minutes. Noise attracts nearby threats.";
            fs.FindProperty("category").enumValueIndex = 5; fs.ApplyModifiedPropertiesWithoutUndo();
            var fuelGo = Object.Instantiate(scrap.WorldPrefab); fuelGo.name = "Generator fuel can"; fuelGo.GetComponent<WorldItem>().Configure(fuel, 1);
            var prefab = PrefabUtility.SaveAsPrefabAsset(fuelGo, Folder + "/GeneratorFuel.prefab"); Object.DestroyImmediate(fuelGo); Set(fuel, "worldPrefab", prefab);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/Game/Items/Definitions/ItemCatalog.asset");
            bool has = false; foreach (var item in catalog.EditorItems) if (item == fuel) has = true;
            if (!has) { var cs = new SerializedObject(catalog); var items = cs.FindProperty("items"); items.InsertArrayElementAtIndex(items.arraySize); items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = fuel; cs.ApplyModifiedPropertiesWithoutUndo(); }
            var recipe = AssetDatabase.LoadAssetAtPath<ShelterRecipe>(Folder + "/RifleAmmunition.asset");
            if (!recipe) { recipe = ScriptableObject.CreateInstance<ShelterRecipe>(); AssetDatabase.CreateAsset(recipe, Folder + "/RifleAmmunition.asset"); }
            recipe.recipeId = "recipe.rifle-ammunition"; recipe.revision = 1; recipe.input = scrap; recipe.output = ammo; recipe.tool = wrench;
            recipe.inputQuantity = 2; recipe.outputQuantity = 10; recipe.durationWorldSeconds = 600; recipe.requiresPower = true;
            site.recipe = recipe; site.material = scrap; site.fuel = fuel;
            var ls = new SerializedObject(flow.GetComponent<ShelterLoop>()); ls.FindProperty("storageCapacity").intValue = 4; ls.ApplyModifiedPropertiesWithoutUndo();
            if (site.sockets == null || site.sockets.Length != 3)
            {
                site.sockets = new ShelterSocket[3];
                Vector3[] positions = { new Vector3(-15.5f, 1.5f, 11), new Vector3(-15.5f, 1.5f, 8), new Vector3(-15.5f, 1.5f, 7) };
                for (int i = 0; i < 3; i++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Shelter socket / " + (ShelterModule)i;
                    go.transform.position = positions[i]; go.transform.localScale = new Vector3(.2f, .45f, .4f); go.GetComponent<BoxCollider>().isTrigger = false;
                    var socket = go.AddComponent<ShelterSocket>(); socket.site = site; socket.module = (ShelterModule)i; socket.socketId = "cabin.socket." + socket.module;
                    socket.clearance = new Vector3(.15f, .2f, .15f); site.sockets[i] = socket;
                    var sign = new GameObject("Socket sign", typeof(TextMesh)); sign.transform.position = positions[i] + Vector3.up * .6f;
                    sign.transform.rotation = Quaternion.Euler(0, 270, 0); var label = sign.GetComponent<TextMesh>(); label.text = socket.module + " / E"; label.characterSize = .035f; label.fontSize = 40; label.anchor = TextAnchor.MiddleCenter;
                }
            }
            var points = Object.FindObjectsByType<LootSpawnPoint>();
            Array.Sort(points, (a,b) => string.CompareOrdinal(a.StableId,b.StableId));
            int assigned = 0;
            foreach (var point in points)
            {
                if (point.Profile.name != "Workshop" && !point.Profile.name.StartsWith("S010")) continue;
                if (assigned >= 3) break;
                ItemDefinition resource = assigned == 0 ? scrap : assigned == 1 ? fuel : wrench;
                int amount = assigned == 0 ? 20 : assigned == 1 ? 4 : 1;
                var path = Folder + "/S010-" + resource.Id.Value + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<LootProfile>(path);
                if (!profile) { profile = ScriptableObject.CreateInstance<LootProfile>(); AssetDatabase.CreateAsset(profile, path); }
                var ps = new SerializedObject(profile); ps.FindProperty("emptyBasisPoints").intValue = 0;
                var entries = ps.FindProperty("entries"); entries.arraySize = 1; var entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("item").objectReferenceValue = resource; entry.FindPropertyRelative("weight").intValue = 1;
                entry.FindPropertyRelative("minQuantity").intValue = amount; entry.FindPropertyRelative("maxQuantity").intValue = amount;
                ps.ApplyModifiedPropertiesWithoutUndo(); Set(point, "profile", profile); assigned++;
            }
            if (assigned != 3 || !site.Validate()) throw new InvalidOperationException("S010 authoring failed: assigned=" + assigned + ", valid=" + site.Validate());
            EditorUtility.SetDirty(recipe); EditorUtility.SetDirty(site); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Docs/Implementation/S010/Evidence/20260928-closure/authoring.txt", "Real WorldPopulationAcceptance scene: three stable sockets, one powered recipe, three existing expedition loot points supply scrap/fuel/tool. Existing loot IDs retained. Authoring validated.\n");
        }
        public static void Build()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = "Builds/S010/LastSignal.app", target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            File.WriteAllText("Docs/Implementation/S010/Evidence/20260928-closure/build.txt", $"Result={report.summary.result}\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nPath={report.summary.outputPath}\nUnity={Application.unityVersion}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("S010 build failed.");
        }
    }
}
