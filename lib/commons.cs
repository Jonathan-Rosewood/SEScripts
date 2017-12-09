// Do NOT hold on to an instance of this class. Always instantiate a new one
// each run.
public class ZACommons
{
    public const StringComparison IGNORE_CASE = StringComparison.CurrentCultureIgnoreCase;

    public readonly MyGridProgram Program;
    public readonly UpdateType UpdateType;
    private readonly string ShipGroupName;

    private readonly ZAStorage Storage;
    public bool IsDirty { get; private set; }

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
                    foreach (var block in AllBlocks)
                    {
                        if (block.CubeGrid == Program.Me.CubeGrid) m_blocks.Add(block);
                    }
                }
            }
            return m_blocks;
        }
    }
    private List<IMyTerminalBlock> m_blocks = null;

    public class BlockGroup
    {
        private readonly IMyBlockGroup MyBlockGroup;

        public BlockGroup(IMyBlockGroup myBlockGroup)
        {
            MyBlockGroup = myBlockGroup;
        }

        public String Name
        {
            get { return MyBlockGroup.Name; }
        }

        public List<IMyTerminalBlock> Blocks
        {
            get
            {
                if (m_blocks == null)
                {
                    m_blocks = new List<IMyTerminalBlock>();
                    MyBlockGroup.GetBlocks(m_blocks);
                }
                return m_blocks;
            }
        }
        private List<IMyTerminalBlock> m_blocks = null;
    }

    public List<BlockGroup> Groups
    {
        get
        {
            if (m_groups == null)
            {
                var groups = new List<IMyBlockGroup>();
                Program.GridTerminalSystem.GetBlockGroups(groups);
                m_groups = new List<BlockGroup>();
                groups.ForEach(group => m_groups.Add(new BlockGroup(group)));
            }
            return m_groups;
        }
    }
    private List<BlockGroup> m_groups = null;

    // NB Names are actually lowercased
    public Dictionary<string, BlockGroup> GroupsByName
    {
        get
        {
            if (m_groupsByName == null)
            {
                m_groupsByName = new Dictionary<string, BlockGroup>();
                foreach (var group in Groups)
                {
                    m_groupsByName.Add(group.Name.ToLower(), group);
                }
            }
            return m_groupsByName;
        }
    }
    private Dictionary<string, BlockGroup> m_groupsByName = null;

    public ZACommons(MyGridProgram program, UpdateType updateType,
                     string shipGroup = null, ZAStorage storage = null)
    {
        Program = program;
        UpdateType = updateType;
        ShipGroupName = shipGroup;
        Storage = storage;
        IsDirty = false;
    }

    // Groups

    public BlockGroup GetBlockGroupWithName(string name)
    {
        BlockGroup group;
        if (GroupsByName.TryGetValue(name.ToLower(), out group))
        {
            return group;
        }
        return null;
    }

    public List<BlockGroup> GetBlockGroupsWithPrefix(string prefix)
    {
        var result = new List<BlockGroup>();
        foreach (var group in Groups)
        {
            if (group.Name.StartsWith(prefix, IGNORE_CASE)) result.Add(group);
        }
        return result;
    }

    // Blocks

    public static List<T> GetBlocksOfType<T>(IEnumerable<IMyTerminalBlock> blocks,
                                             Func<T, bool> collect = null)
    {
        var list = new List<T>();
        foreach (var block in blocks)
        {
            if (block is T && (collect == null || collect((T)block))) list.Add((T)block);
        }
        return list;
    }

    public static T GetBlockWithName<T>(IEnumerable<IMyTerminalBlock> blocks, string name)
        where T : IMyTerminalBlock
    {
        foreach (var block in blocks)
        {
            if(block is T && block.CustomName.Equals(name, IGNORE_CASE)) return (T)block;
        }
        return default(T);
    }

    public static List<IMyTerminalBlock> SearchBlocksOfName(IEnumerable<IMyTerminalBlock> blocks, string name, Func<IMyTerminalBlock, bool> collect = null)
    {
        var result = new List<IMyTerminalBlock>();
        foreach (var block in blocks)
        {
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
        foreach (var block in blocks)
        {
            if (block is T)
            {
                action((T)block);
            }
        }
    }

    public static void EnableBlocks(IEnumerable<IMyTerminalBlock> blocks, bool enabled)
    {
        foreach (var block in blocks)
        {
            // Not all blocks will implement IMyFunctionalBlock, so can't checked Enabled
            block.SetValue<bool>("OnOff", enabled);
        }
    }

    public IMyProgrammableBlock Me
    {
        get { return Program.Me; }
    }

    // Display

    public Action<string> Echo
    {
        get { return Program.Echo; }
    }

    // Misc

    public static bool StartTimerBlockWithName(IEnumerable<IMyTerminalBlock> blocks, string name,
                                               Func<IMyTimerBlock, bool> condition = null)
    {
        var timer = GetBlockWithName<IMyTimerBlock>(blocks, name);
        if (timer != null && timer.Enabled && !timer.IsCountingDown &&
            (condition == null || condition(timer)))
        {
            timer.ApplyAction("Start");
            return true;
        }
        return false;
    }

    public static bool IsConnectedAnywhere(IEnumerable<IMyTerminalBlock> connectors)
    {
        foreach (var block in connectors)
        {
            var connector = block as IMyShipConnector;
            if (connector != null && connector.Status == MyShipConnectorStatus.Connected)
            {
                return true;
            }
        }
        return false;
    }

    /* BROKEN 01.102
    public static bool IsConnectedAnywhere(IEnumerable<IMyTerminalBlock> blocks)
    {
        return IsConnectedAnywhere(GetBlocksOfType<IMyShipConnector>(blocks));
    }
    */

    // Storage

    public void SetValue(string key, string value)
    {
        if (Storage != null)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Storage.Data[key] = value;
            }
            else
            {
                Storage.Data.Remove(key);
            }
            IsDirty = true;
        }
    }

    public string GetValue(string key)
    {
        string value;
        if (Storage != null && Storage.Data.TryGetValue(key, out value))
        {
            return value;
        }
        return null;
    }
}

// This should have a longer scope than ZACommons, hence a separate class
public class ZAStorage
{
    private const char KEY_DELIM = '\\';
    private const char PAIR_DELIM = '$';
    private readonly string PAIR_DELIM_STR = new string(PAIR_DELIM, 1);

    public readonly Dictionary<string, string> Data = new Dictionary<string, string>();

    public string Encode()
    {
        var encoded = new List<string>();

        foreach (var kv in Data)
        {
            ValidityCheck(kv.Key);
            ValidityCheck(kv.Value);
            var pair = new StringBuilder();
            pair.Append(kv.Key);
            pair.Append(KEY_DELIM);
            pair.Append(kv.Value);
            encoded.Add(pair.ToString());
        }

        return string.Join(PAIR_DELIM_STR, encoded);
    }

    public void Decode(string data)
    {
        Data.Clear();

        var pairs = data.Split(PAIR_DELIM);
        for (int i = 0; i < pairs.Length; i++)
        {
            var parts = pairs[i].Split(new char[] { KEY_DELIM }, 2);
            if (parts.Length == 2)
            {
                Data[parts[0]] = parts[1];
            }
        }
    }

    private void ValidityCheck(string value)
    {
        // Yeah... not gonna bother with escape sequences and such.
        if (value.IndexOf(KEY_DELIM) >= 0 ||
            value.IndexOf(PAIR_DELIM) >= 0)
        {
            throw new Exception(string.Format("String '{0}' cannot be used by ZAStorage!", value));
        }
    }
}
