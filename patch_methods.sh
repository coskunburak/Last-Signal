cat << 'INNER_EOF' >> Assets/LastSignal/Scripts/Tests/PlayMode/CombatAcceptanceTests.cs

        void Press(UnityEngine.InputSystem.Controls.ButtonControl control) {
            using (UnityEngine.InputSystem.StateEvent.From(control.device, out var stateEvent)) {
                control.WriteValueIntoEvent(1f, stateEvent);
                UnityEngine.InputSystem.InputSystem.QueueEvent(stateEvent);
            }
        }

        void Release(UnityEngine.InputSystem.Controls.ButtonControl control) {
            using (UnityEngine.InputSystem.StateEvent.From(control.device, out var stateEvent)) {
                control.WriteValueIntoEvent(0f, stateEvent);
                UnityEngine.InputSystem.InputSystem.QueueEvent(stateEvent);
            }
        }
}
INNER_EOF
