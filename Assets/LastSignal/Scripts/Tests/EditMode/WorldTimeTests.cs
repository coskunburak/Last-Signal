using System;
using System.Collections.Generic;
using LastSignal.WorldTime;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEngine;
namespace LastSignal.Tests
{
    public class WorldTimeTests
    {
        static WorldSimulation Create() => new WorldSimulation(new WorldTimeSettings());
        [TestCase(30)] [TestCase(60)] [TestCase(120)]
        public void FrameRateIndependentClockWeatherWetnessAndRecovery(int fps)
        {
            var step = Create(); var bulk = Create(); double healedStep = 0, healedBulk = 0;
            for (int i=0;i<60*fps;i++) step.AdvanceUntil(step.Seconds+60.0/fps,1,0,v=>healedStep+=v);
            bulk.AdvanceUntil(bulk.Seconds+3600,1,0,v=>healedBulk+=v);
            Assert.AreEqual(bulk.Seconds,step.Seconds,.00001); Assert.AreEqual(bulk.Wetness,step.Wetness,.000001);
            Assert.AreEqual(bulk.Raining,step.Raining); Assert.AreEqual(bulk.NextWeather,step.NextWeather,.000001);
            Assert.AreEqual(bulk.RainPresentation,step.RainPresentation,.000001); Assert.AreEqual(healedBulk,healedStep,.00001);
        }
        [Test] public void RealDeltaConversionPauseAndIrregularUpdates()
        {
            var s=Create();double start=s.Seconds;
            s.TickRealSeconds(.1,false,0);s.TickRealSeconds(.7,false,0);s.TickRealSeconds(.2,false,0);
            Assert.AreEqual(start+60,s.Seconds,.000001);s.TickRealSeconds(999,true,0);Assert.AreEqual(start+60,s.Seconds,.000001);
        }
        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidDeltaIsRejectedWithoutMutation(double value)
        {var s=Create();double start=s.Seconds;Assert.Throws<ArgumentOutOfRangeException>(()=>s.TickRealSeconds(value,false,0));Assert.AreEqual(start,s.Seconds);}
        [Test] public void MidnightMultipleDaysAndLargeTimestampPreserveMinutes()
        {
            var s=Create();s.AdvanceUntil(86400*5+60,0);Assert.AreEqual(6,s.Day);Assert.AreEqual(60,s.TimeOfDay);
            var huge=new WorldSimulation(new WorldTimeSettings{startingSeconds=900000000000});huge.AdvanceUntil(huge.Seconds+60,0);Assert.AreEqual(900000000060,huge.Seconds);
            Assert.IsTrue(huge.Capture().Valid);
        }
        [Test] public void DedicatedWeatherSeedSurvivesRestoreAndGlobalRandomChanges()
        {
            var a=Create();a.AdvanceUntil(a.NextWeather+60,1);var b=new WorldSimulation(a.Capture());
            UnityEngine.Random.InitState(791);for(int i=0;i<20;i++){a.AdvanceUntil(a.NextWeather,1);_=UnityEngine.Random.value;b.AdvanceUntil(b.NextWeather,1);Assert.AreEqual(a.NextWeather,b.NextWeather);Assert.AreEqual(a.Raining,b.Raining);}
        }
        [Test] public void WetnessBoundsProtectionAndIndoorDrying()
        {
            var a=Create();a.AdvanceUntil(a.NextWeather,1);a.AdvanceUntil(a.Seconds+1800,1);Assert.AreEqual(1,a.Wetness,.00001);
            var b=new WorldSimulation(a.Capture());a.AdvanceUntil(a.Seconds+3600,0);b.AdvanceUntil(b.Seconds+3600,1,1);
            Assert.AreEqual(0,a.Wetness,.00001);Assert.AreEqual(a.Wetness,b.Wetness,.00001);Assert.AreEqual(1,a.RecoveryMultiplier);
        }
        [Test] public void RainAt45MinutesDuringThreeHourIndoorRestStaysDry()
        {
            var s=new WorldSimulation(new WorldTimeSettings{minimumWeatherSeconds=2700,maximumWeatherSeconds=2700});s.AdvanceUntil(s.Seconds+10800,0);
            Assert.AreEqual(0,s.Wetness);Assert.AreEqual(28800+10800,s.Seconds);Assert.IsTrue(s.Capture().Valid);
        }
        sealed class Boundary : IWorldTimeParticipant
        {
            public double at, processed, powered; public bool fired; public AdvanceReason reason; public WorldSimulation owner; public bool testReentry;
            public double NextBoundary(double now)=>fired?double.PositiveInfinity:at;
            public void ApplyElapsed(double from,double to){processed+=to-from;if(!fired)powered+=to-from;if(to==at)fired=true;if(testReentry)Assert.Throws<InvalidOperationException>(()=>owner.AdvanceUntil(to,0));}
            public AdvanceReason Inspect(double now)=>fired?reason:AdvanceReason.Completed;
        }
        [TestCase(AdvanceReason.Dead)] [TestCase(AdvanceReason.ThreatNearby)] [TestCase(AdvanceReason.Cancelled)]
        public void StopsAtExactBoundaryAndDoesNotApplyRemainingHours(AdvanceReason reason)
        {
            var s=Create();double start=s.Seconds;var b=new Boundary{at=start+9420,reason=reason};s.Register(b);
            var result=s.AdvanceUntil(start+28800,0);Assert.AreEqual(reason,result.Reason);Assert.AreEqual(9420,result.ElapsedSeconds);Assert.AreEqual(9420,b.processed);
            s.RecordSleep(result);var restored=new WorldSimulation(s.Capture());Assert.AreEqual(reason,restored.LastSleepReason);Assert.AreEqual(9420,restored.LastSleepElapsed);
        }
        [Test] public void FuelFixtureDepletesThenOtherSystemsContinueWithoutPoweredTime()
        {
            var s=Create();var fuel=new Boundary{at=s.Seconds+600,reason=AdvanceReason.Completed};s.Register(fuel);s.AdvanceUntil(s.Seconds+3600,0);
            Assert.IsTrue(fuel.fired);Assert.AreEqual(3600,fuel.processed);Assert.AreEqual(600,fuel.powered); // Synthetic event only, not a generator feature.
        }
        [Test] public void AdvancementRejectsReentryAndDuplicateRegistration()
        {var s=Create();var b=new Boundary{at=s.Seconds+10,owner=s,testReentry=true};s.Register(b);Assert.Throws<ArgumentException>(()=>s.Register(b));s.AdvanceUntil(s.Seconds+20,0);}
        [Test] public void NonProgressingBoundaryFailsExplicitly()
        {var s=Create();s.Register(new Boundary{at=s.Seconds});Assert.Throws<InvalidOperationException>(()=>s.AdvanceUntil(s.Seconds+10,0));Assert.IsFalse(s.Advancing);}
        [Test] public void ElapsedSnapshotLoadAndRepeatedActivationNeverDoubleTick()
        {
            var e=new ElapsedWorldState{lastProcessed=28800};Assert.AreEqual(1800,e.TakeElapsed(30600));
            var copy=JsonUtility.FromJson<ElapsedWorldState>(JsonUtility.ToJson(e));Assert.AreEqual(0,copy.TakeElapsed(30600));Assert.AreEqual(0,copy.TakeElapsed(30600));Assert.AreEqual(60,copy.TakeElapsed(30660));
            Assert.Throws<ArgumentOutOfRangeException>(()=>copy.TakeElapsed(1));
        }
        [Test] public void SnapshotIsDetachedAndInvalidValuesRejected()
        {
            var s=Create();var snapshot=s.Capture();snapshot.settings.worldSecondsPerRealSecond=-1;Assert.IsFalse(snapshot.Valid);Assert.AreEqual(60,s.Scale);
            Assert.Throws<ArgumentException>(()=>new WorldSimulation(snapshot));snapshot=s.Capture();snapshot.wetness=double.NaN;Assert.IsFalse(snapshot.Valid);
            snapshot=s.Capture();snapshot.nextWeather=snapshot.seconds;Assert.IsFalse(snapshot.Valid);snapshot=s.Capture();snapshot.randomState=0;Assert.IsFalse(snapshot.Valid);
            snapshot=s.Capture();snapshot.lastProcessed--;Assert.IsFalse(snapshot.Valid);
        }
        [Test] public void InvalidAuthoringRejected()
        {Assert.Throws<ArgumentException>(()=>new WorldSimulation(new WorldTimeSettings{minimumWeatherSeconds=0}));Assert.Throws<ArgumentException>(()=>new WorldSimulation(new WorldTimeSettings{worldSecondsPerRealSecond=-1}));}
        static SaveCodec Codec()=>new SaveCodec(new SaveValidation("world.fixture","content.1",new Dictionary<string,int>{{"ammo.rifle",60},{"medical.bandage",10}},"rifle.1",30));
        [Test] public void SchemaTwoRoundtripPreservesWeatherTimeAndRejectsMissingSection()
        {
            var state=SaveFoundationTests.Fixture();state.header.schemaVersion=2;var s=Create();s.AdvanceUntil(s.NextWeather+800,1);state.worldTime=s.Capture();
            var codec=Codec();Assert.IsTrue(codec.Encode(state,out var json).Success);Assert.IsTrue(codec.Decode(json,out var copy).Success);
            Assert.AreEqual(s.Seconds,copy.worldTime.seconds);Assert.AreEqual(s.Wetness,copy.worldTime.wetness);Assert.AreEqual(s.NextWeather,copy.worldTime.nextWeather);
            var restored=new WorldSimulation(copy.worldTime);s.AdvanceUntil(s.NextWeather,0);restored.AdvanceUntil(restored.NextWeather,0);Assert.AreEqual(s.NextWeather,restored.NextWeather);
            state.worldTime=null;Assert.AreEqual(SaveError.InvalidData,codec.Encode(state,out _).Error);
        }
        [TestCase(double.NaN)] [TestCase(-1)] [TestCase(2)]
        public void SchemaTwoInvalidWetnessIsRejectedRatherThanDefaulted(double wetness)
        {
            var state=SaveFoundationTests.Fixture();state.header.schemaVersion=2;state.worldTime=Create().Capture();state.worldTime.wetness=wetness;
            Assert.AreEqual(SaveError.InvalidData,Codec().Encode(state,out _).Error);
        }
        [Test] public void FatalNinetyMinuteBoundaryDoesNotAdvanceEightHours()
        {
            var s=Create();double start=s.Seconds;var fatal=new Boundary{at=start+5400,reason=AdvanceReason.Dead};s.Register(fatal);
            var result=s.AdvanceUntil(start+28800,0);Assert.IsTrue(fatal.fired);Assert.AreEqual(AdvanceReason.Dead,result.Reason);Assert.AreEqual(start+5400,s.Seconds);Assert.AreEqual(5400,fatal.processed);
        }
        [Test] public void LegacyDefaultsAreDeterministicAndV1FixtureIsPreserved()
        {
            var codec=Codec();Assert.IsTrue(codec.Encode(SaveFoundationTests.Fixture(),out var json).Success);Assert.IsTrue(codec.Decode(json,out var old).Success);
            Assert.AreEqual(1,old.header.schemaVersion);Assert.IsNull(old.worldTime);
            Assert.AreEqual(JsonUtility.ToJson(WorldTimeSnapshot.LegacyDefault()),JsonUtility.ToJson(WorldTimeSnapshot.LegacyDefault()));
        }
        [Test] public void PerformanceClockAndBulkMeasured()
        {
            var s=Create();for(int i=0;i<100;i++)s.TickRealSeconds(1.0/60,false,1);
            var watch=new System.Diagnostics.Stopwatch();long before=GC.GetAllocatedBytesForCurrentThread();watch.Start();
            for(int i=0;i<10000;i++)s.TickRealSeconds(1.0/60,false,1);
            watch.Stop();long bytes=GC.GetAllocatedBytesForCurrentThread()-before;double normal=watch.Elapsed.TotalMilliseconds;
            watch.Restart();s.AdvanceUntil(s.Seconds+86400*100,0);watch.Stop();
            System.IO.Directory.CreateDirectory("Docs/Implementation/P02-GAP/Evidence/20260922-entry");
            System.IO.File.WriteAllText("Docs/Implementation/P02-GAP/Evidence/20260922-entry/domain-performance.txt",$"10000 normal ticks: {normal:F4}ms; allocations={bytes}B\n100-day bulk: {watch.Elapsed.TotalMilliseconds:F4}ms\n");
            Assert.IsTrue(s.Capture().Valid);
        }
    }
}
