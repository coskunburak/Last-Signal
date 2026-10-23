using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.Rendering
{
    /// <summary>Source-channel semantics shared by migration and the scoped P0 recovery.</summary>
    internal static class HdrpMaterialMaskUtility
    {
        internal sealed class Inputs
        {
            public Texture metallic, occlusion, albedoAlpha;
            public float metallicScalar, smoothness, aoStrength;
            public Vector2 scale = Vector2.one, offset;
        }

        internal static Inputs Capture(Material source)
        {
            string shader = source.shader.name;
            bool urp = shader == "Universal Render Pipeline/Lit";
            bool standard = shader == "Standard";
            if ((!urp && !standard) || source.IsKeywordEnabled("_SPECULAR_SETUP") ||
                (source.HasProperty("_WorkflowMode") && source.GetFloat("_WorkflowMode") < .5f))
                throw new InvalidOperationException("Manual review required for material workflow: " + shader);
            bool mapped = source.IsKeywordEnabled(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
            bool alpha = source.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            string baseProperty = urp ? "_BaseMap" : "_MainTex";
            return new Inputs
            {
                metallic = mapped ? source.GetTexture("_MetallicGlossMap") : null,
                occlusion = source.IsKeywordEnabled("_OCCLUSIONMAP") ? source.GetTexture("_OcclusionMap") : null,
                albedoAlpha = alpha ? source.GetTexture(baseProperty) : null,
                metallicScalar = source.GetFloat("_Metallic"),
                smoothness = source.GetFloat(urp ? "_Smoothness" : (mapped || alpha ? "_GlossMapScale" : "_Glossiness")),
                aoStrength = source.GetFloat("_OcclusionStrength"),
                scale = source.GetTextureScale(baseProperty), offset = source.GetTextureOffset(baseProperty)
            };
        }

        internal static void Apply(Material target, Texture mask, bool metallicMapped, bool smoothnessMapped,
            float metallic, float smoothness, float aoStrength, bool aoMapped, Vector2 scale, Vector2 offset)
        {
            target.SetTexture("_MaskMap", mask);
            target.SetTextureScale("_MaskMap", scale);
            target.SetTextureOffset("_MaskMap", offset);
            // HDRP's versioned MetallicRemapping migration copies this scalar into RemapMax.
            // A source metallic map replaces the scalar in URP/Standard; keep both values consistent.
            target.SetFloat("_Metallic", metallicMapped ? 1f : metallic);
            target.SetFloat("_MetallicRemapMin", metallicMapped ? 0f : metallic);
            target.SetFloat("_MetallicRemapMax", metallicMapped ? 1f : metallic);
            target.SetFloat("_Smoothness", smoothness);
            target.SetFloat("_SmoothnessRemapMin", smoothnessMapped ? 0f : smoothness);
            target.SetFloat("_SmoothnessRemapMax", smoothness);
            target.SetFloat("_AORemapMin", aoMapped ? 1f - aoStrength : 1f);
            target.SetFloat("_AORemapMax", 1f);
        }

        internal static Texture BuildForConversion(Inputs input, string output)
        {
            foreach (var map in new[] { input.metallic, input.occlusion })
            {
                if (!map) continue;
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(map)) as TextureImporter;
                if (importer == null || importer.sRGBTexture || importer.textureType != TextureImporterType.Default)
                    throw new InvalidOperationException("Data-map conversion requires verified linear source: " + map.name);
            }
            // Already packed R/G/A or a sole metallic map can be reused with explicit remaps.
            if (input.albedoAlpha == null && (input.occlusion == null || input.occlusion == input.metallic))
                return input.metallic;
            if (input.metallic == null && input.albedoAlpha == null)
                return input.occlusion; // R/A disabled by constant remap ranges; G is the source AO.
            return WritePacked(output, PathOf(input.metallic), PathOf(input.occlusion), "",
                PathOf(input.albedoAlpha), 2048, false);
        }

        static string PathOf(Texture texture) => texture ? AssetDatabase.GetAssetPath(texture) : "";

        // Decode source bytes into linear textures. Never toggle source readability, color space or compression.
        // This intentionally fails closed for non-PNG inputs and for sRGB data maps.
        internal static Texture2D ReadRaw(string path, bool allowSrgb)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Packing supports verified PNG inputs only: " + path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || (!allowSrgb && importer.sRGBTexture))
                throw new InvalidOperationException("Expected linear data-map importer: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                    throw new InvalidOperationException("Cannot decode " + path);
                return texture;
            }
            catch { Object.DestroyImmediate(texture); throw; }
        }

        internal static Texture2D WritePacked(string output, string metal, string ao, string roughness,
            string albedoAlpha, int maxSize, bool rawMedicineData)
        {
            Texture2D m = null, o = null, r = null, a = null, packed = null;
            try
            {
                // Medicine exports are named raw grayscale data; their vendor importer settings remain untouched.
                m = ReadRaw(metal, rawMedicineData); o = ReadRaw(ao, false);
                r = ReadRaw(roughness, rawMedicineData); a = ReadRaw(albedoAlpha, true);
                int width = 1, height = 1;
                foreach (var t in new[] { m, o, r, a })
                    if (t) { width = Math.Max(width, t.width); height = Math.Max(height, t.height); }
                width = Math.Min(width, maxSize); height = Math.Min(height, maxSize);
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = (x + .5f) / width, v = (y + .5f) / height;
                    float smooth = r ? 1f - r.GetPixelBilinear(u, v).r :
                        a ? a.GetPixelBilinear(u, v).a : m ? m.GetPixelBilinear(u, v).a : 1f;
                    pixels[y * width + x] = new Color(m ? m.GetPixelBilinear(u, v).r : 0f,
                        o ? o.GetPixelBilinear(u, v).g : 1f, 1f, smooth);
                }
                packed = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                packed.SetPixels32(pixels); packed.Apply(false, false);
                byte[] bytes = ImageConversion.EncodeToPNG(packed);
                EnsureAssetFolder(Path.GetDirectoryName(output).Replace('\\', '/'));
                if (!File.Exists(output) || !EqualBytes(bytes, File.ReadAllBytes(output)))
                    File.WriteAllBytes(output, bytes);
                AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(output);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = maxSize;
                importer.mipmapEnabled = true;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(output);
            }
            finally
            {
                foreach (var t in new[] { m, o, r, a, packed }) if (t) Object.DestroyImmediate(t);
            }
        }

        internal static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || !folder.StartsWith("Assets/LastSignal/", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid generated asset folder: " + folder);
            EnsureAssetFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(folder))))
                throw new InvalidOperationException("Could not create asset folder: " + folder);
        }

        internal static bool EqualBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
