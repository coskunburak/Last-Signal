#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastSignal.WorldTime;
using LastSignal.Persistence;
using LastSignal.Shelter;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace LastSignal.Tests
{
    public class WorldTimeIntegrationTests
    {
        WorldClock clock;SessionFlow flow;RestPoint bed;string directory;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"LastSignal-Time-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            Time.timeScale=1;yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldTimeAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;clock=Object.FindAnyObjectByType<WorldClock>();flow=clock.Flow;bed=Object.FindAnyObjectByType<RestPoint>();flow.Resume();yield return null;
            var actor=Object.FindAnyObjectByType<ZombieEncounter>().Actor;
            actor.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(10,.05f,-10));Physics.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(flow)flow.ReturnToMenu();Time.timeScale=1;yield return null;if(Directory.Exists(directory))Directory.Delete(directory,true);}
        void AtBed(){WorldTimeAcceptanceRoute.Place(flow.Player,new Vector3(-14.8f,.05f,10));clock.PollExposure(1);clock.PollExposure(1);}
        [UnityTest] public IEnumerator TwoExpeditionsWeatherSleepSaveLoadProductionRoute()
        {
            var report=new System.Text.StringBuilder();yield return WorldTimeAcceptanceRoute.Run(clock,Path.Combine(directory,"route.json"),s=>report.AppendLine(s));
            File.WriteAllText("Docs/Implementation/P02-GAP/Evidence/20260922-entry/two-expedition-world-time.txt",report.ToString());
        }
        [UnityTest] public IEnumerator ActualLifecyclePauseMenuAndPreparationFreezeWorld()
        {
            double before=clock.Simulation.Seconds;yield return null;Assert.Greater(clock.Simulation.Seconds,before);
            flow.Pause();before=clock.Simulation.Seconds;yield return null;yield return null;Assert.AreEqual(before,clock.Simulation.Seconds);
            flow.Resume();var loop=clock.GetComponent<ShelterLoop>();WorldTimeAcceptanceRoute.Place(flow.Player,loop.StoragePoint.transform.position+Vector3.right);Assert.IsTrue(loop.TryUse(loop.StoragePoint));
            before=clock.Simulation.Seconds;yield return null;Assert.AreEqual(before,clock.Simulation.Seconds);loop.ClosePreparation();
            flow.Player.GetComponent<PlayerInputReader>().NotifyFocusLost();Assert.IsTrue(flow.Paused);before=clock.Simulation.Seconds;yield return null;Assert.AreEqual(before,clock.Simulation.Seconds);
            flow.ReturnToMenu();Assert.IsNull(clock.Simulation);yield return null;flow.BeginSession();Assert.IsNotNull(clock.Simulation);
        }
        [UnityTest] public IEnumerator PresentationFollowsClockAtNightDawnDayAndRain()
        {
            flow.Pause();var view=clock.GetComponent<WorldTimePresentation>();clock.Simulation.AdvanceUntil(86400,0);view.Refresh();float night=view.Sun.intensity;Assert.Greater(night,0);
            clock.Simulation.AdvanceUntil(129600,0);view.Refresh();Assert.Greater(view.Sun.intensity,night);
            if(!clock.Simulation.Raining)clock.Simulation.AdvanceUntil(clock.Simulation.NextWeather,1);
            clock.Simulation.AdvanceUntil(clock.Simulation.Seconds+150,1);WorldTimeAcceptanceRoute.Place(flow.Player,new Vector3(-10,.05f,10));clock.PollExposure(1);clock.PollExposure(1);view.Refresh();
            Assert.IsTrue(view.Rain.isPlaying);Assert.Greater(view.Rain.emission.rateOverTime.constant,0);yield return null;
        }
        [UnityTest] public IEnumerator RoofProtectionDebounceAndLiveWetness()
        {
            AtBed();Assert.IsTrue(clock.QueryRoof());Assert.IsTrue(clock.ProtectedFromRain);
            WorldTimeAcceptanceRoute.Place(flow.Player,new Vector3(-10,.05f,10));clock.PollExposure(.1f);Assert.IsTrue(clock.ProtectedFromRain,"Debounce initial exit");
            AtBed();Assert.IsTrue(clock.ProtectedFromRain,"Brief boundary oscillation should retain shelter");
            WorldTimeAcceptanceRoute.Place(flow.Player,new Vector3(-10,.05f,10));clock.PollExposure(.5f);clock.PollExposure(.5f);Assert.IsFalse(clock.ProtectedFromRain);
            if(!clock.Simulation.Raining)clock.Simulation.AdvanceUntil(clock.Simulation.NextWeather,1);double wet=clock.Simulation.Wetness;yield return new WaitForSeconds(.1f);Assert.Greater(clock.Simulation.Wetness,wet);
            AtBed();wet=clock.Simulation.Wetness;yield return new WaitForSeconds(.1f);Assert.Less(clock.Simulation.Wetness,wet);
        }
        [UnityTest] public IEnumerator SleepRejectionsNeverAdvanceOrHeal()
        {
            flow.Pause();double time=clock.Simulation.Seconds;float hp=flow.Player.GetComponent<PlayerHealth>().CurrentHealth;
            Assert.AreEqual(SleepRejection.InvalidDuration,clock.RequestSleep(bed,-1).Reason);Assert.AreEqual(time,clock.Simulation.Seconds);
            flow.Resume();WorldTimeAcceptanceRoute.Place(flow.Player,new Vector3(-10,.05f,10));time=clock.Simulation.Seconds;
            Assert.AreEqual(SleepRejection.NotAtBed,clock.RequestSleep(bed,3600).Reason);Assert.AreEqual(time,clock.Simulation.Seconds);
            AtBed();flow.Pause();Assert.AreEqual(SleepRejection.UnsafeState,clock.RequestSleep(bed,3600).Reason);Assert.AreEqual(hp,flow.Player.GetComponent<PlayerHealth>().CurrentHealth);yield return null;
        }
        [UnityTest] public IEnumerator ActiveNearbyEnemyBlocksButDeadOrDisabledEnemyDoesNot()
        {
            AtBed();var actor=Object.FindAnyObjectByType<ZombieEncounter>().Actor;actor.transform.position=flow.Player.transform.position+Vector3.right*3;Physics.SyncTransforms();
            double before=clock.Simulation.Seconds;Assert.AreEqual(SleepRejection.ThreatNearby,clock.RequestSleep(bed,3600).Reason);Assert.AreEqual(before,clock.Simulation.Seconds);
            actor.gameObject.SetActive(false);Assert.AreEqual(SleepRejection.None,clock.Eligibility(bed,3600));actor.gameObject.SetActive(true);Assert.IsTrue(actor.Initialize());Assert.IsTrue(actor.Bind(flow.Player));
            actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo{Amount=1000});Assert.IsFalse(actor.GetComponent<ZombieHealth>().IsAlive);Assert.AreEqual(SleepRejection.None,clock.Eligibility(bed,3600));yield return null;
        }
        [UnityTest] public IEnumerator DeathDuringSleepStopsAndDoesNotResurrect()
        {
            AtBed();Assert.IsTrue(clock.RequestSleep(bed,28800).Accepted);flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=1000});
            double before=clock.Simulation.Seconds;for(int i=0;i<5&&clock.Sleeping;i++)yield return null;
            Assert.IsFalse(clock.Sleeping);Assert.AreEqual(AdvanceReason.Dead,clock.LastSleep.Reason);Assert.AreEqual(before,clock.Simulation.Seconds);Assert.IsTrue(flow.PlayerDead);
        }
        [UnityTest] public IEnumerator ThreatAppearingDuringSleepInterruptsAndResultRoundtrips()
        {
            AtBed();Assert.IsTrue(clock.RequestSleep(bed,28800).Accepted);yield return null;
            var actor=Object.FindAnyObjectByType<ZombieEncounter>().Actor;actor.transform.position=flow.Player.transform.position+Vector3.right*4;Physics.SyncTransforms();
            for(int i=0;i<5&&clock.Sleeping;i++)yield return null;Assert.IsFalse(clock.Sleeping);Assert.AreEqual(AdvanceReason.ThreatNearby,clock.LastSleep.Reason);Assert.Less(clock.LastSleep.ElapsedSeconds,28800);
            var save=clock.GetComponent<SaveSession>();flow.Pause();string path=Path.Combine(directory,"interrupted.json");Assert.IsTrue(save.Save(path).Success);double time=clock.Simulation.Seconds;
            flow.ReturnToMenu();yield return null;yield return save.Load(path);flow.Pause();Assert.IsTrue(save.LastResult.Success,save.LastResult.Message);Assert.AreEqual(time,clock.Simulation.Seconds);Assert.AreEqual(AdvanceReason.ThreatNearby,clock.Simulation.LastSleepReason);
        }
        [UnityTest] public IEnumerator SaveDuringSleepIsBusyAndMenuCancelsWithoutLeak()
        {
            AtBed();Assert.IsTrue(bed.TryInteract());Assert.AreEqual(SaveError.Busy,clock.GetComponent<SaveSession>().Capture(out _).Error);
            flow.ReturnToMenu();yield return null;Assert.IsFalse(clock.Sleeping);Assert.IsNull(clock.Simulation);flow.BeginSession();Assert.IsFalse(clock.Sleeping);Assert.AreEqual(0,clock.Simulation.Wetness);
        }
        [UnityTest] public IEnumerator OutdoorRainCheckpointRestoresExposureBeforeFirstTick()
        {
            var loop=clock.GetComponent<ShelterLoop>();WorldTimeAcceptanceRoute.Place(flow.Player,loop.InsideAnchor.position);Assert.IsTrue(loop.TryUse(loop.ExitPoint));
            clock.PollExposure(1);clock.PollExposure(1);Assert.IsFalse(clock.ProtectedFromRain);flow.Pause();
            if(!clock.Simulation.Raining)clock.Simulation.AdvanceUntil(clock.Simulation.NextWeather,1);
            clock.Simulation.AdvanceUntil(clock.Simulation.Seconds+300,1);double wet=clock.Simulation.Wetness;Assert.Greater(wet,0);
            Vector3 pose=flow.Player.transform.position;var saves=clock.GetComponent<SaveSession>();string path=Path.Combine(directory,"outside.json");Assert.IsTrue(saves.Save(path).Success);
            flow.ReturnToMenu();yield return null;yield return saves.Load(path);flow.Pause();
            Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message);Assert.IsFalse(flow.Restoring);Assert.Less(Vector3.Distance(pose,flow.Player.transform.position),.01f);
            Assert.IsFalse(clock.ProtectedFromRain,"Completed load must use restored outdoor pose, not sheltered spawn pose");Assert.AreEqual(wet,clock.Simulation.Wetness,1e-6);Assert.IsTrue(clock.Simulation.Raining);
            clock.Simulation.AdvanceUntil(clock.Simulation.Seconds+60,clock.Exposure);Assert.Greater(clock.Simulation.Wetness,wet);
        }
        [UnityTest] public IEnumerator IndoorRainCheckpointRestoresProtectionAndDrying()
        {
            AtBed();flow.Pause();if(!clock.Simulation.Raining)clock.Simulation.AdvanceUntil(clock.Simulation.NextWeather,1);
            clock.Simulation.AdvanceUntil(clock.Simulation.Seconds+300,1);double wet=clock.Simulation.Wetness;Vector3 pose=flow.Player.transform.position;
            var saves=clock.GetComponent<SaveSession>();string path=Path.Combine(directory,"inside.json");Assert.IsTrue(saves.Save(path).Success);
            WorldTimeAcceptanceRoute.Place(flow.Player,new Vector3(-10,.05f,10));clock.PollExposure(1);clock.PollExposure(1);Assert.IsFalse(clock.ProtectedFromRain);
            flow.ReturnToMenu();yield return null;yield return saves.Load(path);flow.Pause();
            Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message);Assert.IsFalse(flow.Restoring);Assert.Less(Vector3.Distance(pose,flow.Player.transform.position),.01f);
            Assert.IsTrue(clock.ProtectedFromRain);Assert.AreEqual(wet,clock.Simulation.Wetness,1e-6);Assert.IsTrue(clock.Simulation.Raining);
            clock.Simulation.AdvanceUntil(clock.Simulation.Seconds+60,clock.Exposure);Assert.Less(clock.Simulation.Wetness,wet);
        }
        [UnityTest] public IEnumerator LegacySceneCheckpointUsesDeterministicWorldDefaults()
        {
            flow.ReturnToMenu();yield return null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/PersistenceAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var legacy=Object.FindAnyObjectByType<SaveSession>();var legacyFlow=legacy.GetComponent<SessionFlow>();legacyFlow.Pause();
            string path=Path.Combine(directory,"legacy.json");Assert.IsTrue(legacy.Save(path).Success);legacyFlow.ReturnToMenu();yield return null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/WorldTimeAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            clock=Object.FindAnyObjectByType<WorldClock>();flow=clock.Flow;flow.ReturnToMenu();yield return null;yield return clock.GetComponent<SaveSession>().Load(path);flow.Pause();
            Assert.IsTrue(clock.GetComponent<SaveSession>().LastResult.Success);Assert.AreEqual(28800,clock.Simulation.Seconds);Assert.IsFalse(clock.Simulation.Raining);Assert.AreEqual(0,clock.Simulation.Wetness);
        }
        [UnityTest] public IEnumerator ExposureAndPresentationCostsMeasured()
        {
            AtBed();for(int i=0;i<100;i++)clock.QueryRoof();var watch=new System.Diagnostics.Stopwatch();long begin=GC.GetAllocatedBytesForCurrentThread();watch.Start();
            for(int i=0;i<1000;i++)clock.QueryRoof();watch.Stop();long bytes=GC.GetAllocatedBytesForCurrentThread()-begin;double exposure=watch.Elapsed.TotalMilliseconds;
            var view=clock.GetComponent<WorldTimePresentation>();watch.Restart();for(int i=0;i<1000;i++)view.Refresh();watch.Stop();
            File.WriteAllText("Docs/Implementation/P02-GAP/Evidence/20260922-entry/runtime-performance.txt",$"1000 actual roof queries: {exposure:F4}ms / {bytes}B\n1000 environment+HUD refresh: {watch.Elapsed.TotalMilliseconds:F4}ms\nHUD normal refresh capped at 4Hz; lighting/particle parameter presentation once per rendered frame.\n");yield return null;
        }
    }
}
#endif
