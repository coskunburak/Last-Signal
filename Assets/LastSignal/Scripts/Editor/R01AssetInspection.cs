using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LastSignal.EditorTools
{
    public static class R01AssetInspection
    {
        public static void Run()
        {
            var report = new StringBuilder();
            string[] paths = {
                "Assets/LastSignal/Models/Weapons/Crowbar/Source/Crowbar.obj",
                "Assets/ThirdParty/Characters/FirstPersonArms/source/fpsarms.fbx",
                "Assets/ThirdParty/Zombies/Zombie_Survival_AssetPack_LowPoly/Modelos/Low Poly Zombie Survival Asset Pack.fbx"
            };
            foreach (string path in paths)
            {
                report.AppendLine(path);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer) report.AppendLine($"rig={importer.animationType} scale={importer.globalScale} units={importer.useFileScale}");
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (a is Mesh m) report.AppendLine($"mesh={m.name} vertices={m.vertexCount} triangles={m.triangles.Length / 3} bounds={m.bounds}");
                    if (a is AnimationClip c) report.AppendLine($"clip={c.name} length={c.length} humanoid={c.humanMotion}");
                    if (a is Avatar v) report.AppendLine($"avatar={v.name} valid={v.isValid} human={v.isHuman}");
                }
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(model) foreach(var t in model.GetComponentsInChildren<Transform>(true)) report.AppendLine($"  {t.name}: local={t.localPosition} rot={t.localEulerAngles} scale={t.localScale}");
            }
            File.WriteAllText("Docs/Implementation/PreS010-Recovery/Evidence/R01/20260926/asset-inspection.txt", report.ToString());
        }
    }
}
