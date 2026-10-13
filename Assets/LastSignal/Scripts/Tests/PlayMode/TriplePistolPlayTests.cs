#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Persistence;
using LastSignal.Slice;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LastSignal.Tests
{
    public sealed class TriplePistolPlayTests
    {
        GameObject player;
        PlayerCombatController combat;
        PlayerInputReader input;
        WeaponGripProbe probe;
        InputFixtureIsolation.SceneScope scene;
        InputFixtureIsolation.NativeInputScope native;
        static readonly string[] Ids={PistolVisualVariant.DxtId,PistolVisualVariant.GlockId,PistolVisualVariant.CustomId};
        [SetUp] public void Setup()
        {
            scene=new InputFixtureIsolation.SceneScope();InputFixtureIsolation.DisableLiveActions();
            native=new InputFixtureIsolation.NativeInputScope();Time.timeScale=1;Spawn();
        }
        void Spawn()
        {
            player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab"),new Vector3(500,100,500),Quaternion.identity);
            player.GetComponent<FirstPersonMotor>().enabled=false;
            var inventory=player.GetComponent<PlayerInventory>();if(!inventory)inventory=player.AddComponent<PlayerInventory>();inventory.Initialize(24);
            input=player.GetComponent<PlayerInputReader>();input.SetGameplay(true);combat=player.GetComponent<PlayerCombatController>();
            probe=player.AddComponent<WeaponGripProbe>();probe.World=player.GetComponentInChildren<CharacterWeaponRig>();
        }
        [TearDown] public void Teardown()
        {
            if(player)Object.DestroyImmediate(player);Time.timeScale=1;InputFixtureIsolation.DisableLiveActions();
            try {native?.Dispose();}finally {scene?.Dispose();}
        }
        static IEnumerator Ready(WeaponController weapon)
        {
            float until=Time.realtimeSinceStartup+5;
            while(weapon.RuntimeState.State!=WeaponState.Ready){Assert.That(Time.realtimeSinceStartup,Is.LessThan(until));yield return null;}
        }
        IEnumerator Sample()
        {int count=probe.Samples;for(int n=0;n<30&&probe.Samples==count;n++)yield return null;Assert.That(probe.Samples,Is.GreaterThan(count));}
        IEnumerator Select(string id)
        {
            yield return null;Assert.That(combat.SelectFirearm("weapon.pistol"),Is.True);
            var pistol=combat.ActiveWeapon;Assert.That(pistol.PistolVariants.Select(id),Is.True);
            probe.Fps=pistol.GetComponent<FirstPersonWeaponGrip>();yield return Ready(pistol);yield return new WaitForSeconds(.25f);yield return Sample();
        }
        void Contacts()
        {
            Assert.That(probe.Active,Is.True);Assert.That(probe.FpsError,Is.LessThan(.02f));
            Assert.That(probe.WorldError,Is.LessThan(.02f));Assert.That(probe.Residual,Is.LessThan(.02f),"Do not hide an unreachable target by moving the gun.");
            Assert.That(probe.World.ActiveProfile,Is.SameAs(combat.ActiveWeapon.PistolVariants.Selected.Pose));
            Assert.That(probe.World.SupportHandWeight,Is.Zero);
        }
        [UnityTest] public IEnumerator DxtEquipAdsFireReloadAndLifecycle() => Exercise(PistolVisualVariant.DxtId);
        [UnityTest] public IEnumerator GlockEquipAdsFireReloadAndLifecycle() => Exercise(PistolVisualVariant.GlockId);
        [UnityTest] public IEnumerator CustomEquipAdsFireReloadAndLifecycle() => Exercise(PistolVisualVariant.CustomId);
        IEnumerator Exercise(string id)
        {
            yield return Select(id);Contacts();var pistol=combat.ActiveWeapon;var variants=pistol.PistolVariants;var state=pistol.RuntimeState;
            Assert.That(variants.SelectedId,Is.EqualTo(id));Assert.That(probe.Fps.Profile,Is.SameAs(variants.Selected.Pose));
            Assert.That(probe.Fps.Visual.GetComponentsInChildren<Renderer>(),Is.Not.Empty);
            int rifle=combat.Rifle.RuntimeState.CurrentMagazine,shots=0;int cues=pistol.GetComponent<WeaponAudioPresenter>().PresentedCueCount;
            pistol.ShotFired+=_=>shots++;
            InputFixtureIsolation.QueueButton(Mouse.current.rightButton,true);
            yield return new WaitForSeconds(.45f);yield return Sample();Contacts();
            var sight=player.GetComponent<FirstPersonLook>().View.transform.InverseTransformPoint(probe.Sight);
            Assert.That(new Vector2(sight.x,sight.y).magnitude,Is.LessThan(.01f));
            int rounds=state.CurrentMagazine;pistol.OnFirePressed();pistol.OnFireReleased();
            Assert.That(shots,Is.EqualTo(1));Assert.That(state.CurrentMagazine,Is.EqualTo(rounds-1));
            Assert.That(combat.Rifle.RuntimeState.CurrentMagazine,Is.EqualTo(rifle));
            Assert.That(pistol.GetComponent<WeaponAudioPresenter>().PresentedCueCount-cues,Is.EqualTo(1));
            InputFixtureIsolation.QueueButton(Mouse.current.rightButton,false);
            var inventory=player.GetComponent<PlayerInventory>();inventory.TryAdd(pistol.Definition.Ammunition,20);
            int total=state.TotalAmmo;
            pistol.OnReloadRequested();yield return new WaitForSeconds(.25f);yield return Sample();
            Assert.That(probe.Reload,Is.GreaterThan(0));
            combat.SelectFirearm("weapon.rifle");yield return null;
            Assert.That(probe.Fps.ReloadAssist,Is.Zero);Assert.That(state.TotalAmmo,Is.EqualTo(total));
            combat.SelectFirearm("weapon.pistol");yield return Ready(pistol);
            pistol.OnReloadRequested();yield return Ready(pistol);yield return Sample();
            Assert.That(state.TotalAmmo,Is.EqualTo(total));Assert.That(probe.Reload,Is.Zero);Contacts();
            player.SetActive(false);yield return null;Assert.That(probe.Fps.HasPose,Is.False);
            player.SetActive(true);input.SetGameplay(true);pistol.RequestEquip();yield return Ready(pistol);yield return Sample();Contacts();
            combat.SetVehicleHolstered(true);yield return Sample();Assert.That(probe.Fps.HasPose,Is.False);
            combat.SetVehicleHolstered(false);yield return Ready(pistol);yield return Sample();Contacts();
            player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=10000,Category=DamageCategory.Melee});
            yield return Sample();Assert.That(probe.Fps.HasPose,Is.False);Assert.That(probe.World.WeaponVisual.activeSelf,Is.False);
            Object.Destroy(player);yield return null;Spawn();yield return null;
            Assert.That(combat.FindFirearm("weapon.pistol").PistolVariants.SelectedId,Is.EqualTo(PistolVisualVariant.DxtId));
        }
        [UnityTest] public IEnumerator RapidVariantsCancelReloadWithoutReplacingAmmoOwnerOrLateTransfer()
        {
            yield return Select(PistolVisualVariant.DxtId);var pistol=combat.ActiveWeapon;var state=pistol.RuntimeState;
            player.GetComponent<PlayerInventory>().TryAdd(pistol.Definition.Ammunition,20);
            int total=state.TotalAmmo;int magazine=state.CurrentMagazine;
            for(int i=0;i<9;i++)
            {
                yield return Ready(pistol);pistol.OnReloadRequested();yield return new WaitForSeconds(.1f);
                Assert.That(pistol.PistolVariants.Select(Ids[(i+1)%3]),Is.True);
                Assert.That(pistol.RuntimeState,Is.SameAs(state));Assert.That(state.TotalAmmo,Is.EqualTo(total));
                Assert.That(state.CurrentMagazine,Is.EqualTo(magazine));
                yield return null;
                Assert.That(pistol.GetComponentsInChildren<PistolMechanics>().Length,Is.EqualTo(1));
            }
            yield return new WaitForSeconds(3);Assert.That(state.CurrentMagazine,Is.EqualTo(magazine));
            yield return Ready(pistol);pistol.OnReloadRequested();
            yield return new WaitForSeconds(pistol.Definition.TacticalReloadSeconds*pistol.Definition.ReloadCommitNormalized+.1f);
            int committed=state.CurrentMagazine;
            Assert.That(pistol.PistolVariants.Select(PistolVisualVariant.GlockId),Is.True);
            yield return new WaitForSeconds(3);
            Assert.That(state.TotalAmmo,Is.EqualTo(total));Assert.That(state.CurrentMagazine,Is.EqualTo(committed));
            Assert.That(pistol.PistolVariants.Select("unknown"),Is.False);Assert.That(state.TotalAmmo,Is.EqualTo(total));
        }
        [UnityTest] public IEnumerator EveryVariantEmptyReloadCrouchAndPauseKeepsItsOwnVisual()
        {
            foreach(var id in Ids)
            {
                yield return Select(id);var pistol=combat.ActiveWeapon;
                pistol.Initialize(player.GetComponent<FirstPersonLook>().View.transform,player,0);pistol.RequestEquip();yield return Ready(pistol);
                player.GetComponent<PlayerInventory>().TryAdd(pistol.Definition.Ammunition,20);
                int shots=0;pistol.ShotFired+=_=>shots++;
                pistol.OnFirePressed();pistol.OnFireReleased();Assert.That(shots,Is.Zero);
                int total=pistol.RuntimeState.TotalAmmo;
                pistol.OnReloadRequested();Assert.That(pistol.RuntimeState.IsEmptyReload,Is.True);
                yield return new WaitForSeconds(.4f);yield return Sample();Assert.That(probe.Reload,Is.GreaterThan(0));
                input.SetGameplay(false);float timer=pistol.RuntimeState.StateTimer;Time.timeScale=0;
                yield return Sample();var magazine=probe.MagazineLocalPosition;
                yield return Sample();Assert.That(probe.MagazineLocalPosition,Is.EqualTo(magazine));Assert.That(pistol.RuntimeState.StateTimer,Is.EqualTo(timer));
                Time.timeScale=1;input.SetGameplay(true);yield return Ready(pistol);yield return Sample();
                Assert.That(pistol.RuntimeState.TotalAmmo,Is.EqualTo(total));Assert.That(probe.Reload,Is.Zero);
                var stance=player.GetComponent<PlayerStance>();Assert.That(stance.TrySetCrouching(true),Is.True);
                yield return new WaitForSeconds(.25f);yield return Sample();Contacts();stance.TrySetCrouching(false);
                combat.SelectFirearm("weapon.rifle");yield return Ready(combat.ActiveWeapon);
                Assert.That(combat.ActiveWeapon.PistolVariants,Is.Null);
            }
        }
        [UnityTest] public IEnumerator EachVariantSprintAndSlideKeepContactsThenReleaseToRifle()
        {
            var floor=new GameObject("Triple pistol movement floor");floor.transform.position=new Vector3(500,99.75f,500);
            floor.AddComponent<BoxCollider>().size=new Vector3(80,.5f,80);
            try
            {
                player.transform.position=new Vector3(500,100.04f,500);Physics.SyncTransforms();
                var motor=player.GetComponent<FirstPersonMotor>();motor.enabled=true;
                foreach(var id in Ids)
                {
                    yield return Select(id);
                    InputSystem.QueueStateEvent(Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.W,Key.LeftShift));
                    yield return new WaitForSeconds(.4f);yield return Sample();
                    Assert.That(motor.IsSprinting,Is.True);Assert.That(probe.FpsError,Is.LessThan(.02f));
                    Assert.That(motor.TryStartSlide(),Is.True);yield return new WaitForSeconds(.15f);yield return Sample();
                    Assert.That(probe.FpsError,Is.LessThan(.02f));Assert.That(probe.WorldError,Is.LessThan(.02f));
                    Assert.That(probe.World.SupportHandWeight,Is.Zero);
                    InputSystem.QueueStateEvent(Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                    yield return new WaitForSeconds(1.2f);
                    player.GetComponent<PlayerStance>().TrySetCrouching(false);
                    combat.SelectFirearm("weapon.rifle");yield return Ready(combat.ActiveWeapon);
                    Assert.That(combat.ActiveWeapon.PresentationProfile,Is.Null);
                }
            }
            finally {Object.DestroyImmediate(floor);}
        }
        [UnityTest] public IEnumerator PlayerBindingCyclesRifleAndAllThreeWithoutNewMagazines()
        {
            yield return null;var pistol=combat.FindFirearm("weapon.pistol");var state=pistol.RuntimeState;
            int total=state.TotalAmmo;
            foreach(var id in Ids)
            {
                InputFixtureIsolation.QueueButton(Keyboard.current.digit1Key,true);yield return null;
                InputFixtureIsolation.QueueButton(Keyboard.current.digit1Key,false);yield return new WaitForSeconds(1.2f);
                Assert.That(combat.ActiveWeapon,Is.SameAs(pistol));Assert.That(pistol.PistolVariants.SelectedId,Is.EqualTo(id));
                Assert.That(pistol.RuntimeState,Is.SameAs(state));Assert.That(state.TotalAmmo,Is.EqualTo(total));
            }
            InputFixtureIsolation.QueueButton(Keyboard.current.digit1Key,true);yield return null;
            InputFixtureIsolation.QueueButton(Keyboard.current.digit1Key,false);yield return new WaitForSeconds(1.2f);
            Assert.That(combat.ActiveWeapon,Is.SameAs(combat.Rifle));
        }
        [UnityTest] public IEnumerator DisabledPlayerCannotApplyAnOldQueuedVariant()
        {
            yield return Select(PistolVisualVariant.DxtId);var pistol=combat.ActiveWeapon;
            Assert.That(pistol.PistolVariants.SelectNext(),Is.True);
            Assert.That(pistol.RuntimeState.State,Is.EqualTo(WeaponState.Unequipping));
            player.SetActive(false);yield return null;player.SetActive(true);input.SetGameplay(true);
            pistol.RequestEquip();yield return Ready(pistol);yield return new WaitForSeconds(.6f);
            Assert.That(pistol.PistolVariants.SelectedId,Is.EqualTo(PistolVisualVariant.DxtId));
        }
        [UnityTest] public IEnumerator EachVariantRoundTripsActualProductionSession()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Production/S013Cabin.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var driver=new GameObject("Triple pistol save fixture").AddComponent<S012Acceptance>();driver.Bind();
            var flow=driver.flow;var saves=flow.GetComponent<SaveSession>();
            string directory=Path.Combine(Application.temporaryCachePath,"triple-pistol-"+System.Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"variants.json");
            try
            {
                yield return driver.relay.ClearThreat();
                foreach(var id in Ids)
                {
                    flow.Resume();flow.Player.GetComponent<PlayerInputReader>().SetGameplay(true);
                    var owner=flow.Player.GetComponent<PlayerCombatController>();owner.SelectFirearm("weapon.pistol");
                    var pistol=owner.Firearm;Assert.That(pistol.PistolVariants.Select(id),Is.True);yield return Ready(pistol);
                    int total=pistol.RuntimeState.TotalAmmo;int magazine=pistol.RuntimeState.CurrentMagazine;
                    Assert.That(saves.Save(path).Success,Is.True,saves.LastResult.Message);
                    flow.ReturnToMenu();yield return null;yield return saves.Load(path);
                    Assert.That(saves.LastResult.Success,Is.True,saves.LastResult.Message);
                    var restored=flow.Player.GetComponent<PlayerCombatController>().Firearm;
                    Assert.That(restored.PistolVariants.SelectedId,Is.EqualTo(id));Assert.That(restored.RuntimeState.CurrentMagazine,Is.EqualTo(magazine));
                    Assert.That(restored.RuntimeState.TotalAmmo,Is.EqualTo(total));
                }
            }
            finally {if(flow)flow.ReturnToMenu();if(driver)Object.DestroyImmediate(driver.gameObject);if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
#endif
