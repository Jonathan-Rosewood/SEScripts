public class MissileLaunch
{
    private const string BATTERY_GROUP = "CM Batteries";
    private const string SYSTEMS_GROUP = "CM Systems";
    private const string RELEASE_GROUP = "CM Release";

    private const double BURN_TIME = 3.0; // In seconds

    private MissileGuidance missileGuidance;
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
                     MissileGuidance missileGuidance)
    {
        this.missileGuidance = missileGuidance;
        eventDriver.Schedule(0, Prime);
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

        eventDriver.Schedule(2.0, Release);
    }

    public void Release(MyGridProgram program, EventDriver eventDriver)
    {
        var releaseGroup = ZALibrary.GetBlockGroupWithName(program, RELEASE_GROUP + MISSILE_GROUP_SUFFIX);
        if (releaseGroup == null)
        {
            throw new Exception("Group missing: " + RELEASE_GROUP + MISSILE_GROUP_SUFFIX);
        }

        // Unlock any landing gear
        for (var e = releaseGroup.Blocks.GetEnumerator(); e.MoveNext();)
        {
            var gear = e.Current as IMyLandingGear;
            if (gear != null)
            {
                gear.GetActionWithName("Unlock").Apply(gear);
            }
        }
        // And then turn everything off (connectors, merge blocks, etc)
        ZALibrary.EnableBlocks(releaseGroup.Blocks, false);

        eventDriver.Schedule(0.1, Burn);
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
        missileGuidance.Init(program, eventDriver, thrustControl, gyroControl,
                             shipUp: ShipUp,
                             shipForward: ShipForward);
    }
}
