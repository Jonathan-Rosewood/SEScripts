public readonly EventDriver eventDriver = new EventDriver(timerGroup: MINER_CLOCK_GROUP);
public readonly DockingManager dockingManager = new DockingManager();
public readonly SafeMode safeMode = new SafeMode();
public readonly SmartUndock smartUndock = new SmartUndock();
public readonly MinerController minerController = new MinerController();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, MINER_REFERENCE_GROUP);

        smartUndock.Init(commons);

        eventDriver.Schedule(0.0);
    }

    eventDriver.Tick(commons, mainAction: () => {
            // This really seems like it should be determined once per run
            var isConnected = ZACommons.IsConnectedAnywhere(commons.Blocks);

            safeMode.Run(commons, eventDriver, isConnected);

            eventDriver.Schedule(1.0);
        }, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument);
            minerController.HandleCommand(commons, eventDriver, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

public class MinerController
{
    private const uint FramesPerRun = 2;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Velocimeter velocimeter = new Velocimeter(30);
    private readonly PIDController thrustPID = new PIDController(1.0 / RunsPerSecond);

    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;

    private const double PerturbTimeScale = 10.0;
    private const double PerturbAmplitude = 0.05;

    private bool Mining = false;

    public MinerController()
    {
        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        var shipControl = (ShipControlCommons)commons;

        argument = argument.Trim().ToLower();
        if (argument == "start")
        {
            var gyroControl = shipControl.GyroControl;
            gyroControl.Reset();
            gyroControl.EnableOverride(true);
            shipControl.ThrustControl.Reset();
            velocimeter.Reset();
            thrustPID.Reset();

            if (!Mining)
            {
                Mining = true;
                eventDriver.Schedule(0, Mine);
            }
        }
        else if (argument == "stop")
        {
            Mining = false;
            shipControl.GyroControl.EnableOverride(false);
            shipControl.ThrustControl.Reset();
        }
    }

    public void Mine(ZACommons commons, EventDriver eventDriver)
    {
        if (!Mining) return;

        var shipControl = (ShipControlCommons)commons;

        var reference = commons.Me;
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
                thrustControl.SetOverride(Base6Directions.Direction.Forward, force);
                thrustControl.SetOverride(Base6Directions.Direction.Backward, false);
            }
            else
            {
                thrustControl.SetOverride(Base6Directions.Direction.Forward, false);
                thrustControl.SetOverride(Base6Directions.Direction.Backward, -force);
            }
        }

        // Perturb yaw/pitch
        var gyroControl = shipControl.GyroControl;
        gyroControl.SetAxisVelocity(GyroControl.Yaw, (float)(Math.Cos(PerturbTimeScale * eventDriver.TimeSinceStart.TotalSeconds) * PerturbAmplitude));
        gyroControl.SetAxisVelocity(GyroControl.Pitch, (float)(Math.Sin(PerturbTimeScale * eventDriver.TimeSinceStart.TotalSeconds) * PerturbAmplitude));
        
        eventDriver.Schedule(FramesPerRun, Mine);
    }
}
