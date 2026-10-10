using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace LastSignal
{
    /// <summary>F6 toggle, F7 angle. Observes the real entity without changing input or aim authority.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(500)]
    public sealed class CharacterInspectionCamera : MonoBehaviour
    {
        static readonly Vector3[] Angles = { new Vector3(0, .3f, 1), new Vector3(0, .3f, -1),
            new Vector3(-1, .3f, 0), new Vector3(1, .3f, 0), new Vector3(.8f, .3f, .8f) };
        static readonly string[] Names = { "Front", "Back", "Left", "Right", "Three-quarter" };
        [SerializeField] Camera firstPerson;
        [SerializeField] CharacterBodyVisibility body;
        [SerializeField, Min(1)] float distance = 3.2f;
        Camera inspection;
        PlayerInputReader input;
        PlayerLocomotionPresenter state;
        bool originalEnabled;
        int angle;
        public bool Inspecting { get; private set; }
        public Camera Inspection => inspection;
        public void Configure(Camera view, CharacterBodyVisibility visibility) { firstPerson = view; body = visibility; }
        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            state = body ? body.GetComponent<PlayerLocomotionPresenter>() : null;
        }
        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current == null || !body || !body.LocalOwner) return;
            if (Keyboard.current.f6Key.wasPressedThisFrame) SetInspection(!Inspecting);
            if (Inspecting && Keyboard.current.f7Key.wasPressedThisFrame) angle = (angle + 1) % Angles.Length;
#endif
        }
        public void SetInspection(bool value)
        {
            if (value == Inspecting || (value && (!isActiveAndEnabled || !firstPerson || !body || !body.LocalOwner || !firstPerson.enabled))) return;
            if (value)
            {
                if (!inspection)
                {
                    var go = new GameObject("CharacterInspectionCamera"); go.transform.SetParent(transform, false);
                    inspection = go.AddComponent<Camera>(); inspection.enabled = false;
                    inspection.CopyFrom(firstPerson);
                    var data = inspection.GetUniversalAdditionalCameraData();
                    data.renderType = CameraRenderType.Base; data.cameraStack?.Clear();
                    inspection.cullingMask = (firstPerson.cullingMask | (1 << 30)) & ~((1 << 29) | (1 << 2));
                    inspection.fieldOfView = 55; inspection.nearClipPlane = .06f;
                    inspection.targetTexture = null;
                }
                originalEnabled = firstPerson.enabled; firstPerson.enabled = false;
                inspection.enabled = true; body.SetInspection(true); Inspecting = true;
                PositionCamera();
            }
            else Restore();
        }
        void LateUpdate()
        {
            if (!Inspecting) return;
            if (!firstPerson || !body || !body.isActiveAndEnabled || !body.LocalOwner) { Restore(); return; }
            PositionCamera();
        }
        void PositionCamera()
        {
            Vector3 target = transform.position + Vector3.up * (state && state.SeatedNow ? .35f : state && state.Crouching ? .65f : .95f);
            Vector3 delta = transform.rotation * Angles[angle].normalized * distance;
            // Layer 2 player is excluded; character visuals have no colliders.
            if (!(input && input.InVehicle) && Physics.SphereCast(target, .12f, delta.normalized, out var hit, distance, ~(1 << 2), QueryTriggerInteraction.Ignore))
                delta = delta.normalized * Mathf.Max(.2f, hit.distance - .1f);
            inspection.transform.SetPositionAndRotation(target + delta, Quaternion.LookRotation(-delta, Vector3.up));
        }
        void Restore()
        {
            if (Inspecting && firstPerson) firstPerson.enabled = originalEnabled;
            if (inspection) inspection.enabled = false;
            if (body) body.SetInspection(false);
            Inspecting = false;
        }
        void OnDisable() => Restore();
        void OnDestroy() { Restore(); if (inspection) Destroy(inspection.gameObject); }
        void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Inspecting) GUI.Label(new Rect(16, 16, 650, 26), "CHARACTER: F6 FPS / inspection · F7 angle: " + Names[angle] + " · movement/aim still use FPS authority");
#endif
        }
    }
}
