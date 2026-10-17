#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed partial class CharacterIntegrationPlayTests
    {
        [UnityTest] public IEnumerator ArberryFirstPersonUsesOwnSkinAndStableSharedMaterials()
        {
            yield return null;
            var combat=player.GetComponent<PlayerCombatController>();
            foreach(var weapon in combat.Firearms.Select(w=>w.gameObject).Concat(combat.MeleeWeapons.Select(w=>w.gameObject)))
            {
                var grip=weapon.GetComponent<FirstPersonWeaponGrip>();Assert.That(grip,Is.Not.Null);
                Assert.That(grip.RightHand.name,Is.EqualTo("hand_r"));Assert.That(grip.LeftHand.name,Is.EqualTo("hand_l"));
                var skins=weapon.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.That(skins,Has.Length.EqualTo(3));
                foreach(var skin in skins)Assert.That(AssetDatabase.GetAssetPath(skin.sharedMaterials[0]),Does.StartWith("Assets/LastSignal/Art/CharacterIntegration/Arberry/Materials/"));
                Assert.That(weapon.GetComponentsInChildren<Animator>(true).All(a=>!a.enabled),Is.True,"One procedural arm owner; legacy animation cannot overwrite it.");
            }
            Assert.That(player.GetComponentsInChildren<PlayerLocomotionPresenter>(true),Has.Length.EqualTo(1));
            Assert.That(player.GetComponentsInChildren<FirstPersonMotor>(true),Has.Length.EqualTo(1));
        }
        [UnityTest] public IEnumerator ArberryVariantResolutionDoesNotReplaceAmmoOrHealthAuthority()
        {
            yield return null;
            var combat=player.GetComponent<PlayerCombatController>();var rig=presenter.GetComponent<CharacterWeaponRig>();
            Assert.That(combat.SelectFirearm("weapon.pistol"),Is.True);
            var pistol=combat.ActiveWeapon;var state=pistol.RuntimeState;var health=player.GetComponent<PlayerHealth>();var controllers=player.GetComponentsInChildren<WeaponController>(true);
            int rounds=state.CurrentMagazine;
            foreach(string id in new[]{PistolVisualVariant.DxtId,PistolVisualVariant.GlockId,PistolVisualVariant.CustomId}){
                Assert.That(pistol.PistolVariants.Select(id),Is.True);
                rig.SendMessage("Update"); // Deterministic profile selection, no elapsed-time assumption.
                var source=pistol.PistolVariants.Selected.Pose;
                Assert.That(rig.ActiveProfile,Is.SameAs(rig.ResolvePresentation(source)));
                Assert.That(rig.ActiveProfile.WorldPistolFingers.Rig,Does.StartWith("Arberry"));
                Assert.That(rig.ActiveProfile.WorldPrefab,Is.SameAs(source.WorldPrefab));
                Assert.That(pistol.RuntimeState,Is.SameAs(state));Assert.That(state.CurrentMagazine,Is.EqualTo(rounds));
                Assert.That(player.GetComponent<PlayerHealth>(),Is.SameAs(health));
                Assert.That(rig.transform.Cast<Transform>().Count(t=>t.name.StartsWith("WorldWeapon_")&&t.gameObject.activeSelf),Is.LessThanOrEqualTo(1));
            }
            CollectionAssert.AreEquivalent(controllers,player.GetComponentsInChildren<WeaponController>(true));
        }
        [UnityTest] public IEnumerator ArberryOwnAvatarAndNeutralGripSurviveReenableWithoutMovingController()
        {
            yield return null;
            Assert.That(animator.avatar.isHuman&&animator.avatar.isValid,Is.True);Assert.That(animator.applyRootMotion,Is.False);
            var rig=presenter.GetComponent<CharacterWeaponRig>();var position=player.transform.position;
            input.SetGameplay(false);rig.enabled=false;rig.enabled=true;
            presenter.Present(0);animator.Update(0);
            Assert.That(player.transform.position,Is.EqualTo(position));
            Assert.That(animator.GetBoneTransform(HumanBodyBones.RightHand).name,Is.EqualTo("hand_r"));
            Assert.That(presenter.GetComponentsInChildren<SkinnedMeshRenderer>(true),Has.Length.EqualTo(11));
        }
        static void SampleArberryFps(WeaponController weapon)
        {
            var grip=weapon.GetComponent<FirstPersonWeaponGrip>();
            grip.SendMessage("Update");weapon.GetComponent<WeaponViewPresenter>().SendMessage("LateUpdate");grip.SendMessage("LateUpdate");
        }
        [UnityTest] public IEnumerator ArberryRifleBothContactsAdsAndReloadCancellationPreserveAmmo()
        {
            yield return null;
            var combat=player.GetComponent<PlayerCombatController>();var weapon=combat.Rifle;var state=weapon.RuntimeState;
            if(state.State==WeaponState.Equipping){state.Tick(weapon.Definition.EquipSeconds);Assert.That(state.TryCompleteEquip(),Is.True);}
            var grip=weapon.GetComponent<FirstPersonWeaponGrip>();Assert.That(grip.Profile.TwoHandedFirearm,Is.True);
            int total=state.TotalAmmo;
            foreach(float aim in new[]{0f,1f}){
                state.AimAmount=aim;SampleArberryFps(weapon);
                Assert.That(grip.ContactError,Is.LessThan(.02f));Assert.That(grip.SecondaryContactError,Is.LessThan(.02f));
                Assert.That(grip.ReachResidual,Is.LessThan(.02f));Assert.That(grip.SecondaryReachResidual,Is.LessThan(.02f));
                if(aim==1){var point=player.GetComponent<FirstPersonLook>().View.transform.InverseTransformPoint(weapon.AimReference.position);Assert.That(new Vector2(point.x,point.y).magnitude,Is.LessThan(.01f));}
                Assert.That(state.TotalAmmo,Is.EqualTo(total));
            }
            state.AimAmount=0;Assert.That(state.TryConsumeShot(),Is.True);
            player.GetComponent<LastSignal.Inventory.PlayerInventory>().TryAdd(weapon.Definition.Ammunition,30);
            var rest=grip.Sockets.Magazine.localPosition;weapon.OnReloadRequested();Assert.That(state.State,Is.EqualTo(WeaponState.Reloading));
            state.Tick(weapon.Definition.TacticalReloadSeconds*.35f);int rounds=state.CurrentMagazine;total=state.TotalAmmo;
            SampleArberryFps(weapon);Assert.That(grip.ReloadAssist,Is.GreaterThan(0));Assert.That(Vector3.Distance(grip.Sockets.Magazine.localPosition,rest),Is.GreaterThan(.001f));
            Assert.That(state.CurrentMagazine,Is.EqualTo(rounds));Assert.That(state.TotalAmmo,Is.EqualTo(total),"Presentation must never commit a reload transaction.");
            input.SetGameplay(false);SampleArberryFps(weapon);var palm=grip.PalmPosition;var root=grip.AnatomicalRoot.localPosition;
            for(int i=0;i<4;i++){SampleArberryFps(weapon);Assert.That(Vector3.Distance(palm,grip.PalmPosition),Is.LessThan(.001f));Assert.That(Vector3.Distance(root,grip.AnatomicalRoot.localPosition),Is.LessThan(.001f));}
            input.SetGameplay(true);weapon.RequestUnequip();SampleArberryFps(weapon);
            Assert.That(grip.ReloadAssist,Is.Zero);Assert.That(grip.Sockets.Magazine.localPosition,Is.EqualTo(rest));Assert.That(state.TotalAmmo,Is.EqualTo(total));
        }
        [UnityTest] public IEnumerator ArberryCrowbarCancelRestoresContactsWithoutSecondDamageOwner()
        {
            yield return null;
            var combat=player.GetComponent<PlayerCombatController>();Assert.That(combat.SelectMeleeWeapon(MeleeWeaponDefinition.CrowbarId),Is.True);
            var melee=combat.Melee;var grip=melee.GetComponent<FirstPersonWeaponGrip>();Assert.That(grip.ValidAuthoring,Is.True);
            var origin=(Transform)new SerializedObject(melee).FindProperty("meleeOrigin").objectReferenceValue;var position=origin.localPosition;
            int windows=0;melee.Simulation.ActiveWindow+=()=>windows++;
            Assert.That(melee.TryAttack(),Is.True);melee.Cancel();
            grip.SendMessage("Update");grip.SendMessage("LateUpdate");
            Assert.That(melee.Simulation.State,Is.EqualTo(MeleeState.Ready));Assert.That(windows,Is.Zero);Assert.That(origin.localPosition,Is.EqualTo(position));
            Assert.That(grip.ContactError,Is.LessThan(.02f));Assert.That(grip.ReachResidual,Is.LessThan(.02f));
            Assert.That(melee.GetComponents<MeleeWeaponController>(),Has.Length.EqualTo(1));
        }
    }
}
#endif
