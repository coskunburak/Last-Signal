using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace LastSignal.Tests
{
    public sealed class R01FoundationTests
    {
        GameObject owner; PlayerStamina stamina; MeleeWeaponDefinition definition;
        [SetUp] public void Setup() { owner=new GameObject("stamina");stamina=owner.AddComponent<PlayerStamina>();definition=ScriptableObject.CreateInstance<MeleeWeaponDefinition>(); }
        [TearDown] public void Teardown() { Object.DestroyImmediate(owner);Object.DestroyImmediate(definition); }
        [Test] public void DrainDelayRecoveryAndHysteresis()
        {
            Assert.AreEqual(100,stamina.CurrentStamina);stamina.Tick(1,false);Assert.AreEqual(100,stamina.CurrentStamina);
            stamina.Tick(1,true);Assert.AreEqual(82,stamina.CurrentStamina);
            stamina.Tick(1,false);Assert.AreEqual(82,stamina.CurrentStamina);
            stamina.Tick(.5f,false);Assert.AreEqual(87.5f,stamina.CurrentStamina);
            stamina.Tick(10,true);Assert.AreEqual(0,stamina.CurrentStamina);Assert.IsFalse(stamina.CanSprint);
            stamina.Tick(PlayerStamina.RegenDelay+1,false);Assert.IsTrue(stamina.Exhausted);
            stamina.Tick(.2f,false);Assert.IsTrue(stamina.CanSprint);
            stamina.Tick(100,false);Assert.AreEqual(100,stamina.CurrentStamina);
        }
        [Test] public void SharedSpendInvalidValuesAndReset()
        {
            stamina.Tick(1,true);Assert.IsTrue(stamina.TrySpend(25));Assert.AreEqual(57,stamina.CurrentStamina);
            Assert.IsFalse(stamina.TrySpend(58));Assert.IsFalse(stamina.TrySpend(float.NaN));Assert.IsFalse(stamina.TrySpend(-1));
            stamina.Tick(float.PositiveInfinity,true);Assert.AreEqual(57,stamina.CurrentStamina);
            stamina.Tick(0,true);Assert.AreEqual(57,stamina.CurrentStamina);
            stamina.Restore(-100);Assert.AreEqual(0,stamina.CurrentStamina);stamina.ResetSession();Assert.AreEqual(100,stamina.CurrentStamina);
        }
        [TestCase(30)] [TestCase(60)] [TestCase(120)] [TestCase(1)]
        public void TimelineNeverSkipsActive(int fps)
        {
            var sim=new MeleeAttackState(definition);var states=new List<MeleeState>();int hits=0;
            sim.Changed+=states.Add;sim.ActiveWindow+=()=>hits++;
            Assert.IsTrue(sim.TryBegin(stamina));Assert.IsFalse(sim.TryBegin(stamina));
            for(int i=0;i<fps*2;i++)sim.Tick(1f/fps);
            CollectionAssert.AreEqual(new[]{MeleeState.Windup,MeleeState.Active,MeleeState.Recovery,MeleeState.Ready},states);
            Assert.AreEqual(1,hits);Assert.AreEqual(75,stamina.CurrentStamina);
        }
        [Test] public void CancellationAndInsufficientStaminaNeverHit()
        {
            var sim=new MeleeAttackState(definition);int hits=0;sim.ActiveWindow+=()=>hits++;
            Assert.IsTrue(sim.TryBegin(stamina));sim.Cancel();sim.Tick(10);Assert.AreEqual(0,hits);
            stamina.Restore(24);ulong id=sim.SwingId;Assert.IsFalse(sim.TryBegin(stamina));Assert.AreEqual(id,sim.SwingId);Assert.AreEqual(24,stamina.CurrentStamina);
        }
    }
}
