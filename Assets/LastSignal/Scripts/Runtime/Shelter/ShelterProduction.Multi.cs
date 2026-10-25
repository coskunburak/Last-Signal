using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Persistence;
using LastSignal.WorldTime;

namespace LastSignal.Shelter
{
    public sealed partial class ShelterProduction
    {
        RecipeCatalog multiCatalog;
        readonly string initialCraftEpoch = Guid.NewGuid().ToString("N");
        public string CraftEpoch => state.craftingVersion == 1 ? state.crafting.sessionEpoch : initialCraftEpoch;
        static readonly ItemAmount[] NoAmounts = Array.Empty<ItemAmount>();
        MultiCraftJob MultiJob => state.crafting?.job;
        public bool HasMultiJob => MultiJob != null;
        public string MultiRecipeId => MultiJob?.recipeId;
        public long CraftHighWater => state.crafting?.highWater ?? 0;
        public double CraftSimulationSeconds => state.crafting?.simulationSeconds ?? 0;
        bool MutationBlocked => busy || OwnershipTransaction.Active;
        static bool KnownSource(string id) => id == "player.inventory" || id == "shelter.storage";
        MultiCraftSnapshot CraftState() => state.craftingVersion == 1 ? state.crafting : new MultiCraftSnapshot { sessionEpoch = initialCraftEpoch };

        /// <summary>Trusted session facade supplies the actual container for request.SourceId.
        /// Accepted means committed in memory; durability is only established by SaveSession publication.</summary>
        public CraftReceipt StartCraft(CraftRequest request, InventoryContainer source)
        {
            if (MutationBlocked || (source != null && source.IsBusy)) return Response(request, CraftReason.Busy);
            if (request.ShelterId != state.shelterId || request.SessionEpoch != CraftEpoch || request.Nonce <= 0 || request.ExpectedRevision < 0 ||
                !RecipeCatalog.ValidId(request.RecipeId) || request.RecipeRevision < 1)
                return Response(request, CraftReason.InvalidRequest);
            if (!KnownSource(request.SourceId)) return Response(request, CraftReason.UnsupportedSource);
            string hash = CraftIdentity.Request(request);
            var current = CraftState();
            foreach (var old in current.receipts)
                if (old.nonce == request.Nonce) return old.payloadHash == hash ? old.Copy() : Response(request, CraftReason.NonceConflict);
            if (request.Nonce <= current.highWater) return Response(request, CraftReason.ReceiptExpired);
            // Never silently downgrade a command requesting crash-durable cross-domain publication.
            if (request.RequireDurableGeneration) return Response(request, CraftReason.DurabilityUnavailable);
            if (source == null) return Response(request, CraftReason.UnsupportedSource);
            var reason = Requirements(request, source, out var definition);
            var receipt = Response(request, reason);
            receipt.payloadHash = hash;
            receipt.beforeRevision = receipt.afterRevision = source?.Revision ?? request.ExpectedRevision;
            receipt.sequence = state.sequence;
            if (reason != CraftReason.Accepted)
            {
                StoreReceipt(current, receipt);
                return receipt.Copy();
            }
            // Every allocation and definition lookup precedes the atomic ownership commit.
            var job = new MultiCraftJob { id = state.shelterId + ":job:" + (state.sequence + 1), nonce = request.Nonce,
                recipeId = definition.Id, revision = definition.Revision, catalogVersion = definition.CatalogVersion,
                definitionHash = definition.Fingerprint, sourceId = request.SourceId, timeBasis = definition.TimeBasis,
                duration = JobDuration(definition, state.upgraded), simulationStartedAt = current.simulationSeconds, status = CraftStatus.Running, escrow = MultiCraftSnapshot.Items(definition.Inputs),
                pendingOutput = Array.Empty<CraftItemSnapshot>() };
            receipt.jobId = job.id; receipt.sequence++;
            receipt.afterRevision = receipt.beforeRevision + 1;
            receipt.consumed = MultiCraftSnapshot.CopyItems(job.escrow);
            var next = WithReceipt(current, receipt); next.job = job;
            var previous = state.crafting; int oldVersion = state.craftingVersion; long oldSequence = state.sequence;
            busy = true;
            try
            {
                if (!source.ExchangeBatch(request.ExpectedRevision, definition.Inputs, NoAmounts,
                    () => { state.crafting = next; state.craftingVersion = 1; state.sequence = oldSequence + 1; },
                    () => { state.crafting = previous; state.craftingVersion = oldVersion; state.sequence = oldSequence; }))
                {
                    // The full plan failed; no escrow, source revision or accepted receipt was published.
                    receipt.reason = CraftReason.MissingIngredients; receipt.jobId = null;
                    receipt.sequence = oldSequence; receipt.afterRevision = receipt.beforeRevision; receipt.consumed = Array.Empty<CraftItemSnapshot>();
                    StoreReceipt(current, receipt);
                }
            }
            finally { busy = false; }
            return receipt.Copy();
        }
        CraftReason Requirements(CraftRequest request, InventoryContainer source, out CompiledRecipe r)
        {
            r = null;
            if (multiCatalog == null || !multiCatalog.Find(request.RecipeId, out r)) return CraftReason.UnknownRecipe;
            if (request.RecipeRevision != r.Revision) return CraftReason.RecipeRevisionMismatch;
            if (!state.claimed) return CraftReason.Locked;
            if (state.job != null || MultiJob != null) return CraftReason.Busy;
            if (state.sequence == long.MaxValue || source?.Revision >= long.MaxValue - 1) return CraftReason.SequenceExhausted;
            if (source == null) return CraftReason.UnsupportedSource;
            if (source.Revision != request.ExpectedRevision) return CraftReason.StaleSource;
            if (request.SourceId == "shelter.storage" && !state.storage) return CraftReason.UnsupportedSource;
            if (r.Station == CraftStation.Workbench && !state.workbench) return CraftReason.MissingStation;
            var available = state.workbench ? CraftCapability.WoodProcessing : CraftCapability.None;
            if ((r.Capabilities & available) != r.Capabilities) return CraftReason.MissingCapability;
            if ((r.Unlock == CraftUnlock.ClaimedShelter && !state.claimed) || (r.Unlock == CraftUnlock.UpgradedWorkbench && !state.upgraded)) return CraftReason.Locked;
            foreach (var policy in r.Sources)
                if ((policy == IngredientSource.PlayerInventory && request.SourceId != "player.inventory") ||
                    (policy == IngredientSource.ShelterStorage && request.SourceId != "shelter.storage")) return CraftReason.UnsupportedSource;
            foreach (var tool in r.Tools)
            {
                if (source.GetTotalQuantity(tool) < 1) return CraftReason.MissingTool;
                for (int i = 0; i < source.Capacity; i++)
                { var slot = source.GetSlot(i); if (!slot.IsEmpty && slot.Item.Id == tool.Id && slot.Item != tool) return CraftReason.MissingTool; }
            }
            foreach (var input in r.Inputs) if (source.GetTotalQuantity(input.Item) < input.Quantity) return CraftReason.MissingIngredients;
            return CraftReason.Accepted;
        }
        static CraftReceipt Response(CraftRequest r, CraftReason reason) => new CraftReceipt { nonce = r.Nonce, recipeId = r.RecipeId,
            sourceId = r.SourceId, expectedRevision = r.ExpectedRevision, recipeRevision = r.RecipeRevision, reason = reason, consumed = Array.Empty<CraftItemSnapshot>() };
        void StoreReceipt(MultiCraftSnapshot current, CraftReceipt receipt)
        { state.crafting = WithReceipt(current, receipt); state.craftingVersion = 1; }
        static MultiCraftSnapshot WithReceipt(MultiCraftSnapshot current, CraftReceipt receipt)
        {
            int count = Math.Min(current.receipts.Length, MultiCraftSnapshot.ReceiptLimit - 1);
            var receipts = new CraftReceipt[count + 1];
            Array.Copy(current.receipts, current.receipts.Length - count, receipts, 0, count);
            receipts[count] = receipt.Copy();
            return new MultiCraftSnapshot { sessionEpoch = current.sessionEpoch, highWater = receipt.nonce, simulationSeconds = current.simulationSeconds, job = current.job, receipts = receipts };
        }
        bool CancelMulti()
        {
            if (MutationBlocked || MultiJob == null || MultiJob.status != CraftStatus.Running) return false;
            MultiJob.status = CraftStatus.CancelledWaitingRefund; return true;
        }
        public CraftReason CollectCraft(InventoryContainer destination, string destinationId, long expectedRevision)
        {
            if (MutationBlocked || (destination != null && destination.IsBusy)) return CraftReason.Busy;
            var job = MultiJob;
            if (job == null || job.status == CraftStatus.Running) return CraftReason.NoPendingResult;
            // Refunds return to the original owner; outputs may use either explicit supported destination.
            if (destination == null || !KnownSource(destinationId) ||
                (destinationId == "shelter.storage" && !state.storage) ||
                (job.status == CraftStatus.CancelledWaitingRefund && job.sourceId != destinationId)) return CraftReason.UnsupportedSource;
            if (destination.Revision != expectedRevision) return CraftReason.StaleSource;
            if (!multiCatalog.Find(job.recipeId, out var recipe)) return CraftReason.UnknownRecipe;
            var payload = job.status == CraftStatus.CancelledWaitingRefund ? recipe.Inputs : recipe.Outputs;
            busy = true;
            try
            {
                return destination.ExchangeBatch(expectedRevision, NoAmounts, payload,
                    () => state.crafting.job = null, () => state.crafting.job = job) ? CraftReason.Accepted : CraftReason.DestinationFull;
            }
            finally { busy = false; }
        }
        void ProgressMulti(double elapsed, CraftTimeBasis basis, double? absoluteProgress = null)
        {
            var job = MultiJob;
            if (job == null || job.status != CraftStatus.Running || job.timeBasis != basis || elapsed <= 0) return;
            // Compiled definition was checked during construction/restore; no catalog scans.
            multiCatalog.Find(job.recipeId, out var recipe);
            double next = Math.Min(job.duration, absoluteProgress ?? (job.progress + elapsed));
            if (next == job.duration)
            {
                var output = MultiCraftSnapshot.Items(recipe.Outputs);
                job.pendingOutput = output; job.escrow = Array.Empty<CraftItemSnapshot>();
                job.status = CraftStatus.CompletedWaitingOutput;
            }
            job.progress = next;
        }
        /// <summary>Contiguous session simulation time only. Caller does not tick during pause/death/sleep/remote cells.</summary>
        public void ApplySimulationElapsed(double from, double to)
        {
            if (MutationBlocked || !WorldTimeSettings.ValidTime(to) || from != CraftSimulationSeconds || to < from)
                throw new InvalidOperationException("Noncontiguous crafting simulation clock.");
            if (MultiJob == null || MultiJob.timeBasis != CraftTimeBasis.SimulationSeconds) return;
            ProgressMulti(to - from, CraftTimeBasis.SimulationSeconds, to - MultiJob.simulationStartedAt);
            state.crafting.simulationSeconds = to;
        }
        internal void SuspendCraftClockUntil(double now)
        {
            if (MutationBlocked || !WorldTimeSettings.ValidTime(now) || now < state.lastProcessed) throw new InvalidOperationException("Invalid station resume clock.");
            state.lastProcessed = now; // Station unavailable: preserve progress and fuel; do not catch up its suspended interval.
        }
        double MultiBoundary(double now)
        {
            var job = MultiJob;
            if (job == null || job.status != CraftStatus.Running || job.timeBasis != CraftTimeBasis.WorldSeconds) return double.PositiveInfinity;
            multiCatalog.Find(job.recipeId, out var r);
            return r.Power && !Powered ? double.PositiveInfinity : now + Math.Max(.000001, job.duration - job.progress);
        }
        void ApplyMultiWorld(double elapsed, double powered)
        {
            if (MultiJob == null || !multiCatalog.Find(MultiJob.recipeId, out var r)) return;
            ProgressMulti(r.Power ? powered : elapsed, CraftTimeBasis.WorldSeconds);
        }
        static double JobDuration(CompiledRecipe recipe, bool upgraded) =>
            recipe.Duration * (upgraded && recipe.Station == CraftStation.Workbench ? .5 : 1);
        bool CanRestoreMulti(ShelterProductionSnapshot snapshot)
        {
            if (snapshot.craftingVersion == 0) return true;
            var s = snapshot.crafting;
            var job = s.job;
            if (!MultiCraftSnapshot.IsEmptyJob(job))
            {
                if (multiCatalog == null || !multiCatalog.Find(job.recipeId, out var r) || r.Fingerprint != job.definitionHash ||
                    r.Revision != job.revision || r.CatalogVersion != job.catalogVersion || JobDuration(r, snapshot.upgraded) != job.duration || r.TimeBasis != job.timeBasis ||
                    (r.Station == CraftStation.Workbench && !snapshot.workbench)) return false;
                if (job.status == CraftStatus.CompletedWaitingOutput ? !MultiCraftSnapshot.Matches(job.pendingOutput, r.Outputs) : !MultiCraftSnapshot.Matches(job.escrow, r.Inputs)) return false;
            }
            return true;
        }
        public static bool ValidMultiSnapshot(ShelterProductionSnapshot snapshot)
        {
            if (snapshot == null) return false;
            if (snapshot.craftingVersion == 0) return MultiCraftSnapshot.IsLegacyEmpty(snapshot.crafting);
            var s = snapshot.crafting;
            if (!RecipeCatalog.ValidId(snapshot.shelterId) || snapshot.craftingVersion != 1 || s == null || !Guid.TryParseExact(s.sessionEpoch, "N", out _) || s.highWater < 0 || !WorldTimeSettings.ValidTime(s.simulationSeconds) ||
                s.receipts == null || s.receipts.Length > MultiCraftSnapshot.ReceiptLimit || (s.highWater > 0 && s.receipts.Length == 0)) return false;
            long last = 0;
            foreach (var receipt in s.receipts)
            {
                if (receipt == null || receipt.nonce <= last || receipt.nonce > s.highWater || !CraftIdentity.ValidHash(receipt.payloadHash) ||
                    !RecipeCatalog.ValidId(receipt.recipeId) || receipt.recipeRevision < 1 || !KnownSource(receipt.sourceId) ||
                    receipt.expectedRevision < 0 || receipt.beforeRevision < 0 || receipt.afterRevision < 0 || receipt.sequence < 0 || receipt.sequence > snapshot.sequence ||
                    !Enum.IsDefined(typeof(CraftReason), receipt.reason) ||
                    receipt.payloadHash != CraftIdentity.Request(new CraftRequest(snapshot.shelterId, s.sessionEpoch, receipt.nonce,
                        receipt.recipeId, receipt.recipeRevision, receipt.sourceId, receipt.expectedRevision))) return false;
                if (receipt.reason == CraftReason.Accepted)
                {
                    if (receipt.beforeRevision != receipt.expectedRevision || receipt.beforeRevision == long.MaxValue || receipt.afterRevision != receipt.beforeRevision + 1 || receipt.sequence == 0 ||
                        receipt.jobId != snapshot.shelterId + ":job:" + receipt.sequence || !ValidPayload(receipt.consumed)) return false;
                }
                else if (receipt.afterRevision != receipt.beforeRevision || !string.IsNullOrEmpty(receipt.jobId) || !MultiCraftSnapshot.Empty(receipt.consumed)) return false;
                last = receipt.nonce;
            }
            if (last != s.highWater) return false;
            var j = s.job;
            if (MultiCraftSnapshot.IsEmptyJob(j)) return true;
            if (snapshot.job != null || j.nonce <= 0 || j.nonce > s.highWater || snapshot.sequence == 0 || j.id != snapshot.shelterId + ":job:" + snapshot.sequence ||
                !RecipeCatalog.ValidId(j.recipeId) || !CraftIdentity.ValidHash(j.definitionHash) || j.revision <= 0 || j.catalogVersion != 1 || !KnownSource(j.sourceId) ||
                !Enum.IsDefined(typeof(CraftTimeBasis), j.timeBasis) || !Enum.IsDefined(typeof(CraftStatus), j.status) ||
                !WorldTimeSettings.ValidTime(j.simulationStartedAt) || j.simulationStartedAt > s.simulationSeconds ||
                !double.IsFinite(j.duration) || j.duration <= 0 || j.duration > 86400 || !double.IsFinite(j.progress) || j.progress < 0 || j.progress > j.duration) return false;
            foreach (var receipt in s.receipts)
                if (receipt.nonce == j.nonce && (receipt.reason != CraftReason.Accepted || receipt.jobId != j.id ||
                    receipt.recipeId != j.recipeId || receipt.recipeRevision != j.revision || receipt.sourceId != j.sourceId)) return false;
            return j.status == CraftStatus.CompletedWaitingOutput
                ? j.progress == j.duration && MultiCraftSnapshot.Empty(j.escrow) && ValidPayload(j.pendingOutput)
                : j.progress < j.duration && ValidPayload(j.escrow) && MultiCraftSnapshot.Empty(j.pendingOutput);
        }
        static bool ValidPayload(CraftItemSnapshot[] items)
        {
            if (items == null || items.Length == 0 || items.Length > 32) return false;
            string last = null;
            foreach (var item in items)
            {
                if (item == null || !RecipeCatalog.ValidId(item.itemId) || item.quantity < 1 ||
                    (last != null && string.CompareOrdinal(last, item.itemId) >= 0)) return false;
                last = item.itemId;
            }
            return true;
        }
        public static bool ValidCraftItems(ShelterProductionSnapshot snapshot, IReadOnlyDictionary<string, int> limits)
        {
            if (snapshot == null || snapshot.craftingVersion == 0) return true;
            if (!ValidMultiSnapshot(snapshot)) return false;
            bool Check(CraftItemSnapshot[] items)
            {
                if (items == null) return true;
                foreach (var a in items) if (!limits.TryGetValue(a.itemId, out int stack) || a.quantity > 256L * stack) return false;
                return true;
            }
            var s = snapshot.crafting;
            if (s.job != null && (!Check(s.job.escrow) || !Check(s.job.pendingOutput))) return false;
            foreach (var r in s.receipts) if (!Check(r.consumed)) return false;
            return true;
        }
    }
}
