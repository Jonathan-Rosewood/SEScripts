//@ shipcontrol eventdriver cruiser
public class TranslateAutopilot
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Cruiser forwardCruiser = new Cruiser(1.0 / RunsPerSecond,
                                                          AUTOPILOT_THRUST_DEAD_ZONE);
    private readonly Cruiser upCruiser = new Cruiser(1.0 / RunsPerSecond,
                                                     AUTOPILOT_THRUST_DEAD_ZONE);
    private readonly Cruiser leftCruiser = new Cruiser(1.0 / RunsPerSecond,
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
        forwardCruiser.Init(shipControl,
                            localForward: shipControl.ShipForward);
        upCruiser.Init(shipControl,
                       localForward: shipControl.ShipUp);
        leftCruiser.Init(shipControl,
                         localForward: Base6Directions.GetLeft(shipControl.ShipUp, shipControl.ShipForward));
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
        var distance = targetVector.Length();

        // Take projection of target vector on each of our axes
        var forwardError = Vector3D.Dot(targetVector, shipControl.ReferenceForward);
        var upError = Vector3D.Dot(targetVector, shipControl.ReferenceUp);
        var leftError = Vector3D.Dot(targetVector, shipControl.ReferenceLeft);

        var velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            // Naive approach: independent control of each axis
            Thrust(shipControl, forwardError, (Vector3D)velocity, forwardCruiser);
            Thrust(shipControl, upError, (Vector3D)velocity, upCruiser);
            Thrust(shipControl, leftError, (Vector3D)velocity, leftCruiser);

            if (distance < AUTOPILOT_DISENGAGE_DISTANCE)
            {
                Reset(commons);
            }
            else
            {
                eventDriver.Schedule(FramesPerRun, Run);
            }
        }
        else
        {
            // Can't measure velocity anymore
            Reset(commons);
        }
    }

    private void Thrust(ShipControlCommons shipControl, double distance,
                        Vector3D velocity, Cruiser cruiser)
    {
        if (Math.Abs(distance) < 1.0)
        {
            // Close enough
            var thrustControl = shipControl.ThrustControl;
            thrustControl.Enable(cruiser.LocalForward, true);
            thrustControl.Enable(cruiser.LocalBackward, true);
        }
        else
        {
            var targetSpeed = Math.Min(Math.Abs(distance) / AUTOPILOT_TTT_BUFFER,
                                       AutopilotSpeed);
            targetSpeed = Math.Max(targetSpeed, AUTOPILOT_MIN_SPEED); // Avoid Zeno's paradox...
            targetSpeed *= Math.Sign(distance);

            cruiser.Cruise(shipControl, targetSpeed, velocity);
        }
    }

    public void Reset(ZACommons commons)
    {
        var shipControl = (ShipControlCommons)commons;
        shipControl.Reset(gyroOverride: false);
        AutopilotEngaged = false;
    }
}
