using System;
using System.Security.Cryptography;
using System.Text;
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
                // Unity JsonUtility materializes null serializable objects. Schema 1 has no time section.
                if (candidate != null && candidate.header != null && candidate.header.schemaVersion == 1) candidate.worldTime = null;
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
    }
}
