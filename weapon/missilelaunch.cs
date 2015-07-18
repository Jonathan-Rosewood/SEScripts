public class MissileLaunch
{
    private const int STATE_PRIME = 0;
    private const int STATE_RELEASE = 1;
    private const int STATE_BURN = 2;
    private const int STATE_ARM = 3;

    private const string BATTERY_GROUP = "CM Batteries";
    private const string SYSTEMS_GROUP = "CM Systems";
    private const string RELEASE_GROUP = "CM Release";
    private const string THRUST_GROUP = "CM Forward";

    private const float THRUST_FORCE = 12000.0f; // In newtons
    private const double BURN_TIME = 3.0; // In seconds

    private MissileGuidance missileGuidance;

    private int CurrentState = STATE_PRIME;
    private TimeSpan BurnStart;

    public void Init(MyGridProgram program, EventDriver eventDriver,
                     MissileGuidance missileGuidance)
    {
        this.missileGuidance = missileGuidance;
        eventDriver.Schedule(0, Run);
    }

    public void Run(MyGridProgram program, EventDriver eventDriver)
    {
        switch (CurrentState)
        {
            case STATE_PRIME:
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
                CurrentState = STATE_RELEASE;
                eventDriver.Schedule(0.5, EventDriver.Seconds, Run);
                break;
            case STATE_RELEASE:
                var releaseGroup = ZALibrary.GetBlockGroupWithName(program, RELEASE_GROUP);
                if (releaseGroup == null)
                {
                    throw new Exception("Group missing: " + RELEASE_GROUP);
                }

                ZALibrary.EnableBlocks(releaseGroup.Blocks, false);
                CurrentState = STATE_BURN;
                eventDriver.Schedule(0.5, EventDriver.Seconds, Run);
                break;
            case STATE_BURN:
                var thrustGroup = ZALibrary.GetBlockGroupWithName(program, THRUST_GROUP);
                if (thrustGroup == null)
                {
                    throw new Exception("Missing group: " + THRUST_GROUP);
                }
                ZAFlightLibrary.SetThrusterOverride(thrustGroup.Blocks, THRUST_FORCE);
                CurrentState = STATE_ARM;
                eventDriver.Schedule(BURN_TIME, EventDriver.Seconds, Run);
                break;
            case STATE_ARM:
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
                missileGuidance.Init(program, eventDriver);
                break;
        }
    }
}
