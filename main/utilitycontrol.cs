//! Utility Controller
//@ shipcontrol eventdriver dockingmanager safemode smartundock
//@ batterymonitor redundancy emergencystop
//@ cruisecontrol vtvlhelper damagecontrol customdata
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
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
private readonly SmartUndock smartUndock = new SmartUndock();
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly VTVLHelper vtvlHelper = new VTVLHelper();
private readonly DamageControl damageControl = new DamageControl();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

private bool FirstRun = true;

private string VTVLHelperRemoteGroup;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation,
                                         shipGroup: SHIP_GROUP,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        customData.Parse(Me);
        VTVLHelperRemoteGroup = customData.GetString("referenceGroup", VTVLHELPER_REMOTE_GROUP);

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, VTVLHelperRemoteGroup);

        dockingManager.Init(commons, eventDriver, safeMode,
                            new BatteryMonitor(),
                            new RedundancyManager());
        smartUndock.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        vtvlHelper.Init(commons, eventDriver, customData, LivenessCheck);
        damageControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument, () =>
                    {
                        dockingManager.ManageShip(commons, eventDriver, false);
                    });
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            vtvlHelper.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            damageControl.Display(commons);
            cruiseControl.Display(commons);
            vtvlHelper.Display(commons);
            smartUndock.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}
