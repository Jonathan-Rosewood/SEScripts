//! Solar Alignment
//@ shipcontrol eventdriver solargyrocontroller
private readonly EventDriver eventDriver = new EventDriver(timerName: "SolarGyroClock", timerGroup: "SolarGyroClock");
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

        shipOrientation.SetShipReference(commons, "SolarGyroReference");

        solarGyroController.ConditionalInit(commons, eventDriver, SOLAR_ALIGNMENT_DEFAULT_ACTIVE);
    }

    eventDriver.Tick(commons, preAction: () =>
            {
                solarGyroController.HandleCommand(commons, eventDriver, argument);
            });
}
