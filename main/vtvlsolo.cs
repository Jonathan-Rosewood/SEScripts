//! VTVL Solo
//@ shipcontrol eventdriver vtvlhelper customdata
private readonly EventDriver eventDriver = new EventDriver();
private readonly VTVLHelper vtvlHelper = new VTVLHelper();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

private bool FirstRun = true;

private string VTVLHelperRemoteGroup;

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

        customData.Parse(Me);
        VTVLHelperRemoteGroup = customData.GetString("referenceGroup", VTVLHELPER_REMOTE_GROUP);

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, VTVLHelperRemoteGroup);

        vtvlHelper.Init(commons, eventDriver, customData);
    }

    eventDriver.Tick(commons, argAction: () => {
            vtvlHelper.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            vtvlHelper.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
