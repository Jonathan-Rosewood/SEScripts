public class MyEmergencyStopHandler : SafeMode.EmergencyStopHandler
{
    public void EmergencyStop(ZACommons commons)
    {
        SafetyStop.ThrusterCheck(commons);
    }
}

public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly DockingManager dockingManager = new DockingManager();
public readonly SafeMode safeMode = new SafeMode(new MyEmergencyStopHandler());
public readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
public readonly SolarRotorController rotorController = new SolarRotorController();
public readonly SmartUndock smartUndock = new SmartUndock();
public readonly ZAStorage myStorage = new ZAStorage();

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

        shipOrientation.SetShipReference(commons, "Autopilot Reference");

        smartUndock.Init(commons);

        eventDriver.Schedule(0.0);
    }

    eventDriver.Tick(commons, mainAction: () => {
            // This really seems like it should be determined once per run
            var isConnected = ZACommons.IsConnectedAnywhere(commons.Blocks);

            dockingManager.Run(commons, isConnected);
            safeMode.Run(commons, isConnected);
            batteryMonitor.Run(commons, isConnected);
            if (MAX_POWER_ENABLED) rotorController.Run(commons);

            eventDriver.Schedule(1.0);
        }, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
