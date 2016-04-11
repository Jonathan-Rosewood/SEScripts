//@ commons eventdriver
public class RandomDecoy
{
    private const uint FramesPerRun = 1;

    private readonly Random random = new Random();

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var decoys = ZACommons.GetBlocksOfType<IMyTerminalBlock>(commons.Blocks,
                                                                 block => block.DefinitionDisplayNameText == "Decoy" &&
                                                                 block.IsFunctional);
        if (decoys.Count == 0) return;

        int chosen = random.Next(decoys.Count);
        for (int i = 0; i < decoys.Count; i++)
        {
            var decoy = decoys[i];
            var enable = i == chosen;
            decoy.SetValue<bool>("OnOff", enable);
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
