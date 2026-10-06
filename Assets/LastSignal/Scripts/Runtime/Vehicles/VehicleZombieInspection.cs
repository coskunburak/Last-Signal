#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Vehicles
{
    /// <summary>Development-only production-prefab fixtures on the existing resident NavMesh.</summary>
    public sealed class VehicleZombieInspection : MonoBehaviour
    {
        bool showTelemetry = true;
        SessionFlow flow;
        VehicleActor car;
        public ZombieController[] Actors { get; private set; }
        public int ActiveCount
        {
            get { int count = 0; if (Actors != null) foreach (var a in Actors) if (a && a.isActiveAndEnabled && !a.IsDead && a.CanHear) count++; return count; }
        }
        public static VehicleZombieInspection Create(SessionFlow owner)
        {
            var fixture = new GameObject("VEH-ZMB-001 development fixtures").AddComponent<VehicleZombieInspection>();
            fixture.showTelemetry = false;
            fixture.flow = owner; fixture.car = owner.GetComponent<VehicleWorld>().Actor;
            fixture.Spawn(); return fixture;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-veh001ZombieInspect") >= 0)
                new GameObject("VEH-ZMB-001 inspection bootstrap").AddComponent<VehicleZombieInspection>();
        }
        void Start()
        {
            if (flow) return;
            flow = FindAnyObjectByType<SessionFlow>();
            if (!flow || !flow.Player) throw new InvalidOperationException("Production session required.");
            flow.Resume(); car = flow.GetComponent<VehicleWorld>().Actor; Spawn();
        }
        void Spawn()
        {
            var prefab = Resources.Load<GameObject>("LS_Zombie_Runtime");
            if (!prefab || !car) throw new InvalidOperationException("Production zombie/pickup missing.");
            var agent = prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            var offsets = new[] { new Vector3(0,0,10), new Vector3(0,0,24), new Vector3(0,0,44), new Vector3(1.1f,0,62), new Vector3(7,0,28) };
            Actors = new ZombieController[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                var desired = car.transform.TransformPoint(offsets[i]);
                if (!NavMesh.SamplePosition(desired, out var hit, 2, filter))
                    throw new InvalidOperationException("Resident inspection NavMesh missing at " + desired);
                var go = Instantiate(prefab, hit.position, Quaternion.LookRotation(-car.transform.forward));
                go.name = "Zombie_" + (char)('A' + i) + "_DevelopmentFixture";
                Actors[i] = go.GetComponent<ZombieController>();
                if (!Actors[i].Initialize() || !Actors[i].Bind(flow.Player))
                    throw new InvalidOperationException("Inspection AI binding failed: " + go.name);
            }
        }
        void Update()
        {
            if (Actors == null) return;
            if (!flow || !flow.Player || !car) { Destroy(gameObject); return; }
            foreach (var actor in Actors) if (actor) actor.SetPaused(flow.Paused || flow.Restoring || flow.InMenu);
        }
        void OnDestroy()
        { if (Actors != null) foreach (var actor in Actors) if (actor) { actor.Shutdown(); Destroy(actor.gameObject); } }
        void OnGUI()
        {
            if (!showTelemetry || !car) return;
            var hit = car.LastZombieImpact;
            GUI.Box(new Rect(20, 110, 690, 85), $"VEH-ZMB DEVELOPMENT · active AI {ActiveCount} · A low / B medium / C high / D glance / E witness\n" +
                $"Impacts {car.ZombieImpactCount} · target {hit.Target} · transaction {hit.Transaction} · closing {hit.ClosingSpeed:F2} m/s\n" +
                $"Damage {hit.Damage:F1} · cost {hit.ConditionCost:P2} · contacts {car.ImpactContactCount} · suppressed {car.ImpactSuppressionCount}\n" +
                "Development fixtures are transient; persistent encounter uses the normal save system.");
        }
    }
}
#endif
