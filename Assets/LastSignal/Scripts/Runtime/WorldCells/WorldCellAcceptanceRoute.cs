using System;
using System.Collections;
using System.IO;
using System.Text;
using LastSignal.Inventory;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastSignal.WorldCells
{
    public static class WorldCellAcceptanceRoute
    {
        public const string A="cell:1:0", B="cell:2:0";
        static void Check(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
        public static IEnumerator Ready(WorldCellManager manager,string id)
        {
            Check(manager.Request(id),"request rejected");
            for(int i=0;i<300 && manager.State(id)!=CellState.Ready && manager.State(id)!=CellState.Failed;i++)yield return null;
            Check(manager.State(id)==CellState.Ready,"cell not ready: "+id+" "+manager.Failure);
        }
        public static IEnumerator Run(WorldCellManager manager,string directory,Action<string> log)
        {
            Directory.CreateDirectory(directory);var flow=manager.GetComponent<SessionFlow>();var saves=manager.GetComponent<SaveSession>();var clock=manager.GetComponent<WorldClock>();
            flow.Resume();manager.Request(A);Check(!manager.TryEnter(A),"entered unsafe cell");Check(saves.Capture(out _).Error==SaveError.Busy,"save accepted half-loaded cell");
            yield return Ready(manager,A);Check(manager.TryEnter(A),"failed entry A");flow.Resume();
            var a=manager.Content(A);Check(a.CollisionReady()&&a.NavigationReady,"readiness false positive");
            var item=a.GetComponentInChildren<WorldItem>();Check(item&&item.TryInteract(),"loot pickup failed");string consumed=item.PersistentId;
            yield return null;
            var inventory=flow.Player.GetComponent<PlayerInventory>();int index=-1;for(int i=0;i<inventory.Container.Capacity;i++)if(!inventory.Container.GetSlot(i).IsEmpty){index=i;break;}
            Check(index>=0,"no carried loot");Check(inventory.TryDrop(index,1),"drop failed");yield return null;
            var drop=a.GetComponentInChildren<WorldItem>();Check(drop&&drop.Origin==WorldItemOrigin.Drop,"drop not cell owned");string dropId=drop.PersistentId;var dropPose=drop.transform.position;
            var door=a.GetComponentInChildren<DoorInteractable>();door.RestoreOpen(true);
            a.encounter.Actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo{Amount=1000});
            double time=clock.Simulation.Seconds;
            yield return Ready(manager,B);Check(manager.TryEnter(B),"entry B failed");Check(manager.State(A)==CellState.Unloaded,"A retained");
            flow.Pause();string path=Path.Combine(directory,"cell-checkpoint.json");Check(saves.Save(path).Success,"save in B failed: "+saves.LastResult.Message);
            flow.ReturnToMenu();yield return null;yield return saves.Load(path);Check(saves.LastResult.Success,"load in B failed: "+saves.LastResult.Message);
            flow.Resume();Check(manager.CurrentCell==B&&manager.State(B)==CellState.Ready,"B not ready after load");
            yield return Ready(manager,A);Check(manager.TryEnter(A),"return A failed");
            a=manager.Content(A);int drops=0;foreach(var worldItem in a.GetComponentsInChildren<WorldItem>())
            {Check(worldItem.PersistentId!=consumed,"tombstone respawned");if(worldItem.PersistentId==dropId){drops++;Check(worldItem.Quantity==1&&Vector3.Distance(worldItem.transform.position,dropPose)<.01f,"drop changed");}}
            Check(drops==1,"drop duplicate/missing");Check(a.GetComponentInChildren<DoorInteractable>().IsOpen,"door reset");Check(!a.encounter.Actor.GetComponent<ZombieHealth>().IsAlive,"enemy revived");
            Check(clock.Simulation.Seconds>=time,"clock reset");
            flow.Pause();Check(saves.Save(path).Success,"save A failed");flow.ReturnToMenu();yield return null;yield return saves.Load(path);Check(saves.LastResult.Success,"load A failed: "+saves.LastResult.Message);flow.Resume();
            log("Functional route PASS: resident → A pickup/drop/open/kill → B → save/load B → A tombstone/drop/door/death → save/load A.");
            log("cycle,scenes,items,enemies,listeners,managedBytes,nativeAllocatedBytes,loadMs");
            for(int cycle=0;cycle<12;cycle++)
            {
                yield return Ready(manager,B);Check(manager.TryEnter(B),"soak B");yield return null;
                yield return Ready(manager,A);Check(manager.TryEnter(A),"soak A");yield return null;
                Check(manager.Content(A).GetComponentsInChildren<WorldItem>().Length==1,"soak item conservation");
                Check(UnityEngine.Object.FindObjectsByType<WorldCellContent>(FindObjectsSortMode.None).Length==1,"retained cell object");
                log($"{cycle},{SceneManager.sceneCount},{UnityEngine.Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None).Length},{UnityEngine.Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None).Length},{manager.ReadyListenerCount},{GC.GetTotalMemory(false)},{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()},{manager.LoadMilliseconds(A):F3}");
            }
            Check(manager.ReturnToResident(new Vector3(-8,.05f,6)),"shelter approach return failed");
            manager.Request(B);manager.Cancel(B);yield return null;yield return null;Check(manager.State(B)==CellState.Unloaded,"cancel callback revived cell");
            manager.Request(B);flow.ReturnToMenu();yield return null;flow.BeginSession();yield return null;yield return null;Check(manager.State(B)==CellState.Unloaded,"old session callback leaked");
            log("PASS: 12 full A↔B cycles; cancellation and session restart; global shelter flow remains session-owned.");
        }
    }
    public sealed class WorldCellStandaloneAcceptance : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="-cellAcceptance")
            {var go=new GameObject("Cell acceptance driver");go.AddComponent<WorldCellStandaloneAcceptance>().directory=args[i+1];break;}
        }
        string directory;
        IEnumerator Start()
        {
            yield return null;yield return null;var report=new StringBuilder();var manager=FindAnyObjectByType<WorldCellManager>();
            var route=WorldCellAcceptanceRoute.Run(manager,directory,s=>{report.AppendLine(s);Debug.Log(s);});
            Exception failure=null;
            // Flatten nested enumerators so failures at every route stage are recorded in standalone evidence.
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(route);
            while(stack.Count>0)
            {
                object current=null;bool next=false;
                try {next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
                catch(Exception error){failure=error;break;}
                if(!next){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
            }
            report.AppendLine(failure==null?"STANDALONE PASS":"STANDALONE FAIL "+failure);
            Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"standalone-result.txt"),report.ToString());Application.Quit(failure==null?0:1);
        }
#endif
    }
}
