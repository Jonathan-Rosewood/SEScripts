public class RandomDecoy
{
    private const uint FramesPerRun = 5;

    private readonly Random random = new Random();

    public void Run(MyGridProgram program, EventDriver eventDriver)
    {
        var decoys = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(decoys,
                                                                     block => block.DefinitionDisplayNameText == "Decoy");
        if (decoys.Count == 0) return;

        int chosen = random.Next(decoys.Count);
        for (int i = 0; i < decoys.Count; i++)
        {
            var decoy = decoys[i];
            var enable = i == chosen;
            decoy.GetActionWithName(enable ? "OnOff_On" : "OnOff_Off").Apply(decoy);
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }
}
