private readonly EventDriver eventDriver = new EventDriver();
private readonly MissileGuidance missileGuidance = new MissileGuidance();
private readonly RandomDecoy randomDecoy = new RandomDecoy();
private readonly MissileLaunch missileLaunch = new MissileLaunch();
private readonly MissilePayload missilePayload = new MissilePayload();

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        missileLaunch.Init(this, eventDriver, missileGuidance);
        missilePayload.Init(this, eventDriver);
        randomDecoy.Init(this, eventDriver);
        // Guidance will be initialized by launch,
        // but we'll acquire the target here so it fails early if missing
        missileGuidance.AcquireTarget(this);
    }

    eventDriver.Tick(this);
}
