private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SolarRotorController rotorController = new SolarRotorController();

void Main(string argument)
{
    var ship = new ZALibrary.Ship(this, SHIP_NAME);

    // This really seems like it should be determined once per run
    var isConnected = ship.IsConnectedAnywhere();

    dockingManager.Run(this, ship, argument, isConnected);
    safeMode.Run(this, ship, isConnected);
    batteryMonitor.Run(this, ship, isConnected);
    if (MAX_POWER_ENABLED) rotorController.Run(this);
}
