private readonly EventDriver eventDriver = new EventDriver(timerName: "Clock" + MISSILE_GROUP_SUFFIX, timerGroup: "CM Launch" + MISSILE_GROUP_SUFFIX);
public readonly LOSGuidance losGuidance = new LOSGuidance();
public readonly GuidanceKill guidanceKill = new GuidanceKill();
private readonly MissileLaunch missileLaunch = new MissileLaunch();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

private IMyTerminalBlock LauncherReference;

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
                    losGuidance.Init(c, ed);
                    guidanceKill.Init(c, ed);
                    if (CHEESY_BEAM_RIDING) ed.Schedule(1, CheesyBeamRiding);
                });
        // Acquire launcher and direction
        LauncherReference = losGuidance.SetLauncherReference(commons, "CM Launcher Reference");
    }

    eventDriver.Tick(commons);
}

public void CheesyBeamRiding(ZACommons commons, EventDriver eventDriver)
{
    losGuidance.SetLauncherReference(LauncherReference);
    eventDriver.Schedule(1, CheesyBeamRiding);
}
