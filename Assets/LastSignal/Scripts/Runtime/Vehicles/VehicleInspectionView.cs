#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Vehicles
{
    /// <summary>Explicit opt-in build inspection, never the production driving camera.</summary>
    public sealed class VehicleInspectionView : MonoBehaviour
    {
        SessionFlow flow;
        Camera inspection, original;
        bool inspect, originalEnabled;
        int angle;
        static readonly Vector3[] Positions = {
            new Vector3(-4, 1.7f, 3), new Vector3(4, 1.7f, 3),
            new Vector3(-4, 1.7f, -3), new Vector3(4, 1.7f, -3)
        };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-veh001Inspect") >= 0)
                new GameObject("VEH-001 opt-in inspection").AddComponent<VehicleInspectionView>();
        }
        void Start()
        {
            flow = FindAnyObjectByType<SessionFlow>();
            inspection = gameObject.AddComponent<Camera>(); inspection.enabled = false;
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit8Key.wasPressedThisFrame) inspect = !inspect;
            if (keyboard.digit9Key.wasPressedThisFrame) angle = (angle + 1) % Positions.Length;
        }
        void LateUpdate()
        {
            var actor = flow ? flow.GetComponent<VehicleWorld>()?.Actor : null;
            var view = flow && flow.Player ? flow.Player.GetComponent<FirstPersonLook>().View : null;
            if (!inspect || !actor || !actor.Occupied || !view) { Restore(); return; }
            if (original != view)
            {
                Restore(); original = view; originalEnabled = view.enabled;
                inspection.CopyFrom(view); inspection.fieldOfView = 60;
            }
            original.enabled = false; inspection.enabled = true;
            inspection.transform.position = actor.transform.TransformPoint(Positions[angle]);
            inspection.transform.LookAt(actor.transform.position + actor.transform.up);
            inspection.transform.position += actor.transform.TransformVector(actor.BodyFeelPosition) * .4f;
            inspection.transform.rotation *= Quaternion.Euler(actor.BodyFeelAngles * .4f);
        }
        void Restore()
        {
            if (original) original.enabled = originalEnabled;
            original = null;
            if (inspection) inspection.enabled = false;
        }
        void OnDisable() => Restore();
        void OnGUI()
        {
            GUI.Label(new Rect(20, 20, 620, 28), "DEVELOPMENT INSPECTION: 8 cockpit/exterior · 9 exterior angle");
        }
    }
}
#endif
