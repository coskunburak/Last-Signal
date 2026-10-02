files = [
    "Assets/LastSignal/Scripts/Tests/PlayMode/CombatAcceptanceTests.cs",
    "Assets/LastSignal/Scripts/Tests/PlayMode/RuntimeSmokeTests.cs"
]

injection = """
            var myPi = Object.FindAnyObjectByType<PlayerInputReader>();
            if (myPi) {
                var myF = typeof(PlayerInputReader).GetField("instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var myA = (UnityEngine.InputSystem.InputActionAsset)myF.GetValue(myPi);
                if (myA != null) {
                    myA.Disable();
                    myA.devices = new UnityEngine.InputSystem.InputDevice[] { mouse, keyboard };
                    myA.Enable();
                }
            }
"""

for file in files:
    with open(file, "r") as f:
        content = f.read()
    
    content = content.replace("session.Resume();", "session.Resume();" + injection, 1)
    
    with open(file, "w") as f:
        f.write(content)

