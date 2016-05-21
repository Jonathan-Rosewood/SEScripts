//! Warship Manager
//@ shipcontrol eventdriver doorautocloser simpleairlock oxygenmanager
//@ redundancy damagecontrol safemode cruisecontrol emergencystop
//@ sequencer speedaction combatranger stocker projectoraction
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

private readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly SimpleAirlock simpleAirlock = new SimpleAirlock();
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DamageControl damageControl = new DamageControl();
private readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly Sequencer sequencer = new Sequencer();
private readonly SpeedAction speedAction = new SpeedAction();
private readonly CombatRanger combatRanger = new CombatRanger();
private readonly Stocker stocker = new Stocker();
private readonly ProjectorAction projectorAction = new ProjectorAction();
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

        doorAutoCloser.Init(commons, eventDriver);
        simpleAirlock.Init(commons, eventDriver);
        oxygenManager.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
        damageControl.Init(commons, eventDriver);
        safeMode.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        speedAction.Init(commons, eventDriver);
        stocker.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            damageControl.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            sequencer.HandleCommand(commons, eventDriver, argument);
            combatRanger.HandleCommand(commons, argument);
            projectorAction.HandleCommand(commons, eventDriver, argument);
            HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            damageControl.Display(commons);
            cruiseControl.Display(commons);
            sequencer.Display(commons);
            combatRanger.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}


void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
{
    var command = argument.Trim().ToLower();
    if (command == "stop")
    {
        // TODO global modes
        cruiseControl.HandleCommand(commons, eventDriver, "cruise reset");
        new ReverseThrust().Init(commons, eventDriver);
    }
}

bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}
