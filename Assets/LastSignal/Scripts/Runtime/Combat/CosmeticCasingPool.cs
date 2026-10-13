using UnityEngine;
using UnityEngine.Rendering;
namespace LastSignal
{
    /// <summary>Ten cosmetic shells, with bounded swept contact and no physics/AI participation.</summary>
    public sealed class CosmeticCasingPool : MonoBehaviour
    {
        const int Capacity = 10;
        readonly Transform[] shells = new Transform[Capacity];
        readonly Vector3[] velocity = new Vector3[Capacity];
        readonly float[] expiry = new float[Capacity];
        int next;
        public void Initialize(Material material)
        {
            for (int i = 0; i < Capacity; i++)
            {
                var shell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shell.name = "Cosmetic brass"; shell.layer = 2; shell.transform.SetParent(transform, false);
                var collider = shell.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
                shell.transform.localScale = new Vector3(.008f, .009f, .008f);
                var renderer = shell.GetComponent<Renderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                shells[i] = shell.transform; shell.SetActive(false);
            }
        }
        public void Eject(Transform point)
        {
            if (!point || !shells[0]) return;
            int i = next; next = (next + 1) % Capacity;
            shells[i].SetPositionAndRotation(point.position, point.rotation);
            velocity[i] = point.right * 1.2f + Vector3.up * .65f;
            expiry[i] = Time.time + 2.5f; shells[i].gameObject.SetActive(true);
        }
        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .05f);
            for (int i = 0; i < Capacity; i++)
            {
                if (!shells[i] || !shells[i].gameObject.activeSelf) continue;
                if (Time.time >= expiry[i]) { shells[i].gameObject.SetActive(false); continue; }
                if (velocity[i] == Vector3.zero || dt <= 0) continue;
                velocity[i] += Physics.gravity * dt;
                Vector3 motion = velocity[i] * dt;
                if (Physics.Raycast(shells[i].position, motion.normalized, out var hit, motion.magnitude + .005f, 1, QueryTriggerInteraction.Ignore))
                {
                    shells[i].position = hit.point + hit.normal * .004f;
                    Vector3 tangent = Vector3.Cross(hit.normal, Mathf.Abs(hit.normal.x) > .9f ? Vector3.forward : Vector3.right).normalized;
                    shells[i].rotation = Quaternion.FromToRotation(shells[i].up, tangent) * shells[i].rotation;
                    velocity[i] = Vector3.zero;
                }
                else { shells[i].position += motion; shells[i].Rotate(180 * dt, 340 * dt, 0); }
            }
        }
        public void Clear() { foreach (var shell in shells) if (shell) shell.gameObject.SetActive(false); }
        void OnDisable() => Clear();
    }
}
