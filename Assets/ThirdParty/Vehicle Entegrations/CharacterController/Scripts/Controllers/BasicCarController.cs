using MotionCore.Vehicle.Core;

namespace MotionCore.Vehicle.Controllers
{
    /// <summary>
    /// Free-core baseline controller. It uses the real WheelCollider-based driving
    /// model from <see cref="VehicleControllerBase"/> as-is: motor torque on the
    /// driven axle, brake torque, real steering, suspension, and tire slip.
    ///
    /// Tuning lives on the inspector fields of the base class. The paid Arcade and
    /// Simulation plugins subclass the same base and override its torque/steering/
    /// drift hooks to change the feel.
    /// </summary>
    public sealed class BasicCarController : VehicleControllerBase
    {
    }
}
