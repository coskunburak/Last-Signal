using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    [CustomEditor(typeof(FirstPersonWeaponGrip))]
    public sealed class FirstPersonWeaponGripEditor : UnityEditor.Editor
    {
        bool contacts = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var g=(FirstPersonWeaponGrip)target;
            contacts=EditorGUILayout.Toggle("Draw anatomical contacts",contacts);
            if(g.Profile && g.Profile.Pistol && GUILayout.Button("Select persistent pistol pose profile"))Selection.activeObject=g.Profile;
            if(Application.isPlaying) {
                EditorGUILayout.LabelField("Palm separation (mm)",(g.ContactError*1000).ToString("F2"));
                EditorGUILayout.LabelField("Unreachable distance (mm)",(g.ReachResidual*1000).ToString("F2"));
                if (g.Profile && g.Profile.TwoHandedMelee)
                {
                    EditorGUILayout.LabelField("Left palm separation / reach (mm)", $"{g.SecondaryContactError * 1000:F2} / {g.SecondaryReachResidual * 1000:F2}");
                    if (g.HasPose && !g.HasValidContact) EditorGUILayout.HelpBox("INVALID two-hand pose: contact or reach exceeds 20 mm. Recalibrate the weapon pose; do not conceal this by moving one socket to its clamped wrist.", MessageType.Error);
                }
                EditorGUILayout.LabelField("Wrist/forearm angle",g.WristBendDegrees.ToString("F1"));
                EditorGUILayout.HelpBox("Read final-frame samples with WeaponGripProbe for tests. Contact alone does not establish visual quality.",MessageType.Info);
                Repaint();
            }
        }
        void OnSceneGUI()
        {
            if(!contacts)return;
            var g=(FirstPersonWeaponGrip)target;
            if(!g.RightUpper||!g.RightLower||!g.RightHand||!g.Sockets)return;
            Handles.color=Color.cyan;
            Handles.DrawLine(g.RightUpper.position,g.RightLower.position);Handles.DrawLine(g.RightLower.position,g.RightHand.position);
            Mark(g.RightHand,"Wrist",Color.white);Mark(g.RightLower,"Elbow",Color.magenta);
            if (g.Profile && g.Profile.TwoHandedMelee)
            {
                Mark(g.Sockets.SecondaryGrip,"Left palm contact",Color.green);
                Handles.color=Color.cyan;
                Handles.DrawLine(g.LeftUpper.position,g.LeftLower.position); Handles.DrawLine(g.LeftLower.position,g.LeftHand.position);
                if(g.LeftFingers!=null) foreach(var bone in g.LeftFingers) if(bone&&bone.childCount>0) Handles.DrawLine(bone.position,bone.GetChild(0).position);
            }
            Mark(g.Sockets.PrimaryGrip,"Palm contact",Color.green);Mark(g.Sockets.AimReference,"Rear sight",Color.yellow);Mark(g.Sockets.Muzzle,"Muzzle",Color.red);
            if(g.RightFingers!=null)foreach(var bone in g.RightFingers)if(bone&&bone.childCount>0){Handles.color=Color.green;Handles.DrawLine(bone.position,bone.GetChild(0).position);}
            if(g.Visual)foreach(var renderer in g.Visual.GetComponentsInChildren<Renderer>())if(renderer.enabled){Handles.color=new Color(1,1,0,.3f);Handles.DrawWireCube(renderer.bounds.center,renderer.bounds.size);}
        }
        static void Mark(Transform t,string label,Color color)
        {
            if(!t)return;Handles.color=color;Handles.SphereHandleCap(0,t.position,Quaternion.identity,.005f,EventType.Repaint);
            Handles.Label(t.position,label);Handles.DrawLine(t.position,t.position+t.forward*.035f);
        }
    }
}
