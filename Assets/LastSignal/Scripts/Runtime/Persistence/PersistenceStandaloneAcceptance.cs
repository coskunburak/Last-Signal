#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LastSignal.Persistence
{
    public sealed class PersistenceStandaloneAcceptance : MonoBehaviour
    {
        string evidence;
        bool worldTime;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            var args=Environment.GetCommandLineArgs();
            int flag=Array.IndexOf(args,"-persistenceAcceptance");
            bool timeRoute = false;
            bool popRoute = false;
            if(flag<0) { flag=Array.IndexOf(args,"-worldTimeAcceptance");timeRoute=true; }
            if(flag<0) { flag=Array.IndexOf(args,"-worldPopulationAcceptance");timeRoute=false;popRoute=true; }
            if(flag<0 || flag+1>=args.Length) return;
            var go=new GameObject("Persistence standalone acceptance");
            var driver=go.AddComponent<PersistenceStandaloneAcceptance>();driver.evidence=Path.GetFullPath(args[flag+1]);driver.worldTime=timeRoute;driver.popRoute=popRoute;
        }
        bool popRoute;
        IEnumerator Start()
        {
            Directory.CreateDirectory(evidence);yield return null;
            var saves=FindAnyObjectByType<SaveSession>();
            var report=new StringBuilder(); Exception failure=null;
            if(!saves) failure=new InvalidOperationException("Persistence scene not loaded.");
            var stack=new Stack<IEnumerator>();
            if(saves) stack.Push(popRoute ? LastSignal.AI.WorldPopulationAcceptanceRoute.Run(saves.GetComponent<LastSignal.WorldTime.WorldClock>(),Path.Combine(evidence,"route-save.json"),line=>report.AppendLine(line)) : (worldTime ? LastSignal.WorldTime.WorldTimeAcceptanceRoute.Run(saves.GetComponent<LastSignal.WorldTime.WorldClock>(),Path.Combine(evidence,"route-save.json"),line=>report.AppendLine(line)) : PersistenceAcceptanceRoute.Run(saves,Path.Combine(evidence,"route-save.json"),line=>report.AppendLine(line))));
            while(stack.Count>0 && failure==null)
            {
                object current=null;bool advanced=false;
                try {advanced=stack.Peek().MoveNext();if(advanced)current=stack.Peek().Current;}
                catch(Exception e){failure=e;}
                if(failure!=null)break;
                if(!advanced){stack.Pop();continue;}
                if(current is IEnumerator nested){stack.Push(nested);continue;}
                yield return current;
            }
            report.AppendLine(failure==null?(popRoute ? "PASS standalone P02-GAP population and pressure integration." : (worldTime ? "PASS standalone P02-GAP two expeditions, weather, sleep and save/load." : "PASS standalone production Save -> menu -> Load (3 sessions) and post-load combat/re-pickup.")):"FAIL "+failure);
            File.WriteAllText(Path.Combine(evidence,"standalone-save-load.txt"),report.ToString());
            if(failure==null)Debug.Log("PERSISTENCE_STANDALONE_PASS");else Debug.LogError("PERSISTENCE_STANDALONE_FAIL "+failure);
            Application.Quit(failure==null?0:1);
        }
    }
}
#endif
