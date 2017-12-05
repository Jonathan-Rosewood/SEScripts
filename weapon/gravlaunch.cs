//@ shipcontrol eventdriver standardmissile
public class GravLaunch
{
    private Action<ZACommons, EventDriver> PostLaunch;

    private double ReleaseDelay;

    public bool Launched { get; private set; }

    public GravLaunch()
    {
        Launched = false;
    }

    public void Init(ZACommons commons, EventDriver eventDriver, ZACustomData customData, 
                     Action<ZACommons, EventDriver> postLaunch)
    {
        PostLaunch = postLaunch;
        ReleaseDelay = customData.GetDouble("releaseDelay");
        eventDriver.Schedule(0, Prime);
    }

    public void Prime(ZACommons commons, EventDriver eventDriver)
    {
        // Wake up batteries
        var batteryGroup = commons.GetBlockGroupWithName(StandardMissile.BATTERY_GROUP + MissileGroupSuffix);
        if (batteryGroup == null)
        {
            throw new Exception("Group missing: " + StandardMissile.BATTERY_GROUP + MissileGroupSuffix);
        }
        var systemsGroup = commons.GetBlockGroupWithName(StandardMissile.SYSTEMS_GROUP + MissileGroupSuffix);
        if (systemsGroup == null)
        {
            throw new Exception("Group missing: " + StandardMissile.SYSTEMS_GROUP + MissileGroupSuffix);
        }

        var batteries = ZACommons.GetBlocksOfType<IMyBatteryBlock>(batteryGroup.Blocks);
        batteries.ForEach(battery =>
                {
                    battery.Enabled = true;
                    battery.SetValue<bool>("Recharge", false);
                    battery.SetValue<bool>("Discharge", true);
                });

        // Activate flight systems
        ZACommons.EnableBlocks(systemsGroup.Blocks, true);

        eventDriver.Schedule(0.1+ReleaseDelay, Release);
    }

    public void Release(ZACommons commons, EventDriver eventDriver)
    {
        // Enable mass
        var group = commons.GetBlockGroupWithName(StandardMissile.MASS_GROUP + MissileGroupSuffix);
        if (group != null)  ZACommons.EnableBlocks(group.Blocks, true);

        var releaseGroup = commons.GetBlockGroupWithName(StandardMissile.RELEASE_GROUP + MissileGroupSuffix);
        if (releaseGroup == null)
        {
            throw new Exception("Group missing: " + StandardMissile.RELEASE_GROUP + MissileGroupSuffix);
        }

        // Unlock any landing gear
        ZACommons.ForEachBlockOfType<IMyLandingGear>(releaseGroup.Blocks,
                                                     gear => gear.ApplyAction("Unlock"));
        // And then turn everything off (connectors, merge blocks, etc)
        ZACommons.EnableBlocks(releaseGroup.Blocks, false);

        // Initialize flight control
        var shipControl = (ShipControlCommons)commons;

        shipControl.Reset(gyroOverride: true, thrusterEnable: null);

        eventDriver.Schedule(0.1, Demass);
    }

    public void Demass(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        var shipController = shipControl.ShipController;
        if (shipController == null || shipController.GetArtificialGravity().LengthSquared() == 0.0)
        {
            // Disable mass
            var group = commons.GetBlockGroupWithName(StandardMissile.MASS_GROUP + MissileGroupSuffix);
            if (group != null)  ZACommons.EnableBlocks(group.Blocks, false);

            // All done (or remote is gone), begin arming sequence
            eventDriver.Schedule(0, Arm);
        }
        else
        {
            eventDriver.Schedule(1, Demass);
        }
    }

    public void Arm(ZACommons commons, EventDriver eventDriver)
    {
        // Find all warheads on board and turn off safeties
        var warheads = ZACommons.GetBlocksOfType<IMyWarhead>(commons.Blocks);
        warheads.ForEach(warhead => warhead.SetValue<bool>("Safety", false));

        // We're done, let other systems take over
        Launched = true;
        PostLaunch(commons, eventDriver);
    }
}
