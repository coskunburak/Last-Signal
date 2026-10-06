using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LastSignal.Editor
{
    // Temporary, unsaved production-rig preview. Never changes the runtime controller or source FBX.
    public sealed class ZombieMixamoPreview : EditorWindow
    {
        const string VisualPath = "Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab";
        const string ClipRoot = "Assets/ThirdParty/Animation/AnimationLibraries/Zombie Animation/";
        static readonly string[] Names = {
            "Zombie Walk", "Zombie Run", "Zombie Attack", "Zombie Attack (1)",
            "Zombie Dying", "Flying Back Death", "Zombie Crawl"
        };

        GameObject preview;
        Animator animator;
        ZombieDefinition tuning;
        Vector3 visualBasePosition;
        AnimationClip clip;
        PlayableGraph graph;
        AnimationClipPlayable playable;
        Object priorSelection;
        int selected;
        float seconds;
        bool playing;
        double lastTick;

        [MenuItem("Last Signal/Zombie/Preview Mixamo On Production Visual")]
        public static void Open()
        {
            var window = GetWindow<ZombieMixamoPreview>("Mixamo On Production Zombie");
            window.Show();
            window.CreatePreview();
        }

        void OnEnable()
        { EditorApplication.update += Tick; lastTick = EditorApplication.timeSinceStartup; }
        void OnDisable()
        { EditorApplication.update -= Tick; DisposePreview(); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Temporary production-rig preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This object is not saved. Death ground correction matches the production presenter. Close this window to remove it.", MessageType.Info);
            if (GUILayout.Button("Create / reset preview")) CreatePreview();
            int choice = EditorGUILayout.Popup("Mixamo clip", selected, Names);
            if (choice != selected) { selected = choice; BindClip(); }
            if (!graph.IsValid()) return;
            if (GUILayout.Button(playing ? "Pause" : "Play"))
            { playing = !playing; lastTick = EditorApplication.timeSinceStartup; }
            float value = EditorGUILayout.Slider("Seconds", seconds, 0, clip.length);
            if (!Mathf.Approximately(value, seconds)) { playing = false; seconds = value; Sample(); }
            EditorGUILayout.LabelField(clip.name + " | " + clip.length.ToString("F3") + " s | source loop=" +
                clip.isLooping + (selected <= 1 ? " | preview repeats" : ""));
        }

        void CreatePreview()
        {
            DisposePreview();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
            if (!source) { Debug.LogError("Production zombie visual prefab is missing: " + VisualPath); return; }
            priorSelection = Selection.activeObject;
            preview = (GameObject)PrefabUtility.InstantiatePrefab(source);
            preview.name = "Mixamo Production Rig Preview (unsaved)";
            preview.hideFlags = HideFlags.DontSave;
            preview.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            animator = preview.GetComponentInChildren<Animator>(true);
            if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
            { Debug.LogError("Production zombie has no valid Humanoid Avatar."); DisposePreview(); return; }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visualBasePosition = animator.transform.localPosition;
            tuning = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(
                "Assets/LastSignal/Data/Enemies/Zombie/Shambler.asset");
            Selection.activeGameObject = preview;
            SceneView.lastActiveSceneView?.FrameSelected();
            BindClip();
        }

        void BindClip()
        {
            if (graph.IsValid()) graph.Destroy();
            clip = null; playing = false; seconds = 0;
            if (!animator) return;
            string path = ClipRoot + Names[selected] + ".fbx";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip candidate && candidate.humanMotion &&
                    !candidate.name.StartsWith("__preview__")) { clip = candidate; break; }
            if (!clip) { Debug.LogError("No Humanoid clip found at " + path); return; }
            graph = PlayableGraph.Create("Last Signal Mixamo production preview");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(graph, "Production skeleton", animator).SetSourcePlayable(playable);
            graph.Play(); Sample();
        }

        void Sample()
        {
            if (!graph.IsValid()) return;
            playable.SetTime(seconds);
            graph.Evaluate(0);
            animator.transform.localPosition = visualBasePosition +
                Vector3.up * (tuning && (selected == 4 || selected == 5)
                    ? tuning.DeathGroundOffset(seconds, selected == 5) : 0);
            SceneView.RepaintAll(); Repaint();
        }

        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (EditorApplication.isPlayingOrWillChangePlaymode || preview && !animator)
            { DisposePreview(); lastTick = now; return; }
            if (playing && graph.IsValid())
            {
                seconds += (float)(now - lastTick);
                if (seconds >= clip.length)
                {
                    if (selected == 0 || selected == 1) seconds %= clip.length;
                    else { seconds = clip.length; playing = false; }
                }
                Sample();
            }
            lastTick = now;
        }

        void DisposePreview()
        {
            playing = false;
            if (graph.IsValid()) graph.Destroy();
            if (preview)
            {
                if (Selection.activeGameObject == preview) Selection.activeObject = priorSelection;
                DestroyImmediate(preview);
            }
            animator = null; preview = null; clip = null; tuning = null;
        }
    }
}
