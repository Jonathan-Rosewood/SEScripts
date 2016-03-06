public class VTVLHelper
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond, 0.02);

    private const int IDLE = 0;
    private const int BURNING_GLIDING = 1;
    private const int BRAKING = 2;
    private const int LAUNCHING = 3;

    private int Mode = IDLE;
    private Func<IMyThrust, bool> ThrusterCondition = null;

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 3);
        if (parts.Length < 2) return;
        var command = parts[0];
        var subcommand = parts[1];

        if (command == "drop")
        {
            var shipControl = (ShipControlCommons)commons;
            ThrusterCondition = parts.Length > 2 ? ParseThrusterFlags(parts[2]) : null;

            if (subcommand == "start")
            {
                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                cruiser.Init(shipControl,
                             localForward: VTVLHELPER_BURN_DIRECTION);

                if (Mode != BURNING_GLIDING)
                {
                    Mode = BURNING_GLIDING;
                    eventDriver.Schedule(0, Burn);
                }
            }
            else if (subcommand == "brake" || subcommand == "descend")
            {
                shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                var down = shipControl.ShipBlockOrientation.TransformDirection(VTVLHELPER_BRAKE_DIRECTION);
                seeker.Init(shipControl,
                            shipUp: Base6Directions.GetPerpendicular(down),
                            shipForward: down);
                cruiser.Init(shipControl,
                             localForward: VTVLHELPER_BRAKE_DIRECTION);

                if (Mode != BRAKING)
                {
                    Mode = BRAKING;
                    eventDriver.Schedule(FramesPerRun, Brake);
                }
            }
            else if (subcommand == "abort" || subcommand == "stop" ||
                     subcommand == "reset")
            {
                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                Mode = IDLE;
            }
        }
        else if (command == "launch")
        {
            var shipControl = (ShipControlCommons)commons;
            ThrusterCondition = parts.Length > 2 ? ParseThrusterFlags(parts[2]) : null;

            if (subcommand == "start")
            {
                shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                var forward = shipControl.ShipBlockOrientation.TransformDirection(VTVLHELPER_LAUNCH_DIRECTION);
                seeker.Init(shipControl,
                            shipUp: Base6Directions.GetPerpendicular(forward),
                            shipForward: forward);
                cruiser.Init(shipControl,
                             localForward: VTVLHELPER_LAUNCH_DIRECTION);

                if (Mode != LAUNCHING)
                {
                    Mode = LAUNCHING;
                    eventDriver.Schedule(FramesPerRun, Launch);
                }
            }
            else if (subcommand == "abort" || subcommand == "stop" ||
                     subcommand == "reset")
            {
                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                Mode = IDLE;
            }
        }
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != BURNING_GLIDING) return;

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            // Override gyro, disable "bottom" thrusters
            shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            var down = shipControl.ShipBlockOrientation.TransformDirection(VTVLHELPER_BRAKE_DIRECTION);
            shipControl.ThrustControl.Enable(Base6Directions.GetFlippedDirection(down), false);

            seeker.Init(shipControl,
                        shipUp: Base6Directions.GetPerpendicular(down),
                        shipForward: down);
            
            eventDriver.Schedule(FramesPerRun, Glide);
        }
        else
        {
            cruiser.Cruise(shipControl, eventDriver, VTVLHELPER_BURN_SPEED,
                           condition: ThrusterCondition);

            eventDriver.Schedule(FramesPerRun, Burn);
        }
    }

    public void Glide(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != BURNING_GLIDING) return;

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
            Mode = IDLE;
        }
    }

    public void Brake(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != BRAKING) return;

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        var accel = gravity.Normalize();
        if (accel > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            cruiser.Cruise(shipControl, eventDriver, VTVLHELPER_BRAKING_SPEED,
                           condition: ThrusterCondition,
                           enableForward: false);

            eventDriver.Schedule(FramesPerRun, Brake);
        }
        else
        {
            // If we left gravity, just abort.
            shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            Mode = IDLE;
        }
    }

    public void Launch(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != LAUNCHING) return;

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        var accel = gravity.Normalize();
        if (accel > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, -gravity, out yawError, out pitchError);

            cruiser.Cruise(shipControl, eventDriver, VTVLHELPER_LAUNCH_SPEED,
                           condition: ThrusterCondition,
                           enableBackward: false);

            eventDriver.Schedule(FramesPerRun, Launch);
        }
        else
        {
            // Out of gravity
            shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            Mode = IDLE;
        }
    }

    private IMyRemoteControl GetRemoteControl(ZACommons commons)
    {
        var remoteGroup = commons.GetBlockGroupWithName(VTVLHELPER_REMOTE_GROUP);
        if (remoteGroup == null)
        {
            throw new Exception("Missing group: " + VTVLHELPER_REMOTE_GROUP);
        }
        var remotes = ZACommons.GetBlocksOfType<IMyRemoteControl>(remoteGroup.Blocks);
        if (remotes.Count == 0)
        {
            throw new Exception("Expecting at least 1 remote in group");
        }
        return (IMyRemoteControl)remotes[0];
    }

    private Func<IMyThrust, bool> ParseThrusterFlags(string flags)
    {
        if (flags == null) return null; // Don't do extra work

        var useIon = flags.IndexOf('i') >= 0;
        var useH = flags.IndexOf('h') >= 0;
        var useAtm = flags.IndexOf('a') >= 0;

        return thruster =>
            {
                // Probably only works in English...
                // Why no subclasses...
                var defName = thruster.DefinitionDisplayNameText;
                var isH = defName.IndexOf("Hydrogen") >= 0;
                var isAtm = defName.IndexOf("Atmospheric") >= 0;
                return ((isH && useH) ||
                        (isAtm && useAtm) ||
                        (!isH && !isAtm && useIon));
            };
    }
}
