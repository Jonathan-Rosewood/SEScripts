//! Tabula Rasa
//@ shipcontrol eventdriver customdata
private readonly EventDriver eventDriver = new EventDriver();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

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

        customData.Parse(Me);

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);
    }

    eventDriver.Tick(commons, argAction: () => {
        },
        postAction: () => {
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
