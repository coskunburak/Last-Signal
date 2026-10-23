using UnityEngine;

namespace LastSignal.Gathering
{
    /// <summary>Authored projection. No Update, generated IDs, damage owner or autonomous rewards.</summary>
    public sealed class ResourceNodeView : MonoBehaviour, IInteractable
    {
        public string nodeId;
        public GameObject standingVisual;
        public Collider retryCollider;
        public GatheringWorld world;
        public TreePhysicsBody physicsBody;
        public bool Available => world && world.Ready && (world.Pending(nodeId) || (physicsBody && !physicsBody.Fractured));
        public string Prompt => !physicsBody && !(world && world.Pending(nodeId)) ? "This tree is not ready for chopping" :
            physicsBody && physicsBody.Fractured && !physicsBody.CanCollect ? "Wait for the tree to settle" :
            world && world.Pending(nodeId) ? "Collect timber at the stump — clear ground required" :
            world && !world.ToolUsable ? "Axe broken" : "Requires axe — aim at the trunk notch";
        public bool TryInteract() => Available && world.RetryReward(this);
        void OnEnable() { if (world) world.Register(this); }
        void OnDisable() { if (world) world.Unregister(this); }
        public void Present(ResourceNodeState state)
        {
            if (physicsBody)
            {
                physicsBody.Present(state);
                if (retryCollider) retryCollider.enabled=state!=null && state.reward==RewardLocation.Pending;
                return;
            }
            // The authored anchor collider serves two roles:
            // 1. Melee targeting while the resource node is alive.
            // 2. Reward retry interaction while the reward is pending.
            //
            // Disable it when the resource is depleted and has no
            // pending interaction, preventing invalid repeated hits.
            if (retryCollider)
            {
                retryCollider.enabled =
                    state != null &&
                    (
                        state.health > 0 ||
                        state.reward == RewardLocation.Pending
                    );
            }

            // S003 only projects the authoritative resource state.
            // Controlled falling and cut visuals belong to S004.
            if (standingVisual && standingVisual != gameObject)
            {
                standingVisual.SetActive(
                    state == null || state.health > 0
                );
            }
        }
    }
}
