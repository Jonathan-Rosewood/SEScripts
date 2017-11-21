//! Transponder Test
//@ shipcontrol eventdriver transponder
private readonly EventDriver eventDriver = new EventDriver();
private readonly Transponder transponder = new Transponder();
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

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);

        transponder.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            transponder.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            var infos = transponder.GetTransponderInfos();
            commons.Echo(string.Format("Count: {0}", infos.Count));
            foreach (var kv in infos)
            {
                commons.Echo(string.Format("{0}: {1} {2}", kv.Value.ID, kv.Value.Position, kv.Value.Orientation));
            }
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
