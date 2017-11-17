//! Relay Satellite Controller
//@ shipcontrol eventdriver launchcontroller batterymonitor solargyrocontroller
public class RelaySatelliteLowBatteryHandler : BatteryMonitor.LowBatteryHandler
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
private readonly LaunchController launchController = new LaunchController();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor(new RelaySatelliteLowBatteryHandler());
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            GyroControl.Yaw,
                            GyroControl.Pitch
                            //GyroControl.Roll
                            );

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, LaunchController.REMOTE_GROUP);

        launchController.Init(commons, eventDriver, (c,ed) =>
                {
                    batteryMonitor.Init(c, ed);
                    solarGyroController.Init(c, ed);
                });
    }

    eventDriver.Tick(commons, preAction: () => {
            solarGyroController.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            solarGyroController.Display(commons);
        });
}
