using System;
using LastSignal.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastSignal.Tests
{
    public sealed class S015AudioTests
    {
        const string CatalogPath="Assets/LastSignal/Audio/Config/S015AudioCatalog.asset";
        [Test] public void VolumeUsesLogarithmicScaleAndSafeMute()
        {
            Assert.That(AudioPreferences.ToDecibels(1),Is.EqualTo(0));
            Assert.That(AudioPreferences.ToDecibels(.5f),Is.EqualTo(-6.0206f).Within(.001f));
            Assert.That(AudioPreferences.ToDecibels(0),Is.EqualTo(-80));
            Assert.That(AudioPreferences.ToDecibels(-1),Is.EqualTo(-80));
            Assert.That(AudioPreferences.ToDecibels(float.NaN),Is.EqualTo(-80));
            Assert.That(AudioPreferences.ToDecibels(float.PositiveInfinity),Is.EqualTo(-80));
        }
        [Test] public void PreferencesRoundtripAllChannelsWithoutTouchingUserKeys()
        {
            string prefix="LastSignal.Tests.Audio."+Guid.NewGuid()+".";
            try
            {
                var saved=new AudioPreferences(prefix);
                for(int i=0;i<5;i++) saved[(AudioBus)i]=i*.2f;
                saved.Captions=false; saved.Save();
                var loaded=new AudioPreferences(prefix); loaded.Load();
                for(int i=0;i<5;i++) Assert.That(loaded[(AudioBus)i],Is.EqualTo(i*.2f));
                Assert.That(loaded.Captions,Is.False);
            }
            finally { foreach(var p in AudioPreferences.Parameters) PlayerPrefs.DeleteKey(prefix+p); PlayerPrefs.DeleteKey(prefix+"Captions"); PlayerPrefs.Save(); }
        }
        [Test] public void CorruptPreferenceIsClamped()
        {
            var settings=new AudioPreferences(); settings[AudioBus.Master]=float.NaN; settings[AudioBus.Music]=2;
            Assert.That(settings[AudioBus.Master],Is.Zero); Assert.That(settings[AudioBus.Music],Is.EqualTo(1));
        }
        [Test] public void VariationsNeverImmediatelyRepeat()
        {
            for(int previous=0;previous<3;previous++) for(int seed=0;seed<100;seed++)
            { int next=AudioCatalog.Variation(3,previous,seed); Assert.That(next,Is.InRange(0,2)); Assert.That(next,Is.Not.EqualTo(previous)); }
            Assert.That(AudioCatalog.Variation(0,0,0),Is.EqualTo(-1)); Assert.That(AudioCatalog.Variation(1,0,0),Is.Zero);
        }
        [Test] public void MusicThreatExpiresThroughReliefToExploration()
        {
            var model=new AudioTension(); model.Signal(10); model.Tick(13.9f); Assert.That(model.State,Is.EqualTo(AudioCue.Threat));
            model.Tick(14); Assert.That(model.State,Is.EqualTo(AudioCue.Relief)); model.Tick(22); Assert.That(model.State,Is.EqualTo(AudioCue.Exploration));
        }
        [Test] public void MusicRefreshIsBoundedAndResetClearsStaleThreat()
        {
            var model=new AudioTension(); model.Signal(0); model.Signal(3); model.Tick(6); Assert.That(model.State,Is.EqualTo(AudioCue.Threat));
            model.Tick(7); Assert.That(model.State,Is.EqualTo(AudioCue.Relief)); model.Signal(8); model.Reset(); model.Tick(9);
            Assert.That(model.State,Is.EqualTo(AudioCue.Exploration));
        }
        [Test] public void CaptionsOnlyExposeCoarseDirection()
        {
            Assert.That(ProductionAudio.Direction(Vector3.forward),Is.EqualTo("Ön"));
            Assert.That(ProductionAudio.Direction(Vector3.back),Is.EqualTo("Arka"));
            Assert.That(ProductionAudio.Direction(Vector3.left*100),Is.EqualTo("Sol"));
            Assert.That(ProductionAudio.Direction(Vector3.right),Is.EqualTo("Sağ"));
        }
        [Test] public void DoorwayBlendIsContinuousAndBounded()
        {
            var size=new Vector3(6,3,6); var center=Vector3.zero;
            Assert.That(ProductionAudio.ShelterBlend(new Vector3(4,0,0),center,size),Is.Zero);
            Assert.That(ProductionAudio.ShelterBlend(new Vector3(3,0,0),center,size),Is.EqualTo(.5f));
            Assert.That(ProductionAudio.ShelterBlend(new Vector3(2,0,0),center,size),Is.EqualTo(1));
            Assert.That(ProductionAudio.ShelterBlend(new Vector3(3.01f,0,0),center,size),Is.EqualTo(.49f).Within(.001f));
        }
        [Test] public void SurfaceMetadataAndFallbackResolveWithoutClipTiming()
        {
            var root=new GameObject("wood"); var child=new GameObject("collider"); child.transform.SetParent(root.transform);
            try { root.AddComponent<AudioSurface>().surface=AudioSurfaceKind.Wood; var col=child.AddComponent<BoxCollider>();
                Assert.That(AudioSurface.Resolve(col),Is.EqualTo(AudioSurfaceKind.Wood)); Assert.That(AudioSurface.Resolve(null),Is.EqualTo(AudioSurfaceKind.Default));
                Assert.That(AudioSurface.Cue(AudioSurfaceKind.Gravel),Is.EqualTo(AudioCue.StepGravel)); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test] public void SoilAreaRespectsFoundationCutoutAndEllipse()
        {
            var go=new GameObject("soil");
            try { var area=go.AddComponent<AudioSurface>(); area.areaRadii=new Vector2(22,19.5f); area.squareCutout=3.25f;
                Assert.That(area.ContainsArea(Vector3.zero),Is.False); Assert.That(area.ContainsArea(new Vector3(10,0,0)),Is.True);
                Assert.That(area.ContainsArea(new Vector3(21,0,19)),Is.False); }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void CatalogCoversRequiredCuesWithProjectOwnedMixer()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath); Assert.That(catalog,Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(catalog.mixer),Does.StartWith("Assets/LastSignal/Audio/"));
            foreach(AudioCue id in Enum.GetValues(typeof(AudioCue)))
            {
                var cue=catalog.Get(id); Assert.That(cue,Is.Not.Null,id.ToString()); Assert.That(cue.clips,Is.Not.Empty,id.ToString());
                foreach(var clip in cue.clips) Assert.That(clip,Is.Not.Null,id.ToString());
                Assert.That(catalog.Group(cue.bus),Is.Not.Null); Assert.That(cue.gain,Is.InRange(0,1));
            }
            Assert.That(catalog.Get((AudioCue)999),Is.Null);
            var so=new SerializedObject(catalog.mixer); var exposed=so.FindProperty("m_ExposedParameters"); Assert.That(exposed.arraySize,Is.EqualTo(5));
            for(int i=0;i<5;i++) Assert.That(exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue,Is.EqualTo(AudioPreferences.Parameters[i]));
        }
        [Test] public void SelectedImportsUseStreamingForLoopsAndMonoForWorldCues()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            foreach(var cue in catalog.cues) foreach(var clip in cue.clips)
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Assert.That(importer.defaultSampleSettings.loadType,Is.EqualTo(cue.loop?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad));
                if(cue.spatial) Assert.That(importer.forceToMono,Is.True);
                if(cue.loop) Assert.That(importer.forceToMono,Is.False);
            }
        }
        [Test] public void ProductionPrefabsCarryAudioPresenters()
        {
            var weapon=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab");
            var presenter=weapon.GetComponent<WeaponAudioPresenter>(); Assert.That(presenter,Is.Not.Null);
            Assert.That(new SerializedObject(presenter).FindProperty("weapon").objectReferenceValue,Is.EqualTo(weapon.GetComponent<WeaponController>()));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab").GetComponent<FootstepAudioPresenter>(),Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab").GetComponent<ZombieAudioPresenter>(),Is.Not.Null);
        }
        [Test] public void CabinHasOneAudioOwnerSettingsAndAuthoredSurfaces()
        {
            const string path="Assets/LastSignal/Scenes/Production/S013Cabin.unity";
            var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                int owners=0,views=0,surfaces=0,rooms=0;
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var owner in root.GetComponentsInChildren<ProductionAudio>(true)) { owners++; Assert.That(owner.Catalog,Is.Not.Null); }
                    foreach(var view in root.GetComponentsInChildren<AudioSettingsView>(true)) { views++; Assert.That(view.sliders.Length,Is.EqualTo(5)); Assert.That(view.captionText,Is.Not.Null); }
                    surfaces+=root.GetComponentsInChildren<AudioSurface>(true).Length; rooms+=root.GetComponentsInChildren<AudioReverbZone>(true).Length;
                }
                Assert.That(owners,Is.EqualTo(1)); Assert.That(views,Is.EqualTo(1)); Assert.That(surfaces,Is.GreaterThanOrEqualTo(4)); Assert.That(rooms,Is.GreaterThanOrEqualTo(1));
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
