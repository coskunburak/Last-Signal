#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Shelter
{
    /// <summary>Opt-in verification only. Uses real loot, combat, interaction, UI, time and save APIs.
    /// Fixture positioning shortens traversal; never grants resources or writes production state.</summary>
    public sealed class S010Acceptance : MonoBehaviour
    {
        string output;
        double duration = 900;
        SessionFlow flow;
        ShelterSite site;
        ShelterLoop loop;
        WorldClock clock;
        int errors;
        double started;
        readonly List<string> lines = new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-s010Acceptance")
            {
                var driver = new GameObject("S010 opt-in acceptance").AddComponent<S010Acceptance>(); driver.output = args[i + 1];
                for (int j = 0; j + 1 < args.Length; j++) if (args[j] == "-s010Duration" && double.TryParse(args[j + 1], out var seconds)) driver.duration = Math.Max(0, seconds);
                return;
            }
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output); Application.logMessageReceived += Observe; started = Time.realtimeSinceStartupAsDouble;
            var stack = new Stack<IEnumerator>(); stack.Push(Run()); Exception failure = null;
            while (stack.Count > 0)
            {
                bool next = false; object current = null;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception e) { failure = e; break; }
                if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            Application.logMessageReceived -= Observe;
            lines.Add($"ElapsedRealSeconds={Time.realtimeSinceStartupAsDouble - started:F3}; errors={errors}; Unity={Application.unityVersion}");
            lines.Add(failure == null && errors == 0 ? "PASS automated production route (fixture positioning; not human continuous traversal)" : "FAIL " + failure);
            File.WriteAllLines(Path.Combine(output, "standalone-route.txt"), lines);
            if (failure != null) Debug.LogException(failure);
            Application.Quit(failure == null && errors == 0 ? 0 : 1);
        }
        void Observe(string message, string trace, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Record(string message) { lines.Add($"{Time.realtimeSinceStartupAsDouble - started:F2}s {message}"); File.WriteAllLines(Path.Combine(output, "progress.txt"), lines); }
        void Place(Vector3 position) => WorldTimeAcceptanceRoute.Place(flow.Player, position);
        void Aim(Vector3 position) { flow.Player.GetComponent<FirstPersonLook>().View.transform.LookAt(position); Physics.SyncTransforms(); }
        void Socket(int index)
        {
            var socket = site.sockets[index]; Place(new Vector3(socket.transform.position.x + 1, .05f, socket.transform.position.z - (index == 0 ? 1.1f : 0)));
            Aim(socket.transform.position); Check(flow.Player.GetComponent<InteractionController>().TryInteract(), "Socket interaction ray " + index);
        }
        void Button(string name)
        {
            foreach (var button in site.GetComponentsInChildren<Button>()) if (button.name == name) { button.onClick.Invoke(); return; }
            throw new InvalidOperationException("Missing visible button " + name);
        }
        void Use(ShelterPoint point)
        {
            Place(new Vector3(point.transform.position.x, .05f, point.transform.position.z) + (point.Action == ShelterAction.Leave ? Vector3.left : Vector3.right) * 1.15f);
            Aim(point.transform.position); Check(flow.Player.GetComponent<InteractionController>().TryInteract(), "Shelter point " + point.Action);
        }
        void Pick(ItemDefinition definition)
        {
            foreach (var item in FindObjectsByType<WorldItem>()) if (item.Available && item.Definition == definition && item.Origin == WorldItemOrigin.Loot)
            {
                Place(new Vector3(item.transform.position.x + 1.3f, .05f, item.transform.position.z)); Aim(item.transform.position);
                Check(flow.Player.GetComponent<InteractionController>().TryInteract(), "World pickup " + definition.Id); return;
            }
            throw new InvalidOperationException("Missing expedition resource " + definition.Id);
        }
        void Transfer(bool deposit, ItemDefinition definition, int quantity)
        { Check(loop.Transfer(deposit, definition, quantity).Moved == quantity, "Exact transfer " + definition.Id); }
        IEnumerator Screenshot(string name)
        { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png")); yield return null; }
        IEnumerator Run()
        {
            yield return null; flow = FindAnyObjectByType<SessionFlow>(); site = flow.GetComponent<ShelterSite>(); loop = flow.GetComponent<ShelterLoop>(); clock = flow.GetComponent<WorldClock>();
            flow.Resume(); yield return new WaitForSeconds(1); Check(site && site.Validate(), "S010 production composition");
            Socket(0); Button("Claim cabin"); Check(!site.Production.Claimed && site.Feedback.Contains("threat"), "Initial living threat rejects claim");
            yield return Screenshot("01-claim-rejected"); Button("Close"); Use(loop.ExitPoint);
            var zombie = FindAnyObjectByType<ZombieController>(); var health = zombie.GetComponent<ZombieHealth>();
            var weapon = flow.Player.GetComponent<PlayerCombatController>().Firearm;
            ZombieHitRegion chest = null; foreach (var region in zombie.GetComponentsInChildren<ZombieHitRegion>()) if (region.name == "Damage_Chest") chest = region;
            Check(chest, "Real zombie hit region");
            for (int shot = 0; shot < 20 && health.IsAlive; shot++)
            {
                Place(zombie.transform.position + zombie.transform.forward * 4); Aim(chest.HitCollider.bounds.center);
                weapon.OnFirePressed(); weapon.OnFireReleased(); yield return new WaitForSeconds(.25f);
            }
            Check(!health.IsAlive, "Threat cleared through production rifle damage"); Record("Threat rejection and real combat clearance PASS; no claim actor deletion.");
            Pick(site.material); Pick(site.fuel); Pick(site.recipe.tool);
            ItemDefinition filler = null;
            foreach (var item in FindObjectsByType<WorldItem>()) if (item.Available && item.Origin == WorldItemOrigin.Loot && item.Definition != site.material && item.Definition != site.fuel && item.Definition != site.recipe.tool && item.Definition != site.recipe.output) { filler = item.Definition; break; }
            Check(filler, "Ordinary expedition item to occupy final storage slot"); Pick(filler); yield return null;
            int acquired = loop.Inventory.GetTotalQuantity(site.material); Check(acquired == 20, "Authored expedition scrap count");
            Use(loop.ReturnPoint); Socket(0); Button("Claim cabin"); Check(site.Production.Claimed, site.Feedback); Button("Install this socket"); Check(site.Production.Installed(ShelterModule.Bed), site.Feedback); Button("Close");
            for (int i = 1; i < 3; i++) { Socket(i); Button("Install this socket"); Check(site.Production.Installed((ShelterModule)i), site.Feedback); Button("Close"); }
            Use(loop.StoragePoint); Transfer(true, site.material, 14); Transfer(true, site.fuel, 4); Transfer(true, site.recipe.tool, 1); Transfer(true, filler, 1); loop.ClosePreparation();
            Record("Legitimate world pickups -> claim -> all three paid sockets -> storage PASS.");
            Socket(2); Button("Start craft"); Check(site.Production.Status == CraftStatus.Running, site.Feedback);
            Button("Start craft"); Check(site.Feedback.StartsWith("Rejected"), "Second job rejected"); Button("Cancel / reserve refund"); Button("Collect output / refund");
            Check(loop.Storage.GetTotalQuantity(site.material) == 14 && site.Production.Status == null, "Full cancellation conservation");
            Button("Start craft"); Button("Add 1 fuel can"); Button("Toggle generator"); Button("Close");
            yield return new WaitForSeconds(3);
            var saves = flow.GetComponent<SaveSession>(); string path = Path.Combine(output, "route-save.json"); flow.Pause();
            Check(saves.Save(path).Success, saves.LastResult.Message); var running = site.Production.Capture(); Check(running.job.status == CraftStatus.Running && running.job.progress > 0, "Running job save");
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path); Check(saves.LastResult.Success, saves.LastResult.Message); flow.Pause();
            Check(site.Production.Capture().job.id == running.job.id && Math.Abs(site.Production.Capture().job.progress - running.job.progress) < .001, "Running escrow/progress restore"); flow.Resume();
            float timeout = Time.realtimeSinceStartup + 30;
            while (site.Production.Status == CraftStatus.Running && Time.realtimeSinceStartup < timeout) yield return null;
            Check(site.Production.Status == CraftStatus.CompletedWaitingOutput, "Normal world ticks finish powered job");
            Socket(2); Button("Collect output / refund"); Check(site.Feedback.StartsWith("Rejected") && site.Production.Status == CraftStatus.CompletedWaitingOutput, "Full output retained");
            yield return Screenshot("02-output-full"); Button("Close"); flow.Pause(); Check(saves.Save(path).Success, saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path); Check(saves.LastResult.Success && site.Production.Status == CraftStatus.CompletedWaitingOutput, "Pending output restore");
            Use(loop.StoragePoint); Transfer(false, filler, 1); loop.ClosePreparation();
            Socket(2); Button("Collect output / refund"); Check(loop.Storage.GetTotalQuantity(site.recipe.output) == 10, "Exact output"); Button("Collect output / refund"); Check(site.Feedback.StartsWith("Rejected"), "Repeat collection rejected");
            Button("Upgrade workbench"); Check(site.Production.Upgraded, site.Feedback); Button("Upgrade workbench"); Check(site.Feedback.StartsWith("Rejected"), "Repeat upgrade rejected");
            Check(loop.Storage.GetTotalQuantity(site.material) + 6 + 2 + 6 == acquired, "Scrap conservation: stored + modules + craft + upgrade");
            Record("Cancel, double spend, fuel, running save/load, full output, pending save/load, exactly-once collection and paid upgrade PASS.");
            yield return Screenshot("03-upgraded"); Button("Close"); flow.Pause(); Check(saves.Save(path).Success, saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path); Check(saves.LastResult.Success && site.Production.Upgraded, "Upgrade persisted");
            // Actual elapsed route, repeated legitimate shelter/expedition transitions, no accelerated clock injection.
            int cycles = 0;
            while (Time.realtimeSinceStartupAsDouble - started < duration)
            {
                Use(loop.ExitPoint); yield return new WaitForSeconds(5); Use(loop.ReturnPoint); cycles++;
                yield return new WaitForSeconds(5);
                if (cycles % 6 == 0) { flow.Pause(); Check(saves.Save(path).Success, saves.LastResult.Message); flow.Resume(); Record("Continued expedition/shelter cycles=" + cycles); }
            }
            Check(loop.Storage.GetTotalQuantity(site.recipe.output) == 10, "Long route output remains exactly once");
            Record("Continued expedition gameplay PASS; cycles=" + cycles);
            flow.ReturnToMenu(); yield return null; flow.BeginSession(); yield return null;
            Check(!site.Production.Claimed && site.Production.Status == null && site.Production.FuelSeconds == 0 && !site.Production.Upgraded, "New session has no production state leakage");
            flow.ReturnToMenu(); yield return null; Check(errors == 0, "No project runtime errors");
        }
    }
}
#endif
