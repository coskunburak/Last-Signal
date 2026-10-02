using UnityEngine;

namespace LastSignal
{
    // One scene-local, bounded physics pool for pre-authored skinned body pieces.
    // The source meshes are baked only on a sever event; no runtime mesh cutting occurs.
    public sealed class ZombieDetachedPartPool : MonoBehaviour
    {
        const int Capacity = 24;
        const int RenderersPerPart = 3;
        const float Lifetime = 8;
        sealed class Slot
        {
            public GameObject root;
            public Rigidbody body;
            public BoxCollider collider;
            public Mesh[] meshes = new Mesh[RenderersPerPart];
            public MeshRenderer[] renderers = new MeshRenderer[RenderersPerPart];
            public float expires;
        }

        static ZombieDetachedPartPool instance;
        readonly Slot[] slots = new Slot[Capacity];
        int next;
        public static void HideAll()
        {
            if (!instance) return;
            foreach (var slot in instance.slots)
                if (slot != null) slot.root.SetActive(false);
        }

        public static void Spawn(SkinnedMeshRenderer[] source, Transform anchor, Vector3 colliderCenter,
            Vector3 colliderSize, Vector3 direction, float impulse)
        {
            if (!Application.isPlaying || source == null || source.Length == 0 || !anchor) return;
            if (!instance)
            {
                var root = new GameObject("Zombie Detached Parts Pool");
                instance = root.AddComponent<ZombieDetachedPartPool>();
            }
            instance.SpawnInternal(source, anchor, colliderCenter, colliderSize, direction, impulse);
        }

        void SpawnInternal(SkinnedMeshRenderer[] source, Transform anchor, Vector3 colliderCenter,
            Vector3 colliderSize, Vector3 direction, float impulse)
        {
            int index = next++ % Capacity;
            var slot = slots[index] ?? (slots[index] = CreateSlot(index));
            slot.root.SetActive(false);
            slot.root.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            int used = 0;
            for (int i = 0; i < source.Length && used < RenderersPerPart; i++)
            {
                var skin = source[i];
                if (!skin || !skin.enabled || !skin.sharedMesh) continue;
                var target = slot.renderers[used];
                var mesh = slot.meshes[used];
                mesh.Clear(); skin.BakeMesh(mesh);
                target.GetComponent<MeshFilter>().sharedMesh = mesh;
                target.sharedMaterials = skin.sharedMaterials;
                target.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                target.transform.localScale = skin.transform.lossyScale;
                target.gameObject.SetActive(true);
                used++;
            }
            for (int i = used; i < RenderersPerPart; i++) slot.renderers[i].gameObject.SetActive(false);
            if (used == 0) return;
            slot.collider.center = colliderCenter;
            slot.collider.size = new Vector3(Mathf.Max(.05f, colliderSize.x),
                Mathf.Max(.05f, colliderSize.y), Mathf.Max(.05f, colliderSize.z));
            slot.expires = Time.time + Lifetime;
            slot.root.SetActive(true);
            slot.body.linearVelocity = Vector3.zero;
            slot.body.angularVelocity = Vector3.zero;
            var launch = direction.sqrMagnitude > .0001f ? direction.normalized : anchor.forward;
            slot.body.AddForce((launch + Vector3.up * .35f).normalized * Mathf.Clamp(impulse, .1f, 4f), ForceMode.Impulse);
            slot.body.AddTorque(anchor.right * .35f, ForceMode.Impulse);
        }

        Slot CreateSlot(int index)
        {
            var slot = new Slot();
            slot.root = new GameObject("Detached Zombie Part " + index);
            slot.root.transform.SetParent(transform, false);
            slot.root.layer = 2; // Ignore Raycast; detached gore is never a damage or AI target.
            slot.body = slot.root.AddComponent<Rigidbody>();
            slot.body.mass = 2;
            slot.body.maxAngularVelocity = 8;
            slot.collider = slot.root.AddComponent<BoxCollider>();
            for (int i = 0; i < RenderersPerPart; i++)
            {
                var child = new GameObject("Body Mesh " + i);
                child.transform.SetParent(slot.root.transform, false);
                child.layer = 2;
                child.AddComponent<MeshFilter>();
                slot.renderers[i] = child.AddComponent<MeshRenderer>();
                slot.meshes[i] = new Mesh { name = "Pooled Detached Zombie Mesh" };
                slot.meshes[i].MarkDynamic();
                child.SetActive(false);
            }
            slot.root.SetActive(false);
            return slot;
        }

        void Update()
        {
            float now = Time.time;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.root.activeSelf && now >= slot.expires)
                {
                    slot.root.SetActive(false);
                    slot.body.linearVelocity = Vector3.zero;
                    slot.body.angularVelocity = Vector3.zero;
                }
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            foreach (var slot in slots)
                if (slot != null) foreach (var mesh in slot.meshes) if (mesh) Destroy(mesh);
        }
    }
}
