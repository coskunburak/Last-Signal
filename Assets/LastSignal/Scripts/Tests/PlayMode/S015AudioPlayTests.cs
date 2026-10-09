#if UNITY_EDITOR
using System.Collections;
using LastSignal.Audio;
using LastSignal.Noise;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LastSignal.Tests
{
    public sealed class S015AudioPlayTests
    {
        SessionFlow flow;
        ProductionAudio audio;
        [UnitySetUp] public IEnumerator OpenProduction()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow=Object.FindFirstObjectByType<SessionFlow>(); Assert.That(flow,Is.Not.Null); flow.Resume();
            audio=flow.GetComponent<ProductionAudio>(); Assert.That(audio,Is.Not.Null); Assert.That(flow.Player,Is.Not.Null);
            foreach(var zombie in Object.FindObjectsByType<ZombieController>()) zombie.SetPaused(true);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(flow) flow.ReturnToMenu(); Time.timeScale=1;
            if(audio) { audio.Preferences.Load(); audio.ApplyPreferences(); }
            yield return null;
        }
        [UnityTest] public IEnumerator MutingPreservesNoiseSemanticsAndDelivery()
        {
            var go=new GameObject("S015 noise probe"); go.transform.position=flow.Player.transform.position;
            var probe=go.AddComponent<NoiseAcceptanceProbe>(); probe.Bind(flow.Noise,990001);
            try
            {
                var request=new GameplayNoiseRequest(1,go.transform.position,GameplayNoiseCategory.SprintFootstep,flow.NoiseTuning.Sprint);
                audio.Preferences[AudioBus.Master]=audio.Preferences[AudioBus.Effects]=1; audio.ApplyPreferences();
                Assert.That(flow.Noise.TryEmit(request,out _),Is.True); var audible=probe.LastEvent;
                audio.Preferences[AudioBus.Master]=audio.Preferences[AudioBus.Effects]=0; audio.ApplyPreferences();
                Assert.That(flow.Noise.TryEmit(request,out _),Is.True); var muted=probe.LastEvent;
                Assert.That(probe.ReceivedCount,Is.EqualTo(2));
                Assert.That(muted.BaseRadiusMeters,Is.EqualTo(audible.BaseRadiusMeters)); Assert.That(muted.Intensity,Is.EqualTo(audible.Intensity));
                Assert.That(muted.ExpiresAt-muted.SimulationTime,Is.EqualTo(audible.ExpiresAt-audible.SimulationTime));
                Assert.That(muted.Category,Is.EqualTo(audible.Category)); Assert.That(muted.Position,Is.EqualTo(audible.Position));
            }
            finally { Object.Destroy(go); }
            yield return null;
        }
        [UnityTest] public IEnumerator RestartKeepsFixedVoiceBudgetAndClearsMusic()
        {
            int count=audio.GetComponentsInChildren<AudioSource>().Length; Assert.That(count,Is.EqualTo(ProductionAudio.VoiceBudget+8));
            audio.Threat(flow.Player.transform); Assert.That(audio.MusicState,Is.EqualTo(AudioCue.Threat));
            flow.ReturnToMenu(); Assert.That(audio.MusicState,Is.EqualTo(AudioCue.Exploration));
            foreach(var source in audio.GetComponentsInChildren<AudioSource>()) Assert.That(source.isPlaying,Is.False);
            yield return null;
            flow.BeginSession(); flow.Resume(); yield return null;
            Assert.That(audio.GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(count));
            Assert.That(Object.FindObjectsByType<ProductionAudio>().Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator WorldRestoreClearsOldThreat()
        {
            var clock=flow.GetComponent<LastSignal.WorldTime.WorldClock>(); Assert.That(clock,Is.Not.Null);
            audio.Threat(flow.Player.transform); Assert.That(audio.MusicState,Is.EqualTo(AudioCue.Threat));
            clock.Restore(clock.Capture()); yield return null;
            Assert.That(audio.MusicState,Is.EqualTo(AudioCue.Exploration)); Assert.That(audio.Caption,Is.Empty);
        }
        [UnityTest] public IEnumerator CriticalCaptionSurvivesMuteWithoutDistantRadar()
        {
            var camera=flow.Player.GetComponentInChildren<Camera>(); var actor=new GameObject("S015 audible cue fixture");
            try
            {
                audio.Preferences[AudioBus.Master]=audio.Preferences[AudioBus.Voice]=0; audio.ApplyPreferences();
                actor.transform.position=camera.transform.position+camera.transform.forward*2;
                Assert.That(audio.Play(AudioCue.ZombieTelegraph,actor.transform),Is.True);
                Assert.That(audio.Caption,Does.Contain("Saldırı hazırlığı")); Assert.That(audio.Caption,Does.Contain("Ön"));
                audio.BeginSession(); actor.transform.position=camera.transform.position+camera.transform.forward*100;
                Assert.That(audio.Play(AudioCue.ZombieTelegraph,actor.transform),Is.False); Assert.That(audio.Caption,Is.Empty);
            }
            finally { Object.Destroy(actor); }
            yield return null;
        }
        [UnityTest] public IEnumerator RealWeaponEventsPlayOnceAndReloadCancellationStaysAuthoritative()
        {
            var combat=flow.Player.GetComponent<PlayerCombatController>();
            combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);
            var weapon=combat.Firearm; Assert.That(weapon,Is.Not.Null);
            float deadline=Time.realtimeSinceStartup+5;
            while(weapon.RuntimeState.State!=WeaponState.Ready) { Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline)); yield return null; }
            var presenter=weapon.GetComponentInChildren<WeaponAudioPresenter>(); Assert.That(presenter,Is.Not.Null);
            int cues=audio.CueCount, ammo=weapon.RuntimeState.CurrentMagazine;
            weapon.OnFirePressed(); weapon.OnFireReleased();
            Assert.That(weapon.RuntimeState.CurrentMagazine,Is.EqualTo(ammo-1)); Assert.That(audio.LastCue,Is.EqualTo(AudioCue.Fire)); Assert.That(audio.CueCount,Is.EqualTo(cues+1));
            var inventory=flow.Player.GetComponent<LastSignal.Inventory.PlayerInventory>(); inventory.TryAdd(weapon.Definition.Ammunition,60);
            weapon.OnReloadRequested(); Assert.That(weapon.RuntimeState.State,Is.EqualTo(WeaponState.Reloading));
            cues=audio.CueCount; presenter.OnMagazineDetach(); presenter.OnMagazineDetach(); Assert.That(audio.CueCount,Is.EqualTo(cues+1));
            int total=weapon.RuntimeState.TotalAmmo; weapon.RequestUnequip(); cues=audio.CueCount;
            presenter.OnBoltAction(); presenter.OnMagazineAttach(); presenter.OnMagazineDetach();
            Assert.That(audio.CueCount,Is.EqualTo(cues)); Assert.That(weapon.RuntimeState.TotalAmmo,Is.EqualTo(total));
        }
        [UnityTest] public IEnumerator RealZombieStateProducesTelegraphAndDisableReleasesVoices()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            Assert.That(UnityEngine.AI.NavMesh.SamplePosition(flow.Player.transform.position+flow.Player.transform.forward*2,out var hit,5,UnityEngine.AI.NavMesh.AllAreas),Is.True);
            var actor=Object.Instantiate(prefab,hit.position,Quaternion.identity);
            try
            {
                var zombie=actor.GetComponent<ZombieController>(); Assert.That(zombie.Initialize(),Is.True); Assert.That(zombie.Bind(flow.Player),Is.True);
                zombie.Runtime.Transition(ZombieState.Chasing); zombie.Runtime.Transition(ZombieState.AttackWindup);
                Assert.That(audio.LastCue,Is.EqualTo(AudioCue.ZombieTelegraph)); Assert.That(audio.Caption,Does.Contain("Saldırı hazırlığı"));
                actor.SetActive(false); int count=audio.CueCount;
                zombie.Runtime.Transition(ZombieState.Chasing); Assert.That(audio.CueCount,Is.EqualTo(count));
            }
            finally { Object.Destroy(actor); }
            yield return null;
        }
    }
}
#endif
