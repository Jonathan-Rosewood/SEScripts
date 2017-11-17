//! VTVL Solo
//@ shipcontrol eventdriver vtvlhelper
private readonly EventDriver eventDriver = new EventDriver();
private readonly VTVLHelper vtvlHelper = new VTVLHelper();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
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
