using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.Objectives
{
    [DisallowMultipleComponent]
    public sealed class RelayMission : MonoBehaviour
    {
        public ItemDefinition fuse, tool;
        public RelayPoint radio, relay;
        public Transform recoveryPoint;
        public const string Clue = "Relay maintenance: the gas-station maintenance cache holds the spare fuse. Bring a wrench to the relay at the south edge of the yard. After repair, return to this radio for the contact bulletin.";
        [SerializeField, TextArea] string maintenanceClue = Clue;
        public string MaintenanceClue => string.IsNullOrWhiteSpace(maintenanceClue) ? Clue : maintenanceClue;
        public const string Intel = "CONTACT — Maintenance channel restored. A northern route is mentioned; its exact location remains unconfirmed.";
        public RelayProgression Progress { get; private set; }
        public string Feedback { get; private set; } = "Kulübeyi keşfet.";
        public bool JournalOpen { get; private set; }
        public long Pending => pending;
        SessionFlow flow;
        WorldClock clock;
        PlayerInventory inventory;
        long sequence, pending, generation;
        double due;
        bool observing;
        public bool Validate() => RelayGraph.Validate(RelayGraph.Definitions()) && fuse && fuse.Id.Value == RelayProgression.FuseId && fuse.MaxStack == 1 && fuse.WorldPrefab &&
            tool && tool.Category == ItemCategory.Tool && radio && relay && radio != relay && radio.mission == this && relay.mission == this &&
            radio.kind == RelayPointKind.Radio && relay.kind == RelayPointKind.Relay && radio.stableId == "cabin.radio.v1" && relay.stableId == "overlook.relay.v1" && recoveryPoint;
        public void Begin()
        {
            End(); flow = GetComponent<SessionFlow>(); clock = GetComponent<WorldClock>();
            if (!Validate()) throw new InvalidOperationException("Invalid relay authoring.");
            Progress = new RelayProgression(); inventory = flow.Player.GetComponent<PlayerInventory>();
            inventory.InventoryChanged += Observe; observing = true; generation = flow.Generation;
            if (!flow.Restoring) Observe();
        }
        public void End()
        {
            CancelRepair(); if (observing && inventory) inventory.InventoryChanged -= Observe;
            observing = false; inventory = null; Progress = null; JournalOpen = false;
        }
        void OnDisable() => End();
        void Observe() { if (Progress != null && inventory && !flow.Restoring) Progress.ObserveInventory(inventory.Container, fuse, tool); }
        // Add the newly authored guaranteed source only to legacy worlds that predate this arc.
        // All existing ownership/loot receipts are retained; current saves never take this path.
        public bool MigrateLegacy(SaveGame save)
        {
            if (save.progression != null) return true;
            if (save.header.progressionVersion != 0 || save.world?.opportunities == null || save.world.items == null) return false;
            bool sourceExists=false;
            foreach (var source in save.world.opportunities) if (source.id == "relay.fuse-source.v1") sourceExists=true;
            if (!sourceExists)
            {
                var point = Array.Find(FindObjectsByType<LastSignal.Loot.LootSpawnPoint>(), p => p.StableId == "relay.fuse-source.v1");
                if (!point) return false;
                var opportunities = new System.Collections.Generic.List<LootOpportunitySnapshot>(save.world.opportunities);
                var items = new System.Collections.Generic.List<WorldItemSnapshot>(save.world.items);
                opportunities.Add(new LootOpportunitySnapshot { id=point.StableId, outcome=OpportunityOutcome.Generated, entityId="loot:"+point.StableId });
                items.Add(new WorldItemSnapshot { id="loot:"+point.StableId, definitionId=RelayProgression.FuseId, quantity=1,
                    origin=WorldItemOrigin.Loot, disposition=EntityDisposition.Present, transform=SaveSession.Pose(point.transform) });
                save.world.opportunities=opportunities.ToArray(); save.world.items=items.ToArray();
            }
            save.progression=new RelayProgression().Capture(); save.header.progressionVersion=1;
            foreach(var item in save.world.items)
                if(item.id=="loot:relay.fuse-source.v1" && item.disposition==EntityDisposition.Consumed) save.progression.acquired=true;
            return true;
        }
        public void Restore(RelaySnapshot snapshot)
        { CancelRepair(); Progress.Restore(snapshot, clock.Simulation.Seconds); Progress.ObserveInventory(inventory.Container, fuse, tool); }
        bool Ready => Progress != null && flow && flow.Player && !flow.PlayerDead && !flow.Restoring && generation == flow.Generation;
        bool Near(Transform point) => Ready && point && (flow.Player.transform.position - point.position).sqrMagnitude <= 9;
        public bool Discover()
        {
            if (!Near(radio.transform) || flow.Paused) return false;
            Progress.DiscoverRadio(); Observe(); if (Progress.Repaired) Progress.Listen();
            Feedback = Progress.Repaired ? Intel : Progress.Acquired ? "Earlier fuse acquisition recognized. Bring the fuse and a wrench to the relay." : MaintenanceClue;
            JournalOpen = true; CancelRepair(); flow.Pause(); return true;
        }
        public string Journal => Progress == null ? "No active session." : !Progress.Radio ? "Unknown signal. Explore the cabin; no relay location confirmed." :
            MaintenanceClue + "\n\n" + (Progress.Repaired ? Intel + (Progress.Listened ? "\nRadio contact heard. Expedition completed." : "\nReturn to the cabin radio to listen.") :
            "Fuse: " + (Progress.Acquired ? "previous acquisition confirmed; carry it for repair" : "not acquired") + "\nTool: bring a wrench. Relay: not repaired.") +
            "\nMap knowledge: gas station / relay overlook are approximate clues. Enemy and loot positions unknown.";
        public long BeginRepair()
        {
            CancelRepair(); Feedback = RepairRejection(); if (Feedback != null) return 0;
            pending = ++sequence; due = Time.timeAsDouble + 3; Feedback = "Repairing — stay nearby for 3 seconds. E or J cancels; materials remain yours until commit."; return pending;
        }
        string RepairRejection()
        {
            if (!Ready || flow.Paused || JournalOpen) return "Close the journal and resume gameplay.";
            if (!Near(relay.transform)) return "Out of range. Approach the relay.";
            if (Progress.Repaired) return "Relay already repaired; Contact intel already received.";
            if (!Progress.Radio) return "Read the cabin maintenance note first.";
            if (inventory.GetTotalQuantity(fuse) < 1) return "Missing fuse. Retrieve it, or recall a dropped fuse at the cabin radio.";
            if (inventory.GetTotalQuantity(tool) < 1) return "Missing compatible tool: wrench.";
            if (clock.ThreatNearby()) return "Nearby threat prevents repair.";
            var cells = GetComponent<WorldCells.WorldCellManager>();
            if (cells && (!cells.Stable || cells.CurrentCell != "resident")) return "Relay cell unavailable.";
            return null;
        }
        public bool FinishRepair(long token)
        {
            if (token == 0 || token != pending) return false;
            var rejection = RepairRejection();
            if (rejection != null) { CancelRepair(); Feedback = rejection + " Repair cancelled; no materials consumed."; return false; }
            if (Time.timeAsDouble < due) return false;
            pending = 0;
            bool ok = Progress.Repair(inventory.Container, fuse, tool, clock.Simulation.Seconds);
            Feedback = ok ? "Relay repaired. Contact intel received exactly once. Return to the cabin radio." : "Repair rejected; resources unchanged.";
            return ok;
        }
        public void CancelRepair() { if (pending != 0) Feedback = "Repair cancelled; fuse retained."; pending = 0; }
        public void CloseJournal() { JournalOpen = false; CancelRepair(); if (Ready && flow.Paused) flow.Resume(); }
        public bool Recover()
        {
            if (!Near(radio.transform) || !Progress.Acquired || Progress.Repaired || OwnershipTransaction.Active) return false;
            var cells = GetComponent<WorldCells.WorldCellManager>();
            if (cells && !cells.Stable) return false;
            if (inventory.GetTotalQuantity(fuse) > 0) { Feedback = "Fuse already in your inventory."; return false; }
            if (GetComponent<Shelter.ShelterLoop>().Storage.GetTotalQuantity(fuse) > 0) { Feedback = "Fuse is safe in shelter storage; withdraw it there."; return false; }
            // Command-time ownership lookup only. Relocate an existing owner; never create a second fuse.
            foreach (var item in FindObjectsByType<WorldItem>())
                if (item.gameObject.scene == gameObject.scene && item.Available && item.Definition == fuse && item.Origin == WorldItemOrigin.Drop)
                {
                    OwnershipTransaction.Enter();
                    try { item.transform.SetParent(null, true); item.transform.position = recoveryPoint.position; }
                    finally { OwnershipTransaction.Exit(); }
                    Feedback = "Existing dropped fuse recalled to the radio recovery tray."; return true;
                }
            if (cells && cells.RecallDroppedItem(fuse, recoveryPoint.position)) { Feedback = "Existing fuse recalled from unloaded region."; return true; }
            Feedback = "No lost drop: the original fuse remains at its source. Death restores the last living checkpoint."; return false;
        }
        public void ToggleJournal()
        { if (!Ready) return; if (JournalOpen) CloseJournal(); else if (!flow.Paused) { JournalOpen = true; CancelRepair(); flow.Pause(); } }
        void Update()
        {
            if (Progress == null) return;
            if (pending != 0) FinishRepair(pending);
        }
        GUIStyle journalText, feedbackText;
        void OnGUI()
        {
            if (!Ready || (flow.UserInterface && flow.UserInterface.Ready)) return;
            if (journalText == null)
            {
                journalText = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 16 };
                feedbackText = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.MiddleLeft, fontSize = 14, padding = new RectOffset(10,10,6,6) };
            }
            GUI.Box(new Rect(20, Screen.height - 78, 780, 58), Feedback ?? "J: journal", feedbackText);
            if (!JournalOpen) return;
            GUI.Box(new Rect(30, 80, 760, 330), "RADIO JOURNAL — J to close");
            GUI.Label(new Rect(50, 115, 720, 240), Journal, journalText);
            if (GUI.Button(new Rect(50, 365, 250, 30), "Recall dropped fuse at radio")) Recover();
            if (GUI.Button(new Rect(530, 365, 240, 30), "Close journal")) CloseJournal();
        }
    }
}
