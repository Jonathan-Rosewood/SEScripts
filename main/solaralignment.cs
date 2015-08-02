private readonly EventDriver eventDriver = new EventDriver(timerName: ZALIBRARY_LOOP_TIMER_BLOCK_NAME);
private readonly SolarGyroController solarGyroController =
    new SolarGyroController(
                            //GyroControl.Yaw,
                            GyroControl.Pitch,
                            GyroControl.Roll
                            );

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    Base6Directions.Direction shipUp, shipForward;

    // See if there's a reference group
    var referenceGroup = ZALibrary.GetBlockGroupWithName(this, "SolarGyroReference");
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

    ZALibrary.Ship ship = new ZALibrary.Ship(this);
    solarGyroController.HandleCommand(this, ship, argument,
                                      shipUp: shipUp,
                                      shipForward: shipForward);

    if (eventDriver.Tick(this))
    {
        solarGyroController.Run(this, ship,
                                shipUp: shipUp,
                                shipForward: shipForward);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}
