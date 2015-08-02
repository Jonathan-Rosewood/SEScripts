public class RelaySatellitePowerDrainHandler : BatteryManager.PowerDrainHandler
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

private readonly EventDriver eventDriver = new EventDriver(timerGroup: RELAY_CLOCK_GROUP);
private readonly LaunchController launchController = new LaunchController();
private readonly BatteryManager batteryManager = new BatteryManager(new RelaySatellitePowerDrainHandler());
private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   GyroControl.Yaw,
                                                                                   GyroControl.Pitch
                                                                                   // GyroControl.Roll
);

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        launchController.Init(this, eventDriver);
    }

    ZALibrary.Ship ship = new ZALibrary.Ship(this);
    batteryManager.HandleCommand(this, ship, argument);
    solarGyroController.HandleCommand(this, ship, argument,
                                      shipUp: launchController.ShipUp,
                                      shipForward: launchController.ShipForward);

    if (eventDriver.Tick(this))
    {
        batteryManager.Run(this, ship);
        solarGyroController.Run(this, ship,
                                shipUp: launchController.ShipUp,
                                shipForward: launchController.ShipForward);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}
