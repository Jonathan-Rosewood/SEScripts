public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly BatteryManager batteryManager = new BatteryManager();
public readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                  // GyroControl.Yaw,
                                                                                  GyroControl.Pitch,
                                                                                  GyroControl.Roll
);
public readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
public readonly BackupMedicalLaunch backupMedicalLaunch = new BackupMedicalLaunch();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        backupMedicalLaunch.Init(commons, eventDriver);
    }

    batteryManager.HandleCommand(commons, argument);
    solarGyroController.HandleCommand(commons, argument,
                                      shipUp: backupMedicalLaunch.ShipUp,
                                      shipForward: backupMedicalLaunch.ShipForward);

    eventDriver.Tick(commons, () =>
            {
                batteryManager.Run(commons);
                solarGyroController.Run(commons,
                                        shipUp: backupMedicalLaunch.ShipUp,
                                        shipForward: backupMedicalLaunch.ShipForward);
                doorAutoCloser.Run(commons);

                eventDriver.Schedule(1.0);
            });
}
