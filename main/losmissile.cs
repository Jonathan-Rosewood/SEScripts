//! LOS Missile Controller
//@ shipcontrol eventdriver weapontrigger losguidance missilelaunch
private readonly EventDriver eventDriver = new EventDriver();
private readonly WeaponTrigger weaponTrigger = new WeaponTrigger();
private readonly LOSGuidance losGuidance = new LOSGuidance();
private readonly MissileLaunch missileLaunch = new MissileLaunch();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

private IMyTerminalBlock LauncherReference;

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

        weaponTrigger.Init(commons, eventDriver, (c,ed) => {
                // TODO This could be better. Should be external to missile so we
                // don't run into any build/PB start race conditions.
                shipOrientation.SetShipReference(c, MissileLaunch.SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX,
                                                 block => block is IMyGyro);

                missileLaunch.Init(c, ed, (c2,ed2) => {
                        losGuidance.Init(c2, ed2);
                        if (CHEESY_BEAM_RIDING) ed2.Schedule(1, CheesyBeamRiding);
                    });
                // Acquire launcher and direction
                LauncherReference = losGuidance.SetLauncherReference(c, "CM Launcher Reference");
            });
    }

    eventDriver.Tick(commons, preAction: () => {
            weaponTrigger.HandleCommand(commons, eventDriver, argument);
        });
}

public void CheesyBeamRiding(ZACommons commons, EventDriver eventDriver)
{
    losGuidance.SetLauncherReference(LauncherReference);
    eventDriver.Schedule(1, CheesyBeamRiding);
}
