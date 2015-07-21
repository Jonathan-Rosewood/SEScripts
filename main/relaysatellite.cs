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

private readonly LaunchController launchController = new LaunchController();
private readonly BatteryManager batteryManager = new BatteryManager(new RelaySatellitePowerDrainHandler());
private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   GyroControl.Yaw,
                                                                                   GyroControl.Pitch
                                                                                   // GyroControl.Roll
);

private bool Ready = false;

void Main(string argument)
{
    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    if (!Ready)
    {
        Ready = launchController.Run(this, ship);
    }
    else
    {
        batteryManager.Run(this, ship, argument);
        solarGyroController.Run(this, ship, argument);

        ZALibrary.KickLoopTimerBlock(this, argument);
    }
}
