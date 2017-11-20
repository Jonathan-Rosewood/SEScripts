//! Missile Controller
//@ shipcontrol eventdriver weapontrigger missileguidance guidancekill missilelaunch
private readonly EventDriver eventDriver = new EventDriver();
private readonly WeaponTrigger weaponTrigger = new WeaponTrigger();
public readonly MissileGuidance missileGuidance = new MissileGuidance();
public readonly GuidanceKill guidanceKill = new GuidanceKill();
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
                        missileGuidance.Init(c2, ed2);
                        guidanceKill.Init(c2, ed2);
                    });
                // Acquire the target here so it fails early if missing
                missileGuidance.AcquireTarget(c);
            });
    }

    eventDriver.Tick(commons, preAction: () => {
            weaponTrigger.HandleCommand(commons, eventDriver, argument);
        });
}
