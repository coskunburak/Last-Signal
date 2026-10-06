using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    // Kaynak prefabların eski GUID bağlantılarına bağımlı olmayan proje kopyaları.
    public static class MRPolyCatalogAuthoring
    {
        public const string Source = "Assets/ThirdParty/Weapons/MRPoly/Low Poly Weapons Set";
        public const string Output = "Assets/LastSignal/Prefabs/Combat/MRPoly";
        public const string Materials = "Assets/LastSignal/Materials/MRPoly";

        [MenuItem("Last Signal/Weapons/Build MR POLY Visual Catalog")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
            Folder(Output + "/World"); Folder(Output + "/FirstPerson"); Folder(Materials);
            foreach (var file in Directory.GetFiles(Source + "/Prefabs", "*.prefab", SearchOption.AllDirectories).OrderBy(p => p))
            {
                string family = Path.GetFileName(Path.GetDirectoryName(file));
                string label = Path.GetFileNameWithoutExtension(file);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "/Models/" + family + ".fbx");
                if (!source) throw new InvalidOperationException("Model bulunamadı: " + family);
                var go = UnityEngine.Object.Instantiate(source);
                try
                {
                    go.name = label;
                    ApplyPalette(go, family, label);
                    // Dünya modeli sunum varlığıdır; etkileşim/fizik sahibi oyun nesnesi tarafından eklenir.
                    Save(go, Output + "/World/" + label + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
                if (family == "Assault Rifle") BuildRifle(label);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MR POLY: 16 dünya modeli ve 7 VAL/optik tüfek prefabı oluşturuldu.");
        }

        static void BuildRifle(string label)
        {
            // Var olan prefabın aynı bileşenleri, animasyonu, ADS referansları ve optiği korunur.
            var root = PrefabUtility.LoadPrefabContents(RealAssetIntegration.Prefab);
            try
            {
                root.name = "FP_" + label;
                ApplyPalette(root, "Assault Rifle", label, true);
                Save(root, Output + "/FirstPerson/" + label + ".prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ApplyPalette(GameObject root, string family, string label, bool firstPerson = false)
        {
            var black = Material("Color_Black_Metal_01_Mat");
            var grip = Material("Color_Black_01_Mat");
            var gray = Material("Color_Gray_Metal_01_Mat");
            string color = label.Contains("Blue") ? "Blue" : label.Contains("Brown") ? "Brown" :
                label.Contains("White") ? "White" : label.Contains("Red") ? "Red" :
                label.Contains("Orange") ? "Orange" : label.Contains("Gold") ? "Gold" : "Black";
            var accent = Material("Color_" + color + "_Metal_01_Mat");
            var secondary = label.Contains("Gold") ? Material("Color_Gold_Metal_01_Mat") : gray;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (firstPerson && renderer.name != "MRPoly_RifleBody" && renderer.name != "MRPoly_Magazine" && renderer.name != "Trigger") continue;
                var mats = renderer.sharedMaterials;
                if (mats.Length == 0) throw new InvalidOperationException("Materyalsiz parça: " + renderer.name);
                if (renderer.name.Contains("Trigger")) mats[0] = grip;
                else if (renderer.name.Contains("Magazine"))
                { mats[0] = grip; if (mats.Length > 1) mats[1] = black; }
                else
                {
                    mats[0] = label.Contains("Black-") ? black : accent;
                    if (mats.Length > 1) mats[1] = label.Contains("Grip Brown") ? accent : grip;
                    if (mats.Length > 2) mats[2] = label.Contains("Black-") || label.Contains("Red-") ? accent : secondary;
                    if (mats.Length == 4 && !firstPerson) mats[3] = AssetDatabase.LoadAssetAtPath<Material>(ScopeOpticAuthoring.Folder + "/MRPoly_FrontLens.mat");
                }
                if (mats.Any(m => !m)) throw new InvalidOperationException("Eksik materyal: " + renderer.name);
                renderer.sharedMaterials = mats;
            }
        }

        static Material Material(string name)
        {
            string path = Materials + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Material>(Source + "/Materials/" + name + ".mat");
            if (!source) throw new InvalidOperationException("Palet materyali bulunamadı: " + name);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP/Lit bulunamadı.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", source.GetColor("_Color"));
            material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
            material.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void Save(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path, out bool success);
            if (!success) throw new InvalidOperationException("Prefab kaydedilemedi: " + path);
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
