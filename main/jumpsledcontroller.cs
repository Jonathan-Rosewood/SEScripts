public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly BatteryManager batteryManager = new BatteryManager();
public readonly SolarGyroController solarGyroController = new SolarGyroController(
                                                                                  // GyroControl.Yaw,
                                                                                  GyroControl.Pitch,
                                                                                  GyroControl.Roll
                                                                                  );
public readonly SafeMode safeMode = new SafeMode();

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
        // The remote and cockpit should be oriented the same, so it doesn't matter which one we pick
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
                safeMode.Run(commons, false);

                eventDriver.Schedule(1.0);
            });
}
