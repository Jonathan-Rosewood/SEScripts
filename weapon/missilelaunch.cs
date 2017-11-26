//@ shipcontrol eventdriver standardmissile
public class MissileLaunch
{
    private Action<ZACommons, EventDriver> PostLaunch;

    public bool Launched { get; private set; }

    public MissileLaunch()
    {
        Launched = false;
    }

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Action<ZACommons, EventDriver> postLaunch)
    {
        PostLaunch = postLaunch;
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
                    battery.SetValue<bool>("OnOff", true);
                    battery.SetValue<bool>("Recharge", false);
                    battery.SetValue<bool>("Discharge", true);
                });

        // Activate flight systems
        ZACommons.EnableBlocks(systemsGroup.Blocks, true);

        eventDriver.Schedule(0.5, Release);
    }

    public void Release(ZACommons commons, EventDriver eventDriver)
    {
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

        eventDriver.Schedule(0.5, Burn);
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        // Initialize flight control
        var shipControl = (ShipControlCommons)commons;

        shipControl.Reset(gyroOverride: true, thrusterEnable: null);

        // Initiate main burn here, otherwise do it later
        var thrustControl = shipControl.ThrustControl;
        if (!DETACH_BURN)
        {
            thrustControl.SetOverride(Base6Directions.Direction.Forward, BURN_FRACTION);
            eventDriver.Schedule(BURN_TIME, Arm);
        }
        else
        {
            // Boost away from launcher
            thrustControl.SetOverride(DETACH_BURN_DIRECTION);
            eventDriver.Schedule(DETACH_BURN_TIME, MainBurn);
        }
    }

    public void MainBurn(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        var thrustControl = shipControl.ThrustControl;
        thrustControl.SetOverride(DETACH_BURN_DIRECTION, false);
        thrustControl.SetOverride(Base6Directions.Direction.Forward, BURN_FRACTION);

        eventDriver.Schedule(BURN_TIME, Arm);
    }

    public void Arm(ZACommons commons, EventDriver eventDriver)
    {
        // Reset and let guidance control thrust
        var thrustControl = ((ShipControlCommons)commons).ThrustControl;
        thrustControl.Reset();

        // Find all warheads on board and turn off safeties
        var warheads = ZACommons.GetBlocksOfType<IMyWarhead>(commons.Blocks);
        warheads.ForEach(warhead => warhead.SetValue<bool>("Safety", false));

        // We're done, let other systems take over
        Launched = true;
        PostLaunch(commons, eventDriver);
    }
}
