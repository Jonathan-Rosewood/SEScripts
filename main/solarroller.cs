private readonly SolarGyroController solarGyroController = new SolarGyroController(GyroControl.Roll);

private bool FirstRun = true;

private Base6Directions.Direction ShipUp, ShipForward;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        // See if there's a reference group
        var referenceGroup = ZALibrary.GetBlockGroupWithName(this, "SolarGyroReference");
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
    }

    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    solarGyroController.HandleCommand(this, ship, argument,
                                      shipUp: ShipUp,
                                      shipForward: ShipForward);
    solarGyroController.Run(this, ship,
                            shipUp: ShipUp,
                            shipForward: ShipForward);

    ZALibrary.KickLoopTimerBlock(this, argument);
}
