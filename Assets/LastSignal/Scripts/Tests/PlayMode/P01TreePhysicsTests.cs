using System.Collections;
using LastSignal.Gathering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Owner-run isolated PhysX fixtures. These do not substitute for authored forest/visual acceptance.</summary>
    public sealed class P01TreePhysicsTests
    {
        GameObject anchor,ground,obstacle;
        TreePhysicsBody tree;
        TreePhysicsProfile profile;
        Mesh mesh;
        ResourceNodeState node;
        float oldTimeScale;
        [SetUp] public void Setup()
        {
            oldTimeScale=Time.timeScale; Time.timeScale=1;
            anchor=new GameObject("Tree physics fixture"); var view=anchor.AddComponent<ResourceNodeView>();
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube); mesh=Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(temp); var indices=mesh.triangles; mesh.subMeshCount=2; mesh.SetTriangles(indices,0); mesh.SetTriangles(new[]{indices[0],indices[1],indices[2]},1);
            profile=ScriptableObject.CreateInstance<TreePhysicsProfile>(); profile.radius=.2f; profile.height=4; profile.mass=60;
            profile.centerOfMass=new Vector3(.06f,1.6f,0); profile.intactTop=mesh; profile.intactStump=mesh;
            for(int i=0;i<32;i++) { profile.cutTops[i]=mesh; profile.cutStumps[i]=mesh; }
            var stump=new GameObject("Stump"); stump.transform.SetParent(anchor.transform,false);
            var sc=stump.AddComponent<BoxCollider>(); sc.size=new Vector3(.28f,.9f,.28f); sc.center=Vector3.up*.45f;
            var body=new GameObject("Trunk"); body.transform.SetParent(anchor.transform,false);
            var tc=body.AddComponent<CapsuleCollider>(); tc.radius=.2f; tc.height=3.1f; tc.center=Vector3.up*2.45f;
            body.AddComponent<Rigidbody>().isKinematic=true;
            tree=body.AddComponent<TreePhysicsBody>(); view.physicsBody=tree; tree.node=view; tree.profile=profile;
            tree.topMesh=body.AddComponent<MeshFilter>(); tree.stumpMesh=stump.AddComponent<MeshFilter>(); tree.trunkCollider=tc; tree.stumpCollider=sc;
            body.AddComponent<MeshRenderer>(); stump.AddComponent<MeshRenderer>();
            tree.topMesh.sharedMesh=mesh; tree.stumpMesh.sharedMesh=mesh;
            node=new ResourceNodeState {health=100,maximumHealth=100,damage=25,tree=new TreeCutSnapshot()};
            tree.Present(node);
            ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name="Ground"; ground.transform.position=new Vector3(0,-.5f,0); ground.transform.localScale=new Vector3(30,1,30);
            Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup()
        { if(anchor) Object.DestroyImmediate(anchor); if(ground) Object.DestroyImmediate(ground); if(obstacle) Object.DestroyImmediate(obstacle); if(profile) Object.DestroyImmediate(profile); if(mesh) Object.DestroyImmediate(mesh); Time.timeScale=oldTimeScale; }
        void Cut()
        { node.revision++; node.health=75; node.tree.depth=.25f; tree.Present(node); tree.AcceptedHit(new Vector3(.2f,.9f,0),Vector3.left); }
        void RestoreFall(Vector3 position,Quaternion rotation,Vector3 angular)
        {
            tree.ResetProjection(); node.revision=1; node.health=0; node.tree.fractured=true; node.tree.depth=.25f;
            node.tree.hasPose=true; node.tree.position=position; node.tree.rotation=rotation; node.tree.angularVelocity=angular;
            tree.Present(node); Physics.SyncTransforms();
        }
        [UnityTest] public IEnumerator AcceptedContactActivatesDynamicBodyWithFullyConstrainedSupport()
        {
            Assert.IsTrue(tree.Valid); Assert.IsTrue(tree.Body.isKinematic); Assert.IsNotNull(tree.Connection);
            Assert.IsTrue(float.IsPositiveInfinity(tree.Connection.breakTorque));
            Cut(); Assert.IsFalse(tree.Body.isKinematic); Assert.IsTrue(tree.Body.useGravity);
            Assert.Greater(tree.Connection.breakTorque,0); Assert.IsFalse(float.IsPositiveInfinity(tree.Connection.breakTorque));
            yield return new WaitForFixedUpdate(); Assert.IsNotNull(tree.Connection);
            Assert.Less(Vector3.Distance(tree.Body.position,Vector3.zero),.05f);
        }
        [UnityTest] public IEnumerator ActualSolverBreakWithoutDurableOwnerFailsClosedAndAwardsNothing()
        {
            Cut(); tree.Connection.breakTorque=.01f; tree.Connection.breakForce=.01f;
            for(int i=0;i<20 && tree.Motion!=TreeMotionState.Fracturing;i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(TreeMotionState.Fracturing,tree.Motion); Assert.IsTrue(tree.Body.isKinematic);
            Assert.IsFalse(node.tree.fractured); Assert.IsNull(node.pickup); Assert.IsFalse(tree.CanCollect);
        }
        [Test] public void StumpContactShrinksWithNotchAndRestoreDoesNotGrowColliderCount()
        {
            var support=(BoxCollider)tree.stumpCollider;
            Cut(); float first=support.size.x;
            node.tree.depth=.75f; node.revision++; tree.Present(node);
            Assert.Less(support.size.x,first); Assert.Less(support.center.x,0);
            int count=support.GetComponents<Collider>().Length;
            for(int i=0;i<8;i++) tree.Present(node);
            Assert.AreEqual(count,support.GetComponents<Collider>().Length); Assert.AreEqual(2,count);
            node.tree.depth=0; node.revision=0; tree.Present(node);
            Assert.AreEqual(.28f,support.size.x,.0001f);
        }
        [UnityTest] public IEnumerator MidFallRestoreUsesGravityContactsAndSettlesWithoutPoseAnimation()
        {
            RestoreFall(new Vector3(0,2,0),Quaternion.Euler(0,0,25),Vector3.forward*.5f);
            float start=tree.Body.worldCenterOfMass.y;
            for(int i=0;i<60;i++) yield return new WaitForFixedUpdate();
            Assert.Less(tree.Body.worldCenterOfMass.y,start-.5f); Assert.IsNull(tree.Connection);
            for(int i=0;i<1200 && !tree.CanCollect;i++) yield return new WaitForFixedUpdate();
            Assert.Greater(tree.LastCollisionImpulse,0); Assert.IsTrue(tree.CanCollect,"Body did not naturally settle within 24 simulated seconds.");
            Assert.AreEqual(TreeMotionState.Resting,tree.Motion); Assert.IsTrue(tree.Body.isKinematic);
        }
        [UnityTest] public IEnumerator ObstacleSupportsFallenTrunkAboveGround()
        {
            obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.transform.position=new Vector3(0,.8f,0); obstacle.transform.localScale=new Vector3(8,1.6f,8);
            RestoreFall(new Vector3(1,3,0),Quaternion.Euler(0,0,90),Vector3.zero);
            for(int i=0;i<600 && !tree.CanCollect;i++) yield return new WaitForFixedUpdate();
            Assert.Greater(tree.LastCollisionImpulse,0); Assert.Greater(tree.Body.worldCenterOfMass.y,1.6f);
            Assert.IsTrue(node.tree.impactRecorded); Assert.IsTrue(tree.CanCollect);
        }
        [UnityTest] public IEnumerator ReenableResumesMidFallWithoutReplayingAudio()
        {
            int audio=0; tree.FeedbackRequested+=(kind,point,gain)=>audio++;
            RestoreFall(new Vector3(0,3,0),Quaternion.Euler(0,0,20),Vector3.forward);
            yield return new WaitForFixedUpdate(); var velocity=tree.Body.linearVelocity;
            tree.gameObject.SetActive(false); tree.gameObject.SetActive(true);
            Assert.IsFalse(tree.Body.isKinematic); Assert.Less((velocity-tree.Body.linearVelocity).magnitude,.001f);
            for(int i=0;i<600 && !tree.CanCollect;i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,audio); Assert.LessOrEqual(tree.SignificantImpactCount,1);
        }
        [UnityTest] public IEnumerator SlopedGroundChangesRestingOrientationThroughContacts()
        {
            ground.transform.rotation=Quaternion.Euler(0,0,12); Physics.SyncTransforms();
            RestoreFall(new Vector3(0,2,0),Quaternion.Euler(0,0,90),Vector3.zero);
            for(int i=0;i<1500 && !tree.CanCollect;i++) yield return new WaitForFixedUpdate();
            Assert.Greater(tree.LastCollisionImpulse,0); Assert.IsTrue(tree.CanCollect);
            Assert.Less(Mathf.Abs(Vector3.Dot(tree.transform.up,ground.transform.up)),.12f,
                "Settled trunk axis should follow the inclined contact plane.");
            Assert.Greater(Mathf.Abs(tree.transform.up.y),.1f,"Slope did not affect the physical resting pose.");
        }
        [Test] public void OnlyAcceptedCutDispatchesContactAudioAndParticles()
        {
            int wood=0; tree.FeedbackRequested+=(kind,point,gain)=> { if(kind==TreeAudioEvent.WoodHit) wood++; };
            Assert.IsFalse(tree.Accepts(tree.stumpCollider,Vector3.zero)); Assert.AreEqual(0,wood);
            Cut(); tree.AcceptedHit(new Vector3(.2f,.9f,0),Vector3.left); Assert.AreEqual(1,wood);
        }
        [UnityTest] public IEnumerator LivePhysicalContactsDispatchOneLandingAndBoundedSecondarySounds()
        {
            int landing=0, secondary=0;
            tree.FeedbackRequested+=(kind,point,gain)=> { if(kind==TreeAudioEvent.GroundImpact) landing++; if(kind==TreeAudioEvent.BranchContact) secondary++; };
            Cut();
            // Inject a committed projection for this isolated contact test; durable publication has separate integration coverage.
            node.tree.fractured=true; node.health=0; tree.Present(node);
            tree.Body.AddTorque(Vector3.forward*30,ForceMode.Impulse);
            for(int i=0;i<1200 && !tree.CanCollect;i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1,landing); Assert.LessOrEqual(secondary,2); Assert.AreEqual(1,tree.SignificantImpactCount);
            Assert.IsNull(node.pickup); // Physical contacts never create rewards.
        }
        [UnityTest] public IEnumerator MissingJointEntersRecoveryInsteadOfAwardingResources()
        {
            Cut(); Object.Destroy(tree.Connection); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.IsTrue(tree.Body.isKinematic); Assert.IsFalse(tree.CanCollect); Assert.IsNull(node.pickup);
        }
        [Test] public void WrongColliderAndOutOfCutBandCannotContributeToStructure()
        {
            Assert.IsFalse(tree.Accepts(tree.stumpCollider,new Vector3(.2f,.4f,0)));
            Assert.IsFalse(tree.Accepts(tree.trunkCollider,new Vector3(.2f,3,0)));
            Assert.IsTrue(tree.Accepts(tree.trunkCollider,new Vector3(.2f,.9f,0)));
        }
        [UnityTest] public IEnumerator RemovedColliderFailsClosedWithoutReward()
        {
            Cut(); Object.Destroy(tree.trunkCollider);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.IsTrue(tree.Body.isKinematic); Assert.IsFalse(tree.CanCollect); Assert.IsNull(node.pickup);
            Assert.DoesNotThrow(()=>tree.AcceptedHit(Vector3.up,Vector3.left));
        }
        [Test] public void DelayedAcceptedContactCannotWakeDisabledTree()
        {
            int feedback=0; tree.FeedbackRequested+=(kind,point,gain)=>feedback++;
            node.revision=1; node.tree.depth=.25f; tree.Present(node);
            tree.gameObject.SetActive(false); tree.AcceptedHit(Vector3.up,Vector3.left);
            Assert.IsTrue(tree.Body.isKinematic); Assert.AreEqual(0,feedback); Assert.IsNull(node.pickup);
        }
        [Test] public void RestoredSettledBodyPreservesPoseAndImpactDeduplication()
        {
            node.tree.resting=true; node.tree.impactRecorded=true;
            var expectedPosition=new Vector3(1,.2f,0);
            var q=Quaternion.Euler(0,0,90); RestoreFall(expectedPosition,q,Vector3.zero);
            Assert.IsTrue(tree.Body.isKinematic);
            // Physics transform synchronization can round the restored pose; require sub-millimetre accuracy, not bit equality.
            var actualPosition=tree.Body.position;
            Assert.Less(Vector3.Distance(expectedPosition,actualPosition),.0001f,
                "Restored position differs: expected "+expectedPosition.ToString("F9")+", actual "+actualPosition.ToString("F9"));
            Assert.Less(Quaternion.Angle(q,tree.Body.rotation),.001f); Assert.IsTrue(node.tree.impactRecorded);
        }
        [UnityTest] public IEnumerator DisableRetainsSampleAndStopsSimulation()
        {
            RestoreFall(new Vector3(0,3,0),Quaternion.Euler(0,0,20),Vector3.forward);
            yield return new WaitForFixedUpdate(); tree.gameObject.SetActive(false);
            Assert.IsTrue(tree.Body.isKinematic); Assert.IsTrue(node.tree.hasPose);
            Assert.IsTrue(TreeStructure.Finite(node.tree.position)); Assert.IsTrue(node.tree.fractured); Assert.IsFalse(node.tree.resting);
        }
        [Test] public void FracturedAndConsumedProjectionKeepsTrunkAndStumpVisible()
        {
            Cut(); node.tree.fractured=true; node.health=0; node.reward=RewardLocation.Pending; tree.Present(node);
            Assert.IsFalse(tree.Body.isKinematic); Assert.IsTrue(tree.topMesh.GetComponent<Renderer>().enabled);
            Assert.IsTrue(tree.stumpMesh.GetComponent<Renderer>().enabled);
            node.tree.resting=true; node.reward=RewardLocation.Consumed; tree.Present(node);
            Assert.IsTrue(tree.Body.isKinematic); Assert.IsTrue(tree.Body.detectCollisions);
            Assert.IsTrue(tree.topMesh.gameObject.activeInHierarchy); Assert.IsTrue(tree.topMesh.GetComponent<Renderer>().enabled);
            Assert.IsTrue(tree.stumpMesh.GetComponent<Renderer>().enabled);
        }
        [Test] public void RestoredDeepCutResumesJointLoadingWithoutAnotherAcceptedStrike()
        {
            tree.ResetProjection(); node.revision=4; node.health=1; node.tree.depth=.975f;
            node.tree.hasPose=true; node.tree.rotation=Quaternion.identity; node.tree.position=Vector3.zero;
            int feedback=0; tree.FeedbackRequested+=(kind,point,gain)=>feedback++;
            tree.Present(node);
            Assert.IsFalse(tree.Body.isKinematic); Assert.IsNotNull(tree.Connection);
            Assert.Greater(tree.Connection.breakTorque,0); Assert.AreEqual(0,feedback);
            Assert.IsFalse(tree.CanCollect); Assert.IsNull(node.pickup);
        }
        [Test] public void EveryProgressiveDepthSelectsMatchingTopAndStumpMeshes()
        {
            profile.cutDepths=new float[TreeStructure.ProgressiveLevels];
            profile.cutTops=new Mesh[TreeStructure.Sides*TreeStructure.ProgressiveLevels];
            profile.cutStumps=new Mesh[profile.cutTops.Length];
            for(int level=0;level<TreeStructure.ProgressiveLevels;level++) profile.cutDepths[level]=TreeStructure.ProgressiveDepth(level+1);
            for(int i=0;i<profile.cutTops.Length;i++) { profile.cutTops[i]=mesh; profile.cutStumps[i]=mesh; }
            var distinct=Object.Instantiate(mesh);
            try
            {
                int target=3*TreeStructure.ProgressiveLevels+7; profile.cutTops[target]=distinct; profile.cutStumps[target]=distinct;
                node.tree.version=2; node.tree.cutStep=.125f; node.tree.side=3; node.tree.cutUnits=8; node.tree.depth=.5f; node.revision=4;
                tree.Present(node);
                Assert.AreSame(distinct,tree.topMesh.sharedMesh); Assert.AreSame(distinct,tree.stumpMesh.sharedMesh);
                Assert.IsTrue(tree.topMesh.GetComponent<Renderer>().enabled); Assert.IsTrue(tree.stumpMesh.GetComponent<Renderer>().enabled);
                Assert.Greater((tree.Connection.connectedAnchor-Vector3.up*profile.cutHeight).magnitude,0);
                for(int units=1;units<=TreeStructure.ProgressiveLevels;units++)
                { node.tree.depth=TreeStructure.ProgressiveDepth(units); Assert.AreEqual(3*TreeStructure.ProgressiveLevels+units-1,profile.Index(node.tree)); }
            }
            finally { Object.DestroyImmediate(distinct); }
        }
    }
}
