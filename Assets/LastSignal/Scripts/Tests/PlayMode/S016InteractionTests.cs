#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed class S016InteractionProbe : MonoBehaviour, IInteractable
    {
        public int Accepted { get; private set; }
        public bool Available => isActiveAndEnabled;
        public string Prompt => "E — Denetle";
        public bool TryInteract() { if (!Available) return false; Accepted++; return true; }
    }

    public sealed class S016InteractionTests : InputTestFixture
    {
        GameObject player;
        PlayerInputReader input;
        InteractionController interaction;
        Transform view;
        readonly System.Collections.Generic.List<GameObject> owned = new System.Collections.Generic.List<GameObject>();

        InputFixtureIsolation.SceneScope sceneScope;
        public override void Setup()
        {
            sceneScope = new InputFixtureIsolation.SceneScope(); InputFixtureIsolation.DisableLiveActions();
            base.Setup();
            InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/LastSignal/Prefabs/Player/Player.prefab"), Vector3.zero, Quaternion.identity);
            player.AddComponent<LastSignal.Inventory.PlayerInventory>().Initialize(24);
            player.GetComponent<FirstPersonMotor>().enabled = false;
            input = player.GetComponent<PlayerInputReader>();
            interaction = player.GetComponent<InteractionController>();
            view = player.GetComponentInChildren<Camera>().transform;
            input.SetGameplay(true);
        }

        public override void TearDown()
        {
            if (player) Object.DestroyImmediate(player);
            foreach (var target in owned) if (target) Object.DestroyImmediate(target);
            owned.Clear();
            InputFixtureIsolation.DisableLiveActions();
            Time.timeScale = 1;
            try { base.TearDown(); } finally { sceneScope?.Dispose(); sceneScope = null; }
        }

        S016InteractionProbe Target(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
            var target = go.AddComponent<S016InteractionProbe>();
            owned.Add(go);
            Physics.SyncTransforms();
            return target;
        }

        [UnityTest] public IEnumerator SmallAimDriftRetainsVisibleTargetAndCommitsIt()
        {
            yield return null; yield return null;
            var target = Target("Edge target", view.position + view.forward * 1.8f,
                new Vector3(.08f, .5f, .08f));
            Assert.That(interaction.Resolve(), Is.SameAs(target));
            view.rotation *= Quaternion.Euler(0, 3f, 0);
            Assert.That(interaction.Resolve(), Is.SameAs(target), "A small miss at the edge should keep a visible target.");
            Assert.That(interaction.TryInteract(), Is.True);
            Assert.That(target.Accepted, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator WallAndDisabledTargetClearRetentionBeforeCommit()
        {
            yield return null; yield return null;
            var target = Target("Edge target", view.position + view.forward * 1.8f,
                new Vector3(.08f, .5f, .08f));
            Assert.That(interaction.Resolve(), Is.SameAs(target));
            view.rotation *= Quaternion.Euler(0, 3f, 0);
            var wall = new GameObject("Blocker");
            wall.transform.position = view.position + view.forward * 1f;
            wall.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, .1f);
            owned.Add(wall); Physics.SyncTransforms();
            Assert.That(interaction.Resolve(), Is.Null);
            Assert.That(interaction.TryInteract(), Is.False);
            Assert.That(target.Accepted, Is.Zero);
            Object.DestroyImmediate(wall); Physics.SyncTransforms();
            target.enabled = false;
            Assert.That(interaction.Resolve(), Is.Null);
            Assert.That(interaction.TryInteract(), Is.False);
        }

        [UnityTest] public IEnumerator OverlappingCollidersChooseStableDeterministicTarget()
        {
            yield return null; yield return null;
            Vector3 position = view.position + view.forward * 1.8f;
            var a = Target("A", position, new Vector3(.4f, .5f, .1f));
            var b = Target("B", position, new Vector3(.4f, .5f, .1f));
            var expected = EntityId.ToULong(a.GetComponent<Collider>().GetEntityId()) <
                EntityId.ToULong(b.GetComponent<Collider>().GetEntityId()) ? a : b;
            for (int i = 0; i < 8; i++) Assert.That(interaction.Resolve(), Is.SameAs(expected));
            Assert.That(interaction.TryInteract(), Is.True);
            Assert.That(expected.Accepted, Is.EqualTo(1));
            Assert.That(a.Accepted + b.Accepted, Is.EqualTo(1));
        }
    }
}
#endif
