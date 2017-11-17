//! Ship Manager
//@ commons eventdriver doorautocloser simpleairlock complexairlock
//@ oxygenmanager airventmanager refinerymanager productionmanager
//@ redundancy dockingaction damagecontrol reactormanager
private readonly EventDriver eventDriver = new EventDriver();
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly SimpleAirlock simpleAirlock = new SimpleAirlock();
private readonly ComplexAirlock complexAirlock = new ComplexAirlock();
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly AirVentManager airVentManager = new AirVentManager();
private readonly RefineryManager refineryManager = new RefineryManager();
private readonly ProductionManager productionManager = new ProductionManager();
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DockingAction dockingAction = new DockingAction();
private readonly DamageControl damageControl = new DamageControl();
private readonly ReactorManager reactorManager = new ReactorManager();
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

        // Door management
        if (AUTO_CLOSE_DOORS_ENABLE) doorAutoCloser.Init(commons, eventDriver);
        if (SIMPLE_AIRLOCK_ENABLE) simpleAirlock.Init(commons, eventDriver);
        if (COMPLEX_AIRLOCK_ENABLE) complexAirlock.Init(commons, eventDriver);

        // Systems management
        if (OXYGEN_MANAGER_ENABLE) oxygenManager.Init(commons, eventDriver);
        if (AIR_VENT_MANAGER_ENABLE) airVentManager.Init(commons, eventDriver);
        if (REFINERY_MANAGER_ENABLE) refineryManager.Init(commons, eventDriver);
        if (PRODUCTION_MANAGER_ENABLE) productionManager.Init(commons, eventDriver);

        // Misc
        if (REDUNDANCY_MANAGER_ENABLE) redundancyManager.Init(commons, eventDriver);
        if (DOCKING_ACTION_ENABLE) dockingAction.Init(commons, eventDriver);
        if (DAMAGE_CONTROL_ENABLE) damageControl.Init(commons, eventDriver);
        if (REACTOR_MANAGER_ENABLE) reactorManager.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            // Handle commands
            if (COMPLEX_AIRLOCK_ENABLE) complexAirlock.HandleCommand(commons, eventDriver, argument);
            if (PRODUCTION_MANAGER_ENABLE) productionManager.HandleCommand(commons, eventDriver, argument);
            if (DAMAGE_CONTROL_ENABLE) damageControl.HandleCommand(commons, eventDriver, argument);
            if (REACTOR_MANAGER_ENABLE) reactorManager.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            if (PRODUCTION_MANAGER_ENABLE) productionManager.Display(commons);
            if (DAMAGE_CONTROL_ENABLE) damageControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
