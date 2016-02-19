private readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly OrbitalDrop orbitalDrop = new OrbitalDrop();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference<IMyRemoteControl>(commons.Blocks);
        orbitalDrop.AcquireTarget(commons);
        orbitalDrop.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons);
}

public class OrbitalDrop
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond, 0.02);

    private Vector3D TargetCenter;
    private double TargetRadius, BrakingRadius;

    public void AcquireTarget(ZACommons commons)
    {
        // Find the sole text panel
        var panelGroup = commons.GetBlockGroupWithName("GS Target");
        if (panelGroup == null)
        {
            throw new Exception("Missing group: GS Target");
        }

        var panels = ZACommons.GetBlocksOfType<IMyTextPanel>(panelGroup.Blocks);
        if (panels.Count == 0)
        {
            throw new Exception("Expecting at least 1 text panel");
        }
        var panel = panels[0] as IMyTextPanel; // Just use the first one
        var targetString = panel.GetPublicText();

        // Parse target info
        var parts = targetString.Split(';');
        if (parts.Length != 4)
        {
            throw new Exception("Expecting exactly 4 parts to target info");
        }
        TargetCenter = new Vector3D();
        for (int i = 0; i < 3; i++)
        {
            TargetCenter.SetDim(i, double.Parse(parts[i]));
        }
        TargetRadius = double.Parse(parts[3]);
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        shipControl.Reset(gyroOverride: false, thrusterEnable: true);
        cruiser.Init(shipControl,
                     localForward: Base6Directions.GetFlippedDirection(shipControl.ShipUp));

        BrakingRadius = TargetRadius + 5000; // FIXME

        eventDriver.Schedule(0, Burn);
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        commons.Echo("Burn phase");

        var shipControl = (ShipControlCommons)commons;

        cruiser.Cruise(shipControl, eventDriver, 98.0); // FIXME

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        if (gravity.Length() > 0.0)
        {
            // Override gyro, disable bottom thrusters
            shipControl.Reset(gyroOverride: true, thrusterEnable: true);
            shipControl.ThrustControl.Enable(Base6Directions.Direction.Up, false);

            var down = Base6Directions.GetFlippedDirection(shipControl.ShipUp);
            seeker.Init(shipControl,
                        localUp: Base6Directions.GetPerpendicular(down),
                        localForward: down);
            
            eventDriver.Schedule(FramesPerRun, Glide);
        }
        else
        {
            eventDriver.Schedule(FramesPerRun, Burn);
        }
    }

    public void Glide(ZACommons commons, EventDriver eventDriver)
    {
        commons.Echo("Glide phase");

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        var accel = gravity.Normalize();
        if (accel > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            var distance = (remote.GetPosition() - TargetCenter).Length();
            commons.Echo(string.Format("Distance: {0:F2} m", distance));
            if (distance < BrakingRadius)
            {
                cruiser.Init(shipControl,
                             localForward: Base6Directions.GetFlippedDirection(shipControl.ShipUp));

                eventDriver.Schedule(FramesPerRun, Approach);
            }
            else
            {
                eventDriver.Schedule(FramesPerRun, Glide);
            }
        }
        else
        {
            // If we left gravity, just abort.
            shipControl.Reset(gyroOverride: false, thrusterEnable: true);
        }
    }

    public void Approach(ZACommons commons, EventDriver eventDriver)
    {
        commons.Echo("Approach phase");

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        var accel = gravity.Normalize();
        if (accel > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            var distance = (remote.GetPosition() - TargetCenter).Length();
            commons.Echo(string.Format("Distance: {0:F2} m", distance));
            if (distance <= (TargetRadius + 1.0) || remote.IsUnderControl)
            {
                // All done. Re-enable thrusters and restore control.
                shipControl.Reset(gyroOverride: false, thrusterEnable: true);
            }
            else
            {
                var distanceToStop = distance - TargetRadius;
                var targetSpeed = Math.Min(distanceToStop / 5.0,
                                           50.0); // FIXME
                targetSpeed = Math.Max(targetSpeed, 1.0); // FIXME
                targetSpeed *= Math.Sign(distanceToStop);

                cruiser.Cruise(shipControl, eventDriver, targetSpeed);

                eventDriver.Schedule(FramesPerRun, Approach);
            }
        }
        else
        {
            // If we left gravity, just abort.
            shipControl.Reset(gyroOverride: false, thrusterEnable: true);
        }
    }

    private IMyRemoteControl GetRemoteControl(ZACommons commons)
    {
        var remotes = ZACommons.GetBlocksOfType<IMyRemoteControl>(commons.Blocks);
        if (remotes.Count == 0)
        {
            throw new Exception("Expecting at least 1 remote control");
        }
        // Return the first one
        return (IMyRemoteControl)remotes[0];
    }
}
