using LastSignal.Persistence;

namespace LastSignal.Gathering
{
    public enum DurableOutcome { Committed, Aborted, RecoveryRequired }
    public static class HarvestDurability
    {
        /// <summary>Synchronous, detached full generation. Ambiguous publish never blindly refunds/retries.</summary>
        public static DurableOutcome Publish(SaveFileStore store, SaveGame candidate, out SaveResult result)
        {
            var session=store.SessionGeneration;
            result=store.Read(session,out var previous);
            if (!result.Success && result.Error!=SaveError.MissingFile) return DurableOutcome.Aborted;
            if (previous?.header.generation == long.MaxValue)
            { result=new SaveResult(SaveError.InvalidData,"Generation exhausted."); return DurableOutcome.Aborted; }
            candidate.header.generation=previous == null ? 1 : previous.header.generation+1;
            result=store.Write(candidate,session);
            if (result.Success) return DurableOutcome.Committed;
            if (!store.LastWriteMayHavePublished) return DurableOutcome.Aborted;
            var read=store.Read(session,out var published);
            if (read.Success && published.header.generation==candidate.header.generation &&
                store.Equivalent(published,candidate))
            { result=SaveResult.Ok; return DurableOutcome.Committed; }
            if (read.Success && previous != null && store.Equivalent(previous,published)) return DurableOutcome.Aborted;
            if (previous == null && read.Error==SaveError.MissingFile) return DurableOutcome.Aborted;
            return DurableOutcome.RecoveryRequired;
        }
    }
}
