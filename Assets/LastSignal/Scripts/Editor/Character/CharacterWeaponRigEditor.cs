using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    [CustomEditor(typeof(CharacterWeaponRig))]
    public sealed class CharacterWeaponRigEditor : UnityEditor.Editor
    {
        static bool drawContacts;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            drawContacts = EditorGUILayout.Toggle("Draw selected pose contacts", drawContacts);
            var rig = (CharacterWeaponRig)target;
            if (!Application.isPlaying || !rig.ActiveProfile || !rig.WeaponVisual || !rig.WeaponVisual.activeSelf)
            {
                EditorGUILayout.HelpBox("Enter Play and equip a weapon. Select WorldBody to inspect the final pose. Geometry does not establish visual acceptance.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Pose weight", rig.PoseWeight.ToString("F3"));
            EditorGUILayout.LabelField("Support hand weight", rig.SupportHandWeight.ToString("F3"));
            if (rig.ActiveProfile.Pistol && rig.ActiveProfile.WorldTwoHandedPistol &&
                (!rig.ActiveProfile.ValidWorldPistolSupport || !rig.Sockets || !rig.Sockets.HasWorldPistolSupport))
                EditorGUILayout.HelpBox("Invalid TPS pistol support settings or PistolSupport reference.", MessageType.Warning);
            EditorGUILayout.LabelField("Palm error R / L (mm)", $"{rig.RightGripError * 1000:F1} / {rig.LeftGripError * 1000:F1}");
            EditorGUILayout.LabelField("Unreachable distance R / L (mm)", $"{rig.RightReachResidual * 1000:F1} / {rig.LeftReachResidual * 1000:F1}");
            EditorGUILayout.LabelField("Jacket penetration R / L (mm)", $"{rig.RightTorsoPenetration * 1000:F1} / {rig.LeftTorsoPenetration * 1000:F1}");
            if (Mathf.Max(rig.RightTorsoPenetration, rig.LeftTorsoPenetration) > .003f)
                EditorGUILayout.HelpBox("Arm intersects the jacket envelope. Contact alone is insufficient for acceptance.", MessageType.Warning);
            if (rig.HasFirearmPose) EditorGUILayout.LabelField("Hip clearance correction (degrees)", rig.HipClearanceDegrees.ToString("F2"));
            if (rig.ActiveProfile.TwoHandedMelee)
                EditorGUILayout.LabelField("Shared axe placement adjustment (mm)", (rig.MeleePlacementCorrection * 1000).ToString("F1"));
            if (rig.RightReachResidual > .01f || rig.LeftReachResidual > .01f)
                EditorGUILayout.HelpBox("Contact exceeds bounded arm reach. The arm clamps without stretching; single-hand visuals retain solved palm contact. Inspect the silhouette before changing calibration.", MessageType.Warning);
            Repaint();
        }
        void OnSceneGUI()
        {
            var rig = (CharacterWeaponRig)target;
            if (!drawContacts || !Application.isPlaying || !rig.ActiveProfile || !rig.WeaponVisual || !rig.WeaponVisual.activeSelf || !rig.Sockets || !rig.Sockets.PrimaryGrip) return;
            if (rig.Sockets.StockContact) { Point(rig.StockTarget, Color.yellow, "Shoulder pocket");
                Point(rig.Sockets.StockContact.position, Color.green, "Stock"); }
            Point(rig.Sockets.PrimaryGrip.position, Color.cyan, "Right palm target");
            if (rig.SupportHandWeight > 0) Point(rig.LeftPalmTarget, Color.cyan, "Left palm target / reload");
            Point(rig.RightWristTarget, Color.white, "Right wrist target");
            if (rig.SupportHandWeight > 0) Point(rig.LeftWristTarget, Color.white, "Left wrist target");
            Handles.color = Color.red;
            Handles.DrawLine(rig.RightPalmPosition, rig.Sockets.PrimaryGrip.position);
            if (rig.SupportHandWeight > 0) Handles.DrawLine(rig.LeftPalmPosition, rig.LeftPalmTarget);
            var animator = rig.GetComponent<Animator>();
            Arm(animator, false, rig.RightElbowHint, rig.RightSolvedWrist, rig.ActiveProfile.MaximumReach);
            if (rig.SupportHandWeight > 0) Arm(animator, true, rig.LeftElbowHint, rig.LeftSolvedWrist, rig.ActiveProfile.MaximumReach);
        }
        static void Arm(Animator animator, bool left, Vector3 hint, Vector3 solved, float reach)
        {
            var upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            var lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (!upper || !lower || !hand) return;
            Point(hint, Color.magenta, left ? "Left elbow hint" : "Right elbow hint");
            Point(hand.position, Color.green, "Actual wrist");
            Point(solved, Color.yellow, "Reach-clamped wrist");
            Handles.color = new Color(0, 1, 1, .3f);
            float radius = (Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position)) * reach;
            Handles.DrawWireDisc(upper.position, Vector3.up, radius);
            Handles.DrawWireDisc(upper.position, Vector3.right, radius);
            Handles.DrawLine(upper.position, lower.position); Handles.DrawLine(lower.position, hand.position);
        }
        static void Point(Vector3 position, Color color, string label)
        {
            Handles.color = color;
            Handles.SphereHandleCap(0, position, Quaternion.identity, .008f, EventType.Repaint);
            Handles.Label(position + Vector3.up * .012f, label);
        }
    }
}
