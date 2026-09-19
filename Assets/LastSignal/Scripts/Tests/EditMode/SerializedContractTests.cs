using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Tests
{
    public class SerializedContractTests
    {
        [Test]
        public void PlayerPrefabHasSafeCapsuleAndCompleteReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            var capsule = prefab.GetComponent<CharacterController>();
            Assert.That(capsule.height, Is.EqualTo(1.8f));
            Assert.That(capsule.stepOffset, Is.LessThan(capsule.height - capsule.radius * 2));
            Assert.That(prefab.GetComponent<FirstPersonMotor>(), Is.Not.Null);
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>())
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Assert.That(property.objectReferenceValue, Is.Not.Null, component.GetType().Name + "." + property.propertyPath);
            }
        }
        [Test]
        public void InputAssetHasSeparateContextsAndTapInteraction()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            foreach (string name in new[] { "Move", "Look", "Sprint", "Crouch", "Interact", "Pause" })
                Assert.That(asset.FindAction("Player/" + name), Is.Not.Null);
            Assert.That(asset.FindAction("Player/Interact").interactions, Is.Null.Or.Empty);
            Assert.That(asset.FindAction("UI/Cancel"), Is.Not.Null);
        }
        [Test]
        public void AcceptanceSceneContainsSessionAndBothDoorFixturesWithoutMissingScripts()
        {
            var scene = EditorSceneManager.OpenScene("Assets/LastSignal/Scenes/S001Acceptance.unity");
            Assert.That(Object.FindObjectsByType<SessionFlow>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DoorInteractable>().Length, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<PlayerInputReader>().Length, Is.Zero, "Spawn from prefab, never a second scene player");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), Is.Zero);
            Assert.That(scene.isDirty, Is.False);
        }
    }
}
