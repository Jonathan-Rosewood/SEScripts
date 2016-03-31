public class SatelliteLowBatteryHandler : BatteryMonitor.LowBatteryHandler
{
    private const string Message = "HELP! NET POWER LOSS!";

    private string OldAntennaName;

    public void LowBattery(ZACommons commons, EventDriver eventDriver,
                           bool started)
    {
        if (started)
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
        else
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
}

private readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor(new SatelliteLowBatteryHandler());
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DamageControl damageControl = new DamageControl();
private readonly ReactorManager reactorManager = new ReactorManager();
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);

        safeMode.Init(commons, eventDriver);
        batteryMonitor.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
        reactorManager.Init(commons, eventDriver);
        solarGyroController.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () =>
            {
                damageControl.HandleCommand(commons, eventDriver, argument);
                reactorManager.HandleCommand(commons, eventDriver, argument);
                solarGyroController.HandleCommand(commons, eventDriver, argument);
            });
}
