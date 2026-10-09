using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace LastSignal.Editor.S019
{
    // Explicit opt-in source folders only. Existing production/vendor assets are never auto-reimported.
    public sealed class ContentImportPolicy : AssetPostprocessor
    {
        public const string Root = "Assets/LastSignal/Art/S019Import/";
        public const string Presets = "Assets/LastSignal/Settings/S019Import/";
        public static string Profile(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Contains("\\") || path.Contains("..")) return null;
            foreach (string family in new[] { "EnvironmentColor", "StaticMesh", "Audio" })
                if (path.StartsWith(Root + family + "/", StringComparison.Ordinal)) return family;
            return null;
        }
        public static string[] Fields(string family)
        {
            // Whitelist only. In particular don't apply material remaps, asset bundles, platform
            // overrides, texture interpretation, scale conversion, colliders or clip tables.
            switch (family)
            {
                case "EnvironmentColor": return new[] { "m_MipMapMode", "m_EnableMipMap", "m_Aniso" };
                case "StaticMesh": return new[] { "m_IsReadable", "m_ImportBlendShapes", "m_ImportVisibility" };
                case "Audio": return new[] { "m_ForceToMono", "m_Normalize", "m_LoadInBackground", "m_PreloadAudioData" };
                default: return Array.Empty<string>();
            }
        }
        internal static bool Matches(string family, AssetImporter importer) =>
            family == "EnvironmentColor" && importer is TextureImporter ||
            family == "StaticMesh" && importer is ModelImporter || family == "Audio" && importer is AudioImporter;
        public static bool Apply(string path, AssetImporter importer, Preset preset)
        {
            string family = Profile(path);
            if (family == null || !importer || !preset || importer.assetPath != path || !Matches(family, importer)) return false;
            if (!preset.CanBeAppliedTo(importer)) return false;
            var leaves = Fields(family);
            var paths = preset.PropertyModifications.Select(p => p.propertyPath)
                .Where(p => leaves.Any(leaf => p == leaf || p.EndsWith("." + leaf, StringComparison.Ordinal))).Distinct().ToArray();
            if (paths.Length == 0) return false;
            return preset.ApplyTo(importer, paths);
        }
        void OnPreprocessTexture() => ApplyScoped();
        void OnPreprocessModel() => ApplyScoped();
        void OnPreprocessAudio() => ApplyScoped();
        void ApplyScoped()
        {
            var family = Profile(assetPath); if (family == null) return;
            var preset = AssetDatabase.LoadAssetAtPath<Preset>(Presets + family + ".preset");
            if (!preset) { Debug.LogWarning("S019: capture approved " + family + " preset before using opt-in imports: " + assetPath); return; }
            if (!Apply(assetPath, assetImporter, preset)) Debug.LogError("S019: importer/preset mismatch or no supported selected fields: " + assetPath);
            // No SaveAndReimport / Refresh / asset writes here: no reimport recursion.
        }
        public static Preset CreateScopedPreset(AssetImporter source, string family)
        {
            if (!source || !Matches(family, source)) throw new ArgumentException("Exemplar importer does not match preset family.");
            var preset = new Preset(source);
            var leaves = Fields(family);
            var allowed = preset.PropertyModifications.Select(p => p.propertyPath)
                .Where(p => leaves.Any(leaf => p == leaf || p.EndsWith("." + leaf, StringComparison.Ordinal))).Distinct().ToArray();
            if (allowed.Length == 0) { UnityEngine.Object.DestroyImmediate(preset); throw new InvalidOperationException("No supported importer fields for " + family); }
            preset.excludedProperties = preset.PropertyModifications.Select(p => p.propertyPath).Except(allowed).Distinct().ToArray();
            return preset;
        }
        [MenuItem("Last Signal/S019/Imports/Capture selected approved exemplar")]
        public static void Capture()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string family = Profile(path); var importer = AssetImporter.GetAtPath(path);
            if (family == null || !importer || !Matches(family, importer))
                throw new InvalidOperationException("Select an approved importer in an S019Import family folder. Vendor assets cannot be captured here.");
            string destination = Presets + family + ".preset";
            if (File.Exists(destination)) throw new IOException("Preset already exists. Edit it explicitly in the native Preset Inspector; capture never overwrites it.");
            Directory.CreateDirectory(Presets); AssetDatabase.Refresh();
            var preset = CreateScopedPreset(importer, family);
            try { AssetDatabase.CreateAsset(preset, destination); AssetDatabase.SaveAssets(); }
            finally { if (!AssetDatabase.Contains(preset)) UnityEngine.Object.DestroyImmediate(preset); }
            Debug.Log("Captured native preset: " + destination + ". Only documented whitelist fields are automatically applied.");
        }
    }
}
