public class TranslateAutopilot
{
    public struct Orientation
    {
        public Vector3D Point;
        public Vector3D Forward;
        public Vector3D Up;
        public Vector3D Left;

        public Orientation(IMyCubeBlock reference,
                           Base6Directions.Direction shipUp = Base6Directions.Direction.Up,
                           Base6Directions.Direction shipForward = Base6Directions.Direction.Forward)
        {
            Point = reference.GetPosition();
            var forward3I = reference.Position + Base6Directions.GetIntVector(shipForward);
            Forward = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(forward3I) - Point);
            var up3I = reference.Position + Base6Directions.GetIntVector(shipUp);
            Up = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(up3I) - Point);
            var left3I = reference.Position + Base6Directions.GetIntVector(Base6Directions.GetLeft(shipUp, shipForward));
            Left = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(left3I) - Point);
        }
    }

    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private readonly PIDController forwardPID = new PIDController(1.0 / RunsPerSecond);
    private readonly PIDController upPID = new PIDController(1.0 / RunsPerSecond);
    private readonly PIDController leftPID = new PIDController(1.0 / RunsPerSecond);

    private const double ThrustKp = 10000.0;
    private const double ThrustKi = 100.0;
    private const double ThrustKd = 0.0;

    private Vector3D AutopilotTarget;
    private double AutopilotSpeed;
    private bool AutopilotEngaged;

    public TranslateAutopilot()
    {
        forwardPID.Kp = ThrustKp;
        forwardPID.Ki = ThrustKi;
        forwardPID.Kd = ThrustKd;

        upPID.Kp = ThrustKp;
        upPID.Ki = ThrustKi;
        upPID.Kd = ThrustKd;

        leftPID.Kp = ThrustKp;
        leftPID.Ki = ThrustKi;
        leftPID.Kd = ThrustKd;
    }

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Vector3D target, double speed,
                     double delay = 1.0)
    {
        if (!AutopilotEngaged)
        {
            AutopilotTarget = target;
            AutopilotSpeed = speed;
            AutopilotEngaged = true;
            eventDriver.Schedule(delay, Start);
        }
    }

    public void Start(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();
        gyroControl.EnableOverride(true); // So the user knows it's engaged
        shipControl.ThrustControl.Reset();
        velocimeter.Reset();
        forwardPID.Reset();
        upPID.Reset();
        leftPID.Reset();
        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (!AutopilotEngaged)
        {
            Reset(commons);
            return;
        }

        var shipControl = (ShipControlCommons)commons;

        var reference = commons.Me;
        var orientation = new Orientation(reference,
                                          shipUp: shipControl.ShipUp,
                                          shipForward: shipControl.ShipForward);

        var targetVector = AutopilotTarget - orientation.Point;
        var distance = targetVector.Length();

        // Take projection of target vector on each of our axes
        var forwardError = Vector3D.Dot(targetVector, orientation.Forward);
        var upError = Vector3D.Dot(targetVector, orientation.Up);
        var leftError = Vector3D.Dot(targetVector, orientation.Left);

        velocimeter.TakeSample(reference.GetPosition(), eventDriver.TimeSinceStart);
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            var forwardSpeed = Vector3D.Dot((Vector3D)velocity, orientation.Forward);
            var upSpeed = Vector3D.Dot((Vector3D)velocity, orientation.Up);
            var leftSpeed = Vector3D.Dot((Vector3D)velocity, orientation.Left);

            // Naive approach: independent control of each axis
            Thrust(shipControl, Base6Directions.Direction.Forward, forwardError, forwardSpeed, forwardPID);
            Thrust(shipControl, Base6Directions.Direction.Up, upError, upSpeed, upPID);
            Thrust(shipControl, Base6Directions.Direction.Left, leftError, leftSpeed, leftPID);
        }

        if (distance < AUTOPILOT_DISENGAGE_DISTANCE)
        {
            Reset(commons);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Run);
        }
    }

    private void Thrust(ShipControlCommons commons, Base6Directions.Direction direction,
                        double distance, double speed, PIDController pid)
    {
        //commons.Echo(string.Format("Distance: {0:F1} m", distance));

        var thrustControl = commons.ThrustControl;
        var flipped = Base6Directions.GetFlippedDirection(direction);

        var targetSpeed = Math.Min(Math.Abs(distance) / AUTOPILOT_TTT_BUFFER,
                                   AutopilotSpeed);
        targetSpeed = Math.Max(targetSpeed, AUTOPILOT_MIN_SPEED); // Avoid Zeno's paradox...
        targetSpeed *= Math.Sign(distance);
        //commons.Echo(string.Format("Target Speed: {0:F1} m/s", targetSpeed));
        //commons.Echo(string.Format("Speed: {0:F1} m/s", speed));

        var error = targetSpeed - speed;
        var force = pid.Compute(error);
        
        if (Math.Abs(distance) < 1.0)
        {
            // Good enough
            thrustControl.SetOverride(direction, 0.0f);
            thrustControl.SetOverride(flipped, 0.0f);
        }
        else if (force > 0.0)
        {
            thrustControl.SetOverride(direction, (float)force);
            thrustControl.SetOverride(flipped, 0.0f);
        }
        else
        {
            thrustControl.SetOverride(direction, 0.0f);
            thrustControl.SetOverride(flipped, (float)-force);
        }
    }

    public void Reset(ZACommons commons)
    {
        var shipControl = (ShipControlCommons)commons;
        shipControl.GyroControl.EnableOverride(false);
        shipControl.ThrustControl.Reset();
        AutopilotEngaged = false;
    }
}
