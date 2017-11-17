//! Survival Ship Manager
//@ shipcontrol eventdriver doorautocloser simpleairlock complexairlock
//@ oxygenmanager airventmanager refinerymanager productionmanager
//@ timerkicker redundancy dockingaction damagecontrol reactormanager
//@ safemode cruisecontrol solargyrocontroller emergencystop
public class MySafeModeHandler : SafeModeHandler
{
    public void SafeMode(ZACommons commons, EventDriver eventDriver)
    {
        // Check after 1 second (let timer block's action take effect)
        eventDriver.Schedule(1.0, (c,ed) =>
                {
                    new EmergencyStop().SafeMode(c, ed);
                });
    }
}

private readonly EventDriver eventDriver = new EventDriver();
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly SimpleAirlock simpleAirlock = new SimpleAirlock();
private readonly ComplexAirlock complexAirlock = new ComplexAirlock();
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly AirVentManager airVentManager = new AirVentManager();
private readonly RefineryManager refineryManager = new RefineryManager();
private readonly ProductionManager productionManager = new ProductionManager();
private readonly TimerKicker timerKicker = new TimerKicker();
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DockingAction dockingAction = new DockingAction();
private readonly DamageControl damageControl = new DamageControl();
private readonly ReactorManager reactorManager = new ReactorManager();
private readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "CruiseControlReference");

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
        if (TIMER_KICKER_ENABLE) timerKicker.Init(commons, eventDriver);
        if (REDUNDANCY_MANAGER_ENABLE) redundancyManager.Init(commons, eventDriver);
        if (DOCKING_ACTION_ENABLE) dockingAction.Init(commons, eventDriver);
        if (DAMAGE_CONTROL_ENABLE) damageControl.Init(commons, eventDriver);
        if (REACTOR_MANAGER_ENABLE) reactorManager.Init(commons, eventDriver);

        safeMode.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        solarGyroController.ConditionalInit(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            // Handle commands
            if (COMPLEX_AIRLOCK_ENABLE) complexAirlock.HandleCommand(commons, eventDriver, argument);
            if (PRODUCTION_MANAGER_ENABLE) productionManager.HandleCommand(commons, eventDriver, argument);
            if (DAMAGE_CONTROL_ENABLE) damageControl.HandleCommand(commons, eventDriver, argument);
            if (REACTOR_MANAGER_ENABLE) reactorManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
            if (PRODUCTION_MANAGER_ENABLE) productionManager.Display(commons);
            if (DAMAGE_CONTROL_ENABLE) damageControl.Display(commons);
            cruiseControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}


bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}
