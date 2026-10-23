using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    public static class HdrpWeaponNormalRepair
    {
        const string Root = "Assets/LastSignal/Art/HdrpMaterialRecovery/WeaponNormals";
        [Serializable] sealed class Report
        {
            public string status, snapshot, error;
            public List<string> changed = new List<string>();
            public List<string> checks = new List<string>();
        }
        [MenuItem("Last Signal/Rendering/HDRP Production/Repair Two Weapon Normal Inputs")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode.");
            var names = new[] { "machete", "wooden_axe" };
            var prefabs = new[] { "Diesel_WorldMachete", "Diesel_WorldWoodenAxe" };
            var ids = new long[] { 1026990684771456800, -4328601806430921911 };
            var guards = new Dictionary<string, string>();
            var sources = new Material[2];
            var paths = new string[2];
            for (int i = 0; i < 2; i++)
            {
                string model = "Assets/ThirdParty/Weapons/" + names[i] + "_4k.blend/" + names[i] + "_4k.blend";
                string normal = "Assets/ThirdParty/Weapons/" + names[i] + "_4k.blend/textures/" + names[i] + "_nor_gl_4k.exr";
                sources[i] = AssetDatabase.LoadAllAssetsAtPath(model).OfType<Material>().Single(m =>
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string guid, out long id) && id == ids[i]);
                if (sources[i].shader.name != "HDRP/Lit" || AssetDatabase.GetAssetPath(sources[i].GetTexture("_NormalMap")) != normal)
                    throw new InvalidOperationException("Unexpected embedded source: " + model);
                if (File.Exists(Root + "/" + names[i] + ".mat")) throw new InvalidOperationException("Repair already exists; audit instead of overwriting.");
                paths[i] = "Assets/LastSignal/Prefabs/Combat/S021/" + prefabs[i] + ".prefab";
                foreach (string p in new[] { model, model + ".meta", normal, normal + ".meta", paths[i] + ".meta" })
                    guards[p] = HdrpMaterialIntegrityUtility.Hash(p);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                if (!prefab || prefab.GetComponentsInChildren<Renderer>(true).Sum(r => r.sharedMaterials.Count(m => m == sources[i])) != 1)
                    throw new InvalidOperationException("Expected exactly one source slot: " + paths[i]);
            }
            var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            var report = new Report { status = "IN_PROGRESS", snapshot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../LastSignal-Recovery/WeaponNormals-" + stamp)) };
            Directory.CreateDirectory(report.snapshot);
            foreach (string path in paths)
            foreach (string p in new[] { path, path + ".meta" })
            {
                string copy = Path.Combine(report.snapshot, p); Directory.CreateDirectory(Path.GetDirectoryName(copy));
                File.Copy(p, copy, false);
                if (HdrpMaterialIntegrityUtility.Hash(p) != HdrpMaterialIntegrityUtility.Hash(copy)) throw new IOException("Backup mismatch");
            }
            HdrpMaterialMaskUtility.EnsureAssetFolder(Root);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    string original = AssetDatabase.GetAssetPath(sources[i].GetTexture("_NormalMap"));
                    string normal = Root + "/" + names[i] + "_normal.exr";
                    if (!AssetDatabase.CopyAsset(original, normal)) throw new IOException("Cannot copy normal");
                    var importer = (TextureImporter)AssetImporter.GetAtPath(normal);
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.sRGBTexture = false;
                    importer.convertToNormalmap = false;
                    importer.SaveAndReimport();
                    var material = new Material(sources[i]) { name = names[i] + " HDRP" };
                    material.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                    HDMaterial.ValidateMaterial(material);
                    string target = Root + "/" + names[i] + ".mat";
                    AssetDatabase.CreateAsset(material, target);
                    var prefab = PrefabUtility.LoadPrefabContents(paths[i]);
                    try
                    {
                        int changed = 0;
                        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                        {
                            var slots = renderer.sharedMaterials; bool update = false;
                            for (int j = 0; j < slots.Length; j++) if (slots[j] == sources[i]) { slots[j] = material; changed++; update = true; }
                            if (!update) continue;
                            renderer.sharedMaterials = slots;
                            if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        }
                        if (changed != 1) throw new InvalidOperationException("Unexpected slot count");
                        PrefabUtility.SaveAsPrefabAsset(prefab, paths[i], out bool saved);
                        if (!saved) throw new IOException("Prefab save failed");
                    }
                    finally { PrefabUtility.UnloadPrefabContents(prefab); }
                    if (HdrpActiveMaterialCoverageUtility.ValidateNormalMapKeywords(material).Length != 0)
                        throw new InvalidOperationException("Normal material validation failed");
                    report.changed.Add(paths[i]); report.changed.Add(target); report.changed.Add(normal);
                }
                foreach (var pair in guards)
                    if (HdrpMaterialIntegrityUtility.Hash(pair.Key) != pair.Value) throw new InvalidOperationException("Protected file changed: " + pair.Key);
                report.status = "TECHNICAL_CHECKS_PASS";
                report.checks.Add("Both source models, normal textures, importers and prefab GUID metadata unchanged.");
                report.checks.Add("Exactly two renderer slots replaced; animation, transform and gameplay properties not edited.");
            }
            catch (Exception e)
            {
                report.error = e.ToString(); report.status = "FAILED_PREFABS_RESTORED";
                foreach (var path in paths)
                {
                    File.Copy(Path.Combine(report.snapshot, path), path, true);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                }
                throw;
            }
            finally { File.WriteAllText("Docs/Implementation/HDRPProductionOptimization/Evidence/weapon-normals-" + stamp + ".json", JsonUtility.ToJson(report, true)); }
        }
    }
}
