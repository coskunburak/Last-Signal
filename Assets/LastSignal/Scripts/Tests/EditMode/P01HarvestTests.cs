using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LastSignal.Gathering;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class P01HarvestTests
    {
        static GatheringSnapshot Initial(int health=100,int damage=25) => new GatheringSnapshot { manifestId="manifest.fixture",manifestRevision=1,nodes=new[] {
            new ResourceNodeState { id="harvest:fixture",definitionId="resource.pine",definitionRevision=1,cellId="resident",
                outputId="material.log",anchor=new TransformSnapshot { qw=1 },health=health,maximumHealth=health,damage=damage,budget=3,remaining=3 } } };
        static TransformSnapshot Ground() => new TransformSnapshot { x=2,y=.26f,qw=1 };
        static GatheringSnapshot Hit(GatheringSnapshot before,long swing,TransformSnapshot ground=null)
        {
            Assert.AreEqual(HarvestReason.Accepted,GatheringState.Prepare(before,before.nodes[0].id,before.nodes[0].revision,
                "session.a",swing,MeleeWeaponDefinition.AxeId,ground,out var next));
            Assert.IsTrue(GatheringState.Valid(next)); return next;
        }
        static SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture","content.1",
            new Dictionary<string,int> { ["ammo.rifle"]=60,["medical.bandage"]=10,["material.log"]=8 },"rifle.1",30));
        static SaveGame Save(GatheringSnapshot gathering=null)
        { var s=SaveFoundationTests.Fixture(); s.gathering=gathering; s.header.gatheringVersion=gathering==null?0:1; return s; }
        [Test] public void AcceptedHitDetachesCandidateAndDebitsOneConditionPoint()
        {
            var before=Initial(); var after=Hit(before,1);
            Assert.AreEqual(100,before.nodes[0].health); Assert.AreEqual(100,before.axeCondition);
            Assert.AreEqual(75,after.nodes[0].health); Assert.AreEqual(1,after.nodes[0].revision);
            Assert.AreEqual(99,after.axeCondition); Assert.AreEqual(ResourceStage.Damaged,after.nodes[0].stage);
            Assert.IsNull(after.nodes[0].pickup);
        }
        [TestCase("weapon.crowbar")][TestCase("weapon.pistol")][TestCase("weapon.machete")][TestCase("")]
        public void WrongToolNeverMutatesSource(string tool)
        {
            var state=Initial(); var json=JsonUtility.ToJson(state);
            Assert.AreEqual(HarvestReason.WrongTool,GatheringState.Prepare(state,state.nodes[0].id,0,"a",1,tool,null,out var next));
            Assert.IsNull(next); Assert.AreEqual(json,JsonUtility.ToJson(state));
        }
        [Test] public void DuplicateAndStaleRevisionCannotAdvanceBudget()
        {
            var state=Hit(Initial(),1);
            Assert.AreEqual(HarvestReason.Duplicate,GatheringState.Prepare(state,state.nodes[0].id,1,"session.a",1,MeleeWeaponDefinition.AxeId,null,out _));
            Assert.AreEqual(HarvestReason.StaleRevision,GatheringState.Prepare(state,state.nodes[0].id,0,"session.a",2,MeleeWeaponDefinition.AxeId,null,out _));
            Assert.AreEqual(75,state.nodes[0].health); Assert.AreEqual(3,state.nodes[0].remaining);
        }
        [TestCase(false)][TestCase(true)] public void FinalHitCreatesOneDeterministicRewardAndNoRespawn(bool clear)
        {
            var state=Initial(); for (int i=1;i<=4;i++) state=Hit(state,i,clear?Ground():null);
            var n=state.nodes[0]; Assert.AreEqual(0,n.health); Assert.AreEqual(0,n.remaining);
            Assert.AreEqual(1,n.yieldSequence); Assert.AreEqual("harvest:fixture:yield:1",n.pickup.id);
            Assert.AreEqual(3,n.pickup.quantity); Assert.AreEqual(WorldItemOrigin.Harvest,n.pickup.origin);
            Assert.AreEqual(clear?RewardLocation.World:RewardLocation.Pending,n.reward);
            Assert.AreEqual(HarvestReason.Depleted,GatheringState.Prepare(state,n.id,n.revision,"session.a",5,MeleeWeaponDefinition.AxeId,Ground(),out _));
        }
        [Test] public void LastConditionPointPaysForBlowThenBrokenToolRejectsNextHit()
        {
            var state=Initial(101,1); for (int i=1;i<=100;i++) state=Hit(state,i);
            Assert.AreEqual(0,state.axeCondition); Assert.AreEqual(1,state.nodes[0].health);
            Assert.AreEqual(HarvestReason.BrokenTool,GatheringState.Prepare(state,state.nodes[0].id,100,"session.a",101,MeleeWeaponDefinition.AxeId,null,out _));
            state=Initial(100,1); for (int i=1;i<=100;i++) state=Hit(state,i,Ground());
            Assert.AreEqual(0,state.axeCondition); Assert.AreEqual(3,state.nodes[0].pickup.quantity);
        }
        [TestCase("version")][TestCase("budget")][TestCase("revision")][TestCase("condition")][TestCase("nan")][TestCase("duplicateId")]
        public void MalformedGatheringNeverEncodes(string kind)
        {
            var s=Save(Hit(Initial(1,1),1,Ground())); var n=s.gathering.nodes[0];
            switch (kind)
            {
                case "version": s.header.gatheringVersion=2; break;
                case "budget": n.pickup.quantity=4; break;
                case "revision": n.revision++; break;
                case "condition": s.gathering.axeCondition=100; break;
                case "nan": n.pickup.transform.x=float.NaN; break;
                case "duplicateId": n.id=s.world.items[0].id; n.pickup.id=n.id+":yield:1"; break;
            }
            Assert.IsFalse(Codec().Encode(s,out _).Success);
        }
        [Test] public void PartialAndDepletedSavesRoundtripTwiceWithoutChangingLegacyOwners()
        {
            var codec=Codec(); var partial=Save(Hit(Initial(),1));
            Assert.IsTrue(codec.Encode(partial,out var json).Success);
            Assert.IsTrue(codec.Decode(json,out var restored).Success);
            Assert.AreEqual(75,restored.gathering.nodes[0].health);
            var state=restored.gathering; for (int i=2;i<=4;i++) state=Hit(state,i,Ground());
            var final=Save(state);
            for (int i=0;i<2;i++)
            {
                Assert.IsTrue(codec.Encode(final,out json).Success); Assert.IsTrue(codec.Decode(json,out final).Success);
                Assert.AreEqual(0,final.gathering.nodes[0].health); Assert.AreEqual(3,final.gathering.nodes[0].pickup.quantity);
                Assert.AreEqual(60,final.inventory.slots[0].quantity); Assert.AreEqual(67,final.shelter.storage.slots[0].quantity+final.shelter.storage.slots[2].quantity);
                Assert.AreEqual(3,final.world.items.Length);
            }
        }
        [Test] public void RestoredReceiptRejectsSameSwingAndNewSessionUsesNewReceiptNamespace()
        {
            var codec=Codec(); var saved=Save(Hit(Initial(),1));
            Assert.IsTrue(codec.Encode(saved,out var json).Success); Assert.IsTrue(codec.Decode(json,out var loaded).Success);
            var n=loaded.gathering.nodes[0];
            Assert.AreEqual(HarvestReason.Duplicate,GatheringState.Prepare(loaded.gathering,n.id,n.revision,"session.a",1,MeleeWeaponDefinition.AxeId,null,out _));
            Assert.AreEqual(HarvestReason.Accepted,GatheringState.Prepare(loaded.gathering,n.id,n.revision,"session.b",1,MeleeWeaponDefinition.AxeId,null,out var next));
            Assert.AreEqual(50,next.nodes[0].health); Assert.AreEqual(2,next.nodes[0].revision);
        }
        [Serializable] sealed class Legacy
        { public SaveHeader header; public PlayerSnapshot player; public ContainerSnapshot inventory; public WeaponSnapshot weapon; public ShelterSnapshot shelter; public WorldSnapshot world; }
        [Serializable] sealed class Envelope { public string format,checksum,payload; }
        [Test] public void TrulyAbsentLegacySectionRemainsAbsent()
        {
            var s=Save(); var payload=JsonUtility.ToJson(new Legacy { header=s.header,player=s.player,inventory=s.inventory,weapon=s.weapon,shelter=s.shelter,world=s.world });
            payload=payload.Replace("\"gatheringVersion\":0,","");
            string hash; using (var sha=SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant();
            var result = Codec().Decode(
                JsonUtility.ToJson(new Envelope
                {
                    format = "last-signal-save-1",
                    checksum = hash,
                    payload = payload
                }),
                out var restored
            );

            Assert.IsTrue(
                result.Success,
                $"Legacy save decode failed: {result.Error} — {result.Message}"
            );
            Assert.IsNull(restored.gathering); Assert.AreEqual(0,restored.header.gatheringVersion);
            Assert.AreEqual(13,restored.world.items[1].quantity);
        }
        [Test] public void NullGatheringRoundtripPreservesLegacyOwnersWithCurrentUnitySerializer()
        {
            var input=Save(); var encoded=Codec().Encode(input,out var json);
            Assert.IsTrue(encoded.Success,encoded.Message);
            for(int i=0;i<2;i++)
            {
                var decoded=Codec().Decode(json,out var restored);
                Assert.IsTrue(decoded.Success,decoded.Message); Assert.IsNull(restored.gathering);
                Assert.AreEqual(0,restored.header.gatheringVersion); Assert.AreEqual(13,restored.world.items[1].quantity);
                Assert.IsTrue(Codec().Encode(restored,out json).Success);
            }
        }
        [TestCase("manifest")][TestCase("revision")][TestCase("sequence")][TestCase("swing")]
        [TestCase("session")][TestCase("nodes")][TestCase("condition")][TestCase("version")]
        public void UnmarkedNonEmptyGatheringCannotBeSilentlyDiscarded(string field)
        {
            var input=Save(); input.gathering=new GatheringSnapshot();
            switch(field)
            {
                case "manifest": input.gathering.manifestId="present"; break;
                case "revision": input.gathering.manifestRevision=1; break;
                case "sequence": input.gathering.sequence=1; break;
                case "swing": input.gathering.lastSwing=1; break;
                case "session": input.gathering.actorSession="present"; break;
                case "nodes": input.gathering.nodes=Initial().nodes; break;
                case "condition": input.gathering.axeCondition=99; break;
                case "version": input.gathering.version=2; break;
            }
            var payload=JsonUtility.ToJson(input);
            string hash; using(var sha=SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant();
            var decoded=Codec().Decode(JsonUtility.ToJson(new Envelope {format="last-signal-save-1",checksum=hash,payload=payload}),out _);
            Assert.IsFalse(decoded.Success,"Unmarked nonempty gathering was discarded: "+field);
        }
        [TestCase(30)][TestCase(60)][TestCase(120)][TestCase(1)] public void ExistingActiveWindowChargesOnceAndIsFrameRateIndependent(int fps)
        {
            var axe=AssetDatabase.LoadAssetAtPath<MeleeWeaponDefinition>("Assets/LastSignal/Data/Combat/S021/WoodenAxe.asset");
            Assert.IsNotNull(axe); var go=new GameObject("harvest stamina fixture");
            try
            {
                var stamina=go.AddComponent<PlayerStamina>(); var attack=new MeleeAttackState(axe); int calls=0;
                var state=Initial();
                attack.ActiveWindow+=()=> { calls++; state=Hit(state,(long)attack.SwingId); };
                Assert.IsTrue(attack.TryBegin(stamina)); Assert.IsFalse(attack.TryBegin(stamina));
                for (int i=0;i<fps*3;i++) attack.Tick(1f/fps);
                Assert.AreEqual(1,calls); Assert.AreEqual(1,state.nodes[0].revision);
                Assert.AreEqual(100-axe.StaminaCost,stamina.CurrentStamina,.001f);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void CancelBeforeActiveWindowProducesNoNodeMutation()
        {
            var axe=AssetDatabase.LoadAssetAtPath<MeleeWeaponDefinition>("Assets/LastSignal/Data/Combat/S021/WoodenAxe.asset");
            var go=new GameObject("cancel fixture");
            try
            { var attack=new MeleeAttackState(axe); int calls=0; attack.ActiveWindow+=()=>calls++; Assert.IsTrue(attack.TryBegin(go.AddComponent<PlayerStamina>())); attack.Cancel(); attack.Tick(10); Assert.AreEqual(0,calls); }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        public sealed class DamageProbe : MonoBehaviour, IDamageable
        {
            public int hits;
            public bool IsAlive => true;
            public void TakeDamage(DamageInfo info) => hits++;
        }
        [TestCase(true)][TestCase(false)] public void NearestTreeRejectionNeverFallsThroughToEnemy(bool enemyInFront)
        {
            var axe=AssetDatabase.LoadAssetAtPath<MeleeWeaponDefinition>("Assets/LastSignal/Data/Combat/S021/WoodenAxe.asset");
            var player=new GameObject("query fixture"); var tree=new GameObject("resource query fixture"); var enemy=new GameObject("enemy query fixture");
            var position=new Vector3(10000,10000,10000); player.transform.position=position;
            try
            {
                var weapon=player.AddComponent<MeleeWeaponController>();
                tree.transform.position=position+Vector3.forward*1.3f; tree.AddComponent<ResourceNodeView>();
                tree.AddComponent<BoxCollider>().size=Vector3.one*.4f;
                enemy.transform.position=position+Vector3.forward*(enemyInFront?.7f:1.7f);
                enemy.AddComponent<BoxCollider>().size=Vector3.one*.4f; var probe=enemy.AddComponent<DamageProbe>();
                Physics.SyncTransforms(); var resolver=new MeleeAttackResolver();
                bool success=resolver.Resolve(player.transform,player.transform,axe,player,1,weapon);
                Assert.AreEqual(enemyInFront,success); Assert.AreEqual(enemyInFront?1:0,probe.hits);
                Assert.AreEqual(enemyInFront?MeleeTargetOutcome.EnemyHit:MeleeTargetOutcome.Rejected,resolver.LastOutcome);
            }
            finally { UnityEngine.Object.DestroyImmediate(enemy); UnityEngine.Object.DestroyImmediate(tree); UnityEngine.Object.DestroyImmediate(player); }
        }
        sealed class Files : ISaveFiles
        {
            public readonly Dictionary<string,string> data=new Dictionary<string,string>();
            public string fault;
            public bool Exists(string path) { if(fault=="unreadable" && !path.EndsWith(".tmp")) throw new IOException(); return data.ContainsKey(path); }
            public string Read(string path) => data[path];
            public void WriteCandidate(string path,string json) { if(fault=="diskfull") throw new IOException(); data[path]=json; }
            public void Publish(string candidate,string current,string backup)
            {
                if (fault=="before") throw new IOException();
                data[current]=data[candidate]; data.Remove(candidate);
                if(fault=="after" || fault=="unknown") { if(fault=="unknown") fault="unreadable"; throw new IOException(); }
            }
            public void DeleteCandidate(string path) => data.Remove(path);
        }
        [TestCase("diskfull",DurableOutcome.Aborted)][TestCase("before",DurableOutcome.Aborted)]
        [TestCase("after",DurableOutcome.Committed)][TestCase("unknown",DurableOutcome.RecoveryRequired)]
        public void RealSaveStoreFaultBoundaryResolvesPublicationWithoutDuplicatingReward(string fault,DurableOutcome expected)
        {
            var files=new Files(); var store=new SaveFileStore(Path.Combine(Path.GetTempPath(),"p01-fault.json"),Codec(),files);
            Assert.IsTrue(store.Write(Save(Initial()),0).Success); files.fault=fault;
            var source=Initial(1,1); var candidate=Save(Hit(source,1,Ground()));
            Assert.AreEqual(expected,HarvestDurability.Publish(store,candidate,out _));
            Assert.AreEqual(1,source.nodes[0].health); // Source adoption belongs to composition root, not file seam.
            files.fault=null; Assert.IsTrue(store.Read(0,out var read).Success);
            Assert.AreEqual(expected==DurableOutcome.Aborted?1:2,read.header.generation);
            if(expected!=DurableOutcome.Aborted) Assert.AreEqual("harvest:fixture:yield:1",read.gathering.nodes[0].pickup.id);
        }
        [Test] public void EquivalentChecksInventoryAsWellAsHarvestState()
        {
            var store=new SaveFileStore(Path.Combine(Path.GetTempPath(),"p01-compare.json"),Codec(),new Files());
            var a=Save(Initial()); var b=Save(Initial()); b.header.timestampUtc=a.header.timestampUtc;
            Assert.IsTrue(store.Equivalent(a,b)); b.inventory.slots[0].quantity--;
            Assert.IsFalse(store.Equivalent(a,b));
        }
    }
}
