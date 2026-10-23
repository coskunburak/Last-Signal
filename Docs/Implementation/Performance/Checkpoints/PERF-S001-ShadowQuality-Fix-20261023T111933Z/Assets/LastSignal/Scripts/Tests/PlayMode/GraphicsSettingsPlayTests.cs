using System.Collections;
using System.Collections.Generic;
using LastSignal.RenderingQuality;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastSignal.Tests
{
    public sealed class GraphicsSettingsPlayTests
    {
        GraphicsSettingsManager manager;
        readonly List<GameObject> owned = new List<GameObject>();
        bool had, hadLegacy;
        string saved;
        int legacy;
        GameObject Make(string name) { var go = new GameObject(name); owned.Add(go); return go; }
        [UnitySetUp] public IEnumerator Setup()
        {
            had = PlayerPrefs.HasKey(GraphicsSettingsPersistence.Key); saved = PlayerPrefs.GetString(GraphicsSettingsPersistence.Key);
            hadLegacy = PlayerPrefs.HasKey("LastSignal.Forest.TreeShadows"); legacy = PlayerPrefs.GetInt("LastSignal.Forest.TreeShadows");
            Assert.That(GraphicsSettingsManager.Instance, Is.Null, "Run this fixture in an empty test scene.");
            PlayerPrefs.DeleteKey(GraphicsSettingsPersistence.Key); PlayerPrefs.DeleteKey("LastSignal.Forest.TreeShadows");
            manager = GraphicsSettingsManager.Ensure();
            Assert.That(manager.Adapter.Supported, Is.True, "Requires project's HDRP configuration.");
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var go in owned) if (go) Object.Destroy(go); owned.Clear();
            yield return null;
            if (manager) Object.Destroy(manager.gameObject);
            yield return null;
            if (had) PlayerPrefs.SetString(GraphicsSettingsPersistence.Key, saved); else PlayerPrefs.DeleteKey(GraphicsSettingsPersistence.Key);
            if (hadLegacy) PlayerPrefs.SetInt("LastSignal.Forest.TreeShadows", legacy); else PlayerPrefs.DeleteKey("LastSignal.Forest.TreeShadows");
            PlayerPrefs.Save();
        }
        [UnityTest] public IEnumerator AllLodsRestoreOriginalModesAndKeepPhysicsAndOtherObjects()
        {
            var root = Make("Registered tree"); root.AddComponent<BoxCollider>(); var body = root.AddComponent<Rigidbody>(); body.useGravity = false;
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube); a.name = "LOD A"; a.transform.SetParent(root.transform);
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = "LOD B"; b.transform.SetParent(root.transform);
            var ra = a.GetComponent<MeshRenderer>(); var rb = b.GetComponent<MeshRenderer>();
            var originalMesh = a.GetComponent<MeshFilter>().sharedMesh; var originalMaterial = ra.sharedMaterial;
            ra.shadowCastingMode = ShadowCastingMode.TwoSided; rb.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            root.AddComponent<LODGroup>().SetLODs(new[] { new LOD(.5f, new Renderer[] { ra }), new LOD(.01f, new Renderer[] { rb }) });
            var unrelated = Make("Building").AddComponent<MeshRenderer>(); unrelated.shadowCastingMode = ShadowCastingMode.On;
            VegetationShadowController.Attach(root);
            var state = manager.Profiles.Get(GraphicsPreset.High); state.treeShadows = ShadowQuality.Off;
            Assert.That(manager.Apply(state, false), Is.True);
            Assert.That(ra.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off)); Assert.That(rb.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(unrelated.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
            Assert.That(ra.enabled && rb.enabled && root.GetComponent<Collider>().enabled, Is.True);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(a.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(originalMesh)); Assert.That(ra.sharedMaterial, Is.SameAs(originalMaterial));
            body.linearVelocity = Vector3.right;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(root.transform.position.x, Is.GreaterThan(0));
            state.treeShadows = ShadowQuality.High; manager.Apply(state, false);
            Assert.That(ra.shadowCastingMode, Is.EqualTo(ShadowCastingMode.TwoSided)); Assert.That(rb.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));
            Object.Destroy(b); yield return null;
            Assert.DoesNotThrow(() => manager.Apply(state, false));
        }
        [UnityTest] public IEnumerator DistanceTiersAndLateRegistrationUseCachedPolicy()
        {
            var camera = Make("Observer").AddComponent<Camera>(); camera.gameObject.tag = "MainCamera"; camera.gameObject.AddComponent<HDAdditionalCameraData>(); manager.RegisterRoot(camera.gameObject);
            var root = Make("Tree at 30m"); root.transform.position = new Vector3(30, 0, 0);
            var renderer = root.AddComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.On;
            var state = manager.Profiles.Get(GraphicsPreset.High); state.treeShadows = ShadowQuality.Low; manager.Apply(state, false);
            VegetationShadowController.Attach(root);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            state.treeShadows = ShadowQuality.Medium; manager.Apply(state, false);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
            root.transform.position = new Vector3(42, 0, 0); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On), "Exit hysteresis avoids rapid toggling.");
            root.transform.position = new Vector3(45, 0, 0); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
        }
        [UnityTest] public IEnumerator HdrpGlobalOffAndEffectiveDistancePreserveArtisticVolume()
        {
            var camera = Make("HDRP observer").AddComponent<Camera>(); var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>(); hd.volumeLayerMask = 1 << 30;
            var light = Make("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.shadows = LightShadows.Soft;
            var lightData = light.gameObject.AddComponent<HDAdditionalLightData>();
            var local = Make("Scene artistic volume"); local.layer = 30;
            var volume = local.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 100;
            var art = ScriptableObject.CreateInstance<VolumeProfile>(); var original = art.Add<HDShadowSettings>(); original.maxShadowDistance.Override(75); volume.sharedProfile = art;
            manager.RegisterRoot(camera.gameObject); manager.RegisterRoot(light.gameObject);
            var state = manager.State; state.globalShadows = ShadowQuality.Off; state.shadowDistance = 30; manager.Apply(state, false);
            yield return null;
            Assert.That(hd.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.ShadowMaps), Is.False);
            Assert.That(light.shadows, Is.EqualTo(LightShadows.Soft), "Do not overwrite atmosphere lamp budgets."); Assert.That(light.enabled, Is.True);
            Assert.That(manager.Adapter.TryReadEffectiveDistance(camera, out float effective), Is.True); Assert.That(effective, Is.EqualTo(30).Within(.01f));
            Assert.That(original.maxShadowDistance.value, Is.EqualTo(75));
            state.globalShadows = ShadowQuality.Low; manager.Apply(state, false); Assert.That(lightData.shadowResolution.@override, Is.EqualTo(512));
            state.globalShadows = ShadowQuality.Medium; manager.Apply(state, false); Assert.That(lightData.shadowResolution.@override, Is.EqualTo(1024));
            state.globalShadows = ShadowQuality.High; manager.Apply(state, false); Assert.That(lightData.shadowResolution.@override, Is.EqualTo(2048));
            Assert.That(light.shadows, Is.EqualTo(LightShadows.Soft));
            manager.RestoreAuthored(); Assert.That(manager.Adapter.TryReadEffectiveDistance(camera, out effective), Is.True); Assert.That(effective, Is.EqualTo(75).Within(.01f));
            volume.sharedProfile = null; Object.Destroy(original); Object.Destroy(art);
        }
        [UnityTest] public IEnumerator PresenterPersistenceSceneLifecycleAndReset()
        {
            Assert.That(manager.State.preset, Is.EqualTo(GraphicsPreset.Low));
            var ui = Make("Graphics view").AddComponent<GraphicsSettingsPresenter>(); ui.Initialize(manager);
            ui.SelectPreset(GraphicsPreset.High); ui.Edit(s => s.treeShadows = ShadowQuality.Off);
            Assert.That(ui.Draft.preset, Is.EqualTo(GraphicsPreset.Custom)); Assert.That(ui.ApplyDraft(), Is.True);
            Assert.That(manager.State.globalShadows, Is.EqualTo(ShadowQuality.High));
            Assert.That(GraphicsSettingsPersistence.Load(manager.Profiles).SameValues(manager.State), Is.True);
            Assert.That(string.Join(" ", System.Array.ConvertAll(ui.GetComponentsInChildren<Text>(), t => t.text)), Does.Contain("Custom"));
            manager.ChangesLocked = true; Assert.That(manager.ResetDefaults(), Is.False); manager.ChangesLocked = false;
            var scene = SceneManager.CreateScene("Graphics lifecycle test");
            var tree = Make("Late scene tree"); SceneManager.MoveGameObjectToScene(tree, scene); var renderer = tree.AddComponent<MeshRenderer>(); VegetationShadowController.Attach(tree);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(GraphicsSettingsManager.Ensure(), Is.SameAs(manager));
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(manager.RegisteredTreeGroups, Is.Zero);
            Assert.That(manager.State.preset, Is.EqualTo(GraphicsPreset.Custom));
            Assert.That(manager.ResetDefaults(), Is.True); Assert.That(ui.Draft.SameValues(manager.Profiles.Get(GraphicsPreset.Low)), Is.True);
        }
    }
}
