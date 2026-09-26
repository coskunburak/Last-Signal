#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace LastSignal.Tests
{
    public class ZombieProductionAcceptanceTests
    {
        const string Evidence="Docs/Implementation/S004/Evidence/20260919-P4/regression-P2";
        SessionFlow session;
        readonly List<GameObject> owned=new List<GameObject>();
        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/ZombieAcceptance.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;session=Object.FindAnyObjectByType<SessionFlow>();session.Resume();
            session.Player.GetComponent<FirstPersonMotor>().enabled=false;
        }
        void Place(Vector3 position)
        {var c=session.Player.GetComponent<CharacterController>();c.enabled=false;session.Player.transform.position=position;c.enabled=true;Physics.SyncTransforms();}
        IEnumerator Until(ZombieController z,ZombieState state,float limit=4)
        {float end=Time.realtimeSinceStartup+limit;while(z.Runtime.State!=state&&Time.realtimeSinceStartup<end)yield return null;Assert.That(z.Runtime.State,Is.EqualTo(state));}
        void Capture(string directory,string label,ZombieController z)
        {
            var go=new GameObject("OwnedEvidenceCamera");owned.Add(go);var cam=go.AddComponent<Camera>();cam.enabled=false;
            cam.transform.position=z.transform.position+new Vector3(-4,3,-5);cam.transform.LookAt(z.transform.position+Vector3.up);
            var rt=RenderTexture.GetTemporary(960,640,24);var previous=RenderTexture.active;
            var texture=new Texture2D(960,640,TextureFormat.RGB24,false);
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,960,640),0,0);texture.Apply();File.WriteAllBytes(directory+"/"+label+".png",texture.EncodeToPNG());}
            finally{cam.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(go);owned.Remove(go);}
            File.AppendAllText(directory+"/sequence.txt",label+" state="+z.Runtime.State+" position="+z.transform.position+" memory="+z.Runtime.LastKnownPosition+" visible="+z.Runtime.Visible+" searchPoint="+z.Search.PointIndex+" velocity="+z.Navigation.Velocity+"\n");
        }
        [UnityTest]public IEnumerator ProductionVisualSequenceAndStableMeleeStop()
        {
            yield return Load();var z=session.GetComponent<ZombieEncounter>().Actor;
            string dir=Evidence+"/visual-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(dir);
            Place(new Vector3(-5,0,-8));yield return new WaitForSeconds(.6f);Assert.That(z.Runtime.State,Is.EqualTo(ZombieState.Idle));Capture(dir,"A-B-idle-outside-fov",z);
            Place(new Vector3(-5,0,7));yield return Until(z,ZombieState.Chasing);Capture(dir,"C-acquisition",z);
            var start=z.transform.position;yield return new WaitForSeconds(3);Assert.That(Vector3.Distance(start,z.transform.position),Is.GreaterThan(1));Capture(dir,"D-walk",z);
            yield return new WaitForSeconds(5);Capture(dir,"E-obstacle-route",z);
            Assert.That(Mathf.Abs(z.transform.position.x+5),Is.GreaterThan(.3f),"Actor must detour around crate");
            Place(new Vector3(3,0,4));yield return Until(z,ZombieState.Searching);var memory=z.Runtime.LastKnownPosition;Capture(dir,"F-G-corner-memory",z);
            Place(new Vector3(7,0,6));yield return new WaitForSeconds(.3f);Assert.That(z.Runtime.LastKnownPosition,Is.EqualTo(memory));
            session.Pause();var paused=z.transform.position;float age=z.Runtime.SearchAge;yield return new WaitForSecondsRealtime(.3f);
            Assert.That(z.transform.position,Is.EqualTo(paused));Assert.That(z.Runtime.SearchAge,Is.EqualTo(age));Capture(dir,"K-search-pause",z);session.Resume();
            yield return new WaitForSeconds(5);Capture(dir,"H-L-search-resume",z);
            yield return Until(z,ZombieState.Idle,14);Capture(dir,"I-search-timeout",z);
            Place(z.transform.position+z.transform.forward*3);yield return Until(z,ZombieState.Chasing);
            Place(new Vector3(100,0,100));yield return Until(z,ZombieState.Searching);
            Place(z.transform.position+z.transform.forward*3);yield return Until(z,ZombieState.Chasing);Capture(dir,"J-reacquisition",z);
            // Known open walkable lane; the forward reveal point can lie inside the low crate.
            Place(new Vector3(-8,0,4));
            float stopDeadline=Time.realtimeSinceStartup+7;while(!z.Attacking&&Time.realtimeSinceStartup<stopDeadline)yield return null;
            Assert.That(z.Attacking,Is.True);var stopped=z.transform.position;
            yield return new WaitForSeconds(.5f);Assert.That(Vector3.Distance(stopped,z.transform.position),Is.LessThan(.08f));Capture(dir,"stable-stop-melee",z);
            Assert.That(session.Player.GetComponent<PlayerHealth>(),Is.Not.Null);
            session.ReturnToMenu();yield return null;session.BeginSession();session.Resume();yield return null;
            z=session.GetComponent<ZombieEncounter>().Actor;Assert.That(z.Target,Is.EqualTo(session.Player));Capture(dir,"M-new-session",z);
        }
        [UnityTest]public IEnumerator ProfileOneTenAndTwentyFiveProductionActors()
        {
            yield return Load();session.GetComponent<ZombieEncounter>().End();Place(new Vector3(-5,0,7));
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab");
            var report=new StringBuilder("Editor technical smoke; not shipping density certification. Per-frame totals after 3 seconds warmup, >=5 seconds and >=300 sampled frames. GC counter includes entire Editor/test harness; direct ManagedTickBytes separately counts synchronous AI ticks. CPU values are nanoseconds converted to ms; nested markers overlap.\n");
            foreach(int count in new[]{1,10,25})
            {
                var actors=new ZombieController[count];
                for(int i=0;i<count;i++)
                {var go=Object.Instantiate(source,new Vector3(-8+(i%5)*1.3f,0,-5+(i/5)*1.3f),Quaternion.identity);owned.Add(go);actors[i]=go.GetComponent<ZombieController>();Assert.That(actors[i].Initialize(),Is.True);Assert.That(actors[i].Bind(session.Player),Is.True);}
                using(var ai=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"LastSignal.Zombie.AI",1))
                using(var perception=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"LastSignal.Zombie.Perception",1))
                using(var nav=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"LastSignal.Zombie.Navigation",1))
                using(var presentation=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"LastSignal.Zombie.Presentation",1))
                using(var animator=ProfilerRecorder.StartNew(ProfilerCategory.Animation,"Animators.Update",1))
                using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1))
                {
                    yield return new WaitForSeconds(3);
                    long a=0,p=0,n=0,v=0,an=0,g=0,maxa=0,maxg=0;float maxFrame=0,elapsed=0;int before=0;
                    foreach(var actor in actors){before+=actor.Navigation.PathRequests;actor.MeasureManagedAllocations=true;}
                    int frames=0;float nextMove=0;
                    while(frames<300||elapsed<5)
                    {yield return null;frames++;a+=ai.LastValue;p+=perception.LastValue;n+=nav.LastValue;v+=presentation.LastValue;an+=animator.LastValue;g+=gc.LastValue;maxa=Math.Max(maxa,ai.LastValue);maxg=Math.Max(maxg,gc.LastValue);maxFrame=Math.Max(maxFrame,Time.unscaledDeltaTime);elapsed+=Time.unscaledDeltaTime;
                        if(elapsed>=nextMove){Place(new Vector3(-5+Mathf.Sin(elapsed)*2,0,7));nextMove=elapsed+.5f;}}
                    int requests=-before;foreach(var actor in actors)requests+=actor.Navigation.PathRequests;
                    long tickBytes=0;int ticks=0;foreach(var actor in actors){tickBytes+=actor.ManagedTickBytes;ticks+=actor.MeasuredTicks;}
                    double divisor=frames*1e6;
                    report.AppendLine("count="+count+" frames="+frames+" AI_ms="+a/divisor+" perception_ms="+p/divisor+" nav_ms="+n/divisor+" presentation_ms="+v/divisor+" Animator_ms="+an/divisor+" AnimatorRecorderValid="+animator.Valid+" AI_max_ms="+maxa/1e6+" GC_mean_B="+g/frames+" GC_max_B="+maxg+" max_frame_ms="+maxFrame*1000+" requests="+requests+" seconds="+elapsed+" requests_per_actor_second="+requests/(elapsed*count)+" marker_valid="+ai.Valid+","+perception.Valid+","+nav.Valid+","+gc.Valid+" direct_AI_bytes="+tickBytes+" measured_ticks="+ticks);
                }
                foreach(var actor in actors){owned.Remove(actor.gameObject);Object.DestroyImmediate(actor.gameObject);}yield return null;
            }
            File.WriteAllText(Evidence+"/performance-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".txt",report.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){foreach(var go in owned)if(go)Object.DestroyImmediate(go);owned.Clear();if(session)session.ReturnToMenu();Time.timeScale=1;yield return null;}
    }
}
#endif
