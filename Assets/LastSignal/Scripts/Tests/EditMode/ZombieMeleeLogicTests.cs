using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieMeleeLogicTests
    {
        ZombieDefinition definition;
        [SetUp] public void Setup() => definition = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ZombieDefinition>("Assets/LastSignal/Data/Enemies/Zombie/Shambler.asset"));
        [TearDown] public void Cleanup() => Object.DestroyImmediate(definition);
        [Test] public void ProductionTuningAndPrefabContractsAreValid()
        {
            Assert.That(definition.IsAttackValid(out var reason), Is.True, reason);
            Assert.That(AssetDatabase.GetAssetPath(definition.AttackClip), Does.EndWith("Zombie Attack.fbx"));
            Assert.That(AssetDatabase.GetAssetPath(definition.AlternateAttackClip), Does.EndWith("Zombie Attack (1).fbx"));
            Assert.That(definition.AttackClip.events, Is.Empty);
            Assert.That(definition.AttackCommitTime, Is.LessThan(definition.AttackContactTime));
            Assert.That(definition.AttackContactTime, Is.EqualTo(definition.AttackDuration * .42f).Within(.001f));
            Assert.That(definition.AttackCommitTimeFor(true), Is.LessThan(definition.AttackContactTimeFor(true)));
            Assert.That(definition.AttackContactTimeFor(true), Is.EqualTo(definition.AttackClipDurationFor(true) * .32f).Within(.001f));
            Assert.That(definition.AttackDurationFor(true), Is.EqualTo(definition.AttackClipDurationFor(true) * .70f).Within(.001f));
            Assert.That(definition.AttackDurationFor(true), Is.GreaterThan(definition.AttackRecoveryTimeFor(true)));
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab");
            Assert.That(player.GetComponents<PlayerHealth>().Length, Is.EqualTo(1));
            var zombie = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            Assert.That(zombie.transform.Find("MeleeOrigin"), Is.Not.Null);
            foreach (var c in player.GetComponentsInChildren<Component>(true)) Assert.That(c, Is.Not.Null);
        }
        [TestCase("attackDamage", 0f)] [TestCase("attackReach", -1f)]
        [TestCase("attackCommitNormalized", .5f)] [TestCase("attackContactNormalized", 1.1f)]
        [TestCase("attackRecoveryNormalized", .2f)] [TestCase("attackHalfAngle", 180f)]
        [TestCase("attackPlaybackSpeed", float.NaN)] [TestCase("attackAbortRange", 1f)]
        [TestCase("alternateAttackEndNormalized", .4f)]
        public void InvalidAttackTuningRejected(string property, float value)
        {
            var s = new SerializedObject(definition); s.FindProperty(property).floatValue = value; s.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(definition.IsAttackValid(out _), Is.False);
        }
        [Test] public void NoClipFailsSafely()
        {
            var s = new SerializedObject(definition); s.FindProperty("attackClip").objectReferenceValue = null; s.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(definition.IsAttackValid(out _), Is.False);
        }
        [Test] public void CombatCycleAndAbortCannotSkipRecovery()
        {
            var state = new ZombieRuntimeState(); state.Transition(ZombieState.Chasing);
            state.Transition(ZombieState.AttackWindup); state.Transition(ZombieState.Chasing);
            state.Transition(ZombieState.AttackWindup); state.Transition(ZombieState.AttackCommit);
            Assert.Throws<System.InvalidOperationException>(() => state.Transition(ZombieState.Chasing));
            Assert.Throws<System.InvalidOperationException>(() => state.Transition(ZombieState.AttackWindup));
            state.Transition(ZombieState.Recovering); state.Transition(ZombieState.Searching);
            state.Transition(ZombieState.Chasing);
        }
    }
}
