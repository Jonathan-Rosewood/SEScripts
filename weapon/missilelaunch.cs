//@ shipcontrol eventdriver
public class MissileLaunch
{
    private const string BATTERY_GROUP = "CM Batteries";
    public const string SYSTEMS_GROUP = "CM Systems";
    private const string RELEASE_GROUP = "CM Release";

    private Action<ZACommons, EventDriver> PostLaunch;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Action<ZACommons, EventDriver> postLaunch = null)
    {
        PostLaunch = postLaunch;
        eventDriver.Schedule(0.0, Prime);
    }

    public void Prime(ZACommons commons, EventDriver eventDriver)
    {
        // Wake up batteries
        var batteryGroup = commons.GetBlockGroupWithName(BATTERY_GROUP + MISSILE_GROUP_SUFFIX);
        if (batteryGroup == null)
        {
            throw new Exception("Group missing: " + BATTERY_GROUP + MISSILE_GROUP_SUFFIX);
        }
        var systemsGroup = commons.GetBlockGroupWithName(SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        if (systemsGroup == null)
        {
            throw new Exception("Group missing: " + SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
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

        eventDriver.Schedule(1.0, Release);
    }

    public void Release(ZACommons commons, EventDriver eventDriver)
    {
        var releaseGroup = commons.GetBlockGroupWithName(RELEASE_GROUP + MISSILE_GROUP_SUFFIX);
        if (releaseGroup == null)
        {
            throw new Exception("Group missing: " + RELEASE_GROUP + MISSILE_GROUP_SUFFIX);
        }

        // Unlock any landing gear
        ZACommons.ForEachBlockOfType<IMyLandingGear>(releaseGroup.Blocks,
                                                     gear => gear.ApplyAction("Unlock"));
        // And then turn everything off (connectors, merge blocks, etc)
        ZACommons.EnableBlocks(releaseGroup.Blocks, false);

        eventDriver.Schedule(1.0, Burn);
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        // Boost away from launcher, initialize flight control
        var shipControl = (ShipControlCommons)commons;

        shipControl.Reset(gyroOverride: true, thrusterEnable: null);

        // Initiate main burn here, otherwise do it later
        var thrustControl = shipControl.ThrustControl;
        if (!BURN_DOWNWARD)
        {
            thrustControl.SetOverride(Base6Directions.Direction.Forward, BURN_FRACTION);
            eventDriver.Schedule(BURN_TIME, Arm);
        }
        else
        {
            thrustControl.SetOverride(Base6Directions.Direction.Down);
            eventDriver.Schedule(BURN_DOWNWARD_TIME, MainBurn);
        }
    }

    public void MainBurn(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        var thrustControl = shipControl.ThrustControl;
        thrustControl.SetOverride(Base6Directions.Direction.Down, false);
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
        if (PostLaunch != null) PostLaunch(commons, eventDriver);
    }
}
