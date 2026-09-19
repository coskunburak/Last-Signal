using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    /// <summary>Live gameplay evidence, never a runtime dependency. Does not substitute sampled clips for gameplay.</summary>
    public static class RealAssetVisualQA
    {
        static int stage;
        static float started;
        static WeaponController weapon;
        static PlayerCombatController player;
        static Camera camera;
        static readonly StringBuilder log = new StringBuilder();
        public static void Begin()
        {
            EditorApplication.update -= Tick;
            player = UnityEngine.Object.FindAnyObjectByType<PlayerCombatController>();
            weapon = player.ActiveWeapon; camera = player.GetComponent<FirstPersonLook>().View;
            UnityEngine.Object.FindAnyObjectByType<SessionFlow>().Resume();
            stage = 0; started = Time.time; log.Clear();
            log.AppendLine("Live gameplay command sequence; scene=" + player.gameObject.scene.name);
            EditorApplication.update += Tick;
        }
        static void Capture(string name)
        {
            RealAssetIntegration.Render(camera, name);
            var anim = weapon.GetComponentInChildren<Animator>();
            var mag = weapon.GetComponentsInChildren<Transform>().First(t => t.name == "MRPoly_Magazine");
            log.AppendLine(name + ": state=" + weapon.RuntimeState.State + "; ammo=" + weapon.RuntimeState.CurrentMagazine + "; reserve=" + weapon.RuntimeState.ReserveAmmo + "; clip=" + string.Join(",", anim.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name)) + "; mag=" + mag.position.ToString("F4"));
        }
        static void Aim(bool held) => typeof(PlayerCombatController).GetMethod(held ? "OnAimPressed" : "OnAimReleased", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(player, null);
        static void Tick()
        {
            if (!EditorApplication.isPlaying || !weapon) { EditorApplication.update -= Tick; return; }
            try
            {
                var session = UnityEngine.Object.FindAnyObjectByType<SessionFlow>();
                if (session.Paused) session.Resume();
                float t = Time.time - started;
                if (stage == 0 && t > .15f) { Capture("01_idle"); Aim(true); stage++; }
                else if (stage == 1 && t > .55f) { Capture("02_aim_down_sights"); RealAssetIntegration.Render(camera,"02_ads_development", camera.pixelWidth, camera.pixelHeight); Aim(false); stage++; }
                else if (stage == 2 && t > .9f) { weapon.OnFirePressed(); weapon.OnFireReleased(); stage++; }
                else if (stage == 3) { Capture("03_fire"); stage++; }
                else if (stage == 4 && t > 1.2f) { weapon.OnReloadRequested(); started = Time.time; stage++; }
                else if (stage == 5 && t > 1.4f) { Capture("04_reload_mag_removed"); stage++; }
                else if (stage == 6 && t > 1.8f) { Capture("05_reload_mag_insert"); stage++; }
                else if (stage == 7 && t > 2.7f) { Capture("06_post_reload_ready"); Aim(true); stage++; }
                else if (stage == 8 && t > 3.1f)
                {
                    Capture("07_ads_after_reload"); Aim(false);
                    File.WriteAllText(RealAssetIntegration.Evidence + "/live-sequence.txt", log.ToString());
                    EditorApplication.update -= Tick;
                    Debug.Log("REAL_ASSET_LIVE_SEQUENCE finished; inspect screenshots for visual acceptance.");
                }
            }
            catch (Exception ex) { EditorApplication.update -= Tick; Debug.LogException(ex); }
        }
    }
}
