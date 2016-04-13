//! Cruise Control
//@ shipcontrol eventdriver cruisecontrol
private readonly EventDriver eventDriver = new EventDriver(timerGroup: "CruiseControlClock");
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: "CruiseControlGroup",
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "CruiseControlReference");

        cruiseControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            cruiseControl.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            cruiseControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
