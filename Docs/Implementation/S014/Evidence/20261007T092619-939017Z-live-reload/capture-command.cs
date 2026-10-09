using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using LastSignal;
using LastSignal.Inventory;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        S014ReloadProbe.Begin(); result.Log("S014 gerçek saatli görsel rota başladı; public gameplay komutları, kontrollü ammo hazırlığı; final D140 değildir.");
    }
}
internal static class S014ReloadProbe
{
    static SessionFlow flow; static GameObject player; static PlayerCombatController combat;
    static FirstPersonLook look; static PlayerInputReader input; static Camera camera;
    static string directory; static double start,next; static int frame,phase; static float lastShot;
    static readonly StringBuilder log=new StringBuilder();
    static readonly double[] times={0,9,13,14,14.3,16,18,20,20.4,22,24,25};
    public static void Begin()
    {
        flow=UnityEngine.Object.FindAnyObjectByType<SessionFlow>(); flow.Resume();player=flow.Player;
        combat=player.GetComponent<PlayerCombatController>();look=player.GetComponent<FirstPersonLook>();input=player.GetComponent<PlayerInputReader>();camera=look.View;
        input.FocusLost-=flow.Pause;
        foreach(var z in UnityEngine.Object.FindObjectsByType<ZombieController>(FindObjectsSortMode.None))z.SetPaused(true);
        player.GetComponent<PlayerInventory>().TryAdd(combat.Firearm.Definition.Ammunition,90);
        directory=File.ReadAllText("/tmp/lastsignal-s014-live-run").Trim();Directory.CreateDirectory(directory+"/frames");
        log.AppendLine("frame,realtime_seconds,phase,weapon_state,magazine,reserve,reload_committed,melee_state,fov,width,height");
        start=EditorApplication.timeSinceStartup; next=0;phase=0;frame=0;lastShot=-1;
        EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        try {
            double t=EditorApplication.timeSinceStartup-start;
            if(!EditorApplication.isPlaying||!player){Finish("ABORTED");return;}
            if(phase<times.Length&&t>=times[phase]){Action(phase);phase++;}
            if(t>=0&&t<8&&t-lastShot>.12){combat.Firearm.OnFirePressed();combat.Firearm.OnFireReleased();lastShot=(float)t;}
            
            if(t>=tLimit){Finish("CAPTURED — görsel inceleme gerekli; kontrollü Editor rotası; D140 değil.");return;}
            if(t<next)return; next=t+1.0/20;
            string name=frame.ToString("D5")+".png";ScreenCapture.CaptureScreenshot(directory+"/frames/"+name);
            var w=combat.Firearm.RuntimeState;
            log.AppendLine(FormattableString.Invariant($"{frame},{t:F6},{phase},{w.State},{w.CurrentMagazine},{w.ReserveAmmo},{w.ReloadCommitted},{combat.Melee.Simulation.State},{camera.fieldOfView},{Screen.width},{Screen.height}"));frame++;
        } catch(Exception ex){Finish("FAIL "+ex);}
    }
    const double tLimit=28;
    static void SetFov(float v){var s=new SerializedObject(look);s.FindProperty("fieldOfViewDegrees").floatValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Action(int p)
    {
        switch(p){
case 0: combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);look.ApplyRecoil(-look.Pitch,0);break;
case 1: if(combat.Firearm.RuntimeState.CurrentMagazine!=0)throw new Exception("Empty setup failed");combat.Firearm.OnReloadRequested();break;
case 2: combat.Firearm.OnFirePressed();combat.Firearm.OnFireReleased();break;
case 3: combat.Firearm.OnReloadRequested();break;
case 4: combat.SelectSlot(PlayerCombatController.CombatSlot.Melee);break;
case 5: combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);break;
case 6: combat.Firearm.OnReloadRequested();break;
case 7: combat.SelectSlot(PlayerCombatController.CombatSlot.Melee);break;
case 8: combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);break;
case 9: combat.Firearm.OnFirePressed();combat.Firearm.OnFireReleased();combat.Firearm.OnReloadRequested();break;
case 10: flow.Pause();break;
case 11: flow.Resume();break;
        }
    }

    static void Finish(string result)
    {
        EditorApplication.update-=Tick;
        File.WriteAllText(directory+"/timeline.csv",log.ToString());File.WriteAllText(directory+"/result.txt",result);
        if(combat)combat.SetEvidenceAim(false);if(input&&flow)input.FocusLost+=flow.Pause;
        if(flow)flow.Pause();
    }
}
