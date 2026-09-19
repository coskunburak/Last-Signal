using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal
{
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        InputActionAsset instance;
        InputActionMap gameplay, ui;
        InputAction move, look, sprint, crouch, interact, pause, cancel;
        InputAction attack, aim, reload;
        bool neutralRequired = true;
        public bool GameplayActive { get; private set; }
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool FireHeld { get; private set; }
        public bool AimHeld { get; private set; }
        public event Action InteractRequested;
        public event Action CrouchRequested;
        public event Action PauseRequested;
        public event Action FocusLost;
        public event Action FirePressed;
        public event Action FireReleased;
        public event Action AimPressed;
        public event Action AimReleased;
        public event Action ReloadRequested;

        public void Configure(InputActionAsset source) => actions = source;

        void Awake()
        {
            if (!actions) { Debug.LogError("Player input requires an InputActionAsset.", this); enabled = false; return; }
            instance = Instantiate(actions);
            gameplay = instance.FindActionMap("Player", true);
            ui = instance.FindActionMap("UI", true);
            move = gameplay.FindAction("Move", true);
            look = gameplay.FindAction("Look", true);
            sprint = gameplay.FindAction("Sprint", true);
            crouch = gameplay.FindAction("Crouch", true);
            interact = gameplay.FindAction("Interact", true);
            pause = gameplay.FindAction("Pause", true);
            cancel = ui.FindAction("Cancel", true);
            attack = gameplay.FindAction("Attack", true);
            aim = gameplay.FindAction("Aim", false); // May not exist in older asset versions
            reload = gameplay.FindAction("Reload", false);
        }

        void OnEnable()
        {
            if (!instance) return;
            interact.performed += OnInteract;
            crouch.performed += OnCrouch;
            pause.performed += OnPause;
            cancel.performed += OnPause;
            attack.performed += OnFirePressed;
            attack.canceled += OnFireReleased;
            if (aim != null) { aim.performed += OnAimPressed; aim.canceled += OnAimReleased; }
            if (reload != null) reload.performed += OnReload;
            SetGameplay(false);
        }

        void OnDisable()
        {
            if (!instance) return;
            interact.performed -= OnInteract;
            crouch.performed -= OnCrouch;
            pause.performed -= OnPause;
            cancel.performed -= OnPause;
            attack.performed -= OnFirePressed;
            attack.canceled -= OnFireReleased;
            if (aim != null) { aim.performed -= OnAimPressed; aim.canceled -= OnAimReleased; }
            if (reload != null) reload.performed -= OnReload;
            instance.Disable();
            GameplayActive = false;
            Clear();
        }

        void OnDestroy() { if (instance) Destroy(instance); }

        public void SetGameplay(bool active)
        {
            GameplayActive = active && isActiveAndEnabled;
            Clear();
            neutralRequired = true;
            if (!instance) return;
            instance.Disable();
            if (GameplayActive) gameplay.Enable(); else ui.Enable();
        }

        void Clear() { Move = Look = Vector2.zero; SprintHeld = false; FireHeld = false; AimHeld = false; }

        void Update()
        {
            if (!GameplayActive) { Clear(); return; }
            // Require release after focus/menu transitions. Held keys cannot resume a stale intent.
            if (neutralRequired)
            {
                neutralRequired = move.ReadValue<Vector2>().sqrMagnitude > .001f ||
                    sprint.IsPressed() || crouch.IsPressed() || interact.IsPressed() ||
                    attack.IsPressed() || (aim != null && aim.IsPressed());
                Clear();
                return;
            }
            Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            Look = look.ReadValue<Vector2>();
            SprintHeld = sprint.IsPressed();
            FireHeld = attack.IsPressed();
            AimHeld = aim != null && aim.IsPressed();
        }

        void OnInteract(InputAction.CallbackContext _) { if (GameplayActive && !neutralRequired) InteractRequested?.Invoke(); }
        void OnCrouch(InputAction.CallbackContext _) { if (GameplayActive && !neutralRequired) CrouchRequested?.Invoke(); }
        void OnPause(InputAction.CallbackContext _) => PauseRequested?.Invoke();
        void OnFirePressed(InputAction.CallbackContext _) { if (GameplayActive && !neutralRequired) FirePressed?.Invoke(); }
        void OnFireReleased(InputAction.CallbackContext _) { if (GameplayActive) FireReleased?.Invoke(); }
        void OnAimPressed(InputAction.CallbackContext _) { if (GameplayActive && !neutralRequired) AimPressed?.Invoke(); }
        void OnAimReleased(InputAction.CallbackContext _) { if (GameplayActive) AimReleased?.Invoke(); }
        void OnReload(InputAction.CallbackContext _) { if (GameplayActive && !neutralRequired) ReloadRequested?.Invoke(); }
        public void NotifyFocusLost()
        {
            SetGameplay(false);
            FocusLost?.Invoke();
        }
        void OnApplicationFocus(bool focused) { if (!focused) NotifyFocusLost(); }
    }
}
