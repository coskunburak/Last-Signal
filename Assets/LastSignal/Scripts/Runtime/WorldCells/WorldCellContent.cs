using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Persistence;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.WorldCells
{
    /// <summary>One exclusive runtime content owner. Uses the existing identities, loot receipts and snapshot DTOs.</summary>
    public sealed class WorldCellContent : MonoBehaviour
    {
        public CellCoordinate coordinate;
        public Transform entry;
        public Collider ground;
        public Collider[] requiredSolids;
        public NavMeshData navigation;
        public LootPopulationService loot;
        public ZombieEncounter encounter;
        NavMeshDataInstance navInstance;
        public bool NavigationReady { get; private set; }
        public void InstallNavigation()
        {
            if (!navigation) throw new InvalidOperationException("Missing required cell navigation.");
            navInstance = NavMesh.AddNavMeshData(navigation);
            NavigationReady = navInstance.valid && NavMesh.SamplePosition(entry.position, out _, .5f, NavMesh.AllAreas);
            if (!NavigationReady) throw new InvalidOperationException("Entry navigation unavailable.");
        }
        public bool CollisionReady()
        {
            if (!ground || !ground.enabled || !ground.gameObject.activeInHierarchy || ground.isTrigger) return false;
            foreach (var solid in requiredSolids) if (!solid || !solid.enabled || !solid.gameObject.activeInHierarchy || solid.isTrigger) return false;
            return ground.Raycast(new Ray(entry.position + Vector3.up, Vector3.down), out _, 2);
        }
        public void Hydrate(CellSnapshot state, ItemCatalog catalog, GameObject player, int seed)
        {
            if (state.visited)
            {
                var doors = new Dictionary<string, DoorInteractable>(StringComparer.Ordinal);
                foreach (var door in GetComponentsInChildren<DoorInteractable>()) doors.Add(door.GetComponent<PersistentEntityId>().Id, door);
                if (doors.Count != state.world.doors.Length) throw new InvalidOperationException("Cell door topology mismatch.");
                foreach (var saved in state.world.doors)
                { if (!doors.TryGetValue(saved.id, out var door)) throw new InvalidOperationException("Missing authored door."); door.RestoreOpen(saved.open); }
                var points = new HashSet<string>(StringComparer.Ordinal);
                foreach (var point in GetComponentsInChildren<LootSpawnPoint>()) points.Add(point.StableId);
                if (points.Count != state.world.opportunities.Length) throw new InvalidOperationException("Cell loot topology mismatch.");
                foreach (var saved in state.world.opportunities) if (!points.Remove(saved.id)) throw new InvalidOperationException("Missing authored loot point.");
                loot.Restore(new SaveGame { header = new SaveHeader { seed = seed }, world = state.world }, catalog, transform);
            }
            else loot.Begin(player, seed, transform);
            if (encounter)
            {
                encounter.Begin(player);
                if (!encounter.Actor) throw new InvalidOperationException("Cell enemy initialization failed.");
                encounter.Actor.transform.SetParent(transform, true);
                if (state.visited)
                {
                    if (state.world.enemies.Length != 1 || state.world.enemies[0].id != encounter.GetComponent<PersistentEntityId>().Id)
                        throw new InvalidOperationException("Cell enemy topology mismatch.");
                    var saved = state.world.enemies[0]; var actor = encounter.Actor;
                    if (!actor.GetComponent<NavMeshAgent>().Warp(Position(saved.transform))) throw new InvalidOperationException("Invalid enemy navigation pose.");
                    actor.transform.rotation = new Quaternion(saved.transform.qx, saved.transform.qy, saved.transform.qz, saved.transform.qw);
                    actor.GetComponent<ZombieHealth>().RestoreHealth(saved.health);
                }
            }
        }
        public WorldSnapshot Capture()
        {
            var doors = new List<DoorSnapshot>();
            foreach (var door in GetComponentsInChildren<DoorInteractable>())
            {
                if (door.Busy) throw new InvalidOperationException("Cell door moving.");
                doors.Add(new DoorSnapshot { id = door.GetComponent<PersistentEntityId>().Id, open = door.IsOpen });
            }
            var live = new Dictionary<string, WorldItem>(StringComparer.Ordinal);
            foreach (var item in GetComponentsInChildren<WorldItem>()) if (item.Available) live.Add(item.PersistentId, item);
            var points = new Dictionary<string, LootSpawnPoint>(StringComparer.Ordinal);
            foreach (var point in GetComponentsInChildren<LootSpawnPoint>()) points.Add(point.StableId, point);
            if (!loot.Populated || loot.Results.Count != points.Count) throw new InvalidOperationException("Unresolved cell loot.");
            var items = new List<WorldItemSnapshot>(); var opportunities = new List<LootOpportunitySnapshot>();
            foreach (var result in loot.Results)
            {
                var opportunity = new LootOpportunitySnapshot { id = result.PointId };
                if (result.Selection.Outcome == LootOutcome.Spawned)
                {
                    opportunity.outcome = OpportunityOutcome.Generated; opportunity.entityId = "loot:" + result.PointId;
                    if (!loot.TryGetConsumed(opportunity.entityId, out bool consumed)) throw new InvalidOperationException("Missing consumption receipt.");
                    if (live.TryGetValue(opportunity.entityId, out var item))
                    {
                        if (consumed) throw new InvalidOperationException("Duplicate consumed item.");
                        items.Add(Item(item)); live.Remove(opportunity.entityId);
                    }
                    else if (!consumed) throw new InvalidOperationException("Cell item disappeared without receipt.");
                    else items.Add(new WorldItemSnapshot { id = opportunity.entityId, definitionId = result.Selection.Item.Id.Value,
                        origin = WorldItemOrigin.Loot, disposition = EntityDisposition.Consumed, transform = SaveSession.Pose(points[result.PointId].transform) });
                }
                else if (result.Selection.Outcome == LootOutcome.Empty) opportunity.outcome = OpportunityOutcome.Empty;
                else if (result.Selection.Outcome == LootOutcome.Blocked) opportunity.outcome = OpportunityOutcome.Blocked;
                else throw new InvalidOperationException("Invalid cell loot point.");
                opportunities.Add(opportunity);
            }
            foreach (var item in live.Values)
            {
                if (item.Origin != WorldItemOrigin.Drop || !CellCoordinate.FromWorld(item.transform.position).Equals(coordinate))
                    throw new InvalidOperationException("Invalid cell item ownership.");
                items.Add(Item(item));
            }
            var enemies = new List<EnemySnapshot>();
            if (encounter)
            {
                if (!encounter.Actor) throw new InvalidOperationException("Missing cell enemy.");
                enemies.Add(new EnemySnapshot { id = encounter.GetComponent<PersistentEntityId>().Id,
                    health = encounter.Actor.GetComponent<ZombieHealth>().CurrentHealth, transform = SaveSession.Pose(encounter.Actor.transform) });
            }
            return new WorldSnapshot { doors = doors.ToArray(), opportunities = opportunities.ToArray(), items = items.ToArray(), enemies = enemies.ToArray() };
        }
        static WorldItemSnapshot Item(WorldItem item) => new WorldItemSnapshot { id = item.PersistentId, definitionId = item.Definition.Id.Value,
            origin = item.Origin, disposition = EntityDisposition.Present, quantity = item.Quantity, transform = SaveSession.Pose(item.transform) };
        static Vector3 Position(TransformSnapshot pose) => new Vector3(pose.x, pose.y, pose.z);
        public void Release()
        {
            if (encounter) encounter.End();
            if (loot) loot.End();
            if (navInstance.valid) navInstance.Remove();
            NavigationReady = false;
        }
        void OnDestroy() => Release();
    }
}
