using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LastSignal.Editor
{
    public static class ZombieDamageAuthoring
    {
        public const string Evidence = "Docs/Implementation/S004/Evidence/20260919-P4";
        public const int HitLayer = 8;
        public static void Compose()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop before authoring.");
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer = tags.FindProperty("layers").GetArrayElementAtIndex(HitLayer);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != "EnemyHitRegion") throw new InvalidOperationException("Layer 8 occupied");
            layer.stringValue = "EnemyHitRegion"; tags.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(HitLayer, i, true);
            var tuning = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(ZombieAcceptanceAuthoring.DefinitionPath);
            var data = new SerializedObject(tuning);
            data.FindProperty("hitReactClip").objectReferenceValue = ZombieAssetIntegration.Clip("Zombie@Damage01");
            data.FindProperty("deathClip").objectReferenceValue = ZombieAssetIntegration.Clip("LS_Zombie_Death");
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(tuning);
            var root = PrefabUtility.LoadPrefabContents(ZombieRuntimeAuthoring.PrefabPath);
            try
            {
                var health = root.GetComponent<ZombieHealth>(); if (!health) health = root.AddComponent<ZombieHealth>();
                var animator = root.GetComponentInChildren<Animator>();
                // Project-owned children, attached to bones without editing vendor source or source presentation prefab.
                AddRegion(animator.GetBoneTransform(HumanBodyBones.Head), "Damage_Head", health, DamageRegion.Head,
                    new Vector3(.00046f, -.02430f, -.06782f), new Vector3(.20f, .27f, .24f), 2);
                AddRegion(animator.GetBoneTransform(HumanBodyBones.Chest), "Damage_Chest", health, DamageRegion.Body,
                    new Vector3(.0006f, -.0407f, -.1396f), new Vector3(.35f, .24f, .42f), 1);
                AddRegion(animator.GetBoneTransform(HumanBodyBones.Hips), "Damage_Pelvis", health, DamageRegion.Body,
                    new Vector3(.001f, .0096f, .0191f), new Vector3(.33f, .27f, .19f), 1);
                AddSegment(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, .11f, health);
                AddSegment(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, .11f, health);
                AddSegment(animator, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, .09f, health);
                AddSegment(animator, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, .09f, health);
                AddSegment(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, .17f, health);
                AddSegment(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, .17f, health);
                AddSegment(animator, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, .11f, health);
                AddSegment(animator, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, .11f, health);
                PrefabUtility.SaveAsPrefabAsset(root, ZombieRuntimeAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            const string weapon = "Assets/LastSignal/Prefabs/Combat/Weapon_AssaultRifle.prefab";
            var gun = PrefabUtility.LoadPrefabContents(weapon);
            try
            {
                var serialized = new SerializedObject(gun.GetComponent<WeaponController>());
                serialized.FindProperty("hitMask").intValue |= 1 << HitLayer;
                serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(gun, weapon);
            }
            finally { PrefabUtility.UnloadPrefabContents(gun); }
            AssetDatabase.SaveAssets();
        }
        static void AddSegment(Animator animator, HumanBodyBones start, HumanBodyBones end, float width, ZombieHealth health)
        {
            var bone = animator.GetBoneTransform(start); var tip = animator.GetBoneTransform(end);
            Vector3 local = bone.InverseTransformPoint(tip.position);
            string name = "Damage_" + start;
            AddRegion(bone, name, health, DamageRegion.Body, Vector3.zero, new Vector3(width, width, local.magnitude), 1);
            var region = bone.Find(name); region.localPosition = local * .5f;
            region.localRotation = Quaternion.LookRotation(local.normalized);
        }
        static void AddRegion(Transform bone, string name, ZombieHealth owner, DamageRegion type, Vector3 center, Vector3 size, float multiplier)
        {
            if (!bone) throw new InvalidOperationException("Missing Humanoid bone");
            var child = bone.Find(name); if (!child) { child = new GameObject(name).transform; child.SetParent(bone, false); }
            child.gameObject.layer = HitLayer;
            var collider = child.GetComponent<BoxCollider>(); if (!collider) collider = child.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = false; collider.center = center; collider.size = size;
            var region = child.GetComponent<ZombieHitRegion>(); if (!region) region = child.gameObject.AddComponent<ZombieHitRegion>();
            region.Configure(owner, type, multiplier, collider);
        }
        public static void Audit()
        {
            Directory.CreateDirectory(Evidence + "/clip-audit");
            var csv = new StringBuilder("clip,time,rootX,rootY,rootZ,rootYaw,headX,headY,headZ,hipsX,hipsY,hipsZ,meshMinY\n");
            var summary = new StringBuilder();
            foreach (string name in new[] { "Zombie@Damage01", "LS_Zombie_Death" })
            {
                var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ZombieRuntimeAuthoring.PrefabPath));
                var graph = PlayableGraph.Create("P4 clip measurement"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                try
                {
                    var animator = go.GetComponentInChildren<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var clip = ZombieAssetIntegration.Clip(name);
                    var playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false);
                    AnimationPlayableOutput.Create(graph, "Audit", animator).SetSourcePlayable(playable); graph.Play();
                    var mesh = new Mesh(); float minY = float.MaxValue, maxHeadDelta = 0; Vector3 firstHead = default;
                    try
                    {
                        for (int i = 0; i <= 120; i++)
                        {
                            float time = clip.length * i / 120; playable.SetTime(time); graph.Evaluate(0);
                            var head = animator.GetBoneTransform(HumanBodyBones.Head).position;
                            var hips = animator.GetBoneTransform(HumanBodyBones.Hips).position;
                            if (i == 0) firstHead = head;
                            maxHeadDelta = Mathf.Max(maxHeadDelta, Vector3.Distance(head, firstHead));
                            float low = float.MaxValue;
                            foreach (var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {
                                skin.BakeMesh(mesh);
                                foreach (var v in mesh.vertices) low = Mathf.Min(low, skin.transform.TransformPoint(v).y);
                            }
                            minY = Mathf.Min(minY, low); var pos = animator.transform.position;
                            csv.AppendLine(string.Join(",", name, time, pos.x, pos.y, pos.z, animator.transform.eulerAngles.y, head.x, head.y, head.z, hips.x, hips.y, hips.z, low));
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(mesh); }
                    summary.AppendLine(name + " duration=" + clip.length + " fps=" + clip.frameRate + " loop=" + clip.isLooping + " events=" + clip.events.Length + " importedRootSpeed=" + clip.averageSpeed + " maxHeadDisplacement=" + maxHeadDelta + " minimumSkinY=" + minY + " rootMotion=" + animator.applyRootMotion);
                }
                finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(go); }
            }
            File.WriteAllText(Evidence + "/clip-audit/samples.csv", csv.ToString());
            File.WriteAllText(Evidence + "/clip-audit/summary.txt", summary.ToString());
        }
    }
}
