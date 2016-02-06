public class MySafeModeHandler : SafeModeHandler
{
    public void SafeMode(ZACommons commons, EventDriver eventDriver)
    {
        // Check after 1 second (let timer block's action take effect)
        eventDriver.Schedule(1.0, (c,ed) =>
                {
                    new EmergencyStop().SafeMode(c, ed);
                });
    }
}

public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME, timerGroup: MINER_CLOCK_GROUP);
public readonly DockingManager dockingManager = new DockingManager();
public readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
public readonly SmartUndock smartUndock = new SmartUndock();
private readonly ZAStorage myStorage = new ZAStorage();
public readonly MinerController minerController = new MinerController();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: SHIP_NAME,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, MINER_REFERENCE_GROUP);

        dockingManager.Init(commons, eventDriver, safeMode,
                            new BatteryMonitor(),
                            new RedundancyManager());
        smartUndock.Init(commons);
    }

    eventDriver.Tick(commons, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument, () =>
                    {
                        dockingManager.ManageShip(commons, eventDriver, false);
                    });
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
    private const double PerturbAmplitude = 0.1;

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
            shipControl.Reset(gyroOverride: true);
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
            shipControl.Reset(gyroOverride: false);
        }
    }

    public void Mine(ZACommons commons, EventDriver eventDriver)
    {
        if (!Mining) return;

        var shipControl = (ShipControlCommons)commons;

        velocimeter.TakeSample(shipControl.ReferencePoint, eventDriver.TimeSinceStart);

        // Determine velocity
        var velocity = velocimeter.GetAverageVelocity();
        if (velocity != null)
        {
            // Take dot product with forward unit vector
            var speed = Vector3D.Dot((Vector3D)velocity, shipControl.ReferenceForward);
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
                thrustControl.Enable(Base6Directions.Direction.Forward, true);
                thrustControl.SetOverride(Base6Directions.Direction.Forward, force);
                thrustControl.Enable(Base6Directions.Direction.Backward, false);
            }
            else
            {
                thrustControl.Enable(Base6Directions.Direction.Forward, false);
                thrustControl.Enable(Base6Directions.Direction.Backward, true);
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
