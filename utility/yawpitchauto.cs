public class YawPitchAutopilot
{
    private Vector3D AutopilotTarget;
    private double AutopilotSpeed;
    private Base6Directions.Direction AutopilotUp;
    private Base6Directions.Direction AutopilotForward;

    private bool AutopilotEngaged;

    // Ripped from my missile guidance script...

    public static Vector3D Zero3D = new Vector3D();
    public static Vector3D Forward3D = new Vector3D(0.0, 0.0, 1.0);

    public struct Orientation
    {
        public Vector3D Point;
        public Vector3D Forward;
        public Vector3D Up;

        public Orientation(IMyCubeBlock reference,
                           Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                           Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
        {
            Point = reference.GetPosition();
            var forward3I = reference.Position + Base6Directions.GetIntVector(shipForward);
            Forward = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(forward3I) - Point);
            var up3I = reference.Position + Base6Directions.GetIntVector(shipUp);
            Up = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(up3I) - Point);
        }
    }

    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private const double GyroMaxRadiansPerSecond = Math.PI; // Really pi*2, but something's odd...

    private const double GyroKp = 1.0; // Proportional constant
    private const double GyroKi = 0.0; // Integral constant
    private const double GyroKd = 0.0; // Derivative constant
    private readonly PIDController yawPID = new PIDController(1.0 / RunsPerSecond);
    private readonly PIDController pitchPID = new PIDController(1.0 / RunsPerSecond);

    // From my miner script...

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;

    public YawPitchAutopilot()
    {
        yawPID.Kp = GyroKp;
        yawPID.Ki = GyroKi;
        yawPID.Kd = GyroKd;

        pitchPID.Kp = GyroKp;
        pitchPID.Ki = GyroKi;
        pitchPID.Kd = GyroKd;

        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
    }

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Vector3D target, double speed,
                     Base6Directions.Direction autopilotUp = Base6Directions.Direction.Up,
                     Base6Directions.Direction autopilotForward = Base6Directions.Direction.Forward,
                     double delay = 1.0)
    {
        if (!AutopilotEngaged)
        {
            AutopilotTarget = target;
            AutopilotSpeed = Math.Max(speed, AUTOPILOT_MIN_SPEED);
            AutopilotUp = autopilotUp;
            AutopilotForward = autopilotForward;
            AutopilotEngaged = true;
            eventDriver.Schedule(delay, AutopilotStart);
        }
    }

    public void AutopilotStart(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Reset all flight systems
        shipControl.GyroControl.Reset();
        shipControl.GyroControl.EnableOverride(true);
        shipControl.ThrustControl.Reset();
        yawPID.Reset();
        pitchPID.Reset();
        thrustPID.Reset();
        velocimeter.Reset();

        eventDriver.Schedule(0, Run); // We real-time now
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        if (!AutopilotEngaged)
        {
            Reset(commons);
            return;
        }

        var reference = commons.Me;
        var orientation = new Orientation(reference,
                                          shipUp: AutopilotUp,
                                          shipForward: AutopilotForward);

        var targetVector = AutopilotTarget - orientation.Point;
        var distance = targetVector.Normalize();

        // Transform relative to our forward vector
        targetVector = Vector3D.Transform(targetVector, MatrixD.CreateLookAt(Zero3D, -orientation.Forward, orientation.Up));

        var yawVector = new Vector3D(targetVector.GetDim(0), 0, targetVector.GetDim(2));
        var pitchVector = new Vector3D(0, targetVector.GetDim(1), targetVector.GetDim(2));
        yawVector.Normalize();
        pitchVector.Normalize();

        var yawError = Math.Acos(Vector3D.Dot(yawVector, Forward3D)) * Math.Sign(targetVector.GetDim(0));
        var pitchError = -Math.Acos(Vector3D.Dot(pitchVector, Forward3D)) * Math.Sign(targetVector.GetDim(1));

        var gyroYaw = yawPID.Compute(yawError);
        var gyroPitch = pitchPID.Compute(pitchError);

        var gyroControl = shipControl.GyroControl;
        var thrustControl = shipControl.ThrustControl;

        if (Math.Abs(gyroYaw) + Math.Abs(gyroPitch) > GyroMaxRadiansPerSecond)
        {
            var adjust = GyroMaxRadiansPerSecond / (Math.Abs(gyroYaw) + Math.Abs(gyroPitch));
            gyroYaw *= adjust;
            gyroPitch *= adjust;
        }

        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)gyroYaw);
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)gyroPitch);

        // Velocity control
        velocimeter.TakeSample(reference.GetPosition(), eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            //var speed = Vector3D.Dot((Vector3D)velocity, orientation.Forward);
            var speed = ((Vector3D)velocity).Length();

            var targetSpeed = Math.Min(distance / AUTOPILOT_TTT_BUFFER,
                                       AutopilotSpeed);
            targetSpeed = Math.Max(targetSpeed, AUTOPILOT_MIN_SPEED); // Avoid Zeno's paradox...

            var error = targetSpeed - speed;

            var force = thrustPID.Compute(error);

            var backward = Base6Directions.GetFlippedDirection(AutopilotForward);

            if (force > 0.0)
            {
                // Thrust forward
                thrustControl.SetOverride(AutopilotForward, force);
                thrustControl.SetOverride(backward, false);
            }
            else
            {
                thrustControl.SetOverride(AutopilotForward, false);
                thrustControl.SetOverride(backward, -force);
            }
        }

        if (distance < AUTOPILOT_DISENGAGE_DISTANCE)
        {
            // All done
            Reset(commons);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Run);
        }
    }

    public void Reset(ZACommons commons)
    {
        var shipControl = (ShipControlCommons)commons;
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();
        gyroControl.EnableOverride(false);
        shipControl.ThrustControl.Reset();
        AutopilotEngaged = false;
    }
}
