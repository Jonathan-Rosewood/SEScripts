private readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SolarRotorController rotorController = new SolarRotorController();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this, shipGroup: SHIP_NAME);

    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    dockingManager.HandleCommand(commons, argument);

    if (eventDriver.Tick(commons))
    {
        // This really seems like it should be determined once per run
        var isConnected = ZACommons.IsConnectedAnywhere(commons.Blocks);

        dockingManager.Run(commons, isConnected);
        safeMode.Run(commons, isConnected);
        batteryMonitor.Run(commons, isConnected);
        if (MAX_POWER_ENABLED) rotorController.Run(commons);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(commons);
    }
}
