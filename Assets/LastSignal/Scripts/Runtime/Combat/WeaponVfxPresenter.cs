using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Muzzle flash, casing ejection, and impact VFX.
    /// Uses object pooling for repeated effects during automatic fire.
    /// All VFX is presentation-only — no damage from VFX.
    /// </summary>
    public sealed class WeaponVfxPresenter : MonoBehaviour
    {
        [SerializeField] WeaponController weapon;

        [Header("Muzzle Flash")]
        [SerializeField] ParticleSystem muzzleFlash;

        [Header("Casing Ejection")]
        [SerializeField] Transform casingEjectionPoint;
        [SerializeField] GameObject casingPrefab;
        [SerializeField, Min(1)] int casingPoolSize = 10;

        [Header("Impact")]
        [SerializeField] GameObject impactPrefab;
        [SerializeField, Min(1)] int impactPoolSize = 15;
        [SerializeField, Min(0)] float impactLifetime = 3f;

        GameObject[] casingPool;
        int casingIndex;
        GameObject[] impactPool;
        int impactIndex;

        public void Configure(WeaponController ctrl) => weapon = ctrl;

        void Awake()
        {
            if (casingPrefab) InitPool(ref casingPool, casingPrefab, casingPoolSize);
            if (impactPrefab) InitPool(ref impactPool, impactPrefab, impactPoolSize);
        }

        void OnEnable()
        {
            if (weapon) weapon.ShotFired += OnShotFired;
        }

        void OnDisable()
        {
            if (weapon) weapon.ShotFired -= OnShotFired;
        }

        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            // Muzzle flash.
            if (muzzleFlash) muzzleFlash.Play();

            // Casing.
            if (casingPool != null && casingEjectionPoint)
                SpawnFromPool(casingPool, ref casingIndex, casingEjectionPoint.position, casingEjectionPoint.rotation);

            // Impact at hit point.
            if (result.Hit && impactPool != null)
            {
                Quaternion rotation = Quaternion.LookRotation(result.HitNormal);
                SpawnFromPool(impactPool, ref impactIndex, result.HitPoint, rotation);
            }
        }

        void InitPool(ref GameObject[] pool, GameObject prefab, int size)
        {
            pool = new GameObject[size];
            for (int i = 0; i < size; i++)
            {
                pool[i] = Instantiate(prefab, transform);
                pool[i].SetActive(false);
            }
        }

        void SpawnFromPool(GameObject[] pool, ref int index, Vector3 position, Quaternion rotation)
        {
            var obj = pool[index % pool.Length];
            index++;
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(false); // Reset.
            obj.SetActive(true);
        }

        void OnDestroy()
        {
            // Pool objects are children — destroyed with this GameObject.
        }
    }
}
