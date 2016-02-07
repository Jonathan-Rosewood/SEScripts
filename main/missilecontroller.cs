private readonly EventDriver eventDriver = new EventDriver();
public readonly MissileGuidance missileGuidance = new MissileGuidance();
public readonly GuidanceKill guidanceKill = new GuidanceKill();
private readonly MissileLaunch missileLaunch = new MissileLaunch();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, MissileLaunch.SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX,
                                         block => block is IMyGyro);

        missileLaunch.Init(commons, eventDriver, (c,ed) =>
                {
                    missileGuidance.Init(c, ed);
                    guidanceKill.Init(c, ed);
                });
        // Acquire the target here so it fails early if missing
        missileGuidance.AcquireTarget(commons);
    }

    eventDriver.Tick(commons);
}
