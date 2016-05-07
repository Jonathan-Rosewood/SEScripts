//! Dumb Shell Controller
//@ shipcontrol eventdriver dumbshell
private readonly EventDriver eventDriver = new EventDriver(timerName: "ShellClock");
private readonly DumbShell dumbShell = new DumbShell();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, "CannonReference");

        dumbShell.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons);
}
