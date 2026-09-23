using System;
using System.IO;
using System.Text;

namespace LastSignal.Persistence
{
    /// <summary>Fault seam covers physical candidate writing and final publication, not fake domain results.</summary>
    public interface ISaveFiles
    {
        bool Exists(string path);
        string Read(string path);
        void WriteCandidate(string path, string json);
        void Publish(string candidate, string current, string backup);
        void DeleteCandidate(string path);
    }
    public sealed class PhysicalSaveFiles : ISaveFiles
    {
        public bool Exists(string path) => File.Exists(path);
        public string Read(string path)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (file.Length > SaveCodec.MaximumBytes) throw new SaveSizeException();
                using (var reader = new StreamReader(file, new UTF8Encoding(false, true), true)) return reader.ReadToEnd();
            }
        }
        public void WriteCandidate(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var bytes = new UTF8Encoding(false, true).GetBytes(json);
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
        }
        public void Publish(string candidate, string current, string backup)
        {
            // Same filesystem; never delete current then move. Unsupported replacement returns failure.
            if (File.Exists(current)) File.Replace(candidate, current, backup);
            else File.Move(candidate, current);
        }
        public void DeleteCandidate(string path) { if (File.Exists(path)) File.Delete(path); }
    }
    sealed class SaveSizeException : IOException { }

    /// <summary>One synchronous owner per save path; no per-frame work, auto-save or implicit recovery.</summary>
    public sealed class SaveFileStore
    {
        readonly SaveCodec codec;
        readonly ISaveFiles files;
        readonly string currentPath;
        bool busy;
        long sessionGeneration;
        public long SessionGeneration => sessionGeneration;
        public SaveFileStore(string currentPath, SaveCodec codec, ISaveFiles files = null)
        {
            this.currentPath = Path.GetFullPath(currentPath ?? throw new ArgumentNullException(nameof(currentPath)));
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
            this.files = files ?? new PhysicalSaveFiles();
        }
        /// <summary>Composition root advances on every session start/end; stale operation receipts fail closed.</summary>
        public long AdvanceSession() => ++sessionGeneration;
        public SaveResult Read(long expectedSession, out SaveGame state, bool backup = false)
        {
            state = null;
            if (busy) return new SaveResult(SaveError.Busy, "Save operation already in progress.");
            if (expectedSession != sessionGeneration) return Stale();
            busy = true;
            try
            {
                var result = ReadPath(backup ? currentPath + ".bak" : currentPath, out var candidate);
                if (expectedSession != sessionGeneration) return Stale();
                if (result.Success) state = candidate;
                return result;
            }
            finally { busy = false; }
        }
        public SaveResult Write(SaveGame state, long expectedSession)
        {
            if (busy) return new SaveResult(SaveError.Busy, "Save operation already in progress.");
            if (expectedSession != sessionGeneration) return Stale();
            busy = true;
            string temporary = null;
            try
            {
                var result = codec.Encode(state, out var json); if (!result.Success) return result;
                // Detach caller-owned DTOs. No live object or caller mutation can change this candidate later.
                result = codec.Decode(json, out var detached); if (!result.Success) return result;
                if (files.Exists(currentPath))
                {
                    result = ReadPath(currentPath, out var previous);
                    if (!result.Success) return result; // Never overwrite an unreadable current/valid backup implicitly.
                    if (detached.header.generation <= previous.header.generation)
                        return new SaveResult(SaveError.StaleGeneration, "Save generation must increase.");
                }
                temporary = currentPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                files.WriteCandidate(temporary, json);
                result = ReadPath(temporary, out var verified); if (!result.Success) return result;
                if (verified.header.generation != detached.header.generation)
                    return new SaveResult(SaveError.InvalidData, "Candidate generation changed during write.");
                // Full exact candidate comparison, in addition to content/schema/checksum validation.
                if (files.Read(temporary) != json) return new SaveResult(SaveError.ChecksumMismatch, "Candidate differs from captured snapshot.");
                if (expectedSession != sessionGeneration) return Stale();
                files.Publish(temporary, currentPath, currentPath + ".bak");
                return SaveResult.Ok;
            }
            catch (SaveSizeException) { return new SaveResult(SaveError.TooLarge, "Save exceeds byte limit."); }
            catch (Exception e) when (IsFileError(e)) { return new SaveResult(SaveError.IoFailure, e.GetType().Name + ": save publication failed."); }
            finally
            {
                if (temporary != null)
                    try { files.DeleteCandidate(temporary); } catch (Exception e) when (IsFileError(e)) { /* Orphan temp is never considered committed. */ }
                busy = false;
            }
        }
        SaveResult ReadPath(string path, out SaveGame state)
        {
            state = null;
            try
            {
                if (!files.Exists(path)) return new SaveResult(SaveError.MissingFile, "No committed save exists.");
                return codec.Decode(files.Read(path), out state);
            }
            catch (SaveSizeException) { return new SaveResult(SaveError.TooLarge, "Save exceeds byte limit."); }
            catch (Exception e) when (IsFileError(e)) { return new SaveResult(SaveError.IoFailure, e.GetType().Name + ": save read failed."); }
        }
        static bool IsFileError(Exception e) => e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is System.Security.SecurityException || e is DecoderFallbackException;
        static SaveResult Stale() => new SaveResult(SaveError.StaleSession, "Session changed; operation discarded.");
    }
}
