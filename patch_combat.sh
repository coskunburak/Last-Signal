sed -i '' 's/: InputTestFixture//g' Assets/LastSignal/Scripts/Tests/PlayMode/CombatAcceptanceTests.cs
sed -i '' 's/base.Setup();/Time.timeScale = 1; mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();/g' Assets/LastSignal/Scripts/Tests/PlayMode/CombatAcceptanceTests.cs
sed -i '' 's/base.TearDown();/if (mouse != null) InputSystem.RemoveDevice(mouse); if (keyboard != null) InputSystem.RemoveDevice(keyboard);/g' Assets/LastSignal/Scripts/Tests/PlayMode/CombatAcceptanceTests.cs
