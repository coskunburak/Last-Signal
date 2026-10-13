using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>Idempotent, targeted S021 cosmetic migration. Never rebuilds scenes or combat definitions.</summary>
    public static class WeaponGripAuthoring
    {
        const string Folder = "Assets/LastSignal/Prefabs/Combat/S021/";
        const string Data = "Assets/LastSignal/Data/Combat/S021/";
        const string Animation = "Assets/LastSignal/Animations/Weapons/Grip/";
        [MenuItem("Last Signal/Character/Author S021 Weapon Grips")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before grip authoring.");
            if (!AssetDatabase.IsValidFolder(Animation.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/LastSignal/Animations/Weapons", "Grip");
            AuthorWeapon("Pistol", "Weapon_TacticalPistol", "Diesel_WorldPistol", "MRPoly_TacticalPistol", WeaponGripMode.OneHandedPistol);
            AuthorWeapon("Machete", "Machete_Viewmodel", "Diesel_WorldMachete", "Machete_Model", WeaponGripMode.OneHandedLightMelee);
            AuthorWeapon("WoodenAxe", "WoodenAxe_Viewmodel", "Diesel_WorldWoodenAxe", "WoodenAxe_Model", WeaponGripMode.OneHandedHeavyMelee);
            MeleeGripAuthoring.Author();
            PistolGripAuthoring.Author();
            AssetDatabase.SaveAssets();
            Debug.Log("S021 grip assets saved. Authoring only; compilation, tests and visual acceptance are NOT_RUN.");
        }
        static void AuthorWeapon(string name, string fpsName, string worldName, string modelName, WeaponGripMode mode)
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(Data + name + "Pose.asset");
            if (!profile) throw new InvalidOperationException("Missing existing pose " + name);
            bool pistol = mode == WeaponGripMode.OneHandedPistol, heavy = mode == WeaponGripMode.OneHandedHeavyMelee;
            profile.GripMode = mode; profile.PrimaryHand = WeaponPrimaryHand.Right;
            profile.AttackOwnership = WeaponPoseOwnership.ProfileArmTargets; profile.WorldScale = 1;
            profile.PoseBlendSeconds = heavy ? .22f : .15f;
            profile.HipGripOffset = pistol ? new Vector3(.03f, -.23f, .30f) : new Vector3(.06f, -.25f, .30f);
            profile.AimGripOffset = new Vector3(-.06f, -.05f, .43f);
            profile.HipEuler = pistol ? new Vector3(18, 0, -6) : new Vector3(heavy ? 32 : 48, heavy ? -8 : 18, heavy ? -18 : -28);
            profile.AimEuler = Vector3.zero; profile.TorsoYaw = pistol ? 8 : 12;
            profile.FpsHipPosition = pistol ? new Vector3(.14f, -.19f, .28f) : heavy ? new Vector3(.16f, -.20f, .32f) : new Vector3(.14f, -.22f, .30f);
            profile.FpsHipEuler = pistol ? new Vector3(18, 0, -6) : new Vector3(heavy ? 32 : 48, heavy ? -8 : 18, heavy ? -18 : -28);
            profile.FpsOffhandPosition = pistol ? new Vector3(-.22f, -.25f, -.03f) : new Vector3(-.18f, -.27f, .05f);
            profile.FpsRaisedPosition = new Vector3(.12f, -.13f, .30f);
            profile.FpsRaisedEuler = new Vector3(3, 0, -3);
            profile.RaisedGripOffset = new Vector3(.02f, -.17f, .36f);
            profile.RaisedEuler = new Vector3(4, 0, -4);
            profile.ReadyHoldSeconds = 1.1f;
            profile.SightDistance = .34f;
            if (pistol) { profile.RecoilDistance = .008f; profile.RecoilPitch = 3; }
            profile.FingerGripWeight = 1; profile.FingerClosure = pistol ? .85f : heavy ? .65f : .80f;
            profile.WindupOffset = heavy ? new Vector3(.04f, .12f, -.08f) : new Vector3(.04f, .04f, -.04f);
            // Keep the heavy contact arc within the measured arm's reach as the wrist turns.
            // Extending forward at impact exhausted reach during the active/follow-through phase.
            profile.StrikeOffset = heavy ? new Vector3(-.10f, -.06f, -.06f) : new Vector3(-.12f, -.025f, .025f);
            profile.WindupEuler = heavy ? new Vector3(-30, 25, 30) : new Vector3(-12, 30, 12);
            profile.StrikeEuler = heavy ? new Vector3(38, -30, -28) : new Vector3(12, -38, -18);
            profile.FollowThroughOffset = profile.StrikeOffset + (heavy ? new Vector3(-.015f, -.025f, -.015f) : new Vector3(-.015f, -.01f, .005f));
            profile.FollowThroughEuler = profile.StrikeEuler + new Vector3(8, -6, -5);
            profile.FollowThroughFraction = .10f;
            profile.FingerClosure = 1;
            profile.FingerFlexion = pistol ? new Vector3(65, 85, 55) : heavy ? new Vector3(62, 80, 48) : new Vector3(58, 75, 45);
            profile.ThumbFlexion = pistol ? new Vector3(22, 38, 28) : new Vector3(25, 40, 28);
            profile.TriggerFlexion = new Vector3(16, 38, 22);
            profile.ThumbOpposition = pistol ? 25 : 30;
            profile.HandPosition = profile.HandEuler = Vector3.zero;
            EditorUtility.SetDirty(profile);
            Edit(Folder + fpsName + ".prefab", root =>
            {
                var frame = Find(root.transform, pistol ? "ViewmodelRoot" : "VisualRoot");
                var model = Find(root.transform, modelName);
                var visual = Find(root.transform, "WeaponVisualRoot");
                if (!visual) visual = NewTransform(frame, "WeaponVisualRoot");
                model.SetParent(visual, false); model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity;
                // Measured imported geometry already uses metres. Pistol is 24 cm, machete 75 cm, axe 63 cm.
                model.localScale = Vector3.one;
                var sockets = visual.GetComponent<CharacterWeaponSockets>() ?? visual.gameObject.AddComponent<CharacterWeaponSockets>();
                ConfigureSockets(sockets, model, pistol, heavy);
                CharacterWeaponSockets.Attach(visual, sockets.PrimaryGrip, frame.position,
                    frame.rotation * Quaternion.Euler(0, 0, 90));
                if (pistol)
                {
                    // Disable inherited rifle-only geometry, including children overlooked by bounds-fit authoring.
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                        if (renderer.name == "Trigger" || renderer.name == "ScopeLens" || renderer.name == "MRPoly_RifleBody" ||
                            renderer.name == "MRPoly_Magazine" || renderer.name == "VAL_Model" || renderer.name == "Environment") renderer.enabled = false;
                    var scope = root.GetComponent<ScopeOpticPresenter>(); if (scope) scope.enabled = false;
                    var oldMuzzle = Find(root.transform, "Muzzle");
                    // Keep the existing gameplay reference identity, only correct its cosmetic location.
                    if (oldMuzzle && oldMuzzle != sockets.Muzzle)
                    { oldMuzzle.SetParent(model, false); oldMuzzle.localPosition = sockets.Muzzle.localPosition; oldMuzzle.localRotation = Quaternion.identity; sockets.Muzzle = oldMuzzle; }
                    var ctrl = new SerializedObject(root.GetComponent<WeaponController>());
                    ctrl.FindProperty("muzzle").objectReferenceValue = sockets.Muzzle;
                    ctrl.FindProperty("aimReference").objectReferenceValue = sockets.AimReference;
                    ctrl.ApplyModifiedPropertiesWithoutUndo();
                    var ejection = Find(root.transform, "CasingEjection");
                    if (ejection) { ejection.SetParent(model, false); ejection.localPosition = new Vector3(.065f, .12f, .015f); ejection.localRotation = Quaternion.identity; }
                    var view = new SerializedObject(root.GetComponent<WeaponViewPresenter>());
                    view.FindProperty("gripProfile").objectReferenceValue = profile;
                    view.FindProperty("recoilKickback").floatValue = .008f;
                    view.ApplyModifiedPropertiesWithoutUndo();
                    // Existing Generic Animator retained, with a single owned idle clip instead of rifle reload/sprint/fire curves.
                    var animator = root.GetComponentInChildren<Animator>(true);
                    animator.runtimeAnimatorController = NeutralController("Pistol", animator);
                    root.GetComponent<WeaponAnimationPresenter>().enabled = false;
                }
                else
                {
                    var animator = frame.GetComponent<Animator>();
                    animator.runtimeAnimatorController = NeutralController(name, null);
                    frame.localPosition = Vector3.zero; frame.localRotation = Quaternion.identity;
                    var presenter = new SerializedObject(root.GetComponent<MeleeWeaponPresenter>());
                    presenter.FindProperty("gripProfile").objectReferenceValue = profile; presenter.ApplyModifiedPropertiesWithoutUndo();
                    var stance = new SerializedObject(root.GetComponent<MeleeStanceViewPresenter>());
                    stance.FindProperty("wideFovPositionOffset").vector3Value = Vector3.zero;
                    stance.ApplyModifiedPropertiesWithoutUndo();
                }
                var grip = root.GetComponent<FirstPersonWeaponGrip>() ?? root.AddComponent<FirstPersonWeaponGrip>();
                grip.Profile = profile; grip.PoseFrame = frame; grip.Visual = visual; grip.Sockets = sockets;
                grip.RightUpper = Find(root.transform, pistol ? "upperarm.R" : "R_arm");
                grip.RightLower = Find(root.transform, pistol ? "forearm.R" : "R_elbow");
                grip.RightHand = Find(root.transform, pistol ? "hand.R" : "R_wrist");
                grip.LeftUpper = Find(root.transform, pistol ? "upperarm.L" : "L_arm");
                grip.LeftLower = Find(root.transform, pistol ? "forearm.L" : "L_elbow");
                grip.LeftHand = Find(root.transform, pistol ? "hand.L" : "L_wrist");
                grip.RightFingers = Fingers(root.transform, pistol, false); grip.LeftFingers = Fingers(root.transform, pistol, true);
                grip.AuthoredFingerCurl = false;
                grip.RightFingerRest = FingerBindRotations(root, grip.RightFingers, grip.RightHand);
                grip.LeftFingerRest = FingerBindRotations(root, grip.LeftFingers, grip.LeftHand);
                for (int i = 0; i < 15; i++)
                {
                    grip.RightFingers[i].localRotation = grip.RightFingerRest[i];
                    grip.LeftFingers[i].localRotation = grip.LeftFingerRest[i];
                }
                if (pistol) RemoveInheritedHandCurves(root.GetComponentInChildren<Animator>(true));
            });
            Edit(Folder + worldName + ".prefab", root =>
            {
                var model = Find(root.transform, modelName);
                model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity; model.localScale = Vector3.one;
                var sockets = root.GetComponent<CharacterWeaponSockets>() ?? root.AddComponent<CharacterWeaponSockets>();
                ConfigureSockets(sockets, model, pistol, heavy);
                sockets.WeaponVisualRoot = root.transform;
            });
        }
        static Quaternion[] FingerBindRotations(GameObject root, Transform[] fingers, Transform hand)
        {
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!skin.enabled || !skin.sharedMesh) continue;
                var bones = skin.bones; var binds = skin.sharedMesh.bindposes;
                if (bones.Length != binds.Length) continue;
                var rotations = new Quaternion[15]; bool complete = true;
                for (int i = 0; i < 15; i++)
                {
                    int bone = Array.IndexOf(bones, fingers[i]);
                    int parent = Array.IndexOf(bones, fingers[i].parent);
                    if (bone < 0 || parent < 0) { complete = false; break; }
                    // Both matrices are in the same skin frame; their quotient is the true local bind pose.
                    rotations[i] = (binds[parent] * binds[bone].inverse).rotation;
                }
                if (complete)
                {
                    // Include finger metacarpals/palm auxiliaries, not only the fifteen flexion joints.
                    for (int j = 0; j < bones.Length; j++)
                    {
                        if (!bones[j] || bones[j] == hand || !bones[j].IsChildOf(hand)) continue;
                        int parent = Array.IndexOf(bones, bones[j].parent);
                        if (parent >= 0)
                        {
                            var local = binds[parent] * binds[j].inverse;
                            bones[j].localRotation = local.rotation;
                            bones[j].localPosition = new Vector3(local.m03, local.m13, local.m23);
                        }
                    }
                    return rotations;
                }
            }
            throw new InvalidOperationException("No complete skin bind-pose chain for FPS fingers.");
        }
        static void RemoveInheritedHandCurves(Animator animator)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                string path = AssetDatabase.GetAssetPath(clip);
                if (!path.StartsWith(Animation, StringComparison.Ordinal)) throw new InvalidOperationException("Refusing to edit vendor animation.");
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.path.Contains("/hand.") || binding.path.Contains("/handIK.") || binding.path.Contains("/wpn_body"))
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                EditorUtility.SetDirty(clip);
            }
        }
        static void ConfigureSockets(CharacterWeaponSockets sockets, Transform model, bool pistol, bool heavy)
        {
            sockets.WeaponVisualRoot = sockets.transform;
            // Contacts on the right palmar surface, measured from source mesh handle cross-sections.
            sockets.RightGrip = Socket(model, pistol ? "PrimaryGrip" : "PrimaryGrip_R", pistol ? new Vector3(.061f, .047f, -.058f) :
                heavy ? new Vector3(.011f, .002f, -.12f) : new Vector3(.017f, -.004f, -.12f),
                Quaternion.LookRotation(pistol ? Vector3.forward : Vector3.down, Vector3.left));
            sockets.PalmContact = sockets.RightGrip;
            sockets.SilhouetteReference = Socket(model, pistol ? "SilhouetteReference" : "BladeReference", pistol ? new Vector3(.05f, .13f, .06f) :
                heavy ? new Vector3(0, -.07f, .34f) : new Vector3(0, 0, .36f), Quaternion.identity);
            sockets.LeftGrip = null; sockets.StockContact = null;
            if (pistol)
            {
                // Rear sight top and barrel centre in the actual Tactical Pistol mesh frame.
                sockets.AimReference = Socket(model, "PistolAimReference", new Vector3(.05016f, .15706f, -.078f), Quaternion.identity);
                sockets.Muzzle = Socket(model, "PistolMuzzle", new Vector3(.05016f, .131f, .14312f), Quaternion.identity);
                sockets.Magazine = Find(model, "Magazine");
                sockets.ReloadReference = Socket(sockets.Magazine ? sockets.Magazine : model, "ReloadPalm", Vector3.zero,
                    Quaternion.LookRotation(Vector3.up, Vector3.right));
            }
            else { sockets.AimReference = sockets.Muzzle = sockets.Magazine = sockets.ReloadReference = null; }
        }
        static RuntimeAnimatorController NeutralController(string name, Animator source)
        {
            string path = Animation + name + "Grip.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (existing) return existing;
            AnimationClip clip = null;
            if (source && source.runtimeAnimatorController)
                foreach (var candidate in source.runtimeAnimatorController.animationClips)
                    if (candidate.name.EndsWith("_idle", StringComparison.Ordinal)) { clip = UnityEngine.Object.Instantiate(candidate); break; }
            if (!clip) clip = new AnimationClip();
            clip.name = name + "GripIdle"; AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            if (!source)
            {
                foreach (var axis in new[] { "x", "y", "z" })
                    clip.SetCurve("", typeof(Transform), "m_LocalPosition." + axis, AnimationCurve.Constant(0, 1, 0));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, Animation + name + "GripIdle.anim");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var state = controller.layers[0].stateMachine.AddState("Idle"); state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            return controller;
        }
        static Transform[] Fingers(Transform root, bool pistol, bool left)
        {
            string[] names = pistol ? new[] { "thumb", "index", "middle", "ring", "pinky" } : new[] { "thumb", "point", "middle", "ring", "pink" };
            var bones = new Transform[15];
            for (int f = 0; f < 5; f++) for (int j = 0; j < 3; j++)
            {
                string name = pistol ? names[f] + (f == 0 ? j == 0 ? "_meta" : j.ToString() : (j + 1).ToString()) + (left ? ".L" : ".R") :
                    (left ? "L_" : "R_") + names[f] + (j + 1);
                bones[f * 3 + j] = Find(root, name);
                if (!bones[f * 3 + j]) throw new InvalidOperationException("Missing Generic finger " + name);
            }
            return bones;
        }
        static Transform Socket(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var t = parent.Find(name); if (!t) t = NewTransform(parent, name);
            t.localPosition = position; t.localRotation = rotation; t.localScale = Vector3.one; return t;
        }
        static Transform NewTransform(Transform parent, string name)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); t.gameObject.layer = parent.gameObject.layer; return t; }
        static Transform Find(Transform root, string name)
        { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t; return null; }
        static void Edit(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
