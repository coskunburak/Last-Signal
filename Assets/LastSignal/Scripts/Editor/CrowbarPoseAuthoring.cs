using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    // A focused, repeatable adjustment to the existing first-person rig. It does
    // not rebuild the weapon or replace the source meshes/materials.
    public static class CrowbarPoseAuthoring
    {
        const string PrefabPath = "Assets/LastSignal/Combat/Crowbar/Crowbar_Viewmodel.prefab";

        [MenuItem("Last Signal/Combat/Refine crowbar pose")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var bones = root.GetComponentsInChildren<Transform>(true);
                Transform Bone(string name) => bones.Single(t => t.name == name);

                // The source bind pose reaches both hands straight toward the camera.
                // Bring the working hand into a compact guard and leave the other hand
                // relaxed below it, instead of making it point at the crosshair.
                PoseArm(Bone("R_arm"), Bone("R_elbow"), Bone("R_wrist"),
                    new Vector3(.16f, -.34f, .43f), new Vector3(.34f, -.48f, .10f));
                PoseArm(Bone("L_arm"), Bone("L_elbow"), Bone("L_wrist"),
                    new Vector3(-.14f, -.38f, .35f), new Vector3(-.30f, -.50f, -.05f));

                var palm = Bone("R_palm");
                var grip = Bone("GripAnchor");
                grip.position = palm.position + new Vector3(-.005f, .004f, .005f);
                grip.rotation = Quaternion.Euler(12f, 0f, 11f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Crowbar guard pose saved to " + PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void PoseArm(Transform shoulder, Transform elbow, Transform wrist,
            Vector3 targetWrist, Vector3 pole)
        {
            var start = shoulder.position;
            var upper = Vector3.Distance(start, elbow.position);
            var lower = Vector3.Distance(elbow.position, wrist.position);
            var direction = targetWrist - start;
            var distance = Mathf.Clamp(direction.magnitude, .001f, upper + lower - .001f);
            direction.Normalize();
            var projection = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            var height = Mathf.Sqrt(Mathf.Max(0f, upper * upper - projection * projection));
            var bend = Vector3.ProjectOnPlane(pole, direction).normalized;
            var targetElbow = start + direction * projection + bend * height;

            shoulder.rotation = Quaternion.FromToRotation(elbow.position - start,
                targetElbow - start) * shoulder.rotation;
            elbow.rotation = Quaternion.FromToRotation(wrist.position - elbow.position,
                targetWrist - elbow.position) * elbow.rotation;
        }

    }
}
