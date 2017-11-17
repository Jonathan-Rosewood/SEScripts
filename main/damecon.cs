//! Damage Control
//@ commons eventdriver damagecontrol
private readonly EventDriver eventDriver = new EventDriver();
private readonly DamageControl damageControl = new DamageControl();
private readonly ZAStorage myStorage = new ZAStorage();

private bool FirstRun = true;

void Main(string argument, UpdateType updateType)
{
    var commons = new ZACommons(this, updateType,
                                storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        damageControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            damageControl.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            damageControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
