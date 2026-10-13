using System;
namespace LastSignal.Art.Editor
{
    public static class S022Build
    {
        public static void DevelopmentMac()
        {
            string output = Environment.GetEnvironmentVariable("LASTSIGNAL_S022_BUILD_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Use Tools/s022-verify.py development-build.");
            S013ProductionAuthoring.Build(output);
        }
    }
}
