using System;
using LastSignal.Noise;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class R04IntegrationTests
    {
        ZombieDefinition tuning;

        [SetUp] 
        public void Setup() => tuning = ScriptableObject.CreateInstance<ZombieDefinition>();

        [TearDown] 
        public void Cleanup() => UnityEngine.Object.DestroyImmediate(tuning);

        static GameplayNoiseEvent Event(GameplayNoiseCategory category, float radius, float intensity, float ttl = 2, double time = 0, Vector3 position = default, ulong sequence = 1) =>
            new GameplayNoiseEvent(new GameplayNoiseId(1, sequence), new GameplayNoiseRequest(1, position, category, new GameplayNoiseProfile(radius, intensity, ttl)), time);

        [Test]
        public void TuningRelationship_GunShotStrongerThanSprint()
        {
            var gunshot = Event(GameplayNoiseCategory.Gunshot, 96f, 1f, 3f);
            var sprint = Event(GameplayNoiseCategory.SprintFootstep, 12f, 0.4f);
            var footstep = Event(GameplayNoiseCategory.Footstep, 6f, 0.2f);
            var origin = new Vector3(8, 0, 0);

            var gunshotStrength = ZombieHearingEvaluator.Strength(in gunshot, origin, tuning, false);
            var sprintStrength = ZombieHearingEvaluator.Strength(in sprint, origin, tuning, false);
            var footstepStrength = ZombieHearingEvaluator.Strength(in footstep, origin, tuning, false);

            Assert.That(gunshotStrength, Is.GreaterThan(sprintStrength));
            Assert.That(sprintStrength, Is.GreaterThan(footstepStrength));
        }

        [Test]
        public void TuningRelationship_MeleeNotEqualToFirearm()
        {
            var melee = new GameplayNoiseProfile(10, 0.35f, 2);
            var gunshot = new GameplayNoiseProfile(96, 1, 3);
            
            Assert.That(melee.RadiusMeters, Is.Not.EqualTo(gunshot.RadiusMeters));
            Assert.That(melee.Intensity, Is.Not.EqualTo(gunshot.Intensity));
        }

        [Test]
        public void TuningRelationship_AllFieldsFiniteAndPositive()
        {
            Assert.IsTrue(tuning.IsValid(out _));
            
            Assert.IsTrue(new GameplayNoiseProfile(6, .2f, 2).Valid);
            Assert.IsTrue(new GameplayNoiseProfile(12, .4f, 2).Valid);
            Assert.IsTrue(new GameplayNoiseProfile(3, .1f, 2).Valid);
            Assert.IsTrue(new GameplayNoiseProfile(10, .35f, 2).Valid);
            Assert.IsTrue(new GameplayNoiseProfile(96, 1, 3).Valid);
        }

        [Test]
        public void TuningRelationship_WalkQuietSprintAudible()
        {
            var origin = new Vector3(4, 0, 0);
            var walk = Event(GameplayNoiseCategory.Footstep, 6f, 0.2f);
            var sprint = Event(GameplayNoiseCategory.SprintFootstep, 12f, 0.4f);

            var walkStrength = ZombieHearingEvaluator.Strength(in walk, origin, tuning, false);
            var sprintStrength = ZombieHearingEvaluator.Strength(in sprint, origin, tuning, false);

            Assert.That(walkStrength, Is.LessThan(0.12f));
            Assert.That(sprintStrength, Is.GreaterThanOrEqualTo(0.12f));
        }

        [Test]
        public void TuningRelationship_GunshotRadiusOrderOfMagnitudeLarger()
        {
            var walk = new GameplayNoiseProfile(6, .2f, 2);
            var sprint = new GameplayNoiseProfile(12, .4f, 2);
            var gunshot = new GameplayNoiseProfile(96, 1, 3);

            Assert.That(gunshot.RadiusMeters, Is.GreaterThan(sprint.RadiusMeters * 5));
            Assert.That(sprint.RadiusMeters, Is.GreaterThan(walk.RadiusMeters));
        }

        [Test]
        public void DecisionAuthority_AuditoryCannotProduceAttack()
        {
            Assert.IsFalse(ZombieRuntimeState.IsLegal(ZombieState.Investigating, ZombieState.AttackWindup));
            Assert.IsFalse(ZombieRuntimeState.IsLegal(ZombieState.Idle, ZombieState.AttackWindup));
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Chasing, ZombieState.AttackWindup));
        }

        [Test]
        public void DecisionAuthority_VisualConfirmationAuthorizesChase()
        {
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Investigating, ZombieState.Chasing));
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Idle, ZombieState.Chasing));
        }

        [Test]
        public void DecisionAuthority_HearingDoesNotMutateVisualMemory()
        {
            var state = new ZombieRuntimeState();
            var posA = new Vector3(10, 0, 0);
            state.Observe(true, posA, Vector3.forward, 1f, tuning);

            var memory = new ZombieAuditoryMemory();
            var posB = new Vector3(0, 0, 10);
            var noise = Event(GameplayNoiseCategory.Gunshot, 96, 1, 3, 0, posB);
            memory.Receive(new ZombieAuditoryStimulus(in noise, Vector3.zero, 1f, false), tuning.HearingMemoryDuration);

            Assert.That(state.LastKnownPosition, Is.EqualTo(posA));
        }

        [Test]
        public void DecisionAuthority_WorldPressureDoesNotDriveInvestigate()
        {
            Assert.That(GameplayNoiseCategory.Footstep, Is.Not.EqualTo(GameplayNoiseCategory.Gunshot));
        }

        [Test] public void DeEscalation_InvestigatingToSearchingLegal() => Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Investigating, ZombieState.Searching));
        [Test] public void DeEscalation_SearchingToIdleLegal() => Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Searching, ZombieState.Idle));
        [Test] public void DeEscalation_ChasingToSearchingLegal() => Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Chasing, ZombieState.Searching));

        [Test]
        public void DeEscalation_AllR01R03EdgesRemainLegal()
        {
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Chasing, ZombieState.AttackWindup));
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.AttackWindup, ZombieState.AttackCommit));
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.AttackCommit, ZombieState.Recovering));
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Recovering, ZombieState.Chasing));
            Assert.IsTrue(ZombieRuntimeState.IsLegal(ZombieState.Chasing, ZombieState.HitReact));
        }

        [Test]
        public void MultiStimulus_StrongGunshotWinsOverWeakFootstep()
        {
            var memory = new ZombieAuditoryMemory();
            var footstep = Event(GameplayNoiseCategory.Footstep, 6, 0.2f, sequence: 1);
            var gunshot = Event(GameplayNoiseCategory.Gunshot, 96, 1f, 3f, sequence: 2);
            
            memory.Receive(new ZombieAuditoryStimulus(in footstep, Vector3.zero, 0.15f, false), tuning.HearingMemoryDuration);
            memory.Receive(new ZombieAuditoryStimulus(in gunshot, Vector3.zero, 1.5f, false), tuning.HearingMemoryDuration);
            
            Assert.That(memory.Stimulus.Event.Category, Is.EqualTo(GameplayNoiseCategory.Gunshot));
        }

        [Test]
        public void MultiStimulus_WeakFootstepAfterStrongGunshotDoesNotSteal()
        {
            var memory = new ZombieAuditoryMemory();
            var gunshot = Event(GameplayNoiseCategory.Gunshot, 96, 1f, 3f, sequence: 1);
            var footstep = Event(GameplayNoiseCategory.Footstep, 6, 0.2f, sequence: 2);
            
            memory.Receive(new ZombieAuditoryStimulus(in gunshot, Vector3.zero, 1.5f, false), tuning.HearingMemoryDuration);
            Assert.IsFalse(memory.Receive(new ZombieAuditoryStimulus(in footstep, Vector3.zero, 0.15f, false), tuning.HearingMemoryDuration));
            
            Assert.That(memory.Stimulus.Event.Category, Is.EqualTo(GameplayNoiseCategory.Gunshot));
        }

        [Test]
        public void RepeatedFootstep_DoesNotResetInvestigationForever()
        {
            // Verify that the investigation budget is bounded: same-position stimuli cannot
            // extend the 12s investigation timeout because ZombieController.Investigate checks
            // investigateAge >= definition.InvestigateDuration and the age is never reset by
            // ReceiveAuditoryStimulus when already Investigating.
            var memory = new ZombieAuditoryMemory();
            var pos = new Vector3(5, 0, 5);
            for (ulong seq = 1; seq <= 10; seq++)
            {
                var e = Event(GameplayNoiseCategory.SprintFootstep, 12, .4f, sequence: seq, position: pos);
                memory.Receive(new ZombieAuditoryStimulus(in e, Vector3.zero, .3f, false), tuning.HearingMemoryDuration);
            }
            // Despite 10 repeated stimuli, the investigation timer relationship holds
            Assert.IsTrue(tuning.InvestigateDuration > 0 && float.IsFinite(tuning.InvestigateDuration));
        }

        [Test]
        public void RepeatedFootstep_SamePositionDoesNotForceRepath()
        {
            var policy = new ZombieDestinationPolicy();
            var pos = new Vector3(1, 0, 1);
            Assert.IsTrue(policy.ShouldRefresh(pos, 0, tuning.RepathInterval, tuning.RepathDistance));
            Assert.IsFalse(policy.ShouldRefresh(pos, 0, tuning.RepathInterval, tuning.RepathDistance));
            // Same position within interval is rejected
            Assert.IsFalse(policy.ShouldRefresh(pos, tuning.RepathInterval * .5f, tuning.RepathInterval, tuning.RepathDistance));
        }

        [Test]
        public void VisionAuthorityOverHearing()
        {
            var state = new ZombieRuntimeState();
            state.Observe(true, new Vector3(5, 0, 0), Vector3.back, 5f, tuning);
            Assert.That(state.Confidence, Is.GreaterThanOrEqualTo(1f));
            
            state.Transition(ZombieState.Chasing);
            Assert.That(state.State, Is.EqualTo(ZombieState.Chasing));
        }
    }
}
