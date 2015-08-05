public class RelaySatellitePowerDrainHandler : BatteryManager.PowerDrainHandler
{
    private const string Message = "HELP! NET POWER LOSS!";

    private string OldAntennaName;

    public void PowerDrainStarted(ZACommons commons)
    {
        // Just change the name of the first active antenna
        for (var e = ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks).GetEnumerator(); e.MoveNext();)
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

    public void PowerDrainEnded(ZACommons commons)
    {
        // Scan for the antenna with the message, change it back
        for (var e = ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks).GetEnumerator(); e.MoveNext();)
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

public readonly EventDriver eventDriver = new EventDriver(timerGroup: RELAY_CLOCK_GROUP);
public readonly LaunchController launchController = new LaunchController();
public readonly BatteryManager batteryManager = new BatteryManager(new RelaySatellitePowerDrainHandler());
public readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                  GyroControl.Yaw,
                                                                                  GyroControl.Pitch
                                                                                  // GyroControl.Roll
);

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, LaunchController.REMOTE_GROUP);

        launchController.Init(commons, eventDriver);
    }

    batteryManager.HandleCommand(commons, argument);
    solarGyroController.HandleCommand(commons, argument);

    eventDriver.Tick(commons, () =>
            {
                batteryManager.Run(commons);
                solarGyroController.Run(commons);

                eventDriver.Schedule(1.0);
            });
}
