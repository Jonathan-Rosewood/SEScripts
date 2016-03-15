public class VTVLHelper
{
    private const string LastCommandKey = "VTVLHelper_LastCommand";

    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond, 0.02);

    private const int IDLE = 0;
    private const int BURNING = 1;
    private const int GLIDING = 2;
    private const int BRAKING = 3;
    private const int APPROACHING = 4;
    private const int LAUNCHING = 5;

    private int Mode = IDLE;
    private Func<IMyThrust, bool> ThrusterCondition = null;

    private bool Autodrop = false;
    private Vector3D TargetCenter;
    private double TargetRadius, BrakingRadius;
    private Func<IMyThrust, bool> AutoThrusterCondition = null;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var lastCommand = commons.GetValue(LastCommandKey);
        if (lastCommand != null)
        {
            HandleCommand(commons, eventDriver, lastCommand);
        }
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 5);
        if (parts.Length < 2) return;
        var command = parts[0];
        var subcommand = parts[1];

        if (command == "drop")
        {
            var shipControl = (ShipControlCommons)commons;

            if (subcommand == "start")
            {
                ThrusterCondition = parts.Length > 2 ? ParseThrusterFlags(parts[2]) : null;
                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                cruiser.Init(shipControl,
                             localForward: VTVLHELPER_BURN_DIRECTION);

                if (Mode != BURNING)
                {
                    Mode = BURNING;
                    Autodrop = false;
                    eventDriver.Schedule(0, Burn);
                }

                SaveLastCommand(commons, argument);
            }
            else if (subcommand == "brake" || subcommand == "descend")
            {
                ThrusterCondition = parts.Length > 2 ? ParseThrusterFlags(parts[2]) : null;
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

                SaveLastCommand(commons, argument);
            }
            else if (subcommand == "abort" || subcommand == "stop" ||
                     subcommand == "reset")
            {
                Reset(shipControl);
            }
            else if (subcommand == "auto")
            {
                if (!AcquireTarget(commons)) return;
                // Defaults
                ThrusterCondition = null;
                var extraRadius = 0.0;
                AutoThrusterCondition = null;
                // From arguments
                if (parts.Length > 2) ThrusterCondition = ParseThrusterFlags(parts[2]);
                if (parts.Length > 3)
                {
                    if (!double.TryParse(parts[3], out extraRadius)) extraRadius = 0.0;
                }
                if (parts.Length > 4) AutoThrusterCondition = ParseThrusterFlags(parts[4]);

                BrakingRadius = TargetRadius + extraRadius;

                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                cruiser.Init(shipControl,
                             localForward: VTVLHELPER_BURN_DIRECTION);

                if (Mode != BURNING)
                {
                    Mode = BURNING;
                    Autodrop = true;
                    eventDriver.Schedule(0, Burn);
                }

                SaveLastCommand(commons, argument);
            }
        }
        else if (command == "launch")
        {
            var shipControl = (ShipControlCommons)commons;

            if (subcommand == "start")
            {
                ThrusterCondition = parts.Length > 2 ? ParseThrusterFlags(parts[2]) : null;
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

                SaveLastCommand(commons, argument);
            }
            else if (subcommand == "abort" || subcommand == "stop" ||
                     subcommand == "reset")
            {
                Reset(shipControl);
            }
        }
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != BURNING) return;

        commons.Echo("VTVL: Burn phase");

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            // Override gyro, disable "bottom" thrusters
            shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            shipControl.ThrustControl.Enable(Base6Directions.GetFlippedDirection(VTVLHELPER_BRAKE_DIRECTION), false);

            var down = shipControl.ShipBlockOrientation.TransformDirection(VTVLHELPER_BRAKE_DIRECTION);
            seeker.Init(shipControl,
                        shipUp: Base6Directions.GetPerpendicular(down),
                        shipForward: down);

            Mode = GLIDING;
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
        if (Mode != GLIDING) return;

        commons.Echo("VTVL: Glide phase");

        var shipControl = (ShipControlCommons)commons;

        var remote = GetRemoteControl(commons);
        var gravity = remote.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            if (Autodrop)
            {
                var distance = (remote.GetPosition() - TargetCenter).Length();
                commons.Echo(string.Format("Distance: {0:F2} m", distance));
                if (distance < BrakingRadius)
                {
                    shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                                      thrusterCondition: ThrusterCondition);
                    ThrusterCondition = AutoThrusterCondition;
                    cruiser.Init(shipControl,
                                 localForward: VTVLHELPER_BRAKE_DIRECTION);

                    Mode = APPROACHING;
                    eventDriver.Schedule(FramesPerRun, Approach);
                }
                else
                {
                    eventDriver.Schedule(FramesPerRun, Glide);
                }
            }
            else
            {
                eventDriver.Schedule(FramesPerRun, Glide);
            }
        }
        else
        {
            // If we left gravity, just abort.
            Reset(shipControl);
        }
    }

    public void Brake(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != BRAKING) return;

        commons.Echo("VTVL: Braking");

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
            Reset(shipControl);
        }
    }

    public void Approach(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != APPROACHING) return;

        commons.Echo("VTVL: Approach phase");

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
            if (distance <= TargetRadius || remote.IsUnderControl)
            {
                // All done. Re-enable thrusters and restore control.
                Reset(shipControl);
            }
            else
            {
                var distanceToStop = distance - TargetRadius;
                var targetSpeed = Math.Min(distanceToStop / VTVLHELPER_TTT_BUFFER,
                                           VTVLHELPER_BRAKING_SPEED);
                targetSpeed = Math.Max(targetSpeed, 5.0);

                cruiser.Cruise(shipControl, eventDriver, targetSpeed,
                               condition: ThrusterCondition,
                               enableForward: false);

                eventDriver.Schedule(FramesPerRun, Approach);
            }
        }
        else
        {
            // If we left gravity, just abort.
            Reset(shipControl);
        }
    }

    public void Launch(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != LAUNCHING) return;

        commons.Echo("VTVL: Launching");

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
            Reset(shipControl);
        }
    }

    private void Reset(ShipControlCommons shipControl)
    {
        shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                          thrusterCondition: ThrusterCondition);
        Mode = IDLE;

        SaveLastCommand(shipControl, null);
    }

    public IMyRemoteControl GetRemoteControl(ZACommons commons)
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

    private void SaveLastCommand(ZACommons commons, string argument)
    {
        commons.SetValue(LastCommandKey, argument);
    }

    private bool AcquireTarget(ZACommons commons)
    {
        var panelGroup = commons.GetBlockGroupWithName(VTVLHELPER_TARGET_GROUP);
        if (panelGroup == null) return false;

        var panels = ZACommons.GetBlocksOfType<IMyTextPanel>(panelGroup.Blocks);
        if (panels.Count == 0) return false;

        var panel = panels[0] as IMyTextPanel; // Just use the first one
        var targetString = panel.GetPublicText();

        // Parse target info
        var parts = targetString.Split(';');
        if (parts.Length != 4) return false;
        TargetCenter = new Vector3D();
        for (int i = 0; i < 3; i++)
        {
            double val;
            if (double.TryParse(parts[i], out val))
            {
                TargetCenter.SetDim(i, val);
            }
            else
            {
                return false;
            }
        }
        return double.TryParse(parts[3], out TargetRadius);
    }
}
