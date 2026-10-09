using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEngine;

namespace LastSignal
{
    [DisallowMultipleComponent]
    public sealed class PlayerSurvival : MonoBehaviour, IWorldTimeParticipant
    {
        public const string BackpackId = "equipment.field-pack";
        public const int BackpackSlots = 8;
        public const float BandageSeconds = 3;
        public SurvivalState State { get; private set; } = new SurvivalState();
        public ItemDefinition Backpack { get; private set; }
        public int BaseCapacity { get; private set; }
        public bool ApplyingTreatment => treatment != null;
        public float TreatmentRemaining => ApplyingTreatment ? Mathf.Max(0, treatment.TreatmentSeconds - elapsed) : 0;
        public string Feedback { get; private set; } = "";
        SessionFlow flow;
        PlayerInventory inventory;
        PlayerHealth health;
        PlayerInputReader input;
        PlayerStamina stamina;
        FirstPersonMotor motor;
        WorldSimulation registered;
        ItemDefinition treatment;
        int treatmentSlot;
        long itemRevision, woundRevision;
        float elapsed;
        Vector3 treatmentPosition;
        public void Initialize(SessionFlow owner, PlayerInventory carried)
        {
            if (health) health.DamageAccepted -= OnDamage;
            flow = owner; inventory = carried; BaseCapacity = carried.Capacity;
            health = GetComponent<PlayerHealth>(); input = GetComponent<PlayerInputReader>();
            stamina = GetComponent<PlayerStamina>(); motor = GetComponent<FirstPersonMotor>();
            if (health) health.DamageAccepted += OnDamage;
            if (stamina) stamina.SetRecoveryMultiplier(State.RecoveryMultiplier * (Backpack ? Backpack.EquippedRecoveryMultiplier : 1));
            BindClock();
        }
        void OnEnable()
        {
            if (!flow) return;
            if (health) { health.DamageAccepted -= OnDamage; health.DamageAccepted += OnDamage; }
            BindClock(); SyncRecovery();
        }
        public void BindClock()
        {
            var next = flow ? flow.GetComponent<WorldClock>()?.Simulation : null;
            if (registered == next) return;
            if (registered != null && !registered.Advancing) registered.Unregister(this);
            registered = next;
            registered?.Register(this);
        }
        void OnDisable()
        {
            CancelTreatment("Tedavi iptal edildi.");
            if (health) health.DamageAccepted -= OnDamage;
            if (registered != null && !registered.Advancing) registered.Unregister(this);
            registered = null;
        }
        void OnDamage(DamageInfo info)
        {
            if (info.Category == DamageCategory.Survival) return;
            CancelTreatment("Hasar aldın; bandaj tüketilmedi.");
            if (health.IsAlive && (info.Category == DamageCategory.Bullet || info.Category == DamageCategory.Melee))
                State.AddWound();
            SyncRecovery();
        }
        void SyncRecovery() { if (stamina) stamina.SetRecoveryMultiplier(State.RecoveryMultiplier * (Backpack ? Backpack.EquippedRecoveryMultiplier : 1)); }
        bool Usable => isActiveAndEnabled && inventory && health && health.IsAlive && flow && flow.Player == gameObject &&
            !flow.Restoring && (flow.Screen == SessionScreen.Inventory ||
                (flow.Screen == SessionScreen.Gameplay && input && input.GameplayActive && Time.timeScale > 0)) &&
            !(flow.GetComponent<WorldClock>()?.Sleeping ?? false) &&
            !(flow.GetComponent<Vehicles.VehicleWorld>()?.Actor?.Occupied ?? false) && !OwnershipTransaction.Active;
        public bool Use(int slot, long revision)
        {
            if (!Usable || ApplyingTreatment) return Reject("Şu anda eşya kullanılamaz.");
            if (revision != inventory.Revision) return Reject("Envanter değişti. Eşyayı yeniden seç.");
            var selected = inventory.GetSlot(slot);
            if (selected.IsEmpty) return Reject("Eşya bulunamadı.");
            var item = selected.Item;
            if (!item.HasValidUse) return Reject("Eşyanın kullanım tanımı geçersiz.");
            switch (item.Use)
            {
                case ItemUse.Hydrate:
                    if (State.Hydration >= 100) return Reject("Su ihtiyacın yok; eşya tüketilmedi.");
                    return Consume(item, slot, () => State.Drink(item.HydrationPoints), "Su içildi.");
                case ItemUse.Nourish:
                    if (State.Nutrition >= 100) return Reject("Yemek ihtiyacın yok; eşya tüketilmedi.");
                    return Consume(item, slot, () => State.Eat(item.NutritionPoints), "Yemek yenildi.");
                case ItemUse.Bandage:
                    if (State.Bleeding == 0) return Reject("Kanayan yara yok; tedavi eşyası tüketilmedi.");
                    treatment = item; treatmentSlot = slot; itemRevision = revision;
                    woundRevision = State.WoundRevision; elapsed = 0;
                    treatmentPosition = transform.position;
                    Feedback = "Tedavi uygulanıyor. Hareket etme; hasar veya menü tedaviyi iptal eder.";
                    if (flow.InventoryOpen) flow.InventoryView.Close();
                    GetComponent<PlayerCombatController>()?.CancelGameplayActions(false);
                    return true;
                case ItemUse.Backpack:
                    if (Backpack == item) return Reject("Bu çanta zaten takılı.");
                    var old = Backpack;
                    bool equipped = inventory.Container.ExchangeCapacity(item, old, BaseCapacity + item.BackpackSlotBonus, () => { Backpack = item; SyncRecovery(); }, slot);
                    if (!equipped) return Reject("Çanta takılamadı: yer veya işlem uygun değil.");
                    Feedback = "Çanta takıldı: +" + item.BackpackSlotBonus + " yuva."; return true;
                default: return Reject("Bu eşya doğrudan kullanılamaz.");
            }
        }
        bool Consume(ItemDefinition item, int slot, Action effect, string message)
        {
            if (!inventory.Container.Exchange(item, 1, null, 0, () => { effect(); SyncRecovery(); }, slot))
                return Reject("Eşya değişti veya işlem meşgul.");
            Feedback = message; return true;
        }
        public bool UnequipBackpack()
        {
            if (!Usable || ApplyingTreatment || !Backpack) return Reject("Çanta çıkarılamıyor.");
            var old = Backpack;
            if (!inventory.Container.ExchangeCapacity(null, old, BaseCapacity, () => { Backpack = null; SyncRecovery(); }))
                return Reject("Önce çanta kapasitesini boşalt. Hiçbir eşya silinmedi.");
            Feedback = "Çanta çıkarıldı."; return true;
        }
        bool Reject(string reason) { Feedback = reason; return false; }
        public void CancelTreatment(string reason)
        { if (!ApplyingTreatment) return; treatment = null; elapsed = 0; Feedback = reason; }
        void Update() => TickTreatment(Time.deltaTime);
        // Called by Update; explicit delta also makes interruption and exact-commit tests deterministic.
        public void TickTreatment(float seconds)
        {
            if (!ApplyingTreatment) return;
            if (!Usable || flow.Screen != SessionScreen.Gameplay || !input || !input.GameplayActive ||
                input.Move.sqrMagnitude > .001f || input.SprintHeld || (motor && motor.IsSliding) ||
                (transform.position - treatmentPosition).sqrMagnitude > .01f ||
                inventory.Revision != itemRevision || !State.CanTreat(woundRevision))
            { CancelTreatment("Tedavi iptal edildi; bandaj tüketilmedi."); return; }
            if (!float.IsFinite(seconds) || seconds <= 0) return;
            elapsed += seconds;
            if (elapsed < treatment.TreatmentSeconds) return;
            var item = treatment;
            var slot = inventory.GetSlot(treatmentSlot);
            treatment = null; elapsed = 0;
            if (slot.IsEmpty || slot.Item != item) { Reject("Bandaj değişti; tedavi iptal edildi."); return; }
            Consume(item, treatmentSlot, () => State.Treat(item.BleedingReduction), "Tedavi uygulandı; kalan kanama: " + Math.Max(0, State.Bleeding - item.BleedingReduction));
        }
        public double NextBoundary(double now)
        {
            var interval = State.NextBoundarySeconds;
            if (health && State.DamagePerWorldSecond > 0)
                interval = Math.Min(interval, health.CurrentHealth / State.DamagePerWorldSecond);
            return now + Math.Max(.001, interval); // Above the clock's resolution even near MaximumTime.
        }
        public void ApplyElapsed(double from, double to)
        {
            if (!health || !health.IsAlive) return;
            double damage = State.Advance(to - from);
            SyncRecovery();
            if (damage > 0) health.TakeDamage(new DamageInfo { Amount = (float)damage, Category = DamageCategory.Survival });
        }
        public AdvanceReason Inspect(double now) => !health || !health.IsAlive ? AdvanceReason.Dead : AdvanceReason.Completed;
        public SurvivalSnapshot Capture()
        {
            if (ApplyingTreatment) throw new InvalidOperationException("Finish or cancel bandaging before saving.");
            return State.Capture(BaseCapacity, Backpack ? Backpack.Id.Value : null);
        }
        public void Restore(SurvivalSnapshot saved, ItemCatalog catalog)
        {
            CancelTreatment("Kayıt yüklendi.");
            if (saved == null) { State = new SurvivalState(); BaseCapacity = inventory.Capacity; Backpack = null; }
            else
            {
                State.Restore(saved); BaseCapacity = saved.baseCapacity;
                Backpack = string.IsNullOrEmpty(saved.backpackId) ? null : catalog.GetItem(new StableItemId(saved.backpackId));
                if ((!string.IsNullOrEmpty(saved.backpackId) && (!Backpack || Backpack.Use != ItemUse.Backpack || !Backpack.HasValidUse)) ||
                    inventory.Capacity != BaseCapacity + (Backpack ? Backpack.BackpackSlotBonus : 0))
                    throw new InvalidOperationException("Saved equipment does not match inventory capacity.");
            }
            SyncRecovery();
        }
    }
}
