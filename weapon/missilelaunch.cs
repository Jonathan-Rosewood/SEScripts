public class MissileLaunch
{
    private const string BATTERY_GROUP = "CM Batteries";
    private const string SYSTEMS_GROUP = "CM Systems";
    private const string RELEASE_GROUP = "CM Release";

    private const double BURN_TIME = 3.0; // In seconds

    private MissileGuidance missileGuidance;
    private Action<MyGridProgram, EventDriver>[] postLaunch;
    private ThrustControl thrustControl;
    private GyroControl gyroControl;

    private Base6Directions.Direction ShipUp, ShipForward;

    private void UpdateShipReference(MyGridProgram program)
    {
        // Use the gyro in SYSTEMS_GROUP as reference
        var systemsGroup = ZALibrary.GetBlockGroupWithName(program, SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        if (systemsGroup == null)
        {
            throw new Exception("Group missing: " + SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        }
        var references = ZALibrary.GetBlocksOfType<IMyGyro>(systemsGroup.Blocks);
        if (references.Count == 0)
        {
            throw new Exception("Expecting at least 1 gyro: " + SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        }
        var reference = references[0];

        ShipUp = reference.Orientation.TransformDirection(Base6Directions.Direction.Up);
        ShipForward = reference.Orientation.TransformDirection(Base6Directions.Direction.Forward);
    }

    public void Init(MyGridProgram program, EventDriver eventDriver,
                     MissileGuidance missileGuidance,
                     params Action<MyGridProgram, EventDriver>[] postLaunch)
    {
        // Guidance is special because it needs more arguments
        this.missileGuidance = missileGuidance;
        this.postLaunch = postLaunch;
        eventDriver.Schedule(0.0, Prime);
    }

    public void Prime(MyGridProgram program, EventDriver eventDriver)
    {
        // Wake up batteries
        var batteryGroup = ZALibrary.GetBlockGroupWithName(program, BATTERY_GROUP + MISSILE_GROUP_SUFFIX);
        if (batteryGroup == null)
        {
            throw new Exception("Group missing: " + BATTERY_GROUP + MISSILE_GROUP_SUFFIX);
        }
        var systemsGroup = ZALibrary.GetBlockGroupWithName(program, SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        if (systemsGroup == null)
        {
            throw new Exception("Group missing: " + SYSTEMS_GROUP + MISSILE_GROUP_SUFFIX);
        }

        var batteries = ZALibrary.GetBlocksOfType<IMyBatteryBlock>(batteryGroup.Blocks);
        ZALibrary.EnableBlocks(batteries, true);
        ZALibrary.SetBatteryRecharge(batteries, false);

        // Activate flight systems
        ZALibrary.EnableBlocks(systemsGroup.Blocks, true);

        eventDriver.Schedule(1.0, Release);
    }

    public void Release(MyGridProgram program, EventDriver eventDriver)
    {
        var releaseGroup = ZALibrary.GetBlockGroupWithName(program, RELEASE_GROUP + MISSILE_GROUP_SUFFIX);
        if (releaseGroup == null)
        {
            throw new Exception("Group missing: " + RELEASE_GROUP + MISSILE_GROUP_SUFFIX);
        }

        // Unlock any landing gear
        ZALibrary.ForEachBlockOfType<IMyLandingGear>(releaseGroup.Blocks,
                                                     gear => gear.GetActionWithName("Unlock").Apply(gear));
        // And then turn everything off (connectors, merge blocks, etc)
        ZALibrary.EnableBlocks(releaseGroup.Blocks, false);

        eventDriver.Schedule(1.0, Burn);
    }

    public void Burn(MyGridProgram program, EventDriver eventDriver)
    {
        // Boost away from launcher, initialize flight control
        UpdateShipReference(program);

        thrustControl = new ThrustControl();
        thrustControl.Init(program, shipUp: ShipUp, shipForward: ShipForward);
        thrustControl.Reset();
        thrustControl.SetOverride(Base6Directions.Direction.Forward, 80000.0f);

        gyroControl = new GyroControl();
        gyroControl.Init(program, shipUp: ShipUp, shipForward: ShipForward);
        gyroControl.Reset();
        gyroControl.EnableOverride(true);

        eventDriver.Schedule(BURN_TIME, Arm);
    }

    public void Arm(MyGridProgram program, EventDriver eventDriver)
    {
        // Just find all warheads on board and turn off safeties
        List<IMyTerminalBlock> warheads = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocksOfType<IMyWarhead>(warheads);
        warheads.ForEach(warhead => warhead.SetValue<bool>("Safety", false));

        // We're done, let MissileGuidance take over
        missileGuidance.Init(program, eventDriver, thrustControl, gyroControl,
                             shipUp: ShipUp,
                             shipForward: ShipForward);
        for (int i = 0; i < postLaunch.Length; i++)
        {
            postLaunch[i](program, eventDriver);
        }
    }
}
