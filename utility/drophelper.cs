public class DropHelper
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond, 0.02);

    private readonly Func<IMyThrust, bool> ThrusterCondition;

    private bool BurningGliding = false, Braking = false;

    public DropHelper()
    {
        if (DROPHELPER_USE_HYDROGEN)
        {
            ThrusterCondition = null; // Use all thrusters
        }
        else
        {
            ThrusterCondition = thruster => thruster.DefinitionDisplayNameText.IndexOf("Hydrogen") < 0;
        }
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 2);
        if (parts.Length < 2) return;
        var command = parts[0];
        var subcommand = parts[1];

        if (command == "drop")
        {
            var shipControl = (ShipControlCommons)commons;

            if (subcommand == "start")
            {
                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                cruiser.Init(shipControl,
                             localForward: shipControl.ShipBlockOrientation.TransformDirection(DROPHELPER_BURN_DIRECTION));

                BurningGliding = true;
                Braking = false;
                eventDriver.Schedule(0, Burn);
            }
            else if (subcommand == "brake" || subcommand == "descend")
            {
                shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                var down = Base6Directions.GetFlippedDirection(shipControl.ShipUp);
                seeker.Init(shipControl,
                            localUp: Base6Directions.GetPerpendicular(down),
                            localForward: down);
                cruiser.Init(shipControl,
                             localForward: Base6Directions.GetFlippedDirection(shipControl.ShipUp));

                BurningGliding = false;
                Braking = true;
                eventDriver.Schedule(FramesPerRun, Brake);
            }
            else if (subcommand == "abort" || subcommand == "stop" ||
                     subcommand == "reset")
            {
                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                BurningGliding = false;
                Braking = false;
            }
        }
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        if (!BurningGliding) return;

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            // Override gyro, disable bottom thrusters
            shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            shipControl.ThrustControl.Enable(Base6Directions.Direction.Up, false);

            var down = Base6Directions.GetFlippedDirection(shipControl.ShipUp);
            seeker.Init(shipControl,
                        localUp: Base6Directions.GetPerpendicular(down),
                        localForward: down);
            
            eventDriver.Schedule(FramesPerRun, Glide);
        }
        else
        {
            cruiser.Cruise(shipControl, eventDriver, DROPHELPER_BURN_SPEED,
                           ThrusterCondition);

            eventDriver.Schedule(FramesPerRun, Burn);
        }
    }

    public void Glide(ZACommons commons, EventDriver eventDriver)
    {
        if (!BurningGliding) return;

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            eventDriver.Schedule(FramesPerRun, Glide);
        }
        else
        {
            // If we left gravity, just abort.
            shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            BurningGliding = false;
            Braking = false;
        }
    }

    public void Brake(ZACommons commons, EventDriver eventDriver)
    {
        if (!Braking) return;

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        var accel = gravity.Normalize();
        if (accel > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            cruiser.Cruise(shipControl, eventDriver, DROPHELPER_BRAKING_SPEED,
                           ThrusterCondition);

            eventDriver.Schedule(FramesPerRun, Brake);
        }
        else
        {
            // If we left gravity, just abort.
            shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            BurningGliding = false;
            Braking = false;
        }
    }

    private IMyRemoteControl GetRemoteControl(ZACommons commons)
    {
        var remoteGroup = commons.GetBlockGroupWithName(DROPHELPER_REMOTE_GROUP);
        if (remoteGroup == null)
        {
            throw new Exception("Missing group: " + DROPHELPER_REMOTE_GROUP);
        }
        var remotes = ZACommons.GetBlocksOfType<IMyRemoteControl>(remoteGroup.Blocks);
        if (remotes.Count == 0)
        {
            throw new Exception("Expecting at least 1 remote in group");
        }
        return (IMyRemoteControl)remotes[0];
    }
}
