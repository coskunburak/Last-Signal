#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastSignal.Gathering;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace LastSignal.Tests
{
    /// <summary>Explicit authored-stage gate. Requires the owner's generated AND clearance-approved S004 scene.</summary>
    public sealed partial class P01TreeStageTests
    {
        SessionFlow flow; SaveSession saves; GatheringWorld gathering; ResourceNodeView view;
        string nodeId;
        [UnitySetUp] public IEnumerator Setup()
        {
            string path=Environment.GetEnvironmentVariable("LASTSIGNAL_S004_STAGE");
            Assert.IsTrue(!string.IsNullOrEmpty(path) && path.StartsWith("Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_",StringComparison.Ordinal) && File.Exists(path),
                "Supply an owner-authored S004 stage with --stage. This gate never authors or promotes a scene.");
            Time.timeScale=1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            gathering=Object.FindAnyObjectByType<GatheringWorld>(); Assert.IsNotNull(gathering);
            Assert.IsTrue(gathering.clearanceApproved,"Owner must review road/navigation/drop clearances and save approval in this isolated stage.");
            view=gathering.residentViews.Single(v=>v && v.physicsBody); nodeId=view.nodeId;
            flow=gathering.GetComponent<SessionFlow>(); saves=gathering.GetComponent<SaveSession>();
            var saveSettings=new SerializedObject(saves); saveSettings.FindProperty("saveSlot").stringValue="s004_test_"+Guid.NewGuid().ToString("N"); saveSettings.ApplyModifiedPropertiesWithoutUndo();
            var population=gathering.GetComponent<LastSignal.AI.WorldPopulationManager>(); if(population) population.MaterializationEnabled=false;
            flow.BeginSession(); flow.Resume(); yield return null;
            foreach(var encounter in flow.ResidentEncounters) encounter.SetPaused(true);
            Assert.IsTrue(gathering.Ready,gathering.Feedback);
            Debug.Log("S004_INTEGRATION_CHECKPOINT "+saves.DefaultPath+" (unique test slot; preserved for evidence)");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if(flow) flow.ReturnToMenu(); Time.timeScale=1; yield return null; }
        ResourceNodeState State() => GatheringState.Find(gathering.Capture(),nodeId);
        Vector3 AimPoint(TreePhysicsBody physical,Vector3 eye,float range)
        {
            // Find a real surface contact within the authored cut band. Different radii,
            // terrain heights and capsule end caps cannot share an AABB-corner aim point.
            foreach(float height in new[]{.05f,.1f,.15f}) foreach(float radial in new[]{.25f,.5f,.75f})
            {
                var aim=view.transform.TransformPoint(new Vector3(physical.profile.radius*radial,physical.profile.cutHeight+height,0));
                if(physical.trunkCollider.Raycast(new Ray(eye,aim-eye),out var hit,range) && physical.Accepts(physical.trunkCollider,hit.point)) return aim;
            }
            return view.transform.TransformPoint(new Vector3(physical.profile.radius*.3f,physical.profile.cutHeight+.05f,0));
        }
        void Strike(bool accepted=true,TreePhysicsBody aimingBody=null)
        {
            var physical=aimingBody?aimingBody:view.physicsBody;
            var actor=flow.Player; var cc=actor.GetComponent<CharacterController>(); if(cc) cc.enabled=false;
            actor.transform.position=view.transform.position+view.transform.right*(physical.profile.radius+1.2f)+Vector3.up*.1f;
            if(Physics.Raycast(actor.transform.position+Vector3.up*2,Vector3.down,out var floor,5,~((1<<8)|(1<<2)),QueryTriggerInteraction.Ignore))
                actor.transform.position=new Vector3(actor.transform.position.x,floor.point.y+.04f,actor.transform.position.z);
            if(cc) cc.enabled=true;
            actor.GetComponent<PlayerStamina>().ResetSession(); // Isolate structural iteration from separately tested stamina regeneration.
            var combat=actor.GetComponent<PlayerCombatController>(); Assert.IsTrue(combat.SelectMeleeWeapon(MeleeWeaponDefinition.AxeId));
            var axe=combat.Melee;
            var aim=AimPoint(physical,axe.Eye.position,axe.Definition.Range);
            axe.Eye.rotation=Quaternion.LookRotation(aim-axe.Eye.position,Vector3.up);
            Physics.SyncTransforms(); long before=State().revision;
            Assert.IsTrue(axe.TryAttack(),axe.LastRejectionReason);
            axe.Simulation.Tick(axe.Definition.WindupSeconds+.001f);
            if(accepted && axe.LastMeleeOutcome!=MeleeTargetOutcome.HarvestHit)
            {
                bool ray=physical.trunkCollider.Raycast(new Ray(axe.Eye.position,axe.Eye.forward),out var contact,axe.Definition.Range);
                Debug.Log("S004_CONTACT_DIAGNOSTIC eye="+axe.Eye.position+" origin="+axe.Origin.position+" aim="+aim+
                    " range="+axe.Definition.Range+" ray="+ray+" point="+view.transform.InverseTransformPoint(contact.point)+
                    " valid="+physical.Valid+" accepts="+(ray&&physical.Accepts(physical.trunkCollider,contact.point))+
                    " bounds="+physical.trunkCollider.bounds+" bodyPos="+physical.Body.position+" nodePos="+view.transform.position);
            }
            Assert.AreEqual(accepted?MeleeTargetOutcome.HarvestHit:MeleeTargetOutcome.Rejected,axe.LastMeleeOutcome,axe.LastMeleeResult+" / "+gathering.Feedback);
            if(!accepted) Assert.AreEqual("Harvest:PhysicsNotReady",axe.LastMeleeResult);
            Assert.AreEqual(before+(accepted?1:0),State().revision); axe.Simulation.Tick(10);
        }
        IEnumerator Reload()
        {
            Assert.IsTrue(saves.Save().Success,saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null;
            yield return saves.Load(); Assert.IsTrue(saves.LastResult.Success,saves.LastResult.Message);
            flow.Resume(); Assert.IsTrue(gathering.Ready,gathering.Feedback);
        }
        [UnityTest] public IEnumerator ActualAxeAndJointCommitSurviveMidFallSaveReloadExactlyOnce()
        {
            Strike(); Assert.AreEqual(1,State().revision); Assert.IsNull(State().pickup);
            yield return Reload(); Assert.AreEqual(1,State().revision); Assert.IsFalse(State().tree.fractured);
            for(int hit=0;hit<TreeStructure.ProgressiveLevels+2 && !State().tree.fractured && State().tree.depth<.975f;hit++)
            { Strike(); for(int step=0;step<3;step++) yield return new WaitForFixedUpdate(); }
            for(int step=0;step<100 && !State().tree.fractured;step++) yield return new WaitForFixedUpdate();
            var failed=State(); Assert.IsTrue(failed.tree.fractured,"Actual joint did not fail under the authored support model. Do not replace with a scripted fall.");
            Assert.AreEqual(1,failed.yieldSequence); Assert.AreEqual(1,failed.tree.fractureGeneration);
            Assert.IsNull(view.physicsBody.Connection); Assert.IsFalse(view.physicsBody.Body.isKinematic,"Expected a mid-fall checkpoint.");
            yield return Reload(); var restored=State();
            Assert.IsTrue(restored.tree.fractured); Assert.AreEqual(failed.revision,restored.revision);
            Assert.AreEqual(failed.pickup.id,restored.pickup.id); Assert.AreEqual(failed.pickup.quantity,restored.pickup.quantity);
            for(int i=0;i<1800 && !view.physicsBody.CanCollect;i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(view.physicsBody.CanCollect,"Owner must inspect ground/obstacle contact if tree does not settle.");
            Assert.AreEqual(1,State().yieldSequence); Assert.AreEqual(failed.pickup.id,State().pickup.id);
        }
        [UnityTest] public IEnumerator PartialCutDisableReloadRetainsRevisionAndDoesNotMintYield()
        {
            Strike(); var before=State();
            view.gameObject.SetActive(false); yield return new WaitForFixedUpdate(); view.gameObject.SetActive(true);
            yield return null;
            Assert.AreEqual(before.revision,State().revision); Assert.IsNull(State().pickup);
            yield return Reload(); Assert.AreEqual(before.tree.depth,State().tree.depth);
            Assert.AreEqual(before.revision,State().revision); Assert.IsNull(State().pickup);
        }
        static int RegressionChops()
        {
            var setting=Environment.GetEnvironmentVariable("LASTSIGNAL_S004_CHOPS");
            if(string.IsNullOrEmpty(setting)) return 4; // Test scenario only; no production four-hit release rule.
            Assert.IsTrue(int.TryParse(setting,out int count) && count>=1 && count<=32,"--chops must be 1..32.");
            return count;
        }
        void AssertVisible()
        {
            var body=view.physicsBody;
            Assert.IsTrue(body.gameObject.activeInHierarchy);
            Assert.IsTrue(body.topMesh.GetComponent<Renderer>().enabled,"Trunk disappeared during resource transition.");
            Assert.IsTrue(body.stumpMesh.gameObject.activeInHierarchy);
            Assert.IsTrue(body.stumpMesh.GetComponent<Renderer>().enabled,"Stump disappeared.");
            Assert.IsNotNull(body.Body); Assert.IsNotNull(body.topMesh.sharedMesh);
        }
        [UnityTest] public IEnumerator ConfiguredChopsNeverDeleteTreeAndActualFractureRetainsOneYield()
        {
            int budget=RegressionChops(), hits=0;
            while(hits<Mathf.Max(budget,TreeStructure.ProgressiveLevels+2) && !State().tree.fractured && State().tree.depth<.975f)
            {
                var prior=view.physicsBody.topMesh.sharedMesh; float depth=State().tree.depth;
                Strike(); hits++; AssertVisible();
                if(State().tree.depth>depth) Assert.AreNotSame(prior,view.physicsBody.topMesh.sharedMesh,"Accepted cut did not advance its visible notch.");
                Assert.IsNull(State().pickup,"A cut cannot mint yield before solver failure.");
                for(int step=0;step<3;step++) yield return new WaitForFixedUpdate();
                AssertVisible();
                if(hits==budget) Debug.Log("S004_DISAPPEARANCE_REGRESSION chops="+hits+" visible=true fractured="+State().tree.fractured);
            }
            for(int step=0;step<100 && !State().tree.fractured;step++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(State().tree.fractured,"Joint must fail under the actual profile; do not replace with an animated fall.");
            Assert.IsNull(view.physicsBody.Connection); Assert.IsFalse(view.physicsBody.Body.isKinematic);
            Assert.AreEqual(1,State().yieldSequence); var yieldId=State().pickup.id;
            for(int step=0;step<1800 && !view.physicsBody.CanCollect;step++) yield return new WaitForFixedUpdate();
            AssertVisible(); Assert.IsTrue(view.physicsBody.CanCollect);
            Assert.IsTrue(gathering.RetryReward(view),gathering.Feedback);
            Assert.AreEqual(RewardLocation.World,State().reward); Assert.AreEqual(yieldId,State().pickup.id);
            Assert.IsFalse(gathering.RetryReward(view)); Assert.AreEqual(1,State().yieldSequence);
            yield return Reload(); AssertVisible(); Assert.AreEqual(yieldId,State().pickup.id);
        }
        [UnityTest] public IEnumerator MissingPhysicalBindingCannotUseLegacyFourHitDeletion()
        {
            var physical=view.physicsBody; long revision=State().revision; int condition=gathering.Capture().axeCondition;
            view.physicsBody=null;
            try
            {
                for(int i=0;i<RegressionChops();i++) Strike(false,physical);
                Assert.AreEqual(revision,State().revision); Assert.AreEqual(condition,gathering.Capture().axeCondition);
                Assert.IsNull(State().pickup); Assert.IsTrue(physical.topMesh.GetComponent<Renderer>().enabled);
            }
            finally { view.physicsBody=physical; }
            yield return null;
        }
    }
}
#endif
