// Do NOT hold on to an instance of this class. Always instantiate a new one
// each run.
public class ZACommons
{
    public const StringComparison IGNORE_CASE = StringComparison.CurrentCultureIgnoreCase;

    public readonly MyGridProgram Program;
    private readonly string ShipGroupName;

    // All accessible blocks
    public List<IMyTerminalBlock> AllBlocks
    {
        get
        {
            // No multithreading makes lazy init so much easier
            if (m_allBlocks == null)
            {
                m_allBlocks = new List<IMyTerminalBlock>();
                Program.GridTerminalSystem.GetBlocks(m_allBlocks);
            }
            return m_allBlocks;
        }
    }
    private List<IMyTerminalBlock> m_allBlocks = null;

    // Blocks on the same grid only
    public List<IMyTerminalBlock> Blocks
    {
        get
        {
            if (m_blocks == null)
            {
                // Try group first, if it exists
                if (ShipGroupName != null)
                {
                    var group = GetBlockGroupWithName(ShipGroupName);
                    if (group != null) m_blocks = group.Blocks;
                }

                // Otherwise fetch all blocks on the same grid
                if (m_blocks == null)
                {
                    m_blocks = new List<IMyTerminalBlock>();
                    for (var e = AllBlocks.GetEnumerator(); e.MoveNext();)
                    {
                        var block = e.Current;
                        if (block.CubeGrid == Program.Me.CubeGrid) m_blocks.Add(block);
                    }
                }
            }
            return m_blocks;
        }
    }
    private List<IMyTerminalBlock> m_blocks = null;

    public List<IMyBlockGroup> Groups
    {
        get
        {
            if (m_groups == null)
            {
                m_groups = new List<IMyBlockGroup>();
                Program.GridTerminalSystem.GetBlockGroups(m_groups);
            }
            return m_groups;
        }
    }
    private List<IMyBlockGroup> m_groups = null;

    // NB Names are actually lowercased
    public Dictionary<string, IMyBlockGroup> GroupsByName
    {
        get
        {
            if (m_groupsByName == null)
            {
                m_groupsByName = new Dictionary<string, IMyBlockGroup>();
                for (var e = Groups.GetEnumerator(); e.MoveNext();)
                {
                    var group = e.Current;
                    m_groupsByName.Add(group.Name.ToLower(), group);
                }
            }
            return m_groupsByName;
        }
    }
    private Dictionary<string, IMyBlockGroup> m_groupsByName = null;

    public ZACommons(MyGridProgram program, string shipGroup = null)
    {
        Program = program;
        ShipGroupName = shipGroup;
    }

    // Groups

    public IMyBlockGroup GetBlockGroupWithName(string name)
    {
        IMyBlockGroup group;
        if (GroupsByName.TryGetValue(name.ToLower(), out group))
        {
            return group;
        }
        return null;
    }

    public List<IMyBlockGroup> GetBlockGroupsWithPrefix(string prefix)
    {
        var result = new List<IMyBlockGroup>();
        for (var e = Groups.GetEnumerator(); e.MoveNext();)
        {
            var group = e.Current;
            if (group.Name.StartsWith(prefix, IGNORE_CASE)) result.Add(group);
        }
        return result;
    }

    // Blocks

    public static List<T> GetBlocksOfType<T>(IEnumerable<IMyTerminalBlock> blocks,
                                             Func<T, bool> collect = null)
    {
        List<T> list = new List<T>();
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var block = e.Current;
            if (block is T && (collect == null || collect((T)block))) list.Add((T)block);
        }
        return list;
    }

    public static T GetBlockWithName<T>(IEnumerable<IMyTerminalBlock> blocks, string name)
        where T : IMyTerminalBlock
    {
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var block = e.Current;
            if(block is T && block.CustomName.Equals(name, IGNORE_CASE)) return (T)block;
        }
        return default(T);
    }

    public static List<IMyTerminalBlock> SearchBlocksOfName(IEnumerable<IMyTerminalBlock> blocks, string name, Func<IMyTerminalBlock, bool> collect = null)
    {
        var result = new List<IMyTerminalBlock>();
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var block = e.Current;
            if (block.CustomName.IndexOf(name, IGNORE_CASE) >= 0 &&
                (collect == null || collect(block)))
            {
                result.Add(block);
            }
        }
        return result;
    }

    public static void ForEachBlockOfType<T>(IEnumerable<IMyTerminalBlock> blocks, Action<T> action)
    {
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var block = e.Current;
            if (block is T)
            {
                action((T)block);
            }
        }
    }

    public static void EnableBlocks(IEnumerable<IMyTerminalBlock> blocks, bool enabled)
    {
        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var block = e.Current;
            // Not all blocks will implement IMyFunctionalBlock, so can't checked Enabled
            block.GetActionWithName(enabled ? "OnOff_On" : "OnOff_Off").Apply(block);
        }
    }

    public IMyProgrammableBlock Me
    {
        get { return Program.Me; }
    }

    // Batteries

    public static bool IsBatteryRecharging(IMyBatteryBlock battery)
    {
        return !battery.ProductionEnabled;
    }

    public static void SetBatteryRecharge(IMyBatteryBlock battery, bool recharge)
    {
        var recharging = IsBatteryRecharging(battery);
        if ((recharging && !recharge) || (!recharging && recharge))
        {
            battery.GetActionWithName("Recharge").Apply(battery);
        }
    }

    public static void SetBatteryRecharge(IEnumerable<IMyBatteryBlock> batteries, bool recharge)
    {
        for (var e = batteries.GetEnumerator(); e.MoveNext();)
        {
            var battery = e.Current;
            SetBatteryRecharge(battery, recharge);
        }
    }

    // Display

    public Action<string> Echo
    {
        get { return Program.Echo; }
    }

    public static string FormatPower(float value)
    {
        if (value >= 1.0f)
        {
            return string.Format("{0:F2} MW", value);
        }
        else if (value >= 0.001)
        {
            return string.Format("{0:F2} kW", value * 1000f);
        }
        else
        {
            return string.Format("{0:F2} W", value * 1000000f);
        }
    }

    // Misc

    public static bool StartTimerBlockWithName(IEnumerable<IMyTerminalBlock> blocks, string name,
                                               Func<IMyTimerBlock, bool> condition = null)
    {
        var timer = GetBlockWithName<IMyTimerBlock>(blocks, name);
        if (timer != null && timer.Enabled && !timer.IsCountingDown &&
            (condition == null || condition(timer)))
        {
            timer.GetActionWithName("Start").Apply(timer);
            return true;
        }
        return false;
    }

    public static bool IsConnectedAnywhere(IEnumerable<IMyShipConnector> connectors)
    {
        for (var e = connectors.GetEnumerator(); e.MoveNext();)
        {
            var connector = e.Current;
            if (connector.IsLocked && connector.IsConnected)
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsConnectedAnywhere(IEnumerable<IMyTerminalBlock> blocks)
    {
        return IsConnectedAnywhere(GetBlocksOfType<IMyShipConnector>(blocks));
    }
}
