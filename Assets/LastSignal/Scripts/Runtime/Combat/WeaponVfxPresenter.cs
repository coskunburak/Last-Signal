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
        [SerializeField] CombatFeedbackProfile feedback;
        BallisticFeedbackPool ballistics;
        CosmeticCasingPool casings;
        WeaponController subscribedWeapon;
        float[] casingExpiry, impactExpiry;
        public BallisticFeedbackPool Ballistics => ballistics;

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

        public void Configure(WeaponController ctrl)
        { Unsubscribe(); weapon = ctrl; if (isActiveAndEnabled) Subscribe(); }

        void Awake()
        {
            if (feedback && feedback.IsValid)
            {
                var root = new GameObject("Weapon world feedback");
                ballistics = root.AddComponent<BallisticFeedbackPool>();
                ballistics.Initialize(feedback, transform);
                if (!casingPrefab && feedback.casingMaterial)
                { casings = root.AddComponent<CosmeticCasingPool>(); casings.Initialize(feedback.casingMaterial); }
            }
            if (casingPrefab) InitPool(ref casingPool, casingPrefab, casingPoolSize);
            if (impactPrefab) InitPool(ref impactPool, impactPrefab, impactPoolSize);
            casingExpiry = casingPool == null ? null : new float[casingPool.Length];
            impactExpiry = impactPool == null ? null : new float[impactPool.Length];
        }

        public void BindPistolSockets(CharacterWeaponSockets sockets, Transform ejection)
        {
            casingEjectionPoint = ejection;
            if (muzzleFlash)
            {
                muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                muzzleFlash.transform.SetParent(sockets.Muzzle, false);
                muzzleFlash.transform.localPosition = Vector3.zero;
                muzzleFlash.transform.localRotation = Quaternion.identity;
            }
        }
        void OnEnable() => Subscribe();
        void Subscribe()
        {
            Unsubscribe(); subscribedWeapon = weapon;
            if (subscribedWeapon) subscribedWeapon.ShotFired += OnShotFired;
        }
        void Unsubscribe()
        {
            if (subscribedWeapon) subscribedWeapon.ShotFired -= OnShotFired;
            subscribedWeapon = null;
        }

        void OnDisable()
        {
            Unsubscribe();
            if (ballistics) ballistics.Clear();
            if (casings) casings.Clear();
            Clear(casingPool); Clear(impactPool);
            if (muzzleFlash) muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            if (ballistics) ballistics.Present(result);
            // Muzzle flash.
            if (muzzleFlash)
            {
                muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = muzzleFlash.main; main.startRotation = Random.Range(0, Mathf.PI * 2);
                muzzleFlash.Play(true);
            }

            if (casings && !feedback.reducedEffects && !CombatPresentationPreference.ReducedEffects) casings.Eject(casingEjectionPoint);
            // Casing.
            if (casingPool != null && casingEjectionPoint)
                SpawnFromPool(casingPool, casingExpiry, ref casingIndex, casingEjectionPoint.position, casingEjectionPoint.rotation);

            // Impact at hit point.
            if (result.Hit && !ImpactSurface.Organic(result.Surface) && !ballistics && impactPool != null)
            {
                Quaternion rotation = Quaternion.LookRotation(result.HitNormal);
                SpawnFromPool(impactPool, impactExpiry, ref impactIndex, result.HitPoint, rotation);
            }
        }

        void InitPool(ref GameObject[] pool, GameObject prefab, int size)
        {
            pool = new GameObject[Mathf.Clamp(size, 1, 64)];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = Instantiate(prefab);
                pool[i].SetActive(false);
            }
        }

        void SpawnFromPool(GameObject[] pool, float[] expiry, ref int index, Vector3 position, Quaternion rotation)
        {
            var obj = pool[index % pool.Length];
            expiry[index % pool.Length] = Time.time + Mathf.Max(.05f, impactLifetime);
            index = (index + 1) % pool.Length;
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(false); // Reset.
            obj.SetActive(true);
        }

        static void Clear(GameObject[] pool) { if (pool != null) foreach (var item in pool) if (item) item.SetActive(false); }
        static void DestroyPool(GameObject[] pool) { if (pool != null) foreach (var item in pool) if (item) Destroy(item); }
        void Update()
        {
            if (casings && (feedback.reducedEffects || CombatPresentationPreference.ReducedEffects)) casings.Clear();
            Expire(casingPool, casingExpiry); Expire(impactPool, impactExpiry);
        }
        static void Expire(GameObject[] pool, float[] expiry)
        { if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].activeSelf && Time.time >= expiry[i]) pool[i].SetActive(false); }

        void OnDestroy()
        {
            if (ballistics) Destroy(ballistics.gameObject);
            DestroyPool(casingPool); DestroyPool(impactPool);
        }
    }
}
