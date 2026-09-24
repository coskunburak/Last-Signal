using System;
using System.IO;
using LastSignal.WorldTime;
using LastSignal.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;
namespace LastSignal.Editor
{
    public static class WorldTimeAuthoring
    {
        public const string ScenePath="Assets/LastSignal/Scenes/WorldTimeAcceptance.unity";
        public const string Evidence="Docs/Implementation/P02-GAP/Evidence/20260924-final-closure";
        public static void Create()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop PlayMode first.");
            var scene=EditorSceneManager.OpenScene(PersistenceBuild.ScenePath);
            var flow=UnityEngine.Object.FindAnyObjectByType<SessionFlow>();
            var clock=flow.gameObject.AddComponent<WorldClock>();var view=flow.gameObject.AddComponent<WorldTimePresentation>();
            GameObject.Find("Roof").AddComponent<RainRoof>();
            var bed=GameObject.CreatePrimitive(PrimitiveType.Cube);bed.name="Shelter rest bed";bed.transform.position=new Vector3(-15,.35f,11);bed.transform.localScale=new Vector3(2,.5f,.9f);
            bed.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Shelter/Terminal.mat");
            bed.AddComponent<RestPoint>().Configure(clock);
            var hud=UnityEngine.Object.FindAnyObjectByType<AcceptanceHud>();
            var label=new GameObject("World time and rest feedback",typeof(RectTransform),typeof(Text));label.transform.SetParent(hud.transform,false);
            var rect=label.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(24,-75);rect.sizeDelta=new Vector2(850,95);
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=18;text.color=Color.white;text.raycastTarget=false;
            var particles=new GameObject("Local rain presentation",typeof(ParticleSystem));var rain=particles.GetComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main;main.playOnAwake=false;main.loop=true;main.startLifetime=1;main.startSpeed=14;main.startSize=.035f;main.maxParticles=1500;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=new Color(.65f,.8f,1,.5f);var emission=rain.emission;emission.rateOverTime=0;
            var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(14,14,.1f);particles.transform.rotation=Quaternion.Euler(90,0,0);
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=4;renderer.velocityScale=.04f;
            string materialPath="Assets/LastSignal/Shelter/WorldRain.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,materialPath);}renderer.sharedMaterial=material;
            var saveButton=SaveButton(hud.transform,"Save checkpoint",new Vector2(24,-200));
            var loadButton=SaveButton(hud.transform,"Load checkpoint",new Vector2(24,-200));
            var saveFeedback=new GameObject("Checkpoint feedback",typeof(RectTransform),typeof(Text));saveFeedback.transform.SetParent(hud.transform,false);
            var feedbackRect=saveFeedback.GetComponent<RectTransform>();feedbackRect.anchorMin=feedbackRect.anchorMax=feedbackRect.pivot=new Vector2(0,1);feedbackRect.anchoredPosition=new Vector2(24,-245);feedbackRect.sizeDelta=new Vector2(700,50);
            var feedback=saveFeedback.GetComponent<Text>();feedback.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");feedback.fontSize=18;feedback.raycastTarget=false;
            hud.gameObject.AddComponent<WorldTimeSaveControls>().Configure(flow.GetComponent<SaveSession>(),saveButton,loadButton,feedback);
            view.Configure(GameObject.Find("Sun").GetComponent<Light>(),text,rain);clock.Configure(view);
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/authoring.txt","Derived from local PersistenceAcceptance. Same shelter, persistent IDs, loot and encounter retained. Added marked roof, bed, WorldClock, existing uGUI Text and rain/sun presenter.\n");
        }
        static Button SaveButton(Transform parent,string title,Vector2 position)
        {
            var g=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));g.transform.SetParent(parent,false);
            var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=new Vector2(240,40);
            g.GetComponent<Image>().color=new Color(.08f,.2f,.2f,.95f);
            var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(g.transform,false);var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.sizeDelta=Vector2.zero;
            var t=label.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=18;t.alignment=TextAnchor.MiddleCenter;t.text=title;t.raycastTarget=false;return g.GetComponent<Button>();
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop PlayMode first.");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/P02-GAP/LastSignal.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build-result.txt",$"{report.summary.result}\nUnity={Application.unityVersion}\nTarget=StandaloneOSX\nDevelopment=true\nErrors={report.summary.totalErrors}\nWarnings={report.summary.totalWarnings}\nDuration={report.summary.totalTime}\nPath={report.summary.outputPath}\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("P02-GAP build failed.");
        }
    }
}
