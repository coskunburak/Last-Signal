using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class PlayerHealthTests
    {
        GameObject owner;
        PlayerHealth health;
        [SetUp] public void Setup()
        { owner = new GameObject("OwnedHealthTest"); health = owner.AddComponent<PlayerHealth>(); health.ResetForSession(); }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(owner);
        [Test] public void StartsFullAndReceivesExistingDamageContract()
        {
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            IDamageable receiver = health;
            receiver.TakeDamage(new DamageInfo { Amount = 17 });
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - 17));
            Assert.That(health.DamageTransactions, Is.EqualTo(1));
        }
        [Test] public void OverkillClampsAndDeathOccursOnce()
        {
            int deaths = 0, changes = 0;
            health.Died += () => deaths++; health.HealthChanged += () => changes++;
            health.TakeDamage(new DamageInfo { Amount = float.MaxValue });
            health.TakeDamage(new DamageInfo { Amount = 1 });
            Assert.That(health.CurrentHealth, Is.Zero); Assert.That(health.IsAlive, Is.False);
            Assert.That(deaths, Is.EqualTo(1)); Assert.That(changes, Is.EqualTo(1));
        }
        [TestCase(-1f)] [TestCase(0f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidDamageIsRejected(float amount)
        {
            health.TakeDamage(new DamageInfo { Amount = amount });
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(health.DamageTransactions, Is.Zero);
        }
        [Test] public void SessionResetClearsDeathAndTransactions()
        {
            health.TakeDamage(new DamageInfo { Amount = 1000 }); health.ResetForSession();
            Assert.That(health.IsAlive, Is.True); Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(health.DamageTransactions, Is.Zero);
        }
    }
}
