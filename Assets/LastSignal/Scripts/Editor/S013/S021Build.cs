using System;

namespace LastSignal.Art.Editor
{
    // Explicit user-operated build entry; keeps the S013Cabin production scene and BuildReport path.
    public static class S021Build
    {
        public static void DevelopmentMac()
        {
            string output = Environment.GetEnvironmentVariable("LASTSIGNAL_S021_BUILD_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Use Tools/s021-verify.py development-build.");
            S013ProductionAuthoring.Build(output);
        }
    }
}
