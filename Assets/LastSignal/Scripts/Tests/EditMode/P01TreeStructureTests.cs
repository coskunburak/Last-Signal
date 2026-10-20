using System;
using LastSignal.Gathering;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class P01TreeStructureTests
    {
        [Test] public void UnapprovedAudioPreviewCannotEscapeEditorValidationScene()
        {
            var bank=ScriptableObject.CreateInstance<TreeAudioBank>();
            try
            {
                string stage="Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_fixture.unity";
                Assert.IsFalse(bank.CanPlay(stage)); bank.previewInValidationScene=true;
                Assert.IsTrue(bank.CanPlay(stage)); Assert.IsFalse(bank.mappingApproved);
                Assert.IsFalse(bank.CanPlay(null)); Assert.IsFalse(bank.CanPlay("Assets/LastSignal/Scenes/Production/FirstTown.unity"));
                Assert.IsFalse(bank.CanPlay("Assets/LastSignal/Scenes/Validation/FirstTownHarvest_fixture.unity"));
                Assert.IsFalse(bank.CanPlay(stage+".backup"));
                bank.mappingApproved=true; Assert.IsTrue(bank.CanPlay("Assets/LastSignal/Scenes/Production/FirstTown.unity"));
            }
            finally { UnityEngine.Object.DestroyImmediate(bank); }
        }
        public static GatheringSnapshot Initial() => new GatheringSnapshot { manifestId="physics.fixture",manifestRevision=1,
            nodes=new[]{ new ResourceNodeState { id="tree.fixture",cellId="resident",definitionId="pine.physics",definitionRevision=1,
                outputId="material.log",anchor=new TransformSnapshot {qw=1},health=100,maximumHealth=100,damage=25,budget=3,remaining=3,treeVersion=1,tree=new TreeCutSnapshot {hasPose=true,rotation=Quaternion.identity} } } };
        static GatheringSnapshot Hit(GatheringSnapshot s,long swing)
        {
            Assert.AreEqual(HarvestReason.Accepted,TreeStructure.Cut(s,"tree.fixture",s.nodes[0].revision,"session",swing,
                MeleeWeaponDefinition.AxeId,new Vector3(.25f,.9f,0),Vector3.left,out var next));
            Assert.IsTrue(GatheringState.Valid(next)); return next;
        }
        [Test] public void FourLocalizedCutsWeakenButNeverAwardUntilActualFracture()
        {
            var s=Initial(); float previous=1;
            for(int i=1;i<=4;i++)
            { s=Hit(s,i); var area=TreeStructure.RemainingArea(s.nodes[0].tree.depth); Assert.Less(area,previous); previous=area;
                Assert.IsNull(s.nodes[0].pickup); Assert.AreEqual(3,s.nodes[0].remaining); }
            Assert.AreEqual(1,s.nodes[0].health); Assert.IsFalse(s.nodes[0].tree.fractured);
            Assert.AreEqual(96,s.axeCondition);
        }
        [Test] public void FractureCreatesOnePendingIdentityAndDoesNotDebitToolAgain()
        {
            var s=Hit(Initial(),1);
            Assert.AreEqual(HarvestReason.Accepted,TreeStructure.Fracture(s,"tree.fixture",s.nodes[0].revision,out var next));
            Assert.IsTrue(GatheringState.Valid(next)); Assert.AreEqual(s.axeCondition,next.axeCondition);
            Assert.AreEqual("tree.fixture:yield:1",next.nodes[0].pickup.id); Assert.AreEqual(3,next.nodes[0].pickup.quantity);
            Assert.AreEqual(RewardLocation.Pending,next.nodes[0].reward); Assert.IsNull(next.nodes[0].pickup.transform);
            Assert.AreEqual(HarvestReason.Duplicate,TreeStructure.Fracture(next,"tree.fixture",next.nodes[0].revision,out _));
            Assert.AreEqual(HarvestReason.Depleted,TreeStructure.Cut(next,"tree.fixture",1,"session",2,MeleeWeaponDefinition.AxeId,
                Vector3.right,Vector3.left,out _));
        }
        [Test] public void IntactTreeCannotRequestFailureOrUseLegacyHealthPath()
        {
            var s=Initial();
            Assert.AreEqual(HarvestReason.Invalid,TreeStructure.Fracture(s,"tree.fixture",s.nodes[0].revision,out _));
            Assert.AreEqual(HarvestReason.Invalid,GatheringState.Prepare(s,"tree.fixture",0,"session",1,MeleeWeaponDefinition.AxeId,null,out _));
        }
        [Test] public void DuplicateAndWrongSideCannotConsumeDurabilityOrYield()
        {
            var s=Hit(Initial(),1); string before=JsonUtility.ToJson(s);
            Assert.AreEqual(HarvestReason.Duplicate,TreeStructure.Cut(s,"tree.fixture",1,"session",1,MeleeWeaponDefinition.AxeId,Vector3.right,Vector3.left,out _));
            Assert.AreEqual(HarvestReason.Blocked,TreeStructure.Cut(s,"tree.fixture",1,"session",2,MeleeWeaponDefinition.AxeId,Vector3.left,Vector3.right,out _));
            Assert.AreEqual(before,JsonUtility.ToJson(s));
        }
        [TestCase("weapon.crowbar")][TestCase("")]
        public void InvalidToolNeverProducesCut(string tool)
        { var s=Initial(); Assert.AreEqual(HarvestReason.WrongTool,TreeStructure.Cut(s,"tree.fixture",0,"session",1,tool,Vector3.right,Vector3.left,out var next)); Assert.IsNull(next); }
        [Test] public void NonFiniteImpactAndStaleRevisionRejectWithoutMutation()
        {
            var s=Initial();
            Assert.AreEqual(HarvestReason.Invalid,TreeStructure.Cut(s,"tree.fixture",0,"session",1,MeleeWeaponDefinition.AxeId,new Vector3(float.NaN,0,0),Vector3.left,out _));
            Assert.AreEqual(HarvestReason.StaleRevision,TreeStructure.Cut(s,"tree.fixture",1,"session",1,MeleeWeaponDefinition.AxeId,Vector3.right,Vector3.left,out _));
        }
        [Test] public void LastDurabilityPointPaysOnlyForAcceptedCut()
        {
            var s=Initial(); for(int i=1;i<=100;i++) s=Hit(s,i);
            Assert.AreEqual(0,s.axeCondition);
            Assert.AreEqual(HarvestReason.BrokenTool,TreeStructure.Cut(s,"tree.fixture",100,"session",101,MeleeWeaponDefinition.AxeId,Vector3.right,Vector3.left,out _));
        }
        [Test] public void PersistenceRoundTripRetainsCutPoseAndConservedReward()
        {
            var s=Hit(Initial(),1); TreeStructure.Fracture(s,"tree.fixture",s.nodes[0].revision,out s);
            var t=s.nodes[0].tree; t.hasPose=true; t.position=new Vector3(2,1,3); t.rotation=Quaternion.Euler(30,0,0);
            t.velocity=Vector3.down; t.angularVelocity=Vector3.forward; t.impactRecorded=true;
            var copy=GatheringState.Copy(s);
            Assert.IsTrue(GatheringState.Valid(copy)); Assert.AreEqual(JsonUtility.ToJson(s),JsonUtility.ToJson(copy));
            Assert.IsNull(copy.nodes[0].pickup.transform);
            copy.nodes[0].tree.velocity=Vector3.one*float.PositiveInfinity; Assert.IsFalse(GatheringState.Valid(copy));
        }
        [Test] public void OldS003SnapshotRetainsItsExistingSemantics()
        {
            var s=Initial(); s.nodes[0].tree=null; s.nodes[0].treeVersion=0;
            var copy=GatheringState.Copy(s); Assert.IsNull(copy.nodes[0].tree); Assert.IsTrue(GatheringState.Valid(copy));
            Assert.AreEqual(HarvestReason.Accepted,GatheringState.Prepare(copy,"tree.fixture",0,"s",1,MeleeWeaponDefinition.AxeId,null,out var next));
            Assert.AreEqual(75,next.nodes[0].health);
        }
        [Test] public void SupportDependsOnSectionAndScaleNotAnUnconstrainedJointAxis()
        {
            Assert.AreEqual(1,TreeStructure.RemainingArea(0),.0001f); Assert.AreEqual(.5f,TreeStructure.RemainingArea(.5f),.0001f);
            Assert.AreEqual(0,TreeStructure.RemainingArea(1),.0001f);
            Assert.AreEqual(8,TreeStructure.MomentCapacity(.5f,1,.5f)/TreeStructure.MomentCapacity(.25f,1,.5f),.0001f);
        }
        [Test] public void NullGeometryCannotBecomePhysicsReady()
        { var p=ScriptableObject.CreateInstance<TreePhysicsProfile>(); try { Assert.IsFalse(p.Valid); Assert.Throws<ArgumentException>(()=>LastSignal.Editor.TreePhysicsAuthoring.Extract(null,0,Matrix4x4.identity)); } finally { UnityEngine.Object.DestroyImmediate(p); } }
        [TestCase(false)][TestCase(true)]
        public void AuthoringRegistersInactiveBodyBeforeValidationAndRetainsSourceOnFailure(bool missingTopMesh)
        {
            var anchor=new GameObject("Authoring binding fixture");
            var profile=ScriptableObject.CreateInstance<TreePhysicsProfile>();
            var mesh=new Mesh();
            try
            {
                mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up}; mesh.subMeshCount=2;
                mesh.SetTriangles(new[]{0,1,2},0); mesh.SetTriangles(new[]{2,1,0},1);
                profile.intactTop=mesh; profile.intactStump=mesh;
                for(int i=0;i<profile.cutTops.Length;i++) { profile.cutTops[i]=mesh; profile.cutStumps[i]=mesh; }
                Assert.IsTrue(profile.Valid);
                var node=anchor.AddComponent<ResourceNodeView>();
                var original=new GameObject("Original visual"); original.transform.SetParent(anchor.transform,false);
                node.standingVisual=original;
                var trunk=new GameObject("Inactive assembled trunk"); trunk.transform.SetParent(anchor.transform,false); trunk.SetActive(false);
                var tc=trunk.AddComponent<CapsuleCollider>();
                var rb=trunk.AddComponent<Rigidbody>(); rb.isKinematic=true;
                var physical=trunk.AddComponent<TreePhysicsBody>();
                physical.node=node; physical.profile=profile; physical.trunkCollider=tc; node.physicsBody=physical;
                physical.topMesh=trunk.AddComponent<MeshFilter>(); trunk.AddComponent<MeshRenderer>();
                physical.topMesh.sharedMesh=missingTopMesh?null:mesh;
                var stump=new GameObject("Static stump"); stump.transform.SetParent(anchor.transform,false);
                physical.stumpCollider=stump.AddComponent<BoxCollider>();
                physical.stumpMesh=stump.AddComponent<MeshFilter>(); stump.AddComponent<MeshRenderer>(); physical.stumpMesh.sharedMesh=mesh;
                if(missingTopMesh)
                {
                    var error=Assert.Throws<InvalidOperationException>(()=>LastSignal.Editor.TreePhysicsAuthoring.ActivatePhysicalProjection(physical));
                    StringAssert.Contains("incomplete top mesh/renderer",error.Message);
                    Assert.IsFalse(trunk.activeSelf);
                }
                else
                {
                    LastSignal.Editor.TreePhysicsAuthoring.ActivatePhysicalProjection(physical);
                    Assert.IsTrue(trunk.activeInHierarchy); Assert.IsTrue(physical.Valid);
                    Assert.AreSame(rb,tc.attachedRigidbody); Assert.IsNull(physical.stumpCollider.attachedRigidbody);
                }
                Assert.IsTrue(original.activeSelf,"Original visual must survive until the caller accepts the replacement.");
                Assert.IsTrue(rb.isKinematic); Assert.IsNull(physical.Connection);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(anchor); UnityEngine.Object.DestroyImmediate(profile); UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        [TestCase(.25f,4)][TestCase(.125f,8)]
        public void ConfiguredCutEffortAdvancesGeometryWithoutHealthTriggeredYield(float penetration,int hits)
        {
            var s=Initial(); s.nodes[0].tree.version=2; s.nodes[0].tree.cutStep=penetration;
            for(int i=1;i<=hits;i++)
            {
                float previous=s.nodes[0].tree.depth; s=Hit(s,i);
                Assert.Greater(s.nodes[0].tree.depth,previous);
                Assert.IsNull(s.nodes[0].pickup); Assert.IsFalse(s.nodes[0].tree.fractured);
            }
            Assert.AreEqual(.975f,s.nodes[0].tree.depth,.00001f);
            Assert.IsTrue(GatheringState.Valid(GatheringState.Copy(s)));
            int condition=s.axeCondition;
            Assert.AreEqual(HarvestReason.AwaitingFall,TreeStructure.Cut(s,"tree.fixture",s.nodes[0].revision,"session",hits+1,
                MeleeWeaponDefinition.AxeId,new Vector3(.25f,.9f,0),Vector3.left,out var exhausted));
            Assert.IsNull(exhausted); Assert.AreEqual(condition,s.axeCondition);
        }
        [Test] public void ToolConditionAndResistanceChangeEffortWithoutChangingYieldBudget()
        {
            Assert.Greater(TreeStructure.CuttingUnits(.25f,100),TreeStructure.CuttingUnits(.25f,25));
            var definition=ScriptableObject.CreateInstance<ResourceNodeDefinition>();
            try
            {
                var normal=TreeStructure.CreateCut(definition); definition.cuttingResistance=2;
                var resistant=TreeStructure.CreateCut(definition);
                Assert.Less(resistant.cutStep,normal.cutStep); Assert.AreEqual(2,resistant.version);
                Assert.AreEqual(3,definition.yield);
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }
        [Test] public void FurtherContactsKeepFirstNotchPositionAndRejectCorruptQuantization()
        {
            var s=Initial(); s.nodes[0].tree.version=2; s.nodes[0].tree.cutStep=.125f; s=Hit(s,1);
            var first=s.nodes[0].tree.localImpact;
            Assert.AreEqual(HarvestReason.Accepted,TreeStructure.Cut(s,"tree.fixture",1,"session",2,MeleeWeaponDefinition.AxeId,
                new Vector3(.3f,1,0),Vector3.left,out var next));
            Assert.AreEqual(first,next.nodes[0].tree.localImpact);
            next.nodes[0].tree.cutUnits++; Assert.IsFalse(GatheringState.Valid(next));
        }
        [Test] public void SupportCentroidCreatesGravityLeverArmOppositeTheCut()
        {
            Assert.AreEqual(0,TreeStructure.SupportOffset(.25f,0),.000001f);
            Assert.AreEqual(.25f*4/(3*Mathf.PI),TreeStructure.SupportOffset(.25f,.5f),.00001f);
            Assert.Greater(TreeStructure.SupportOffset(.25f,.75f),TreeStructure.SupportOffset(.25f,.25f));
            Assert.Less(TreeStructure.SupportOffset(.25f,.975f),.25f);
        }
        [Test] public void AuthoringProducesProgressivelyRemovedWoodWithInteriorCaps()
        {
            var source=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Mesh bark=null,top=null,stump=null;
            try
            {
                bark=LastSignal.Editor.TreePhysicsAuthoring.Extract(source.GetComponent<MeshFilter>().sharedMesh,0,Matrix4x4.identity);
                LastSignal.Editor.TreePhysicsAuthoring.PrepareTrunk(bark,0,out top,out stump);
                float previous=Volume(top)+Volume(stump);
                foreach(float depth in new[]{.25f,.5f,.75f,.975f})
                {
                    LastSignal.Editor.TreePhysicsAuthoring.BuildNotch(top,stump,.5f,0,0,depth,out var upper,out var lower);
                    try
                    {
                        Assert.AreEqual(2,upper.subMeshCount); Assert.AreEqual(2,lower.subMeshCount);
                        Assert.Greater(upper.GetIndexCount(1),0); Assert.Greater(lower.GetIndexCount(1),0);
                        float remaining=Volume(upper)+Volume(lower); Assert.Less(remaining,previous); Assert.Greater(remaining,0); previous=remaining;
                        LastSignal.Editor.TreePhysicsAuthoring.ValidateMesh(upper); LastSignal.Editor.TreePhysicsAuthoring.ValidateMesh(lower);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(upper); UnityEngine.Object.DestroyImmediate(lower); }
                }
            }
            finally
            {
                if(bark) UnityEngine.Object.DestroyImmediate(bark); if(top) UnityEngine.Object.DestroyImmediate(top);
                if(stump) UnityEngine.Object.DestroyImmediate(stump); UnityEngine.Object.DestroyImmediate(source);
            }
        }
        static float Volume(Mesh mesh)
        {
            var vertices=mesh.vertices; var indices=mesh.triangles; float volume=0;
            for(int i=0;i<indices.Length;i+=3) volume+=Vector3.Dot(vertices[indices[i]],Vector3.Cross(vertices[indices[i+1]],vertices[indices[i+2]]))/6;
            return Mathf.Abs(volume);
        }
    }
}
