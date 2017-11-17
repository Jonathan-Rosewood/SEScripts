//! Jump Sled Controller
//@ shipcontrol eventdriver batterymonitor oxygenmanager
//@ timerkicker reactormanager damagecontrol
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
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly OxygenManager oxygenManager = new OxygenManager();
private readonly TimerKicker timerKicker = new TimerKicker();
private readonly ReactorManager reactorManager = new ReactorManager();
private readonly DamageControl damageControl = new DamageControl();
private readonly SafeMode safeMode = new SafeMode();
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

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);

        batteryMonitor.Init(commons, eventDriver);
        oxygenManager.Init(commons, eventDriver);
        timerKicker.Init(commons, eventDriver);
        reactorManager.Init(commons, eventDriver);

        safeMode.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        solarGyroController.ConditionalInit(commons, eventDriver, true);
        damageControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            reactorManager.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
            damageControl.Display(commons);
            cruiseControl.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}


bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
}
