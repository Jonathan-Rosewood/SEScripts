//@ shipcontrol eventdriver seeker cruiser customdata
public class VTVLHelper
{
    private const string LastCommandKey = "VTVLHelper_LastCommand";

    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);
    private readonly Cruiser cruiser = new Cruiser(1.0 / RunsPerSecond, 0.02);
    private readonly Cruiser LongCruiser = new Cruiser(1.0 / RunsPerSecond, 0.02);
    private readonly Cruiser LatCruiser = new Cruiser(1.0 / RunsPerSecond, 0.02);

    enum Modes { Idle, Burning, Gliding, Braking, Approaching, Launching, Orbiting };

    private Modes Mode = Modes.Idle;
    private Func<IMyThrust, bool> ThrusterCondition = null;

    private bool Autodrop = false;
    private double TargetElevation, BrakingElevation;
    private Func<IMyThrust, bool> AutoThrusterCondition = null;
    private double MinimumSpeed;
    private Vector3D? DropTarget = null;

    private Func<ZACommons, EventDriver, bool> LivenessCheck = null;

    private double Elevation, Distance;

    // Multiply by FramesPerRun to get real frame delay
    private const uint OrbitOnDelay = 5;
    private const uint OrbitOffDelay = 85;
    private ulong OrbitTicks = 0;

    private Base6Directions.Direction BurnDirection, BrakeDirection, LaunchDirection;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     ZACustomData customData,
                     Func<ZACommons, EventDriver, bool> livenessCheck = null)
    {
        BurnDirection = customData.GetDirection("burnDirection", VTVLHELPER_BURN_DIRECTION);
        BrakeDirection = customData.GetDirection("brakeDirection", VTVLHELPER_BRAKE_DIRECTION);
        LaunchDirection = customData.GetDirection("launchDirection", VTVLHELPER_LAUNCH_DIRECTION);

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
        var parts = argument.Split(new char[] { ' ' }, 10);
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
                             localForward: BurnDirection);

                if (Mode != Modes.Burning)
                {
                    Mode = Modes.Burning;
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
                var down = shipControl.ShipBlockOrientation.TransformDirection(BrakeDirection);
                seeker.Init(shipControl,
                            shipUp: Base6Directions.GetPerpendicular(down),
                            shipForward: down);
                cruiser.Init(shipControl,
                             localForward: BrakeDirection);

                if (Mode != Modes.Braking)
                {
                    Mode = Modes.Braking;
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

                DropTarget = null;
                if (parts.Length > 9)
                {
                    double x, y, z;
                    if (double.TryParse(parts[7], out x) &&
                        double.TryParse(parts[8], out y) &&
                        double.TryParse(parts[9], out z))
                    {
                        DropTarget = new Vector3D(x, y, z);
                    }
                }

                shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                                  thrusterCondition: ThrusterCondition);
                cruiser.Init(shipControl,
                             localForward: BurnDirection);

                if (Mode != Modes.Burning)
                {
                    Mode = Modes.Burning;
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
                var forward = shipControl.ShipBlockOrientation.TransformDirection(LaunchDirection);
                seeker.Init(shipControl,
                            shipUp: Base6Directions.GetPerpendicular(forward),
                            shipForward: forward);
                cruiser.Init(shipControl,
                             localForward: LaunchDirection);

                if (Mode != Modes.Launching)
                {
                    Mode = Modes.Launching;
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

                if (Mode != Modes.Orbiting)
                {
                    OrbitTicks = 0;
                    Mode = Modes.Orbiting;
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
        if (ShouldAbort(commons, eventDriver, Modes.Burning, false)) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            // Override gyro, disable "bottom" thrusters
            shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                              thrusterCondition: ThrusterCondition);
            shipControl.ThrustControl.Enable(Base6Directions.GetFlippedDirection(BrakeDirection), false);

            var down = shipControl.ShipBlockOrientation.TransformDirection(BrakeDirection);
            seeker.Init(shipControl,
                        shipUp: Base6Directions.GetPerpendicular(down),
                        shipForward: down);

            if (Autodrop)
            {
                // "forward" & "right"
                var forward = Base6Directions.GetPerpendicular(BrakeDirection);
                var right = Base6Directions.GetCross(forward, BrakeDirection);
                // Actual orientations don't matter
                // Just as long as they're planar & perpendicular to down
                LongCruiser.Init(shipControl, localForward: forward);
                LatCruiser.Init(shipControl, localForward: right);
            }

            Mode = Modes.Gliding;
            eventDriver.Schedule(FramesPerRun, Glide);
        }
        else
        {
            cruiser.Cruise(shipControl, VTVLHELPER_BURN_SPEED,
                           condition: ThrusterCondition);

            eventDriver.Schedule(FramesPerRun, Burn);
        }
    }

    public void Glide(ZACommons commons, EventDriver eventDriver)
    {
        if (ShouldAbort(commons, eventDriver, Modes.Gliding, Autodrop)) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawPitchError;
            seeker.Seek(shipControl, gravity, out yawPitchError);

            if (Autodrop)
            {
                // Shouldn't fail since we do gravity check above
                if (!controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out Elevation)) Elevation = 0.0;

                // Stay on target if we have one
                Alignment(shipControl, controller);

                Distance = Elevation - BrakingElevation;
                if (Elevation < BrakingElevation)
                {
                    shipControl.Reset(gyroOverride: true, thrusterEnable: true,
                                      thrusterCondition: ThrusterCondition);
                    ThrusterCondition = AutoThrusterCondition;
                    cruiser.Init(shipControl,
                                 localForward: BrakeDirection);

                    Mode = Modes.Approaching;
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
        if (ShouldAbort(commons, eventDriver, Modes.Braking, Autodrop)) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawPitchError;
            seeker.Seek(shipControl, gravity, out yawPitchError);

            cruiser.Cruise(shipControl, VTVLHELPER_BRAKING_SPEED,
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
        if (Mode != Modes.Approaching) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawPitchError;
            seeker.Seek(shipControl, gravity, out yawPitchError);

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

                cruiser.Cruise(shipControl, targetSpeed,
                               condition: ThrusterCondition,
                               enableForward: false);

                // Keep on target
                Alignment(shipControl, controller);

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
        if (Mode != Modes.Launching) return;

        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawPitchError;
            seeker.Seek(shipControl, -gravity, out yawPitchError);

            cruiser.Cruise(shipControl, VTVLHELPER_LAUNCH_SPEED,
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
        if (Mode != Modes.Orbiting) return;
        var shipControl = (ShipControlCommons)commons;

        var controller = GetShipController(shipControl);
        if (controller == null) return;
        var gravity = controller.GetNaturalGravity();
        if (gravity.LengthSquared() > 0.0)
        {
            double yawPitchError;
            seeker.Seek(shipControl, gravity, out yawPitchError);

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
        if (Mode != Modes.Orbiting) return;
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
            case Modes.Idle:
                break;
            case Modes.Burning:
                commons.Echo("VTVL: Burn phase");
                if (Autodrop) commons.Echo("Auto-drop starting");
                break;
            case Modes.Gliding:
                commons.Echo("VTVL: Glide phase");
                if (Autodrop)
                {
                    commons.Echo(string.Format("Elevation: {0:F2} m", Elevation));
                    commons.Echo(string.Format("Brake Distance: {0:F2} m", Distance));
                }
                break;
            case Modes.Braking:
                commons.Echo("VTVL: Braking");
                break;
            case Modes.Approaching:
                commons.Echo("VTVL: Approach phase");
                commons.Echo(string.Format("Elevation: {0:F2} m", Elevation));
                commons.Echo(string.Format("Stop Distance: {0:F2} m", Distance));
                break;
            case Modes.Launching:
                commons.Echo("VTVL: Launching");
                break;
            case Modes.Orbiting:
                commons.Echo("VTVL: Orbiting");
                break;
        }
    }

    private void Reset(ShipControlCommons shipControl)
    {
        shipControl.Reset(gyroOverride: false, thrusterEnable: true,
                          thrusterCondition: ThrusterCondition);
        Mode = Modes.Idle;

        SaveLastCommand(shipControl, null);
    }

    private void ResetOrbit(ShipControlCommons shipControl)
    {
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();
        gyroControl.EnableOverride(false);

        Mode = Modes.Idle;

        SaveLastCommand(shipControl, null);
    }

    private IMyShipController GetShipController(ShipControlCommons shipControl)
    {
        if (shipControl.ShipController == null)
        {
            // No more controllers? Just abort
            if (Mode == Modes.Orbiting)
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
                             Modes expectedMode, bool ignoreLiveness)
    {
        if (!ignoreLiveness && LivenessCheck != null &&
            !LivenessCheck(commons, eventDriver))
        {
            Reset((ShipControlCommons)commons);
        }
        return Mode != expectedMode;
    }

    private void Alignment(ShipControlCommons shipControl, IMyShipController controller)
    {
        Vector3D center;
        if (DropTarget == null || !controller.TryGetPlanetPosition(out center)) return;

        // Project the target position to our sphere
        var targetRayDirection = Vector3D.Normalize((Vector3D)DropTarget - center);
        var myRayLength = (shipControl.ReferencePoint - center).Length();
        var targetPosition = center + targetRayDirection * myRayLength;

        // Now get offset to target point on our sphere
        // (not all that accurate over large distances, but eh)
        var targetOffset = targetPosition - shipControl.ReferencePoint;

        // Project targetOffset along each reference vector,
        // set cruiser speed appropriately
        AlignmentThrust(shipControl, targetOffset, LongCruiser);
        AlignmentThrust(shipControl, targetOffset, LatCruiser);
    }

    private void AlignmentThrust(ShipControlCommons shipControl, Vector3D offset, Cruiser cruiser)
    {
        var velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            // Project offset against reference direction
            var referenceDirection = GetReferenceVector(shipControl, cruiser.LocalForward);
            var referenceDistance = Vector3D.Dot(offset, referenceDirection);
            var targetSpeed = Math.Min(Math.Abs(referenceDistance) * VTVLHELPER_APPROACH_GAIN, VTVLHELPER_MAXIMUM_SPEED);
            targetSpeed *= Math.Sign(referenceDistance);

            Func<IMyThrust, bool> AlignThrusterCondition = VTVLHELPER_USE_BRAKING_THRUSTER_SPEC_FOR_ALIGN ? ThrusterCondition : null;
            cruiser.Cruise(shipControl, targetSpeed, (Vector3D)velocity,
                           condition: AlignThrusterCondition);
        }
    }

    private Vector3D GetReferenceVector(ShipControlCommons shipControl, Base6Directions.Direction direction)
    {
        var offset = shipControl.Me.Position + Base6Directions.GetIntVector(shipControl.ShipBlockOrientation.TransformDirection(direction));
        return Vector3D.Normalize(shipControl.Me.CubeGrid.GridIntegerToWorld(offset) - shipControl.Me.GetPosition());
    }
}
