using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class CombatAcceptanceProject
    {
        [MenuItem("Last Signal/S002/Create Combat Assets")]
        public static void CreateAssets()
        {
            Directory.CreateDirectory("Assets/LastSignal/Runtime/Combat");
            Directory.CreateDirectory("Assets/LastSignal/Prefabs/Combat");
            Directory.CreateDirectory("Assets/LastSignal/Animations");
            Directory.CreateDirectory("Assets/LastSignal/Scenes");

            Debug.Log("Starting combat asset creation...");
            var def = CreateWeaponDefinition();
            Debug.Log("WeaponDefinition created.");
            RealAssetIntegration.BuildAdapter();
            RealAssetIntegration.IntegratePlayer();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/LastSignal/Scenes/CombatAcceptance.unity")) CreateTestScene();
            
            AssetDatabase.SaveAssets();
            Debug.Log("S002_COMBAT_ASSETS CREATED");
        }

        static WeaponDefinition CreateWeaponDefinition()
        {
            string path = "Assets/LastSignal/Runtime/Combat/WeaponDefinition_AssaultRifle.asset";
            var def = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (!def)
            {
                def = ScriptableObject.CreateInstance<WeaponDefinition>();
                var so = new SerializedObject(def);
                so.FindProperty("weaponName").stringValue = "Assault Rifle";
                so.FindProperty("magazineCapacity").intValue = 30;
                so.FindProperty("ammunition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LastSignal.Inventory.Data.ItemDefinition>("Assets/Game/Items/Definitions/ammo.rifle.asset");
                so.FindProperty("startingMagazine").intValue = 30;
                so.FindProperty("fireMode").enumValueIndex = (int)FireMode.Automatic;
                so.FindProperty("roundsPerMinute").floatValue = 600;
                so.FindProperty("baseDamage").floatValue = 30;
                // Clip durations from analysis
                so.FindProperty("equipSeconds").floatValue = 0.5f; // wpn_val_draw
                so.FindProperty("unequipSeconds").floatValue = 0.5f; // wpn_val_hide
                so.FindProperty("tacticalReloadSeconds").floatValue = 2.5f; // wpn_val_reload
                so.FindProperty("emptyReloadSeconds").floatValue = 3.33f; // wpn_val_reload_full
                so.FindProperty("reloadCommitNormalized").floatValue = 0.55f;
                so.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(def, path);
            }
            return def;
        }

        static AnimatorController CreateAnimatorController()
        {
            string path = "Assets/LastSignal/Animations/FPSArms.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller) return controller;

            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("WeaponState", AnimatorControllerParameterType.Int); // 0=Holstered, 1=Equipping, 2=Ready, 3=Reloading, 4=Unequipping
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("EmptyReload", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Equip", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Unequip", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AimAmount", AnimatorControllerParameterType.Float);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Load clips
            var valAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/LastSignal/VAL.fbx");
            AnimationClip GetClip(string name) => valAssets.OfType<AnimationClip>().FirstOrDefault(c => c.name == "LVA4_Armature|" + name);

            var clipIdle = GetClip("wpn_val_idle");
            var clipFire = GetClip("wpn_val_shoot");
            var clipReload = GetClip("wpn_val_reload");
            var clipEmptyReload = GetClip("wpn_val_reload_full");
            var clipEquip = GetClip("wpn_val_draw");
            var clipUnequip = GetClip("wpn_val_hide");

            var stateHolstered = rootStateMachine.AddState("Holstered");
            var stateEquipping = rootStateMachine.AddState("Equipping"); stateEquipping.motion = clipEquip;
            var stateReady = rootStateMachine.AddState("Ready"); stateReady.motion = clipIdle;
            var stateFiring = rootStateMachine.AddState("Firing"); stateFiring.motion = clipFire;
            var stateReloading = rootStateMachine.AddState("Reloading"); stateReloading.motion = clipReload;
            var stateEmptyReloading = rootStateMachine.AddState("EmptyReloading"); stateEmptyReloading.motion = clipEmptyReload;
            var stateUnequipping = rootStateMachine.AddState("Unequipping"); stateUnequipping.motion = clipUnequip;

            rootStateMachine.defaultState = stateHolstered;

            // Transitions from Any State (driven by triggers)
            var anyToEquip = rootStateMachine.AddAnyStateTransition(stateEquipping);
            anyToEquip.AddCondition(AnimatorConditionMode.If, 0, "Equip");
            anyToEquip.duration = 0.1f;

            var anyToUnequip = rootStateMachine.AddAnyStateTransition(stateUnequipping);
            anyToUnequip.AddCondition(AnimatorConditionMode.If, 0, "Unequip");
            anyToUnequip.duration = 0.1f;

            // Firing
            var anyToFire = rootStateMachine.AddAnyStateTransition(stateFiring);
            anyToFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            anyToFire.duration = 0.05f;

            var fireToReady = stateFiring.AddTransition(stateReady);
            fireToReady.hasExitTime = true;
            fireToReady.exitTime = 1f; // End of clip
            fireToReady.duration = 0.1f;

            // Reloads
            var anyToReload = rootStateMachine.AddAnyStateTransition(stateReloading);
            anyToReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            anyToReload.duration = 0.1f;

            var anyToEmptyReload = rootStateMachine.AddAnyStateTransition(stateEmptyReloading);
            anyToEmptyReload.AddCondition(AnimatorConditionMode.If, 0, "EmptyReload");
            anyToEmptyReload.duration = 0.1f;

            var reloadToReady = stateReloading.AddTransition(stateReady);
            reloadToReady.hasExitTime = true; reloadToReady.exitTime = 1f; reloadToReady.duration = 0.15f;

            var emptyReloadToReady = stateEmptyReloading.AddTransition(stateReady);
            emptyReloadToReady.hasExitTime = true; emptyReloadToReady.exitTime = 1f; emptyReloadToReady.duration = 0.15f;

            // Equip/Unequip completion
            var equipToReady = stateEquipping.AddTransition(stateReady);
            equipToReady.hasExitTime = true; equipToReady.exitTime = 1f; equipToReady.duration = 0.1f;

            var unequipToHolstered = stateUnequipping.AddTransition(stateHolstered);
            unequipToHolstered.hasExitTime = true; unequipToHolstered.exitTime = 1f; unequipToHolstered.duration = 0.1f;

            return controller;
        }

        static void CreateFPSArmsPrefab(AnimatorController controller)
        {
            string path = "Assets/LastSignal/Prefabs/Combat/FPSArms.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;

            var valModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/VAL.fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(valModel);
            instance.name = "FPSArms";
            
            var anim = instance.GetComponent<Animator>();
            if (!anim) anim = instance.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            // Expose the weapon socket (wpn_body) for attaching the actual weapon mesh later,
            // or we use the WeaponViewPresenter to move the whole arms parent.
            // Since VAL brings its own gun mesh, we will hide it and attach our MR POLY gun.
            var valGun = instance.transform.Find("VAL_Model");
            if (valGun) valGun.gameObject.SetActive(false); // Hide the built-in model, we just want arms

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void CreateAssaultRiflePrefab(WeaponDefinition def)
        {
            string path = "Assets/Resources/Weapon_AssaultRifle.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;

            var mrPoly = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/MR POLY/Low Poly Weapons Set/Models/Assault Rifle.fbx");
            if (!mrPoly) { Debug.LogError("Could not find MR POLY Assault Rifle FBX!"); return; }
            
            var root = new GameObject("Weapon_AssaultRifle");
            var weaponCtrl = root.AddComponent<WeaponController>();
            var viewPres = root.AddComponent<WeaponViewPresenter>();
            var animPres = root.AddComponent<WeaponAnimationPresenter>();
            var vfxPres = root.AddComponent<WeaponVfxPresenter>();
            var audioPres = root.AddComponent<WeaponAudioPresenter>();

            // Setup weapon view root
            var viewRoot = new GameObject("ViewmodelRoot").transform;
            viewRoot.SetParent(root.transform, false);
            
            // Setup arms
            var armsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Combat/FPSArms.prefab");
            var arms = (GameObject)PrefabUtility.InstantiatePrefab(armsPrefab, viewRoot);
            
            // Put MR POLY weapon into the wpn_body socket of the arms
            var socket = arms.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "wpn_body");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(mrPoly);
            model.transform.SetParent(socket ? socket : arms.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Adjust MR Poly model rotation to match the wpn_body orientation
            model.transform.localRotation = Quaternion.Euler(0, -90, 0); // Need to tweak this visually usually

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(model.transform, false);
            muzzle.localPosition = new Vector3(0, 0.1f, 0.5f); // Guess based on usual assault rifle lengths

            var aimRef = new GameObject("AimReference").transform;
            aimRef.SetParent(model.transform, false);
            
            // Wire components
            var so = new SerializedObject(weaponCtrl);
            so.FindProperty("definition").objectReferenceValue = def;
            so.FindProperty("muzzle").objectReferenceValue = muzzle;
            so.FindProperty("aimReference").objectReferenceValue = aimRef;
            so.ApplyModifiedProperties();

            var vso = new SerializedObject(viewPres);
            vso.FindProperty("weapon").objectReferenceValue = weaponCtrl;
            vso.FindProperty("viewmodelRoot").objectReferenceValue = viewRoot;
            vso.ApplyModifiedProperties();

            var aso = new SerializedObject(animPres);
            aso.FindProperty("weapon").objectReferenceValue = weaponCtrl;
            aso.FindProperty("animator").objectReferenceValue = arms.GetComponent<Animator>();
            aso.ApplyModifiedProperties();

            var vfso = new SerializedObject(vfxPres);
            vfso.FindProperty("weapon").objectReferenceValue = weaponCtrl;
            vfso.ApplyModifiedProperties();

            var auso = new SerializedObject(audioPres);
            auso.FindProperty("weapon").objectReferenceValue = weaponCtrl;
            auso.FindProperty("audioSource").objectReferenceValue = root.AddComponent<AudioSource>();
            auso.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        public static void RecoverMissingScene()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Exit Play Mode and save the current scene before recovery.");
            if (System.IO.File.Exists("Assets/LastSignal/Scenes/CombatAcceptance.unity"))
                throw new System.InvalidOperationException("Refusing to overwrite CombatAcceptance.");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player.prefab");
            if (!player || !player.GetComponent<PlayerCombatController>())
                throw new System.InvalidOperationException("Recovery requires the existing combat-ready player prefab.");
            System.IO.Directory.CreateDirectory("Assets/LastSignal/Scenes");
            CreateTestScene();
        }

        static void CreateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .6f, .65f);
            
            var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.25f, 0);
            ground.transform.localScale = new Vector3(40, 0.5f, 40);
            
            var target1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target1.name = "CombatTarget_10m";
            target1.transform.position = new Vector3(0, 1f, 10);
            target1.transform.localScale = new Vector3(1f, 2f, 1f);
            target1.AddComponent<DamageableTarget>();

            var target2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target2.name = "CombatTarget_20m";
            target2.transform.position = new Vector3(5, 1f, 20);
            target2.transform.localScale = new Vector3(1f, 2f, 1f);
            target2.AddComponent<DamageableTarget>();

            // Setup session player
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player.prefab");
            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            
            // Add Combat Controller to player
            if (!playerInstance.GetComponent<PlayerCombatController>())
            {
                var input = playerInstance.GetComponent<PlayerInputReader>();
                var look = playerInstance.GetComponent<FirstPersonLook>();
                var wpnParent = new GameObject("WeaponParent").transform;
                wpnParent.SetParent(look.View.transform, false);

                var combat = playerInstance.AddComponent<PlayerCombatController>();
                var so = new SerializedObject(combat);
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("look").objectReferenceValue = look;
                so.FindProperty("weaponParent").objectReferenceValue = wpnParent;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(playerInstance, "Assets/LastSignal/Prefabs/Player.prefab");
            }
            Object.DestroyImmediate(playerInstance);

            var spawn = new GameObject("Spawn").transform;
            var session = new GameObject("Session", typeof(SessionFlow)).GetComponent<SessionFlow>();
            session.Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player.prefab"), spawn, new DoorInteractable[0]);

            // Add HUD
            var hudMethod = typeof(S001Project).GetMethod("CreateHud", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (hudMethod != null) hudMethod.Invoke(null, new object[] { session });

            // We need to fetch the AcceptanceHud and add an Ammo text field to it
            var hud = Object.FindAnyObjectByType<AcceptanceHud>();
            if (hud)
            {
                var panel = hud.transform.Find("Menu");
                var ammoObj = new GameObject("AmmoDisplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
                ammoObj.transform.SetParent(hud.transform, false);
                var rect = ammoObj.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(1, 0); // Bottom Right
                rect.anchoredPosition = new Vector2(-150, 50);
                rect.sizeDelta = new Vector2(200, 60);
                var text = ammoObj.GetComponent<UnityEngine.UI.Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 32;
                text.alignment = TextAnchor.MiddleRight;
                text.color = Color.white;
                
                var so = new SerializedObject(hud);
                so.FindProperty("ammoDisplay").objectReferenceValue = text;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.SaveScene(scene, "Assets/LastSignal/Scenes/CombatAcceptance.unity");
        }
    }
}
