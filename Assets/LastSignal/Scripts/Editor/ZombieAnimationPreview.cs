using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LastSignal.Editor
{
    /// <summary>Editor-only continuous/scrub preview. Does not drive any gameplay state.</summary>
    public sealed class ZombieAnimationPreview : EditorWindow
    {
        int selected;
        float time;
        bool playing;
        double lastUpdate;
        PlayableGraph graph;
        AnimationClipPlayable playable;
        AnimationClip clip;
        Animator animator;
        AnimatorCullingMode originalCulling;

        [MenuItem("Last Signal/Zombie Assets/Animation preview")]
        public static void Open()
        {
            var window = GetWindow<ZombieAnimationPreview>("Zombie Asset Preview");
            window.StartPreview();
            window.playing = window.graph.IsValid();
        }
        void OnEnable() { EditorApplication.update += Tick; lastUpdate = EditorApplication.timeSinceStartup; }
        void OnDisable() { EditorApplication.update -= Tick; Stop(); }
        void Stop()
        {
            playing = false;
            if (graph.IsValid()) graph.Destroy();
            if (animator) { animator.Rebind(); animator.Update(0); animator.cullingMode = originalCulling; }
            animator = null;
        }
        void OnGUI()
        {
            EditorGUILayout.LabelField("B0B — presentation only", EditorStyles.boldLabel);
            if (GUILayout.Button("Open acceptance scene"))
            {
                Stop();
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ZombieAssetIntegration.ScenePath);
            }
            int next = EditorGUILayout.Popup("Motion", selected, ZombieAssetIntegration.States);
            if (next != selected) { selected = next; StartPreview(); }
            if (GUILayout.Button(playing ? "Pause" : "Play"))
            {
                if (!graph.IsValid()) StartPreview();
                playing = !playing; lastUpdate = EditorApplication.timeSinceStartup;
            }
            if (clip && graph.IsValid())
            {
                float nextTime = EditorGUILayout.Slider("Seconds", time, 0, clip.length);
                if (nextTime != time) { time = nextTime; playing = false; Sample(); }
                EditorGUILayout.LabelField(clip.name + (clip.isLooping ? " (loop)" : " (holds final pose)"));
            }
            if (GUILayout.Button("Reset preview")) Stop();
        }
        void StartPreview()
        {
            Stop();
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ZombieAssetIntegration.ScenePath) return;
            animator = Object.FindFirstObjectByType<Animator>();
            if (!animator) return;
            originalCulling = animator.cullingMode;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            clip = ZombieAssetIntegration.Clip(selected == 4 ? "LS_Zombie_Death" : ZombieAssetIntegration.Clips[selected]);
            graph = PlayableGraph.Create("Zombie editor preview"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(graph, "Presentation", animator).SetSourcePlayable(playable); graph.Play(); time = 0; Sample();
        }
        void Sample() { playable.SetTime(time); graph.Evaluate(0); SceneView.RepaintAll(); Repaint(); }
        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (EditorApplication.isPlayingOrWillChangePlaymode || (graph.IsValid() && !animator)) Stop();
            if (playing && graph.IsValid())
            {
                time += (float)(now - lastUpdate);
                if (time >= clip.length) { if (clip.isLooping) time %= clip.length; else { time = clip.length; playing = false; } }
                Sample();
            }
            lastUpdate = now;
        }
    }
}
