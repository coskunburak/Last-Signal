using System;
using System.Security.Cryptography;
using System.Text;
using LastSignal.WorldCells;
using UnityEngine;

namespace LastSignal.Persistence
{
    public sealed class SaveCodec
    {
        public const int MaximumBytes = 8 * 1024 * 1024;
        [Serializable] sealed class Envelope { public string format, checksum, payload; }
        readonly SaveValidation validation;
        public SaveCodec(SaveValidation validation) { this.validation = validation ?? throw new ArgumentNullException(nameof(validation)); }
        public SaveResult Encode(SaveGame state, out string json)
        {
            json = null;
            var result = validation.Validate(state); if (!result.Success) return result;
            string payload = JsonUtility.ToJson(state, true);
            json = JsonUtility.ToJson(new Envelope { format = "last-signal-save-1", checksum = Hash(payload), payload = payload }, true);
            if (Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            { json = null; return new SaveResult(SaveError.TooLarge, "Save exceeds byte limit."); }
            return SaveResult.Ok;
        }
        public SaveResult Decode(string json, out SaveGame state)
        {
            state = null;
            if (json == null) return new SaveResult(SaveError.InvalidJson, "Missing JSON.");
            if (json.Length > MaximumBytes || Encoding.UTF8.GetByteCount(json) > MaximumBytes) return new SaveResult(SaveError.TooLarge, "Save exceeds byte limit.");
            try
            {
                var envelope = JsonUtility.FromJson<Envelope>(json);
                if (envelope == null || envelope.format != "last-signal-save-1" || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.checksum))
                    return new SaveResult(SaveError.InvalidData, "Missing save envelope.");
                if (!string.Equals(Hash(envelope.payload), envelope.checksum, StringComparison.Ordinal))
                    return new SaveResult(SaveError.ChecksumMismatch, "Save payload checksum mismatch.");
                var candidate = JsonUtility.FromJson<SaveGame>(envelope.payload);
                var survival = candidate?.survival;
                if (candidate?.header != null && candidate.header.survivalVersion == 0 && survival != null &&
                    survival.version == 0 && survival.hydration == 0 && survival.nutrition == 0 && survival.bleeding == 0 &&
                    survival.woundRevision == 0 && survival.baseCapacity == 0 && string.IsNullOrEmpty(survival.backpackId))
                    candidate.survival = null;
                // JsonUtility materializes absent optional serializable objects as empty instances.
                // Keep old enemy/population saves semantically intact while rejecting partial anatomy data.
                if (candidate?.world?.enemies != null)
                    foreach (var enemy in candidate.world.enemies)
                        if (enemy != null) NormalizeAnatomy(ref enemy.anatomy);
                NormalizeCellAnatomy(candidate?.cells);
                if (candidate?.population?.actors != null)
                    foreach (var actor in candidate.population.actors)
                        if (actor != null) NormalizeAnatomy(ref actor.anatomy);
                var resident = candidate?.population?.residentPressure;
                if (resident != null && string.IsNullOrEmpty(resident.cellId) && resident.pressure == 0 && resident.lastUpdateTime == 0 &&
                    (resident.receipts == null || resident.receipts.Length == 0)) candidate.population.residentPressure = null;
                // Unity JsonUtility materializes null serializable objects. Schema 1 has no time section.
                if (candidate != null && candidate.header != null && candidate.header.schemaVersion == 1) candidate.worldTime = null;
                if (candidate != null && candidate.header != null && candidate.header.schemaVersion < 3) candidate.cells = null;
                if (candidate?.header != null &&
                    candidate.header.schemaVersion < 4 &&
                    candidate.population != null)
                {
                    var population = candidate.population;

                    // Eski şemalarda population yoktur. Unity'nin oluşturduğu
                    // tamamen boş nesneyi yok kabul et; gerçek veriyi silme.
                    bool emptyPopulation =
                        population.lastNoiseSequence == 0 && population.residentPressure == null &&
                        (population.pressures == null ||
                        population.pressures.Length == 0) &&
                        (population.ledgers == null ||
                        population.ledgers.Length == 0) &&
                        (population.migrations == null ||
                        population.migrations.Length == 0) &&
                        (population.actors == null ||
                        population.actors.Length == 0);

                    if (emptyPopulation)
                    {
                        candidate.population = null;
                    }
                }
                if (candidate != null && candidate.combat != null && candidate.combat.version == 0) candidate.combat = null;
                if (candidate?.header != null && candidate.header.progressionVersion == 0 && LastSignal.Objectives.RelayProgression.Empty(candidate.progression)) candidate.progression = null;
                var production = candidate?.shelter?.production;
                if (production != null)
                {
                    // JsonUtility emits/materializes zero-filled objects for null optional
                    // serializable classes. Normalize ONLY the exact empty representation.
                    var job = production.job;
                    bool emptyJob = job == null || (string.IsNullOrEmpty(job.id) && string.IsNullOrEmpty(job.recipeId) &&
                        string.IsNullOrEmpty(job.inputId) && string.IsNullOrEmpty(job.outputId) && job.revision == 0 &&
                        job.inputQuantity == 0 && job.outputQuantity == 0 && job.progress == 0 && job.duration == 0 && (int)job.status == 0);
                    if (emptyJob) production.job = null;
                    if (production.version == 0 && string.IsNullOrEmpty(production.shelterId) && !production.claimed && !production.bed &&
                        !production.storage && !production.workbench && !production.upgraded && !production.generatorEnabled &&
                        production.fuelSeconds == 0 && production.lastProcessed == 0 && production.sequence == 0 && emptyJob)
                        candidate.shelter.production = null;
                }
                if (candidate?.header != null && candidate.header.vehicleVersion == 0 && candidate.vehicles != null &&
                    candidate.vehicles.version == 0 && string.IsNullOrEmpty(candidate.vehicles.occupiedVehicleId) &&
                    (candidate.vehicles.vehicles == null || candidate.vehicles.vehicles.Length == 0)) candidate.vehicles = null;
                var result = validation.Validate(candidate); if (!result.Success) return result;
                state = candidate;
                return SaveResult.Ok;
            }
            catch (ArgumentException) { return new SaveResult(SaveError.InvalidJson, "Malformed save JSON."); }
        }
        static string Hash(string payload)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
        }
        public static void NormalizeCellAnatomy(CellWorldSnapshot cells)
        {
            if (cells?.cells == null) return;
            foreach (var cell in cells.cells)
                if (cell?.world?.enemies != null)
                    foreach (var enemy in cell.world.enemies)
                        if (enemy != null) NormalizeAnatomy(ref enemy.anatomy);
        }
        static void NormalizeAnatomy(ref ZombieAnatomySnapshot anatomy)
        {
            if (anatomy != null && anatomy.severedMask == 0 && anatomy.torsoDamage == 0 &&
                (anatomy.regionalDamage == null || anatomy.regionalDamage.Length == 0))
                anatomy = null;
        }
    }
}
