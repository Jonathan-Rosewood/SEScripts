public class SatellitePowerDrainHandler : BatteryManager.PowerDrainHandler
{
    private const string Message = "HELP! NET POWER LOSS!";

    private string OldAntennaName;

    public void PowerDrainStarted(ZALibrary.Ship ship)
    {
        // Just change the name of the first active antenna
        for (var e = ship.GetBlocksOfType<IMyRadioAntenna>().GetEnumerator(); e.MoveNext();)
        {
            var antenna = e.Current;

            if (antenna.IsFunctional && antenna.IsWorking)
            {
                OldAntennaName = antenna.CustomName;
                antenna.SetCustomName(Message);
                break;
            }
        }
    }

    public void PowerDrainEnded(ZALibrary.Ship ship)
    {
        // Scan for the antenna with the message, change it back
        for (var e = ship.GetBlocksOfType<IMyRadioAntenna>().GetEnumerator(); e.MoveNext();)
        {
            var antenna = e.Current;

            if (antenna.CustomName == Message)
            {
                antenna.SetCustomName(OldAntennaName);
                break;
            }
        }
    }
}

private readonly EventDriver eventDriver = new EventDriver(timerName: ZALIBRARY_LOOP_TIMER_BLOCK_NAME);
private readonly BatteryManager batteryManager = new BatteryManager(new SatellitePowerDrainHandler());
private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   // GyroControl.Yaw,
                                                                                   GyroControl.Pitch,
                                                                                   GyroControl.Roll
);

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    Base6Directions.Direction shipUp, shipForward;

    // Look for our ship controllers
    var controllers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers, block => block.CubeGrid == Me.CubeGrid);
    // Pick one. Assume they're all oriented the same.
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

    if (eventDriver.Tick(this))
    {
        batteryManager.Run(this, ship);
        solarGyroController.Run(this, ship,
                                shipUp: shipUp,
                                shipForward: shipForward);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}
