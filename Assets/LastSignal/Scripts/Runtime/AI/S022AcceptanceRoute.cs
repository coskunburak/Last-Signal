using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace LastSignal
{
    // Dedicated validation scene only. It never edits a production scene or publishes a build.
    public sealed class S022AcceptanceRoute : MonoBehaviour
    {
        [SerializeField] ZombieController shambler, lurker, runner;
        [SerializeField] ZombieEncounterRisk riskProfile;
        SessionFlow flow;
        readonly List<ZombieController> actors = new List<ZombieController>(30);
        readonly List<GameObject> geometry = new List<GameObject>(4);
        uint encounterGeneration;
        int requested, spawned, preset;
        float nextSpawn;
        bool active;
        string status = "Select a route after starting the session.";
        public int ActiveActors { get { int count = 0; foreach (var actor in actors) if (actor && actor.isActiveAndEnabled && !actor.IsDead) count++; return count; } }
        public string Status => status;
        static readonly string[] Routes = { "1 Shambler", "2 Lurker", "3 Runner", "4 Interior Lurker", "5 Open Runner",
            "6 Mixed", "7 Doorway mixed", "8 Save/load resident", "10 mixed benchmark", "20 mixed benchmark", "30 mixed benchmark", "Pair Shambler", "Pair Lurker", "Pair Runner" };
        void Start() => flow = FindAnyObjectByType<SessionFlow>();
        public void Select(int index)
        {
            if (!flow || !flow.Player || index < 0 || index >= Routes.Length) return;
            Clear(); preset = index;
            var resident = flow.GetComponent<ZombieEncounter>(); encounterGeneration = resident.Generation;
            if (index == 7) { resident.SetPaused(false); status = "Use existing session save/load UI; resident identity is captured by SaveSession. Use WorldPopulationAcceptance for streamed mixed identities."; return; }
            resident.SetPaused(true);
            // Validation owns the alternate cast; hide the resident with its original encounter identity intact.
            if (resident.Actor) resident.Actor.gameObject.SetActive(false);
            active = true;
            requested = index >= 11 ? 2 : index >= 8 ? (index - 7) * 10 : index >= 5 ? 3 : 1;
            var capsule = flow.Player.GetComponent<CharacterController>();
            capsule.enabled = false; flow.Player.transform.SetPositionAndRotation(new Vector3(-8, 0, 6), Quaternion.Euler(0,180,0)); capsule.enabled = true;
            if (index == 3 || index == 6)
            {
                Wall(new Vector3(-10,1,2),new Vector3(2.5f,2,.3f));
                Wall(new Vector3(-6,1,2),new Vector3(2.5f,2,.3f));
                if (index == 3) Wall(new Vector3(-11.5f,1,0),new Vector3(.3f,2,4));
            }
            nextSpawn = Time.time + .5f;
            var context = new ZombieEncounterContext { visibility = index == 3 ? .35f : 1, escapeRoutes = index == 3 || index == 6 ? 1 : 2,
                interior = index == 3, ambush = index == 3, constrainedTraversal = index == 6, exposure = .5f, approachDistance = 6, recoverySpacing = 2 };
            int forcedKind = index >= 11 ? index - 11 : index == 0 ? 0 : index == 1 || index == 3 ? 1 : index == 2 || index == 4 ? 2 : -1;
            int s = forcedKind == 0 ? requested : forcedKind < 0 ? (requested + 2) / 3 : 0;
            int l = forcedKind == 1 ? requested : forcedKind < 0 ? (requested + 1) / 3 : 0;
            int r = requested - s - l;
            float risk = riskProfile ? riskProfile.Score(s,l,r,context) : 0;
            status = Routes[index] + $" S={s} L={l} R={r} risk={risk:F1}; authored validation override; one spawn / 0.1s; " +
                (index == 3 || index == 6 ? "one doorway escape" : "open retreat");
        }
        void Wall(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); geometry.Add(wall);
            wall.name = "S022 temporary doorway"; wall.transform.position = position; wall.transform.localScale = scale;
            var obstacle = wall.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box; obstacle.size = Vector3.one; obstacle.carving = true;
        }
        void Update()
        {
            if (!active) return;
            if (!flow || !flow.Player) { Clear(); status = "Session ended: validation actors released."; return; }
            if (flow.GetComponent<ZombieEncounter>().Generation != encounterGeneration) { Clear(); status = "Session changed: validation actors released."; return; }
            flow.GetComponent<ZombieEncounter>().SetPaused(true);
            foreach (var actor in actors) if (actor && actor.Paused != (flow.Paused || flow.PlayerDead)) actor.SetPaused(flow.Paused || flow.PlayerDead);
            if (flow.Paused || flow.PlayerDead || spawned >= requested || Time.time < nextSpawn) return;
            nextSpawn = Time.time + .1f;
            int kind = preset >= 11 ? preset - 11 : preset == 0 ? 0 : preset == 1 || preset == 3 ? 1 : preset == 2 || preset == 4 ? 2 : spawned % 3;
            var prefab = kind == 1 ? lurker : kind == 2 ? runner : shambler;
            if (!prefab) { status = "BLOCKED: missing prefab"; requested = spawned; return; }
            Vector3 position = new Vector3(-8 + (spawned % 5 - (requested > 3 ? 2 : 0)) * 1.5f, 0, -(spawned / 5) * 1.5f);
            var sourceAgent = prefab.GetComponent<NavMeshAgent>();
            if (!NavMesh.SamplePosition(position, out var hit, .35f, new NavMeshQueryFilter { agentTypeID = sourceAgent.agentTypeID, areaMask = sourceAgent.areaMask }))
            { status = "BLOCKED: route grid outside baked navigation at " + position; requested = spawned; return; }
            var spawnedActor = Instantiate(prefab,hit.position,Quaternion.identity); actors.Add(spawnedActor); spawned++;
            if (!spawnedActor.Initialize() || !spawnedActor.Bind(flow.Player)) { status = "BLOCKED: actor initialization"; requested = spawned; }
        }
        void Clear()
        {
            active = false; requested = spawned = 0;
            foreach (var actor in actors) if (actor) { actor.Shutdown(); actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
            actors.Clear(); foreach (var wall in geometry) if (wall) { wall.SetActive(false); Destroy(wall); } geometry.Clear();
            if (flow)
            {
                var resident = flow.GetComponent<ZombieEncounter>();
                if (flow.Player && resident && resident.Actor && !resident.Actor.gameObject.activeSelf)
                    resident.Begin(flow.Player);
                resident?.SetPaused(flow.Paused);
            }
        }
        void OnDisable() => Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, Screen.height - 280, 710, 270), GUI.skin.box);
            GUILayout.Label("S022 — user acceptance / profiler capture (NOT_RUN until recorded)");
            for (int row = 0; row < 4; row++) { GUILayout.BeginHorizontal();
                for (int col = 0; col < 4; col++) { int i = row * 4 + col; if (i < Routes.Length && GUILayout.Button(Routes[i])) Select(i); }
                GUILayout.EndHorizontal(); }
            GUILayout.Label(status + " | active=" + ActiveActors);
            GUILayout.EndArea();
        }
#endif
    }
}
