private readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly DamageControl damageControl = new DamageControl();
private readonly ZAStorage myStorage = new ZAStorage();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this,
                                shipGroup: SHIP_GROUP,
                                storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        dockingManager.Init(commons, eventDriver, safeMode,
                            new BatteryMonitor(),
                            new RedundancyManager());
    }

    eventDriver.Tick(commons, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
