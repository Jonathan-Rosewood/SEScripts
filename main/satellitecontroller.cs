public class SatellitePowerDrainHandler : BatteryManager.PowerDrainHandler
{
    private const string Message = "HELP! NET POWER LOSS!";

    private string OldAntennaName;

    public void PowerDrainStarted(ZACommons commons)
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

    public void PowerDrainEnded(ZACommons commons)
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

public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly BatteryManager batteryManager = new BatteryManager(new SatellitePowerDrainHandler());
public readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                  // GyroControl.Yaw,
                                                                                  GyroControl.Pitch,
                                                                                  GyroControl.Roll
);

public Base6Directions.Direction ShipUp, ShipForward;

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;

        // Look for our ship controllers
        var controllers = ZACommons.GetBlocksOfType<IMyShipController>(commons.Blocks);
        // Pick one. Assume they're all oriented the same.
        var reference = controllers.Count > 0 ? controllers[0] : null;
        if (reference != null)
        {
            ShipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
            ShipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);
        }
        else
        {
            // Default to grid up/forward
            ShipUp = Base6Directions.Direction.Up;
            ShipForward = Base6Directions.Direction.Forward;
        }

        eventDriver.Schedule(0.0);
    }

    batteryManager.HandleCommand(commons, argument);
    solarGyroController.HandleCommand(commons, argument,
                                      shipUp: ShipUp,
                                      shipForward: ShipForward);

    eventDriver.Tick(commons, () =>
            {
                batteryManager.Run(commons);
                solarGyroController.Run(commons,
                                        shipUp: ShipUp,
                                        shipForward: ShipForward);

                eventDriver.Schedule(1.0);
            });
}
