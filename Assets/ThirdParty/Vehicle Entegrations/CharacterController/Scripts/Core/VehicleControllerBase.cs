using UnityEngine;

namespace MotionCore.Vehicle.Core
{
    public enum DrivetrainLayout
    {
        RearWheelDrive,
        FrontWheelDrive,
        AllWheelDrive
    }

    /// <summary>
    /// One axle of the vehicle. Wheels are simulated with Unity <see cref="WheelCollider"/>s,
    /// so suspension, tire slip, motor torque, brake torque, and steering are real physics.
    /// Axles are expected to be ordered front-to-rear in the controller's axle array.
    /// </summary>
    [System.Serializable]
    public sealed class VehicleAxle
    {
        [Tooltip("Left WheelCollider for this axle.")]
        public WheelCollider leftWheel;
        [Tooltip("Right WheelCollider for this axle.")]
        public WheelCollider rightWheel;
        [Tooltip("Left visual wheel transform, posed from the collider each frame.")]
        public Transform leftVisual;
        [Tooltip("Right visual wheel transform, posed from the collider each frame.")]
        public Transform rightVisual;
        [Tooltip("Does this axle steer? (Usually the front axle.)")]
        public bool steering;
        [Tooltip("Does the handbrake lock and slide this axle? (Usually the rear axle.)")]
        public bool handbrake;
        [Tooltip("Anti-roll bar strength for this axle. Higher resists body roll; too high makes the ride harsh.")]
        public float antiRollForce = 5000f;

        [System.NonSerialized] public float cachedLeftForwardStiffness = 1f;
        [System.NonSerialized] public float cachedRightForwardStiffness = 1f;
        [System.NonSerialized] public float cachedLeftSideStiffness = 1f;
        [System.NonSerialized] public float cachedRightSideStiffness = 1f;
    }

    [RequireComponent(typeof(Rigidbody))]
    public abstract class VehicleControllerBase : MonoBehaviour
    {
        [Header("Body")]
        [Tooltip("Optional transform used as the rigidbody center of mass. Keep it low and central to resist tipping.")]
        [SerializeField] private Transform centerOfMass;
        [Tooltip("Rigidbody mass (kg), applied on Awake. ~1100-1500 for a car.")]
        [SerializeField] private float bodyMass = 1300f;

        [Header("Axles (front-to-rear)")]
        [Tooltip("Axles ordered front to rear. A standard car has a steering front axle and a handbrake rear axle.")]
        [SerializeField] private VehicleAxle[] axles = new VehicleAxle[0];

        [Header("Drivetrain")]
        [Tooltip("Which axle(s) receive motor torque. RWD drives the rear axle, FWD the front, AWD all.")]
        [SerializeField] private DrivetrainLayout drivetrain = DrivetrainLayout.RearWheelDrive;
        [Tooltip("Total drive torque (Nm) split across the driven wheels. Higher = quicker acceleration.")]
        [SerializeField] private float maxMotorTorque = 2000f;
        [Tooltip("Braking torque (Nm) applied to all wheels.")]
        [SerializeField] private float maxBrakeTorque = 3200f;
        [Tooltip("Fraction of motor torque used when reversing.")]
        [SerializeField, Range(0f, 1f)] private float reverseTorqueScale = 0.45f;
        [Tooltip("Brake torque on the handbrake axle while the handbrake is held.")]
        [SerializeField] private float handbrakeTorque = 4500f;
        [Tooltip("Light drag applied when there is no throttle or brake input.")]
        [SerializeField] private float idleBrakeTorque = 150f;
        [Tooltip("Soft forward speed cap (km/h). Motor torque cuts out above it.")]
        [SerializeField] private float maxSpeedKph = 160f;
        [Tooltip("Reverse speed cap (km/h).")]
        [SerializeField] private float maxReverseSpeedKph = 35f;

        [Header("Steering")]
        [Tooltip("Steer angle (degrees) when parked/crawling — near full lock.")]
        [SerializeField] private float steerAngleAtZeroSpeed = 32f;
        [Tooltip("Steer angle (degrees) at top speed — small, for high-speed stability.")]
        [SerializeField] private float steerAngleAtTopSpeed = 8f;
        [Tooltip("How fast the wheels turn toward the target angle (deg/sec).")]
        [SerializeField] private float steerInputRateDegPerSec = 100f;
        [Tooltip("How fast the wheels return toward center (deg/sec). Usually faster than turn-in.")]
        [SerializeField] private float steerReturnRateDegPerSec = 200f;

        [Header("Handbrake")]
        [Tooltip("Fraction of rear cornering grip kept while the handbrake is held. Lower = slides out more; higher = tighter, more controllable.")]
        [SerializeField, Range(0f, 1f)] private float handbrakeGripFactor = 0.6f;

        [Header("Stability")]
        [Tooltip("Speed-scaled downward force for high-speed grip.")]
        [SerializeField] private float downforce = 50f;

        private Rigidbody body;
        private float currentSteerAngle;
        private IVehicleSurfaceProvider surfaceProvider;

        protected Rigidbody Body => body;
        protected CarInput InputState { get; private set; }
        protected VehicleAxle[] Axles => axles;
        protected DrivetrainLayout Drivetrain => drivetrain;
        protected float CurrentSteerAngle => currentSteerAngle;
        protected float MaxMotorTorque => maxMotorTorque;

        /// <summary>Signed speed along the car's forward axis, in m/s.</summary>
        public float ForwardSpeed { get; private set; }
        public float SpeedKph => body != null ? body.linearVelocity.magnitude * 3.6f : 0f;
        public float NormalizedSpeed => Mathf.Clamp01(Mathf.Abs(ForwardSpeed) * 3.6f / Mathf.Max(maxSpeedKph, 1f));
        public bool IsGrounded { get; private set; }
        public int GroundedWheelCount { get; private set; }
        public bool IsDrifting { get; protected set; }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = bodyMass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (centerOfMass != null)
            {
                body.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
            }

            // Remember each wheel's authored friction so the handbrake and surface
            // system can scale it relative to the tuned baseline (and restore it).
            for (int i = 0; i < axles.Length; i++)
            {
                VehicleAxle axle = axles[i];
                if (axle.leftWheel != null)
                {
                    axle.cachedLeftForwardStiffness = axle.leftWheel.forwardFriction.stiffness;
                    axle.cachedLeftSideStiffness = axle.leftWheel.sidewaysFriction.stiffness;
                }

                if (axle.rightWheel != null)
                {
                    axle.cachedRightForwardStiffness = axle.rightWheel.forwardFriction.stiffness;
                    axle.cachedRightSideStiffness = axle.rightWheel.sidewaysFriction.stiffness;
                }
            }

            // Optional surface-aware grip provider (see IVehicleSurfaceProvider).
            surfaceProvider = GetComponent<IVehicleSurfaceProvider>();

            // More physics substeps keep WheelColliders stable at speed.
            WheelCollider reference = FindFirstWheel();
            if (reference != null)
            {
                reference.ConfigureVehicleSubsteps(5f, 12, 15);
            }
        }

        public void SetInput(CarInput nextInput)
        {
            InputState = nextInput;
        }

        /// <summary>
        /// Assign a surface-aware grip provider explicitly. If none is set, the
        /// controller auto-detects one on its own GameObject on Awake.
        /// </summary>
        public void SetSurfaceProvider(IVehicleSurfaceProvider provider)
        {
            surfaceProvider = provider;
        }

        protected virtual void FixedUpdate()
        {
            UpdateMotionState();
            ApplySteering();
            ApplyDrive();
            ApplyWheelGrip();
            ApplyAntiRoll();
            ApplyDownforce();
            UpdateDriftState();
        }

        protected virtual void Update()
        {
            UpdateWheelVisuals();
        }

        // --- Extension points for plugin controllers -------------------------

        /// <summary>Front-wheel steer angle in degrees for the given input and speed.</summary>
        protected virtual float EvaluateSteerAngle(float steerInput, float normalizedSpeed)
        {
            // Steer authority falls off as speed builds: near-full lock for parking,
            // only a few degrees near top speed. The sqrt easing keeps low-speed
            // maneuverability while making the car calm and stable at speed — this is
            // what makes speed actually matter to the steering.
            float t = Mathf.Sqrt(Mathf.Clamp01(normalizedSpeed));
            float maxAngle = Mathf.Lerp(steerAngleAtZeroSpeed, steerAngleAtTopSpeed, t);
            return steerInput * maxAngle;
        }

        /// <summary>Total drive torque (Nm) to distribute across the driven wheels.</summary>
        protected virtual float EvaluateMotorTorque(float throttle, float normalizedSpeed)
        {
            return throttle * maxMotorTorque;
        }

        /// <summary>Sideways slip magnitude above which the car is considered drifting.</summary>
        protected virtual float DriftSlipThreshold => 0.4f;

        // --- Internal physics ------------------------------------------------

        private void UpdateMotionState()
        {
            ForwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);

            int grounded = 0;
            for (int i = 0; i < axles.Length; i++)
            {
                VehicleAxle axle = axles[i];
                if (axle.leftWheel != null && axle.leftWheel.isGrounded)
                {
                    grounded++;
                }

                if (axle.rightWheel != null && axle.rightWheel.isGrounded)
                {
                    grounded++;
                }
            }

            GroundedWheelCount = grounded;
            IsGrounded = grounded > 0;
        }

        private void ApplySteering()
        {
            float targetAngle = EvaluateSteerAngle(InputState.Steer, NormalizedSpeed);

            // Rate-limit the wheels so they cannot snap to lock instantly. Digital
            // (keyboard) input otherwise feels arcadey; returning toward center is
            // allowed to be quicker than turning in, like a self-centering rack.
            bool returningToCenter = Mathf.Sign(targetAngle) != Mathf.Sign(currentSteerAngle)
                || Mathf.Abs(targetAngle) < Mathf.Abs(currentSteerAngle);
            float rate = returningToCenter ? steerReturnRateDegPerSec : steerInputRateDegPerSec;
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetAngle, rate * Time.fixedDeltaTime);

            for (int i = 0; i < axles.Length; i++)
            {
                VehicleAxle axle = axles[i];
                if (!axle.steering)
                {
                    continue;
                }

                if (axle.leftWheel != null)
                {
                    axle.leftWheel.steerAngle = currentSteerAngle;
                }

                if (axle.rightWheel != null)
                {
                    axle.rightWheel.steerAngle = currentSteerAngle;
                }
            }
        }

        private void ApplyDrive()
        {
            float throttle = InputState.Throttle;
            float brakeInput = InputState.Brake;
            bool handbrake = InputState.Handbrake;

            int drivenWheels = CountDrivenWheels();
            float drivePerWheel = 0f;
            float serviceBrake = 0f;

            bool movingForward = ForwardSpeed > 0.5f;
            bool overTopSpeed = SpeedKph >= maxSpeedKph;

            if (throttle > 0.05f && !overTopSpeed)
            {
                float total = EvaluateMotorTorque(throttle, NormalizedSpeed);
                drivePerWheel = drivenWheels > 0 ? total / drivenWheels : 0f;
            }

            if (brakeInput > 0.05f)
            {
                if (movingForward)
                {
                    serviceBrake = brakeInput * maxBrakeTorque;
                    drivePerWheel = 0f;
                }
                else if (Mathf.Abs(ForwardSpeed) * 3.6f < maxReverseSpeedKph)
                {
                    float reverseTotal = brakeInput * maxMotorTorque * reverseTorqueScale;
                    drivePerWheel = drivenWheels > 0 ? -reverseTotal / drivenWheels : 0f;
                    serviceBrake = 0f;
                }
                else
                {
                    serviceBrake = brakeInput * maxBrakeTorque;
                }
            }

            if (throttle <= 0.05f && brakeInput <= 0.05f)
            {
                serviceBrake = idleBrakeTorque;
            }

            for (int i = 0; i < axles.Length; i++)
            {
                VehicleAxle axle = axles[i];
                float axleDrive = IsAxleDriven(i) ? drivePerWheel : 0f;
                float axleBrake = serviceBrake;

                if (handbrake && axle.handbrake)
                {
                    axleBrake = Mathf.Max(axleBrake, handbrakeTorque);
                    axleDrive = 0f;
                }

                ApplyWheelTorque(axle.leftWheel, axleDrive, axleBrake);
                ApplyWheelTorque(axle.rightWheel, axleDrive, axleBrake);
            }
        }

        private static void ApplyWheelTorque(WheelCollider wheel, float motorTorque, float brakeTorque)
        {
            if (wheel == null)
            {
                return;
            }

            wheel.motorTorque = motorTorque;
            wheel.brakeTorque = brakeTorque;
        }

        private void ApplyWheelGrip()
        {
            bool handbrake = InputState.Handbrake;

            for (int i = 0; i < axles.Length; i++)
            {
                VehicleAxle axle = axles[i];
                float handbrakeFactor = handbrake && axle.handbrake ? handbrakeGripFactor : 1f;

                ApplyGripToWheel(axle.leftWheel, axle.cachedLeftForwardStiffness, axle.cachedLeftSideStiffness, handbrakeFactor);
                ApplyGripToWheel(axle.rightWheel, axle.cachedRightForwardStiffness, axle.cachedRightSideStiffness, handbrakeFactor);
            }
        }

        private void ApplyGripToWheel(WheelCollider wheel, float baseForward, float baseSideways, float handbrakeFactor)
        {
            if (wheel == null)
            {
                return;
            }

            float surfaceForward = 1f;
            float surfaceSideways = 1f;

            if (surfaceProvider != null && wheel.GetGroundHit(out WheelHit hit))
            {
                SurfaceProperties surface = surfaceProvider.GetSurface(wheel, hit);
                surfaceForward = surface.forwardGripMultiplier;
                surfaceSideways = surface.sidewaysGripMultiplier;
            }

            SetStiffness(wheel, baseForward * surfaceForward, baseSideways * surfaceSideways * handbrakeFactor);
        }

        private static void SetStiffness(WheelCollider wheel, float forwardStiffness, float sidewaysStiffness)
        {
            WheelFrictionCurve forward = wheel.forwardFriction;
            if (!Mathf.Approximately(forward.stiffness, forwardStiffness))
            {
                forward.stiffness = forwardStiffness;
                wheel.forwardFriction = forward;
            }

            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            if (!Mathf.Approximately(sideways.stiffness, sidewaysStiffness))
            {
                sideways.stiffness = sidewaysStiffness;
                wheel.sidewaysFriction = sideways;
            }
        }

        private bool IsAxleDriven(int index)
        {
            switch (drivetrain)
            {
                case DrivetrainLayout.AllWheelDrive:
                    return true;
                case DrivetrainLayout.FrontWheelDrive:
                    return index == 0;
                default:
                    return index == axles.Length - 1;
            }
        }

        private int CountDrivenWheels()
        {
            int count = 0;
            for (int i = 0; i < axles.Length; i++)
            {
                if (!IsAxleDriven(i))
                {
                    continue;
                }

                if (axles[i].leftWheel != null)
                {
                    count++;
                }

                if (axles[i].rightWheel != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void ApplyAntiRoll()
        {
            for (int i = 0; i < axles.Length; i++)
            {
                ApplyAxleAntiRoll(axles[i]);
            }
        }

        private void ApplyAxleAntiRoll(VehicleAxle axle)
        {
            if (axle.leftWheel == null || axle.rightWheel == null || axle.antiRollForce <= 0f)
            {
                return;
            }

            float travelLeft = 1f;
            float travelRight = 1f;

            bool groundedLeft = axle.leftWheel.GetGroundHit(out WheelHit hitLeft);
            bool groundedRight = axle.rightWheel.GetGroundHit(out WheelHit hitRight);

            if (groundedLeft)
            {
                travelLeft = (-axle.leftWheel.transform.InverseTransformPoint(hitLeft.point).y - axle.leftWheel.radius) / axle.leftWheel.suspensionDistance;
            }

            if (groundedRight)
            {
                travelRight = (-axle.rightWheel.transform.InverseTransformPoint(hitRight.point).y - axle.rightWheel.radius) / axle.rightWheel.suspensionDistance;
            }

            float antiRoll = (travelLeft - travelRight) * axle.antiRollForce;

            if (groundedLeft)
            {
                body.AddForceAtPosition(axle.leftWheel.transform.up * -antiRoll, axle.leftWheel.transform.position);
            }

            if (groundedRight)
            {
                body.AddForceAtPosition(axle.rightWheel.transform.up * antiRoll, axle.rightWheel.transform.position);
            }
        }

        private void ApplyDownforce()
        {
            if (downforce <= 0f || !IsGrounded)
            {
                return;
            }

            body.AddForce(-transform.up * (downforce * NormalizedSpeed), ForceMode.Acceleration);
        }

        private void UpdateDriftState()
        {
            float maxSideSlip = 0f;
            for (int i = 0; i < axles.Length; i++)
            {
                maxSideSlip = Mathf.Max(maxSideSlip, GetAxleSideSlip(axles[i]));
            }

            IsDrifting = IsGrounded && (InputState.Handbrake || maxSideSlip > DriftSlipThreshold);
        }

        private static float GetAxleSideSlip(VehicleAxle axle)
        {
            float slip = 0f;

            if (axle.leftWheel != null && axle.leftWheel.GetGroundHit(out WheelHit left))
            {
                slip = Mathf.Max(slip, Mathf.Abs(left.sidewaysSlip));
            }

            if (axle.rightWheel != null && axle.rightWheel.GetGroundHit(out WheelHit right))
            {
                slip = Mathf.Max(slip, Mathf.Abs(right.sidewaysSlip));
            }

            return slip;
        }

        private void UpdateWheelVisuals()
        {
            for (int i = 0; i < axles.Length; i++)
            {
                UpdateVisual(axles[i].leftWheel, axles[i].leftVisual);
                UpdateVisual(axles[i].rightWheel, axles[i].rightVisual);
            }
        }

        private static void UpdateVisual(WheelCollider wheel, Transform visual)
        {
            if (wheel == null || visual == null)
            {
                return;
            }

            wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
            visual.SetPositionAndRotation(position, rotation);
        }

        private WheelCollider FindFirstWheel()
        {
            for (int i = 0; i < axles.Length; i++)
            {
                if (axles[i].leftWheel != null)
                {
                    return axles[i].leftWheel;
                }

                if (axles[i].rightWheel != null)
                {
                    return axles[i].rightWheel;
                }
            }

            return null;
        }
    }
}
