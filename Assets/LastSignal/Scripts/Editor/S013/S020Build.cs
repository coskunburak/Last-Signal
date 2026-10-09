using System;
namespace LastSignal.Art.Editor
{
    // Explicit user-only batch entry; delegates to the established production builder.
    public static class S020Build
    {
        public static void DevelopmentMac()
        {
            string output = Environment.GetEnvironmentVariable("LASTSIGNAL_S020_BUILD_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Use Tools/s020-verify.py development-build.");
            S013ProductionAuthoring.Build(output);
        }
    }
}
