//! Smart Undock
//@ shipcontrol eventdriver smartundock
public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME, timerGroup: "SmartUndockClock");
public readonly SmartUndock smartUndock = new SmartUndock();
public readonly ZAStorage myStorage = new ZAStorage();

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

        shipOrientation.SetShipReference(commons, "SmartUndockReference");

        smartUndock.Init(commons);
    }

    eventDriver.Tick(commons, preAction: () => {
            smartUndock.HandleCommand(commons, eventDriver, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
