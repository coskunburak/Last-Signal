#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using LastSignal.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed partial class CharacterIntegrationPlayTests
    {
        GameObject player, floor;
        InputFixtureIsolation.SceneScope scene;
        InputFixtureIsolation.NativeInputScope nativeInput;
        PlayerInputReader input;
        FirstPersonMotor motor;
        PlayerStance stance;
        PlayerLocomotionPresenter presenter;
        Animator animator;
        CharacterBodyVisibility visibility;
        CharacterInspectionCamera inspection;
        [SetUp] public void Setup()
        {
            // Keep the native manager and clock: scene-owned/shared UI actions can survive across fixtures.
            // Swapping managers here leaves those actions pointing at devices with discarded state buffers.
            scene=new InputFixtureIsolation.SceneScope();
            nativeInput=new InputFixtureIsolation.NativeInputScope();Time.timeScale=1;
            floor=new GameObject("Character fixture floor");floor.transform.position=new Vector3(500,99.75f,500);floor.AddComponent<BoxCollider>().size=new Vector3(50,.5f,50);
            player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab"),new Vector3(500,100.04f,500),Quaternion.identity);
            player.AddComponent<PlayerInventory>().Initialize(24);
            input=player.GetComponent<PlayerInputReader>();input.SetGameplay(true);
            motor=player.GetComponent<FirstPersonMotor>();stance=player.GetComponent<PlayerStance>();
            presenter=player.GetComponentInChildren<PlayerLocomotionPresenter>();animator=presenter.GetComponent<Animator>();
            visibility=presenter.GetComponent<CharacterBodyVisibility>();inspection=player.GetComponent<CharacterInspectionCamera>();
            Physics.SyncTransforms();motor.Simulate(Vector2.zero,false,.2f);
        }
        [TearDown] public void TearDown()
        {
            if(player)Object.DestroyImmediate(player);if(floor)Object.DestroyImmediate(floor);Time.timeScale=1;
            try { nativeInput?.Dispose(); }
            finally { nativeInput=null;scene?.Dispose();scene=null; }
        }
        static void Press(UnityEngine.InputSystem.Controls.ButtonControl button) => InputFixtureIsolation.QueueButton(button, true);
        static void Release(UnityEngine.InputSystem.Controls.ButtonControl button) => InputFixtureIsolation.QueueButton(button, false);
        [Test] public void LocomotionUsesDisplacementAndAuthoritativeStance()
        {
            motor.Simulate(Vector2.right,false,.15f);presenter.Present(.15f);
            Assert.That(animator.GetFloat("MoveX"),Is.GreaterThan(.5f));
            Assert.That(animator.GetFloat("HorizontalSpeed"),Is.EqualTo(motor.HorizontalVelocity.magnitude).Within(.001f));
            Assert.That(animator.GetFloat("MoveY"),Is.EqualTo(0).Within(.01f));
            motor.Simulate(Vector2.up,true,.15f);presenter.Present(.15f);
            Assert.That(animator.GetFloat("MoveY"),Is.GreaterThan(.5f));
            Assert.That(animator.GetFloat("PlaybackRate"),Is.GreaterThan(1),"Sprint must advance the gait faster than normal movement.");
            Assert.That(animator.GetBool("Sprinting"),Is.EqualTo(motor.IsSprinting));
            Assert.That(motor.IsSprinting,Is.True);
            Assert.That(motor.TryStartSlide(),Is.True);presenter.Present(.1f);
            Assert.That(animator.GetBool("IsSliding"),Is.True);Assert.That(animator.GetBool("IsCrouching"),Is.True);
            for(int i=0;i<8;i++)motor.Simulate(Vector2.up,false,.15f);
            presenter.Present(.1f);Assert.That(animator.GetBool("IsSliding"),Is.False);
            stance.TrySetCrouching(true);presenter.Present(.1f);Assert.That(animator.GetBool("IsCrouching"),Is.True);
            stance.TrySetCrouching(false);presenter.Present(.1f);Assert.That(animator.GetBool("IsCrouching"),Is.False);
            motor.Simulate(Vector2.down,false,.3f);presenter.Present(.3f);
            Assert.That(animator.GetFloat("MoveY"),Is.LessThan(-.5f));
            Assert.That(animator.GetFloat("PlaybackRate"),Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator FinishedHitReleasesActionLayerAndReturnsToTwoHandedRiflePose()
        {
            yield return null;yield return new WaitForSeconds(.8f);
            Assert.That(animator.GetLayerWeight(1),Is.Zero);
            player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=1,Category=DamageCategory.Melee});
            Assert.That(animator.GetLayerWeight(1),Is.EqualTo(1),"Accepted damage must still show its action.");
            yield return new WaitForSeconds(.2f);
            Assert.That(presenter.UpperActionBusy,Is.True);
            yield return new WaitForSeconds(3);
            Assert.That(presenter.UpperActionBusy,Is.False);
            Assert.That(animator.GetLayerWeight(1),Is.Zero,"No Action must not retain the previous arm pose over rifle IK.");
            var rig=presenter.GetComponent<CharacterWeaponRig>();
            Assert.That(rig.WeaponVisual.activeSelf,Is.True);
            yield return ObserveRenderedGrip(false);
        }
        [UnityTest] public IEnumerator PausedFirearmPoseDoesNotAccumulateBoneOffsets()
        {
            yield return null;yield return new WaitForSeconds(.8f);
            input.SetGameplay(false);yield return null;
            yield return ObserveRenderedGrip(true);
            input.SetGameplay(true);
        }
        IEnumerator ObserveRenderedGrip(bool checkDrift)
        {
            // The firearm solver is a final LateUpdate pass. Observe the real render boundary,
            // not coroutine Update where last frame's pose has deliberately been restored.
            var rig=presenter.GetComponent<CharacterWeaponRig>();
            var camera=player.GetComponent<FirstPersonLook>().View;
            var previous=camera.targetTexture;
            var target=new RenderTexture(64,64,24);
            int samples=0;float error=0,stockError=0,drift=0;
            Quaternion chestRotation=Quaternion.identity;
            System.Action<ScriptableRenderContext,Camera> observe=(context,view)=>
            {
                if(view!=camera)return;
                error=Mathf.Max(error,Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position,rig.RightWristTarget));
                error=Mathf.Max(error,Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position,rig.LeftWristTarget));
                stockError=Mathf.Max(stockError,Vector3.Distance(rig.Sockets.StockContact.position,rig.StockTarget));
                var current=animator.GetBoneTransform(HumanBodyBones.Chest).rotation;
                if(samples>0)drift=Mathf.Max(drift,Quaternion.Angle(chestRotation,current));
                chestRotation=current;samples++;
            };
            try
            {
                camera.targetTexture=target;
                RenderPipelineManager.beginCameraRendering+=observe;
                for(int i=0;i<120&&samples<(checkDrift?8:1);i++)yield return null;
                Assert.That(samples,Is.GreaterThanOrEqualTo(checkDrift?8:1));
                Assert.That(error,Is.LessThan(.025f),"Both final wrists must remain at their calibrated grip targets.");
                Assert.That(stockError,Is.LessThan(.001f),"The stock must stay at the shoulder reference.");
                if(checkDrift)Assert.That(drift,Is.LessThan(.1f),"Paused procedural pose must not accumulate rotation.");
            }
            finally
            {
                RenderPipelineManager.beginCameraRendering-=observe;
                if(camera)camera.targetTexture=previous;
                target.Release();Object.DestroyImmediate(target);
            }
        }
        [UnityTest] public IEnumerator InspectionRestoresCameraAndShadowsWithoutChangingGameplay()
        {
            yield return null;
            var view=player.GetComponent<FirstPersonLook>().View;
            foreach(var r in presenter.GetComponentsInChildren<Renderer>(true))Assert.That(r.shadowCastingMode,Is.Not.EqualTo(ShadowCastingMode.ShadowsOnly),"Body must remain visible outside its owner's render pass, including Scene view.");
            var position=player.transform.position;bool active=input.GameplayActive;
            inspection.SetInspection(true);
            Assert.That(inspection.Inspection.enabled,Is.True);Assert.That(view.enabled,Is.False);
            Assert.That(inspection.Inspection.cullingMask&(1<<29),Is.Zero);
            Assert.That(inspection.Inspection.cullingMask&(1<<2),Is.Zero,"Legacy crowbar FPS layer must be hidden too.");
            Assert.That(inspection.Inspection.cullingMask&(1<<30),Is.Not.Zero);
            Assert.That(input.GameplayActive,Is.EqualTo(active));Assert.That(player.transform.position,Is.EqualTo(position));
            foreach(var r in presenter.GetComponentsInChildren<Renderer>(true))Assert.That(r.shadowCastingMode,Is.Not.EqualTo(ShadowCastingMode.ShadowsOnly));
            inspection.enabled=false;
            Assert.That(view.enabled,Is.True);Assert.That(inspection.Inspection.enabled,Is.False);
            Assert.That(visibility.Inspecting,Is.False);
        }
        [UnityTest] public IEnumerator EquipReloadMeleeAndPauseShareExistingGameplayState()
        {
            yield return null;yield return new WaitForSeconds(.8f);
            var combat=player.GetComponent<PlayerCombatController>();var weapon=combat.ActiveWeapon;
            Assert.That(weapon,Is.Not.Null);Assert.That(weapon.RuntimeState.State,Is.EqualTo(WeaponState.Ready));
            var rig=presenter.GetComponent<CharacterWeaponRig>();Assert.That(rig.ActiveProfile.Firearm,Is.SameAs(weapon.Definition));
            player.GetComponent<PlayerInventory>().TryAdd(weapon.Definition.Ammunition,30);
            weapon.RuntimeState.TryConsumeShot();weapon.OnReloadRequested();yield return null;
            Assert.That(animator.GetInteger("WeaponState"),Is.EqualTo((int)WeaponState.Reloading));
            input.SetGameplay(false);yield return null;
            float timer=weapon.RuntimeState.StateTimer;float reload=animator.GetFloat("ReloadTime");
            yield return new WaitForSeconds(.2f);
            Assert.That(weapon.RuntimeState.StateTimer,Is.EqualTo(timer));Assert.That(animator.GetFloat("ReloadTime"),Is.EqualTo(reload));
            input.SetGameplay(true);Assert.That(combat.SelectSlot(PlayerCombatController.CombatSlot.Melee),Is.True);yield return null;
            Assert.That(rig.ActiveProfile.Melee,Is.SameAs(combat.Melee.Definition));
            var meleePose=AddPoseProbe();
            Assert.That(combat.Melee.TryAttack(),Is.True);yield return NextPose(meleePose);
            Assert.That(combat.Melee.Simulation.State,Is.Not.EqualTo(MeleeState.Ready));
            bool procedural=rig.ActiveProfile.AttackOwnership==WeaponPoseOwnership.ProfileArmTargets;
            Assert.That(animator.GetBool("MeleeActive"),Is.EqualTo(!procedural),
                "Procedural melee must not also play the legacy crowbar animation.");
            if(procedural)Assert.That(rig.RightGripError,Is.LessThan(.02f));
            Assert.That(animator.GetFloat("MeleeTime"),Is.EqualTo(combat.Melee.Simulation.PresentationProgress).Within(.1f));
            yield return new WaitForSeconds(1);
            Assert.That(animator.GetBool("MeleeActive"),Is.False);
        }
        [UnityTest] public IEnumerator FallDeathRecoveryAndCleanupDoNotMoveGameplayRoot()
        {
            var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;player.transform.position+=Vector3.up*2;capsule.enabled=true;
            Physics.SyncTransforms();motor.Simulate(Vector2.zero,false,.1f);presenter.Present(.1f);
            Assert.That(animator.GetBool("Grounded"),Is.False);Assert.That(animator.GetFloat("VerticalVelocity"),Is.LessThan(0));
            for(int i=0;i<15;i++){motor.Simulate(Vector2.zero,false,.1f);presenter.Present(.1f);}
            Assert.That(animator.GetBool("Grounded"),Is.True);
            var health=player.GetComponent<PlayerHealth>();health.TakeDamage(new DamageInfo{Amount=health.MaxHealth});
            presenter.Present(.1f);var position=player.transform.position;animator.Update(.2f);
            Assert.That(animator.GetBool("Dead"),Is.True);Assert.That(player.transform.position,Is.EqualTo(position));
            health.ResetForSession();presenter.Present(.1f);Assert.That(animator.GetBool("Dead"),Is.False);
            inspection.SetInspection(true);var camera=inspection.Inspection;
            Object.Destroy(player);yield return null;yield return null;
            Assert.That(camera==null,Is.True,"Inspection camera must not survive its player.");
        }
        [UnityTest] public IEnumerator UnequipReplacementAndCombatRemovalClearWorldTargets()
        {
            yield return null;yield return new WaitForSeconds(.8f);
            var combat=player.GetComponent<PlayerCombatController>();
            var rig=presenter.GetComponent<CharacterWeaponRig>();
            var retired=rig.WeaponVisual;Assert.That(retired,Is.Not.Null);
            combat.UnequipWeapon();yield return null;yield return null;
            Assert.That(rig.WeaponVisual==null,Is.True);Assert.That(rig.Sockets==null,Is.True);Assert.That(rig.ActiveProfile==null,Is.True);
            Assert.That(retired==null,Is.True);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab");
            combat.EquipWeapon(Object.Instantiate(prefab).GetComponent<WeaponController>());
            yield return null;
            Assert.That(rig.WeaponVisual,Is.Not.Null);Assert.That(rig.Sockets.HasFirearmGrips,Is.True);
            Object.Destroy(combat);yield return null;yield return null;
            Assert.That(rig.WeaponVisual==null,Is.True);Assert.That(rig.Sockets==null,Is.True);
        }
        [UnityTest] public IEnumerator InspectionRespectsOtherCameraOwnerAndRemoteVisibility()
        {
            yield return null;
            var view=player.GetComponent<FirstPersonLook>().View;
            view.enabled=false;inspection.SetInspection(true);
            Assert.That(inspection.Inspecting,Is.False,"Must not take over an already-disabled gameplay camera.");
            view.enabled=true;inspection.SetInspection(true);Assert.That(inspection.Inspecting,Is.True);
            visibility.SetLocalOwner(false);yield return null;
            Assert.That(inspection.Inspecting,Is.False);Assert.That(view.enabled,Is.True);
            foreach(var renderer in presenter.GetComponentsInChildren<Renderer>(true))
                Assert.That(renderer.shadowCastingMode,Is.Not.EqualTo(ShadowCastingMode.ShadowsOnly));
        }
        [UnityTest] public IEnumerator EachCameraSeesOtherPlayersFullBodiesAndOnlyItsOwnFpsArms()
        {
            var other = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab"),
                player.transform.position + Vector3.right * 3, Quaternion.identity);
            other.AddComponent<PlayerInventory>().Initialize(24);
            other.GetComponent<PlayerInputReader>().SetGameplay(false);
            var observerObject = new GameObject("External character observer");
            var scopeObject = new GameObject("Owner auxiliary camera fixture");
            var firstView = player.GetComponent<FirstPersonLook>().View;
            var otherView = other.GetComponent<FirstPersonLook>().View;
            var observer = observerObject.AddComponent<Camera>();
            scopeObject.transform.SetParent(firstView.transform, false);
            var scope = scopeObject.AddComponent<Camera>();
            var cameras = new[] { firstView, otherView, observer, scope };
            var previousTargets = cameras.Select(c => c.targetTexture).ToArray();
            var targets = new RenderTexture[cameras.Length];
            System.Action<ScriptableRenderContext, Camera> observe = null;
            try
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    targets[i] = new RenderTexture(64,64,24);
                    cameras[i].targetTexture = targets[i];
                    cameras[i].enabled = true;
                }
                yield return null; yield return null;
                var otherBody = other.GetComponentInChildren<CharacterBodyVisibility>();
                var firstSkins = visibility.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var otherSkins = otherBody.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var firstArms = firstView.GetComponentsInChildren<Renderer>(true);
                var otherArms = otherView.GetComponentsInChildren<Renderer>(true);
                Assert.That(firstSkins,Has.Length.EqualTo(11));Assert.That(otherSkins,Has.Length.EqualTo(11));
                Assert.That(firstArms,Is.Not.Empty);Assert.That(otherArms,Is.Not.Empty);
                int seen = 0;
                bool remote = false;
                string failure = null;
                // Observe real URP render callbacks after the production visibility subscribers.
                observe = (context, camera) =>
                {
                    int index = System.Array.IndexOf(cameras,camera);
                    if(index < 0)return;
                    seen |= 1 << index;
                    bool hideFirst = camera == firstView || camera == scope;
                    bool hideOther = !remote && camera == otherView;
                    bool correct = firstSkins.All(r => (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) == hideFirst) &&
                        otherSkins.All(r => (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) == hideOther) &&
                        firstArms.All(r => r.forceRenderingOff == (camera != firstView)) &&
                        otherArms.All(r => r.forceRenderingOff == (remote || camera != otherView));
                    if(!correct && failure == null)failure = "Incorrect body/viewmodel visibility for camera " + index + ", remote=" + remote;
                };
                RenderPipelineManager.beginCameraRendering += observe;
                for(int i=0;i<120 && seen!=15;i++)yield return null;
                Assert.That(seen,Is.EqualTo(15),"All four real camera render passes must be observed.");
                Assert.That(failure,Is.Null);
                Assert.That(firstSkins.All(r => r.shadowCastingMode != ShadowCastingMode.ShadowsOnly),Is.True,"Render completion must restore full body visibility.");
                remote = true;otherBody.SetLocalOwner(false);seen = 0;
                for(int i=0;i<120 && seen!=15;i++)yield return null;
                Assert.That(seen,Is.EqualTo(15));Assert.That(failure,Is.Null);
                // Re-enable must recache original states instead of retaining hidden viewmodels.
                visibility.enabled=false;visibility.enabled=true;
                RenderPipelineManager.beginCameraRendering -= observe;
                RenderPipelineManager.beginCameraRendering += observe;
                seen=0;
                for(int i=0;i<120 && seen!=15;i++)yield return null;
                Assert.That(seen,Is.EqualTo(15));Assert.That(failure,Is.Null);
            }
            finally
            {
                if(observe != null)RenderPipelineManager.beginCameraRendering -= observe;
                for(int i=0;i<cameras.Length;i++)
                {
                    if(cameras[i])cameras[i].targetTexture=previousTargets[i];
                    if(targets[i]){targets[i].Release();Object.DestroyImmediate(targets[i]);}
                }
                Object.DestroyImmediate(other);Object.DestroyImmediate(observerObject);Object.DestroyImmediate(scopeObject);
            }
        }
        [UnityTest] public IEnumerator VehicleContextKeepsSeatedPoseThroughInputPause()
        {
            yield return null;
            Assert.That(input.SetDrivingContext(true,true),Is.True);motor.enabled=false;stance.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            presenter.Present(.1f);Assert.That(animator.GetBool("Seated"),Is.True);
            input.SetGameplay(false);presenter.Present(.1f);Assert.That(animator.GetBool("Seated"),Is.True);
            Assert.That(animator.speed,Is.Zero);
            input.SetDrivingContext(false,true);motor.enabled=true;stance.enabled=true;player.GetComponent<CharacterController>().enabled=true;
            presenter.Present(.1f);Assert.That(animator.GetBool("Seated"),Is.False);
        }
        [UnityTest] public IEnumerator SurvivalBindingUpdatesTreatmentAnimation()
        {
            var survival = player.AddComponent<PlayerSurvival>();
            survival.Initialize(null, player.GetComponent<PlayerInventory>());
            yield return null;
            Assert.That(animator.GetBool("Treating"), Is.False);

            // Simulate treatment start by injecting a mock item definition via reflection
            var itemDef = ScriptableObject.CreateInstance<LastSignal.Inventory.Data.ItemDefinition>();
            var field = typeof(PlayerSurvival).GetField("treatment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(survival, itemDef);

            presenter.Present(.1f);
            Assert.That(animator.GetBool("Treating"), Is.True);
            Assert.That(presenter.UpperActionBusy, Is.False, "Treatment doesn't use the transient action layer");

            survival.CancelTreatment("Test");
            presenter.Present(.1f);
            Assert.That(animator.GetBool("Treating"), Is.False);
            Object.DestroyImmediate(itemDef);
        }

        [UnityTest] public IEnumerator VehicleIKBindsToSteeringWheel()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "Invalid cockpit steering-wheel binding.");
            var vehicleObj = new GameObject("TestVehicle");
            var wheel = vehicleObj.AddComponent<LastSignal.Vehicles.VehicleSteeringWheelPresenter>();
            var wheelPivot = new GameObject("Pivot").transform;
            wheelPivot.SetParent(vehicleObj.transform);
            wheel.GetType().GetField("pivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(wheel, wheelPivot);

            player.transform.SetParent(vehicleObj.transform);
            yield return null;

            // Simulate entering vehicle context
            input.SetDrivingContext(true, true);
            presenter.Present(.1f);
            Assert.That(animator.GetBool("Seated"), Is.True);

            // Wait for LateUpdate and IK to process
            yield return null;

            // The CharacterSeatPresentation should have found the wheel and constrained to it.
            // We can check if it successfully captured the wheel by looking at its private field.
            var seatRig = presenter.GetComponent<CharacterSeatPresentation>();
            var capturedWheel = seatRig.GetType().GetField("wheel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(seatRig);
            Assert.That(capturedWheel, Is.Not.Null, "Seat presentation must bind to the steering wheel in parent");

            Object.Destroy(vehicleObj);
        }

        [UnityTest] public IEnumerator HolsteredWeaponStateTurnsOffRigIK()
        {
            yield return null; yield return new WaitForSeconds(.8f);
            var combat = player.GetComponent<PlayerCombatController>();
            var rig = presenter.GetComponent<CharacterWeaponRig>();

            Assert.That(rig.WeaponVisual, Is.Not.Null);
            Assert.That(rig.WeaponVisual.activeSelf, Is.True);

            combat.UnequipWeapon();
            yield return null;

            Assert.That(rig.WeaponVisual == null || !rig.WeaponVisual.activeSelf, Is.True, "Visual hidden or destroyed");
            yield return null;
            Assert.That(rig.ActiveProfile, Is.Null, "Rig profile cleared");
        }
    }
}
#endif
