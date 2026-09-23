using System;
using System.Collections.Generic;

namespace LastSignal.WorldTime
{
    [Serializable] public sealed class WorldTimeSettings
    {
        public double worldSecondsPerRealSecond = 60;
        public double startingSeconds = 28800;
        public int weatherSeed = 7183;
        public double rainIntensity = 1;
        public double minimumWeatherSeconds = 2700, maximumWeatherSeconds = 7200, transitionSeconds = 120;
        public double wetnessPerSecond = 1.0 / 1800, dryingPerSecond = 1.0 / 3600;
        public double restHealthPerHour = 8, maximumSleepSeconds = 28800;
        public bool Valid => Finite(worldSecondsPerRealSecond) && worldSecondsPerRealSecond >= 0 && worldSecondsPerRealSecond <= 3600 &&
            Finite(rainIntensity) && rainIntensity >= 0 && rainIntensity <= 1 && ValidTime(startingSeconds) && weatherSeed != 0 && Finite(minimumWeatherSeconds) && minimumWeatherSeconds >= 60 &&
            Finite(maximumWeatherSeconds) && maximumWeatherSeconds >= minimumWeatherSeconds && maximumWeatherSeconds <= 604800 &&
            Finite(transitionSeconds) && transitionSeconds > 0 && transitionSeconds <= minimumWeatherSeconds &&
            Finite(wetnessPerSecond) && wetnessPerSecond >= 0 && wetnessPerSecond <= 1 &&
            Finite(dryingPerSecond) && dryingPerSecond >= 0 && dryingPerSecond <= 1 &&
            Finite(restHealthPerHour) && restHealthPerHour >= 0 && restHealthPerHour <= 100 &&
            Finite(maximumSleepSeconds) && maximumSleepSeconds > 0 && maximumSleepSeconds <= 604800;
        public WorldTimeSettings Copy() => (WorldTimeSettings)MemberwiseClone();
        public static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        public const double MaximumTime = 1e12;
        public static bool ValidTime(double n) => Finite(n) && n >= 0 && n <= MaximumTime;
    }
    [Serializable] public sealed class WorldTimeSnapshot
    {
        public WorldTimeSettings settings;
        public double seconds, weatherStarted, nextWeather, wetness, lastProcessed;
        public int weather; // 0 clear, 1 rain
        public uint randomState;
        public int lastSleepReason;
        public double lastSleepElapsed;
        public static WorldTimeSnapshot LegacyDefault() => new WorldSimulation(new WorldTimeSettings()).Capture();
        public bool Valid => settings != null && settings.Valid && WorldTimeSettings.ValidTime(seconds) &&
            WorldTimeSettings.ValidTime(weatherStarted) && weatherStarted <= seconds &&
            WorldTimeSettings.Finite(nextWeather) && nextWeather > seconds && nextWeather - weatherStarted >= settings.minimumWeatherSeconds - .001 &&
            nextWeather - weatherStarted <= settings.maximumWeatherSeconds + .001 && (weather == 0 || weather == 1) && randomState != 0 &&
            WorldTimeSettings.Finite(wetness) && wetness >= 0 && wetness <= 1 && lastProcessed == seconds &&
            Enum.IsDefined(typeof(AdvanceReason), lastSleepReason) && WorldTimeSettings.Finite(lastSleepElapsed) && lastSleepElapsed >= 0 && lastSleepElapsed <= settings.maximumSleepSeconds;
    }
    public enum AdvanceReason { Completed, ThreatNearby, Dead, Cancelled, BoundaryLimit }
    public readonly struct AdvanceResult
    {
        public readonly double Start, End;
        public readonly AdvanceReason Reason;
        public double ElapsedSeconds => End - Start;
        public AdvanceResult(double start, double end, AdvanceReason reason) { Start = start; End = end; Reason = reason; }
    }
    /// <summary>Absolute world seconds. NextBoundary must be strictly after now, or PositiveInfinity.
    /// ApplyElapsed is called once per interval; Inspect runs at the resulting timestamp, including the initial timestamp.
    /// Participants must not mutate registration or reenter advancement. They own their persisted processed timestamp.</summary>
    public interface IWorldTimeParticipant
    {
        double NextBoundary(double now);
        void ApplyElapsed(double from, double to);
        AdvanceReason Inspect(double now);
    }
    [Serializable] public sealed class ElapsedWorldState
    {
        public double lastProcessed;
        public double TakeElapsed(double now)
        {
            if (!WorldTimeSettings.ValidTime(now) || !WorldTimeSettings.ValidTime(lastProcessed) || now < lastProcessed)
                throw new ArgumentOutOfRangeException(nameof(now));
            double elapsed = now - lastProcessed; lastProcessed = now; return elapsed;
        }
    }
    public sealed class WorldSimulation
    {
        readonly List<IWorldTimeParticipant> participants = new List<IWorldTimeParticipant>();
        readonly WorldTimeSettings settings;
        uint random;
        bool advancing;
        double weatherStarted, nextWeather;
        public double Seconds { get; private set; }
        public double Wetness { get; private set; }
        public bool Raining { get; private set; }
        public double NextWeather => nextWeather;
        public double WeatherStarted => weatherStarted;
        public long Day => (long)(Seconds / 86400) + 1;
        public double TimeOfDay => Seconds % 86400;
        public double RecoveryMultiplier => 1 - .5 * Wetness;
        public bool Advancing => advancing;
        public double MaximumSleepSeconds => settings.maximumSleepSeconds;
        public double Scale => settings.worldSecondsPerRealSecond;
        public double RestHealthPerHour => settings.restHealthPerHour;
        public AdvanceReason LastSleepReason { get; private set; }
        public double LastSleepElapsed { get; private set; }
        public double RainPresentation => settings.rainIntensity * (Raining ? Math.Min(1, (Seconds - weatherStarted) / settings.transitionSeconds) : (weatherStarted == settings.startingSeconds ? 0 : Math.Max(0, 1 - (Seconds - weatherStarted) / settings.transitionSeconds)));
        public WorldSimulation(WorldTimeSettings configuration)
        {
            if (configuration == null || !configuration.Valid) throw new ArgumentException("Invalid world-time settings.");
            settings = configuration.Copy(); Seconds = weatherStarted = settings.startingSeconds;
            random = unchecked((uint)settings.weatherSeed); nextWeather = Seconds + Duration();
            // Initial clear state is already settled; no phantom rain on a new game.

        }
        public WorldSimulation(WorldTimeSnapshot state)
        {
            if (state == null || !state.Valid) throw new ArgumentException("Invalid world-time snapshot.");
            settings = state.settings.Copy(); Seconds = state.seconds; weatherStarted = state.weatherStarted; nextWeather = state.nextWeather;
            Wetness = state.wetness; Raining = state.weather == 1; random = state.randomState;
            LastSleepReason = (AdvanceReason)state.lastSleepReason; LastSleepElapsed = state.lastSleepElapsed;
        }
        double Duration()
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            return settings.minimumWeatherSeconds + (settings.maximumWeatherSeconds - settings.minimumWeatherSeconds) * (random / (double)uint.MaxValue);
        }
        public void Register(IWorldTimeParticipant participant)
        {
            if (advancing) throw new InvalidOperationException("Cannot change participants during advancement.");
            if (participant == null || participants.Contains(participant)) throw new ArgumentException("Invalid/duplicate participant.");
            participants.Add(participant);
        }
        public void Unregister(IWorldTimeParticipant participant)
        { if (advancing) throw new InvalidOperationException("Cannot change participants during advancement."); participants.Remove(participant); }
        public AdvanceResult TickRealSeconds(double realSeconds, bool paused, double exposure, double protection = 0)
        {
            if (!WorldTimeSettings.Finite(realSeconds) || realSeconds < 0) throw new ArgumentOutOfRangeException(nameof(realSeconds));
            return AdvanceUntil(Seconds + (paused ? 0 : realSeconds * settings.worldSecondsPerRealSecond), exposure, protection);
        }
        public AdvanceResult AdvanceUntil(double target, double exposure, double protection = 0, Action<double> recoverHealth = null)
        {
            if (advancing) throw new InvalidOperationException("Reentrant world advancement.");
            if (!WorldTimeSettings.ValidTime(target) || target < Seconds || !Unit(exposure) || !Unit(protection)) throw new ArgumentOutOfRangeException(nameof(target));
            double start = Seconds; advancing = true;
            try
            {
                for (int count = 0; ; count++)
                {
                    var reason = Inspect();
                    if (reason != AdvanceReason.Completed) return new AdvanceResult(start, Seconds, reason);
                    if (Seconds >= target) return new AdvanceResult(start, Seconds, AdvanceReason.Completed);
                    if (count >= 100000) return new AdvanceResult(start, Seconds, AdvanceReason.BoundaryLimit);
                    double next = Math.Min(target, nextWeather);
                    for (int i = 0; i < participants.Count; i++)
                    {
                        double boundary = participants[i].NextBoundary(Seconds);
                        if (double.IsNaN(boundary) || boundary <= Seconds) throw new InvalidOperationException("Participant must make forward progress.");
                        next = Math.Min(next, boundary);
                    }
                    double from = Seconds, dt = next - from;
                    double rate = Raining && settings.rainIntensity * exposure * (1 - protection) > 0 ? settings.wetnessPerSecond * settings.rainIntensity * exposure * (1 - protection) : -settings.dryingPerSecond;
                    double endWet = Math.Max(0, Math.Min(1, Wetness + rate * dt));
                    double changing = rate == 0 ? dt : Math.Min(dt, Math.Abs((endWet - Wetness) / rate));
                    double wetIntegral = (Wetness + endWet) * .5 * changing + endWet * (dt - changing);
                    Wetness = endWet; Seconds = next;
                    for (int i = 0; i < participants.Count; i++) participants[i].ApplyElapsed(from, next);
                    // Death/interrupt effects happen before recovery, so healing never revives a boundary death.
                    if (Inspect() == AdvanceReason.Completed) recoverHealth?.Invoke(settings.restHealthPerHour / 3600 * (dt - .5 * wetIntegral));
                    if (Seconds >= nextWeather) { Raining = !Raining; weatherStarted = Seconds; nextWeather = Seconds + Duration(); }
                }
            }
            finally { advancing = false; }
        }
        AdvanceReason Inspect()
        {
            var reason = AdvanceReason.Completed;
            for (int i = 0; i < participants.Count; i++)
            { var r = participants[i].Inspect(Seconds); if (r == AdvanceReason.Dead) return r; if (r != AdvanceReason.Completed) reason = r; }
            return reason;
        }
        static bool Unit(double n) => WorldTimeSettings.Finite(n) && n >= 0 && n <= 1;
        public void RecordSleep(AdvanceResult result) { LastSleepReason = result.Reason; LastSleepElapsed = result.ElapsedSeconds; }
        public WorldTimeSnapshot Capture()
        {
            if (advancing) throw new InvalidOperationException("Snapshot requires a settled world boundary.");
            return new WorldTimeSnapshot { settings = settings.Copy(), seconds = Seconds, weatherStarted = weatherStarted, nextWeather = nextWeather,
                randomState = random, weather = Raining ? 1 : 0, wetness = Wetness, lastProcessed = Seconds, lastSleepReason = (int)LastSleepReason, lastSleepElapsed = LastSleepElapsed };
        }
    }
}
