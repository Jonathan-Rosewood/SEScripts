public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly BatteryManager batteryManager = new BatteryManager();
public readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                  // GyroControl.Yaw,
                                                                                  GyroControl.Pitch,
                                                                                  GyroControl.Roll
                                                                                  );
public readonly SafeMode safeMode = new SafeMode();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);

        eventDriver.Schedule(0.0);
    }

    batteryManager.HandleCommand(commons, argument);
    solarGyroController.HandleCommand(commons, argument);

    eventDriver.Tick(commons, () =>
            {
                batteryManager.Run(commons);
                solarGyroController.Run(commons);
                safeMode.Run(commons, eventDriver, false);

                eventDriver.Schedule(1.0);
            });
}
