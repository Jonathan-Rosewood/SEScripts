//@ shipcontrol eventdriver velocimeter pid
public class CruiseControl
{
    private const string LastCommandKey = "CruiseControl_LastCommand";

    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;

    private readonly double[] Bias;

    private bool Active = false;
    private double TargetSpeed, CurrentSpeed;
    private Base6Directions.Direction CruiseDirection;
    private string CruiseFlags;
    private uint VTicks;

    private Func<ZACommons, EventDriver, bool> LivenessCheck = null;

    public CruiseControl()
    {
        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;

        // Have to do this here
        Bias = new double[6];
        Bias[(int)Base6Directions.Direction.Forward] = CRUISE_CONTROL_BIAS_Z;
        Bias[(int)Base6Directions.Direction.Backward] = 1.0 / CRUISE_CONTROL_BIAS_Z;
        Bias[(int)Base6Directions.Direction.Left] = CRUISE_CONTROL_BIAS_X;
        Bias[(int)Base6Directions.Direction.Right] = 1.0 / CRUISE_CONTROL_BIAS_X;
        Bias[(int)Base6Directions.Direction.Up] = CRUISE_CONTROL_BIAS_Y;
        Bias[(int)Base6Directions.Direction.Down] = 1.0 / CRUISE_CONTROL_BIAS_Y;
    }

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

    private void Reset(ZACommons commons)
    {
        var thrustControl = ((ShipControlCommons)commons).ThrustControl;
        var collect = ParseCruiseFlags();
        thrustControl.Enable(true, collect);
        thrustControl.Reset(collect);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 4);
        if (parts.Length < 2) return;
        var command = parts[0];
        var speed = parts[1];

        if (command == "cruise")
        {
            if (speed == "stop")
            {
                CruiseFlags = null;
                if (parts.Length >= 3) CruiseFlags = parts[2];
                Reset(commons);
                Active = false;
                SaveLastCommand(commons, null);
            }
            else
            {
                CruiseDirection = Base6Directions.Direction.Forward;
                if (parts.Length >= 3)
                {
                    switch (parts[2])
                    {
                        case "forward":
                        case "forwards":
                        default:
                            break;

                        case "backward":
                        case "backwards":
                        case "reverse":
                            CruiseDirection = Base6Directions.Direction.Backward;
                            break;

                        case "left":
                            CruiseDirection = Base6Directions.Direction.Left;
                            break;

                        case "right":
                            CruiseDirection = Base6Directions.Direction.Right;
                            break;

                        case "up":
                            CruiseDirection = Base6Directions.Direction.Up;
                            break;

                        case "down":
                            CruiseDirection = Base6Directions.Direction.Down;
                            break;
                    }
                }

                CruiseFlags = null;
                if (parts.Length == 4) CruiseFlags = parts[3];

                double desiredSpeed;
                if (double.TryParse(speed, out desiredSpeed))
                {
                    TargetSpeed = Math.Max(desiredSpeed, 0.0);

                    velocimeter.Reset();
                    VTicks = 0;
                    thrustPID.Reset();

                    if (!Active)
                    {
                        Active = true;
                        eventDriver.Schedule(0, Run);
                    }

                    SaveLastCommand(commons, argument);
                }
            }
        }
    }

    private Func<IMyThrust, bool> ParseCruiseFlags()
    {
        if (CruiseFlags == null) return null; // Don't do extra work

        var useIon = CruiseFlags.IndexOf('i') >= 0;
        var useH = CruiseFlags.IndexOf('h') >= 0;
        var useAtm = CruiseFlags.IndexOf('a') >= 0;

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

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        
        ResetIfNotLive(commons, eventDriver);
        if (!Active) return;

        velocimeter.TakeSample(shipControl.ReferencePoint, TimeSpan.FromSeconds((double)VTicks / RunsPerSecond));
        VTicks++;

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            var cruiseDirectionFlipped = Base6Directions.GetFlippedDirection(CruiseDirection);
            // Determine forward vector
            var forward3I = shipControl.Reference.Position + Base6Directions.GetIntVector(shipControl.ShipBlockOrientation.TransformDirection(CruiseDirection));
            var forward = Vector3D.Normalize(shipControl.Reference.CubeGrid.GridIntegerToWorld(forward3I) - shipControl.ReferencePoint);
            
            CurrentSpeed = Vector3D.Dot((Vector3D)velocity, forward);
            var error = TargetSpeed - CurrentSpeed;

            var force = thrustPID.Compute(error);
            //commons.Echo("Force: " + force);

            var thrustControl = shipControl.ThrustControl;
            var collect = ParseCruiseFlags();
            if (Math.Abs(error) < CRUISE_CONTROL_DEAD_ZONE * TargetSpeed)
            {
                // Close enough, just disable both sets of thrusters
                thrustControl.Enable(CruiseDirection, false, collect);
                thrustControl.Enable(cruiseDirectionFlipped, false, collect);
            }
            else if (force > 0.0)
            {
                // Thrust forward
                thrustControl.Enable(CruiseDirection, true, collect);
                thrustControl.SetOverride(CruiseDirection, force * Bias[(int)CruiseDirection], collect);
                thrustControl.Enable(cruiseDirectionFlipped, false, collect);
            }
            else
            {
                thrustControl.Enable(CruiseDirection, false, collect);
                thrustControl.Enable(cruiseDirectionFlipped, true, collect);
                thrustControl.SetOverride(cruiseDirectionFlipped, -force * Bias[(int)cruiseDirectionFlipped], collect);
            }
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }

    public void Display(ZACommons commons)
    {
        if (Active)
        {
            commons.Echo("Cruise control active");
            commons.Echo(string.Format("Set Speed: {0:F1} m/s", TargetSpeed));
            commons.Echo(string.Format("Actual Speed: {0:F1} m/s", CurrentSpeed));
        }
    }

    private void SaveLastCommand(ZACommons commons, string argument)
    {
        commons.SetValue(LastCommandKey, argument);
    }

    private void ResetIfNotLive(ZACommons commons, EventDriver eventDriver)
    {
        if (LivenessCheck != null && !LivenessCheck(commons, eventDriver))
        {
            Reset(commons);
            Active = false;
            SaveLastCommand(commons, null);
        }
    }
}
