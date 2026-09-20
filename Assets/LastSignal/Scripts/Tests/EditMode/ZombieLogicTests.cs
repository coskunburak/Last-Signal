using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieLogicTests
    {
        ZombieDefinition tuning;
        [SetUp] public void SetUp() => tuning = ScriptableObject.CreateInstance<ZombieDefinition>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(tuning);
        [TestCase(0, 0, 8, true)]
        [TestCase(0, 0, -8, false)]
        [TestCase(8, 0, 0, false)]
        [TestCase(0, 0, 16, false)]
        [TestCase(0, 8, 1, false)]
        public void FieldOfViewRespectsRangeFacingAndVerticalAngle(float x, float y, float z, bool expected)
        { Assert.That(ZombiePerception.InFieldOfView(Vector3.forward,new Vector3(x,y,z),15,120,60),Is.EqualTo(expected)); }
        [Test] public void TuningDefaultsAreValid() => Assert.That(tuning.IsValid(out _), Is.True);
        [Test] public void StateMachineInitialStateIsIdle() => Assert.That(new ZombieRuntimeState().State, Is.EqualTo(ZombieState.Idle));
        [Test] public void StateMachineLegalCycleAndReacquisition()
        {
            var state = new ZombieRuntimeState();
            state.Transition(ZombieState.Chasing); state.Transition(ZombieState.Searching);
            state.Transition(ZombieState.Chasing); state.Transition(ZombieState.Searching); state.Transition(ZombieState.Idle);
            Assert.That(state.Transitions, Is.EqualTo(5)); Assert.That(state.HasMemory, Is.False);
        }
        [Test] public void StateMachineRejectsUnlistedEdges()
        {
            foreach (ZombieState from in System.Enum.GetValues(typeof(ZombieState)))
                foreach (ZombieState to in System.Enum.GetValues(typeof(ZombieState)))
                    Assert.That(ZombieRuntimeState.IsLegal(from, to), Is.EqualTo(
                        from != ZombieState.Dead && to == ZombieState.Dead ||
                        from != ZombieState.Dead && from != ZombieState.HitReact && to == ZombieState.HitReact ||
                        from == ZombieState.HitReact && (to == ZombieState.Idle || to == ZombieState.Chasing || to == ZombieState.Searching) ||
                        from == ZombieState.Idle && to == ZombieState.Chasing ||
                        from == ZombieState.Chasing && (to == ZombieState.Searching || to == ZombieState.AttackWindup) ||
                        from == ZombieState.Searching && (to == ZombieState.Chasing || to == ZombieState.Idle) ||
                        from == ZombieState.AttackWindup && (to == ZombieState.AttackCommit || to == ZombieState.Chasing || to == ZombieState.Searching) ||
                        from == ZombieState.AttackCommit && to == ZombieState.Recovering ||
                        from == ZombieState.Recovering && (to == ZombieState.Chasing || to == ZombieState.Searching)));
            Assert.Throws<System.InvalidOperationException>(() => new ZombieRuntimeState().Transition(ZombieState.Searching));
        }
        [Test] public void ConfidenceAccumulatesAndDecays()
        {
            var state = new ZombieRuntimeState();
            state.Observe(true, Vector3.one, Vector3.forward, .1f, tuning);
            Assert.That(state.Confidence, Is.InRange(.2f, .4f));
            state.Observe(true, Vector3.one, Vector3.forward, .3f, tuning); Assert.That(state.Confidence, Is.EqualTo(1));
            state.Observe(false, Vector3.zero, Vector3.zero, .2f, tuning); Assert.That(state.Confidence, Is.InRange(.5f, .7f));
            state.Observe(false, Vector3.zero, Vector3.zero, 1, tuning); Assert.That(state.Confidence, Is.Zero);
        }
        [Test] public void MemoryLastKnownPositionDoesNotFollowHiddenPlayer()
        {
            var state = new ZombieRuntimeState(); var a = new Vector3(2, 0, 3); var b = new Vector3(9, 0, 12);
            state.Observe(true, a, Vector3.forward, .4f, tuning);
            state.Observe(false, b, Vector3.right, 1, tuning);
            Assert.That(state.LastKnownPosition, Is.EqualTo(a)); Assert.That(state.LastSeenDirection, Is.EqualTo(Vector3.forward));
        }
        [Test] public void DestinationRefreshRequiresTimeAndMovementThresholds()
        {
            var policy = new ZombieDestinationPolicy();
            Assert.That(policy.ShouldRefresh(Vector3.zero, 0, .3f, .45f), Is.True);
            Assert.That(policy.ShouldRefresh(Vector3.one, .1f, .3f, .45f), Is.False);
            Assert.That(policy.ShouldRefresh(Vector3.zero, .5f, .3f, .45f), Is.False);
            Assert.That(policy.ShouldRefresh(Vector3.one, .5f, .3f, .45f), Is.True);
        }
    }
}
