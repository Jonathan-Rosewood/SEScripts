//@ shipcontrol eventdriver seeker cruiser
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
    private const int ORBITING = 6;

    private int Mode = IDLE;
    private Func<IMyThrust, bool> ThrusterCondition = null;

    private bool Autodrop = false;
    private double TargetElevation, BrakingElevation;
    private Func<IMyThrust, bool> AutoThrusterCondition = null;
    private double MinimumSpeed;

    private Func<ZACommons, EventDriver, bool> LivenessCheck = null;

    private double Elevation, Distance;

    // Multiply by FramesPerRun to get real frame delay
    private const uint OrbitOnDelay = 5;
    private const uint OrbitOffDelay = 85;
    private ulong OrbitTicks = 0;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Func<ZACommons, EventDriver, bool> livenessCheck = null)
    {
        LivenessCheck = livenessCheck;

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
        var parts = argument.Split(new char[] { ' ' }, 7);
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
                // Defaults
                ThrusterCondition = null;
                AutoThrusterCondition = null;
                TargetElevation = 1000.0;
                // From arguments
                if (parts.Length > 2)
                {
                    if (double.TryParse(parts[2], out TargetElevation))
                    {
                        TargetElevation = Math.Max(0.0, TargetElevation);
                    }
                    else
                    {
                        TargetElevation = 1000.0;
                    }
                }

                if (parts.Length > 3) ThrusterCondition = ParseThrusterFlags(parts[3]);

                BrakingElevation = TargetElevation;
                if (parts.Length > 4)
                {
                    if (double.TryParse(parts[4], out BrakingElevation))
                    {
                        BrakingElevation = Math.Max(TargetElevation, BrakingElevation);
                    }
                    else
                    {
                        BrakingElevation = TargetElevation;
                    }
                }

                if (parts.Length > 5) AutoThrusterCondition = ParseThrusterFlags(parts[5]);

                MinimumSpeed = VTVLHELPER_MINIMUM_SPEED;
                if (parts.Length > 6)
                {
                    if (double.TryParse(parts[6], out MinimumSpeed))
                    {
                        MinimumSpeed = Math.Max(1.0, MinimumSpeed);
                    }
                    else
                    {
                        MinimumSpeed = VTVLHELPER_MINIMUM_SPEED;
                    }
                }

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
        else if (command == "orbit")
        {
            var shipControl = (ShipControlCommons)commons;

            if (subcommand == "start")
            {
                OrbitInit(shipControl);

                if (Mode != ORBITING)
                {
                    OrbitTicks = 0;
                    Mode = ORBITING;
                    eventDriver.Schedule(0, OrbitOn);
                }

                SaveLastCommand(commons, argument);
            }
            else if (subcommand == "abort" || subcommand == "stop" ||
                     subcommand == "reset")
            {
                ResetOrbit(shipControl);
            }
        }
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        if (ShouldAbort(commons, eventDriver, BURNING, false)) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
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
        if (ShouldAbort(commons, eventDriver, GLIDING, Autodrop)) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            if (Autodrop)
            {
                // Shouldn't fail since we do gravity check above
                if (!controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elevation)) Elevation = 0.0;

                Distance = Elevation - BrakingElevation;
                if (Elevation < BrakingElevation)
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
        if (ShouldAbort(commons, eventDriver, BRAKING, Autodrop)) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
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

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            // Shouldn't fail since we do gravity check above
            if (!controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elevation)) Elevation = 0.0;

            Distance = Elevation - TargetElevation;
            if (Elevation <= TargetElevation)
            {
                // All done. Re-enable thrusters and restore control.
                Reset(shipControl);

                ZACommons.StartTimerBlockWithName(commons.Blocks, VTVLHELPER_DROP_DONE);
            }
            else
            {
                var targetSpeed = Math.Min(Distance * VTVLHELPER_APPROACH_GAIN,
                                           VTVLHELPER_BRAKING_SPEED);
                targetSpeed = Math.Max(targetSpeed, MinimumSpeed);

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

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
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

            ZACommons.StartTimerBlockWithName(commons.Blocks, VTVLHELPER_LAUNCH_DONE);
        }
    }

    private void OrbitInit(ShipControlCommons shipControl)
    {
        // Don't touch thrusters at all
        shipControl.GyroControl.Reset();
        shipControl.GyroControl.EnableOverride(true);
        var forward = shipControl.ShipBlockOrientation.TransformDirection(VTVLHELPER_ORBIT_DIRECTION);
        seeker.Init(shipControl,
                    shipUp: Base6Directions.GetPerpendicular(forward),
                    shipForward: forward);
    }

    public void OrbitOn(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != ORBITING) return;
        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawError, pitchError;
            seeker.Seek(shipControl, gravity, out yawError, out pitchError);

            OrbitTicks++;
            if (OrbitTicks >= OrbitOnDelay)
            {
                // Switch off
                shipControl.GyroControl.Reset();
                shipControl.GyroControl.EnableOverride(false);
                eventDriver.Schedule(OrbitOffDelay, OrbitOff);
            }
            else
            {
                eventDriver.Schedule(FramesPerRun, OrbitOn);
            }
        }
        else
        {
            // Out of gravity
            ResetOrbit(shipControl);
        }
    }

    public void OrbitOff(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != ORBITING) return;
        var shipControl = (ShipControlCommons)commons;

        // Switch on
        OrbitInit(shipControl);
        OrbitTicks = 0;
        eventDriver.Schedule(FramesPerRun, OrbitOn);
    }

    public void Display(ZACommons commons)
    {
        switch (Mode)
        {
            case IDLE:
                break;
            case BURNING:
                commons.Echo("VTVL: Burn phase");
                if (Autodrop) commons.Echo("Auto-drop starting");
                break;
            case GLIDING:
                commons.Echo("VTVL: Glide phase");
                if (Autodrop)
                {
                    commons.Echo(string.Format("Elevation: {0:F2} m", Elevation));
                    commons.Echo(string.Format("Brake Distance: {0:F2} m", Distance));
                }
                break;
            case BRAKING:
                commons.Echo("VTVL: Braking");
                break;
            case APPROACHING:
                commons.Echo("VTVL: Approach phase");
                commons.Echo(string.Format("Elevation: {0:F2} m", Elevation));
                commons.Echo(string.Format("Stop Distance: {0:F2} m", Distance));
                break;
            case LAUNCHING:
                commons.Echo("VTVL: Launching");
                break;
            case ORBITING:
                commons.Echo("VTVL: Orbiting");
                break;
        }
    }

    private void Reset(ShipControlCommons shipControl)
    {
        shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                          thrusterCondition: ThrusterCondition);
        Mode = IDLE;

        SaveLastCommand(shipControl, null);
    }

    private void ResetOrbit(ShipControlCommons shipControl)
    {
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();
        gyroControl.EnableOverride(false);

        Mode = IDLE;

        SaveLastCommand(shipControl, null);
    }

    private IMyShipController GetShipController(ShipControlCommons shipControl)
    {
        if (shipControl.ShipController == null)
        {
            // No more controllers? Just abort
            if (Mode == ORBITING)
            {
                ResetOrbit(shipControl);
            }
            else
            {
                Reset(shipControl);
            }
        }
        return shipControl.ShipController;
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

    private bool ShouldAbort(ZACommons commons, EventDriver eventDriver,
                             int expectedMode, bool ignoreLiveness)
    {
        if (!ignoreLiveness && LivenessCheck != null &&
            !LivenessCheck(commons, eventDriver))
        {
            Reset((ShipControlCommons)commons);
        }
        return Mode != expectedMode;
    }
}
