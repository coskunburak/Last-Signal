using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    // Manual, bounded validation of the two body-parts candidates and the current gameplay wrapper.
    public static class StudioNewPunchZombieValidator
    {
        const string Shirtless = "Assets/LastSignal/Assets/Zombie/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_BodyParts_FREE_URP.prefab";
        const string ZombieMale = "Assets/LastSignal/Assets/Zombie/ZombieMale_AAB/Prefabs/URP/ZombieMale_AAB_BodyParts_URP.prefab";
        const string Runtime = "Assets/Resources/LS_Zombie_Runtime.prefab";

        [MenuItem("Last Signal/Zombie/Validate Studio New Punch Candidates")]
        public static void Validate()
        {
            var report = new StringBuilder();
            bool valid = ValidateCandidate(Shirtless, new[] { "Head", "ArmL", "ArmR", "ForeArmL", "ForeArmR", "HandL", "HandR", "Ribcage" }, report);
            valid &= ValidateCandidate(ZombieMale, new[] { "Head_01", "Arm_L", "Arm_R", "ForeArm_L", "ForeArm_R", "Hand_L", "Hand_R", "Torso" }, report);
            valid &= ValidateRuntime(report);
            if (valid) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
        }

        static bool ValidateCandidate(string path, string[] requiredMeshes, StringBuilder report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) { report.AppendLine("MISSING prefab: " + path); return false; }
            bool valid = true;
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
            { report.AppendLine("INVALID Humanoid Avatar: " + path); valid = false; }
            var names = new HashSet<string>();
            var skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int triangles = 0;
            foreach (var skin in skins)
            {
                names.Add(skin.name);
                if (!skin.sharedMesh) { report.AppendLine("MISSING mesh: " + skin.name); valid = false; continue; }
                for (int submesh = 0; submesh < skin.sharedMesh.subMeshCount; submesh++)
                    triangles += (int)skin.sharedMesh.GetIndexCount(submesh) / 3;
                foreach (var material in skin.sharedMaterials)
                    if (!material || !material.shader || !material.shader.name.StartsWith("Universal Render Pipeline/"))
                    { report.AppendLine("NON-URP or missing material: " + skin.name); valid = false; }
            }
            foreach (var meshName in requiredMeshes)
                if (!names.Contains(meshName)) { report.AppendLine("MISSING body mesh: " + meshName); valid = false; }
            report.AppendLine(path + " | skins=" + skins.Length + " triangles=" + triangles + " | " + (valid ? "STRUCTURE OK" : "STRUCTURE FAIL"));
            return valid;
        }

        static bool ValidateRuntime(StringBuilder report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Runtime);
            if (!prefab) { report.AppendLine("MISSING runtime prefab"); return false; }
            var health = prefab.GetComponent<ZombieHealth>();
            bool valid = health && prefab.GetComponents<ZombieHealth>().Length == 1;
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (!animator || animator.applyRootMotion) { report.AppendLine("INVALID runtime root-motion policy"); valid = false; }
            var found = new HashSet<ZombieBodyPart>();
            foreach (var region in prefab.GetComponentsInChildren<ZombieHitRegion>(true))
            {
                if (region.Owner != health || !region.HitCollider) valid = false;
                found.Add(region.BodyPart);
            }
            foreach (var part in new[] { ZombieBodyPart.Head, ZombieBodyPart.Torso, ZombieBodyPart.LeftArm, ZombieBodyPart.RightArm })
                if (!found.Contains(part)) { report.AppendLine("MISSING runtime hit region: " + part); valid = false; }
            report.AppendLine(Runtime + " | " + (valid ? "AUTHORITY/REGIONS OK" : "AUTHORITY/REGIONS FAIL"));
            report.AppendLine("Topology and wound appearance still require visual inspection; this menu does not certify them.");
            return valid;
        }
    }
}
