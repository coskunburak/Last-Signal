#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LastSignal.WorldTime;
using LastSignal.WorldCells;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
namespace LastSignal.Slice
{
    public sealed class S012Performance : MonoBehaviour
    {
        string output;SessionFlow flow;int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-s012Performance")new GameObject("S012 performance capture").AddComponent<S012Performance>().output=args[i+1];
        }
        void Observe(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        IEnumerator Start()
        {
            if(output==null)yield break;Directory.CreateDirectory(output);Application.logMessageReceived+=Observe;
            yield return null;flow=FindAnyObjectByType<SessionFlow>();
            File.WriteAllText(Path.Combine(output,"environment.txt"),$"Unity={Application.unityVersion}\nEditor={Application.isEditor}\nDevelopment={Debug.isDebugBuild}\nOS={SystemInfo.operatingSystem}\nDevice={SystemInfo.deviceModel}\nCPU={SystemInfo.processorType}\nGPU={SystemInfo.graphicsDeviceName}\nRAM_MB={SystemInfo.systemMemorySize}\nResolution={Screen.width}x{Screen.height}\nVSync={QualitySettings.vSyncCount}\nTargetFrameRate={Application.targetFrameRate}\nRoute=southwest open lane, 60 s real motor traversal per condition; 5 s warmup excluded. Environmental snapshot + starting position are explicit fixtures. No combat/streaming in steady-state sample.\n");
            var stack=new Stack<IEnumerator>();stack.Push(Run());Exception failure=null;
            while(stack.Count>0){bool next=false;object current=null;try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){failure=e;break;}if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;}
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Application.logMessageReceived-=Observe;
            File.WriteAllText(Path.Combine(output,"result.txt"),$"{(failure==null&&errors==0?"PASS capture completed; not a performance-budget PASS":"FAIL")}\nErrors={errors}\n{failure}");
            if(failure!=null)Debug.LogException(failure);Application.Quit(failure==null&&errors==0?0:1);
        }
        IEnumerator Run()
        {
            foreach(string condition in new[]{"day","night","rain"})
            {
                flow.ReturnToMenu();yield return null;flow.BeginSession();yield return null;flow.Resume();
                var clock=flow.GetComponent<WorldClock>();
                // Advance the live world before changing its presentation snapshot. Production and
                // population participants must reach the same timestamp as the restored clock.
                double target=condition=="night"?82800:43200;
                var advance=clock.Simulation.AdvanceUntil(target,0);
                if(advance.Reason!=AdvanceReason.Completed||clock.Simulation.Seconds!=target)
                    throw new InvalidOperationException("Performance clock setup did not reach its target.");
                var snapshot=clock.Capture();
                snapshot.settings.worldSecondsPerRealSecond=1;
                snapshot.weatherStarted=snapshot.seconds-300;snapshot.nextWeather=snapshot.weatherStarted+3600;snapshot.weather=condition=="rain"?1:0;
                clock.Restore(snapshot);WorldTimeAcceptanceRoute.Place(flow.Player,new Vector3(-330,.05f,-160));
                flow.Player.transform.rotation=Quaternion.identity;flow.Player.GetComponent<FirstPersonLook>().View.transform.rotation=Quaternion.identity;
                var motor=flow.Player.GetComponent<FirstPersonMotor>();motor.enabled=false;
                yield return new WaitForSeconds(5);
                if(errors!=0)throw new InvalidOperationException("Performance warmup logged an error; capture is invalid.");
                var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
                var recorders=new Dictionary<string,ProfilerRecorder>();
                foreach(var handle in handles)
                {
                    var d=ProfilerRecorderHandle.GetDescription(handle);
                    if(d.Name=="Main Thread"||d.Name=="Render Thread"||d.Name=="GPU Frame Time"||d.Name=="GC Allocated In Frame"||d.Name=="Draw Calls Count"||d.Name=="Batches Count"||d.Name=="Total Used Memory"||d.Name=="GC Used Memory")
                        if(!recorders.ContainsKey(d.Name))recorders.Add(d.Name,ProfilerRecorder.StartNew(d.Category,d.Name,1));
                }
                var values=new Dictionary<string,List<long>>();foreach(var key in recorders.Keys)values[key]=new List<long>(10000);
                var frames=new List<double>(10000);int[] gc={GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};
                double start=Time.realtimeSinceStartupAsDouble,previous=start;bool north=true;
                try
                {
                    while(Time.realtimeSinceStartupAsDouble-start<60)
                    {
                        if(errors!=0)throw new InvalidOperationException("Performance sample logged an error; capture is invalid.");
                        if(flow.Paused)throw new InvalidOperationException("Performance route paused; invalid comparable capture.");
                        float z=flow.Player.transform.position.z;if(z>-90)north=false;else if(z<-160)north=true;
                        flow.Player.transform.rotation=Quaternion.Euler(0,north?0:180,0);motor.Simulate(Vector2.up,false,Time.deltaTime);
                        yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-previous)*1000);previous=now;
                        foreach(var pair in recorders)if(pair.Value.Valid&&pair.Value.Count>0)values[pair.Key].Add(pair.Value.LastValue);
                    }
                    frames.Sort();double sum=0;foreach(var f in frames)sum+=f;
                    var lines=new List<string>{$"condition={condition}; frames={frames.Count}; avg_ms={sum/frames.Count:F4}; p50_ms={frames[(int)(frames.Count*.50)]:F4}; p95_ms={frames[(int)(frames.Count*.95)]:F4}; p99_ms={frames[(int)(frames.Count*.99)]:F4}",
                        $"GC collections delta={GC.CollectionCount(0)-gc[0]},{GC.CollectionCount(1)-gc[1]},{GC.CollectionCount(2)-gc[2]}",
                        $"ManagedUsed={Profiler.GetMonoUsedSizeLong()}; UnityAllocated={Profiler.GetTotalAllocatedMemoryLong()}; UnityReserved={Profiler.GetTotalReservedMemoryLong()}; Native-only memory=UNAVAILABLE (Unity total includes managed/native allocations)",
                        $"PopulationPhysical={flow.GetComponent<LastSignal.AI.WorldPopulationManager>().PhysicalCount}; PopulationAccounted={flow.GetComponent<LastSignal.AI.WorldPopulationManager>().TotalAccounted}; AuthoredResidentActors=1; StreamingTransitions=0"};
                    foreach(var name in new[]{"Main Thread","Render Thread","GPU Frame Time","GC Allocated In Frame","Draw Calls Count","Batches Count","Total Used Memory","GC Used Memory"})
                    {
                        if(!values.TryGetValue(name,out var v)||v.Count==0){lines.Add(name+"=UNAVAILABLE");continue;}
                        double total=0;foreach(var n in v)total+=n;lines.Add(name+" mean_raw="+(total/v.Count).ToString("F3",CultureInfo.InvariantCulture)+" samples="+v.Count+" (time counters ns; memory bytes; rendering counts)");
                    }
                    File.WriteAllLines(Path.Combine(output,condition+".txt"),lines);
                    var raw=new string[frames.Count+1];raw[0]="sorted_frame_ms";for(int i=0;i<frames.Count;i++)raw[i+1]=frames[i].ToString("R",CultureInfo.InvariantCulture);File.WriteAllLines(Path.Combine(output,condition+"-frames.csv"),raw);
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,condition+".png"));yield return null;
                }
                finally{foreach(var r in recorders.Values)r.Dispose();motor.enabled=true;}
            }
            var lifecycle=new List<string>{"cycle,managed_used,unity_allocated,unity_reserved,ready_listeners,noise_listeners"};
            for(int i=0;i<10;i++)
            {
                flow.ReturnToMenu();yield return null;flow.BeginSession();yield return null;flow.Resume();var cells=flow.GetComponent<WorldCellManager>();
                for(int j=0;j<5;j++)
                {
                    foreach(var id in new List<string>(cells.DefinedCellIds))
                    {
                        if(!cells.Request(id))throw new InvalidOperationException("Soak request");float until=Time.realtimeSinceStartup+15;
                        while(cells.State(id)!=CellState.Ready&&Time.realtimeSinceStartup<until)yield return null;
                        if(cells.State(id)!=CellState.Ready||!cells.Unload(id))throw new InvalidOperationException("Soak readiness/unload");yield return null;
                    }
                }
                yield return Resources.UnloadUnusedAssets();GC.Collect();yield return null;
                lifecycle.Add($"{i},{Profiler.GetMonoUsedSizeLong()},{Profiler.GetTotalAllocatedMemoryLong()},{Profiler.GetTotalReservedMemoryLong()},{cells.ReadyListenerCount},{flow.Noise.ListenerCount}");
                File.WriteAllLines(Path.Combine(output,"lifecycle.csv"),lifecycle);
            }
        }
    }
}
#endif
