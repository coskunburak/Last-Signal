using UnityEngine.InputSystem;

namespace LastSignal.Tests
{
    internal static class InputFixtureIsolation
    {
        // InputTestFixture cihaz yöneticisini değiştirir. Önceki sahnenin etkin
        // eylemleri eski yöneticide kontrol izleyicileri bırakmamalıdır.
        // Liste bir anlık kopyadır; Disable sırasında etkin eylem listesi değişebilir.
        public static void DisableLiveActions()
        {
            var actions = InputSystem.ListEnabledActions();
            foreach (var action in actions) action.Disable();
        }
    }
}
