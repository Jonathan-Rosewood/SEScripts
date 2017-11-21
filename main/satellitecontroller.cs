//! MUP Controller
//@ shipcontrol eventdriver safemode batterymonitor redundancy damagecontrol
//@ reactormanager solargyrocontroller
public class SatelliteLowBatteryHandler : BatteryMonitor.LowBatteryHandler
{
    private const string Message = "HELP! NET POWER LOSS!";

    private string OldAntennaName;

    public void LowBattery(ZACommons commons, EventDriver eventDriver,
                           bool started)
    {
        if (started)
        {
            // Just change the name of the first active antenna
            foreach (var antenna in ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks))
            {
                if (antenna.IsFunctional && antenna.IsWorking)
                {
                    OldAntennaName = antenna.CustomName;
                    antenna.CustomName = Message;
                    break;
                }
            }
        }
        else
        {
            // Scan for the antenna with the message, change it back
            foreach (var antenna in ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks))
            {
                if (antenna.CustomName == Message)
                {
                    antenna.CustomName = OldAntennaName;
                    break;
                }
            }
        }
    }
}

private readonly EventDriver eventDriver = new EventDriver();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor(new SatelliteLowBatteryHandler());
private readonly RedundancyManager redundancyManager = new RedundancyManager();
private readonly DamageControl damageControl = new DamageControl();
private readonly ReactorManager reactorManager = new ReactorManager();
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);

        safeMode.Init(commons, eventDriver);
        batteryMonitor.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
        reactorManager.Init(commons, eventDriver);
        solarGyroController.Init(commons, eventDriver);
        damageControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            damageControl.HandleCommand(commons, eventDriver, argument);
            reactorManager.HandleCommand(commons, eventDriver, argument);
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
            damageControl.Display(commons);
        });
}
