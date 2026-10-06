using UnityEngine;
using MotionCore.Vehicle.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MotionCore.Vehicle.Camera
{
    public sealed class VehicleChaseCamera : MonoBehaviour
    {
        [System.Serializable]
        public struct CameraView
        {
            [Tooltip("Label for this view (for your reference in the inspector).")]
            public string name;
            [Tooltip("Camera position relative to the car (local space).")]
            public Vector3 followOffset;
            [Tooltip("How far ahead of the car the camera looks.")]
            public float lookAhead;
            [Tooltip("Field of view for this view (a speed boost is added on top).")]
            public float fieldOfView;
        }

        [Tooltip("Vehicle this camera follows.")]
        [SerializeField] private VehicleControllerBase target;

        [Tooltip("Camera views cycled with the view-switch key (C). The first is used on start.")]
        [SerializeField]
        private CameraView[] views = new CameraView[]
        {
            new CameraView { name = "Chase", followOffset = new Vector3(0f, 4.5f, -8.5f), lookAhead = 7f, fieldOfView = 62f },
            new CameraView { name = "Far",   followOffset = new Vector3(0f, 6f, -12f),    lookAhead = 9f, fieldOfView = 66f },
            new CameraView { name = "Hood",  followOffset = new Vector3(0f, 1.7f, 0.6f),  lookAhead = 14f, fieldOfView = 72f },
        };

        [Tooltip("How quickly the camera moves toward its target position.")]
        [SerializeField] private float positionSharpness = 9f;
        [Tooltip("How quickly the camera rotates toward its look target.")]
        [SerializeField] private float rotationSharpness = 8f;
        [Tooltip("Sideways look offset while the car is drifting.")]
        [SerializeField] private float driftLookSide = 2.2f;
        [Tooltip("Extra field of view added at top speed for a sense of speed.")]
        [SerializeField] private float speedFovBoost = 14f;
        [Tooltip("Camera to drive. Defaults to a Camera on this GameObject.")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        private int viewIndex;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<UnityEngine.Camera>();
            }
        }

        private void Update()
        {
            if (views != null && views.Length > 1 && WasViewSwitchPressed())
            {
                viewIndex = (viewIndex + 1) % views.Length;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            CameraView view = CurrentView();

            Transform targetTransform = target.transform;
            Vector3 desiredPosition = targetTransform.TransformPoint(view.followOffset);
            float positionBlend = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionBlend);

            Vector3 driftOffset = target.IsDrifting ? targetTransform.right * (Mathf.Sign(target.ForwardSpeed) * driftLookSide) : Vector3.zero;
            Vector3 lookTarget = targetTransform.position + targetTransform.forward * view.lookAhead + driftOffset;
            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
            }

            if (targetCamera == null)
            {
                return;
            }

            float desiredFieldOfView = view.fieldOfView + speedFovBoost * target.NormalizedSpeed;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, desiredFieldOfView, positionBlend);
        }

        private CameraView CurrentView()
        {
            if (views == null || views.Length == 0)
            {
                return new CameraView { name = "Default", followOffset = new Vector3(0f, 4.5f, -8.5f), lookAhead = 7f, fieldOfView = 62f };
            }

            return views[Mathf.Clamp(viewIndex, 0, views.Length - 1)];
        }

        private static bool WasViewSwitchPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                return true;
            }
#endif

            return false;
        }
    }
}
