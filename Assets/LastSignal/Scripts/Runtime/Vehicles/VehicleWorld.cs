using UnityEngine;

namespace LastSignal.Vehicles
{
    /// <summary>One authored resident pickup per session. Portal travel does not move its rigidbody.</summary>
    [DisallowMultipleComponent]
    public sealed class VehicleWorld : MonoBehaviour
    {
        [SerializeField] VehicleActor prefab;
        [SerializeField] Vector3 spawnPosition;
        [SerializeField] Vector3 spawnEuler;
        [SerializeField] Bounds residentBounds = new Bounds(new Vector3(-200, 0, 0), new Vector3(400, 100, 400));
        public bool ContainsResidentPosition(Vector3 position) => residentBounds.Contains(position);
        public VehicleActor Prefab => prefab;
        public VehicleActor Actor { get; private set; }
        public void Begin(SessionFlow flow)
        {
            End();
            if (!prefab) throw new System.InvalidOperationException("Missing resident pickup prefab.");
            Actor = Instantiate(prefab, spawnPosition, Quaternion.Euler(spawnEuler));
            Actor.Bind(flow);
        }
        public void Suspend() { if (Actor) Actor.Suspend(); }
        public void End()
        {
            if (!Actor) return;
            Actor.Suspend(); Actor.gameObject.SetActive(false); Destroy(Actor.gameObject); Actor = null;
        }
        void OnDestroy() => End();
    }
}
