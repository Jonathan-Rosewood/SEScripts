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
private readonly DropHelper dropHelper = new DropHelper();
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
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: SHIP_NAME,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, DROPHELPER_REMOTE_GROUP);

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
    }

    eventDriver.Tick(commons, preAction: () => {
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            dropHelper.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
