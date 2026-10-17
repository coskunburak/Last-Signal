using System;
using System.IO;
using System.Linq;
using LastSignal.Atmosphere;
using LastSignal.WorldTime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace LastSignal.Editor.FirstTown
{
    /// <summary>Explicit USER-RUN checks. Not called by installer or on import.</summary>
    public static class FirstTownAtmosphereVerification
    {
        [MenuItem("Last Signal/First Town/Atmosphere/Verify authored references (user run)")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit PlayMode before reference validation.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=FirstTownAuthoring.ScenePath)
            {
                for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                    if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save modified scenes first.");
                scene=EditorSceneManager.OpenScene(FirstTownAuthoring.ScenePath);
            }
            var roots=scene.GetRootGameObjects();var directors=roots.SelectMany(r=>r.GetComponentsInChildren<TownAtmosphere>(true)).ToArray();
            if(directors.Length!=1)throw new InvalidOperationException("Expected one atmosphere director.");var d=directors[0];
            if(!d.sun || !d.moon || !d.sky || !d.rain || !d.volume || !d.volume.sharedProfile || !d.exteriorReflection)throw new InvalidOperationException("Missing atmosphere dependency.");
            var view=roots.SelectMany(r=>r.GetComponentsInChildren<WorldTimePresentation>(true)).Single();
            if(new SerializedObject(view).FindProperty("atmosphere").objectReferenceValue!=d)throw new InvalidOperationException("Presentation ownership missing.");
            if(roots.SelectMany(r=>r.GetComponentsInChildren<LastSignal.Slice.S013EnvironmentPresentation>(true)).Any())throw new InvalidOperationException("Competing legacy environment writer.");
            if(view.Rain && view.Rain.gameObject.activeSelf)throw new InvalidOperationException("Legacy rain active.");
            if(d.rain.middle.Length!=8 || d.rain.middle.Any(p=>!p) || !d.rain.near || !d.rain.splash)throw new InvalidOperationException("Rain wiring incomplete.");
            foreach(var n in new[]{"_DayTex","_OvercastTex","_SunsetTex","_NightTex","_CloudTex"})if(!d.sky.GetTexture(n))throw new InvalidOperationException("Missing sky texture "+n);
            if(ShaderUtil.ShaderHasError(d.sky.shader))throw new InvalidOperationException("Sky shader has compiler errors.");
            if(!d.volume.sharedProfile.TryGet<Tonemapping>(out var tone) || tone.mode.value!=TonemappingMode.ACES || d.volume.sharedProfile.components.Any(c=>!c))throw new InvalidOperationException("Invalid persistent volume profile.");
            if(d.surfaces.Any(s=>!s.material) || d.wetRenderers.Any(r=>!r) || d.lamps.Any(l=>!l.light))throw new InvalidOperationException("Dangling presentation references.");
            Directory.CreateDirectory("Docs/Implementation/FirstTownAtmosphere/Evidence");
            File.WriteAllText("Docs/Implementation/FirstTownAtmosphere/Evidence/references-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json","{\"references\":\"PASS\",\"visual\":\"NOT_RUN\",\"gameplay\":\"NOT_RUN\",\"performance\":\"NOT_RUN\"}");
            Debug.Log("FirstTown atmosphere reference checks PASS; visual/gameplay/performance NOT_RUN.");
        }
    }
}
