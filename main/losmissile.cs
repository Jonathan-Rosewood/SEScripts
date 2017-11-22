//! LOS Missile Controller
//@ shipcontrol eventdriver weapontrigger losguidance missilelaunch
//@ customdata
private readonly EventDriver eventDriver = new EventDriver();
private readonly WeaponTrigger weaponTrigger = new WeaponTrigger();
private readonly LOSGuidance losGuidance = new LOSGuidance();
private readonly MissileLaunch missileLaunch = new MissileLaunch();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

private bool FirstRun = true;

public static string MissileGroupSuffix = "";

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
        MissileGroupSuffix = customData.GetValue("suffix", "");
        Echo(string.Format("Group suffix is \"{0}\"", MissileGroupSuffix));

        weaponTrigger.Init(commons, eventDriver, (c,ed) => {
                // TODO This could be better. Should be external to missile so we
                // don't run into any build/PB start race conditions.
                shipOrientation.SetShipReference(c, MissileLaunch.SYSTEMS_GROUP + MissileGroupSuffix,
                                                 block => block is IMyGyro);

                missileLaunch.Init(c, ed, (c2,ed2) => {
                        losGuidance.Init(c2, ed2);
                    });
            });
    }

    eventDriver.Tick(commons, argAction: () => {
            weaponTrigger.HandleCommand(commons, eventDriver, argument);
            losGuidance.HandleCommand(commons, eventDriver, argument);
        });
}
