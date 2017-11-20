//! PN Missile Controller
//@ shipcontrol eventdriver weapontrigger oneturn pnguidance missilelaunch
private readonly EventDriver eventDriver = new EventDriver();
private readonly WeaponTrigger weaponTrigger = new WeaponTrigger();
private readonly OneTurn oneTurn = new OneTurn();
private readonly ProNavGuidance proNavGuidance = new ProNavGuidance();
private readonly MissileLaunch missileLaunch = new MissileLaunch();

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

        weaponTrigger.Init(commons, eventDriver, (c,ed) => {
                // TODO This could be better. Should be external to missile so we
                // don't run into any build/PB start race conditions.
                shipOrientation.SetShipReference(c, MissileLaunch.SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX,
                                                 block => block is IMyGyro);

                missileLaunch.Init(c, ed, (c2,ed2) => {
                        oneTurn.Init(c2, ed2, (c3,ed3) => {
                                proNavGuidance.Init(c3, ed3);
                            });
                    });
                // Acquire the target here so it fails early if missing
                oneTurn.AcquireTarget(c);
                proNavGuidance.AcquireTarget(c, ed);
            });
    }

    eventDriver.Tick(commons, preAction: () => {
            weaponTrigger.HandleCommand(commons, eventDriver, argument);
            proNavGuidance.HandleCommand(commons, eventDriver, argument);
        });
}
