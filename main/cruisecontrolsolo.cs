public readonly EventDriver eventDriver = new EventDriver(timerGroup: "CruiseControlClock");
public readonly CruiseControl cruiseControl = new CruiseControl();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: "CruiseControlGroup");

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, "CruiseControlReference");
    }

    eventDriver.Tick(commons, preAction: () =>
            {
                cruiseControl.HandleCommand(commons, eventDriver, argument);
            });
}
