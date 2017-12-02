//! Large Lander Controller
//@ shipcontrol eventdriver safemode redundancy doorautocloser simpleairlock
//@ cruisecontrol vtvlhelper damagecontrol reactormanager
//@ batterymonitor solargyrocontroller oxygenmanager airventmanager
//@ emergencystop customdata
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
private readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly SimpleAirlock simpleAirlock = new SimpleAirlock();
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly VTVLHelper vtvlHelper = new VTVLHelper();
private readonly DamageControl damageControl = new DamageControl();
private readonly ReactorManager reactorManager = new ReactorManager();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly AirVentManager airVentManager = new AirVentManager();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

private bool FirstRun = true;

private string VTVLHelperRemoteGroup;
private bool OxygenManagerEnable, AirVentManagerEnable, LanderCarrierEnable;

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
        OxygenManagerEnable = customData.GetBool("oxygenManager", OXYGEN_MANAGER_ENABLE);
        AirVentManagerEnable = customData.GetBool("airVentManager", AIR_VENT_MANAGER_ENABLE);
        LanderCarrierEnable = customData.GetBool("landerCarrier", LANDER_CARRIER_ENABLE);

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, VTVLHelperRemoteGroup);

        safeMode.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
        doorAutoCloser.Init(commons, eventDriver);
        simpleAirlock.Init(commons, eventDriver);
        if (LanderCarrierEnable) reactorManager.Init(commons, eventDriver);
        batteryMonitor.Init(commons, eventDriver);
        solarGyroController.ConditionalInit(commons, eventDriver);
        if (OxygenManagerEnable) oxygenManager.Init(commons, eventDriver);
        if (AirVentManagerEnable) airVentManager.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        vtvlHelper.Init(commons, eventDriver, customData, LivenessCheck);
        damageControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            vtvlHelper.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
            reactorManager.HandleCommand(commons, eventDriver, argument);
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
            damageControl.Display(commons);
            cruiseControl.Display(commons);
            vtvlHelper.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}
