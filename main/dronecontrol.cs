private readonly EventDriver eventDriver = new EventDriver(timerName: ZALIBRARY_LOOP_TIMER_BLOCK_NAME);
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SolarRotorController rotorController = new SolarRotorController();

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    var ship = new ZALibrary.Ship(this, SHIP_NAME);
    dockingManager.HandleCommand(this, ship, argument);

    if (eventDriver.Tick(this))
    {
        // This really seems like it should be determined once per run
        var isConnected = ship.IsConnectedAnywhere();

        dockingManager.Run(this, ship, isConnected);
        safeMode.Run(this, ship, isConnected);
        batteryMonitor.Run(this, ship, isConnected);
        if (MAX_POWER_ENABLED) rotorController.Run(this);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}
