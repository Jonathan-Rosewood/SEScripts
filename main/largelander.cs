//! Large Lander Controller
//@ shipcontrol eventdriver safemode redundancy doorautocloser simpleairlock
//@ cruisecontrol vtvlhelper damagecontrol reactormanager timerkicker
//@ batterymonitor solargyrocontroller oxygenmanager airventmanager
//@ emergencystop gravitysurveyor
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
private readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DoorAutoCloser doorAutoCloser = new DoorAutoCloser();
private readonly SimpleAirlock simpleAirlock = new SimpleAirlock();
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly VTVLHelper vtvlHelper = new VTVLHelper();
private readonly DamageControl damageControl = new DamageControl();
private readonly ReactorManager reactorManager = new ReactorManager();
private readonly TimerKicker timerKicker = new TimerKicker();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly AirVentManager airVentManager = new AirVentManager();
private readonly GravitySurveyor gravitySurveyor = new GravitySurveyor(VTVLHELPER_TARGET_GROUP);
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

private Rangefinder.LineSample first;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: SHIP_GROUP,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, VTVLHELPER_REMOTE_GROUP);

        safeMode.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
        doorAutoCloser.Init(commons, eventDriver);
        simpleAirlock.Init(commons, eventDriver);
        if (LANDER_CARRIER_ENABLE)
        {
            reactorManager.Init(commons, eventDriver);
            timerKicker.Init(commons, eventDriver);
        }
        batteryMonitor.Init(commons, eventDriver);
        solarGyroController.ConditionalInit(commons, eventDriver);
        if (OXYGEN_MANAGER_ENABLE) oxygenManager.Init(commons, eventDriver);
        if (AIR_VENT_MANAGER_ENABLE) airVentManager.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        vtvlHelper.Init(commons, eventDriver, LivenessCheck);
        damageControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            vtvlHelper.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
            reactorManager.HandleCommand(commons, eventDriver, argument);
            solarGyroController.HandleCommand(commons, eventDriver, argument);
            gravitySurveyor.HandleCommand(commons, argument, vtvlHelper.GetRemoteControl);
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
