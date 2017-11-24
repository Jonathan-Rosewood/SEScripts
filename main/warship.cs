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
private readonly CombatRanger combatRanger = new CombatRanger();
private readonly Stocker stocker = new Stocker();
private readonly ProjectorAction projectorAction = new ProjectorAction();
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

        shipOrientation.SetShipReference(commons, "CruiseControlReference");

        if (AUTO_CLOSE_DOORS_ENABLE) doorAutoCloser.Init(commons, eventDriver);
        if (SIMPLE_AIRLOCK_ENABLE) simpleAirlock.Init(commons, eventDriver);
        if (OXYGEN_MANAGER_ENABLE) oxygenManager.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
        damageControl.Init(commons, eventDriver);
        safeMode.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        speedAction.Init(commons, eventDriver);
        stocker.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
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

bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}

public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
{
    argument = argument.Trim().ToLower();
    if (argument == "firefirefire")
    {
        var group = commons.GetBlockGroupWithName(GC_FIRE_GROUP);
        if (group != null)
        {
            foreach (var block in group.Blocks)
            {
                if (block is IMyProgrammableBlock)
                {
                    ((IMyProgrammableBlock)block).TryRun("firefirefire");
                }
            }
        }
    }
}
