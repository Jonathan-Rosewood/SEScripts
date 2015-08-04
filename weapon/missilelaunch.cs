public class MissileLaunch
{
    private const string BATTERY_GROUP = "CM Batteries";
    private const string SYSTEMS_GROUP = "CM Systems";
    private const string RELEASE_GROUP = "CM Release";

    private const double BURN_TIME = 3.0; // In seconds

    private MissileGuidance missileGuidance;
    private Action<ZACommons, EventDriver>[] postLaunch;
    private ThrustControl thrustControl;
    private GyroControl gyroControl;

    private Base6Directions.Direction ShipUp, ShipForward;

    private void UpdateShipReference(ZACommons commons)
    {
        // Use the gyro in SYSTEMS_GROUP as reference
        var systemsGroup = commons.GetBlockGroupWithName(SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        if (systemsGroup == null)
        {
            throw new Exception("Group missing: " + SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        }
        var references = ZACommons.GetBlocksOfType<IMyGyro>(systemsGroup.Blocks);
        if (references.Count == 0)
        {
            throw new Exception("Expecting at least 1 gyro: " + SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        }
        var reference = references[0];

        ShipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
        ShipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);
    }

    public void Init(ZACommons commons, EventDriver eventDriver,
                     MissileGuidance missileGuidance,
                     params Action<ZACommons, EventDriver>[] postLaunch)
    {
        // Guidance is special because it needs more arguments
        this.missileGuidance = missileGuidance;
        this.postLaunch = postLaunch;
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
        ZACommons.EnableBlocks(batteries, true);
        ZACommons.SetBatteryRecharge(batteries, false);

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
                                                     gear => gear.GetActionWithName("Unlock").Apply(gear));
        // And then turn everything off (connectors, merge blocks, etc)
        ZACommons.EnableBlocks(releaseGroup.Blocks, false);

        eventDriver.Schedule(1.0, Burn);
    }

    public void Burn(ZACommons commons, EventDriver eventDriver)
    {
        // Boost away from launcher, initialize flight control
        UpdateShipReference(commons);

        thrustControl = new ThrustControl();
        thrustControl.Init(commons.Blocks, shipUp: ShipUp, shipForward: ShipForward);
        thrustControl.Reset();
        thrustControl.SetOverride(Base6Directions.Direction.Forward, BURN_FORCE);
        if (BURN_DOWNWARD) thrustControl.SetOverride(Base6Directions.Direction.Down);

        gyroControl = new GyroControl();
        gyroControl.Init(commons.Blocks, shipUp: ShipUp, shipForward: ShipForward);
        gyroControl.Reset();
        gyroControl.EnableOverride(true);

        eventDriver.Schedule(BURN_TIME, Arm);
    }

    public void Arm(ZACommons commons, EventDriver eventDriver)
    {
        if (BURN_DOWNWARD) thrustControl.SetOverride(Base6Directions.Direction.Down, 0.0f);

        // Just find all warheads on board and turn off safeties
        var warheads = ZACommons.GetBlocksOfType<IMyWarhead>(commons.Blocks);
        warheads.ForEach(warhead => warhead.SetValue<bool>("Safety", false));

        // We're done, let MissileGuidance take over
        missileGuidance.Init(commons, eventDriver, thrustControl, gyroControl,
                             shipUp: ShipUp,
                             shipForward: ShipForward);
        for (int i = 0; i < postLaunch.Length; i++)
        {
            postLaunch[i](commons, eventDriver);
        }
    }
}
