public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly SolarGyroController solarGyroController = new SolarGyroController(GyroControl.Roll);

public Base6Directions.Direction ShipUp, ShipForward;

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;

        // See if there's a reference group
        var referenceGroup = commons.GetBlockGroupWithName("SolarGyroReference");
        var reference = (referenceGroup != null && referenceGroup.Blocks.Count > 0) ? referenceGroup.Blocks[0] : null;
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

    solarGyroController.HandleCommand(commons, argument,
                                      shipUp: ShipUp,
                                      shipForward: ShipForward);

    eventDriver.Tick(commons, () =>
            {
                solarGyroController.Run(commons,
                                        shipUp: ShipUp,
                                        shipForward: ShipForward);

        eventDriver.Schedule(1.0);
            });
}
