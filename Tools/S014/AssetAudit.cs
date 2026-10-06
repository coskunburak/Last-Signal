using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

// Read-only MCP command. Run in the already open Editor; never changes importers/assets.
internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var report = new StringBuilder();
        report.AppendLine("Unity=" + Application.unityVersion + " GPU=" + SystemInfo.graphicsDeviceName);
        var paths = AssetDatabase.GetAllAssetPaths().Where(p =>
            (p.StartsWith("Assets/ThirdParty/S14 ASSETS/") && p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) ||
            p.EndsWith("/VAL.fbx") || p.EndsWith("/fpsarms.fbx")).ToList();
        paths.AddRange(new[] { "Assets/LastSignal/Prefabs/Player/Player.prefab", "Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab", "Assets/LastSignal/Prefabs/Combat/Crowbar/Crowbar_Viewmodel.prefab", "Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shambler.prefab", "Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab" });
        foreach (var path in paths)
        {
            report.AppendLine("\nASSET " + path);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!root) { report.AppendLine("MISSING"); continue; }
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer) report.AppendLine("IMPORT rig=" + importer.animationType + " scale=" + importer.globalScale + " fileScale=" + importer.useFileScale + " animation=" + importer.importAnimation + " readable=" + importer.isReadable);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                report.AppendLine("NODE " + AnimationUtility.CalculateTransformPath(t, root.transform) + " scale=" + t.localScale.ToString("F4") + " active=" + t.gameObject.activeSelf);
            foreach (var a in root.GetComponentsInChildren<Animator>(true))
                report.AppendLine("ANIMATOR " + a.name + " avatar=" + (a.avatar ? a.avatar.name + "/valid=" + a.avatar.isValid + "/human=" + a.avatar.isHuman : "NONE") + " controller=" + AssetDatabase.GetAssetPath(a.runtimeAnimatorController) + " rootMotion=" + a.applyRootMotion + " culling=" + a.cullingMode);
            long totalTriangles = 0, totalVertices = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var skin = r as SkinnedMeshRenderer;
                var filter = r.GetComponent<MeshFilter>();
                var mesh = skin ? skin.sharedMesh : filter ? filter.sharedMesh : null;
                if (!mesh) continue;
                long triangles = 0;
                for (int s = 0; s < mesh.subMeshCount; s++) if (mesh.GetTopology(s) == MeshTopology.Triangles) triangles += mesh.GetIndexCount(s) / 3;
                totalTriangles += triangles; totalVertices += mesh.vertexCount;
                report.AppendLine("MESH " + r.name + " vertices=" + mesh.vertexCount + " triangles=" + triangles + " slots=" + r.sharedMaterials.Length + " bones=" + (skin ? skin.bones.Length : 0) + " bounds=" + mesh.bounds.size.ToString("F4") + " enabled=" + r.enabled);
                foreach (var mat in r.sharedMaterials)
                    report.AppendLine("MATERIAL " + (mat ? AssetDatabase.GetAssetPath(mat) + " shader=" + (mat.shader ? mat.shader.name : "MISSING") : "MISSING"));
            }
            report.AppendLine("TOTAL_INCLUDING_INACTIVE vertices=" + totalVertices + " triangles=" + totalTriangles + " LODGroups=" + root.GetComponentsInChildren<LODGroup>(true).Length);
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
                report.AppendLine("CLIP " + clip.name + " seconds=" + clip.length + " loop=" + clip.isLooping + " bindings=" + AnimationUtility.GetCurveBindings(clip).Length);
            foreach (var c in root.GetComponentsInChildren<Component>(true))
                if (!c) report.AppendLine("MISSING_SCRIPT");
        }
        foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/ThirdParty/S14 ASSETS/")))
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!ti) continue;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            report.AppendLine("TEXTURE " + path + " imported=" + (texture ? texture.width + "x" + texture.height : "MISSING") + " sRGB=" + ti.sRGBTexture + " type=" + ti.textureType + " max=" + ti.maxTextureSize);
        }
        var output = File.ReadAllText("/tmp/lastsignal-s014-run").Trim() + "/unity-asset-audit.txt";
        using (var stream = new StreamWriter(new FileStream(output, FileMode.CreateNew))) stream.Write(report);
        result.Log("Read-only audit saved: " + output + " assets=" + paths.Count);
    }
}
