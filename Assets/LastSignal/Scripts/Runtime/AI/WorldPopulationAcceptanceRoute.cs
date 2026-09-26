#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using LastSignal.WorldCells;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.AI
{
    public static class WorldPopulationAcceptanceRoute
    {
        public const string A = "cell:1:0";
        public const string B = "cell:2:0";

        public static IEnumerator Run(
            WorldClock clock,
            string path,
            Action<string> log)
        {
            var flow = clock.Flow;
            var saves = clock.GetComponent<SaveSession>();
            var pop = clock.GetComponent<WorldPopulationManager>();
            var cells = clock.GetComponent<WorldCellManager>();

            Check(pop && cells && flow.Player, "Authored population composition");

            flow.Resume();

            Check(pop.TotalAccounted == 30, "Initial L=30 P=0 D=0 M=0");
            log?.Invoke("Initial L=30 P=0 D=0 M=0 total=30");

            yield return WorldCellAcceptanceRoute.Ready(cells, A);
            Check(cells.TryEnter(A), "Cell Ready and entry");

            var quiet = pop.GetPressure(A).Pressure;

            Check(
                quiet == 0 && pop.MigrationCount == 0,
                "Quiet baseline");

            log?.Invoke(
                $"QUIET pressure={quiet}, migrations=0, " +
                $"local live={pop.GetLedger(A).Logical + pop.GetLedger(A).Physical}");

            var weapon =
                flow.Player.GetComponent<PlayerCombatController>().ActiveWeapon;

            Check(weapon, "Authored rifle");

            var inventory =
                flow.Player.GetComponent<LastSignal.Inventory.PlayerInventory>();
            var state = weapon.RuntimeState;

            Check(
                inventory && state != null,
                "Inventory and weapon initialized");

            Check(
                state.Definition.Ammunition,
                "Rifle ammunition definition");

            weapon.OnFireReleased();

            // Silahın normal kuşanılma işleminin tamamlanmasını bekle.
            float readyDeadline = Time.realtimeSinceStartup + 10f;

            while (state.State != WeaponState.Ready &&
                   Time.realtimeSinceStartup < readyDeadline)
            {
                yield return null;
            }

            Check(
                state.State == WeaponState.Ready,
                "Rifle finished equipping");

            if (state.CurrentMagazine < 2)
            {
                // Kabul rotası için gerekli yedek mühimmatı hazırla.
                if (state.ReserveAmmo < 2)
                {
                    inventory.TryAdd(
                        state.Definition.Ammunition,
                        2 - state.ReserveAmmo);
                }

                Check(
                    state.ReserveAmmo >= 2,
                    "Fixture supplied rifle ammunition");

                // Şarjör durumunu doğrudan değiştirmeden gerçek reload başlat.
                weapon.OnReloadRequested();

                Check(
                    state.State == WeaponState.Reloading,
                    "Real reload started");

                float reloadDeadline = Time.realtimeSinceStartup + 10f;

                while (state.State == WeaponState.Reloading &&
                       Time.realtimeSinceStartup < reloadDeadline)
                {
                    yield return null;
                }
            }

            Check(
                state.State == WeaponState.Ready &&
                state.CurrentMagazine >= 2,
                "Rifle ready with at least two rounds");

            // Kabul atışlarını giriş duvarına yönlendir.
            flow.Player.transform.rotation = Quaternion.Euler(0, 225, 0);

            float loudInitialPressure = pop.GetPressure(A).Pressure;
            int initialAmmo = state.CurrentMagazine + state.ReserveAmmo;
            long initialSequence = pop.LastNoiseSequence;
            log?.Invoke($"LOUD INITIAL: pressure={loudInitialPressure:F6}, threshold=0.3");

            float fireDeadline = Time.realtimeSinceStartup + 5f;
            try
            {
                while (pop.LastNoiseSequence < 2 &&
                    Time.realtimeSinceStartup < fireDeadline)
                {
                    long seqBefore = pop.LastNoiseSequence;
                    weapon.OnFirePressed();
                    yield return null;
                    weapon.OnFireReleased();
                    
                    if (pop.LastNoiseSequence > seqBefore)
                    {
                        log?.Invoke($"SHOT FIRED: sequence={pop.LastNoiseSequence}, contribution={pop.NoiseIntensityMultiplier:F6}, new pressure={pop.GetPressure(A).Pressure:F6}");
                    }
                }
            }
            finally
            {
                weapon.OnFireReleased();
            }

            int shotsFired = (int)(pop.LastNoiseSequence - initialSequence);
            int ammoConsumed = initialAmmo - (state.CurrentMagazine + state.ReserveAmmo);
            log?.Invoke($"LOUD FIRING COMPLETE: shotsFired={shotsFired}, ammoConsumed={ammoConsumed}");

            if (pop.LastNoiseSequence < 2 || pop.MigrationCount <= 0)
            {
                log?.Invoke(
                    $"DIAGNOSTICS: LastNoiseSequence={pop.LastNoiseSequence} " +
                    $"MigrationCount={pop.MigrationCount} " +
                    $"Pressure={pop.GetPressure(A).Pressure} " +
                    $"WeaponState={weapon.RuntimeState.State} " +
                    $"Mag={weapon.RuntimeState.CurrentMagazine} " +
                    $"Ammo={weapon.RuntimeState.ReserveAmmo}");
            }

            Check(
                pop.LastNoiseSequence >= 2 && pop.MigrationCount > 0,
                "Real rifle crosses .3 threshold; " +
                $"NoiseSequence={pop.LastNoiseSequence}; " +
                $"Migrations={pop.MigrationCount}; " +
                $"Pressure={pop.GetPressure(A).Pressure:F6}; " +
                $"CurrentCell={cells.CurrentCell}; " +
                $"WeaponState={state.State}; " +
                $"Magazine={state.CurrentMagazine}; " +
                $"Reserve={state.ReserveAmmo}; " +
                $"Cooldown={state.FireCooldownRemaining:F6}; " +
                $"Paused={flow.Paused}; " +
                $"Restoring={flow.Restoring}; " +
                $"PlayerDead={flow.PlayerDead}; " +
                $"TimeScale={Time.timeScale}; " +
                $"WorldSeconds={clock.Simulation.Seconds:F6}");

            weapon.OnFireReleased();
            flow.Pause();

            var travel = pop.GetSaveSnapshot().migrations[0];

            Check(
                travel.sourceCellId == B &&
                travel.targetCellId == A &&
                travel.arrivalTime > travel.departureTime,
                "Migration toward noise");

            Check(pop.TotalAccounted == 30, "Departure conservation");

            log?.Invoke(
                $"LOUD pressure={pop.GetPressure(A).Pressure:F6}; " +
                $"group={travel.groupId}; {B} -> {A}; " +
                $"departure={travel.departureTime:F6}; " +
                $"ETA={travel.arrivalTime:F6}; size={travel.size}");

            Check(
                saves.Save(path).Success,
                "Mid-travel save: " + saves.LastResult.Message);

            long receipt = pop.LastNoiseSequence;

            flow.ReturnToMenu();
            yield return null;
            yield return saves.Load(path);

            Check(
                saves.LastResult.Success,
                "Mid-travel load: " + saves.LastResult.Message);

            flow.Pause();

            Check(
                pop.TotalAccounted == 30 && pop.MigrationCount == 1,
                "Exactly one restored journey");

            var restored = pop.GetSaveSnapshot().migrations[0];

            Check(
                restored.groupId == travel.groupId &&
                restored.arrivalTime == travel.arrivalTime &&
                restored.departureTime == travel.departureTime,
                "ETA identity preserved");

            float beforeReplay = pop.GetPressure(A).Pressure;

            pop.ReportNoise("noise:" + receipt, A, 1);

            Check(
                pop.GetPressure(A).Pressure == beforeReplay,
                "No noise replay after load");

            int beforeArrival = pop.GetLedger(A).Logical;

            clock.Simulation.AdvanceUntil(
                travel.arrivalTime - .01,
                clock.Exposure);

            Check(pop.MigrationCount == 1, "No early arrival");

            clock.Simulation.AdvanceUntil(
                travel.arrivalTime,
                clock.Exposure);

            Check(
                pop.MigrationCount == 0 &&
                pop.GetLedger(A).Logical == beforeArrival + 3 &&
                pop.TotalAccounted == 30,
                "Arrival transfers once");

            log?.Invoke(
                $"ARRIVAL t={clock.Simulation.Seconds:F6}, " +
                $"L(A)={pop.GetLedger(A).Logical}, " +
                $"total={pop.TotalAccounted}");

            flow.Resume();
            flow.Player.transform.rotation = Quaternion.Euler(0, 225, 0);

            for (int i = 0; i < 300 && pop.PhysicalCount == 0; i++)
            {
                pop.TryMaterialize();
                yield return null;
            }

            Check(
                pop.PhysicalCount > 0,
                "Visibility-safe materialization occurred");

            log?.Invoke($"MATERIALIZATION: result=SUCCESS, physicalCount={pop.PhysicalCount}, risk={(pop.GetLedger(A).Physical > 0 ? "ThreatNearby" : "None")}");

            Check(
                pop.TotalAccounted == 30,
                "Materialization conservation");

            ZombieController victim = null;

            foreach (var actor in UnityEngine.Object.FindObjectsByType<ZombieController>(
                         FindObjectsSortMode.None))
            {
                if (actor.gameObject.activeInHierarchy &&
                    actor.transform.parent == null &&
                    actor != flow.GetComponent<ZombieEncounter>().Actor)
                {
                    victim = actor;
                    break;
                }
            }

            Check(victim, "Population actor exists");

            Check(
                victim.GetComponent<NavMeshAgent>().agentTypeID == -1372625422 &&
                victim.GetComponent<NavMeshAgent>().isOnNavMesh,
                "Shambler navigation");

            var health = victim.GetComponent<ZombieHealth>();
            int dead = pop.GetLedger(A).Dead;

            health.TakeDamage(new DamageInfo
            {
                Amount = 1000,
                Category = DamageCategory.Bullet
            });

            health.TakeDamage(new DamageInfo
            {
                Amount = 1000,
                Category = DamageCategory.Bullet
            });

            Check(
                pop.GetLedger(A).Dead == dead + 1 &&
                pop.TotalAccounted == 30,
                "Death exactly once");

            log?.Invoke(
                $"DEATH L={pop.GetLedger(A).Logical}, " +
                $"P={pop.GetLedger(A).Physical}, " +
                $"D={pop.GetLedger(A).Dead}, " +
                $"total={pop.TotalAccounted}");

            yield return WorldCellAcceptanceRoute.Ready(cells, B);
            Check(cells.TryEnter(B), "Enter B / unload A");

            Check(
                pop.GetLedger(A).Physical == 0 &&
                pop.TotalAccounted == 30,
                "Dematerialization conservation");

            yield return WorldCellAcceptanceRoute.Ready(cells, A);
            Check(cells.TryEnter(A), "Reload A");

            flow.Pause();

            Check(
                saves.Save(path).Success,
                "Post-death save: " + saves.LastResult.Message);

            flow.ReturnToMenu();
            yield return null;
            yield return saves.Load(path);

            Check(
                saves.LastResult.Success &&
                pop.GetLedger(A).Dead == dead + 1 &&
                pop.TotalAccounted == 30,
                "Death survives reload and save/load");

            log?.Invoke(
                "STANDALONE PASS: rifle, pressure, travel, arrival, " +
                "materialization, death, unload/reload, save/load and conservation.");
        }

        static void Check(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(
                    "S009 acceptance: " + message);
            }
        }
    }
}
#endif