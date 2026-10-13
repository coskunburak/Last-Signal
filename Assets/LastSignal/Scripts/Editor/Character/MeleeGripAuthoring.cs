using System;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>Targeted, repeatable melee-only calibration. Never changes gameplay definitions or firearm assets.</summary>
    public static class MeleeGripAuthoring
    {
        const string Data = "Assets/LastSignal/Data/Combat/S021/";
        const string Prefabs = "Assets/LastSignal/Prefabs/Combat/S021/";
        [MenuItem("Last Signal/Character/Author Two Hand Axe and One Hand Blade")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring.");
            foreach (bool axe in new[] { true, false })
            {
                string name = axe ? "WoodenAxe" : "Machete";
                var p = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + name + "Pose.asset");
                if (!p) throw new InvalidOperationException("Missing melee profile: " + name);
                p.GripMode = axe ? WeaponGripMode.TwoHandedHeavyMelee : WeaponGripMode.OneHandedLightMelee;
                p.AttackOwnership = WeaponPoseOwnership.ProfileArmTargets;
                p.WorldScale = 1;
                p.FpsHipPosition = axe ? new Vector3(.14f, -.01f, .32f) : new Vector3(.14f, -.22f, .30f);
                p.FpsHipEuler = axe ? new Vector3(10, -8, -45) : new Vector3(48, 18, -28);
                p.HipGripOffset = axe ? new Vector3(-.20f, .04f, .40f) : new Vector3(.06f, -.25f, .30f);
                p.HipEuler = axe ? new Vector3(-10, -8, -28) : p.FpsHipEuler;
                if (axe)
                {
                    p.PoseBlendSeconds = .22f;
                    p.SprintEuler = new Vector3(12, -8, 4);
                    p.WindupOffset = new Vector3(.025f, .07f, -.05f);
                    p.WindupEuler = new Vector3(-24, 20, 18);
                    p.StrikeOffset = new Vector3(-.075f, -.025f, -.015f);
                    p.StrikeEuler = new Vector3(28, -22, -18);
                    p.FollowThroughOffset = new Vector3(-.09f, -.045f, -.025f);
                    p.FollowThroughEuler = new Vector3(36, -28, -23);
                    p.FollowThroughFraction = .18f;
                    p.FingerFlexion = new Vector3(62, 80, 48);
                    p.SecondaryFingerFlexion = new Vector3(58, 76, 48);
                    p.SecondaryThumbFlexion = new Vector3(25, 38, 26);
                    p.SecondaryThumbOpposition = 28;
                }
                EditorUtility.SetDirty(p);
                Edit(Prefabs + name + "_Viewmodel.prefab", name + "_Model", axe);
                Edit(Prefabs + "Diesel_World" + name + ".prefab", name + "_Model", axe);
            }
            AssetDatabase.SaveAssets();
        }
        static void Edit(string path, string modelName, bool axe)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var s = root.GetComponentInChildren<CharacterWeaponSockets>(true);
                if (!s || !s.PrimaryGrip) throw new InvalidOperationException("Missing authored primary contact: " + path);
                Transform model = null;
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == modelName) { model = t; break; }
                if (!model) throw new InvalidOperationException("Missing imported model: " + path);
                model.localScale = Vector3.one;
                s.PrimaryGrip.name = "PrimaryGrip_R";
                s.PrimaryGrip.localPosition = axe ? new Vector3(.011f, .002f, .08f) : new Vector3(.017f, -.004f, -.12f);
                s.PrimaryGrip.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.left);
                if (axe)
                {
                    var left = s.SecondaryGrip ? s.SecondaryGrip : model.Find("SecondaryGrip_L");
                    if (!left) { left = new GameObject("SecondaryGrip_L").transform; left.SetParent(model, false); }
                    left.name = "SecondaryGrip_L"; left.gameObject.layer = model.gameObject.layer;
                    left.localPosition = new Vector3(-.011f, .002f, -.12f);
                    left.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.right);
                    left.localScale = Vector3.one; s.LeftGrip = left;
                }
                else s.LeftGrip = null;
                if (s.SilhouetteReference) s.SilhouetteReference.name = "BladeReference";
                s.WeaponVisualRoot = s.transform;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
