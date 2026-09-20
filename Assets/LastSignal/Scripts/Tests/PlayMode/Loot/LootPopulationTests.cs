#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace LastSignal.Tests
{
    public class LootPopulationTests
    {
        readonly List<Object> owned=new List<Object>();
        LootPopulationService service;
        LootProfile profile;
        ItemDefinition item;
        GameObject prefab,player;
        Scene scene;
        static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        GameObject Go(string name,Vector3 position)
        {var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=position;owned.Add(go);return go;}
        [SetUp] public void Setup()
        {
            scene=SceneManager.CreateScene("LootIsolatedTest");SceneManager.SetActiveScene(scene);
            var ground=Go("Ground",Vector3.down*.5f);ground.AddComponent<BoxCollider>().size=new Vector3(300,1,300);
            prefab=Go("Prefab",new Vector3(120,10,120));prefab.AddComponent<BoxCollider>().size=Vector3.one*.2f;prefab.AddComponent<WorldItem>();
            item=ScriptableObject.CreateInstance<ItemDefinition>();owned.Add(item);Set(item,"stableId",new StableItemId("loot.test"));Set(item,"maxStack",5);Set(item,"worldPrefab",prefab);
            profile=ScriptableObject.CreateInstance<LootProfile>();owned.Add(profile);Set(profile,"emptyBasisPoints",0);Set(profile,"entries",new[]{new LootEntry(item,1,8,8)});
            service=Go("Service",Vector3.zero).AddComponent<LootPopulationService>();
            player=Go("Player",new Vector3(20,0,20));player.AddComponent<PlayerInventory>();
            Physics.SyncTransforms();
        }
        LootSpawnPoint Point(string id,Vector3 position)
        {var p=Go(id,position).AddComponent<LootSpawnPoint>();Set(p,"stableId",id);Set(p,"profile",profile);return p;}
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(service)service.End();yield return null;
            foreach(var o in owned)if(o)Object.DestroyImmediate(o);owned.Clear();
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale=1;
        }
        WorldItem Generated()
        {foreach(var r in scene.GetRootGameObjects())if(r.name=="Session Loot")return r.GetComponentInChildren<WorldItem>();return null;}
        [UnityTest] public IEnumerator ExactlyOncePartialFullDropAndNewCycle()
        {
            Point("a",Vector3.up*.1f);service.Begin(player,123);
            Assert.AreEqual(1,service.GeneratedCount);var w=Generated();Assert.NotNull(w);
            var inv=player.GetComponent<PlayerInventory>();inv.Initialize(1);inv.ConfigureDrop(player.transform);
            Assert.IsTrue(((IInteractable)w).TryInteract());Assert.AreEqual(5,inv.GetTotalQuantity(item));Assert.AreEqual(3,w.Quantity);
            Assert.IsFalse(w.TryInteract());Assert.AreEqual(3,w.Quantity);
            Assert.IsTrue(inv.TryDrop(0,5));Assert.AreEqual(0,inv.GetTotalQuantity(item));
            Assert.IsTrue(w.TryInteract());Assert.AreEqual(3,inv.GetTotalQuantity(item));
            yield return null;Assert.IsTrue(w==null);
            service.Begin(player,456);Assert.AreEqual(1,service.GeneratedCount);Assert.IsNull(Generated());Assert.AreEqual(123,service.Seed);
            service.End();yield return null;
            foreach(var r in scene.GetRootGameObjects()) foreach(var wi in r.GetComponentsInChildren<WorldItem>(true)) Assert.AreSame(prefab,wi.gameObject);
            Assert.AreEqual(0,service.Results.Count);
            service.Begin(player,123);Assert.AreEqual(8,Generated().Quantity);Assert.AreEqual(1,service.GeneratedCount);
        }
        [UnityTest] public IEnumerator EmptyRemainsResolved()
        {Set(profile,"emptyBasisPoints",10000);Point("a",Vector3.up*.1f);service.Begin(player,1);service.Begin(player,2);Assert.AreEqual(1,service.Results.Count);Assert.AreEqual(LootOutcome.Empty,service.Results[0].Selection.Outcome);Assert.AreEqual(0,service.GeneratedCount);yield return null;}
        [UnityTest] public IEnumerator InvalidPointDoesNotPreventValidPoint()
        {
            var bad=Point("bad",new Vector3(2,.1f,0));Set(bad,"profile",null);Point("good",Vector3.up*.1f);
            LogAssert.Expect(LogType.Warning,"S006 loot 'bad': Missing loot profile.");service.Begin(player,1);Assert.AreEqual(1,service.GeneratedCount);yield return null;
        }
        [UnityTest] public IEnumerator DuplicateIdsRejectAllCopies()
        {
            Point("duplicate",Vector3.up*.1f);Point("duplicate",new Vector3(1,.1f,0));
            LogAssert.Expect(LogType.Warning,"S006 loot 'duplicate': Duplicate point ID; all matching points rejected.");LogAssert.Expect(LogType.Warning,"S006 loot 'duplicate': Duplicate point ID; all matching points rejected.");
            service.Begin(player,1);Assert.AreEqual(0,service.GeneratedCount);yield return null;
        }
        [UnityTest] public IEnumerator OverlapResolvesInStableIdOrder()
        {Point("b",Vector3.up*.1f);Point("a",Vector3.up*.1f);LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("S006 loot 'b':"));service.Begin(player,1);Assert.AreEqual(1,service.GeneratedCount);Assert.AreEqual("a",service.Results[0].PointId);yield return null;}
        [UnityTest] public IEnumerator UnsupportedPlacementFailsWithoutRetry()
        {
            Point("floating",new Vector3(0,2,0));Point("wall",new Vector3(2,.1f,0));Go("Wall",new Vector3(2,.5f,0)).AddComponent<BoxCollider>();
            LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("S006 loot 'floating':"));LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("S006 loot 'wall':"));service.Begin(player,1);Assert.AreEqual(0,service.GeneratedCount);Assert.AreEqual(2,service.Results.Count);yield return null;
        }
        [UnityTest] public IEnumerator PlayerSpawnExcluded()
        {
            player.transform.position=Vector3.zero;var c=player.AddComponent<CharacterController>();c.center=Vector3.up;c.height=2;
            Point("player",Vector3.up*.1f);LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("S006 loot 'player':"));service.Begin(player,1);Assert.AreEqual(0,service.GeneratedCount);yield return null;
        }
        [UnityTest] public IEnumerator ForeignSceneNotPopulated()
        {
            var other=SceneManager.CreateScene("UnrelatedLoot");var point=Point("foreign",new Vector3(3,.1f,0));SceneManager.MoveGameObjectToScene(point.gameObject,other);
            Point("local",Vector3.up*.1f);SceneManager.SetActiveScene(other);service.Begin(player,1);
            Assert.AreEqual(1,service.Results.Count);Assert.AreEqual(scene,Generated().gameObject.scene);
            yield return SceneManager.UnloadSceneAsync(other);SceneManager.SetActiveScene(scene);
        }
        [UnityTest] public IEnumerator TenCyclesReproduceAndClean()
        {
            Set(profile,"emptyBasisPoints",3500);for(int i=0;i<25;i++)Point("point-"+i,new Vector3(i*.5f,.1f,0));
            string expected=null;
            for(int cycle=0;cycle<10;cycle++)
            {
                service.Begin(player,12345);string signature="";foreach(var r in service.Results)signature+=r.PointId+":"+r.Selection.Outcome+":"+r.Selection.Quantity+";";
                if(expected==null)expected=signature;else Assert.AreEqual(expected,signature);
                var w=Generated();if(w)w.TryInteract();service.End();yield return null;Assert.IsNull(Generated());Assert.AreEqual(0,service.Results.Count);
            }
        }
    }
}
#endif
