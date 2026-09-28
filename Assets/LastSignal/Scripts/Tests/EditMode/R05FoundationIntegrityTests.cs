using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using LastSignal.AI;

namespace LastSignal.Tests
{
    public class R05FoundationIntegrityTests
    {
        [Test]
        public void NoDuplicateServiceAuthority()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/LastSignal/Prefabs", "Assets/LastSignal/Scenes" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj == null) continue;
                
                // GameplayNoiseSystem is a plain C# service owned by SessionFlow,
                // not a component serialized into a prefab.
                var sessions = obj.GetComponentsInChildren<SessionFlow>(true);
                Assert.That(sessions.Length, Is.LessThanOrEqualTo(1), $"Duplicate SessionFlow noise authority in {path}");

                var populations = obj.GetComponentsInChildren<WorldPopulationManager>(true);
                Assert.That(populations.Length, Is.LessThanOrEqualTo(1), $"Duplicate WorldPopulationManager in {path}");
            }
        }

        [Test]
        public void ZombiePrefabValidation()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/LastSignal/Prefabs/AI" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("Zombie")) continue;
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                var controller = obj.GetComponent<ZombieController>();
                if (controller == null) continue; // Not a full zombie actor

                var nav = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
                Assert.That(nav, Is.Not.Null, $"Missing NavMeshAgent in {path}");
                
                var perc = obj.GetComponent<ZombiePerception>();
                Assert.That(perc, Is.Not.Null, $"Missing ZombiePerception in {path}");

                var listeners = obj.GetComponentsInChildren<ZombieNoiseListener>(true);
                Assert.That(listeners.Length, Is.EqualTo(1), $"Exactly one ZombieNoiseListener required in {path}");

                var health = obj.GetComponent<ZombieHealth>();
                Assert.That(health, Is.Not.Null, $"Missing ZombieHealth in {path}");
            }
        }

        [Test]
        public void PlayerPrefabValidation()
        {
            var guid = AssetDatabase.FindAssets("Player t:Prefab", new[] { "Assets/LastSignal/Prefabs/Player" });
            if (guid.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid[0]);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                var stamina = obj.GetComponentsInChildren<PlayerStamina>(true);
                Assert.That(stamina.Length, Is.LessThanOrEqualTo(1), $"Duplicate PlayerStamina in {path}");

                var combat = obj.GetComponentsInChildren<PlayerCombatController>(true);
                Assert.That(combat.Length, Is.LessThanOrEqualTo(1), $"Duplicate PlayerCombatController in {path}");
            }
        }

        [Test]
        public void NoMissingScriptsInProductionPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/LastSignal/Prefabs" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var components = obj.GetComponentsInChildren<Component>(true);
                foreach (var c in components)
                {
                    Assert.That(c, Is.Not.Null, $"Missing script in prefab {path}");
                }
            }
        }
    }
}
