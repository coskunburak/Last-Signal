#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LastSignal.Gathering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Profiling;
using Object=UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed partial class P01TreeStageTests
    {
        static void CaptureCamera(Camera camera,string path)
        {
            if(Application.isBatchMode) return; // Batch gates validate logic; live MCP supplies rendered evidence.
            var previous=camera.targetTexture; var active=RenderTexture.active;
            var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally { camera.targetTexture=previous; RenderTexture.active=active; RenderTexture.ReleaseTemporary(target); Object.Destroy(pixels); }
        }
        void CaptureOverview(Camera source,string path)
        {
            var go=new GameObject("S004 evidence camera");
            try
            {
                var camera=go.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled=false;
                camera.fieldOfView=65;
                camera.transform.position=view.transform.position+new Vector3(9,5,10);
                camera.transform.LookAt(view.transform.position+Vector3.up*2.6f);
                CaptureCamera(camera,path);
            }
            finally { Object.Destroy(go); }
        }
        [UnityTest] public IEnumerator NaturalAttackTimingRecordsNotchFallAudioAndEditorFrameSamples()
        {
            string folder=SessionState.GetString("LastSignal.S004.VerificationRun","");
            if(string.IsNullOrEmpty(folder)) folder=Environment.GetEnvironmentVariable("LASTSIGNAL_S004_EVIDENCE");
            if(string.IsNullOrEmpty(folder)) folder="Docs/Implementation/BLD/P01/S004/Evidence/TEST-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N");
            folder+="/live-capture"; Directory.CreateDirectory(folder);
            var physical=view.physicsBody; var actor=flow.Player;
            var look=actor.GetComponent<FirstPersonLook>(); Assert.IsNotNull(look);
            var combat=actor.GetComponent<PlayerCombatController>(); Assert.IsTrue(combat.SelectMeleeWeapon(MeleeWeaponDefinition.AxeId));
            var axe=combat.Melee; var audio=gathering.GetComponent<LastSignal.Audio.ProductionAudio>();
            Assert.IsTrue(audio && audio.Active); Assert.IsTrue(physical.audioBank.CanPlay(physical.gameObject.scene.path));
            var cues=new List<TreeAudioEvent>();
            var voices=audio.GetComponentsInChildren<AudioSource>();
            var playedClips=new HashSet<string>();
            Action<TreeAudioEvent,Vector3,float> handler=(kind,point,gain)=>cues.Add(kind);
            physical.FeedbackRequested+=handler;
            var frames=new List<string> {"phase,frame_ms,editor_gc_bytes,physics_simulate_ns"};
            int oldCap=Application.targetFrameRate, oldVsync=QualitySettings.vSyncCount;
            QualitySettings.vSyncCount=0; Application.targetFrameRate=60;
            var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            var physics=ProfilerRecorder.StartNew(ProfilerCategory.Physics,"Physics.Simulate",1);
            Action<string> sample=phase=>
            {
                frames.Add(phase+","+(Time.unscaledDeltaTime*1000).ToString("F4",CultureInfo.InvariantCulture)+","+(gc.Valid?gc.LastValue:-1)+","+(physics.Valid?physics.LastValue:-1));
                foreach(var voice in voices) if(voice && voice.isPlaying && voice.clip) playedClips.Add(voice.clip.name);
            };
            try
            {
                for(int frame=0;frame<120;frame++) { yield return null; sample("standing"); }
                int hits=0, audible=0;
                while(!State().tree.fractured && State().tree.depth<.975f && hits<TreeStructure.ProgressiveLevels+2)
                {
                    var cc=actor.GetComponent<CharacterController>(); cc.enabled=false;
                    actor.transform.position=view.transform.position+view.transform.right*(physical.profile.radius+1.2f)+Vector3.up*.1f; cc.enabled=true;
                    for(int step=0;step<20;step++) yield return new WaitForFixedUpdate();
                    Vector3 aim=AimPoint(physical,axe.Eye.position,axe.Definition.Range);
                    Vector3 direction=aim-axe.Eye.position;
                    actor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,Vector3.up));
                    direction=aim-axe.Eye.position;
                    look.ApplyRecoil(-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg-look.Pitch,0);
                    actor.GetComponent<PlayerStamina>().ResetSession(); Physics.SyncTransforms();
                    long before=State().revision; int cueBefore=audio.CueCount;
                    if(hits==0) CaptureCamera(look.View,folder+"/00-standing-fps.png");
                    Assert.IsTrue(axe.TryAttack(),axe.LastRejectionReason);
                    float deadline=Time.realtimeSinceStartup+5;
                    while(axe.Simulation.State!=MeleeState.Ready && Time.realtimeSinceStartup<deadline)
                    { yield return null; sample("cutting"); }
                    Assert.AreEqual(MeleeState.Ready,axe.Simulation.State,"Natural Update attack did not recover.");
                    if(axe.LastMeleeOutcome!=MeleeTargetOutcome.HarvestHit)
                    {
                        bool ray=physical.trunkCollider.Raycast(new Ray(axe.Eye.position,axe.Eye.forward),out var contact,axe.Definition.Range);
                        Debug.Log("S004_NATURAL_AIM eye="+axe.Eye.position+" forward="+axe.Eye.forward+" aim="+aim+" ray="+ray+" point="+view.transform.InverseTransformPoint(contact.point)+" pitch="+look.Pitch+" state="+flow.Screen);
                        CaptureCamera(look.View,folder+"/failed-aim.png");
                    }
                    Assert.AreEqual(MeleeTargetOutcome.HarvestHit,axe.LastMeleeOutcome,axe.LastMeleeResult);
                    Assert.Greater(State().revision,before); hits++;
                    if(audio.CueCount>cueBefore) audible++;
                    AssertVisible();
                    if(hits<=3)
                    {
                        // Let the procedural weapon LateUpdate finish before rendering its pose.
                        if(!Application.isBatchMode) yield return new WaitForEndOfFrame();
                        else yield return new WaitForFixedUpdate();
                        CaptureCamera(look.View,folder+"/0"+hits+"-notch-fps.png");
                    }
                }
                for(int frame=0;frame<300 && !State().tree.fractured;frame++) { yield return null; sample("weakened"); }
                Assert.IsTrue(State().tree.fractured); Assert.AreEqual(1,State().yieldSequence);
                CaptureOverview(look.View,folder+"/04-fracture.png");
                bool middle=false; float stop=Time.realtimeSinceStartup+45;
                while(!physical.CanCollect && Time.realtimeSinceStartup<stop)
                {
                    yield return null; sample("falling");
                    if(!middle && Vector3.Angle(physical.transform.up,Vector3.up)>35)
                    { CaptureOverview(look.View,folder+"/05-falling.png"); middle=true; }
                }
                Assert.IsTrue(physical.CanCollect); AssertVisible();
                CaptureOverview(look.View,folder+"/06-resting.png");
                Assert.Greater(Vector3.Angle(physical.transform.up,Vector3.up),60,"Released trunk remained propped upright by its stump collider.");
                File.WriteAllText(folder+"/events.txt","hits="+hits+" audibleStrikeWindows="+audible+" yield="+State().pickup.id+"\n"+string.Join("\n",cues)+"\nPLAYING CLIPS\n"+string.Join("\n",playedClips));
                Assert.AreEqual(hits,cues.FindAll(e=>e==TreeAudioEvent.WoodHit).Count);
                Assert.AreEqual(1,cues.FindAll(e=>e==TreeAudioEvent.JointFailure).Count);
                Assert.AreEqual(1,cues.FindAll(e=>e==TreeAudioEvent.GroundImpact).Count);
                Assert.Greater(audible,0,"Accepted strikes did not dispatch audible pool voices.");
                Assert.Contains(physical.audioBank.woodHit.name,new List<string>(playedClips));
                Assert.Contains(physical.audioBank.fracture.name,new List<string>(playedClips));
                Assert.Contains(physical.audioBank.groundImpact.name,new List<string>(playedClips));
                for(int frame=0;frame<120;frame++) { yield return null; sample("resting"); }
            }
            finally
            {
                physical.FeedbackRequested-=handler; Application.targetFrameRate=oldCap; QualitySettings.vSyncCount=oldVsync;
                File.WriteAllLines(folder+"/editor-frames.csv",frames);
                File.WriteAllText(folder+"/measurement-scope.txt","Unity Editor "+Application.unityVersion+"\n"+SystemInfo.processorType+"\n"+SystemInfo.graphicsDeviceName+"\n60 FPS target, vsync disabled, fixedDeltaTime="+Time.fixedDeltaTime+"\nGC recorder valid="+gc.Valid+" physics recorder valid="+physics.Valid+"\nWhole Editor frame samples; allocations include test instrumentation and Editor. Capture-adjacent frames included. Not a standalone player benchmark or production performance acceptance.");
                gc.Dispose(); physics.Dispose();
            }
        }
    }
}
#endif
