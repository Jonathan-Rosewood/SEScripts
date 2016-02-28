public class BackupMedicalLowBatteryHandler : BatteryMonitor.LowBatteryHandler
{
    public void LowBattery(ZACommons commons, EventDriver eventDriver,
                           bool started)
    {
        // Enable/disable reactor according to state
        ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyReactor>(commons.Blocks), started);
    }
}

private readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly BackupMedicalLaunch backupMedicalLaunch = new BackupMedicalLaunch();
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor(new BackupMedicalLowBatteryHandler());
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

        shipOrientation.SetShipReference<IMyRemoteControl>(commons.Blocks);

        backupMedicalLaunch.Init(commons, eventDriver, (c,ed) =>
                {
                    doorAutoCloser.Init(c, ed);
                    batteryMonitor.Init(c, ed);
                    solarGyroController.Init(c, ed);
                });
    }

    eventDriver.Tick(commons, preAction: () =>
            {
                solarGyroController.HandleCommand(commons, eventDriver, argument);
            });
}
