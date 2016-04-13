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
            for (var e = ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks).GetEnumerator(); e.MoveNext();)
            {
                var antenna = e.Current;

                if (antenna.IsFunctional && antenna.IsWorking)
                {
                    OldAntennaName = antenna.CustomName;
                    antenna.SetCustomName(Message);
                    break;
                }
            }
        }
        else
        {
            // Scan for the antenna with the message, change it back
            for (var e = ZACommons.GetBlocksOfType<IMyRadioAntenna>(commons.Blocks).GetEnumerator(); e.MoveNext();)
            {
                var antenna = e.Current;

                if (antenna.CustomName == Message)
                {
                    antenna.SetCustomName(OldAntennaName);
                    break;
                }
            }
        }
    }
}

private readonly EventDriver eventDriver = new EventDriver(timerGroup: RELAY_CLOCK_GROUP);
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
