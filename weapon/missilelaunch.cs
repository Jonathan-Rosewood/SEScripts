public class MissileLaunch
{
    private const string BATTERY_GROUP = "CM Batteries";
    private const string SYSTEMS_GROUP = "CM Systems";
    private const string RELEASE_GROUP = "CM Release";
    private const string THRUST_GROUP = "CM Forward";

    private const double BURN_TIME = 3.0; // In seconds

    private MissileGuidance missileGuidance;
    private GyroControl gyroControl;

    public void Init(MyGridProgram program, EventDriver eventDriver,
                     MissileGuidance missileGuidance)
    {
        this.missileGuidance = missileGuidance;
        eventDriver.Schedule(0, Prime);
    }

    public void Prime(MyGridProgram program, EventDriver eventDriver)
    {
        // Wake up batteries
        var batteryGroup = ZALibrary.GetBlockGroupWithName(program, BATTERY_GROUP);
        if (batteryGroup == null)
        {
            throw new Exception("Group missing: " + BATTERY_GROUP);
        }
        var systemsGroup = ZALibrary.GetBlockGroupWithName(program, SYSTEMS_GROUP);
        if (systemsGroup == null)
        {
            throw new Exception("Group missing: " + SYSTEMS_GROUP);
        }

        var batteries = ZALibrary.GetBlocksOfType<IMyBatteryBlock>(batteryGroup.Blocks);
        ZALibrary.EnableBlocks(batteries, true);
        ZALibrary.SetBatteryRecharge(batteries, false);

        // Activate flight systems
        ZALibrary.EnableBlocks(systemsGroup.Blocks, true);

        eventDriver.Schedule(2.0, Release);
    }

    public void Release(MyGridProgram program, EventDriver eventDriver)
    {
        var releaseGroup = ZALibrary.GetBlockGroupWithName(program, RELEASE_GROUP);
        if (releaseGroup == null)
        {
            throw new Exception("Group missing: " + RELEASE_GROUP);
        }

        ZALibrary.EnableBlocks(releaseGroup.Blocks, false);

        eventDriver.Schedule(0.5, Burn);
    }

    public void Burn(MyGridProgram program, EventDriver eventDriver)
    {
        // Boost away from launcher, initialize flight control
        var thrustGroup = ZALibrary.GetBlockGroupWithName(program, THRUST_GROUP);
        if (thrustGroup == null)
        {
            throw new Exception("Missing group: " + THRUST_GROUP);
        }

        var thrustControl = new ThrustControl();
        thrustControl.Init(program, blocks: thrustGroup.Blocks);
        thrustControl.SetOverride(Base6Directions.Direction.Forward); // Max thrust

        gyroControl = new GyroControl();
        gyroControl.Init(program);
        gyroControl.Reset();
        gyroControl.EnableOverride(true);

        eventDriver.Schedule(BURN_TIME, Arm);
    }

    public void Arm(MyGridProgram program, EventDriver eventDriver)
    {
        // Just find all warheads on board and turn off safeties
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocks(blocks);
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var warhead = e.Current as IMyWarhead;
            if (warhead != null)
            {
                warhead.SetValue<bool>("Safety", false);
            }
        }

        // We're done, let MissileGuidance take over
        missileGuidance.Init(program, eventDriver, gyroControl);
    }
}
