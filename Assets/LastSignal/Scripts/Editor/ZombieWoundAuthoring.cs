using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class ZombieWoundAuthoring
    {
        public const string PrefabPath = "Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab";
        public const string MeshFolder = "Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces";
        public const string ProfilePath = "Assets/LastSignal/Data/VFX/Blood/ZombieWounds.asset";
        [MenuItem("Last Signal/Zombie/Log Wound Resource Snapshot")]
        public static void LogResources()
        {
            int count = 0, wounds = 0, overlays = 0, meshes = 0, misses = 0;
            long meshBytes = 0;
            foreach (var actor in Object.FindObjectsByType<ZombieLocalizedWounds>(FindObjectsSortMode.None))
            {
                count++; wounds += actor.WoundCount; overlays += actor.ActiveOverlayCount; meshes += actor.RuntimeMeshCount;
                if (actor.Resolver == null) continue;
                misses += actor.Resolver.ResolutionMisses;
                foreach (var surface in actor.Resolver.Surfaces)
                    if (surface.renderer.sharedMesh != surface.original) meshBytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(surface.renderer.sharedMesh);
            }
            var pool = Object.FindAnyObjectByType<ZombieBloodVfxPool>();
            int particles = 0;
            if (pool) foreach (var p in pool.GetComponentsInChildren<ParticleSystem>(true)) particles += p.particleCount;
            Debug.Log($"LS_WOUND_DIAGNOSTICS actors={count} wounds={wounds} overlays={overlays} runtimeMeshes={meshes} runtimeMeshBytes={meshBytes} surfaceMisses={misses} particles={particles} hits={(pool ? pool.ActiveHits : 0)} bleeds={(pool ? pool.ActiveBleeds : 0)} marks={(pool ? pool.ActiveMarks : 0)} corpses={(pool ? pool.RetainedCorpses : 0)}");
        }
        [MenuItem("Last Signal/Zombie/Bind Dynamic Wound Surfaces")]
        public static void Bind()
        {
            if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/LastSignal/Models/Enemies/Zombie", "WoundSurfaces");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (ZombieWoundSurfaceResolver.PartFor(r.name) == ZombieBodyPart.Unspecified || !r.sharedMesh) continue;
                    string path = MeshFolder + "/" + r.name + ".asset";
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (!mesh)
                    {
                        mesh = Object.Instantiate(r.sharedMesh); mesh.name = "WoundSurface_" + r.name;
                        mesh.SetUVs(2, new List<Vector3>(mesh.vertices)); mesh.SetUVs(3, new List<Vector3>(mesh.normals));
                        AssetDatabase.CreateAsset(mesh, path);
                    }
                    // Imported meshes preserve their read flag when cloned. Keep derivative CPU buffers in players.
                    var meshData = new SerializedObject(mesh);
                    meshData.FindProperty("m_IsReadable").boolValue = true;
                    meshData.FindProperty("m_KeepVertices").boolValue = true;
                    meshData.FindProperty("m_KeepIndices").boolValue = true;
                    meshData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(mesh);
                    r.sharedMesh = mesh; r.quality = SkinQuality.Bone4;
                }
                foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (r.name != "TorsoWound_Visual") continue;
                    var torso = r.transform.parent ? r.transform.parent.GetComponent<SkinnedMeshRenderer>() : null;
                    if (!torso) throw new System.InvalidOperationException("Torso wound parent skin missing.");
                    r.quality = torso.quality;
                }
                if (!root.GetComponent<ZombieLocalizedWounds>()) root.AddComponent<ZombieLocalizedWounds>();
                if (!root.GetComponent<ZombieCorpseImpactReceiver>()) root.AddComponent<ZombieCorpseImpactReceiver>();
                var profile = AssetDatabase.LoadAssetAtPath<ZombieWoundProfile>(ProfilePath);
                if (!profile) { profile = ScriptableObject.CreateInstance<ZombieWoundProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
                var blood = root.GetComponent<ZombieBloodVfxPresenter>().Profile;
                blood.woundProfile = profile; EditorUtility.SetDirty(blood);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
