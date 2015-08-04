private readonly EventDriver eventDriver = new EventDriver(timerName: ZALIBRARY_LOOP_TIMER_BLOCK_NAME);
private readonly BatteryManager batteryManager = new BatteryManager();
private readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                   // GyroControl.Yaw,
                                                                                   GyroControl.Pitch,
                                                                                   GyroControl.Roll
);
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly BackupMedicalLaunch backupMedicalLaunch = new BackupMedicalLaunch();

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        backupMedicalLaunch.Init(this, eventDriver);
    }

    ZALibrary.Ship ship = new ZALibrary.Ship(this);
    batteryManager.HandleCommand(this, ship, argument);
    solarGyroController.HandleCommand(this, ship, argument,
                                      shipUp: backupMedicalLaunch.ShipUp,
                                      shipForward: backupMedicalLaunch.ShipForward);

    if (eventDriver.Tick(this))
    {
        batteryManager.Run(this, ship);
        solarGyroController.Run(this, ship,
                                shipUp: backupMedicalLaunch.ShipUp,
                                shipForward: backupMedicalLaunch.ShipForward);
        doorAutoCloser.Run(this);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}
