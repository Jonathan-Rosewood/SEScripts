//! Solar Alignment
//@ shipcontrol eventdriver solargyrocontroller
private readonly EventDriver eventDriver = new EventDriver();
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "SolarGyroReference");

        solarGyroController.ConditionalInit(commons, eventDriver, SOLAR_ALIGNMENT_DEFAULT_ACTIVE);
    }

    eventDriver.Tick(commons, argAction: () => {
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
