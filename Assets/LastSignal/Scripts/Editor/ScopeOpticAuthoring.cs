using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Editor
{
    public static class ScopeOpticAuthoring
    {
        public const string SourcePath = "Assets/LastSignal/Assets/MR POLY/Low Poly Weapons Set/Models/Assault Rifle.fbx";
        public const string Folder = "Assets/LastSignal/Combat/Optics";
        public const string BodyPath = Folder + "/MRPoly_Body_Optics.asset";
        public const string LensPath = Folder + "/MRPoly_RearLens.asset";
        public const string ProfilePath = Folder + "/MRPoly_Scope.asset";
        public const string MaterialPath = Folder + "/MRPoly_ScopeLens.mat";
        public const string FrontMaterialPath = Folder + "/MRPoly_FrontLens.mat";
        public const string LayerName = "FirstPersonViewmodel";
        const string PrefabPath = "Assets/Resources/Weapon_AssaultRifle.prefab";
        const float RearZ = -.120204f;

        [MenuItem("Last Signal/Optics/Install on Existing Rifle Prefab")]
        public static void InstallExistingPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Optik kurulumu Edit Mode'da yapılmalıdır.");
            ValidateLayer();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Optik mevcut tüfek prefabına kuruldu. ADS ve animasyon ayarları korunmuştur.");
        }

        public static void Apply(GameObject root)
        {
            ValidateLayer();
            var weapon = root.GetComponent<WeaponController>();
            var body = root.GetComponentsInChildren<MeshFilter>(true)
                .SingleOrDefault(f => f.name == "MRPoly_RifleBody");
            if (!weapon || !weapon.AimReference || !body)
                throw new InvalidOperationException("Beklenen WeaponController, AimReference veya MRPoly_RifleBody yok.");
            if (weapon.AimReference.parent != body.transform ||
                Quaternion.Angle(weapon.AimReference.localRotation, Quaternion.identity) > .01f)
                throw new InvalidOperationException("Bu rifle adaptörü gövdeye bağlı, +Z eksenli AimReference bekler.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Viewmodel collider içeriyor; layer değişimini fizik açısından inceleyin.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (!source) throw new InvalidOperationException("Kaynak tüfek FBX bulunamadı.");
            var sourceMesh = source.GetComponent<MeshFilter>().sharedMesh;
            if (sourceMesh.subMeshCount != 4)
                throw new InvalidOperationException("Kaynak mesh değişmiş: dört submesh bekleniyor.");
            var shader = Shader.Find("LastSignal/Optics/ScopeLens");
            if (!shader || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("ScopeLens shader bulunamadı veya derleme hatası var.");
            EnsureFolder(Folder);
            var profile = AssetDatabase.LoadAssetAtPath<ScopeOpticDefinition>(ProfilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<ScopeOpticDefinition>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            if (profile.ViewmodelLayer != 29)
                throw new InvalidOperationException("Bu adaptör FirstPersonViewmodel = layer 29 bekler.");
            Mesh bodyCopy = null, rear = null;
            try
            {
                SplitRearLens(sourceMesh, out bodyCopy, out rear);
                var bodyAsset = SaveMesh(bodyCopy, BodyPath); bodyCopy = null;
                var lensAsset = SaveMesh(rear, LensPath); rear = null;
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (!material)
                {
                    material = new Material(shader) { name = "MRPoly_ScopeLens" };
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                else if (material.shader != shader)
                    throw new InvalidOperationException("Mevcut lens materyali beklenmeyen shader kullanıyor.");
                body.sharedMesh = bodyAsset;
                var bodyRenderer = body.GetComponent<MeshRenderer>();
                var bodyMaterials = bodyRenderer.sharedMaterials;
                if (bodyMaterials.Length != 4)
                    throw new InvalidOperationException("Gövde renderer'ında dört materyal bekleniyor.");
                bodyMaterials[3] = GetFrontMaterial();
                bodyRenderer.sharedMaterials = bodyMaterials;
                var lensTransform = body.transform.Find("ScopeLens");
                if (!lensTransform)
                {
                    lensTransform = new GameObject("ScopeLens").transform;
                    lensTransform.SetParent(body.transform, false);
                }
                lensTransform.localPosition = Vector3.zero;
                lensTransform.localRotation = Quaternion.identity;
                lensTransform.localScale = Vector3.one;
                var filter = lensTransform.GetComponent<MeshFilter>();
                if (!filter) filter = lensTransform.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = lensAsset;
                var renderer = lensTransform.GetComponent<MeshRenderer>();
                if (!renderer) renderer = lensTransform.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var presenter = root.GetComponent<ScopeOpticPresenter>();
                if (!presenter) presenter = root.AddComponent<ScopeOpticPresenter>();
                // Mevcut ADS ekseni korunur; reticle bu eksene yerleşir.
                Vector3 opticalCenter = weapon.AimReference.localPosition;
                opticalCenter.z = RearZ;
                presenter.Configure(weapon, profile, renderer, opticalCenter,
                    new Vector2(lensAsset.bounds.extents.x, lensAsset.bounds.extents.y));
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = profile.ViewmodelLayer;
                EditorUtility.SetDirty(presenter);
            }
            finally
            {
                if (bodyCopy) UnityEngine.Object.DestroyImmediate(bodyCopy);
                if (rear) UnityEngine.Object.DestroyImmediate(rear);
            }
        }

        static void ValidateLayer()
        {
            if (LayerMask.NameToLayer(LayerName) != 29)
                throw new InvalidOperationException("Project Settings > Tags and Layers: User Layer 29 = FirstPersonViewmodel yapın.");
        }

        static Material GetFrontMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(FrontMaterialPath);
            if (existing) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader bulunamadı.");
            var material = new Material(shader) { name = "MRPoly_FrontLens", renderQueue = 3000 };
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_BlendModePreserveSpecular", 0);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", .85f);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetColor("_BaseColor", new Color(.65f, .8f, .85f, .06f));
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            AssetDatabase.CreateAsset(material, FrontMaterialPath);
            return material;
        }

        static void SplitRearLens(Mesh source, out Mesh body, out Mesh rear)
        {
            body = null; rear = null;
            // Editor mesh erişimi: FBX Read/Write ayarı değiştirilmez.
            var vertices = source.vertices;
            var sourceNormals = source.normals;
            var sourceIndices = source.GetTriangles(3);
            var front = new List<int>();
            var rearIndices = new List<int>();
            var rearVertices = new List<Vector3>();
            var rearNormals = new List<Vector3>();
            var map = new Dictionary<int, int>();
            for (int i = 0; i < sourceIndices.Length; i += 3)
            {
                bool selected = true;
                for (int j = 0; j < 3; j++)
                    selected &= Mathf.Abs(vertices[sourceIndices[i + j]].z - RearZ) < .00001f;
                for (int j = 0; j < 3; j++)
                {
                    int old = sourceIndices[i + j];
                    if (!selected) { front.Add(old); continue; }
                    if (!map.TryGetValue(old, out int index))
                    {
                        index = rearVertices.Count;
                        map.Add(old, index);
                        rearVertices.Add(vertices[old]);
                        rearNormals.Add(sourceNormals[old]);
                    }
                    rearIndices.Add(index);
                }
            }
            if (rearIndices.Count != 24 || front.Count != 24)
                throw new InvalidOperationException("Kaynak geometri değişmiş: arka ve ön camda sekizer üçgen bekleniyor.");
            body = UnityEngine.Object.Instantiate(source);
            body.name = source.name;
            body.SetTriangles(front, 3);
            body.RecalculateBounds();
            rear = new Mesh { name = "MRPoly_RearLens" };
            rear.SetVertices(rearVertices);
            rear.SetNormals(rearNormals);
            rear.SetTriangles(rearIndices, 0);
            rear.RecalculateBounds();
        }

        static Mesh SaveMesh(Mesh fresh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!existing) { AssetDatabase.CreateAsset(fresh, path); return fresh; }
            EditorUtility.CopySerialized(fresh, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(fresh);
            return existing;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }
    }
}
