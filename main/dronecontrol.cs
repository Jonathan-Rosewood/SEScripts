public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly DockingManager dockingManager = new DockingManager();
public readonly SafeMode safeMode = new SafeMode();
public readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
public readonly SolarRotorController rotorController = new SolarRotorController();
private readonly SmartUndock smartUndock = new SmartUndock();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: SHIP_NAME);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, "Autopilot Reference");

        eventDriver.Schedule(0.0);
    }

    dockingManager.HandleCommand(commons, argument);
    smartUndock.HandleCommand(commons, eventDriver, argument);

    eventDriver.Tick(commons, () =>
            {
                // This really seems like it should be determined once per run
                var isConnected = ZACommons.IsConnectedAnywhere(commons.Blocks);

                dockingManager.Run(commons, isConnected);
                safeMode.Run(commons, isConnected);
                batteryMonitor.Run(commons, isConnected);
                if (MAX_POWER_ENABLED) rotorController.Run(commons);

                eventDriver.Schedule(1.0);
            });
}
