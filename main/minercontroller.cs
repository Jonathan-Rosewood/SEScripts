private readonly EventDriver eventDriver = new EventDriver(timerGroup: MINER_CLOCK_GROUP);
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();

private const uint FramesPerRun = 5;
private const double RunsPerSecond = 60.0 / FramesPerRun;

private readonly Velocimeter velocimeter = new Velocimeter(10);
private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

private bool FirstRun = true;
private bool Mining = false;

private const double ThrustKp = 10000.0;
private const double ThrustKi = 100.0;
private const double ThrustKd = 0.0;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;

        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;

        eventDriver.Schedule(0.0, DroneController);
    }

    argument = argument.Trim().ToLower();
    if (argument.Length > 0)
    {
        Base6Directions.Direction shipUp, shipForward;
        GetReference(out shipUp, out shipForward);
        var gyroControl = GetGyroControl(shipUp: shipUp, shipForward: shipForward);
        var thrustControl = GetThrustControl(shipUp: shipUp, shipForward: shipForward);

        switch (argument)
        {
            case "start":
                gyroControl.EnableOverride(true);
                thrustControl.Reset();
                thrustControl.SetOverride(Base6Directions.Direction.Forward); // Get things moving
                thrustPID.Reset();
                velocimeter.Reset();

                if (!Mining)
                {
                    Mining = true;
                    eventDriver.Schedule(1, Mine);
                }
                break;
            case "stop":
                Mining = false;
                gyroControl.EnableOverride(false);
                thrustControl.Reset();
                break;
            default:
                var ship = new ZALibrary.Ship(this);
                dockingManager.HandleCommand(this, ship, argument);
                break;
        }
    }

    eventDriver.Tick(this);
}

public void DroneController(MyGridProgram program, EventDriver eventDriver)
{
    var ship = new ZALibrary.Ship(this);

    // This really seems like it should be determined once per run
    var isConnected = ship.IsConnectedAnywhere();

    dockingManager.Run(this, ship, isConnected);
    safeMode.Run(this, ship, isConnected);
    batteryMonitor.Run(this, ship, isConnected);

    eventDriver.Schedule(1.0, DroneController);
}

public void Mine(MyGridProgram program, EventDriver eventDriver)
{
    if (!Mining) return;

    Base6Directions.Direction shipUp, shipForward;
    var reference = GetReference(out shipUp, out shipForward);
    velocimeter.TakeSample(reference.GetPosition(), eventDriver.TimeSinceStart);

    // Determine velocity
    var velocity = velocimeter.GetAverageVelocity();
    if (velocity != null)
    {
        // Only absolute velocity (for now)
        // TODO take dot product with forward vector
        var speed = ((Vector3D)velocity).Length();
        var error = TARGET_MINING_SPEED - speed;

        var force = thrustPID.Compute(error);
        // program.Echo(string.Format("Speed: {0:F2} m/s", speed));
        // program.Echo(string.Format("Error: {0:F2}", error));
        // program.Echo(string.Format("Force: {0:F1} N", force));
        program.Echo("Mining");

        var thrustControl = GetThrustControl(shipUp: shipUp, shipForward: shipForward);
        if (force > 0.0)
        {
            // Thrust forward
            thrustControl.SetOverride(Base6Directions.Direction.Forward, (float)force);
            thrustControl.SetOverride(Base6Directions.Direction.Backward, 0.0f);
        }
        else
        {
            thrustControl.SetOverride(Base6Directions.Direction.Forward, 0.0f);
            thrustControl.SetOverride(Base6Directions.Direction.Backward, (float)(-force));
        }
    }

    eventDriver.Schedule(FramesPerRun, Mine);
}

private IMyCubeBlock GetReference(out Base6Directions.Direction shipUp, out Base6Directions.Direction shipForward)
{
    var referenceGroup = ZALibrary.GetBlockGroupWithName(this, MINER_REFERENCE_GROUP);
    if (referenceGroup == null)
    {
        throw new Exception("Missing group: " + MINER_REFERENCE_GROUP);
    }
    var reference = referenceGroup.Blocks[0];

    shipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
    shipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);

    return reference;
}

private GyroControl GetGyroControl(Base6Directions.Direction shipUp, Base6Directions.Direction shipForward)
{
    var gyroControl = new GyroControl();
    gyroControl.Init(this, shipUp: shipUp, shipForward: shipForward);
    return gyroControl;
}

private ThrustControl GetThrustControl(Base6Directions.Direction shipUp, Base6Directions.Direction shipForward)
{
    var thrustControl = new ThrustControl();
    thrustControl.Init(this, shipUp: shipUp, shipForward: shipForward);
    return thrustControl;
}
