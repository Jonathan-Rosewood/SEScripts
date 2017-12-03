//! Cannon Warship Manager
//@ shipcontrol eventdriver doorautocloser simpleairlock oxygenmanager
//@ redundancy damagecontrol safemode cruisecontrol emergencystop
//@ sequencer speedaction stocker projectoraction customdata firecontrol
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
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DamageControl damageControl = new DamageControl();
private readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly Sequencer sequencer = new Sequencer();
private readonly SpeedAction speedAction = new SpeedAction();
private readonly Stocker stocker = new Stocker();
private readonly ProjectorAction projectorAction = new ProjectorAction();
private readonly FireControl fireControl = new FireControl();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

private bool FirstRun = true;

private bool AutoCloseDoorsEnable, SimpleAirlockEnable, OxygenManagerEnable;

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
        OxygenManagerEnable = customData.GetBool("oxygenManager", OXYGEN_MANAGER_ENABLE);

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "CruiseControlReference");

        if (AutoCloseDoorsEnable) doorAutoCloser.Init(commons, eventDriver);
        if (SimpleAirlockEnable) simpleAirlock.Init(commons, eventDriver);
        if (OxygenManagerEnable) oxygenManager.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
        damageControl.Init(commons, eventDriver);
        safeMode.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        speedAction.Init(commons, eventDriver);
        stocker.Init(commons, eventDriver);
        fireControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            damageControl.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            sequencer.HandleCommand(commons, eventDriver, argument);
            projectorAction.HandleCommand(commons, eventDriver, argument);
            fireControl.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            damageControl.Display(commons);
            cruiseControl.Display(commons);
            sequencer.Display(commons);
            fireControl.Display(commons, eventDriver);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}
