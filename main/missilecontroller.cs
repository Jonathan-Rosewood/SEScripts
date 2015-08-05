private readonly EventDriver eventDriver = new EventDriver(timerGroup: "CM Launch" + MISSILE_GROUP_SUFFIX);
private readonly MissileGuidance missileGuidance = new MissileGuidance();
private readonly RandomDecoy randomDecoy = new RandomDecoy();
private readonly MissileLaunch missileLaunch = new MissileLaunch();
private readonly MissilePayload missilePayload = new MissilePayload();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, MissileLaunch.SYSTEMS_GROUP,
                                         block => block is IMyGyro);

        missileLaunch.Init(commons, eventDriver, missileGuidance, missilePayload.Init,
                           randomDecoy.Init);
        // Guidance, payload, decoy will be initialized by launch,
        // but we'll acquire the target here so it fails early if missing
        missileGuidance.AcquireTarget(commons);
    }

    eventDriver.Tick(commons);
}
