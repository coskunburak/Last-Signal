using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.WorldTime;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Shelter
{
    [DisallowMultipleComponent, RequireComponent(typeof(ShelterLoop), typeof(WorldClock))]
    public sealed class ShelterSite : MonoBehaviour, IWorldTimeParticipant
    {
        public string shelterId = "shelter.cabin", cellId = "resident";
        public ShelterRecipe recipe;
        public ItemDefinition material, fuel;
        public int moduleCost = 2, upgradeCost = 6;
        public double fuelUnitSeconds = 1800;
        public ShelterSocket[] sockets;
        public GameplayNoiseSettings generatorNoise = new GameplayNoiseSettings();
        [Serializable] public sealed class GameplayNoiseSettings
        { public Noise.GameplayNoiseProfile profile = new Noise.GameplayNoiseProfile(32, .25f, 3); public double interval = 60; }
        SessionFlow flow;
        ShelterLoop loop;
        WorldClock clock;
        WorldSimulation bound;
        ShelterSocket selected;
        double noiseElapsed;
        GameObject panel;
        Text status;
        float nextRefresh;
        public ShelterProduction Production { get; private set; }
        public string Feedback { get; private set; } = "";
        public bool IsOpen => panel && panel.activeSelf;
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(shelterId) || cellId != "resident" || !ShelterRecipe.ValidateCatalog(new[] { recipe }) || !material || !fuel ||
                moduleCost < 1 || upgradeCost < 1 || !WorldTimeSettings.Finite(fuelUnitSeconds) || fuelUnitSeconds < 1 || fuelUnitSeconds > 86400 ||
                generatorNoise == null || !generatorNoise.profile.Valid || !WorldTimeSettings.Finite(generatorNoise.interval) || generatorNoise.interval < 1 ||
                sockets == null || sockets.Length != 3) return false;
            int types = 0; var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var socket in sockets)
            {
                if (!socket || socket.site != this || socket.gameObject.scene != gameObject.scene || string.IsNullOrWhiteSpace(socket.socketId) ||
                    !ids.Add(socket.socketId) || !Enum.IsDefined(typeof(ShelterModule), socket.module) || (types & (1 << (int)socket.module)) != 0) return false;
                types |= 1 << (int)socket.module;
            }
            return types == 7;
        }
        ShelterProduction Create(double now) => new ShelterProduction(shelterId, recipe, material, fuel, moduleCost, upgradeCost, fuelUnitSeconds, now);
        public void Begin()
        {
            End(); if (!Validate()) throw new InvalidOperationException("Invalid authored S010 shelter.");
            flow = GetComponent<SessionFlow>(); loop = GetComponent<ShelterLoop>(); clock = GetComponent<WorldClock>();
            Production = Create(clock.Simulation.Seconds); Production.Generated += Generated; BindClock();
            if (!panel) CreatePanel(); Feedback = "Claim this cabin, then install modules using expedition scrap.";
        }
        public void BindClock()
        {
            if (bound != null && Production != null) { bound.Unregister(Production); bound.Unregister(this); }
            bound = null;
            if (Production != null && clock && clock.Simulation != null) { bound = clock.Simulation; bound.Register(Production); bound.Register(this); }
        }
        public void End()
        {
            if (panel) panel.SetActive(false); selected = null;
            if (Production != null) { if (bound != null) { bound.Unregister(Production); bound.Unregister(this); } Production.Generated -= Generated; }
            Production = null; bound = null; noiseElapsed = 0;
        }
        void OnDisable() => End();
        public bool CanAccess(Transform point, bool modal)
        {
            if (Production == null || !flow || !flow.Player || flow.PlayerDead || flow.Restoring || !point || point.gameObject.scene != gameObject.scene ||
                clock.Sleeping || (flow.Paused && !modal) || (flow.Player.transform.position - point.position).sqrMagnitude > 6.25f) return false;
            var cells = GetComponent<WorldCells.WorldCellManager>();
            return !cells || (cells.Stable && cells.CurrentCell == cellId);
        }
        public bool Open(ShelterSocket socket)
        {
            if (!socket || Array.IndexOf(sockets, socket) < 0 || !CanAccess(socket.transform, false)) return false;
            selected = socket; flow.Pause(); panel.SetActive(true); Refresh(); return true;
        }
        public void Close() { if (panel) panel.SetActive(false); selected = null; if (flow && flow.Player && !flow.Restoring) flow.Resume(); }
        public string Claim()
        {
            if (!selected || !Validate() || !CanAccess(selected.transform, IsOpen)) return "Invalid building or out of range.";
            if (Production.Claimed) return "Already claimed by this session.";
            if (clock.ThreatNearby()) return "Nearby living threat: clear the area before claiming.";
            return Production.Claim() ? "Shelter claimed. Enemies remain in the world." : "Claim rejected.";
        }
        public string Install()
        {
            if (!selected || !CanAccess(selected.transform, IsOpen)) return "Socket unavailable / out of range.";
            if (!selected.Clear) return "Socket clearance blocked; no materials consumed.";
            return Production.Install(selected.module, loop.Inventory.Container) ? selected.module + " installed." : "Claim first; socket occupied or missing scrap.";
        }
        public string Act(int action)
        {
            if (!selected || !CanAccess(selected.transform, IsOpen)) return "Shelter unavailable / out of range.";
            if (action == 0) return Claim();
            if (action == 1) return Install();
            if (selected.module != ShelterModule.Workbench) return "Use the authored workbench socket for production.";
            bool ok;
            // Craft, refund, fuel and upgrade use explicit shelter storage as their only source.
            if (!Production.Installed(ShelterModule.Storage)) return "Install storage and deposit expedition resources first.";
            var source = loop.Storage.Container;
            switch (action)
            {
                case 2: ok = Production.Start(source); break;
                case 3: ok = Production.Cancel(); break;
                case 4: ok = Production.Collect(source); break;
                case 5: ok = Production.Refuel(source); break;
                case 6: ok = Production.ToggleGenerator(); break;
                case 7: ok = Production.Upgrade(source); break;
                default: return "Unknown action.";
            }
            return ok ? "Done. " + (action == 3 ? "Progress discarded; collect your full input refund." : "") :
                "Rejected: check module, tool, ingredients, pending job or destination space. Nothing consumed.";
        }
        public bool CanRestore(ShelterProductionSnapshot snapshot, double now) => Validate() && (snapshot == null || Create(now).CanRestore(snapshot, now));
        public void Restore(ShelterProductionSnapshot snapshot)
        {
            if (snapshot != null) Production.Restore(snapshot, clock.Simulation.Seconds);
            else
            {
                // Pre-S010 saves already owned a functioning bed and stash. Preserve access
                // to their existing resources; only the new workbench still needs installation.
                var legacy = Create(clock.Simulation.Seconds).Capture();
                legacy.claimed = legacy.bed = legacy.storage = true;
                Production.Restore(legacy, clock.Simulation.Seconds);
            }
        }
        // Bound generator cadence also during large sleep/remote-cell advances; no per-frame world scan.
        public double NextBoundary(double now) => Production != null && Production.Powered
            ? now + Math.Max(.000001, generatorNoise.interval - noiseElapsed) : double.PositiveInfinity;
        public void ApplyElapsed(double from, double to) { }
        public AdvanceReason Inspect(double now) => AdvanceReason.Completed;
        void Generated(double seconds)
        {
            noiseElapsed += seconds;
            if (noiseElapsed < generatorNoise.interval) return;
            noiseElapsed %= generatorNoise.interval;
            flow.Noise?.TryEmit(new Noise.GameplayNoiseRequest(2, sockets[2].transform.position, Noise.GameplayNoiseCategory.Generator, generatorNoise.profile), out _);
        }
        void Update()
        {
            if (!IsOpen) return;
            if (!selected || !CanAccess(selected.transform, true) || !flow.Paused) { Close(); return; }
            if (Time.unscaledTime >= nextRefresh) { Refresh(); nextRefresh = Time.unscaledTime + .2f; }
        }
        void Refresh()
        {
            if (!status || Production == null) return;
            status.text = "SHELTER / " + (Production.Claimed ? "CLAIMED" : "UNCLAIMED") + " / " + (selected ? selected.module.ToString() : "") +
                "\nModule: " + moduleCost + " scrap | Bench upgrade: " + upgradeCost + " scrap, twice as fast" +
                "\n" + recipe.inputQuantity + " " + recipe.input.DisplayName + " -> " + recipe.outputQuantity + " " + recipe.output.DisplayName +
                " | tool: " + (recipe.tool ? recipe.tool.DisplayName : "none") + " | " + recipe.durationWorldSeconds + " world seconds (base)" +
                "\nJob: " + (Production.Status?.ToString() ?? "Idle") + " " + (Production.Progress * 100).ToString("F0") + "% | Fuel: " + Production.FuelSeconds.ToString("F0") + " world seconds" +
                " | Generator " + (Production.GeneratorEnabled ? "ON (auto resumes on refuel)" : "OFF") +
                "\nAll production uses shelter storage. Close panel to advance time.\n" + Feedback;
        }
        void CreatePanel()
        {
            var canvas = new GameObject("Shelter production UI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder = 40;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 900);
            panel = new GameObject("Production panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1180, 760); panel.GetComponent<Image>().color = new Color(.025f, .035f, .045f, .99f);
            status = Label(panel.transform, new Vector2(0, 230), new Vector2(1100, 260), 22);
            string[] labels = { "Claim cabin", "Install this socket", "Start craft", "Cancel / reserve refund", "Collect output / refund", "Add 1 fuel can", "Toggle generator", "Upgrade workbench", "Close" };
            for (int i = 0; i < labels.Length; i++)
            {
                int action = i;
                var go = new GameObject(labels[i], typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(panel.transform, false);
                var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(500, 60); rect.anchoredPosition = new Vector2(i % 2 == 0 ? -270 : 270, 50 - (i / 2) * 78);
                go.GetComponent<Image>().color = new Color(.1f, .25f, .25f);
                Label(go.transform, Vector2.zero, rect.sizeDelta, 22).text = labels[i];
                go.GetComponent<Button>().onClick.AddListener(() => { if (action == 8) Close(); else { Feedback = Act(action); Refresh(); } });
            }
            panel.SetActive(false);
        }
        static Text Label(Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.sizeDelta = size; r.anchoredPosition = position;
            var t = go.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = fontSize; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false; return t;
        }
    }
}
