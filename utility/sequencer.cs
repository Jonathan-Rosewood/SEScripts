//@ commons eventdriver
public class Sequencer
{
    private Dictionary<string, int> Indexes = new Dictionary<string, int>();

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 3);
        if (parts.Length < 3 || parts[0] != "sequence") return;
        var command = parts[1];
        var sequence = parts[2];

        if (command == "start")
        {
            var blocks = GetSequenceBlocks(commons, sequence);
            if (blocks == null) return;

            ZACommons.EnableBlocks(blocks, false);
            blocks[0].SetValue<bool>("OnOff", true);

            var first = Indexes.Count == 0;
            if (!Indexes.ContainsKey(sequence)) Indexes.Add(sequence, 0);
            if (first)
            {
                eventDriver.Schedule(SEQUENCER_FRAMES_PER_RUN, Run);
            }
        }
        else if (command == "stop")
        {
            var blocks = GetSequenceBlocks(commons, sequence);
            if (blocks != null) ZACommons.EnableBlocks(blocks, true);

            Indexes.Remove(sequence);
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (Indexes.Count == 0) return;

        var newIndexes = new Dictionary<string, int>();

        foreach (var kv in Indexes)
        {
            var sequence = kv.Key;
            var index = kv.Value;

            var blocks = GetSequenceBlocks(commons, sequence);
            if (blocks == null) continue;
            ZACommons.EnableBlocks(blocks, false);

            // TODO sort?
            index++;
            index %= blocks.Count;

            blocks[index].SetValue<bool>("OnOff", true);

            newIndexes.Add(sequence, index);
        }

        Indexes = newIndexes;

        eventDriver.Schedule(SEQUENCER_FRAMES_PER_RUN, Run);
    }

    public void Display(ZACommons commons)
    {
        if (Indexes.Count > 0)
        {
            commons.Echo("Sequencer active: " + string.Join(", ", Indexes.Keys));
        }
    }

    private List<IMyTerminalBlock> GetSequenceBlocks(ZACommons commons,
                                                     string sequence)
    {
        var groupName = SEQUENCER_PREFIX + sequence;
        var group = commons.GetBlockGroupWithName(groupName);
        if (group == null || group.Blocks.Count == 0) return null;
        return group.Blocks;
    }
}
