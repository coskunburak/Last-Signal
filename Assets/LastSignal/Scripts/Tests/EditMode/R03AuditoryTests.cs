using LastSignal.Noise;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastSignal.Tests
{
    public sealed class R03AuditoryTests
    {
        ZombieDefinition tuning;
        [SetUp] public void Setup() => tuning = ScriptableObject.CreateInstance<ZombieDefinition>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(tuning);
        static GameplayNoiseEvent Event(ulong sequence = 1, float intensity = 1, double time = 0, Vector3 position = default) =>
            new GameplayNoiseEvent(new GameplayNoiseId(1, sequence), new GameplayNoiseRequest(1, position, GameplayNoiseCategory.Gunshot, new GameplayNoiseProfile(96, intensity, 3)), time);
        [Test] public void SnapshotAgeExpiryAndClear()
        {
            var memory = new ZombieAuditoryMemory(); var e = Event(position: new Vector3(2,3,4));
            Assert.IsTrue(memory.Receive(new ZombieAuditoryStimulus(e, Vector3.zero, .6f, true), 10));
            Assert.AreEqual(e.Position, memory.Stimulus.Event.Position); Assert.AreEqual(e.EventId, memory.Stimulus.Event.EventId);
            memory.Advance(5,10); Assert.AreEqual(5,memory.Age); Assert.AreEqual(.3f,memory.Confidence(10),.0001);
            memory.Advance(0,10); Assert.AreEqual(5,memory.Age); memory.Advance(5,10); Assert.IsFalse(memory.HasStimulus);
            memory.Clear(); Assert.AreEqual(0,memory.Age); Assert.AreEqual(default(GameplayNoiseId),memory.Stimulus.Event.EventId);
        }
        [Test] public void DuplicateCannotRefreshEvenAfterConsumption()
        {
            var m=new ZombieAuditoryMemory();var s=new ZombieAuditoryStimulus(Event(),Vector3.zero,1,false);
            Assert.IsTrue(m.Receive(s,10));m.Advance(2,10);Assert.IsFalse(m.Receive(s,10));Assert.AreEqual(2,m.Age);
            m.Consume();Assert.IsFalse(m.Receive(s,10));Assert.IsFalse(m.HasStimulus);
        }
        [Test] public void StrongerWinsWeakDoesNotAndEqualUsesNewerSequence()
        {
            var m=new ZombieAuditoryMemory();Assert.IsTrue(m.Receive(new ZombieAuditoryStimulus(Event(),Vector3.zero,.3f,false),10));
            Assert.IsTrue(m.Receive(new ZombieAuditoryStimulus(Event(2),Vector3.zero,1,false),10));
            Assert.IsFalse(m.Receive(new ZombieAuditoryStimulus(Event(3),Vector3.zero,.2f,false),10));
            Assert.IsTrue(m.Receive(new ZombieAuditoryStimulus(Event(4),Vector3.zero,1,false),10));
            Assert.AreEqual(4,m.Stimulus.Event.EventId.Sequence);
        }
        [Test] public void OldEpochRequiresExplicitLifecycleClear()
        {
            var m=new ZombieAuditoryMemory();m.Receive(new ZombieAuditoryStimulus(Event(),Vector3.zero,1,false),10);
            var e=new GameplayNoiseEvent(new GameplayNoiseId(2,1),new GameplayNoiseRequest(1,Vector3.zero,GameplayNoiseCategory.Gunshot,new GameplayNoiseProfile(96,1,3)),0);
            Assert.IsFalse(m.Receive(new ZombieAuditoryStimulus(e,Vector3.zero,1,false),10));m.Clear();Assert.IsTrue(m.Receive(new ZombieAuditoryStimulus(e,Vector3.zero,1,false),10));
        }
        [TestCase(0,true)][TestCase(2.99,true)][TestCase(3,false)][TestCase(-1,false)][TestCase(double.NaN,false)]
        public void TtlUsesSimulationTime(double now,bool valid) => Assert.AreEqual(valid,ZombieHearingEvaluator.Valid(Event(),now));
        [Test] public void InvalidEventFailsClosed()
        { Assert.IsFalse(ZombieHearingEvaluator.Valid(default,0));Assert.IsFalse(ZombieHearingEvaluator.Valid(Event(intensity:float.NaN),0));Assert.IsFalse(ZombieHearingEvaluator.Valid(Event(position:new Vector3(float.NaN,0,0)),0)); }
        [Test] public void WallAttenuatesWithoutSilencingGunshot()
        {
            var e=Event();float clear=ZombieHearingEvaluator.Strength(e,Vector3.right*20,tuning,false),blocked=ZombieHearingEvaluator.Strength(e,Vector3.right*20,tuning,true);
            Assert.AreEqual(clear*.4f,blocked,.0001);Assert.Greater(blocked,tuning.HearingThreshold);
        }
        [Test] public void WalkSprintAndMeleeHaveMeaningfulRanges()
        {
            var t=new GameplayNoiseTuning();
            var walk=new GameplayNoiseEvent(new GameplayNoiseId(1,1),new GameplayNoiseRequest(1,Vector3.zero,GameplayNoiseCategory.Footstep,t.Walk),0);
            var sprint=new GameplayNoiseEvent(new GameplayNoiseId(1,2),new GameplayNoiseRequest(1,Vector3.zero,GameplayNoiseCategory.SprintFootstep,t.Sprint),0);
            var melee=new GameplayNoiseEvent(new GameplayNoiseId(1,3),new GameplayNoiseRequest(1,Vector3.zero,GameplayNoiseCategory.MeleeImpact,t.Melee),0);
            Assert.Less(ZombieHearingEvaluator.Strength(walk,Vector3.right*4,tuning,false),tuning.HearingThreshold);
            Assert.Greater(ZombieHearingEvaluator.Strength(sprint,Vector3.right*4,tuning,false),tuning.HearingThreshold);
            Assert.Greater(ZombieHearingEvaluator.Strength(melee,Vector3.right*4,tuning,false),tuning.HearingThreshold);
            Assert.Less(ZombieHearingEvaluator.Strength(walk,Vector3.zero,tuning,true),tuning.HearingThreshold);
            Assert.AreEqual(0,ZombieHearingEvaluator.Strength(walk,Vector3.up*7,tuning,false));
        }
        [TestCase(ZombieState.Idle,ZombieState.Investigating,true)]
        [TestCase(ZombieState.Investigating,ZombieState.Searching,true)]
        [TestCase(ZombieState.Investigating,ZombieState.Chasing,true)]
        [TestCase(ZombieState.Investigating,ZombieState.HitReact,true)]
        [TestCase(ZombieState.HitReact,ZombieState.Investigating,true)]
        [TestCase(ZombieState.Investigating,ZombieState.Dead,true)]
        [TestCase(ZombieState.Searching,ZombieState.Investigating,true)]
        [TestCase(ZombieState.Dead,ZombieState.Investigating,false)]
        [TestCase(ZombieState.Investigating,ZombieState.AttackWindup,false)]
        [TestCase(ZombieState.AttackCommit,ZombieState.Investigating,false)]
        public void LegalTransitions(ZombieState from,ZombieState to,bool legal)=>Assert.AreEqual(legal,ZombieRuntimeState.IsLegal(from,to));
        [TestCase("hearingThreshold",float.NaN)][TestCase("hearingMemoryDuration",-1)]
        [TestCase("investigateDuration",0)][TestCase("investigateArrivalDistance",0)]
        [TestCase("occludedTransmission",1.1f)][TestCase("gunshotSensitivity",float.PositiveInfinity)]
        public void InvalidAuthoring(string field,float value)
        { var so=new SerializedObject(tuning);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();Assert.IsFalse(tuning.IsValid(out _)); }
        [Test] public void EveryProductionPrefabHasOneListenerAndSafeWorldMask()
        {
            int count=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/LastSignal/Prefabs/Resources","Assets/LastSignal/Prefabs/Enemies"}))
            foreach(var actor in AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponentsInChildren<ZombieController>(true))
            { count++;Assert.AreEqual(1,actor.GetComponents<ZombieNoiseListener>().Length);Assert.IsTrue(actor.Definition.IsValid(out _));foreach(var c in actor.GetComponentsInChildren<Collider>(true))Assert.AreEqual(0,actor.Definition.OcclusionMask&(1<<c.gameObject.layer)); }
            Assert.Greater(count,0);
        }
        [Test] public void HearingDoesNotOverwriteVisualMemory()
        {
            var visual=new ZombieRuntimeState();visual.Observe(true,Vector3.left,Vector3.forward,.1f,tuning);visual.Observe(false,Vector3.right,Vector3.back,.1f,tuning);
            var memory=new ZombieAuditoryMemory();memory.Receive(new ZombieAuditoryStimulus(Event(position:Vector3.up),Vector3.zero,1,false),10);
            Assert.AreEqual(Vector3.left,visual.LastKnownPosition);Assert.AreEqual(Vector3.forward,visual.LastSeenDirection);Assert.IsFalse(visual.Visible);
        }
    }
}
