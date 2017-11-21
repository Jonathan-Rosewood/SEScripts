//! Smart Undock
//@ shipcontrol eventdriver smartundock
public readonly EventDriver eventDriver = new EventDriver();
public readonly SmartUndock smartUndock = new SmartUndock();
public readonly ZAStorage myStorage = new ZAStorage();

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

        shipOrientation.SetShipReference(commons, "SmartUndockReference");

        smartUndock.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            smartUndock.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            smartUndock.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
