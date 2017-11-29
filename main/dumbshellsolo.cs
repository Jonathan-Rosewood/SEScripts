//! Dumb Shell Controller
//@ shipcontrol eventdriver weapontrigger dumbshell
private readonly EventDriver eventDriver = new EventDriver();
private readonly WeaponTrigger weaponTrigger = new WeaponTrigger();
private readonly DumbShell dumbShell = new DumbShell();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, "CannonReference");

        weaponTrigger.Init(commons, eventDriver, (c, ed) => {
                dumbShell.Init(c, ed);
            });
    }

    eventDriver.Tick(commons, argAction: () => {
            weaponTrigger.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            dumbShell.Display(commons);
        });
}
