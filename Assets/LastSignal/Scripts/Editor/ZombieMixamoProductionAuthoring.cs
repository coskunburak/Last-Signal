using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LastSignal.Editor
{
    /// <summary>Idempotent project-owned integration of the supplied Humanoid takes.</summary>
    public static class ZombieMixamoProductionAuthoring
    {
        const string AnimationRoot = "Assets/ThirdParty/Animation/AnimationLibraries/Zombie Animation/";
        const string ControllerPath = "Assets/LastSignal/Animations/Zombie/Controllers/AC_Zombie_Shambler.controller";
        const string MaskPath = "Assets/LastSignal/Animations/Zombie/Controllers/LS_Zombie_HitUpperBody.mask";
        const string DefinitionPath = "Assets/LastSignal/Data/Enemies/Zombie/Shambler.asset";
        const string VisualPath = "Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab";

        /// <summary>Builds the production scene into a unique evidence directory supplied by the caller.</summary>
        public static void BuildDevelopmentMac()
        {
            const string scene = "Assets/LastSignal/Scenes/Production/RelayExpedition.unity";
            const string evidenceRoot = "Docs/Implementation/S004/Evidence";
            var requested = Environment.GetEnvironmentVariable("LS_ANIMATION_BUILD_EVIDENCE");
            if (string.IsNullOrWhiteSpace(requested))
                throw new InvalidOperationException("Set LS_ANIMATION_BUILD_EVIDENCE to a new S004 evidence directory.");

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var root = Path.GetFullPath(Path.Combine(projectRoot, evidenceRoot)) + Path.DirectorySeparatorChar;
            var evidence = Path.GetFullPath(requested);
            if (!evidence.StartsWith(root, StringComparison.Ordinal) ||
                (Directory.Exists(evidence) && Directory.EnumerateFileSystemEntries(evidence)
                    .Any(path => Path.GetFileName(path) != "build.log")))
                throw new InvalidOperationException("Build evidence must contain only the current build.log beneath " + root);
            if (!File.Exists(Path.Combine(projectRoot, scene)))
                throw new FileNotFoundException("Production scene is missing.", scene);

            Directory.CreateDirectory(evidence);
            var output = Path.Combine(evidence, "LastSignal.app");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            var summary = "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors +
                "\nWarnings=" + report.summary.totalWarnings + "\nDuration=" + report.summary.totalTime +
                "\nUnity=" + Application.unityVersion + "\nOutput=" + output + "\n";
            File.WriteAllText(Path.Combine(evidence, "build-summary.txt"), summary);
            Debug.Log("Zombie animation development build: " + summary);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Zombie animation development build failed.");
        }

        [MenuItem("Last Signal/Zombie/Apply Supplied Production Animations")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying zombie animation assets.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var definition = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(DefinitionPath);
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
            if (!controller || !definition || !visual)
                throw new InvalidOperationException("Production controller, definition or visual prefab is missing.");

            var walk = Import("Zombie Walk", true);
            var run = Import("Zombie Run", true);
            var attack = Import("Zombie Attack", false);
            var alternate = Import("Zombie Attack (1)", false);
            var dying = Import("Zombie Dying", false);
            var flying = Import("Flying Back Death", false);
            var dyingGround = MeasureGroundOffset(dying);
            var flyingGround = MeasureGroundOffset(flying);
            // Crawl has its own hitbox, navigation and attack contract; retain it for the authoring preview.
            Import("Zombie Crawl", true);

            var baseMachine = controller.layers[0].stateMachine;
            SetMotion(baseMachine, "Locomotion", walk);
            SetMotion(baseMachine, "Run", run);
            SetMotion(baseMachine, "Attack", attack);
            SetMotion(baseMachine, "AttackAlternate", alternate);
            SetMotion(baseMachine, "Death", dying);
            SetMotion(baseMachine, "FlyingBackDeath", flying);

            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (!mask) { mask = new AvatarMask(); AssetDatabase.CreateAsset(mask, MaskPath); }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (var part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head,
                AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers })
                mask.SetHumanoidBodyPartActive(part, true);
            EditorUtility.SetDirty(mask);

            int layerIndex = Array.FindIndex(controller.layers, l => l.name == "Hit Overlay");
            if (layerIndex < 0) { controller.AddLayer("Hit Overlay"); layerIndex = controller.layers.Length - 1; }
            var layers = controller.layers;
            var layer = layers[layerIndex];
            layer.avatarMask = mask;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.defaultWeight = 0;
            const string hitSpeed = "HitReactSpeed";
            var parameter = controller.parameters.FirstOrDefault(p => p.name == hitSpeed);
            if (parameter == null) controller.AddParameter(hitSpeed, AnimatorControllerParameterType.Float);
            else if (parameter.type != AnimatorControllerParameterType.Float)
                throw new InvalidOperationException("HitReactSpeed must be a Float Animator parameter.");
            var parameters = controller.parameters;
            foreach (var candidate in parameters)
                if (candidate.name == hitSpeed) candidate.defaultFloat = 1;
            controller.parameters = parameters;
            var hitState = SetMotion(layer.stateMachine, "HitReact", definition.HitReactClip);
            hitState.speed = 1;
            hitState.speedParameter = hitSpeed;
            hitState.speedParameterActive = true;
            EditorUtility.SetDirty(hitState);
            controller.layers = layers;
            EditorUtility.SetDirty(controller);

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("speed").floatValue = .92f;
            serialized.FindProperty("runSpeed").floatValue = 2.6f;
            serialized.FindProperty("measuredWalkSpeed").floatValue = .38f;
            serialized.FindProperty("measuredRunSpeed").floatValue = 2.6f;
            serialized.FindProperty("attackClip").objectReferenceValue = attack;
            serialized.FindProperty("alternateAttackClip").objectReferenceValue = alternate;
            serialized.FindProperty("attackPlaybackSpeed").floatValue = 1.2f;
            serialized.FindProperty("attackCommitNormalized").floatValue = .35f;
            serialized.FindProperty("attackContactNormalized").floatValue = .42f;
            serialized.FindProperty("attackRecoveryNormalized").floatValue = .55f;
            serialized.FindProperty("alternateAttackCommitNormalized").floatValue = .26f;
            serialized.FindProperty("alternateAttackContactNormalized").floatValue = .32f;
            serialized.FindProperty("alternateAttackRecoveryNormalized").floatValue = .45f;
            serialized.FindProperty("alternateAttackEndNormalized").floatValue = .70f;
            serialized.FindProperty("deathClip").objectReferenceValue = dying;
            serialized.FindProperty("flyingBackDeathClip").objectReferenceValue = flying;
            serialized.FindProperty("deathGroundOffset").animationCurveValue = dyingGround;
            serialized.FindProperty("flyingBackGroundOffset").animationCurveValue = flyingGround;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            var animator = visual.GetComponentInChildren<Animator>(true);
            bool attackValid = definition.IsAttackValid(out var reason);
            if (!animator || animator.runtimeAnimatorController != controller || animator.applyRootMotion ||
                !attackValid || !definition.IsDamagePresentationValid)
                throw new InvalidOperationException("Production animation contract failed: " + reason);
            var runtimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            if (!runtimePrefab) throw new InvalidOperationException("Production zombie runtime prefab is missing.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(runtimePrefab);
            try
            {
                var presenter = instance.GetComponent<ZombieAnimationPresenter>();
                if (!presenter || !presenter.Initialize(definition) || !presenter.HasDamagePresentation() ||
                    !presenter.HasAttackPresentation(attack) || !presenter.HasAttackPresentation(alternate) ||
                    !definition.IsValid(out reason))
                    throw new InvalidOperationException("Runtime zombie animation contract failed: " + reason);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            Debug.Log("Last Signal zombie: walk, run, both attacks, both deaths and masked hit overlay applied.");
        }

        static AnimationClip Import(string file, bool looping)
        {
            string path = AnimationRoot + file + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer || importer.animationType != ModelImporterAnimationType.Human)
                throw new InvalidOperationException("Expected Humanoid animation: " + path);
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("Expected one source take: " + path);
            if (clips[0].loopTime != looping || clips[0].lockRootPositionXZ)
            {
                clips[0].loopTime = looping;
                // NavMeshAgent owns position. Keep source travel in root motion, which the visual ignores.
                clips[0].lockRootPositionXZ = false;
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => c.humanMotion && !c.name.StartsWith("__preview__"));
            if (!clip || clip.isLooping != looping || !clip.humanMotion || clip.events.Length != 0)
                throw new InvalidOperationException("Imported clip failed animation contract: " + path);
            return clip;
        }

        static AnimatorState SetMotion(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            if (!clip) throw new InvalidOperationException("Missing clip for " + name);
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
            if (!state) state = machine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = false;
            EditorUtility.SetDirty(state);
            return state;
        }

        static AnimationCurve MeasureGroundOffset(AnimationClip source)
        {
            var groundOffset = new AnimationCurve();
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(visual);
            var graph = PlayableGraph.Create("Zombie death floor correction");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var mesh = new Mesh();
            try
            {
                var animator = go.GetComponentInChildren<Animator>(true);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                var playable = AnimationClipPlayable.Create(graph, source);
                AnimationPlayableOutput.Create(graph, "Death skeleton", animator).SetSourcePlayable(playable);
                graph.Play();
                for (int i = 0; i <= 100; i++)
                {
                    float time = source.length * i / 100f;
                    playable.SetTime(time); graph.Evaluate(0);
                    float low = float.PositiveInfinity;
                    foreach (var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        skin.BakeMesh(mesh);
                        foreach (var point in mesh.vertices)
                            low = Mathf.Min(low, skin.transform.TransformPoint(point).y);
                    }
                    groundOffset.AddKey(time, .005f - low);
                }
            }
            finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(go); }
            for (int i = 0; i < groundOffset.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(groundOffset, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(groundOffset, i, AnimationUtility.TangentMode.Linear);
            }
            return groundOffset;
        }

        public static void AuditMotion()
        {
            const string evidence = "Docs/Implementation/S004/Evidence/20261001-MixamoProduction/motion.csv";
            Directory.CreateDirectory(Path.GetDirectoryName(evidence));
            var rows = new StringBuilder("clip,time,headY,leftForward,rightForward,minSkinY,presenterGroundY\n");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
            var definition = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(DefinitionPath);
            foreach (string name in new[] { "Zombie Walk", "Zombie Run", "Zombie Attack",
                "Zombie Attack (1)", "Zombie Dying", "Flying Back Death" })
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(AnimationRoot + name + ".fbx")
                    .OfType<AnimationClip>().First(c => c.humanMotion && !c.name.StartsWith("__preview__"));
                var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
                var graph = PlayableGraph.Create("Zombie motion audit");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var mesh = new Mesh();
                try
                {
                    var animator = go.GetComponentInChildren<Animator>(true);
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.applyRootMotion = false;
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    AnimationPlayableOutput.Create(graph, "Skeleton", animator).SetSourcePlayable(playable);
                    graph.Play();
                    var head = animator.GetBoneTransform(HumanBodyBones.Head);
                    var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                    var left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    var right = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    for (int i = 0; i <= 60; i++)
                    {
                        float time = clip.length * i / 60f;
                        playable.SetTime(time); graph.Evaluate(0);
                        float low = float.PositiveInfinity;
                        foreach (var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            skin.BakeMesh(mesh);
                            foreach (var point in mesh.vertices)
                                low = Mathf.Min(low, skin.transform.TransformPoint(point).y);
                        }
                        rows.Append(name).Append(',').Append(time.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append(head.position.y.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append(Vector3.Dot(left.position - hips.position, animator.transform.forward).ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append(Vector3.Dot(right.position - hips.position, animator.transform.forward).ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append(low.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                            .Append((low + (name == "Zombie Dying" || name == "Flying Back Death" ?
                                definition.DeathGroundOffset(time, name == "Flying Back Death") : 0))
                                .ToString("F4", CultureInfo.InvariantCulture)).AppendLine();
                    }
                }
                finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(go); }
            }
            File.WriteAllText(evidence, rows.ToString());
            Debug.Log("Zombie production motion audit: " + evidence);
        }
    }
}
