//! Cruise Control
//@ shipcontrol eventdriver cruisecontrol
private readonly EventDriver eventDriver = new EventDriver();
private readonly CruiseControl cruiseControl = new CruiseControl();
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
                                         shipGroup: "CruiseControlGroup",
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "CruiseControlReference");

        cruiseControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            cruiseControl.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            cruiseControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
