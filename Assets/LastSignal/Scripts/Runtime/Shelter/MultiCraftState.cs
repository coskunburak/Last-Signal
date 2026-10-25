using System;
using System.IO;
using System.Security.Cryptography;
using LastSignal.Inventory;

namespace LastSignal.Shelter
{
    public enum CraftReason
    {
        Accepted, Busy, InvalidRequest, UnknownRecipe, RecipeRevisionMismatch, StaleSource,
        UnsupportedSource, MissingStation, MissingCapability, Locked, MissingTool, MissingIngredients,
        NonceConflict, ReceiptExpired, SequenceExhausted, NoPendingResult, DestinationFull, DurabilityUnavailable
    }
    public readonly struct CraftRequest
    {
        public readonly string ShelterId, SessionEpoch, RecipeId, SourceId;
        public readonly long Nonce, ExpectedRevision;
        public readonly int RecipeRevision;
        public readonly bool RequireDurableGeneration;
        public CraftRequest(string shelterId, string sessionEpoch, long nonce, string recipeId, int recipeRevision, string sourceId, long expectedRevision, bool requireDurableGeneration = false)
        { ShelterId = shelterId; SessionEpoch = sessionEpoch; Nonce = nonce; RecipeId = recipeId; RecipeRevision = recipeRevision; SourceId = sourceId; ExpectedRevision = expectedRevision; RequireDurableGeneration = requireDurableGeneration; }
    }
    [Serializable] public sealed class CraftItemSnapshot
    {
        public string itemId;
        public int quantity;
    }
    [Serializable] public sealed class CraftReceipt
    {
        public long nonce, expectedRevision, beforeRevision, afterRevision, sequence;
        public string payloadHash, recipeId, sourceId, jobId;
        public int recipeRevision;
        public CraftReason reason;
        public CraftItemSnapshot[] consumed;
        public CraftReceipt Copy() => new CraftReceipt { nonce = nonce, expectedRevision = expectedRevision, beforeRevision = beforeRevision, afterRevision = afterRevision,
            sequence = sequence, payloadHash = payloadHash, recipeId = recipeId, sourceId = sourceId, jobId = jobId,
            recipeRevision = recipeRevision, reason = reason, consumed = MultiCraftSnapshot.CopyItems(consumed) };
    }
    [Serializable] public sealed class MultiCraftJob
    {
        public string id, recipeId, definitionHash, sourceId;
        public long nonce;
        public int revision, catalogVersion;
        public CraftTimeBasis timeBasis;
        public double duration, progress, simulationStartedAt;
        public CraftStatus status;
        // Only one payload owns units: escrow while Running/Refund; output after completion.
        public CraftItemSnapshot[] escrow, pendingOutput;
        public MultiCraftJob Copy() => new MultiCraftJob { id = id, recipeId = recipeId, definitionHash = definitionHash, sourceId = sourceId,
            nonce = nonce, revision = revision, catalogVersion = catalogVersion, timeBasis = timeBasis, duration = duration,
            progress = progress, simulationStartedAt = simulationStartedAt, status = status, escrow = MultiCraftSnapshot.CopyItems(escrow), pendingOutput = MultiCraftSnapshot.CopyItems(pendingOutput) };
    }
    [Serializable] public sealed class MultiCraftSnapshot
    {
        public const int ReceiptLimit = 32;
        public string sessionEpoch;
        public long highWater;
        public double simulationSeconds;
        public MultiCraftJob job;
        public CraftReceipt[] receipts = Array.Empty<CraftReceipt>();
        public MultiCraftSnapshot Copy()
        {
            var copy = new MultiCraftSnapshot { sessionEpoch = sessionEpoch, highWater = highWater, simulationSeconds = simulationSeconds, job = IsEmptyJob(job) ? null : job.Copy(),
                receipts = new CraftReceipt[receipts?.Length ?? 0] };
            for (int i = 0; i < copy.receipts.Length; i++) copy.receipts[i] = receipts[i]?.Copy();
            return copy;
        }
        internal static CraftItemSnapshot[] CopyItems(CraftItemSnapshot[] items)
        {
            if (items == null) return null;
            var copy = new CraftItemSnapshot[items.Length];
            for (int i = 0; i < copy.Length; i++) if (items[i] != null) copy[i] = new CraftItemSnapshot { itemId = items[i].itemId, quantity = items[i].quantity };
            return copy;
        }
        internal static CraftItemSnapshot[] Items(ItemAmount[] items)
        {
            var copy = new CraftItemSnapshot[items.Length];
            for (int i = 0; i < copy.Length; i++) copy[i] = new CraftItemSnapshot { itemId = items[i].Item.Id.Value, quantity = items[i].Quantity };
            return copy;
        }
        internal static bool Matches(CraftItemSnapshot[] actual, ItemAmount[] expected)
        {
            if (actual == null || actual.Length != expected.Length) return false;
            for (int i = 0; i < actual.Length; i++)
                if (actual[i] == null || actual[i].itemId != expected[i].Item.Id.Value || actual[i].quantity != expected[i].Quantity) return false;
            return true;
        }
        internal static bool Empty(CraftItemSnapshot[] items) => items == null || items.Length == 0;
        internal static bool IsEmptyJob(MultiCraftJob j) => j == null ||
            (string.IsNullOrEmpty(j.id) && string.IsNullOrEmpty(j.recipeId) && string.IsNullOrEmpty(j.definitionHash) &&
             string.IsNullOrEmpty(j.sourceId) && j.nonce == 0 && j.revision == 0 && j.catalogVersion == 0 &&
             j.simulationStartedAt == 0 && j.timeBasis == 0 && j.duration == 0 && j.progress == 0 && j.status == 0 && Empty(j.escrow) && Empty(j.pendingOutput));
        internal static bool IsLegacyEmpty(MultiCraftSnapshot s) => s == null ||
            (string.IsNullOrEmpty(s.sessionEpoch) && s.highWater == 0 && s.simulationSeconds == 0 && IsEmptyJob(s.job) && (s.receipts == null || s.receipts.Length == 0));
    }
    internal static class CraftIdentity
    {
        internal static string Request(CraftRequest r) => Hash(w => {
            w.Write(r.ShelterId); w.Write(r.SessionEpoch); w.Write(r.Nonce); w.Write(r.RecipeId); w.Write(r.RecipeRevision); w.Write(r.SourceId); w.Write(r.ExpectedRevision); w.Write(r.RequireDurableGeneration);
        });
        internal static string Definition(CompiledRecipe r) => Hash(w => {
            w.Write(r.Id); w.Write(r.Revision); w.Write(r.CatalogVersion); w.Write((int)r.Station); w.Write((int)r.Capabilities);
            w.Write((int)r.Unlock); w.Write((int)r.TimeBasis); w.Write(r.Duration); w.Write(r.Power);
            w.Write(r.Inputs.Length);
            for (int i = 0; i < r.Inputs.Length; i++) { Write(w, r.Inputs[i]); w.Write((int)r.Sources[i]); }
            w.Write(r.Outputs.Length); foreach (var a in r.Outputs) Write(w, a);
            w.Write(r.Tools.Length); foreach (var tool in r.Tools) w.Write(tool.Id.Value);
        });
        static void Write(BinaryWriter w, ItemAmount a) { w.Write(a.Item.Id.Value); w.Write(a.Quantity); }
        static string Hash(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) write(writer);
                using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }
        internal static bool ValidHash(string hash)
        {
            if (hash == null || hash.Length != 64) return false;
            foreach (var c in hash) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }
    }
}
