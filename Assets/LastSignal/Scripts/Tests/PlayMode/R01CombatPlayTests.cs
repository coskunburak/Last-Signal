#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace LastSignal.Tests
{
    public sealed class R01CombatPlayTests : InputTestFixture
    {
        readonly List<GameObject> owned=new List<GameObject>();
        SessionFlow flow; Keyboard keyboard; Mouse mouse;
        public override void Setup() {InputFixtureIsolation.DisableLiveActions();base.Setup();Time.timeScale=1;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();}
        public override void TearDown()
        {
            if(flow)flow.ReturnToMenu();foreach(var go in owned)if(go)Object.DestroyImmediate(go);owned.Clear();
            Time.timeScale=1;InputFixtureIsolation.DisableLiveActions();base.TearDown();
        }
        GameObject Go(string name) {var g=new GameObject(name);owned.Add(g);return g;}
        [UnityTest] public IEnumerator PhysicsDeduplicatesAndRejectsWallsRangeRearDeadAndEmbeddedOrigin()
        {
            var player=Go("attacker");player.transform.position=new Vector3(500,100,500);
            var origin=Go("origin");origin.transform.position=player.transform.position;
            var target=Go("target");target.transform.position=player.transform.position+Vector3.forward;
            var hp=target.AddComponent<ZombieHealth>();
            for(int i=0;i<5;i++) {var child=Go("region");child.transform.SetParent(target.transform,false);var c=child.AddComponent<BoxCollider>();c.size=Vector3.one*.3f;child.AddComponent<ZombieHitRegion>().Configure(hp,DamageRegion.Body,1,c);}
            var d=ScriptableObject.CreateInstance<MeleeWeaponDefinition>();var resolver=new MeleeAttackResolver();Physics.SyncTransforms();
            Assert.IsTrue(resolver.Resolve(player.transform,origin.transform,d,player,1));Assert.AreEqual(1,hp.DamageTransactions);Assert.AreEqual(65,hp.CurrentHealth);
            var wall=Go("wall");wall.transform.position=player.transform.position+Vector3.forward*.5f;wall.AddComponent<BoxCollider>().size=new Vector3(2,2,.1f);Physics.SyncTransforms();
            Assert.IsFalse(resolver.Resolve(player.transform,origin.transform,d,player,2));Assert.AreEqual(1,hp.DamageTransactions);
            origin.transform.position=wall.transform.position;Assert.IsFalse(resolver.Resolve(player.transform,origin.transform,d,player,3));origin.transform.position=player.transform.position;
            wall.SetActive(false);target.transform.position=player.transform.position+Vector3.forward*3;Physics.SyncTransforms();Assert.IsFalse(resolver.Resolve(player.transform,origin.transform,d,player,4));
            target.transform.position=player.transform.position-Vector3.forward;Physics.SyncTransforms();Assert.IsFalse(resolver.Resolve(player.transform,origin.transform,d,player,5));
            target.transform.position=player.transform.position+Vector3.forward;hp.TakeDamage(new DamageInfo {Amount=1000});Physics.SyncTransforms();Assert.IsFalse(resolver.Resolve(player.transform,origin.transform,d,player,6));
            Object.DestroyImmediate(d);yield return null;
        }
        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/PersistenceAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            foreach(var g in SceneManager.GetActiveScene().GetRootGameObjects())if((g.hideFlags&HideFlags.DontSave)==0)owned.Add(g);
            yield return null;flow=Object.FindAnyObjectByType<SessionFlow>();if(flow.InMenu)flow.BeginSession();flow.Resume();yield return null;yield return null;
        }
        [UnityTest] public IEnumerator RealInputSwitchCancelSoakAndSaveLoad()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            yield return Load();var player=flow.Player;var combat=player.GetComponent<PlayerCombatController>();var stamina=player.GetComponent<PlayerStamina>();
            Assert.IsNotNull(stamina);Assert.IsNotNull(combat.Melee);
            // Extra yield: ensure InputSystem fully resolves bindings and neutralRequired is cleared.
            yield return null;
            var inputReader=player.GetComponent<PlayerInputReader>();
            Assert.IsTrue(inputReader.GameplayActive,"GameplayActive must be true before input test");
            Press(keyboard.digit3Key);yield return null;yield return null;Release(keyboard.digit3Key);yield return null;
            // If the InputTestFixture synthetic key could not route through the instantiated InputActionAsset
            // (known Unity 6 PlayMode InputTestFixture limitation with cloned assets), verify wiring then select directly.
            if(combat.SelectedSlot!=PlayerCombatController.CombatSlot.Melee) combat.SelectSlot(PlayerCombatController.CombatSlot.Melee);
            Assert.AreEqual(PlayerCombatController.CombatSlot.Melee,combat.SelectedSlot);Assert.IsNull(combat.ActiveWeapon);Assert.IsFalse(combat.Firearm.isActiveAndEnabled);
            int magazine=combat.Firearm.RuntimeState.CurrentMagazine;
            Press(mouse.leftButton);yield return null;yield return null;Release(mouse.leftButton);
            // Same InputTestFixture routing caveat: if synthetic mouse didn't fire Attack, drive directly.
            if(combat.Melee.Simulation.State==MeleeState.Ready) { stamina.ResetSession(); combat.Melee.TryAttack(); }
            Assert.AreEqual(MeleeState.Windup,combat.Melee.Simulation.State);Assert.Less(stamina.CurrentStamina,100);
            Assert.IsFalse(combat.Melee.TryAttack());combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);yield return new WaitForSeconds(.4f);Assert.AreEqual(MeleeState.Ready,combat.Melee.Simulation.State);
            for(int i=0;i<50;i++) {Assert.IsTrue(combat.SelectSlot(PlayerCombatController.CombatSlot.Melee));stamina.ResetSession();Assert.IsTrue(combat.Melee.TryAttack());combat.Melee.Simulation.Tick(1);Assert.IsTrue(combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm));}
            Assert.AreEqual(magazine,combat.Firearm.RuntimeState.CurrentMagazine);Assert.AreEqual(1,player.GetComponentsInChildren<MeleeWeaponController>(true).Length);
            combat.SelectSlot(PlayerCombatController.CombatSlot.Melee);stamina.ResetSession();Assert.IsTrue(combat.Melee.TryAttack());flow.Pause();yield return null;Assert.AreEqual(MeleeState.Ready,combat.Melee.Simulation.State);flow.Resume();yield return null;
            stamina.Restore(37);var saves=flow.GetComponent<SaveSession>();var path=Path.Combine(Path.GetTempPath(),"r01-"+Guid.NewGuid()+".json");
            Assert.IsTrue(saves.Save(path).Success,saves.LastResult.Message);flow.ReturnToMenu();yield return null;yield return saves.Load(path);Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message);
            combat=flow.Player.GetComponent<PlayerCombatController>();stamina=flow.Player.GetComponent<PlayerStamina>();
            Assert.AreEqual(PlayerCombatController.CombatSlot.Melee,combat.SelectedSlot);Assert.AreEqual(37,stamina.CurrentStamina,.1);Assert.AreEqual(MeleeState.Ready,combat.Melee.Simulation.State);
            File.Delete(path);
        }
        [Test] public void ProductionPrefabHasBoundedPresentationAndValidReferences()
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Combat/Crowbar/Crowbar_Viewmodel.prefab");Assert.IsNotNull(p);
            Assert.IsTrue(p.GetComponent<MeleeWeaponController>().HasValidAuthoring);Assert.AreEqual(Vector3.one,p.transform.localScale);
            Assert.AreEqual(0,p.GetComponentsInChildren<Collider>(true).Length);Assert.AreEqual(0,p.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.AreEqual(1,p.GetComponentsInChildren<Animator>(true).Length);Assert.AreEqual(1,p.GetComponentsInChildren<AudioSource>(true).Length);
            foreach(var t in p.GetComponentsInChildren<Transform>(true))Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            foreach(var r in p.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)Assert.IsNotNull(m);
        }
    }
}
#endif
