//! Solar Max Power
//@ commons eventdriver solarrotorcontroller
private readonly EventDriver eventDriver = new EventDriver();
private readonly SolarRotorController rotorController = new SolarRotorController();
private readonly ZAStorage myStorage = new ZAStorage();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this,
                                storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        rotorController.ConditionalInit(commons, eventDriver,
                                        defaultActive: true);
    }

    eventDriver.Tick(commons, preAction: () => {
            rotorController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            rotorController.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
