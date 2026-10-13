using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif
namespace LastSignal
{
    /// <summary>Manual-only inspection controls. Never runs tests or drives the player automatically.</summary>
    public sealed class CombatFeedbackManualRoute : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        SessionFlow flow;
        ZombieController[] actors;
        Light sun, inspectionLight;
        GameObject inspectionRoof;
        float daylight;
        Color ambient;
        int countIndex;
        bool night, previewCuts, wasPaused;
        readonly int[] counts = { 1, 10, 20, 30 };
        void Start()
        {
            flow = GetComponent<SessionFlow>(); ambient = RenderSettings.ambientLight;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { sun = light; daylight = light.intensity; break; }
        }
        void Update()
        {
            if (!flow || !flow.Player) { if (actors != null) Clear(); return; }
            if (wasPaused != flow.Paused)
            { wasPaused = flow.Paused; if (actors != null) foreach (var actor in actors) if (actor) actor.SetPaused(wasPaused); }
            if (flow.Paused) return;
            var keys = Keyboard.current; if (keys == null) return;
            if (keys.f4Key.wasPressedThisFrame)
            {
                if (!inspectionRoof)
                {
                    inspectionRoof = GameObject.CreatePrimitive(PrimitiveType.Cube); inspectionRoof.name = "Manual inspection canopy";
                    inspectionRoof.transform.position = new Vector3(-6, 3, 3); inspectionRoof.transform.localScale = new Vector3(7, .2f, 6);
                }
                else inspectionRoof.SetActive(!inspectionRoof.activeSelf);
            }
            if (keys.f5Key.wasPressedThisFrame)
            {
                if (!inspectionLight)
                {
                    var camera = flow.Player.GetComponentInChildren<Camera>();
                    var root = new GameObject("Manual inspection flashlight"); root.transform.SetParent(camera.transform, false);
                    inspectionLight = root.AddComponent<Light>(); inspectionLight.type = LightType.Spot;
                    inspectionLight.range = 18; inspectionLight.spotAngle = 45; inspectionLight.intensity = 3;
                }
                else inspectionLight.enabled = !inspectionLight.enabled;
            }
            if (keys.f6Key.wasPressedThisFrame) { Spawn(counts[countIndex]); countIndex = (countIndex + 1) % counts.Length; }
            if (keys.f7Key.wasPressedThisFrame) ZombieGorePreference.SetGraphicGoreEnabled(!ZombieGorePreference.GraphicGoreEnabled);
            if (keys.f8Key.wasPressedThisFrame)
            { night = !night; if (sun) sun.intensity = night ? .02f : daylight; RenderSettings.ambientLight = night ? new Color(.015f,.018f,.025f) : ambient; }
            if (keys.f9Key.wasPressedThisFrame) CombatPresentationPreference.SetReducedEffects(!CombatPresentationPreference.ReducedEffects);
            if (keys.f10Key.wasPressedThisFrame) { previewCuts = !previewCuts; Spawn(1); }
            if (keys.f11Key.wasPressedThisFrame)
            {
                var weapon = flow.Player.GetComponent<PlayerCombatController>().Firearm;
                if (weapon) flow.Player.GetComponent<Inventory.PlayerInventory>().TryAdd(weapon.Definition.Ammunition, 120);
            }
        }
        void Spawn(int count)
        {
            Clear(); flow.GetComponent<ZombieEncounter>()?.End();
            actors = R04AcceptanceRoute.SpawnZombies(flow, count, new Vector3(-10, 0, -7), 1.65f);
            foreach (var actor in actors) if (actor && previewCuts) actor.GetComponent<ZombieDismemberment>().PreviewUnapprovedCuts();
        }
        void Clear()
        {
            if (actors != null) foreach (var actor in actors) if (actor) { actor.Shutdown(); Destroy(actor.gameObject); }
            actors = null; ZombieBloodVfxPool.ResetSession();
        }
        void OnGUI()
        {
            if (!flow || !flow.Player) return;
            GUI.Box(new Rect(12, Screen.height - 112, 850, 100),
                "MANUAL COMBAT INSPECTION — no automated acceptance\nF4: inspection canopy · F5: inspection flashlight\nF6: replace with 1/10/20/30 zombies · F7: gore · F8: day/night · F9: reduced effects\nF10: preview unapproved cuts (" + (previewCuts ? "ON — ART NOT ACCEPTED" : "OFF") + ") · F11: add reserve ammo, then R to reload");
        }
        void OnDisable() { Clear(); if (inspectionRoof) Destroy(inspectionRoof); if (inspectionLight) Destroy(inspectionLight.gameObject); if (sun) sun.intensity = daylight; RenderSettings.ambientLight = ambient; }
#endif
    }
}
