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
        CharacterBodyVisibility visibleBody;
        public bool Inspecting => inspection && inspection.enabled;
        public Camera Inspection => inspection;
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
            if (keyboard.digit8Key.wasPressedThisFrame) SetInspection(!inspect);
            if (keyboard.digit9Key.wasPressedThisFrame) angle = (angle + 1) % Positions.Length;
        }
        public bool SetInspection(bool value)
        {
            var character = flow && flow.Player ? flow.Player.GetComponent<CharacterInspectionCamera>() : null;
            if (value && (!isActiveAndEnabled || (character && character.Inspecting))) return false;
            inspect = value;
            if (!value) Restore();
            return true;
        }
        void LateUpdate()
        {
            var characterInspection = flow && flow.Player ? flow.Player.GetComponent<CharacterInspectionCamera>() : null;
            if (characterInspection && characterInspection.Inspecting) { Restore(); return; }
            var actor = flow ? flow.GetComponent<VehicleWorld>()?.Actor : null;
            var look = flow && flow.Player ? flow.Player.GetComponent<FirstPersonLook>() : null;
            var view = look ? look.View : null;
            if (!inspect || !actor || !actor.Occupied || !view) { Restore(); return; }
            if (original != view)
            {
                if (!view.enabled) { Restore(); return; }
                Restore(); original = view; originalEnabled = view.enabled;
                inspection.CopyFrom(view); inspection.fieldOfView = 60;
                inspection.cullingMask = (view.cullingMask | (1 << 30)) & ~((1 << 29) | (1 << 2));
                visibleBody = flow.Player.GetComponentInChildren<CharacterBodyVisibility>();
                if (visibleBody) visibleBody.SetInspection(true);
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
            if (visibleBody) visibleBody.SetInspection(false);
            visibleBody = null;
            if (inspection) inspection.enabled = false;
        }
        void OnDisable() { inspect = false; Restore(); }
        void OnGUI()
        {
            GUI.Label(new Rect(20, 20, 620, 28), "DEVELOPMENT INSPECTION: 8 cockpit/exterior · 9 exterior angle");
        }
    }
}
#endif
