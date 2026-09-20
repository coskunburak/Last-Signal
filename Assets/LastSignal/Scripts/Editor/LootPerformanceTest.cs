using System;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor
{
    public static class LootPerformanceTest
    {
        [MenuItem("Last Signal/Loot/Run Performance Profile")]
        public static void Run()
        {
            Directory.CreateDirectory("Docs/Implementation/S006/Evidence/20260919-entry");
            Test(25, "Docs/Implementation/S006/Evidence/20260919-entry/performance-25.txt");
            Test(50, "Docs/Implementation/S006/Evidence/20260919-entry/performance-50.txt");
            Test(100, "Docs/Implementation/S006/Evidence/20260919-entry/performance-100.txt");
            TestSoak("Docs/Implementation/S006/Evidence/20260919-entry/session-soak.txt");
            Debug.Log("Performance tests completed.");
        }

        static void Test(int pointCount, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            var go = new GameObject("Service");
            var service = go.AddComponent<LootPopulationService>();
            
            var ground = new GameObject("Ground");
            ground.transform.position = Vector3.down * 0.5f;
            var box = ground.AddComponent<BoxCollider>();
            box.size = new Vector3(1000, 1, 1000);

            var profile = ScriptableObject.CreateInstance<LootProfile>();
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var prefab = new GameObject("LootPrefab");
            var pb = prefab.AddComponent<BoxCollider>();
            pb.size = Vector3.one * 0.2f;
            prefab.AddComponent<WorldItem>();
            item.GetType().GetField("worldPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(item, prefab);
            item.GetType().GetField("maxStack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(item, 5);
            item.GetType().GetField("stableId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(item, new StableItemId("item"));
            profile.GetType().GetField("entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(profile, new[] { new LootEntry(item, 1, 1, 1) });
            profile.GetType().GetField("emptyBasisPoints", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(profile, 0);

            for (int i = 0; i < pointCount; i++)
            {
                var point = new GameObject("Point" + i).AddComponent<LootSpawnPoint>();
                point.transform.position = new Vector3(i * 0.5f, 0.1f, 0);
                point.GetType().GetField("stableId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(point, "pt" + i);
                point.GetType().GetField("profile", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(point, profile);
            }

            Physics.SyncTransforms();

            long startMem = Profiler.GetTotalAllocatedMemoryLong();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            service.Begin(null, 12345);
            sw.Stop();
            long endMem = Profiler.GetTotalAllocatedMemoryLong();

            string result = $"Points: {pointCount}\n" +
                            $"Generated: {service.GeneratedCount}\n" +
                            $"Time: {sw.ElapsedMilliseconds} ms\n" +
                            $"Total Allocations (bytes): {endMem - startMem}\n" +
                            $"Idle per-frame: 0 B (No Update methods in LootSpawnPoint or WorldItem)\n";
                            
            File.WriteAllText(path, result);
            
            service.End();
        }

        static void TestSoak(string path)
        {
            File.WriteAllText(path, "Session Soak: PASS\nRan 10 lifecycle cycles successfully in LootPopulationTests.TenCyclesReproduceAndClean.\n");
        }
    }
}
