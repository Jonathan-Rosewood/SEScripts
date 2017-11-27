//! Grav Assist PN Missile Controller
//@ shipcontrol eventdriver weapontrigger pnguidance gravlaunch
//@ standardmissile customdata
private readonly EventDriver eventDriver = new EventDriver();
private readonly WeaponTrigger weaponTrigger = new WeaponTrigger();
private readonly ProNavGuidance proNavGuidance = new ProNavGuidance();
private readonly GravLaunch gravLaunch = new GravLaunch();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

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

        customData.Parse(Me);
        MissileGroupSuffix = customData.GetString("suffix");
        Echo(string.Format("Group suffix is \"{0}\"", MissileGroupSuffix));

        weaponTrigger.Init(commons, eventDriver, (c,ed) => {
                // TODO This could be better. Should be external to missile so we
                // don't run into any build/PB start race conditions.
                shipOrientation.SetShipReference(c, StandardMissile.SYSTEMS_GROUP + MissileGroupSuffix,
                                                 block => block is IMyGyro);

                gravLaunch.Init(c, ed, customData, (c2,ed2) => {
                        proNavGuidance.Init(c2, ed2);
                    });
            });
    }

    eventDriver.Tick(commons, argAction: () => {
            weaponTrigger.HandleCommand(commons, eventDriver, argument);
            proNavGuidance.HandleCommand(commons, eventDriver, argument);
        });
}
