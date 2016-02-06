public class YawPitchAutopilot
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond,
                                                   AUTOPILOT_THRUST_DEAD_ZONE);

    private Vector3D AutopilotTarget;
    private double AutopilotSpeed;
    private bool AutopilotEngaged;

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

        cruiser.Init(shipControl,
                     localForward: shipControl.ShipForward);

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

        var targetSpeed = Math.Min(distance / AUTOPILOT_TTT_BUFFER,
                                   AutopilotSpeed);
        targetSpeed = Math.Max(targetSpeed, AUTOPILOT_MIN_SPEED); // Avoid Zeno's paradox...

        cruiser.Cruise(shipControl, eventDriver, targetSpeed);

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
