//! VTVL Solo
//@ shipcontrol eventdriver vtvlhelper
private readonly EventDriver eventDriver = new EventDriver();
private readonly VTVLHelper vtvlHelper = new VTVLHelper();
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
                                         shipGroup: SHIP_GROUP,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, VTVLHELPER_REMOTE_GROUP);

        vtvlHelper.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            vtvlHelper.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            vtvlHelper.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
