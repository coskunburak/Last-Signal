#if UNITY_EDITOR
using NUnit.Framework;
namespace LastSignal.Tests
{
    [SetUpFixture]
    public sealed class S017ProfileIsolation
    {
        readonly ProfileSnapshot profile = new ProfileSnapshot();
        [OneTimeSetUp] public void Capture() => profile.Capture();
        [OneTimeTearDown] public void Restore() => profile.Restore();
    }
}
#endif
