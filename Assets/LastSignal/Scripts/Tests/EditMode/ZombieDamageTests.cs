using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieDamageTests
    {
        GameObject root, child;
        ZombieHealth health;
        ZombieHitRegion region;
        [SetUp] public void Setup()
        {
            root = new GameObject("OwnedHealthFixture"); health = root.AddComponent<ZombieHealth>();
            // EditMode does not invoke runtime Awake.
            typeof(ZombieHealth).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(health, null);
            child = new GameObject("OwnedRegion"); child.transform.SetParent(root.transform);
            var box = child.AddComponent<BoxCollider>(); region = child.AddComponent<ZombieHitRegion>();
            region.Configure(health, DamageRegion.Body, 1, box);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        [Test] public void StartsFullAndIndependent()
        { Assert.That(health.CurrentHealth, Is.EqualTo(100)); Assert.That(health.IsAlive); Assert.That(health.DamageTransactions, Is.Zero); }
        [TestCase(30, 1, 70)] [TestCase(30, 2, 40)] [TestCase(1000, 2, 0)]
        public void RegionMathAndSingleTransaction(float amount, float multiplier, float expected)
        {
            region.Configure(health, multiplier == 2 ? DamageRegion.Head : DamageRegion.Body, multiplier, region.HitCollider);
            region.TakeDamage(new DamageInfo { Amount = amount, ShotId = 42 });
            Assert.That(health.CurrentHealth, Is.EqualTo(expected)); Assert.That(health.DamageTransactions, Is.EqualTo(1));
            Assert.That(health.LastDamage.BaseAmount, Is.EqualTo(amount)); Assert.That(health.LastDamage.Multiplier, Is.EqualTo(multiplier));
            Assert.That(health.LastDamage.ShotId, Is.EqualTo(42)); Assert.That(health.LastDamage.HitCollider, Is.EqualTo(region.HitCollider));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidDamageRejected(float amount)
        { region.TakeDamage(new DamageInfo { Amount = amount }); Assert.That(health.CurrentHealth, Is.EqualTo(100)); Assert.That(health.DamageTransactions, Is.Zero); }
        [Test] public void DeathExactlyOnceIncludingReentrantHit()
        {
            int deaths = 0; health.Died += () => { deaths++; health.TakeDamage(new DamageInfo { Amount = 100 }); };
            for (int i = 0; i < 10; i++) region.TakeDamage(new DamageInfo { Amount = 1000 });
            Assert.That(health.CurrentHealth, Is.Zero); Assert.That(deaths, Is.EqualTo(1)); Assert.That(health.DamageTransactions, Is.EqualTo(1));
        }
        [Test] public void WrongColliderAndMissingOwnerFailSafely()
        {
            health.TakeDamage(new DamageInfo { Amount = 30, HitCollider = region.HitCollider });
            region.Configure(null, DamageRegion.Head, 2, region.HitCollider); region.TakeDamage(new DamageInfo { Amount = 30 });
            Assert.That(health.DamageTransactions, Is.Zero);
        }
        [Test] public void DisabledIntakeAndOverflowMultiplierReject()
        {
            health.SetDamageEnabled(false); region.TakeDamage(new DamageInfo { Amount = 30 });
            health.SetDamageEnabled(true); region.Configure(health, DamageRegion.Head, float.MaxValue, region.HitCollider);
            region.TakeDamage(new DamageInfo { Amount = float.MaxValue }); Assert.That(health.DamageTransactions, Is.Zero);
        }
        [Test] public void DeadIsTerminalEvenAfterResetAndObservation()
        {
            var state = new ZombieRuntimeState(); state.Transition(ZombieState.Dead); state.Reset();
            var tuning = ScriptableObject.CreateInstance<ZombieDefinition>();
            try { state.Observe(true, Vector3.one, Vector3.forward, 1, tuning); state.Advance(10); }
            finally { Object.DestroyImmediate(tuning); }
            foreach (ZombieState next in System.Enum.GetValues(typeof(ZombieState))) Assert.That(ZombieRuntimeState.IsLegal(ZombieState.Dead, next), Is.False);
            Assert.That(state.State, Is.EqualTo(ZombieState.Dead)); Assert.That(state.HasMemory, Is.False);
        }
        [Test] public void SearchKnowledgeAndBudgetSurviveReaction()
        {
            var state = new ZombieRuntimeState(); state.Transition(ZombieState.Chasing); state.Transition(ZombieState.Searching); state.Advance(3);
            state.Transition(ZombieState.HitReact); state.Transition(ZombieState.Searching); Assert.That(state.SearchAge, Is.EqualTo(3));
        }
        [Test] public void ProductionPrefabRegionsMasksAndAnimationReferencesAreValid()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab");
            var owner = prefab.GetComponent<ZombieHealth>(); Assert.That(owner, Is.Not.Null);
            Assert.That(prefab.GetComponents<ZombieHealth>().Length, Is.EqualTo(1)); bool head = false, body = false;
            foreach (var r in prefab.GetComponentsInChildren<ZombieHitRegion>())
            {
                Assert.That(r.Owner, Is.EqualTo(owner)); Assert.That(r.HitCollider, Is.Not.Null); Assert.That(r.HitCollider.isTrigger, Is.False);
                Assert.That(r.gameObject.layer, Is.EqualTo(8)); head |= r.Region == DamageRegion.Head; body |= r.Region == DamageRegion.Body;
            }
            Assert.That(head && body); Assert.That(prefab.GetComponent<ZombieController>().Definition.IsDamagePresentationValid);
            for (int i=0;i<32;i++) Assert.That(Physics.GetIgnoreLayerCollision(8,i), Is.True);
            var weapon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Weapon_AssaultRifle.prefab");
            Assert.That(new SerializedObject(weapon.GetComponent<WeaponController>()).FindProperty("hitMask").intValue & (1<<8), Is.Not.Zero);
        }
    }
}
