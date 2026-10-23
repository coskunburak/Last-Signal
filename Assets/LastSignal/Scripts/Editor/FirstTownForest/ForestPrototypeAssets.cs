using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.FirstTownForest
{
    /// <summary>Owns derivatives only. Vendor importers, materials, meshes and prefabs are read-only.</summary>
    internal sealed class ForestPrototypeAssets
    {
        internal const string Root = "Assets/LastSignal/Art/ForestPrototype120";
        internal const string Trees = "Assets/Realistic Tree/Prefabs/HDRP";
        internal const string Fern = "Assets/ThirdParty/fern_02_4k.blend";
        internal const string Fir = "Assets/ThirdParty/fir_tree_01_4k.blend (1)";
        internal const string Ground = "Assets/ThirdParty/forrest_ground_03_4k.blend/textures/forrest_ground_03_";
        readonly string folder;
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        internal readonly List<GameObject> canopy = new List<GameObject>();
        internal readonly List<GameObject> ferns = new List<GameObject>();
        internal GameObject hero, shrub, rock, grass, debris;
        internal TerrainLayer[] layers;
        internal Material terrainMaterial;
        internal Material distantGroundMaterial;
        internal ForestPrototypeAssets(string output) { folder = output; }
        internal static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing forest source: " + path);
        internal static void Folder(string path) => ForestAuthoring.Folder(path);
        internal static Shader ShaderNamed(string name) => Shader.Find(name) ?? throw new InvalidOperationException("Shader unavailable: " + name);
        internal static long Triangles(Mesh mesh)
        {
            long n = 0; for (int i = 0; i < mesh.subMeshCount; i++) n += mesh.GetIndexCount(i) / 3;
            return n;
        }
        internal void Build()
        {
            Folder(folder + "/Textures"); Folder(folder + "/Materials"); Folder(folder + "/Prefabs");
            foreach (int n in new[] { 1, 4, 6, 8, 9 }) canopy.Add(Tree("Spruce", n, n < 4 ? 11.5f : n < 8 ? 15 : 18));
            canopy.Add(Tree("Birch", 2, 12));
            PrepareFern(); PrepareHero();
            shrub = Simple("Bush", .75f, true); rock = Simple("Rock", 1, false); PrepareGrass();
            PrepareDebris(); PrepareTerrain();
        }
        internal static Bounds BoundsOf(GameObject g)
        {
            var rs = g.GetComponentsInChildren<MeshRenderer>(true);
            if (rs.Length == 0) throw new InvalidOperationException("No mesh renderers: " + g.name);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }
        Texture2D TextureCopy(string source, bool linear = false, bool normal = false, bool readable = false)
        {
            Load<Texture2D>(source);
            // Different species ship identically named Leaf_1 textures. Include source identity.
            string path = folder + "/Textures/" + Path.GetFileNameWithoutExtension(source) + "_" + AssetDatabase.AssetPathToGUID(source).Substring(0, 8)
                + (normal ? "_normal" : linear ? "_data" : "_color") + Path.GetExtension(source);
            if (!File.Exists(path) && !AssetDatabase.CopyAsset(source, path)) throw new IOException("Copy failed: " + source);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear && !normal; importer.isReadable = readable;
            importer.maxTextureSize = 2048; importer.mipmapEnabled = true; importer.streamingMipmaps = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = !linear && !normal;
            importer.textureCompression = readable ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 8;
            importer.SaveAndReimport(); return Load<Texture2D>(path);
        }
        Texture2D WriteTexture(string name, Color[] pixels, int size, bool linear, bool alpha)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            string path = folder + "/Textures/" + name + ".png";
            try { t.SetPixels(pixels); t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); }
            finally { Object.DestroyImmediate(t); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var i = (TextureImporter)AssetImporter.GetAtPath(path);
            i.sRGBTexture = !linear; i.alphaIsTransparency = alpha; i.alphaSource = TextureImporterAlphaSource.FromInput;
            i.maxTextureSize = 2048; i.mipmapEnabled = true; i.streamingMipmaps = true; i.anisoLevel = 8;
            i.textureCompression = TextureImporterCompression.CompressedHQ; i.SaveAndReimport();
            return Load<Texture2D>(path);
        }
        void ReleaseCpu(Texture2D t)
        {
            var i = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t));
            i.isReadable = false; i.textureCompression = TextureImporterCompression.CompressedHQ; i.SaveAndReimport();
        }
        Texture2D PackedMask(string name, string roughness, string height = null)
        {
            var r = TextureCopy(roughness, true, false, true);
            var h = height == null ? null : TextureCopy(height, true, false, true);
            const int size = 1024;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size, v = (y + .5f) / size;
                pixels[y * size + x] = new Color(0, 1, h ? h.GetPixelBilinear(u, v).r : .5f, 1 - r.GetPixelBilinear(u, v).r);
            }
            var output = WriteTexture(name, pixels, size, true, false); ReleaseCpu(r); if (h) ReleaseCpu(h); return output;
        }
        Texture2D ColorWithAlpha(string name, string diffuse, string alpha)
        {
            var d = TextureCopy(diffuse, false, false, true); var a = TextureCopy(alpha, true, false, true);
            const int size = 2048; var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            { float u = (x + .5f) / size, v = (y + .5f) / size; var c = d.GetPixelBilinear(u, v); c.a = a.GetPixelBilinear(u, v).r; pixels[y * size + x] = c; }
            var output = WriteTexture(name, pixels, size, false, true); ReleaseCpu(d); ReleaseCpu(a); return output;
        }
        Material Lit(string name, Texture color, Texture normal, Texture mask, bool foliage)
        {
            var m = new Material(ShaderNamed("HDRP/Lit")) { name = name, enableInstancing = true, doubleSidedGI = foliage };
            m.SetTexture("_BaseColorMap", color); m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_NormalMap", normal); m.SetFloat("_NormalScale", .8f);
            m.SetTexture("_MaskMap", mask); m.SetFloat("_Metallic", 0); m.SetFloat("_MetallicRemapMax", 0);
            m.SetFloat("_Smoothness", foliage ? .22f : .12f);
            m.SetFloat("_SmoothnessRemapMin", 0); m.SetFloat("_SmoothnessRemapMax", .5f);
            m.SetFloat("_AlphaCutoffEnable", foliage ? 1 : 0); m.SetFloat("_AlphaCutoff", .4f);
            m.SetFloat("_DoubleSidedEnable", foliage ? 1 : 0); m.SetFloat("_DoubleSidedNormalMode", 1);
            HDMaterial.ValidateMaterial(m);
            AssetDatabase.CreateAsset(m, folder + "/Materials/" + name + ".mat"); return m;
        }
        Material TreeMaterial(Material source)
        {
            string id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            if (materials.TryGetValue(id, out var cached)) return cached;
            if (!source.shader.name.Contains("HDRP")) throw new InvalidOperationException("Unexpected tree shader: " + source.shader.name);
            var m = new Material(source) { name = "Forest_" + source.name, enableInstancing = true };
            foreach (string property in source.GetTexturePropertyNames())
            {
                var t = source.GetTexture(property); if (!t || property.StartsWith("unity_")) continue;
                string p = AssetDatabase.GetAssetPath(t);
                bool normal = p.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0;
                m.SetTexture(property, TextureCopy(p, normal, normal));
            }
            // Verified vendor Shader Graph property names, same deformation in its HDRP passes.
            if (m.HasProperty("Vector1_15425b8b103a4e46acb8796d0a7d47b9")) m.SetFloat("Vector1_15425b8b103a4e46acb8796d0a7d47b9", 1.2f);
            if (m.HasProperty("Vector1_eabdb1c804a3442f8655288ebb89e349")) m.SetFloat("Vector1_eabdb1c804a3442f8655288ebb89e349", .09f);
            if (m.HasProperty("Vector1_9fd3215e954844f3bfcebed762ec7389")) m.SetFloat("Vector1_9fd3215e954844f3bfcebed762ec7389", .18f);
            HDMaterial.ValidateMaterial(m);
            AssetDatabase.CreateAsset(m, folder + "/Materials/Tree_" + id + ".mat"); materials.Add(id, m); return m;
        }
        static void Strip(GameObject g)
        {
            // Some vendor birches put their falling-leaf system on the prefab root.
            // Remove only particle components, never the object that owns the tree hierarchy.
            foreach (var p in g.GetComponentsInChildren<ParticleSystem>(true))
            {
                var renderer = p.GetComponent<ParticleSystemRenderer>();
                if (renderer) Object.DestroyImmediate(renderer);
                Object.DestroyImmediate(p);
            }
            foreach (var b in g.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in g.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(r);
        }
        GameObject Save(GameObject g, string name)
        {
            g.name = name;
            foreach (var r in g.GetComponentsInChildren<Renderer>(true)) GameObjectUtility.SetStaticEditorFlags(r.gameObject, 0);
            var saved = PrefabUtility.SaveAsPrefabAsset(g, folder + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(g); return saved;
        }
        GameObject Tree(string species, int variant, float height)
        {
            var g = Object.Instantiate(Load<GameObject>(Trees + "/" + species + "/" + species + " " + variant + ".prefab"));
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); Strip(g);
            var wrapper = new GameObject(); g.transform.SetParent(wrapper.transform, false);
            var bounds = BoundsOf(g); g.transform.localScale *= height / bounds.size.y;
            bounds = BoundsOf(g); g.transform.position -= Vector3.up * bounds.min.y;
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterials = r.sharedMaterials.Select(TreeMaterial).ToArray();
            foreach (var group in g.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs(); float[] screen = { .52f, .27f, .12f, .012f };
                for (int i = 0; i < lods.Length; i++) { lods[i].screenRelativeTransitionHeight = screen[Mathf.Min(i, 3)]; lods[i].fadeTransitionWidth = .15f; }
                group.SetLODs(lods); group.fadeMode = LODFadeMode.CrossFade; group.animateCrossFading = true; group.RecalculateBounds();
            }
            var collider = wrapper.AddComponent<CapsuleCollider>(); collider.radius = species == "Birch" ? .22f : .31f;
            collider.height = Mathf.Min(height * .65f, 10); collider.center = Vector3.up * collider.height * .5f;
            return Save(wrapper, species + variant);
        }
        void PrepareFern()
        {
            var color = ColorWithAlpha("Fern_BaseAlpha", Fern + "/textures/fern_02_diff_4k.jpg", Fern + "/textures/fern_02_alpha_4k.png");
            var normal = TextureCopy(Fern + "/textures/fern_02_nor_gl_4k.exr", true, true);
            var mask = PackedMask("Fern_Mask", Fern + "/textures/fern_02_rough_4k.exr");
            var material = Lit("Fern", color, normal, mask, true);
            var source = Load<GameObject>(Fern + "/fern_02_4k.blend");
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true).OrderBy(f => f.name))
            {
                var root = new GameObject(); var visual = new GameObject("Fronds"); visual.transform.SetParent(root.transform);
                // Preserve Blender-to-Unity rotation, remove the source's presentation-grid offset.
                visual.transform.localRotation = filter.transform.rotation;
                visual.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var r = visual.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = ShadowCastingMode.Off;
                visual.transform.position -= Vector3.up * BoundsOf(root).min.y;
                var lod = root.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.018f, new Renderer[] { r }) { fadeTransitionWidth = .2f } });
                lod.fadeMode = LODFadeMode.CrossFade; lod.animateCrossFading = true; lod.RecalculateBounds();
                ferns.Add(Save(root, filter.name));
            }
        }
        Material FirMaterial(string name)
        {
            if (materials.TryGetValue(name, out var value)) return value;
            bool leaf = name.EndsWith("twig", StringComparison.Ordinal);
            string stem = name.EndsWith("dead_branches", StringComparison.Ordinal) ? "fir_tree_01_bark" : name;
            string prefix = Fir + "/textures/" + stem;
            var color = leaf ? ColorWithAlpha("FirTwig_BaseAlpha", prefix + "_diff_4k.png", prefix + "_alpha_4k.png") : TextureCopy(prefix + "_diff_4k.png");
            value = Lit(name, color, TextureCopy(prefix + "_nor_gl_4k.png", true, true), PackedMask(name + "_Mask", prefix + "_rough_4k.png"), leaf);
            materials.Add(name, value); return value;
        }
        void PrepareHero()
        {
            var source = Load<GameObject>(Fir + "/fir_tree_01_4k.blend"); var root = new GameObject();
            var lods = new List<LOD>();
            foreach (int level in new[] { 1, 2 })
            {
                var filter = source.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "fir_tree_01_a_LOD" + level);
                if (Triangles(filter.sharedMesh) > 180000) throw new InvalidOperationException("Hero mesh exceeds restricted budget.");
                var g = new GameObject("Hero_LOD" + (level - 1)); g.transform.SetParent(root.transform);
                g.transform.localRotation = filter.transform.rotation;
                g.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials.Select(m => FirMaterial(m.name)).ToArray();
                lods.Add(new LOD(level == 1 ? .65f : .018f, new Renderer[] { r }) { fadeTransitionWidth = .15f });
            }
            float minimum = BoundsOf(root).min.y;
            foreach (Transform child in root.transform) child.localPosition -= Vector3.up * minimum;
            var group = root.AddComponent<LODGroup>(); group.SetLODs(lods.ToArray()); group.fadeMode = LODFadeMode.CrossFade; group.animateCrossFading = true; group.RecalculateBounds();
            var c = root.AddComponent<CapsuleCollider>(); c.radius = .4f; c.height = 12; c.center = Vector3.up * 6;
            hero = Save(root, "FirHero_LimitedLOD1LOD2");
        }
        GameObject Simple(string key, float targetHeight, bool foliage)
        {
            var g = Object.Instantiate(Load<GameObject>(ForestSources.Kit + "Prefabs/" + key + ".prefab"));
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); Strip(g);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(source =>
                {
                    string id = "Simple_" + source.name;
                    if (materials.TryGetValue(id, out var m)) return m;
                    Texture texture = source.HasProperty("_BaseColorMap") ? source.GetTexture("_BaseColorMap") : null;
                    if (!texture) throw new InvalidOperationException("Source has no base color: " + source.name);
                    m = Lit(id, TextureCopy(AssetDatabase.GetAssetPath(texture)), null, null, foliage); materials.Add(id, m); return m;
                }).ToArray();
                if (foliage) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            var b = BoundsOf(g); var root = new GameObject(); g.transform.SetParent(root.transform);
            g.transform.localScale *= targetHeight / b.size.y; g.transform.position -= Vector3.up * BoundsOf(g).min.y;
            foreach (var old in g.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(old);
            var lod = root.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(foliage ? .018f : .009f, root.GetComponentsInChildren<Renderer>()) { fadeTransitionWidth = .2f } });
            lod.fadeMode = LODFadeMode.CrossFade; lod.RecalculateBounds();
            if (!foliage) { b = BoundsOf(root); var c = root.AddComponent<BoxCollider>(); c.center = b.center; c.size = b.size * .82f; }
            return Save(root, key);
        }
        void PrepareGrass()
        {
            var source = Load<GameObject>(ForestSources.Kit + "Prefabs/Grass.prefab");
            var filter = source.GetComponentInChildren<MeshFilter>();
            var root = new GameObject("Grass detail");
            var mesh = new Mesh { name = "Grass detail — flattened transform" };
            mesh.CombineMeshes(new[] { new CombineInstance { mesh = filter.sharedMesh, transform = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix } }, true, true);
            AssetDatabase.CreateAsset(mesh, folder + "/GrassMesh.asset");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = materials["Simple_" + Load<GameObject>(ForestSources.Kit + "Prefabs/Bush.prefab").GetComponentInChildren<MeshRenderer>().sharedMaterial.name];
            // This kit's grass and bush share the Plant atlas; assert that assumption rather than silently substituting.
            var grassSource = filter.GetComponent<MeshRenderer>().sharedMaterial;
            if (grassSource.GetTexture("_BaseColorMap") != Load<GameObject>(ForestSources.Kit + "Prefabs/Bush.prefab").GetComponentInChildren<MeshRenderer>().sharedMaterial.GetTexture("_BaseColorMap"))
                throw new InvalidOperationException("Grass atlas differs from the inspected Plant atlas.");
            var renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            grass = Save(root, "GrassDetail");
        }
        void PrepareDebris()
        {
            // Existing bark submesh scaled into a small branch; never a harvestable resource.
            var source = Load<GameObject>(ForestSources.Kit + "Prefabs/Pine.prefab").GetComponentInChildren<MeshFilter>().sharedMesh;
            {
                // Instantiate preserves UVs/normals and all vertex attributes; select only bark indices.
                var mesh = Object.Instantiate(source); mesh.name = "FallenBranch";
                // Imported source is readable in the existing forest pipeline.
                mesh.subMeshCount = 1;
                AssetDatabase.CreateAsset(mesh, folder + "/BranchMesh.asset");
                var root = new GameObject(); var visual = new GameObject("Bark branch"); visual.transform.SetParent(root.transform);
                visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                var bark = Load<GameObject>(ForestSources.Kit + "Prefabs/Pine.prefab").GetComponentInChildren<MeshRenderer>().sharedMaterials[0];
                var r = visual.AddComponent<MeshRenderer>();
                r.sharedMaterial = Lit("BranchBark", TextureCopy(AssetDatabase.GetAssetPath(bark.GetTexture("_BaseColorMap"))), null, null, false);
                visual.transform.localRotation = Quaternion.Euler(0, 0, 88);
                visual.transform.localScale = new Vector3(.2f, .16f, .2f);
                visual.transform.position -= Vector3.up * BoundsOf(root).min.y;
                var lod = root.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.012f, new Renderer[] { r }) }); lod.RecalculateBounds();
                debris = Save(root, "FallenBranch");
            }
        }
        void PrepareTerrain()
        {
            var baseColor = TextureCopy(Ground + "diff_4k.jpg"); var normal = TextureCopy(Ground + "nor_gl_4k.exr", true, true);
            var mask = PackedMask("Woodland_Mask", Ground + "rough_4k.jpg", Ground + "disp_4k.png");
            distantGroundMaterial = Lit("Distant woodland soil", baseColor, normal, mask, false);
            var floor = new TerrainLayer { name = "Needle and leaf litter", diffuseTexture = baseColor, normalMapTexture = normal, maskMapTexture = mask,
                tileSize = new Vector2(3.5f, 3.5f), normalScale = .75f, metallic = 0, smoothness = .12f,
                maskMapRemapMin = Vector4.zero, maskMapRemapMax = new Vector4(0, 1, 1, .45f) };
            var list = new List<TerrainLayer> { floor }; AssetDatabase.CreateAsset(floor, folder + "/Woodland.terrainlayer");
            foreach (string key in new[] { "soil", "grass", "rock" })
            {
                var layer = Object.Instantiate(Load<TerrainLayer>(ForestSources.Vendor + "TerrainLayers/layer_ground_" + key + ".terrainlayer"));
                layer.name = key; layer.tileSize = Vector2.one * (key == "rock" ? 4 : 3); layer.smoothness = .08f;
                layer.diffuseTexture = TextureCopy(AssetDatabase.GetAssetPath(layer.diffuseTexture));
                // Use the genuine forest normal for woodland only; never invent missing source maps.
                AssetDatabase.CreateAsset(layer, folder + "/" + key + ".terrainlayer"); list.Add(layer);
            }
            layers = list.ToArray(); terrainMaterial = new Material(ShaderNamed("HDRP/TerrainLit")) { name = "Forest Terrain Lit", enableInstancing = true };
            terrainMaterial.EnableKeyword("_TERRAIN_BLEND_HEIGHT"); terrainMaterial.SetFloat("_EnableHeightBlend", 1); terrainMaterial.SetFloat("_HeightTransition", .25f);
            AssetDatabase.CreateAsset(terrainMaterial, folder + "/Terrain.mat");
        }
    }
}
