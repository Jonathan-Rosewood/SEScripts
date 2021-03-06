//! Backup Medical Controller
//@ shipcontrol eventdriver backupmedicallaunch doorautocloser batterymonitor
//@ solargyrocontroller
public class BackupMedicalLowBatteryHandler : BatteryMonitor.LowBatteryHandler
{
    public void LowBattery(ZACommons commons, EventDriver eventDriver,
                           bool started)
    {
        // Enable/disable reactor according to state
        ZACommons.EnableBlocks(ZACommons.GetBlocksOfType<IMyReactor>(commons.Blocks), started);
    }
}

private readonly EventDriver eventDriver = new EventDriver();
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

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference<IMyRemoteControl>(commons.Blocks);

        // TODO Need explicit launch command
        backupMedicalLaunch.Init(commons, eventDriver, (c,ed) =>
                {
                    doorAutoCloser.Init(c, ed);
                    batteryMonitor.Init(c, ed);
                    solarGyroController.Init(c, ed);
                });
    }

    eventDriver.Tick(commons, argAction: () => {
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
        });
}
