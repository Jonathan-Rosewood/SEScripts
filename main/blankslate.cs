//! Tabula Rasa
//@ shipcontrol eventdriver
private readonly EventDriver eventDriver = new EventDriver();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);
    }

    eventDriver.Tick(commons, preAction: () => {
        },
        postAction: () => {
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
