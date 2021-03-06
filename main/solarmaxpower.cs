//! Solar Max Power
//@ commons eventdriver solarrotorcontroller
private readonly EventDriver eventDriver = new EventDriver();
private readonly SolarRotorController rotorController = new SolarRotorController();
private readonly ZAStorage myStorage = new ZAStorage();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ZACommons(this, updateType,
                                storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        rotorController.ConditionalInit(commons, eventDriver,
                                        defaultActive: true);
    }

    eventDriver.Tick(commons, argAction: () => {
            rotorController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            rotorController.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
