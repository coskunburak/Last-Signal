using LastSignal.Vehicles;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class VehiclePresentationMathTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(32, 32, 270)]
        [TestCase(-32, -32, -270)]
        [TestCase(0, 16, 67.5f)]
        [TestCase(64, 64, 270)]
        [TestCase(-64, -64, -270)]
        public void SteeringUsesActualFrontAnglesAndClampsRange(float left, float right, float expected)
        { Assert.AreEqual(expected, VehicleSteeringWheelPresenter.EvaluateAngle(left, right, 32, 270), .001f); }
        [Test] public void InvalidSteeringTelemetryCannotProduceInvalidRotation()
        {
            Assert.AreEqual(0, VehicleSteeringWheelPresenter.EvaluateAngle(float.NaN, 0, 32, 270));
            Assert.AreEqual(0, VehicleSteeringWheelPresenter.EvaluateAngle(20, 20, 0, 270));
        }
    }
}
