private readonly EventDriver eventDriver = new EventDriver(timerGroup: MINER_CLOCK_GROUP);
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SmartUndock smartUndock = new SmartUndock();

private const uint FramesPerRun = 5;
private const double RunsPerSecond = 60.0 / FramesPerRun;

private readonly Velocimeter velocimeter = new Velocimeter(10);
private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

private const double ThrustKp = 10000.0;
private const double ThrustKi = 100.0;
private const double ThrustKd = 0.0;

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;
private bool Mining = false;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;

        shipOrientation.SetShipReference(commons, MINER_REFERENCE_GROUP);

        eventDriver.Schedule(0.0, DroneController);
    }

    argument = argument.Trim().ToLower();
    if (argument.Length > 0)
    {
        switch (argument)
        {
            case "start":
                commons.GyroControl.EnableOverride(true);
                commons.ThrustControl.Reset();
                commons.ThrustControl.SetOverride(Base6Directions.Direction.Forward); // Get things moving
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
                commons.GyroControl.EnableOverride(false);
                commons.ThrustControl.Reset();
                break;
            default:
                dockingManager.HandleCommand(commons, argument);
                smartUndock.HandleCommand(commons, eventDriver, argument);
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

    var shipControl = (ShipControlCommons)commons;

    var referenceGroup = commons.GetBlockGroupWithName(MINER_REFERENCE_GROUP);
    if (referenceGroup == null)
    {
        throw new Exception("Missing group: " + MINER_REFERENCE_GROUP);
    }
    var reference = referenceGroup.Blocks[0];
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

        var thrustControl = shipControl.ThrustControl;
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
