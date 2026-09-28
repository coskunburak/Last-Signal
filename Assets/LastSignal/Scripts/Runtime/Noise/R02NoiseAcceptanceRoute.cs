#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using LastSignal.AI;
using LastSignal.Persistence;
using LastSignal.WorldCells;
using UnityEngine;

namespace LastSignal.Noise
{
    /// <summary>Opt-in development acceptance, shared by PlayMode and standalone. No shipping AI listener.</summary>
    public static class R02NoiseAcceptanceRoute
    {
        static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException("R02: " + message); }
        static IEnumerator ReadyWeapon(WeaponController weapon)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while ((weapon.RuntimeState.State != WeaponState.Ready || weapon.RuntimeState.FireCooldownRemaining > 0) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(weapon.RuntimeState.State == WeaponState.Ready && weapon.RuntimeState.FireCooldownRemaining <= 0, "weapon ready timeout");
        }
        public static IEnumerator Run(SessionFlow flow, string directory, Action<string> log, bool screenshots = false)
        {
            Directory.CreateDirectory(directory); flow.Resume();
            var cells = flow.GetComponent<WorldCellManager>(); var pop = flow.GetComponent<WorldPopulationManager>();
            pop.MaterializationEnabled = false;
            yield return WorldCellAcceptanceRoute.Ready(cells, "cell:1:0"); Check(cells.TryEnter("cell:1:0"), "enter cell");
            // Remove only the acceptance threat so the deterministic route is not interrupted by combat AI.
            var encounter = cells.Content("cell:1:0").encounter;
            if (encounter && encounter.Actor) encounter.Actor.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo { Amount = 1000 });
            yield return null; yield return null;
            var player = flow.Player; var motor = player.GetComponent<FirstPersonMotor>(); var combat = player.GetComponent<PlayerCombatController>();
            var stamina = player.GetComponent<PlayerStamina>(); var input = player.GetComponent<PlayerInputReader>();
            player.transform.rotation = Quaternion.identity;
            var authority = flow.Noise;
            Check(authority != null && combat.Melee, "production composition");
            var probeObject = new GameObject("R02 near candidate probe"); var farObject = new GameObject("R02 far candidate probe");
            probeObject.transform.position = player.transform.position;
            farObject.transform.position = player.transform.position + Vector3.right * 1000;
            var near = probeObject.AddComponent<NoiseAcceptanceProbe>(); near.Bind(authority, 100);
            var far = farObject.AddComponent<NoiseAcceptanceProbe>(); far.Bind(authority, 101);
            GameObject target = null;
            float originalVolume = AudioListener.volume;
            try
            {
                ulong before = authority.AcceptedCount;
                for (int i = 0; i < 60; i++) motor.Simulate(Vector2.zero, false, 1f / 60);
                Check(authority.AcceptedCount == before, "standing emitted footsteps"); log("Standing: 0 events");
                log($"Walk start: position={player.transform.position} grounded={motor.Grounded} allowed={authority.CanEmit} input={input.GameplayActive}");
                for (int i = 0; i < 120; i++) motor.Simulate(Vector2.up, false, 1f / 60);
                log($"Walk end: position={player.transform.position} grounded={motor.Grounded} allowed={authority.CanEmit} count={authority.AcceptedCount}");
                Check(authority.AcceptedCount > before && authority.LastTrace.Event.Category == GameplayNoiseCategory.Footstep, "walking did not emit");
                log(authority.LastTrace.ToString()); before = authority.AcceptedCount;
                stamina.ResetSession();
                for (int i = 0; i < 120; i++) motor.Simulate(Vector2.down, true, 1f / 60);
                Check(authority.AcceptedCount > before && authority.LastTrace.Event.Category == GameplayNoiseCategory.SprintFootstep, "sprint did not emit");
                Check(pop.LastNoiseSequence == 0 && pop.MigrationCount == 0, "footsteps changed regional pressure"); log(authority.LastTrace.ToString());
                before = authority.AcceptedCount;
                for (int i = 0; i < 60; i++) motor.Simulate(Vector2.zero, false, 1f / 60);
                Check(authority.AcceptedCount == before, "stopping emitted");
                flow.Pause(); motor.Simulate(Vector2.up, true, .1f); Check(authority.AcceptedCount == before, "paused movement emitted");
                flow.Resume(); yield return null; yield return null;
                probeObject.transform.position = player.transform.position; authority.UpdatePosition(near, probeObject.transform.position);
                Check(combat.SelectSlot(PlayerCombatController.CombatSlot.Melee), "crowbar selection");
                var melee = combat.Melee;
                stamina.ResetSession(); Check(melee.TryAttack(), "miss swing rejected"); melee.Simulation.Tick(1);
                Check(authority.AcceptedCount == before, "air swing emitted impact");
                stamina.Restore(0); Check(!melee.TryAttack(), "exhausted attack accepted"); Check(authority.AcceptedCount == before, "exhausted noise");
                stamina.ResetSession(); Check(melee.TryAttack(), "cancel setup"); melee.Cancel(); melee.Simulation.Tick(1); Check(authority.AcceptedCount == before, "cancel emitted");
                var eye = player.GetComponentInChildren<Camera>().transform;
                target = GameObject.CreatePrimitive(PrimitiveType.Cube); target.name = "R02 impact QA target";
                target.transform.position = eye.position + eye.forward * 1.1f; target.transform.localScale = Vector3.one * .4f;
                var hp = target.AddComponent<ZombieHealth>();
                target.GetComponent<Collider>().enabled = false;
                for (int i = 0; i < 5; i++)
                {
                    var region = new GameObject("QA body region"); region.transform.SetParent(target.transform, false);
                    var collider = region.AddComponent<BoxCollider>();
                    region.AddComponent<ZombieHitRegion>().Configure(hp, DamageRegion.Body, 1, collider);
                }
                Physics.SyncTransforms(); stamina.ResetSession(); Check(melee.TryAttack(), "impact setup"); melee.Simulation.Tick(1);
                Check(authority.AcceptedCount == before + 1 && authority.LastTrace.Event.Category == GameplayNoiseCategory.MeleeImpact, "impact must emit once: " + melee.LastMeleeResult);
                Check(hp.DamageTransactions == 1, "multi collider damage duplicate"); log(authority.LastTrace.ToString());
                Check(pop.LastNoiseSequence == 0, "melee pressure policy");
                target.SetActive(false); UnityEngine.Object.Destroy(target); target = null;
                Check(combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm), "rifle selection");
                var weapon = combat.Firearm;
                // Acceptance fixture ammunition uses existing Initialize; each actual shot still passes the real commit gate.
                weapon.Initialize(eye, player, 6); weapon.RequestEquip(); yield return ReadyWeapon(weapon);
                before = authority.AcceptedCount; int nearBefore = near.ReceivedCount;
                AudioListener.volume = 1; weapon.OnFireReleased(); weapon.OnFirePressed(); weapon.OnFireReleased();
                Check(authority.AcceptedCount == before + 1 && authority.LastTrace.Event.Category == GameplayNoiseCategory.Gunshot, "valid shot not exactly one");
                var full = authority.LastTrace.Event; Check(full.ActionId != 0, "shot correlation missing");
                Check(near.ReceivedCount == nearBefore + 1 && far.ReceivedCount == 0, "near/far distribution");
                Check(pop.LastNoiseSequence == 1 && authority.LastTrace.PressureForwarded, "pressure not exactly once");
                Check(!authority.TryReceive(full) && pop.LastNoiseSequence == 1, "duplicate pressure"); log(authority.LastTrace.ToString());
                yield return ReadyWeapon(weapon); AudioListener.volume = 0; before = authority.AcceptedCount;
                var mutedOrigin = weapon.Muzzle.position;
                weapon.OnFirePressed(); weapon.OnFireReleased(); var muted = authority.LastTrace.Event;
                Check(authority.AcceptedCount == before + 1 && muted.BaseRadiusMeters == full.BaseRadiusMeters && muted.Intensity == full.Intensity && muted.Category == full.Category && muted.SourceId == full.SourceId && muted.Position == mutedOrigin, "mute changed semantics");
                Check(pop.LastNoiseSequence == 2, "muted shot pressure"); log(authority.LastTrace.ToString()); AudioListener.volume = originalVolume;
                if (screenshots) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory, "noise-development.png")); yield return null; }
                yield return ReadyWeapon(weapon);
                before = authority.AcceptedCount;
                weapon.RuntimeState.ForceHolster(); weapon.OnFirePressed(); weapon.OnFireReleased(); Check(authority.AcceptedCount == before, "blocked weapon emitted");
                weapon.Initialize(eye, player, 0); weapon.RequestEquip(); yield return ReadyWeapon(weapon); weapon.OnFirePressed(); weapon.OnFireReleased(); Check(authority.AcceptedCount == before, "empty emitted");
                var inventory = player.GetComponent<LastSignal.Inventory.PlayerInventory>(); inventory.TryAdd(weapon.Definition.Ammunition, 10);
                weapon.OnReloadRequested(); Check(weapon.RuntimeState.State == WeaponState.Reloading, "reload setup"); weapon.OnFirePressed(); weapon.OnFireReleased(); Check(authority.AcceptedCount == before, "reload emitted");
                combat.SelectSlot(PlayerCombatController.CombatSlot.Melee); weapon.OnFirePressed(); Check(authority.AcceptedCount == before, "inactive slot emitted");
                flow.Pause(); Check(!melee.TryAttack(), "paused melee accepted"); Check(!authority.TryEmit(new GameplayNoiseRequest(1, player.transform.position, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _), "paused authority accepted");
                flow.Resume(); yield return null;
                log("Producer rejection gates: empty/holstered/reloading/inactive slot/pause/exhaustion/cancel/miss PASS");
                // One event exactly on a half-open 64 m cell boundary affects only its owning ledger.
                float pA = pop.GetPressure("cell:1:0").Pressure, pB = pop.GetPressure("cell:2:0").Pressure;
                Check(authority.TryEmit(new GameplayNoiseRequest(1, new Vector3(128, 0, 1), GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out var boundary), "boundary emit");
                float contribution = pop.GetPressure("cell:1:0").Pressure + pop.GetPressure("cell:2:0").Pressure - pA - pB;
                Check(Mathf.Abs(contribution - .2f) < .0001f, "boundary doubled pressure"); log(authority.LastTrace.ToString());
                var saves = flow.GetComponent<SaveSession>(); var savePath = Path.Combine(directory, "noise-save.json");
                flow.Pause(); Check(saves.Save(savePath).Success, "save: " + saves.LastResult.Message);
                long savedReceipt = pop.LastNoiseSequence; float savedPressure = pop.GetPressure("cell:2:0").Pressure;
                flow.ReturnToMenu(); Check(authority.ListenerCount == 0 && authority.RecentCount == 0 && !authority.Active, "menu retained local state");
                yield return null; yield return saves.Load(savePath); Check(saves.LastResult.Success, "load: " + saves.LastResult.Message);
                Check(flow.Noise.RecentCount == 0, "load replayed events");
                Check(near.ReceivedCount > 0 && !authority.Active && authority.ListenerCount == 0, "load retained old listener authority");
                Check(pop.LastNoiseSequence == savedReceipt && Mathf.Abs(pop.GetPressure("cell:2:0").Pressure - savedPressure) < .001f, "load replayed pressure");
                Check(!flow.Noise.TryReceive(boundary), "old epoch replay");
                near.Bind(flow.Noise, 100); flow.Resume(); yield return null; yield return null;
                Check(flow.Noise.TryEmit(new GameplayNoiseRequest(1, new Vector3(128, 0, 1), GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _), "post-load emission");
                Check(pop.LastNoiseSequence == savedReceipt + 1, "post-load receipt mapping"); log(flow.Noise.LastTrace.ToString());
                log("Save/load: transient events absent, pressure retained once, new epoch and new gunshot PASS");
                for (int cycle = 0; cycle < 6; cycle++)
                {
                    var previous = flow.Noise; flow.ReturnToMenu(); yield return null; flow.BeginSession(); yield return null; yield return null;
                    flow.Resume(); yield return null; yield return null;
                    int physicalListeners = flow.Noise.ListenerCount;
                    near.Bind(flow.Noise, 100);
                    var freshCombat = flow.Player.GetComponent<PlayerCombatController>();
                    freshCombat.Firearm.Initialize(flow.Player.GetComponentInChildren<Camera>().transform, flow.Player, 2);
                    freshCombat.Firearm.RequestEquip(); yield return ReadyWeapon(freshCombat.Firearm);
                    freshCombat.Firearm.OnFirePressed(); freshCombat.Firearm.OnFireReleased();
                    Check(flow.Noise.AcceptedCount == 1, "session shot callback multiplied");
                    Check(previous.ListenerCount == 0 && !previous.Active && flow.Noise.ListenerCount == physicalListeners + 1, "session listener leak");
                    var steps = new FootstepNoiseProducer(flow.Noise, flow.NoiseTuning, 1);
                    steps.Advance(Vector3.right * 1.6f, flow.Player.transform.position, true, false, false);
                    steps.Advance(Vector3.right * 1.6f, flow.Player.transform.position, true, true, false);
                    freshCombat.SelectSlot(PlayerCombatController.CombatSlot.Melee); flow.Player.GetComponent<PlayerStamina>().ResetSession();
                    Check(freshCombat.Melee.TryAttack(), "soak crowbar"); freshCombat.Melee.Simulation.Tick(1);
                    log($"Soak cycle={cycle + 1}: epoch={flow.Noise.Epoch}, listeners={flow.Noise.ListenerCount}, events={flow.Noise.AcceptedCount}, oldListeners={previous.ListenerCount}");
                }
                var terminalAuthority = flow.Noise;
                var terminalCombat = flow.Player.GetComponent<PlayerCombatController>();
                ulong terminalCount = terminalAuthority.AcceptedCount;
                flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo { Amount = 1000 });
                flow.Player.GetComponent<FirstPersonMotor>().Simulate(Vector2.up, true, .25f);
                Check(!terminalCombat.Melee.TryAttack(), "dead melee accepted");
                Check(!terminalAuthority.TryEmit(new GameplayNoiseRequest(1, Vector3.zero, GameplayNoiseCategory.Gunshot, flow.NoiseTuning.Gunshot), out _), "dead authority accepted");
                Check(terminalAuthority.AcceptedCount == terminalCount, "death emitted noise");
                flow.ReturnToMenu(); Check(!terminalAuthority.CanEmit && terminalAuthority.ListenerCount == 0, "terminal menu cleanup");
                log("Death/menu: no movement or combat noise; listeners cleared PASS");
                log("R02 ROUTE PASS");
            }
            finally { AudioListener.volume = originalVolume; if (target) UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(probeObject); UnityEngine.Object.Destroy(farObject); }
        }
    }
}
#endif
