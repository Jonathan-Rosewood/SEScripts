private readonly EventDriver eventDriver = new EventDriver(timerGroup: "CruiseControlClock");
private readonly CruiseControl cruiseControl = new CruiseControl();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, "CruiseControlReference");
    }

    cruiseControl.HandleCommand(commons, eventDriver, argument);

    eventDriver.Tick(commons);
}

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
    
    public CruiseControl()
    {
        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 2);
        if (parts.Length != 2) return;
        var command = parts[0];
        argument = parts[1];

        if (command == "cruise")
        {
            if (argument == "stop")
            {
                if (Active) Active = false;
            }
            else
            {
                double desiredSpeed;
                if (double.TryParse(argument, out desiredSpeed))
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
        
        if (!Active)
        {
            shipControl.ThrustControl.Reset();
            return;
        }

        var reference = commons.Me;
        velocimeter.TakeSample(reference.GetPosition(), eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            // Determine forward vector
            var forward3I = reference.Position + Base6Directions.GetIntVector(shipControl.ShipForward);
            var forward = Vector3D.Normalize(reference.CubeGrid.GridIntegerToWorld(forward3I) - reference.GetPosition());
            
            var speed = Vector3D.Dot((Vector3D)velocity, forward);
            var error = TargetSpeed - speed;

            var force = thrustPID.Compute(error);
            commons.Echo("Cruise control active");
            commons.Echo(string.Format("Set Speed: {0:F1} m/s", TargetSpeed));
            commons.Echo(string.Format("Actual Speed: {0:F1} m/s", speed));
            //commons.Echo("Force: " + force);

            var thrustControl = shipControl.ThrustControl;
            if (force > 0.0)
            {
                // Thrust forward
                thrustControl.SetOverridePercent(Base6Directions.Direction.Forward, force);
                thrustControl.SetOverride(Base6Directions.Direction.Backward, 0.0f);
            }
            else
            {
                thrustControl.SetOverride(Base6Directions.Direction.Forward, 0.0f);
                thrustControl.SetOverridePercent(Base6Directions.Direction.Backward, -force);
            }
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
