using System.Collections.Generic;
using LastSignal.Gathering;
using LastSignal.Persistence;
using LastSignal.Noise;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class P01TreePersistenceTests
    {
        static SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture","content.1",
            new Dictionary<string,int>{{"ammo.rifle",60},{"medical.bandage",10},{"material.log",8}},"rifle.1",30));
        static GatheringSnapshot Cut(int cutVersion=1)
        {
            var s=P01TreeStructureTests.Initial();
            s.nodes[0].tree.version=cutVersion;
            if(cutVersion==2) s.nodes[0].tree.cutStep=.125f;
            Assert.AreEqual(HarvestReason.Accepted,TreeStructure.Cut(s,"tree.fixture",0,"session",1,MeleeWeaponDefinition.AxeId,
                new Vector3(.25f,.9f,0),Vector3.left,out var next)); return next;
        }
        [TestCase(false,1)][TestCase(true,1)][TestCase(false,2)][TestCase(true,2)]
        public void RealSaveEnvelopeRestoresPartialCutOrFractureWithoutAnotherYield(bool fractured,int cutVersion)
        {
            var g=Cut(cutVersion); if(fractured) Assert.AreEqual(HarvestReason.Accepted,TreeStructure.Fracture(g,"tree.fixture",1,out g));
            var save=SaveFoundationTests.Fixture(); save.header.gatheringVersion=1; save.gathering=g;
            var codec=Codec(); Assert.IsTrue(codec.Encode(save,out var json).Success);
            Assert.IsTrue(codec.Decode(json,out var restored).Success);
            var n=restored.gathering.nodes[0]; Assert.AreEqual(1,n.treeVersion); Assert.AreEqual(1,n.revision);
            Assert.AreEqual(cutVersion,n.tree.version); Assert.AreEqual(g.nodes[0].tree.cutStep,n.tree.cutStep);
            Assert.AreEqual(g.nodes[0].tree.cutUnits,n.tree.cutUnits);
            Assert.AreEqual(fractured,n.tree.fractured); Assert.AreEqual(fractured?1:0,n.yieldSequence);
            Assert.AreEqual(fractured?1:0,n.tree.fractureGeneration); Assert.AreEqual(fractured?1:0,n.tree.fractureRevision);
            Assert.AreEqual(99,restored.gathering.axeCondition);
        }
        [Test] public void StaleFractureAndInvalidPhysicalPoseCannotBePublished()
        {
            var g=Cut(); string before=JsonUtility.ToJson(g);
            Assert.AreEqual(HarvestReason.StaleRevision,TreeStructure.Fracture(g,"tree.fixture",0,out var next)); Assert.IsNull(next);
            Assert.AreEqual(before,JsonUtility.ToJson(g));
            g.nodes[0].tree.rotation=new Quaternion(0,0,0,0);
            var save=SaveFoundationTests.Fixture(); save.header.gatheringVersion=1; save.gathering=g;
            Assert.IsFalse(Codec().Encode(save,out _).Success);
        }
        [Test] public void CorruptedFractureGenerationIsRejected()
        { var g=Cut(); TreeStructure.Fracture(g,"tree.fixture",1,out g); g.nodes[0].tree.fractureGeneration=2; Assert.IsFalse(GatheringState.Valid(g)); }
        [TestCase(30)][TestCase(60)][TestCase(120)]
        public void ActiveWindowProducesOneCutDurabilityDebitAndNoiseReceiptAtEveryRenderRate(int fps)
        {
            var axe=AssetDatabase.LoadAssetAtPath<MeleeWeaponDefinition>("Assets/LastSignal/Data/Combat/S021/WoodenAxe.asset");
            var player=new GameObject("Tree FPS fixture");
            try
            {
                var stamina=player.AddComponent<PlayerStamina>(); var attack=new MeleeAttackState(axe);
                var g=P01TreeStructureTests.Initial(); var noise=new GameplayNoiseSystem(()=>0,()=>true); int windows=0;
                attack.ActiveWindow+=()=>
                {
                    windows++;
                    var reason=TreeStructure.Cut(g,"tree.fixture",g.nodes[0].revision,"session",(long)attack.SwingId,axe.WeaponId,
                        new Vector3(.25f,.9f,0),Vector3.left,out var next);
                    Assert.AreEqual(HarvestReason.Accepted,reason); g=next;
                    Assert.IsTrue(noise.TryEmit(new GameplayNoiseRequest(1,Vector3.zero,GameplayNoiseCategory.TreeChop,new GameplayNoiseTuning().TreeChop,attack.SwingId),out var receipt));
                    Assert.IsFalse(noise.TryReceive(receipt)); // Network/replay duplicate receipt must not enter pressure twice.
                    Assert.AreEqual(HarvestReason.Duplicate,TreeStructure.Cut(g,"tree.fixture",1,"session",(long)attack.SwingId,axe.WeaponId,Vector3.right,Vector3.left,out _));
                };
                Assert.IsTrue(attack.TryBegin(stamina)); for(int i=0;i<fps*3;i++) attack.Tick(1f/fps);
                Assert.AreEqual(1,windows); Assert.AreEqual(1,noise.AcceptedCount); Assert.AreEqual(99,g.axeCondition);
                Assert.AreEqual(100-axe.StaminaCost,stamina.CurrentStamina,.001f); Assert.IsNull(g.nodes[0].pickup);
            }
            finally { Object.DestroyImmediate(player); }
        }
        [TestCase(GameplayNoiseCategory.TreeChop)][TestCase(GameplayNoiseCategory.TreeImpact)]
        public void TreeNoiseIsEligibleForExistingPressureAndZombieHearing(GameplayNoiseCategory category)
        {
            var request=new GameplayNoiseRequest(1,Vector3.zero,category,new GameplayNoiseProfile(12,.5f,2));
            var noise=new GameplayNoiseEvent(new GameplayNoiseId(1,1),request,0);
            Assert.IsTrue(WorldPressureNoiseAdapter.Contributes(category));
            Assert.IsTrue(LastSignal.ZombieHearingEvaluator.Valid(noise,0));
        }
        [TestCase(1.3f,false,"Harvest:Invalid")][TestCase(1.3f,true,"Miss")][TestCase(8f,false,"Miss")]
        public void RealMeleeQueryChecksRangeAndOcclusionBeforeDispatch(float distance,bool wall,string outcome)
        {
            var axe=AssetDatabase.LoadAssetAtPath<MeleeWeaponDefinition>("Assets/LastSignal/Data/Combat/S021/WoodenAxe.asset");
            var player=new GameObject("Query actor"); var tree=new GameObject("Query tree"); GameObject blocker=null;
            var origin=new Vector3(20000,20000,20000); player.transform.position=origin;
            try
            {
                var weapon=player.AddComponent<MeleeWeaponController>(); tree.transform.position=origin+Vector3.forward*distance;
                tree.AddComponent<ResourceNodeView>(); tree.AddComponent<BoxCollider>().size=Vector3.one*.4f;
                if(wall) { blocker=GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position=origin+Vector3.forward*.6f; blocker.transform.localScale=new Vector3(3,3,.1f); }
                Physics.SyncTransforms(); var resolver=new MeleeAttackResolver();
                Assert.IsFalse(resolver.Resolve(player.transform,player.transform,axe,player,1,weapon));
                Assert.AreEqual(outcome,resolver.LastResult); Assert.IsNull(resolver.LastTarget);
            }
            finally { if(blocker) Object.DestroyImmediate(blocker); Object.DestroyImmediate(tree); Object.DestroyImmediate(player); }
        }
    }
}
