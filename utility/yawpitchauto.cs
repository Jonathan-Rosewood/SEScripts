public class YawPitchAutopilot
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;

    private Vector3D AutopilotTarget;
    private double AutopilotSpeed;
    private bool AutopilotEngaged;

    public YawPitchAutopilot()
    {
        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
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

        shipControl.Reset(gyroOverride: true, thrusterEnable: null);

        seeker.Init(shipControl,
                    localUp: shipControl.ShipUp,
                    localForward: shipControl.ShipForward);

        velocimeter.Reset();
        thrustPID.Reset();

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

        var targetVector = AutopilotTarget - shipControl.ReferencePoint;
        var distance = targetVector.Normalize();

        double yawError, pitchError;
        var gyroControl = seeker.Seek(shipControl, targetVector,
                                      out yawError, out pitchError);

        // Velocity control
        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            var speed = ((Vector3D)velocity).Length();

            var targetSpeed = Math.Min(distance / AUTOPILOT_TTT_BUFFER,
                                       AutopilotSpeed);
            targetSpeed = Math.Max(targetSpeed, AUTOPILOT_MIN_SPEED); // Avoid Zeno's paradox...

            var error = targetSpeed - speed;
            var force = thrustPID.Compute(error);

            var thrustControl = shipControl.ThrustControl;
            if (Math.Abs(error) < 0.02 * targetSpeed)
            {
                // Close enough, just disable both sets of thrusters
                thrustControl.Enable(Base6Directions.Direction.Forward, false);
                thrustControl.Enable(Base6Directions.Direction.Backward, false);
            }
            else if (force > 0.0)
            {
                // Thrust forward
                thrustControl.Enable(Base6Directions.Direction.Forward, true);
                thrustControl.SetOverride(Base6Directions.Direction.Forward, force);
                thrustControl.Enable(Base6Directions.Direction.Backward, false);
            }
            else
            {
                thrustControl.Enable(Base6Directions.Direction.Forward, false);
                thrustControl.Enable(Base6Directions.Direction.Backward, true);
                thrustControl.SetOverride(Base6Directions.Direction.Backward, -force);
            }
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

    public void Reset(ZACommons commons)
    {
        var shipControl = (ShipControlCommons)commons;
        shipControl.Reset(gyroOverride: false);
        AutopilotEngaged = false;
    }
}
