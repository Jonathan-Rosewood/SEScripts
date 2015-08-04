public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    Base6Directions.Direction shipUp, shipForward;

    // See if there's a reference group
    var referenceGroup = commons.GetBlockGroupWithName("SolarGyroReference");
    var reference = (referenceGroup != null && referenceGroup.Blocks.Count > 0) ? referenceGroup.Blocks[0] : null;
    if (reference != null)
    {
        shipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
        shipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);
    }
    else
    {
        // Default to grid up/forward
        shipUp = Base6Directions.Direction.Up;
        shipForward = Base6Directions.Direction.Forward;
    }

    solarGyroController.HandleCommand(commons, argument,
                                      shipUp: shipUp,
                                      shipForward: shipForward);

    eventDriver.Tick(commons, () =>
            {
                solarGyroController.Run(commons,
                                        shipUp: shipUp,
                                        shipForward: shipForward);

                eventDriver.Schedule(1.0);
            });
}
