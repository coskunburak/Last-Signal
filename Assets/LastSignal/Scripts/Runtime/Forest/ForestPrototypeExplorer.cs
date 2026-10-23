using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Forest
{
    /// <summary>Isolated scene controls; movement remains owned by the existing player motor.</summary>
    public sealed class ForestPrototypeExplorer : MonoBehaviour
    {
        public ForestPrototypeState state;
        public PlayerInputReader input;
        public FirstPersonMotor motor;
        public FirstPersonLook look;
        public ForestPrototypeLighting lighting;
        CursorLockMode previousLock;
        bool previousVisible;
        void Start()
        {
            var shadows = GetComponent<ForestTreeShadowSettings>();
            if (!shadows) shadows = gameObject.AddComponent<ForestTreeShadowSettings>();
            shadows.Initialize(this);
            previousLock = Cursor.lockState; previousVisible = Cursor.visible;
            input.SetGameplay(true);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                bool active = !input.GameplayActive;
                input.SetGameplay(active); Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !active;
            }
            if (!input.GameplayActive) return;
            if (keys.spaceKey.wasPressedThisFrame) motor.TryJump(1.05f);
            if (keys.f7Key.wasPressedThisFrame) lighting.Apply(false);
            if (keys.f8Key.wasPressedThisFrame) lighting.Apply(true);
            if (keys.f9Key.wasPressedThisFrame) GetComponent<ForestPrototypeCapture>().Begin();
            if (keys.f1Key.wasPressedThisFrame) GoToShot(0);
            if (keys.f2Key.wasPressedThisFrame) GoToShot(1);
            if (keys.f3Key.wasPressedThisFrame) GoToShot(2);
            if (keys.f4Key.wasPressedThisFrame) GoToShot(3);
            if (keys.f5Key.wasPressedThisFrame) GoToShot(4);
            if (keys.f6Key.wasPressedThisFrame) GoToShot(5);
        }
        public void GoToShot(int index)
        {
            if (index < 0 || index >= state.shots.Length) return;
            var shot = state.shots[index];
            var controller = state.player.GetComponent<CharacterController>();
            controller.enabled = false;
            state.player.SetPositionAndRotation(shot.position - Vector3.up * 1.62f, Quaternion.Euler(0, shot.eulerAngles.y, 0));
            look.RestorePitch(Mathf.DeltaAngle(0, shot.eulerAngles.x));
            controller.enabled = true;
            lighting.Apply(index == 5);
        }
        void OnDestroy()
        {
            Cursor.lockState = previousLock; Cursor.visible = previousVisible;
        }
    }
}
