private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SolarRotorController rotorController = new SolarRotorController();

void Main(string argument)
{
    var ship = new ZALibrary.Ship(this, SHIP_NAME);

    // This really seems like it should be determined once per run
    var isConnected = ship.IsConnectedAnywhere();

    dockingManager.HandleCommand(this, ship, argument);
    dockingManager.Run(this, ship, isConnected);
    safeMode.Run(this, ship, isConnected);
    batteryMonitor.Run(this, ship, isConnected);
    if (MAX_POWER_ENABLED) rotorController.Run(this);

    ZALibrary.KickLoopTimerBlock(this, argument);
}
