public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly SolarGyroController solarGyroController = new SolarGyroController(GyroControl.Roll);

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, "SolarGyroReference");

        eventDriver.Schedule(0.0);
    }

    solarGyroController.HandleCommand(commons, argument);

    eventDriver.Tick(commons, () =>
            {
                solarGyroController.Run(commons);

                eventDriver.Schedule(1.0);
            });
}
