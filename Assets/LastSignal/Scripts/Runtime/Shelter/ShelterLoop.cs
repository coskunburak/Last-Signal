using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using UnityEngine;

namespace LastSignal.Shelter
{
    public enum ExpeditionState { Inactive, Shelter, Preparing, Expedition, Dead }

    [DisallowMultipleComponent]
    public sealed class ShelterLoop : MonoBehaviour
    {
        [SerializeField, Range(1,256)] int storageCapacity = 48;
        [SerializeField] Transform insideAnchor, outsideAnchor;
        [SerializeField] ShelterPoint storagePoint, exitPoint, returnPoint;
        [SerializeField] ShelterStorageUI preparationUI;
        SessionFlow session;
        PlayerInventory inventory;
        PlayerHealth health;
        CharacterController capsule;
        bool transitioning;
        public event Action Changed;
        public ExpeditionState State { get; private set; }
        public int ExpeditionIndex { get; private set; }
        public ShelterStorage Storage { get; private set; }
        public PlayerInventory Inventory => inventory;
        public SessionFlow Session => session;
        public bool Preparing => State == ExpeditionState.Preparing;
        public ShelterPoint StoragePoint => storagePoint;
        public ShelterPoint ExitPoint => exitPoint;
        public ShelterPoint ReturnPoint => returnPoint;
        public Transform InsideAnchor => insideAnchor;
        public Transform OutsideAnchor => outsideAnchor;
        public ShelterStorageUI PreparationUI => preparationUI;

        public bool Validate(out string error)
        {
            error = null;
            if (storageCapacity < 1 || storageCapacity > 256) error = "Storage capacity must be 1–256.";
            else if (!insideAnchor || !outsideAnchor || !storagePoint || !exitPoint || !returnPoint || !preparationUI) error = "Assign both anchors, all three points and preparation UI.";
            else if (!GetComponent<SessionFlow>() || GetComponents<ShelterLoop>().Length != 1) error = "One ShelterLoop must share the SessionFlow object.";
            else if (insideAnchor.gameObject.scene != gameObject.scene || outsideAnchor.gameObject.scene != gameObject.scene || storagePoint.gameObject.scene != gameObject.scene || exitPoint.gameObject.scene != gameObject.scene || returnPoint.gameObject.scene != gameObject.scene || preparationUI.gameObject.scene != gameObject.scene) error = "All shelter references must belong to this scene.";
            else if (storagePoint == exitPoint || storagePoint == returnPoint || exitPoint == returnPoint || insideAnchor == outsideAnchor) error = "Shelter points and anchors must be distinct.";
            else if (storagePoint.Loop != this || exitPoint.Loop != this || returnPoint.Loop != this || storagePoint.Action != ShelterAction.Prepare || exitPoint.Action != ShelterAction.Leave || returnPoint.Action != ShelterAction.Return) error = "Assign each point to this loop and its correct action.";
            return error == null;
        }
        public void Begin(SessionFlow flow)
        {
            End();
            if (!Validate(out var error)) { Debug.LogError("Shelter authoring: " + error, this); return; }
            session = flow; inventory = flow.Player.GetComponent<PlayerInventory>(); health = flow.Player.GetComponent<PlayerHealth>();
            capsule = flow.Player.GetComponent<CharacterController>();
            Storage = new ShelterStorage(storageCapacity); ExpeditionIndex = 0; State = ExpeditionState.Shelter;
            health.Died += OnDeath;
            preparationUI.Bind(this);
            Changed?.Invoke();
        }
        public void End()
        {
            if (health) health.Died -= OnDeath;
            if (preparationUI) preparationUI.Unbind();
            State = ExpeditionState.Inactive; Storage = null; inventory = null; health = null; capsule = null; session = null; ExpeditionIndex = 0;
            Changed?.Invoke();
        }
        bool Alive => isActiveAndEnabled && session && session.Player && health && health.IsAlive && Storage != null;
        public bool CanUse(ShelterPoint point)
        {
            if (!Alive || session.Paused || transitioning || !point || point.Loop != this) return false;
            if ((session.Player.transform.position - point.transform.position).sqrMagnitude > point.UseRange * point.UseRange) return false;
            return point == storagePoint ? preparationUI && preparationUI.isActiveAndEnabled && State == ExpeditionState.Shelter : point == exitPoint ? State == ExpeditionState.Shelter : point == returnPoint && State == ExpeditionState.Expedition;
        }
        public bool TryUse(ShelterPoint point)
        {
            if (!CanUse(point)) return false;
            transitioning = true;
            try
            {
                if (point == storagePoint)
                {
                    State = ExpeditionState.Preparing;
                    session.Pause(); // Existing modal policy freezes the current reload timer until resume.
                    preparationUI.Show();
                }
                else
                {
                    var anchor = point == exitPoint ? outsideAnchor : insideAnchor;
                    // Short authored doorway traversal, in the same world. No scene load/session reset.
                    if (!AnchorClear(anchor)) return false;
                    var combat = session.Player.GetComponent<PlayerCombatController>();
                    if (combat) combat.CancelGameplayActions(false);
                    bool enabledCapsule = capsule && capsule.enabled;
                    if (capsule) capsule.enabled = false;
                    session.Player.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                    if (capsule) capsule.enabled = enabledCapsule;
                    Physics.SyncTransforms();
                    State = point == exitPoint ? ExpeditionState.Expedition : ExpeditionState.Shelter;
                    if (point == exitPoint) ExpeditionIndex++;
                }
                Changed?.Invoke(); return true;
            }
            finally { transitioning = false; }
        }
        bool AnchorClear(Transform anchor)
        {
            if (!capsule) return false;
            float radius = capsule.radius;
            float half = Mathf.Max(radius, capsule.height * .5f);
            Vector3 center = anchor.position + capsule.center;
            // Ignore Player layer (8), raycast-ignore layer (2), and trigger volumes.
            return !Physics.CheckCapsule(center + Vector3.up * (half-radius), center - Vector3.up * (half-radius), radius * .95f, ~((1<<8)|(1<<2)), QueryTriggerInteraction.Ignore);
        }
        public void ClosePreparation()
        {
            if (!Preparing) return;
            State = health && health.IsAlive ? ExpeditionState.Shelter : ExpeditionState.Dead;
            preparationUI.Hide(); Changed?.Invoke();
            if (session && !session.InMenu) session.Resume();
        }
        public TransferResult Transfer(bool deposit, ItemDefinition item, int quantity)
        {
            if (!Alive || !Preparing || !preparationUI.IsOpen) return new TransferResult(quantity,0,TransferReason.Unavailable);
            return deposit ? ItemTransferService.Deposit(inventory,Storage,item,quantity) : ItemTransferService.Withdraw(Storage,inventory,item,quantity);
        }
        void OnDeath() { State = ExpeditionState.Dead; preparationUI.Hide(); Changed?.Invoke(); }
        void OnDisable() { if (session) End(); }
        void OnDestroy() => End();
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            if (insideAnchor) Gizmos.DrawWireSphere(insideAnchor.position,.4f);
            if (outsideAnchor) Gizmos.DrawWireSphere(outsideAnchor.position,.4f);
            if (insideAnchor && outsideAnchor) Gizmos.DrawLine(insideAnchor.position,outsideAnchor.position);
        }
    }
}
