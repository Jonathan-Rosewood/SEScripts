//! Survival Ship Manager
//@ shipcontrol eventdriver doorautocloser simpleairlock complexairlock
//@ oxygenmanager airventmanager refinerymanager productionmanager
//@ redundancy dockingaction damagecontrol reactormanager
//@ safemode cruisecontrol solargyrocontroller emergencystop customdata
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
private readonly ZACustomData customData = new ZACustomData();

private bool FirstRun = true;

private bool AutoCloseDoorsEnable, SimpleAirlockEnable, ComplexAirlockEnable;
private bool OxygenManagerEnable, AirVentManagerEnable, RefineryManagerEnable;
private bool ProductionManagerEnable, RedundancyManagerEnable;
private bool DockingActionEnable, DamageControlEnable, ReactorManagerEnable;

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
        AutoCloseDoorsEnable = customData.GetBool("autoCloseDoors", AUTO_CLOSE_DOORS_ENABLE);
        SimpleAirlockEnable = customData.GetBool("simpleAirlock", SIMPLE_AIRLOCK_ENABLE);
        ComplexAirlockEnable = customData.GetBool("complexAirlock", COMPLEX_AIRLOCK_ENABLE);
        OxygenManagerEnable = customData.GetBool("oxygenManager", OXYGEN_MANAGER_ENABLE);
        AirVentManagerEnable = customData.GetBool("airVentManager", AIR_VENT_MANAGER_ENABLE);
        RefineryManagerEnable = customData.GetBool("refineryManager", REFINERY_MANAGER_ENABLE);
        ProductionManagerEnable = customData.GetBool("productionManager", PRODUCTION_MANAGER_ENABLE);
        RedundancyManagerEnable = customData.GetBool("redundancyManager", REDUNDANCY_MANAGER_ENABLE);
        DockingActionEnable = customData.GetBool("dockingAction", DOCKING_ACTION_ENABLE);
        DamageControlEnable = customData.GetBool("damageControl", DAMAGE_CONTROL_ENABLE);
        ReactorManagerEnable = customData.GetBool("reactorManager", REACTOR_MANAGER_ENABLE);

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "CruiseControlReference");

        // Door management
        if (AutoCloseDoorsEnable) doorAutoCloser.Init(commons, eventDriver);
        if (SimpleAirlockEnable) simpleAirlock.Init(commons, eventDriver);
        if (ComplexAirlockEnable) complexAirlock.Init(commons, eventDriver);

        // Systems management
        if (OxygenManagerEnable) oxygenManager.Init(commons, eventDriver);
        if (AirVentManagerEnable) airVentManager.Init(commons, eventDriver);
        if (RefineryManagerEnable) refineryManager.Init(commons, eventDriver);
        if (ProductionManagerEnable) productionManager.Init(commons, eventDriver);

        // Misc
        if (RedundancyManagerEnable) redundancyManager.Init(commons, eventDriver);
        if (DockingActionEnable) dockingAction.Init(commons, eventDriver);
        if (DamageControlEnable) damageControl.Init(commons, eventDriver);
        if (ReactorManagerEnable) reactorManager.Init(commons, eventDriver);

        safeMode.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        solarGyroController.ConditionalInit(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            // Handle commands
            if (ComplexAirlockEnable) complexAirlock.HandleCommand(commons, eventDriver, argument);
            if (ProductionManagerEnable) productionManager.HandleCommand(commons, eventDriver, argument);
            if (DamageControlEnable) damageControl.HandleCommand(commons, eventDriver, argument);
            if (ReactorManagerEnable) reactorManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
            if (ProductionManagerEnable) productionManager.Display(commons);
            if (DamageControlEnable) damageControl.Display(commons);
            cruiseControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}


bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}
