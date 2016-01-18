public class CruiseControl
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;

    private bool Active = false;
    private double TargetSpeed;
    private Base6Directions.Direction CruiseDirection;
    
    public CruiseControl()
    {
        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
    }

    private void Reset(ZACommons commons)
    {
        var thrustControl = ((ShipControlCommons)commons).ThrustControl;
        thrustControl.Enable(true);
        thrustControl.Reset();
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 3);
        if (parts.Length < 2) return;
        var command = parts[0];
        var speed = parts[1];

        CruiseDirection = Base6Directions.Direction.Forward;
        if (parts.Length == 3)
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
                    {
                        CruiseDirection = Base6Directions.Direction.Backward;
                        break;
                    }

                case "left":
                    {
                        CruiseDirection = Base6Directions.Direction.Left;
                        break;
                    }

                case "right":
                    {
                        CruiseDirection = Base6Directions.Direction.Right;
                        break;
                    }

                case "up":
                    {
                        CruiseDirection = Base6Directions.Direction.Up;
                        break;
                    }

                case "down":
                    {
                        CruiseDirection = Base6Directions.Direction.Down;
                        break;
                    }
            }
        }

        if (command == "cruise")
        {
            if (speed == "stop")
            {
                Reset(commons);
                Active = false;
            }
            else
            {
                double desiredSpeed;
                if (double.TryParse(speed, out desiredSpeed))
                {
                    TargetSpeed = Math.Max(desiredSpeed, 0.0);

                    velocimeter.Reset();
                    thrustPID.Reset();

                    if (!Active)
                    {
                        Active = true;
                        eventDriver.Schedule(0, Run);
                    }
                }
            }
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        
        if (!Active) return;

        var reference = commons.Me;
        velocimeter.TakeSample(reference.GetPosition(), eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            var cruiseDirectionFlipped = Base6Directions.GetFlippedDirection(CruiseDirection);
            // Determine forward vector
            var forward3I = reference.Position + Base6Directions.GetIntVector(shipControl.ShipBlockOrientation.TransformDirectionInverse(CruiseDirection));
            var forward = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(forward3I) - reference.GetPosition());
            
            var speed = Vector3D.Dot((Vector3D)velocity, forward);
            var error = TargetSpeed - speed;

            var force = thrustPID.Compute(error);
            commons.Echo("Cruise control active");
            commons.Echo(string.Format("Set Speed: {0:F1} m/s", TargetSpeed));
            commons.Echo(string.Format("Actual Speed: {0:F1} m/s", speed));
            //commons.Echo("Force: " + force);

            var thrustControl = shipControl.ThrustControl;
            if (Math.Abs(error) < CRUISE_CONTROL_DEAD_ZONE * TargetSpeed)
            {
                // Close enough, just disable both sets of thrusters
                thrustControl.Enable(CruiseDirection, false);
                thrustControl.Enable(cruiseDirectionFlipped, false);
            }
            else if (force > 0.0)
            {
                // Thrust forward
                thrustControl.Enable(CruiseDirection, true);
                thrustControl.SetOverride(CruiseDirection, force);
                thrustControl.Enable(cruiseDirectionFlipped, false);
            }
            else
            {
                thrustControl.Enable(CruiseDirection, false);
                thrustControl.Enable(cruiseDirectionFlipped, true);
                thrustControl.SetOverride(cruiseDirectionFlipped, -force);
            }
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
