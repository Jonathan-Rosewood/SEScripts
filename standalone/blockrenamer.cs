//! Block Renamer
public static HashSet<string> EXCLUDED_BLOCK_TYPES = new HashSet<string>(new string[] {
        "Antenna",
        "Beacon",
        "Camera",
    });

public static HashSet<string> EXCLUDED_BLOCK_NAMES = new HashSet<string>(new string[] {
        "Emergency Stop",
        "Low Battery",
        "Safe Mode",
    });

public class BlockRenamer
{
    public void Run(MyGridProgram program, string prefix)
    {
        prefix = prefix.Trim();

        // Get list of all blocks
        var blocks = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocks(blocks);

        var countsByType = new Dictionary<string, uint>();

        for (var e = blocks.GetEnumerator(); e.MoveNext();)
        {
            var block = e.Current;

            if (EXCLUDED_BLOCK_TYPES.Contains(block.DefinitionDisplayNameText)) continue;
            if (EXCLUDED_BLOCK_NAMES.Contains(block.CustomName)) continue;

            var builder = new StringBuilder();
            builder.Append(prefix);
            builder.Append(' ');
            builder.Append(block.DefinitionDisplayNameText);
            var genericName = builder.ToString();

            // If it starts with a generic prefixed name, look at it closer..
            if (block.CustomName == genericName)
            {
                // No need to check more
            }
            else if (block.CustomName.StartsWith(genericName + " "))
            {
                var rest = block.CustomName.Substring(genericName.Length + 1);
                // See if it's a simple number
                uint num;
                if (rest.Length > 0 && !uint.TryParse(rest, out num))
                {
                    // Can't parse it, must be customized
                    continue;
                }
            }
            else if (block.CustomName == prefix ||
                     block.CustomName.StartsWith(prefix + " "))
            {
                // Otherwise, if it already has the prefix, leave it alone
                continue;
            }
            
            uint count;
            if (!countsByType.TryGetValue(block.DefinitionDisplayNameText, out count))
            {
                count = 0;
            }
            count++;
            countsByType[block.DefinitionDisplayNameText] = count;

            if (count > 1)
            {
                builder.Append(' ');
                builder.Append(count);
            }

            block.SetCustomName(builder);
        }
    }
}

void Main(string argument)
{
    new BlockRenamer().Run(this, argument);
}
