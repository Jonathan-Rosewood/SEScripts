public class LOSMiner
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const double LOS_OFFSET = 200.0; // Meters forward

    private Vector3D StartPoint;
    private Vector3D StartDirection, StartUp, StartLeft;

    private bool Mining = false;
    private bool Reversing = false;

    public LOSMiner()
    {
        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        var command = argument.Trim().ToLower();
        switch (command)
        {
            case "start":
                {
                    var shipControl = (ShipControlCommons)commons;
                    SetTarget(shipControl);
                    shipControl.Reset(gyroOverride: true);

                    if (MINING_ROLL_RPM > 0.0f) shipControl.GyroControl.SetAxisVelocityRPM(GyroControl.Roll, MINING_ROLL_RPM);

                    seeker.Init(shipControl,
                                localUp: shipControl.ShipUp,
                                localForward: shipControl.ShipForward);
                    velocimeter.Reset();
                    thrustPID.Reset();

                    Reversing = false;
                    if (!Mining)
                    {
                        Mining = true;
                        eventDriver.Schedule(0, Mine);
                    }
                }
                break;
            case "reverse":
                {
                    var shipControl = (ShipControlCommons)commons;
                    SetTarget(shipControl);
                    shipControl.Reset(gyroOverride: true);

                    seeker.Init(shipControl,
                                localUp: shipControl.ShipUp,
                                localForward: Base6Directions.GetFlippedDirection(shipControl.ShipForward));
                    velocimeter.Reset();
                    thrustPID.Reset();

                    Mining = false;
                    if (!Reversing)
                    {
                        Reversing = true;
                        eventDriver.Schedule(0, Reverse);
                    }
                }
                break;
            case "stop":
                {
                    Mining = false;
                    Reversing = false;

                    var shipControl = (ShipControlCommons)commons;
                    shipControl.Reset(gyroOverride: false);
                }
                break;
        }
    }

    public void Mine(ZACommons commons, EventDriver eventDriver)
    {
        if (!Mining) return;

        var shipControl = (ShipControlCommons)commons;

        var targetVector = GetTarget(shipControl, StartDirection);
        targetVector = Perturb(eventDriver.TimeSinceStart, targetVector);

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector,
                    out yawError, out pitchError);

        Thrust(commons, eventDriver, shipControl.ReferenceForward);

        eventDriver.Schedule(FramesPerRun, Mine);
    }

    public void Reverse(ZACommons commons, EventDriver eventDriver)
    {
        if (!Reversing) return;

        var shipControl = (ShipControlCommons)commons;

        var targetVector = GetTarget(shipControl, -StartDirection);
        targetVector = Perturb(eventDriver.TimeSinceStart, targetVector);

        double yawError, pitchError;
        seeker.Seek(shipControl, targetVector,
                    out yawError, out pitchError);

        Thrust(commons, eventDriver, -shipControl.ReferenceForward,
               Base6Directions.Direction.Backward);

        eventDriver.Schedule(FramesPerRun, Reverse);
    }

    private void SetTarget(ShipControlCommons shipControl)
    {
        // Only set if neither mining nor reversing
        if (!Mining && !Reversing)
        {
            StartPoint = shipControl.ReferencePoint;
            StartDirection = shipControl.ReferenceForward;
            StartUp = shipControl.ReferenceUp;
            StartLeft = shipControl.ReferenceLeft;
        }
    }

    private Vector3D GetTarget(ShipControlCommons shipControl,
                               Vector3D direction)
    {
        // Vector from start to current
        var startVector = shipControl.ReferencePoint - StartPoint;
        // Determine projection on start direction vector
        var startDot = startVector.Dot(direction);

        Vector3D targetVector;
        if (startDot >= 0.0)
        {
            // Set targetVector to that projection
            targetVector = startDot * direction;
        }
        else
        {
            // Or set it to the start (0,0,0) if the ship is
            // behind the start position
            targetVector = new Vector3D(0, 0, 0);
        }

        // Offset forward by some amount
        targetVector += LOS_OFFSET * direction;

        // Offset by difference between start and current positions
        targetVector += StartPoint - shipControl.ReferencePoint;

        return targetVector;
    }

    private Vector3D Perturb(TimeSpan timeSinceStart, Vector3D original)
    {
        original += StartUp * MINING_PERTURB_AMPLITUDE * Math.Cos(MINING_PERTURB_SCALE * timeSinceStart.TotalSeconds);
        original += StartLeft * MINING_PERTURB_AMPLITUDE * Math.Sin(MINING_PERTURB_SCALE * timeSinceStart.TotalSeconds);
        return original;
    }

    private void Thrust(ZACommons commons, EventDriver eventDriver,
                        Vector3D referenceForward,
                        Base6Directions.Direction forward = Base6Directions.Direction.Forward)
    {
        var shipControl = (ShipControlCommons)commons;

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            // Take dot product with forward unit vector
            var speed = Vector3D.Dot((Vector3D)velocity, referenceForward);
            var error = TARGET_MINING_SPEED - speed;

            var force = thrustPID.Compute(error);

            var thrustControl = shipControl.ThrustControl;
            var backward = Base6Directions.GetFlippedDirection(forward);
            if (force > 0.0)
            {
                thrustControl.Enable(forward, true);
                thrustControl.SetOverride(forward, force);
                thrustControl.Enable(backward, false);
            }
            else
            {
                thrustControl.Enable(forward, false);
                thrustControl.Enable(backward, true);
                thrustControl.SetOverride(backward, -force);
            }
        }
    }
}
