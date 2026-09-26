using System;
using System.Collections.Generic;
using System.Globalization;

namespace LastSignal.Persistence
{
    /// <summary>Immutable compatibility boundary supplied by the world composition root.</summary>
    public sealed class SaveValidation
    {
        public const int SchemaVersion = 2;
        public const int MaxEntities = 10000;
        readonly string worldId, contentVersion, weaponId;
        readonly int magazineCapacity;
        readonly float maximumHealth, maximumEnemyHealth;
        readonly Dictionary<string, int> stackLimits;
        public SaveValidation(string worldId, string contentVersion, IDictionary<string, int> stackLimits,
            string weaponId, int magazineCapacity, float maximumHealth = 100, float maximumEnemyHealth = 100)
        {
            if (!Id(worldId) || !Id(contentVersion) || !Id(weaponId) || stackLimits == null ||
                magazineCapacity < 1 || !Finite(maximumHealth) || maximumHealth <= 0 ||
                !Finite(maximumEnemyHealth) || maximumEnemyHealth <= 0) throw new ArgumentException("Invalid save compatibility context.");
            this.worldId = worldId; this.contentVersion = contentVersion; this.weaponId = weaponId;
            this.magazineCapacity = magazineCapacity; this.maximumHealth = maximumHealth; this.maximumEnemyHealth = maximumEnemyHealth;
            this.stackLimits = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in stackLimits)
            {
                if (!Id(pair.Key) || pair.Value < 1) throw new ArgumentException("Invalid item catalog entry.");
                this.stackLimits.Add(pair.Key, pair.Value);
            }
        }
        public SaveResult Validate(SaveGame save)
        {
            if (save == null || save.header == null) return Invalid("Missing header.");
            var h = save.header;
            if (h.schemaVersion != 1 && h.schemaVersion != SchemaVersion && h.schemaVersion != 3 && h.schemaVersion != 4) return Fail(SaveError.UnsupportedSchema, "Unsupported save schema.");
            if (h.schemaVersion >= 2 && (save.worldTime == null || !save.worldTime.Valid)) return Invalid("Invalid or missing world-time state.");
            if (h.schemaVersion == 1 && save.worldTime != null) return Invalid("Schema 1 cannot contain world-time state.");
            if (h.worldId != worldId) return Fail(SaveError.WrongWorld, "Save belongs to another world.");
            if (h.contentVersion != contentVersion) return Fail(SaveError.IncompatibleContent, "Content version requires an explicit migration.");
            if (h.generation < 1 || !Id(h.buildId) || !DateTimeOffset.TryParseExact(h.timestampUtc, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var timestamp) || timestamp.Offset != TimeSpan.Zero) return Invalid("Invalid generation/build/timestamp.");
            if (save.player == null || save.inventory == null || save.weapon == null || save.shelter == null || save.world == null)
                return Invalid("Missing required section.");
            if (h.schemaVersion < 3 && save.cells != null) return Invalid("Legacy schema cannot contain cells.");
            if (h.schemaVersion < 4 && save.population != null) return Invalid("Unversioned population data is unsupported; use a pre-S009 checkpoint.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var p = save.player;
            if (!AddId(ids, p.id)) return Identity();
            if (!Pose(p.transform) || !Finite(p.health) || p.health <= 0 || p.health > maximumHealth || !Finite(p.pitch) || Math.Abs(p.pitch) > 85)
                return Invalid("Invalid player state. Schema v1 accepts living-player checkpoints only.");
            var result = Container(save.inventory, ids); if (!result.Success) return result;
            result = Container(save.shelter.storage, ids); if (!result.Success) return result;
            if (save.shelter.expeditionIndex < 0) return Invalid("Invalid expedition index.");
            if (save.weapon.definitionId != weaponId) return Fail(SaveError.UnknownDefinition, "Unknown weapon definition.");
            if (save.weapon.magazine < 0 || save.weapon.magazine > magazineCapacity) return Invalid("Invalid magazine quantity.");
            var w = save.world;
            if (w.doors == null || w.opportunities == null || w.items == null || w.enemies == null ||
                (long)w.doors.Length + w.opportunities.Length + w.items.Length + w.enemies.Length > MaxEntities)
                return Invalid("Missing or excessive world entries.");
            foreach (var d in w.doors) if (d == null || !AddId(ids, d.id)) return Identity();
            var items = new Dictionary<string, WorldItemSnapshot>(StringComparer.Ordinal);
            foreach (var item in w.items)
            {
                if (item == null || !AddId(ids, item.id)) return Identity();
                if (!stackLimits.TryGetValue(item.definitionId ?? "", out var limit)) return Fail(SaveError.UnknownDefinition, "Unknown world-item definition.");
                if (item.origin != WorldItemOrigin.Loot && item.origin != WorldItemOrigin.Drop && item.origin != WorldItemOrigin.Authored)
                    return Invalid("Unknown item origin.");
                if (!Pose(item.transform)) return Invalid("Invalid item transform.");
                if (item.disposition == EntityDisposition.Present)
                { if (item.quantity < 1) return Invalid("Invalid world quantity."); }
                else if (item.disposition == EntityDisposition.Consumed)
                { if (item.quantity != 0) return Invalid("Tombstone carries quantity."); }
                else return Invalid("Unknown item disposition.");
                items.Add(item.id, item);
            }
            var referenced = new HashSet<string>(StringComparer.Ordinal);
            foreach (var opportunity in w.opportunities)
            {
                if (opportunity == null || !AddId(ids, opportunity.id)) return Identity();
                if (opportunity.outcome == OpportunityOutcome.Generated)
                {
                    if (!items.TryGetValue(opportunity.entityId ?? "", out var item) || item.origin != WorldItemOrigin.Loot || !referenced.Add(item.id))
                        return Invalid("Generated opportunity needs exactly one present item or explicit tombstone.");
                }
                else if (opportunity.outcome == OpportunityOutcome.Empty || opportunity.outcome == OpportunityOutcome.Blocked)
                { if (!string.IsNullOrEmpty(opportunity.entityId)) return Invalid("Empty/blocked opportunity cannot own an item."); }
                else return Invalid("Unresolved opportunity cannot be saved.");
            }
            foreach (var item in w.items)
                if (item.origin == WorldItemOrigin.Loot && !referenced.Contains(item.id)) return Invalid("Orphan generated loot.");
            foreach (var enemy in w.enemies)
            {
                if (enemy == null || !AddId(ids, enemy.id)) return Identity();
                if (!Pose(enemy.transform) || !Finite(enemy.health) || enemy.health < 0 || enemy.health > maximumEnemyHealth)
                    return Invalid("Invalid enemy state.");
            }
            if (h.schemaVersion >= 3)
            {
                if (save.cells == null || save.cells.cells == null || save.cells.cells.Length < 1 || save.cells.cells.Length > 16) return Invalid("Missing/excessive cells.");
                var cellIds = new HashSet<string>(StringComparer.Ordinal);
                bool destination = save.cells.playerCell == "resident";
                int count = w.items.Length + w.doors.Length + w.enemies.Length + w.opportunities.Length;
                foreach (var cell in save.cells.cells)
                {
                    if (cell == null || !Id(cell.id) || !cellIds.Add(cell.id) || !LastSignal.WorldTime.WorldTimeSettings.ValidTime(cell.lastProcessed) || cell.lastProcessed > save.worldTime.seconds) return Invalid("Invalid cell identity/time.");
                    if (!cell.visited) continue;
                    var nested = new SaveGame { header = new SaveHeader { schemaVersion=2, worldId=h.worldId,contentVersion=h.contentVersion,generation=h.generation,buildId=h.buildId,timestampUtc=h.timestampUtc },
                        worldTime=save.worldTime,player=save.player,inventory=save.inventory,weapon=save.weapon,shelter=save.shelter,world=cell.world };
                    var valid = Validate(nested); if (!valid.Success) return valid;
                    foreach (var item in cell.world.items)
                    {
                        if (!ids.Add(item.id)) return Identity();
                        if (LastSignal.WorldCells.CellCoordinate.FromWorld(new UnityEngine.Vector3(item.transform.x,item.transform.y,item.transform.z)).Id != cell.id) return Invalid("Item has wrong canonical cell owner.");
                    }
                    foreach (var door in cell.world.doors) if (!ids.Add(door.id)) return Identity();
                    foreach (var enemy in cell.world.enemies) if (!ids.Add(enemy.id)) return Identity();
                    foreach (var point in cell.world.opportunities) if (!ids.Add(point.id)) return Identity();
                    count += cell.world.items.Length + cell.world.doors.Length + cell.world.enemies.Length + cell.world.opportunities.Length;
                    if (count > MaxEntities) return Invalid("Excessive cell entities.");
                    if (cell.id == save.cells.playerCell) destination = true;
                }
                if (h.schemaVersion == 4 && !ValidPopulation(save.population, cellIds, save.worldTime.seconds)) return Invalid("Invalid population state.");
                if (!destination) return Invalid("Player destination is not a visited cell.");
                if (save.cells.playerCell != "resident" && LastSignal.WorldCells.CellCoordinate.FromWorld(new UnityEngine.Vector3(p.transform.x,p.transform.y,p.transform.z)).Id != save.cells.playerCell) return Invalid("Player cell ownership mismatch.");
            }
            return SaveResult.Ok;
        }
        public static bool ValidPopulation(PopulationSnapshot pop, HashSet<string> cells, double now)
        {
            if (pop == null || pop.pressures == null || pop.ledgers == null || pop.migrations == null || pop.actors == null ||
                pop.lastNoiseSequence < 0 || pop.pressures.Length != cells.Count || pop.ledgers.Length != cells.Count ||
                pop.migrations.Length > MaxEntities || pop.actors.Length > MaxEntities) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var receipts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in pop.pressures)
            {
                if (p == null || !cells.Contains(p.cellId ?? "") || !ids.Add(p.cellId) || !Finite(p.pressure) || p.pressure < 0 || p.pressure > 1 ||
                    !LastSignal.WorldTime.WorldTimeSettings.ValidTime(p.lastUpdateTime) || p.lastUpdateTime > now || p.receipts == null || p.receipts.Length > 64) return false;
                foreach (var r in p.receipts)
                    if (r == null || !Id(r.id) || !r.id.StartsWith("noise:", StringComparison.Ordinal) ||
                        !long.TryParse(r.id.Substring(6), out var sequence) || sequence < 1 || sequence > pop.lastNoiseSequence || !receipts.Add(r.id)) return false;
            }
            ids.Clear(); long total = 0;
            var available = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var l in pop.ledgers)
            {
                if (l == null || !cells.Contains(l.cellId ?? "") || !ids.Add(l.cellId) || l.logical < 0 || l.dead < 0) return false;
                total += (long)l.logical + l.dead; available.Add(l.cellId, l.logical);
            }
            ids.Clear();
            foreach (var m in pop.migrations)
            {
                if (m == null || !AddId(ids, m.groupId) || !cells.Contains(m.sourceCellId ?? "") || !cells.Contains(m.targetCellId ?? "") ||
                    m.sourceCellId == m.targetCellId || m.size <= 0 || !LastSignal.WorldTime.WorldTimeSettings.ValidTime(m.departureTime) ||
                    !LastSignal.WorldTime.WorldTimeSettings.ValidTime(m.arrivalTime) || m.departureTime > now || m.arrivalTime <= m.departureTime) return false;
                total += m.size;
            }
            if (total > MaxEntities) return false;
            foreach (var a in pop.actors)
            {
                if (a == null || !AddId(ids, a.id) || !available.TryGetValue(a.cellId ?? "", out int count) || count <= 0 || !Finite(a.health) || a.health <= 0 || a.health > 100) return false;
                available[a.cellId] = count - 1;
            }
            return true;
        }
        SaveResult Container(ContainerSnapshot c, HashSet<string> ids)
        {
            if (c == null || c.capacity < 1 || c.capacity > 256 || c.slots == null || c.slots.Length != c.capacity)
                return Invalid("Invalid container shape.");
            if (!AddId(ids, c.id)) return Identity();
            foreach (var s in c.slots)
            {
                if (s == null) return Invalid("Missing slot record.");
                if (string.IsNullOrEmpty(s.definitionId))
                { if (s.quantity != 0) return Invalid("Empty slot has quantity."); continue; }
                if (!stackLimits.TryGetValue(s.definitionId, out var limit)) return Fail(SaveError.UnknownDefinition, "Unknown inventory definition.");
                if (s.quantity < 1 || s.quantity > limit) return Invalid("Invalid stack quantity.");
            }
            return SaveResult.Ok;
        }
        static bool Id(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
        static bool AddId(HashSet<string> ids, string id) => Id(id) && ids.Add(id);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Pose(TransformSnapshot p)
        {
            if (p == null || !Finite(p.x) || !Finite(p.y) || !Finite(p.z) || Math.Abs(p.x) > 100000 || Math.Abs(p.y) > 100000 || Math.Abs(p.z) > 100000 ||
                !Finite(p.qx) || !Finite(p.qy) || !Finite(p.qz) || !Finite(p.qw)) return false;
            double norm = (double)p.qx*p.qx + (double)p.qy*p.qy + (double)p.qz*p.qz + (double)p.qw*p.qw;
            return Math.Abs(norm - 1) < .001;
        }
        static SaveResult Invalid(string message) => Fail(SaveError.InvalidData, message);
        static SaveResult Identity() => Fail(SaveError.DuplicateIdentity, "Missing, invalid or duplicate entity/container identity.");
        static SaveResult Fail(SaveError error, string message) => new SaveResult(error, message);
    }
}
