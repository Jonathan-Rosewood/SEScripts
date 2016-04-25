//! Solar Alignment
//@ shipcontrol eventdriver solargyrocontroller
private readonly EventDriver eventDriver = new EventDriver(timerName: "SolarGyroClock", timerGroup: "SolarGyroClock");
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "SolarGyroReference");

        solarGyroController.ConditionalInit(commons, eventDriver, SOLAR_ALIGNMENT_DEFAULT_ACTIVE);
    }

    eventDriver.Tick(commons, preAction: () => {
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
