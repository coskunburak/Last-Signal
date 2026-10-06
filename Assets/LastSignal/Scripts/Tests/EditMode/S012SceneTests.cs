using System.Collections.Generic;
using LastSignal.Loot;
using LastSignal.Objectives;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace LastSignal.Tests
{
    public sealed class S012SceneTests
    {
        [SetUp] public void Setup(){EditorSceneManager.OpenScene("Assets/LastSignal/Scenes/Production/IntegratedGraybox.unity");}
        [Test] public void CandidateHasFullAreaAndValidSingleAuthority()
        {
            var ground=GameObject.Find("S012 ground 400 x 400 m").GetComponent<Collider>();
            Assert.That(ground.bounds.size.x,Is.EqualTo(400).Within(.01));Assert.That(ground.bounds.size.z,Is.EqualTo(400).Within(.01));
            Assert.AreEqual(1,Object.FindObjectsByType<SessionFlow>().Length);
            var save=Object.FindAnyObjectByType<SaveSession>();Assert.IsTrue(save.ValidateAuthoring().Success,save.ValidateAuthoring().Message);
            Assert.AreEqual(1,Object.FindObjectsByType<RelayMission>().Length);
        }
        [Test] public void CriticalLootHasOneGuaranteedOwnerAndValidPlacement()
        {
            var ids=new HashSet<string>();int sources=0;
            foreach(var p in Object.FindObjectsByType<LootSpawnPoint>())
            {
                Assert.IsTrue(ids.Add(p.StableId));
                var selected=p.Profile.Select(12345,p.StableId);
                if(selected.Outcome==LootOutcome.Spawned) Assert.IsTrue(LootPopulationService.PlacementValid(p,selected.Item.WorldPrefab,null,out var error),p.StableId+": "+error);
                if(p.StableId=="relay.fuse-source.v1")
                {sources++;for(int seed=0;seed<100;seed++){var fuse=p.Profile.Select(seed,p.StableId);Assert.AreEqual(LootOutcome.Spawned,fuse.Outcome);Assert.AreEqual(RelayProgression.FuseId,fuse.Item.Id.Value);Assert.AreEqual(1,fuse.Quantity);}}
            }
            Assert.AreEqual(1,sources);
        }
        [Test] public void ZombieCanReachBothResourceApproaches()
        {
            var from=GameObject.Find("ZombieSpawn").transform.position;
            foreach(var to in new[]{new Vector3(-160,0,-52),new Vector3(-100,0,100),new Vector3(-280,0,24),new Vector3(-350,0,-139)})
            {
                var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(from,to,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,to.ToString());
            }
        }
    }
}
