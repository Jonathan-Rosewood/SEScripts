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
    var commons = new ZACommons(this);

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
        GetReference(commons, out shipUp, out shipForward);
        var gyroControl = GetGyroControl(commons, shipUp: shipUp, shipForward: shipForward);
        var thrustControl = GetThrustControl(commons, shipUp: shipUp, shipForward: shipForward);

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
                    eventDriver.Schedule(0, Mine);
                }
                break;
            case "stop":
                Mining = false;
                gyroControl.EnableOverride(false);
                thrustControl.Reset();
                break;
            default:
                dockingManager.HandleCommand(commons, argument);
                break;
        }
    }

    eventDriver.Tick(commons);
}

public void DroneController(ZACommons commons, EventDriver eventDriver)
{
    // This really seems like it should be determined once per run
    var isConnected = ZACommons.IsConnectedAnywhere(commons.Blocks);

    dockingManager.Run(commons, isConnected);
    safeMode.Run(commons, isConnected);
    batteryMonitor.Run(commons, isConnected);

    eventDriver.Schedule(1.0, DroneController);
}

public void Mine(ZACommons commons, EventDriver eventDriver)
{
    if (!Mining) return;

    Base6Directions.Direction shipUp, shipForward;
    var reference = GetReference(commons, out shipUp, out shipForward);
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
        // commons.Echo(string.Format("Speed: {0:F2} m/s", speed));
        // commons.Echo(string.Format("Error: {0:F2}", error));
        // commons.Echo(string.Format("Force: {0:F1} N", force));
        commons.Echo("Mining");

        var thrustControl = GetThrustControl(commons, shipUp: shipUp, shipForward: shipForward);
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

private IMyCubeBlock GetReference(ZACommons commons, out Base6Directions.Direction shipUp, out Base6Directions.Direction shipForward)
{
    var referenceGroup = commons.GetBlockGroupWithName(MINER_REFERENCE_GROUP);
    if (referenceGroup == null)
    {
        throw new Exception("Missing group: " + MINER_REFERENCE_GROUP);
    }
    var reference = referenceGroup.Blocks[0];

    shipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
    shipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);

    return reference;
}

private GyroControl GetGyroControl(ZACommons commons, Base6Directions.Direction shipUp, Base6Directions.Direction shipForward)
{
    var gyroControl = new GyroControl();
    gyroControl.Init(commons.Blocks, shipUp: shipUp, shipForward: shipForward);
    return gyroControl;
}

private ThrustControl GetThrustControl(ZACommons commons, Base6Directions.Direction shipUp, Base6Directions.Direction shipForward)
{
    var thrustControl = new ThrustControl();
    thrustControl.Init(commons.Blocks, shipUp: shipUp, shipForward: shipForward);
    return thrustControl;
}
