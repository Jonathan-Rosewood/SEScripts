private readonly BatteryManager batteryManager = new BatteryManager();
private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   // GyroControl.Yaw,
                                                                                   GyroControl.Pitch,
                                                                                   GyroControl.Roll
                                                                                   );
private readonly SafeMode safeMode = new SafeMode();

void Main(string argument)
{
    Base6Directions.Direction shipUp, shipForward;

    // Look for our ship controllers
    var controllers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers, block => block.CubeGrid == Me.CubeGrid);
    // The remote and cockpit should be oriented the same, so it doesn't matter which one we pick
    var reference = controllers.Count > 0 ? controllers[0] : null;
    if (reference != null)
    {
        shipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
        shipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);
    }
    else
    {
        // Default to grid up/forward
        shipUp = Base6Directions.Direction.Up;
        shipForward = Base6Directions.Direction.Forward;
    }

    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    batteryManager.HandleCommand(this, ship, argument);
    solarGyroController.HandleCommand(this, ship, argument,
                                      shipUp: shipUp,
                                      shipForward: shipForward);

    batteryManager.Run(this, ship);
    solarGyroController.Run(this, ship,
                            shipUp: shipUp,
                            shipForward: shipForward);
    safeMode.Run(this, ship, false);

    ZALibrary.KickLoopTimerBlock(this, argument);
}
